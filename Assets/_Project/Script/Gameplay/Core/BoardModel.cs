using System.Collections.Generic;
using UnityEngine;

/// <summary>Kết quả kiểm tra 1 vùng chọn (chưa xét lời giải).</summary>
public enum PlacementResult
{
    Ignored,      // tap 1 ô trống không phải số: bỏ qua, không báo gì
    Ready,        // đủ điều kiện đặt -> hiện mèo xem trước; thả tay mới kiểm tra lời giải
    BlockedCell,  // vùng chứa ô Void hoặc ô đã có khối
    ClueCount,    // không có hoặc có nhiều hơn 1 ô số
    Locked,       // ô số còn khóa
    WrongShape,   // sai hướng ngang / dọc / vuông
    WrongSize     // sai số ô
}

public readonly struct PlacementCheck
{
    public readonly PlacementResult Result;
    public readonly RectInt Rect;
    public readonly List<CellModel> Cells;
    public readonly CellModel ClueCell;

    public PlacementCheck(PlacementResult result, RectInt rect, List<CellModel> cells, CellModel clueCell)
    {
        Result = result;
        Rect = rect;
        Cells = cells;
        ClueCell = clueCell;
    }

    public bool IsReady => Result == PlacementResult.Ready;
}

/// <summary>
/// Trạng thái và luật của cả lưới (class C# thuần): các ô, các khối đã đặt, kiểm tra vùng chọn, ổ khóa, thắng.
/// GridManager giữ instance này và nối nó với input / view.
/// </summary>
public class BoardModel
{
    private readonly Dictionary<Vector2Int, CellModel> _cells = new Dictionary<Vector2Int, CellModel>();
    private readonly List<BlockModel> _blocks = new List<BlockModel>();

    public LevelDefinition Level { get; }
    public GridGeometry Geometry { get; }
    public IReadOnlyList<BlockModel> Blocks => _blocks;
    public IEnumerable<CellModel> Cells => _cells.Values;

    public BoardModel(LevelDefinition level, GridGeometry geometry, IEnumerable<CellModel> cells)
    {
        Level = level;
        Geometry = geometry;
        foreach (CellModel cell in cells) _cells[cell.Position] = cell;
    }

    public static RectInt RectFrom(Vector2Int a, Vector2Int b)
    {
        int xMin = Mathf.Min(a.x, b.x);
        int yMin = Mathf.Min(a.y, b.y);
        return new RectInt(xMin, yMin, Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);
    }

    /// <summary>Các ô thực sự có trong vùng (bỏ ô Void).</summary>
    public List<CellModel> GetCellsIn(RectInt rect)
    {
        List<CellModel> res = new List<CellModel>(rect.width * rect.height);
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                if (_cells.TryGetValue(new Vector2Int(x, y), out CellModel cell)) res.Add(cell);
            }
        }
        return res;
    }

    public CellModel GetCell(Vector2Int pos)
    {
        return _cells.TryGetValue(pos, out CellModel cell) ? cell : null;
    }

    /// <summary>Vùng không chứa ô Void hay ô đã có khối (khung chọn được phép phủ lên).</summary>
    public bool IsAreaFree(RectInt rect)
    {
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, y), out CellModel cell) || cell.IsOccupied) return false;
            }
        }
        return true;
    }

    public PlacementCheck Check(RectInt rect)
    {
        List<CellModel> cells = GetCellsIn(rect);
        int area = rect.width * rect.height;

        // Tap 1 ô trống không phải số -> bỏ qua. (Không có xóa khối: mọi khối đã đặt luôn đúng.)
        if (area == 1 && (cells.Count == 0 || !cells[0].HasClue))
            return new PlacementCheck(PlacementResult.Ignored, rect, cells, null);

        // 1. Vùng chứa ô Void hoặc ô đã có khối
        if (cells.Count != area || cells.Exists(c => c.IsOccupied))
            return new PlacementCheck(PlacementResult.BlockedCell, rect, cells, null);

        // 2. Phải có ĐÚNG 1 ô số
        CellModel clueCell = null;
        foreach (CellModel cell in cells)
        {
            if (!cell.HasClue) continue;
            if (clueCell != null) return new PlacementCheck(PlacementResult.ClueCount, rect, cells, null);
            clueCell = cell;
        }
        if (clueCell == null) return new PlacementCheck(PlacementResult.ClueCount, rect, cells, null);

        ClueCell clue = clueCell.Clue;

        // 3. Ô số còn khóa
        if (clue.IsLocked) return new PlacementCheck(PlacementResult.Locked, rect, cells, clueCell);

        // 4. Hướng ngang / dọc / vuông
        if (!ShikakuSolver.MatchesConstraint(clue.Constraint, rect.width, rect.height))
            return new PlacementCheck(PlacementResult.WrongShape, rect, cells, clueCell);

        // 5. Số ô (ô ẩn "?" thì người chơi không biết số -> để lời giải quyết định)
        if (!clue.IsHidden && clue.Value != cells.Count)
            return new PlacementCheck(PlacementResult.WrongSize, rect, cells, clueCell);

        return new PlacementCheck(PlacementResult.Ready, rect, cells, clueCell);
    }

    /// <summary>Đặt thêm vùng này mà lưới vẫn còn lời giải thì là nước đi đúng.</summary>
    public bool IsSolvableWith(RectInt rect)
    {
        if (Level == null || Level.clues == null || Level.clues.Length == 0) return true;

        List<RectInt> rects = new List<RectInt>(_blocks.Count + 1);
        foreach (BlockModel b in _blocks) rects.Add(b.Rect);
        rects.Add(rect);
        return ShikakuSolver.IsSolvable(Level, rects);
    }

    /// <summary>Đặt khối từ 1 vùng đã Ready, tô màu <paramref name="color"/>. Các ô vừa mở khóa được thêm vào <paramref name="unlocked"/>.</summary>
    public BlockModel Place(PlacementCheck check, ColorType color, List<CellModel> unlocked)
    {
        check.ClueCell.RevealClue();

        BlockModel block = new BlockModel(check.Rect, check.Cells, check.ClueCell, color);
        block.Occupy();
        _blocks.Add(block);

        // Mỗi khối đặt đúng làm mọi ổ khóa giảm 1
        foreach (CellModel cell in _cells.Values)
        {
            if (cell.StepLock()) unlocked?.Add(cell);
        }
        return block;
    }

    public bool IsFull()
    {
        if (_cells.Count == 0) return false;
        foreach (CellModel cell in _cells.Values)
        {
            if (!cell.IsOccupied) return false;
        }
        return true;
    }

    public void ClearBlocks()
    {
        foreach (BlockModel b in _blocks) b.Release();
        _blocks.Clear();
    }
}
