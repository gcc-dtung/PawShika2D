namespace PawBlock.Core.Contracts
{
    /// <summary>Số coin đổi (Economy – CoinWallet). Delta &gt; 0: cộng, &lt; 0: tiêu.</summary>
    public readonly struct CoinsChangedEvent : IGameEvent
    {
        public readonly int Balance;
        public readonly int Delta;

        public CoinsChangedEvent(int balance, int delta)
        {
            Balance = balance;
            Delta = delta;
        }
    }
}
