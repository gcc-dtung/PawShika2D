using System;

namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// 1 món quà: coin / booster / năng lượng vô hạn. Dữ liệu trong DailyRewardDatabase, DailyChallengeDatabase,
    /// LuckyWheelDatabase; trao qua IRewardGranter.
    /// </summary>
    [Serializable]
    public class DailyRewardItem
    {
        public DailyRewardKind kind;
        public BoosterType booster; // chỉ dùng khi kind = Booster
        public int amount;
    }
}
