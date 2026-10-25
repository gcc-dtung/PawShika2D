using PawBlock.Core.Contracts;

namespace PawBlock.Shared
{
    /// <summary>Chữ số lượng của 1 món quà: "15m" cho năng lượng vô hạn (phút), "x2" cho booster / coin.</summary>
    public static class RewardText
    {
        public static string Amount(DailyRewardItem item) =>
            item.kind == DailyRewardKind.InfiniteEnergy ? $"{item.amount}m" : $"x{item.amount}";
    }
}
