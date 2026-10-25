namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Level vừa nạp, đưa cho từng ILevelIntro (nội bộ Gameplay). LevelLoadedEvent (Contracts) chỉ mang id
    /// để feature khác không phụ thuộc LevelDefinition; intro cần cả asset nên LevelIntroSequencer ghép thêm.
    /// </summary>
    public readonly struct LevelIntroContext
    {
        public readonly int Number;           // số level hiển thị (bắt đầu từ 1); Daily vẫn là level thường hiện tại
        public readonly LevelDefinition Level;
        public readonly bool IsDaily;

        public LevelIntroContext(int number, LevelDefinition level, bool isDaily)
        {
            Number = number;
            Level = level;
            IsDaily = isDaily;
        }
    }
}
