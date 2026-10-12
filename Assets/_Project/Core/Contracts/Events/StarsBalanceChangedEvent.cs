namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Số dư ví sao đổi (Economy – StarWallet): cộng khi thắng, tiêu ở Vòng quay may mắn.
    /// Khác StarsChangedEvent (số sao của ván đang chơi).
    /// </summary>
    public readonly struct StarsBalanceChangedEvent : IGameEvent
    {
        public readonly int Balance;
        public readonly int Delta;

        public StarsBalanceChangedEvent(int balance, int delta)
        {
            Balance = balance;
            Delta = delta;
        }
    }
}
