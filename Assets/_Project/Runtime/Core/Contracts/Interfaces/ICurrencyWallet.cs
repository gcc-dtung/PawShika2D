namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Ví 1 loại tiền (coin, sao). Economy cài đặt (CoinWallet, StarWallet);
    /// Gameplay, Shop, DailyReward, LuckyWheel dùng để cộng / tiêu mà không biết Economy.
    /// </summary>
    public interface ICurrencyWallet
    {
        int Balance { get; }

        /// <summary>Đủ tiền thì trừ và trả về true; không đủ (hoặc amount &lt; 0) thì không đổi gì.</summary>
        bool TrySpend(int amount);

        /// <summary>Cộng tiền; amount &lt;= 0 thì bỏ qua.</summary>
        void Add(int amount);
    }
}
