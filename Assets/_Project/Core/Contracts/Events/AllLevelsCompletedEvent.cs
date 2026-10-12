namespace PawBlock.Core.Contracts
{
    /// <summary>Thắng level cuối cùng của database (màn "Coming soon").</summary>
    public readonly struct AllLevelsCompletedEvent : IGameEvent
    {
        public readonly int LevelCount;

        public AllLevelsCompletedEvent(int levelCount)
        {
            LevelCount = levelCount;
        }
    }
}
