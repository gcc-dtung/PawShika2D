using UnityEngine;

/// <summary>
/// Số liệu kinh tế: coin thưởng thắng, giá / số lượng hồi mạng và nạp năng lượng, quảng cáo.
/// Giá booster ở BoosterDatabase, gói Shop ở ShopDatabase, quà điểm danh ở DailyRewardDatabase.
/// Lấy qua GameConfig.Economy.
/// </summary>
[CreateAssetMenu(menuName = "PawBlock/Config/Economy Config", fileName = "EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Thắng level")]
    [Tooltip("Coin nút Continue; nút x2 Coin (quảng cáo) nhận gấp đôi. gamePlay_CoinWinLevelNormal trong RemoteConfig gốc")]
    [Min(0)] public int winCoinReward = 10;

    [Header("Hồi mạng (hết mạng trong màn)")]
    [Min(1)] public int reviveAdsLives = 1;
    [Min(1)] public int reviveCoinLives = 3;
    [Min(0)] public int reviveCoinCost = 900;

    [Header("Nạp năng lượng")]
    [Min(1)] public int refillAdsEnergy = 1;
    [Tooltip("Giá coin nạp đầy năng lượng")]
    [Min(0)] public int refillCoinCost = 900;

    [Header("Quảng cáo (giả lập khi chưa có SDK)")]
    [Min(0)] public float adsSimulatedDuration = 0.5f;
}
