namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Trao quà vào ví / kho / năng lượng. DailyReward cài đặt (DailyRewardManager); DailyChallenge (quà mốc),
    /// LuckyWheel (ô trúng) dùng mà không biết DailyReward.
    /// </summary>
    public interface IRewardGranter
    {
        void Grant(DailyRewardItem item);
    }
}
