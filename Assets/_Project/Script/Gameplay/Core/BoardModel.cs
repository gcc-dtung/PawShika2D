using System.Collections.Generic;
using UnityEngine;

// [MECHANIC] Luật mới chặn thao tác thì thêm 1 kết quả ở đây
/// <summary>Kết quả kiểm tra 1 vùng chọn (không xét lời giải: vùng hợp luật là đặt được, sai thì chạm khối để gỡ).</summary>
public enum PlacementResult
{
    Ignored,      // tap 1 ô trống không phải số: bỏ qua, không báo gì
    Ready,        // đủ điều kiện đặt -> hiện mèo xem trước; thả tay là đặt
    BlockedCell,  // vùng chứa ô Void hoặc ô đã có khối
    ClueCount,    // không có hoặc có nhiều hơn 1 ô số
    Locked,       // ô số còn khóa
    WrongShape,   // sai hướng ngang / dọc / vuông
    WrongSize     // sai số ô
}

/// <summary>Trạng thái vùng đang kéo, cho phản hồi trong lúc kéo (bong bóng đếm ô, âm thanh).</summary>
public enum SelectionHint
{
    Neutral, // đang kéo, chưa đủ / chưa có ô số
    Ready,   // thả tay là đặt được
    Invalid  // chắc chắn sai: thừa ô, 2 ô số, ô số khóa, sai hướng mà đã đủ ô
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

    /// <summary>
    /// Sai "chắc chắn" mới báo Invalid; còn kéo tiếp có thể thành đúng (thiếu ô, chưa có ô số,
    /// sai hướng khi chưa đủ ô, ô "?") thì Neutral.
    /// </summary>
    public SelectionHint Hint
    {
        get
        {
            switch (Result)
            {
                case PlacementResult.Ready:
                    return SelectionHint.Ready;
                case PlacementResult.BlockedCell:
                case PlacementResult.Locked:
                    return SelectionHint.Invalid;
                case PlacementResult.ClueCount:
                    int clues = 0;
                    if (Cells != null) foreach (CellModel cell in Cells) if (cell.HasClue) clues++;
                    return clues > 1 ? SelectionHint.Invalid : SelectionHint.Neutral;
                case PlacementResult.WrongShape:
                case PlacementResult.WrongSize:
                    ClueCell clue = ClueCell != null ? ClueCell.Clue : null;
                    if (clue == null || clue.IsHidden || Cells == null) return SelectionHint.Neutral;
                    return Cells.Count >= clue.Value ? SelectionHint.Invalid : SelectionHint.Neutral;
                default:
                    return SelectionHint.Neutral;
            }
        }
    }
}

/// <summary>
/// Trạng thái và luật của cả lưới (class C# thuần): các ô, các khối đã đặt / gỡ, kiểm tra vùng chọn, ổ khóa, thắng.
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

        // Tap 1 ô trống không phải số -> bỏ qua. (Chạm khối đã đặt để gỡ: DragAndDropController xử lý trước khi kéo.)
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

        // [MECHANIC] Luật đặt khối của từng cơ chế (thứ tự: chặn cứng trước, kiểm tra số ô cuối cùng)
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

    /// <summary>
    /// Lời giải cho các ô số chưa có khối, khớp với các khối đã đặt (dùng cho Hint / Reveal).
    /// Solver dùng giá trị thật của ô ẩn "?" nên luôn ra 1 lời giải nhất quán.
    /// </summary>
    public bool TrySolveRemaining(out List<RectInt> remaining)
    {
        remaining = null;
        if (Level == null || Level.clues == null) return false;

        List<RectInt> rects = new List<RectInt>(_blocks.Count);
        foreach (BlockModel b in _blocks) rects.Add(b.Rect);
        return ShikakuSolver.TrySolve(Level, rects, out remaining) && remaining != null;
    }

    /// <summary>
    /// Lời giải gợi ý cho Hint / Reveal / Cheat: ưu tiên lời giải khớp các khối đã đặt; khối đặt sai làm lưới hết lời giải
    /// thì lấy lời giải từ lưới trống (bỏ qua mọi khối đã đặt) và chỉ trả về các vùng chưa có khối đúng y hệt.
    /// Đặt vùng trả về bằng GridManager.PlaceSolved (tự gỡ khối sai đang đè lên).
    /// </summary>
    public bool TrySolveHint(out List<RectInt> missing)
    {
        if (TrySolveRemaining(out missing)) return true;

        missing = null;
        if (Level == null || Level.clues == null) return false;
        if (!ShikakuSolver.TrySolve(Level, new List<RectInt>(), out List<RectInt> full) || full == null) return false;

        missing = new List<RectInt>(full.Count);
        foreach (RectInt rect in full)
        {
            if (!_blocks.Exists(b => b.Rect.Equals(rect))) missing.Add(rect);
        }
        return true;
    }

    /// <summary>Khối đang chiếm ô <paramref name="pos"/>, null nếu ô trống / không có ô.</summary>
    public BlockModel GetBlockAt(Vector2Int pos)
    {
        CellModel cell = GetCell(pos);
        return cell != null ? cell.Block : null;
    }

    /// <summary>Các khối đã đặt có chung ô với vùng <paramref name="rect"/>.</summary>
    public List<BlockModel> GetBlocksOverlapping(RectInt rect)
    {
        List<BlockModel> res = new List<BlockModel>();
        foreach (BlockModel b in _blocks)
        {
            if (b.Rect.Overlaps(rect)) res.Add(b);
        }
        return res;
    }

    /// <summary>Gỡ 1 khối (chạm khối / booster đặt đè). Ổ khóa tính lại: bớt 1 khối thì khóa lại 1 bước.</summary>
    public bool Remove(BlockModel block)
    {
        if (block == null || !_blocks.Remove(block)) return false;
        block.Release();
        SyncLocks(null);
        return true;
    }

    /// <summary>
    /// Vùng có đặt được không nếu gỡ <paramref name="replaced"/> trước (booster đặt đè khối sai).
    /// Gỡ khối làm ổ khóa đóng lại nên phải thử gỡ thật rồi trả lưới về như cũ.
    /// </summary>
    public bool CanPlaceReplacing(RectInt rect, List<BlockModel> replaced)
    {
        foreach (BlockModel b in replaced) { _blocks.Remove(b); b.Release(); }
        SyncLocks(null);
        bool ready = Check(rect).IsReady;
        foreach (BlockModel b in replaced) { _blocks.Add(b); b.Occupy(); }
        SyncLocks(null);
        return ready;
    }

    /// <summary>
    /// Đặt khối từ 1 vùng đã Ready, tô màu <paramref name="color"/>. Các ô vừa mở khóa được thêm vào <paramref name="unlocked"/>.
    /// Ô "?" không bao giờ lộ số: Check không xét số ô của "?" nên khối đặt lên có thể sai số, lộ số thật là lộ đáp án.
    /// Số thật vẫn dùng để chấm (IsSolved).
    /// </summary>
    public BlockModel Place(PlacementCheck check, ColorType color, List<CellModel> unlocked)
    {
        BlockModel block = new BlockModel(check.Rect, check.Cells, check.ClueCell, color);
        block.Occupy();
        _blocks.Add(block);

        // [MECHANIC] Cơ chế phản ứng khi 1 khối được đặt (đếm bước...)
        SyncLocks(unlocked);
        return block;
    }

    /// <summary>
    /// Ổ khóa còn lại = số bước gốc - số khối khác đang trên lưới (khối nằm trên chính ô đó không tính).
    /// Đặt khối thì mọi ổ khóa giảm 1, gỡ khối thì khóa lại 1. Ô vừa mở khóa được thêm vào <paramref name="unlocked"/>.
    /// </summary>
    private void SyncLocks(List<CellModel> unlocked)
    {
        foreach (CellModel cell in _cells.Values)
        {
            int others = _blocks.Count - (cell.IsOccupied && cell.Block.ClueCell == cell ? 1 : 0);
            if (cell.SyncLock(others)) unlocked?.Add(cell);
        }
    }

    /// <summary>
    /// Lưới kín và mọi khối đúng số ô. Vùng hợp luật luôn đúng số ô, trừ ô "?" (Check bỏ qua số ô vì người chơi không biết số):
    /// khối "?" sai số ô được thêm vào <paramref name="wrong"/>.
    /// </summary>
    public bool IsSolved(List<BlockModel> wrong)
    {
        if (!IsFull()) return false;
        bool ok = true;
        foreach (BlockModel b in _blocks)
        {
            if (b.Area == b.ClueCell.Clue.Value) continue;
            ok = false;
            wrong?.Add(b);
        }
        return ok;
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
        SyncLocks(null);
    }
}
