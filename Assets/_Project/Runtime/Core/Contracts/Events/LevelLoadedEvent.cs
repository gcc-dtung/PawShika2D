namespace PawBlock.Core.Contracts
{
    /// <summary>LevelManager vừa nạp 1 level thường hoặc level Daily Challenge (không phát cho màn tutorial).</summary>
    public readonly struct LevelLoadedEvent : IGameEvent
    {
        public readonly int Number;           // số level hiển thị (bắt đầu từ 1); Daily vẫn là level thường hiện tại
        public readonly string LevelId;       // tên asset level (Contracts không biết LevelDefinition của Gameplay)
        public readonly bool IsDaily;         // level Daily Challenge (sinh theo ngày, không thuộc LevelDatabase)

        public LevelLoadedEvent(int number, string levelId, bool isDaily = false)
        {
            Number = number;
            LevelId = levelId;
            IsDaily = isDaily;
        }
    }
}
