using UnityEngine;
namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Số liệu kinh tế: coin thưởng thắng, giá / số giây thêm giờ và nạp năng lượng, quảng cáo.
    /// Giá booster ở BoosterDatabase, gói Shop ở ShopDatabase, quà điểm danh ở DailyRewardDatabase.
    /// Lấy qua GameConfig.economy; phần nạp năng lượng (Economy) GameBootstrap truyền dạng số cho RefillEnergyPopup.
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Config/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Thắng level (nút Continue; nút x2 Coin xem quảng cáo nhận gấp đôi)")]
        [Tooltip("Level Normal. gamePlay_CoinWinLevelNormal trong RemoteConfig gốc")]
        [Min(0)] public int winCoinReward = 10;
        [Tooltip("Level Hard. gamePlay_CoinWinLevelHard trong RemoteConfig gốc")]
        [Min(0)] public int winCoinRewardHard = 20;
        [Tooltip("Level Very Hard. gamePlay_CoinWinLevelExpert trong RemoteConfig gốc")]
        [Min(0)] public int winCoinRewardVeryHard = 30;
        [Tooltip("Coin cộng thêm cho mỗi sao đạt được (StarRating): 3 sao ở level Normal = 10 + 3 x 5")]
        [Min(0)] public int coinsPerStar = 5;

        [Header("Thêm giờ (hết giờ trong màn, MoreTimePopup)")]
        [Tooltip("Số giây cộng khi xem quảng cáo")]
        [Min(1)] public int moreTimeAdsSeconds = 30;
        [Tooltip("Số giây cộng khi trả coin")]
        [Min(1)] public int moreTimeCoinSeconds = 60;
        [Min(0)] public int moreTimeCoinCost = 900;

        [Header("Nạp năng lượng")]
        [Min(1)] public int refillAdsEnergy = 1;
        [Tooltip("Giá coin nạp đầy năng lượng")]
        [Min(0)] public int refillCoinCost = 900;

        [Header("Quảng cáo (giả lập khi chưa có SDK)")]
        [Min(0)] public float adsSimulatedDuration = 0.5f;

        /// <summary>Coin thưởng khi thắng: theo độ khó + coinsPerStar cho mỗi sao (0..3).</summary>
        public int WinCoinReward(LevelDifficulty difficulty, int stars) => WinCoinReward(difficulty) + coinsPerStar * Mathf.Max(0, stars);

        /// <summary>Coin thưởng khi thắng level có độ khó <paramref name="difficulty"/> (chưa tính sao).</summary>
        public int WinCoinReward(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard: return winCoinRewardHard;
                case LevelDifficulty.VeryHard: return winCoinRewardVeryHard;
                default: return winCoinReward;
            }
        }
    }
}
