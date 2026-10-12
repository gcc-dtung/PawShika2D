using System;
using UnityEngine;

/// <summary>
/// Dữ liệu của 1 ô trên lưới (class C# thuần): vị trí, ô số (nếu có) và khối đang chiếm ô.
/// CellView lắng nghe Changed để vẽ lại.
/// </summary>
public class CellModel
{
    public Vector2Int Position { get; }
    public ClueCell Clue { get; private set; }
    public BlockModel Block { get; private set; }

    public bool HasClue => Clue != null;
    public bool IsOccupied => Block != null;

    /// <summary>Trạng thái ô số đổi (khóa, ẩn) -> view cần vẽ lại.</summary>
    public event Action Changed;

    public CellModel(Vector2Int position)
    {
        Position = position;
    }

    public CellModel(Vector2Int position, ClueData clue) : this(position)
    {
        Clue = new ClueCell(clue);
    }

    /// <summary>Tính lại khóa theo số khối khác đang trên lưới. Trả về true nếu ô vừa được mở khóa.</summary>
    public bool SyncLock(int otherBlocks)
    {
        if (Clue == null) return false;
        bool wasLocked = Clue.IsLocked;
        if (!Clue.SyncLock(otherBlocks)) return false;
        Changed?.Invoke();
        return wasLocked && !Clue.IsLocked;
    }

    public void SetBlock(BlockModel block)
    {
        Block = block;
    }
}
