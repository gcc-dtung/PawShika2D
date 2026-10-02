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

    public BoardModel Board => _board;
    public bool IsGameWon => _isGameWon;
    public bool IsGameOver => LivesManager.Instance != null && LivesManager.Instance.IsOutOfLives;
    public bool IsInputLocked => _isGameWon || IsGameOver || (LevelManager.Instance != null && !LevelManager.Instance.IsPlaying);
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

    /// <summary>Số ô của vùng đang kéo, dùng cho bong bóng đếm ô.</summary>
    public int SelectionCellCount => _drag.IsActive ? _drag.CellCount : 0;

    /// <summary>Khung chọn bám vùng; vùng đủ điều kiện thì hiện mèo xem trước.</summary>
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
        if (check.IsReady)
        {
            if (_preview == null) _preview = _blockPool.Get();
            if (!_preview.IsPreview || !_preview.Rect.Equals(rect)) _preview.ShowPreview(rect, check.ClueCell, _dragColor, geometry);
        }
        else
        {
            ReleasePreview();
        }

        if (selectionBox != null) selectionBox.SetFilled(!check.IsReady);
    }

    /// <summary>Thả tay: đặt khối nếu đúng, mèo rung đỏ + trừ mạng nếu sai lời giải, hủy nếu chưa đủ điều kiện.</summary>
    public void EndSelection()
    {
        if (!_drag.IsActive) return;
        RectInt rect = _drag.Rect;
        _drag.End();
        if (selectionBox != null) selectionBox.Hide();

        // Khi đã win hoặc hết mạng -> Khóa toàn bộ thao tác
        if (IsInputLocked || _board == null)
        {
            ReleasePreview();
            return;
        }

        PlacementCheck check = _board.Check(rect);

        if (check.Result == PlacementResult.Ignored)
        {
            ReleasePreview();
            return;
        }

        if (!check.IsReady)
        {
            ReleasePreview();
            Reject(check);
            return;
        }

        // Thả tay mà chưa kịp hiện mèo xem trước -> tạo luôn
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

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.LoseLife();
        }
    }

    private void HandleGameWin()
    {
        if (_isGameWon) return;
        _isGameWon = true;

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
