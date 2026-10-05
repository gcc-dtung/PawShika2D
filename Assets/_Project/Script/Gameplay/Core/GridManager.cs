using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

/// <summary>
/// Nối input kéo thả với luật chơi (BoardModel) và các view (SelectionBoxView, PlacedBlockView).
/// Luật nằm ở BoardModel; script này chỉ điều phối và phát event.
/// </summary>
public class GridManager : Singleton<GridManager>
{
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private SelectionBoxView selectionBox;

    [Header("Khối mèo (lấy từ pool, không tạo mới mỗi lần đặt)")]
    [SerializeField] private PlacedBlockView blockPrefab;
    [SerializeField] private Transform blockRoot;
    [SerializeField] private int blockPoolCapacity = 64;

    [Header("Thắng")]
    [SerializeField] private float winFlashDelay = 0.1f;

    [Header("Events")]
    public UnityEvent onGameWin = new UnityEvent();
    public UnityEvent<BlockModel> onBlockPlaced = new UnityEvent<BlockModel>();
    public UnityEvent<Vector3, Vector2> onMistake = new UnityEvent<Vector3, Vector2>();     // (tâm, kích thước) vùng sai
    public UnityEvent<PlacementResult, Vector3, Vector2> onInvalidMove = new UnityEvent<PlacementResult, Vector3, Vector2>(); // thao tác bị hủy, không trừ mạng
    public UnityEvent<CellModel> onClueUnlocked = new UnityEvent<CellModel>();

    private BoardModel _board;
    private readonly Dictionary<BlockModel, PlacedBlockView> _blockViews = new Dictionary<BlockModel, PlacedBlockView>();
    private readonly List<CellModel> _unlockedBuffer = new List<CellModel>();
    private ObjectPool<PlacedBlockView> _blockPool;
    private PlacedBlockView _preview; // mèo xem trước của lượt kéo hiện tại
    private ColorType _dragColor;     // màu khung chọn của lượt kéo -> màu khối mèo
    private readonly DragSelection _drag = new DragSelection();
    private bool _isGameWon = false;
    private bool _finishing; // đã thả tay, đang chờ khung giãn xong rồi mới đặt / báo sai

    public BoardModel Board => _board;
    public bool IsGameWon => _isGameWon;
    public bool IsGameOver => LivesManager.Instance != null && LivesManager.Instance.IsOutOfLives;
    public bool IsInputLocked => _isGameWon || IsGameOver || _finishing
        || (LevelManager.Instance != null && !LevelManager.Instance.IsPlaying)
        || (BoosterManager.Instance != null && BoosterManager.Instance.IsBusy) // Reveal đang tự đặt khối
        || (gridSpawner != null && gridSpawner.IsIntroPlaying)                  // lưới đang xuất hiện
        || DifficultyIntro.Playing                                               // băng "HARD / EXTREME LEVEL" đang chạy
        || BoosterTutorial.Active;                                               // tutorial mở khóa booster: chỉ bấm được nút booster
    public LevelDefinition CurrentLevel => gridSpawner != null ? gridSpawner.LevelData : null;

    protected override void Awake()
    {
        base.Awake();
        if (selectionBox == null) Debug.LogError("[GridManager] Chưa gán SelectionBoxView");
        if (blockPrefab == null) Debug.LogError("[GridManager] Chưa gán PlacedBlock prefab");
        if (blockRoot == null) blockRoot = transform;

        _blockPool = new ObjectPool<PlacedBlockView>(
            createFunc: () => Instantiate(blockPrefab, blockRoot),
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
        RefreshSelection();
        return true;
    }

    /// <summary>Ngón tay ở điểm world này. Vùng chọn giãn theo nhưng không phủ lên khối đã đặt / ô Void.</summary>
    public void UpdateSelection(Vector3 fingerWorld)
    {
        if (!_drag.IsActive || _board == null) return;

        GridGeometry geometry = _board.Geometry;
        if (_drag.MoveTo(geometry.WorldToCell(fingerWorld), _board)) RefreshSelection();

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
    /// Thả tay: đặt khối nếu đúng, mèo rung + trừ mạng nếu sai lời giải, hủy nếu chưa đủ điều kiện.
    /// Đủ điều kiện mà khung chưa giãn xong thì chờ khung giãn hết, hiện mèo, rồi mới đặt / báo sai.
    /// </summary>
    public void EndSelection()
    {
        if (!_drag.IsActive) return;
        RectInt rect = _drag.Rect;
        _drag.End();

        // Khi đã win hoặc hết mạng -> Khóa toàn bộ thao tác
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
            HideSelectionBox();
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

    /// <summary>Vùng đủ điều kiện sau khi thả tay: hiện mèo (nếu chưa) rồi kiểm tra lời giải.</summary>
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

        // Đặt xong mà lưới không còn lời giải -> NƯỚC ĐI SAI, trừ mạng
        if (!_board.IsSolvableWith(rect))
        {
            view.PlayReject(() => _blockPool.Release(view));
            DeductLife(rect);
            return;
        }

        PlaceBlock(check, view);
    }

    /// <summary>Hủy lượt kéo (bị khóa input, đổi level...).</summary>
    public void CancelSelection()
    {
        _finishing = false; // Hide() bỏ luôn callback chờ khung giãn xong
        _drag.End();
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

        foreach (CellModel cell in _unlockedBuffer) onClueUnlocked?.Invoke(cell);
        onBlockPlaced?.Invoke(block);

        // Kiểm tra sau khi đặt khối xem đã phủ kín toàn bộ grid chưa
        if (_board.IsFull()) HandleGameWin();
    }

    /// <summary>
    /// Booster (Hint / Reveal) đặt 1 khối của lời giải: không qua kéo thả, không kiểm tra lời giải lại.
    /// Màu random như khung chọn. Trả về false nếu vùng không đặt được (đã có khối, ô còn khóa...).
    /// </summary>
    public bool PlaceSolved(RectInt rect)
    {
        if (_board == null || _isGameWon || IsGameOver) return false;
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

    private void DeductLife(RectInt rect)
    {
        Debug.LogWarning("<color=red>[GridManager] Nước đi SAI! Đặt khối này thì lưới không còn lời giải -> TRỪ 1 MẠNG</color>");
        GridGeometry geometry = _board.Geometry;
        onMistake?.Invoke(geometry.RectCenter(rect), geometry.RectSize(rect));

        if (LivesManager.Instance != null && !TutorialController.Active) // tutorial: chỉ báo sai, không trừ mạng
        {
            LivesManager.Instance.LoseLife();
        }
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

        List<CellModel> cells = new List<CellModel>(grid.Count);
        foreach (CellView cell in grid)
        {
            if (cell != null && cell.Model != null) cells.Add(cell.Model);
        }
        _board = new BoardModel(CurrentLevel, gridSpawner.Geometry, cells);
    }
}
