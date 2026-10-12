using PawBlock.Features.Gameplay;

namespace PawBlock.Core.Contracts
{
    /// <summary>LevelManager vừa nạp 1 level thường hoặc level Daily Challenge (không phát cho màn tutorial).</summary>
    public readonly struct LevelLoadedEvent : IGameEvent
    {
        public readonly int Number;           // số level hiển thị (bắt đầu từ 1); Daily vẫn là level thường hiện tại
        public readonly LevelDefinition Level;
        public readonly bool IsDaily;         // level Daily Challenge (sinh theo ngày, không thuộc LevelDatabase)

        public LevelLoadedEvent(int number, LevelDefinition level, bool isDaily = false)
        {
            Number = number;
            Level = level;
            IsDaily = isDaily;
        }
    }
}
