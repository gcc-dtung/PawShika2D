namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Thắng 1 level thường hoặc Daily (Gameplay – LevelManager, cùng lúc state Won). Không phát cho màn tutorial.
    /// </summary>
    public readonly struct LevelCompletedEvent : IGameEvent
    {
        public readonly int Number;     // số level thường hiện tại (bắt đầu từ 1); Daily vẫn là level thường chưa thắng
        public readonly string LevelId; // tên asset level
        public readonly bool IsDaily;
        public readonly long DailyDay;  // ngày của level Daily (số ngày từ 1/1/1970), -1 nếu không phải Daily
        public readonly bool IsHard;    // độ khó khác Normal (Hard / Very Hard)

        public LevelCompletedEvent(int number, string levelId, bool isDaily, long dailyDay, bool isHard)
        {
            Number = number;
            LevelId = levelId;
            IsDaily = isDaily;
            DailyDay = dailyDay;
            IsHard = isHard;
        }
    }
}
