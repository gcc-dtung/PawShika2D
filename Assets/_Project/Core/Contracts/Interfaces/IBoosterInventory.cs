namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Số lượt booster của người chơi. Gameplay cài đặt (BoosterManager); Shop (mua gói), DailyReward (trao quà) dùng.
    /// </summary>
    public interface IBoosterInventory
    {
        /// <summary>Số lượt hiện có (lần đầu = số lượt khởi đầu của booster).</summary>
        int GetCount(BoosterType type);

        /// <summary>Cộng lượt; amount &lt;= 0 hoặc booster không có trong database thì bỏ qua.</summary>
        void Add(BoosterType type, int amount);
    }
}
