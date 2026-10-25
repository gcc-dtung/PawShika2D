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
        public ClueCorner Corner => Data.mechanic.corner;
        public bool IsLocked => LockSteps > 0;
        public ClueAmbiguity Ambiguity => Data.mechanic.ambiguity;
        public bool IsAmbiguous => Ambiguity != ClueAmbiguity.None;

        /// <summary>Chữ hiện trên ô số / khối: "?" cho ô ẩn, ">5" / "E" / "3/5"... cho số mơ hồ, còn lại là số ô.</summary>
        public string Label { get; }
        /// <summary>Khoảng số ô thỏa ô số (số thường: Min = Max = Value). Max = int.MaxValue: không có trần.</summary>
        public int MinArea => AmbiguityRules.MinArea(Data.mechanic, Data.value);
        public int MaxArea => AmbiguityRules.MaxArea(Data.mechanic, Data.value);
        public bool AcceptsArea(int area) => AmbiguityRules.Accepts(Data.mechanic, Data.value, area);

        // [MECHANIC] Trạng thái lúc chơi của cơ chế (đọc từ data.mechanic) + hàm đổi trạng thái (như SyncLock). Ô "?" giữ ẩn suốt ván.
        public ClueCell(ClueData data)
        {
            Data = data;
            LockSteps = data.mechanic.lockSteps;
            IsHidden = data.mechanic.hidden;
            Label = IsHidden ? "?" : AmbiguityRules.Label(data.mechanic, data.value);
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
