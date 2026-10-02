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

    /// <summary>Giảm 1 bước khóa. Trả về true nếu ô vừa được mở khóa.</summary>
    public bool StepLock()
    {
        if (Clue == null || !Clue.IsLocked) return false;
        bool unlocked = Clue.StepLock();
        Changed?.Invoke();
        return unlocked;
    }

    public void RevealClue()
    {
        if (Clue != null && Clue.Reveal()) Changed?.Invoke();
    }

    public void SetBlock(BlockModel block)
    {
        Block = block;
    }
}
