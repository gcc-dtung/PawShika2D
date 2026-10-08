/// <summary>
/// Dữ liệu và luật của 1 ô số (class C# thuần, không phải MonoBehaviour).
/// CellModel sở hữu instance này; CellView lo phần hiển thị.
/// </summary>
public class ClueCell
{
    public ClueData Data { get; }
    public int LockSteps { get; private set; }
    public bool IsHidden { get; private set; }

    public int Value => Data.value;
    public int ColorIndex => Data.color;
    public ClueConstraint Constraint => Data.mechanic.constraint;
    public bool IsLocked => LockSteps > 0;

    // [MECHANIC] Trạng thái lúc chơi của cơ chế (đọc từ data.mechanic) + hàm đổi trạng thái (như StepLock / Reveal)
    public ClueCell(ClueData data)
    {
        Data = data;
        LockSteps = data.mechanic.lockSteps;
        IsHidden = data.mechanic.hidden;
    }

    /// <summary>Gọi mỗi khi người chơi đặt đúng 1 khối. Trả về true nếu ô vừa được mở khóa.</summary>
    public bool StepLock()
    {
        if (!IsLocked) return false;
        LockSteps--;
        return LockSteps == 0;
    }

    /// <summary>Khối chứa ô ẩn đã đặt đúng -> lộ số. Trả về true nếu trạng thái thay đổi.</summary>
    public bool Reveal()
    {
        if (!IsHidden) return false;
        IsHidden = false;
        return true;
    }
}
