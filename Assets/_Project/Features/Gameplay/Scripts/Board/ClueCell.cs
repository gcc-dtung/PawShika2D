namespace PawBlock.Features.Gameplay
{
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

        // [MECHANIC] Trạng thái lúc chơi của cơ chế (đọc từ data.mechanic) + hàm đổi trạng thái (như SyncLock). Ô "?" giữ ẩn suốt ván.
        public ClueCell(ClueData data)
        {
            Data = data;
            LockSteps = data.mechanic.lockSteps;
            IsHidden = data.mechanic.hidden;
        }

        /// <summary>
        /// Khóa còn lại = số bước gốc - số khối khác đang trên lưới: đặt khối thì mở dần, gỡ khối thì khóa lại.
        /// Trả về true nếu số bước khóa thay đổi.
        /// </summary>
        public bool SyncLock(int otherBlocks)
        {
            int steps = System.Math.Max(0, Data.mechanic.lockSteps - otherBlocks);
            if (steps == LockSteps) return false;
            LockSteps = steps;
            return true;
        }
    }
}
