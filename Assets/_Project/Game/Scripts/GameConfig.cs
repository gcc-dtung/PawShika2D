using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// File cấu hình gốc (Configs/GameConfig): mở 1 file là thấy mọi config của game.
    /// GameBootstrap giữ tham chiếu và truyền config con (Economy, LivesEnergy) cho từng component qua Init. Các database cũ (Booster, Shop, Daily, Audio, DifficultyTheme)
    /// vẫn gán thẳng vào component như trước; ở đây chỉ trỏ cùng asset để tìm / sửa nhanh.
    /// Mọi asset config nằm ở Assets/_Project/Configs (dựng bằng Tools/PawBlock/Builders/Setup Game Config).
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Config/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {

        [Header("Đọc qua GameConfig")]
        public EconomyConfig economy;
        public LivesEnergyConfig livesEnergy;

        [Header("Tham khảo (component gán thẳng cùng asset)")]
        public BoosterDatabase boosters;
        public ShopDatabase shop;
        public DailyRewardDatabase dailyReward;
        public AudioDatabase audio;
        public DifficultyTheme difficultyTheme;
        public GameplayVisuals visuals;

    }
}
