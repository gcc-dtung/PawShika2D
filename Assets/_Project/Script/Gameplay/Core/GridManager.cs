using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class GridManager : Singleton<GridManager>
{
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private SelectionBox selectionBox;
    [SerializeField] private float cellSize = GridSpawner.CellSpacing;

    [Header("Khối mèo (lấy từ pool, không tạo mới mỗi lần đặt)")]
    [SerializeField] private PlacedBlock blockPrefab;
    [SerializeField] private Transform blockRoot;
    [SerializeField] private int blockPoolCapacity = 64;

    [Header("Events")]
    public UnityEvent onGameWin = new UnityEvent();
    public UnityEvent<PlacedBlock> onBlockPlaced = new UnityEvent<PlacedBlock>();
    public UnityEvent<Vector3, Vector2> onMistake = new UnityEvent<Vector3, Vector2>();     // (tâm, kích thước) vùng sai
    public UnityEvent<Vector3, Vector2> onInvalidMove = new UnityEvent<Vector3, Vector2>(); // thao tác bị hủy, không trừ mạng
    public UnityEvent<Cell> onClueUnlocked = new UnityEvent<Cell>();

    private List<Cell> _currentGrid;
    private readonly Dictionary<Vector2Int, Cell> _cellMap = new Dictionary<Vector2Int, Cell>();
    private static readonly int ColorTypeCount = Enum.GetValues(typeof(ColorType)).Length;

    private List<PlacedBlock> _placedBlocks = new List<PlacedBlock>();
    private ObjectPool<PlacedBlock> _blockPool;
    private bool _isGameWon = false;

    public IReadOnlyList<PlacedBlock> PlacedBlocks => _placedBlocks;
    public bool IsGameWon => _isGameWon;
    public bool IsGameOver => LivesManager.Instance != null && LivesManager.Instance.IsOutOfLives;
    public bool IsInputLocked => _isGameWon || IsGameOver;
    public LevelDefinition CurrentLevel => gridSpawner != null ? gridSpawner.LevelData : null;

    protected override void Awake()
    {
        base.Awake();
        if (selectionBox == null) Debug.LogError("[GridManager] Chưa gán SelectionBox");
        if (blockPrefab == null) Debug.LogError("[GridManager] Chưa gán PlacedBlock prefab");
        if (blockRoot == null) blockRoot = transform;

        _blockPool = new ObjectPool<PlacedBlock>(
            createFunc: () => Instantiate(blockPrefab, blockRoot),
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b =>
            {
                b.ReleaseCells();
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

    public void OnDrawGrid(Cell prev, Cell curr)
    {
        if (IsInputLocked) return;

        // Hiển thị khung SelectionBox đè lên trên toàn bộ vùng đang kéo (không đổi màu cell bên dưới)
        if (selectionBox != null)
        {
            selectionBox.Show(prev, curr, isFinalSelect: false);
        }
    }

    /// <summary>Số ô thực sự nằm trong vùng kéo (bỏ ô Void), dùng cho bong bóng đếm ô.</summary>
    public int CountCellsInRange(Cell a, Cell b)
    {
        return GetGridInRange(a, b).Count;
    }

    public void OnSelect(Cell prev, Cell curr)
    {
        // Luôn tắt SelectionBox khi thả tay
        HideSelectionBox();

        // Khi đã win hoặc hết mạng -> Khóa toàn bộ thao tác
        if (IsInputLocked) return;
        if (prev == null || curr == null) return;

        RectInt rect = GetRect(prev, curr);
        List<Cell> cellsInRange = GetGridInRange(prev, curr);

        // TAP 1 ô trống không phải số -> bỏ qua. (Không có xóa khối: mọi khối đã đặt luôn đúng.)
        if (rect.width * rect.height == 1 && !prev.HasClue) return;

        // 1. Vùng chứa ô Void hoặc ô đã có khối -> hủy
        if (cellsInRange.Count != rect.width * rect.height || cellsInRange.Exists(c => c.IsOccupied))
        {
            Reject(rect, "Vùng chứa ô trống hoặc khối đã đặt");
            return;
        }

        // 2. Phải có ĐÚNG 1 ô số
        List<Cell> clueCells = cellsInRange.FindAll(c => c.HasClue);
        if (clueCells.Count != 1)
        {
            Reject(rect, $"Vùng có {clueCells.Count} ô số");
            return;
        }

        Cell clueCell = clueCells[0];
        ClueCell clue = clueCell.Clue;

        // 3. Ô số còn khóa -> chưa chọn được
        if (clue.IsLocked)
        {
            Reject(rect, "Ô số đang bị khóa");
            return;
        }

        // 4. Mũi tên ngang / dọc
        if (!ShikakuSolver.MatchesConstraint(clue.Constraint, rect.width, rect.height))
        {
            Reject(rect, "Sai hướng mũi tên");
            return;
        }

        // 5. Sai số ô (ô ẩn "?" thì người chơi không biết số -> để solver quyết định)
        if (!clue.IsHidden && clue.Value != cellsInRange.Count)
        {
            Reject(rect, $"Cần {clue.Value} ô, đang chọn {cellsInRange.Count}");
            return;
        }

        // 6. Đặt xong mà lưới không còn lời giải -> NƯỚC ĐI SAI, trừ mạng
        if (!IsPlacementSolvable(rect))
        {
            DeductLife(rect);
            return;
        }

        // Debug.Log($"<color=green>[GridManager] Đặt hộp hợp lệ</color> ({rect.x}, {rect.y}) {rect.width}x{rect.height}");
        CreatePlacedBlock(rect, cellsInRange, clueCell);
    }

    private bool IsPlacementSolvable(RectInt rect)
    {
        LevelDefinition level = CurrentLevel;
        if (level == null || level.clues == null || level.clues.Length == 0) return true;

        List<RectInt> rects = new List<RectInt>(_placedBlocks.Count + 1);
        foreach (PlacedBlock b in _placedBlocks)
        {
            if (b != null) rects.Add(b.Rect);
        }
        rects.Add(rect);
        return ShikakuSolver.IsSolvable(level, rects);
    }

    private void Reject(RectInt rect, string reason)
    {
        Debug.Log($"[GridManager] Hủy thao tác: {reason}");
        GetRectWorld(rect, out Vector3 center, out Vector2 size);
        onInvalidMove?.Invoke(center, size);
    }

    private void DeductLife(RectInt rect)
    {
        Debug.LogWarning("<color=red>[GridManager] Nước đi SAI! Đặt khối này thì lưới không còn lời giải -> TRỪ 1 MẠNG</color>");
        GetRectWorld(rect, out Vector3 center, out Vector2 size);
        onMistake?.Invoke(center, size);

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.LoseLife();
        }
    }

    public bool CheckWinCondition()
    {
        if (_currentGrid == null || _currentGrid.Count == 0) return false;

        foreach (Cell cell in _currentGrid)
        {
            if (cell != null && !cell.IsOccupied)
            {
                return false;
            }
        }

        return true;
    }

    private void HandleGameWin()
    {
        if (_isGameWon) return;
        _isGameWon = true;

        Debug.Log("<color=yellow>★ ★ ★ LEVEL COMPLETED! FULL GRID -> YOU WIN! ★ ★ ★</color>");
        onGameWin?.Invoke();
    }

    private void CreatePlacedBlock(RectInt rect, List<Cell> cells, Cell clueCell)
    {
        clueCell.RevealClue();

        PlacedBlock newBlock = _blockPool.Get();

        ColorType color = (ColorType)(Mathf.Abs(clueCell.Clue.ColorIndex) % ColorTypeCount);
        newBlock.Init(rect, cells, clueCell, color, cellSize);
        _placedBlocks.Add(newBlock);

        StepLocks();
        onBlockPlaced?.Invoke(newBlock);

        // Kiểm tra sau khi đặt khối xem đã phủ kín toàn bộ grid chưa
        if (CheckWinCondition())
        {
            HandleGameWin();
        }
    }

    /// <summary>Mỗi khối đặt đúng làm mọi ổ khóa giảm 1.</summary>
    private void StepLocks()
    {
        if (_currentGrid == null) return;
        foreach (Cell cell in _currentGrid)
        {
            if (cell != null && cell.StepLock())
            {
                onClueUnlocked?.Invoke(cell);
            }
        }
    }

    public void ClearAllPlacedBlocks()
    {
        for (int i = _placedBlocks.Count - 1; i >= 0; i--)
        {
            if (_placedBlocks[i] != null)
            {
                _blockPool.Release(_placedBlocks[i]);
            }
        }
        _placedBlocks.Clear();
        _isGameWon = false;
    }

    public void HideSelectionBox()
    {
        if (selectionBox != null)
        {
            selectionBox.Hide();
        }
    }

    public List<Cell> GetGridInRange(Cell a, Cell b)
    {
        List<Cell> res = new List<Cell>();
        if (a == null || b == null || _currentGrid == null) return res;

        RectInt rect = GetRect(a, b);
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                if (_cellMap.TryGetValue(new Vector2Int(x, y), out Cell cell) && cell != null)
                {
                    res.Add(cell);
                }
            }
        }

        return res;
    }

    private static RectInt GetRect(Cell a, Cell b)
    {
        Vector2Int pa = a.GetPositionInCell();
        Vector2Int pb = b.GetPositionInCell();
        int xMin = Mathf.Min(pa.x, pb.x);
        int yMin = Mathf.Min(pa.y, pb.y);
        return new RectInt(xMin, yMin, Mathf.Abs(pa.x - pb.x) + 1, Mathf.Abs(pa.y - pb.y) + 1);
    }

    /// <summary>Tâm và kích thước (world) của vùng, kể cả khi vùng chứa ô Void.</summary>
    private void GetRectWorld(RectInt rect, out Vector3 center, out Vector2 size)
    {
        LevelDefinition level = CurrentLevel;
        int width = level != null ? level.size : 0;
        int height = level != null ? level.Height : 0;
        float cx = (rect.x + (rect.width - 1) / 2f - (width - 1) / 2f) * cellSize;
        float cy = (rect.y + (rect.height - 1) / 2f - (height - 1) / 2f) * cellSize;
        // GridSpawner đặt cell theo tọa độ world quanh gốc (0, 0)
        center = new Vector3(cx, cy, -0.2f);
        size = new Vector2(rect.width * cellSize, rect.height * cellSize);
    }

    private void SetGrid(List<Cell> grid)
    {
        _isGameWon = false;
        ClearAllPlacedBlocks();
        _currentGrid = grid;

        _cellMap.Clear();
        foreach (Cell cell in grid)
        {
            if (cell != null) _cellMap[cell.GetPositionInCell()] = cell;
        }
    }
}
