using UnityEditor;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// Nối GameConfig (Game/Configs) tới config của từng feature (chạy lại được, không ghi đè số đã chỉnh).
    /// Config nằm ở Configs/ của feature sở hữu (từ P8); tìm theo kiểu nên đổi thư mục không phải sửa tool.
    /// Thiếu EconomyConfig / LivesEnergyConfig thì tạo mới trong Features/Gameplay/Configs.
    /// </summary>
    public static class GameConfigSetup
    {
        private const string GameConfigPath = "Assets/_Project/Game/Configs/GameConfig.asset";
        private const string GameplayConfigs = "Assets/_Project/Features/Gameplay/Configs/";

        [MenuItem("Tools/PawBlock/Builders/Setup Game Config", priority = 99)]
        public static void Setup()
        {
            GameConfig config = Find<GameConfig>() ?? Create<GameConfig>(GameConfigPath);
            config.economy = Find<EconomyConfig>() ?? Create<EconomyConfig>(GameplayConfigs + "EconomyConfig.asset");
            config.livesEnergy = Find<LivesEnergyConfig>() ?? Create<LivesEnergyConfig>(GameplayConfigs + "LivesEnergyConfig.asset");
            config.boosters = Find<BoosterDatabase>();
            config.shop = Find<ShopDatabase>();
            config.dailyReward = Find<DailyRewardDatabase>();
            config.audio = Find<AudioDatabase>();
            config.difficultyTheme = Find<DifficultyTheme>();
            config.visuals = Find<GameplayVisuals>();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            Debug.Log($"[GameConfigSetup] Xong: {AssetDatabase.GetAssetPath(config)} trỏ tới config của các feature.");
        }

        /// <summary>Asset đầu tiên kiểu <typeparamref name="T"/> trong Assets/_Project (null nếu chưa có).</summary>
        public static T Find<T>() where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project" }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }
            return null;
        }

        private static T Create<T>(string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
