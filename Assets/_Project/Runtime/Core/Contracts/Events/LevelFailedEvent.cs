namespace PawBlock.Core.Contracts
{
    /// <summary>Thua hẳn 1 level: hết giờ rồi từ chối thêm giờ (Gameplay – LevelManager, cùng lúc state Lost).</summary>
    public readonly struct LevelFailedEvent : IGameEvent
    {
        public readonly int Number;     // số level thường hiện tại (bắt đầu từ 1)
        public readonly string LevelId; // tên asset level
        public readonly bool IsDaily;

        public LevelFailedEvent(int number, string levelId, bool isDaily)
        {
            Number = number;
            LevelId = levelId;
            IsDaily = isDaily;
        }
    }
}
