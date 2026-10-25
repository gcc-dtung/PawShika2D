namespace PawBlock.Core.Contracts
{
    /// <summary>Booster vừa mở khóa / khóa lại (tutorial hoặc Debug): nút booster vẽ lại.</summary>
    public readonly struct BoosterLockChangedEvent : IGameEvent
    {
        public readonly BoosterType Type;
        public readonly bool Unlocked;

        public BoosterLockChangedEvent(BoosterType type, bool unlocked)
        {
            Type = type;
            Unlocked = unlocked;
        }
    }
}
