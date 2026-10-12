using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Nối input kéo thả với luật chơi (BoardModel) và các view (SelectionBoxView, PlacedBlockView).
    /// Luật nằm ở BoardModel; script này chỉ điều phối và phát event.
    /// Cơ chế thời gian (như bản gốc): vùng hợp luật là đặt luôn, không hỏi lời giải; đặt sai thì chạm khối để gỡ.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        private GameplayVisuals _visuals;
        private LevelTimer _timer;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelTimer timer, GameplayVisuals visuals)
        {
            _timer = timer;
            _visuals = visuals;
        }

        [SerializeField] private GridSpawner gridSpawner;
        [SerializeField] private SelectionBoxView selectionBox;

        [Header("Khối mèo (lấy từ pool, không tạo mới mỗi lần đặt)")]
        [SerializeField] private PlacedBlockView blockPrefab;
        [SerializeField] private Transform blockRoot;
        [SerializeField] private int blockPoolCapacity = 64;

        [Header("Thắng")]
        [SerializeField] private float winFlashDelay = 0.1f;

        [Header("Feel khi kéo / đặt")]
        [Tooltip("Ô dưới ngón tay \"dính\" thêm bao nhiêu ô mỗi phía trước khi chuyển sang ô bên cạnh (0 = tắt)")]
        [SerializeField, Range(0f, 0.4f)] private float cellStick = 0.15f;
        [Tooltip("Đặt khối tiếp trong khoảng này (giây) thì tính combo")]
        [SerializeField, Min(0f)] private float comboWindow = 2.5f;

        [Header("Events")]
        public UnityEvent onGameWin = new UnityEvent();
        public UnityEvent<BlockModel> onBlockPlaced = new UnityEvent<BlockModel>();
        public UnityEvent<BlockModel> onBlockRemoved = new UnityEvent<BlockModel>();             // chạm khối để gỡ / booster gỡ khối sai
        public UnityEvent<Vector3, Vector2> onMistake = new UnityEvent<Vector3, Vector2>();     // (tâm, kích thước) khối sai khi lưới kín mà chưa đúng
        public UnityEvent<PlacementResult, Vector3, Vector2> onInvalidMove = new UnityEvent<PlacementResult, Vector3, Vector2>(); // thao tác sai luật, chỉ hủy
        public UnityEvent<CellModel> onClueUnlocked = new UnityEvent<CellModel>();
        public UnityEvent<Vector2Int> onDragCellChanged = new UnityEvent<Vector2Int>(); // ngón tay đang kéo sang ô khác – rung tick nhẹ
        public UnityEvent<int, SelectionHint> onSelectionChanged = new UnityEvent<int, SelectionHint>(); // vùng kéo đổi số ô / trạng thái; (0, Neutral) = hết kéo

        private BoardModel _board;
        private readonly Dictionary<BlockModel, PlacedBlockView> _blockViews = new Dictionary<BlockModel, PlacedBlockView>();
        private readonly List<CellModel> _unlockedBuffer = new List<CellModel>();
        private readonly List<BlockModel> _wrongBuffer = new List<BlockModel>();
        private ObjectPool<PlacedBlockView> _blockPool;
        private PlacedBlockView _preview; // mèo xem trước của lượt kéo hiện tại
        private ColorType _dragColor;     // màu khung chọn của lượt kéo -> màu khối mèo
        private readonly DragSelection _drag = new DragSelection();
        private bool _isGameWon = false;
        private bool _finishing; // đã thả tay, đang chờ khung giãn xong rồi mới đặt / báo sai
        private Vector2Int _fingerCell; // ô dưới ngón tay ở lần kéo trước
        private SelectionHint _selectionHint;
        private ComboCounter _combo;

        public BoardModel Board => _board;
        public bool IsGameWon => _isGameWon;
        public bool IsGameOver => _timer != null && _timer.IsTimeUp;
        /// <summary>
        /// Khóa kéo thả: trạng thái riêng của lưới + InputGate (ván không Playing, intro / tutorial, Reveal đang tự đặt khối...).
        /// Lớp phủ mới chỉ cần InputGate.Lock(this), không sửa ở đây.
        /// </summary>
        public bool IsInputLocked => _isGameWon || IsGameOver || _finishing
            || (gridSpawner != null && gridSpawner.IsIntroPlaying) // lưới đang xuất hiện
            || InputGate.IsLocked;
        public LevelDefinition CurrentLevel => gridSpawner != null ? gridSpawner.LevelData : null;

        /// <summary>Combo đặt khối liên tiếp (đọc trong onBlockPlaced): 1 = lần đặt lẻ, 2+ = đặt nhanh liên tục.</summary>
        public int Combo => ComboTracker.Count;
        private ComboCounter ComboTracker => _combo ??= new ComboCounter(comboWindow);

        private void Awake()
        {
            if (selectionBox == null) Debug.LogError("[GridManager] Chưa gán SelectionBoxView");
            if (blockPrefab == null) Debug.LogError("[GridManager] Chưa gán PlacedBlock prefab");
            if (blockRoot == null) blockRoot = transform;

            _blockPool = new ObjectPool<PlacedBlockView>(
                createFunc: () =>
                {
                    PlacedBlockView b = Instantiate(blockPrefab, blockRoot);
                    b.Init(_visuals); // pool truyền dependency ngay khi tạo (guide 8.3)
                    return b;
                },
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b =>
                {
                    b.ResetView();
                    b.gameObject.SetActive(false);
                },
                // Thoát Play mode: scene hủy object trước, rồi Editor mới Clear() pool -> object có thể đã null
                actionOnDestroy: b => { if (b != null) Destroy(b.gameObject); },
                collectionCheck: false,
                defaultCapacity: blockPoolCapacity,
                maxSize: blockPoolCapacity * 4);
        }

        private void OnEnable()
        {
            if (gridSpawner != null)
            {
                gridSpawner.gridCellEvent.AddListener(SetGrid);
            }
        }

        private void OnDisable()
        {
            if (gridSpawner != null)
            {
                gridSpawner.gridCellEvent.RemoveListener(SetGrid);
            }
        }

        // ---------- Input ----------

        /// <summary>Bắt đầu kéo tại 1 ô. Ô Void / ô đã có khối thì không kéo được.</summary>
        public bool BeginSelection(Vector2Int anchor)
        {
            if (IsInputLocked || _board == null) return false;

            CellModel cell = _board.GetCell(anchor);
            if (cell == null || cell.IsOccupied) return false;

            _drag.Begin(anchor);
            _fingerCell = anchor;
            RefreshSelection();
            return true;
        }

        /// <summary>Ngón tay ở điểm world này. Vùng chọn giãn theo nhưng không phủ lên khối đã đặt / ô Void.</summary>
        public void UpdateSelection(Vector3 fingerWorld)
        {
            if (!_drag.IsActive || _board == null) return;

            GridGeometry geometry = _board.Geometry;
            Vector2Int cell = FeelCurves.StickyCell(geometry, fingerWorld, _fingerCell, cellStick);
            if (cell != _fingerCell)
            {
                _fingerCell = cell;
                onDragCellChanged?.Invoke(cell);
            }
            if (_drag.MoveTo(cell, _board)) RefreshSelection();

            // Hướng bị chặn: đẩy vào khối đã đặt / ô Void, hoặc ngón tay ra ngoài lưới ở mép vùng chọn
            Vector2Int dir = _drag.BlockDir;
            Vector2Int raw = geometry.WorldToCellUnclamped(fingerWorld);
            RectInt rect = _drag.Rect;
            if (dir.x == 0 && (raw.x < 0 && rect.xMin == 0 || raw.x >= geometry.Width && rect.xMax == geometry.Width)) dir.x = raw.x < 0 ? -1 : 1;
            if (dir.y == 0 && (raw.y < 0 && rect.yMin == 0 || raw.y >= geometry.Height && rect.yMax == geometry.Height)) dir.y = raw.y < 0 ? -1 : 1;
            if (selectionBox != null) selectionBox.SetBlocked(dir);
        }

        /// <summary>Người chơi đang kéo (bàn tay tutorial ẩn đi).</summary>
        public bool IsSelecting => _drag.IsActive;

        /// <summary>Số ô của vùng đang kéo, dùng cho bong bóng đếm ô.</summary>
        public int SelectionCellCount => _drag.IsActive ? _drag.CellCount : 0;

        /// <summary>Vùng đang kéo (chỉ có nghĩa khi IsSelecting).</summary>
        public RectInt SelectionRect => _drag.Rect;

        /// <summary>Trạng thái vùng đang kéo (đủ / sai / đang kéo), dùng cho bong bóng đếm ô.</summary>
        public SelectionHint SelectionHint => _drag.IsActive ? _selectionHint : SelectionHint.Neutral;

        /// <summary>
        /// Khung chọn bám vùng. Vùng đủ điều kiện: khung giãn hết vào vùng rồi mới biến thành mèo xem trước
        /// (SelectionBoxView.WhenSettled); kéo sang vùng khác trước khi giãn xong thì không hiện mèo.
        /// </summary>
        private void RefreshSelection()
        {
            RectInt rect = _drag.Rect;
            GridGeometry geometry = _board.Geometry;
            if (selectionBox != null)
            {
                selectionBox.Show(geometry.RectCenter(rect), geometry.RectSize(rect));
                _dragColor = selectionBox.ColorType;
            }

            PlacementCheck check = _board.Check(rect);
            _selectionHint = check.Hint;
            onSelectionChanged?.Invoke(_drag.CellCount, _selectionHint);
            if (_preview != null && !(check.IsReady && _preview.Rect.Equals(rect))) ReleasePreview();
            if (selectionBox != null) selectionBox.SetFilled(true);

            if (!check.IsReady)
            {
                if (selectionBox != null) selectionBox.WhenSettled(null);
                return;
            }
            if (_preview != null) return; // đã là mèo của đúng vùng này

            if (selectionBox != null) selectionBox.WhenSettled(() => ShowPreviewFor(rect));
            else ShowPreviewFor(rect);
        }

        /// <summary>Khung đã giãn xong vào vùng đủ điều kiện -> hiện mèo, khung mờ đi.</summary>
        private void ShowPreviewFor(RectInt rect)
        {
            if (_board == null || (_drag.IsActive && !_drag.Rect.Equals(rect))) return;
            PlacementCheck check = _board.Check(rect);
            if (!check.IsReady) return;

            if (_preview == null) _preview = _blockPool.Get();
            if (!_preview.IsPreview || !_preview.Rect.Equals(rect)) _preview.ShowPreview(rect, check.ClueCell, _dragColor, _board.Geometry);
            if (selectionBox != null) selectionBox.SetFilled(false);
        }

        /// <summary>
        /// Thả tay: vùng đủ điều kiện thì đặt khối (không hỏi lời giải), chưa đủ thì hủy.
        /// Đủ điều kiện mà khung chưa giãn xong thì chờ khung giãn hết, hiện mèo, rồi mới đặt.
        /// </summary>
        public void EndSelection()
        {
            if (!_drag.IsActive) return;
            RectInt rect = _drag.Rect;
            _drag.End();
            onSelectionChanged?.Invoke(0, SelectionHint.Neutral);

            // Khi đã win hoặc hết giờ -> Khóa toàn bộ thao tác
            if (IsInputLocked || _board == null)
            {
                HideSelectionBox();
                ReleasePreview();
                return;
            }

            PlacementCheck check = _board.Check(rect);

            if (check.Result == PlacementResult.Ignored)
            {
                HideSelectionBox();
                ReleasePreview();
                return;
            }

            if (!check.IsReady)
            {
                // Khung co lại, xám đi rồi tắt (thay vì tắt cái rụp) -> người chơi hiểu là bị từ chối
                if (selectionBox != null) selectionBox.HideRejected();
                ReleasePreview();
                Reject(check);
                return;
            }

            if (_preview != null || selectionBox == null || selectionBox.IsSettled)
            {
                HideSelectionBox();
                CommitSelection(check);
                return;
            }

            // Khung đang giãn dở: khóa thao tác, chờ giãn xong
            _finishing = true;
            selectionBox.WhenSettled(() =>
            {
                _finishing = false;
                HideSelectionBox();
                CommitSelection(check);
            });
        }

        private void HideSelectionBox()
        {
            if (selectionBox != null) selectionBox.Hide();
        }

        /// <summary>Vùng đủ điều kiện sau khi thả tay: hiện mèo (nếu chưa) rồi đặt.</summary>
        private void CommitSelection(PlacementCheck check)
        {
            RectInt rect = check.Rect;
            if (_preview == null)
            {
                _preview = _blockPool.Get();
                _preview.ShowPreview(rect, check.ClueCell, _dragColor, _board.Geometry);
            }

            PlacedBlockView view = _preview;
            _preview = null;
            PlaceBlock(check, view);
        }

        /// <summary>Hủy lượt kéo (bị khóa input, đổi level...).</summary>
        public void CancelSelection()
        {
            _finishing = false; // Hide() bỏ luôn callback chờ khung giãn xong
            if (_drag.IsActive)
            {
                _drag.End();
                onSelectionChanged?.Invoke(0, SelectionHint.Neutral);
            }
            if (selectionBox != null) selectionBox.Hide();
            ReleasePreview();
        }

        // ---------- Kết quả ----------

        private void PlaceBlock(PlacementCheck check, PlacedBlockView view)
        {
            _unlockedBuffer.Clear();
            BlockModel block = _board.Place(check, _dragColor, _unlockedBuffer);

            view.Confirm(block.ClueCell);
            _blockViews[block] = view;
            ComboTracker.Window = comboWindow;
            ComboTracker.Register(Time.time);

            foreach (CellModel cell in _unlockedBuffer) onClueUnlocked?.Invoke(cell);
            onBlockPlaced?.Invoke(block);

            // Phủ kín lưới: đúng hết -> thắng; còn khối "?" sai số ô -> mèo sai rung, người chơi chạm để gỡ
            if (!_board.IsFull()) return;
            _wrongBuffer.Clear();
            if (_board.IsSolved(_wrongBuffer)) HandleGameWin();
            else ShowWrongBlocks(_wrongBuffer);
        }

        private void ShowWrongBlocks(List<BlockModel> wrong)
        {
            Debug.LogWarning($"<color=red>[GridManager] Lưới kín nhưng {wrong.Count} khối sai số ô -> chạm để gỡ</color>");
            GridGeometry geometry = _board.Geometry;
            foreach (BlockModel block in wrong)
            {
                if (_blockViews.TryGetValue(block, out PlacedBlockView view) && view != null) view.PlayWrong();
                onMistake?.Invoke(geometry.RectCenter(block.Rect), geometry.RectSize(block.Rect));
            }
        }

        /// <summary>Chạm 1 ô đang có khối: gỡ khối đó. Trả về true nếu đã gỡ.</summary>
        public bool TryRemoveBlockAt(Vector2Int pos)
        {
            if (IsInputLocked || _board == null || _drag.IsActive) return false;
            BlockModel block = _board.GetBlockAt(pos);
            if (block == null) return false;
            RemoveBlock(block);
            return true;
        }

        private void RemoveBlock(BlockModel block)
        {
            if (!_board.Remove(block)) return;
            ComboTracker.Reset(); // gỡ khối là đứt combo
            if (_blockViews.TryGetValue(block, out PlacedBlockView view))
            {
                _blockViews.Remove(block);
                if (view != null) view.PlayRemove(() => _blockPool.Release(view));
            }
            onBlockRemoved?.Invoke(block);
        }

        /// <summary>
        /// Booster (Hint / Reveal) đặt 1 khối của lời giải: không qua kéo thả. Khối sai đang đè lên vùng thì gỡ trước.
        /// Màu random như khung chọn. Trả về false nếu vùng không đặt được (ô số còn khóa...).
        /// </summary>
        public bool PlaceSolved(RectInt rect)
        {
            if (_board == null || _isGameWon || IsGameOver) return false;
            List<BlockModel> overlapping = _board.GetBlocksOverlapping(rect);
            if (overlapping.Count == 1 && overlapping[0].Rect.Equals(rect)) return false; // đã đúng sẵn
            if (overlapping.Count > 0)
            {
                // Ô số còn khóa (hoặc khóa lại vì gỡ khối) thì không đặt được: giữ nguyên lưới
                if (!_board.CanPlaceReplacing(rect, overlapping)) return false;
                foreach (BlockModel b in overlapping) RemoveBlock(b);
            }
            PlacementCheck check = _board.Check(rect);
            if (!check.IsReady) return false;

            CancelSelection();
            _dragColor = ColorTypeExtensions.GetRandomColorType();
            PlacedBlockView view = _blockPool.Get();
            view.ShowPreview(rect, check.ClueCell, _dragColor, _board.Geometry);
            PlaceBlock(check, view);
            return true;
        }

        private void Reject(PlacementCheck check)
        {
            Debug.Log($"[GridManager] Hủy thao tác: {check.Result}");
            GridGeometry geometry = _board.Geometry;
            onInvalidMove?.Invoke(check.Result, geometry.RectCenter(check.Rect), geometry.RectSize(check.Rect));
        }

        private void HandleGameWin()
        {
            if (_isGameWon) return;
            _isGameWon = true;

            // Cả lưới lóe sáng (như bản gốc), khối vừa đặt đang bật nên chờ 1 nhịp
            foreach (PlacedBlockView view in _blockViews.Values)
            {
                if (view != null) view.PlayWinFlash(winFlashDelay);
            }

            Debug.Log("<color=yellow>★ ★ ★ LEVEL COMPLETED! FULL GRID -> YOU WIN! ★ ★ ★</color>");
            onGameWin?.Invoke();
        }

        // ---------- Quản lý view ----------

        /// <summary>
        /// Mèo mờ (Hint) ở vùng <paramref name="rect"/>: lấy 1 view từ pool, dựng như mèo xem trước (màu ngẫu nhiên).
        /// Không phải khối thật (không chiếm ô). Trả lại bằng ReturnGhost.
        /// </summary>
        public PlacedBlockView RentGhost(RectInt rect)
        {
            if (_board == null) return null;
            CellModel clue = null;
            foreach (CellModel cell in _board.GetCellsIn(rect))
            {
                if (cell.HasClue) { clue = cell; break; }
            }
            if (clue == null) return null;
            PlacedBlockView view = _blockPool.Get();
            view.ShowPreview(rect, clue, ColorTypeExtensions.GetRandomColorType(), _board.Geometry);
            return view;
        }

        public void ReturnGhost(PlacedBlockView view)
        {
            if (view != null && view.gameObject.activeSelf) _blockPool.Release(view);
        }

        /// <summary>View của khối đã đặt (cho hiệu ứng: mèo kề bên phản ứng, run khi đóng băng...).</summary>
        public bool TryGetView(BlockModel block, out PlacedBlockView view)
        {
            view = null;
            return block != null && _blockViews.TryGetValue(block, out view) && view != null;
        }

        private void ReleasePreview()
        {
            if (_preview == null) return;
            _blockPool.Release(_preview);
            _preview = null;
        }

        public void ClearAllPlacedBlocks()
        {
            CancelSelection();
            foreach (PlacedBlockView view in _blockViews.Values)
            {
                if (view != null) _blockPool.Release(view);
            }
            _blockViews.Clear();
            if (_board != null) _board.ClearBlocks();
            _isGameWon = false;
        }

        private void SetGrid(List<CellView> grid)
        {
            ClearAllPlacedBlocks();
            ComboTracker.Reset();

            List<CellModel> cells = new List<CellModel>(grid.Count);
            foreach (CellView cell in grid)
            {
                if (cell != null && cell.Model != null) cells.Add(cell.Model);
            }
            _board = new BoardModel(CurrentLevel, gridSpawner.Geometry, cells);
        }
    }
}
