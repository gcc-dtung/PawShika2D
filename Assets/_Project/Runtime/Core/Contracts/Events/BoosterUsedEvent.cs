namespace PawBlock.Core.Contracts
{
    /// <summary>Vừa dùng 1 booster có tác dụng (Gameplay – BoosterManager), kể cả lượt miễn phí của tutorial.</summary>
    public readonly struct BoosterUsedEvent : IGameEvent
    {
        public readonly BoosterType Type;

        public BoosterUsedEvent(BoosterType type)
        {
            Type = type;
        }
    }
}
