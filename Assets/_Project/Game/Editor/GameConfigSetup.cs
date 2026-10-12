using UnityEditor;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// Dựng cấu hình game (chạy lại được, không ghi đè số đã chỉnh):
    /// - Database của dự án (Booster, Shop, Daily, Audio, DifficultyTheme) đang ở GameAssets/10_Data_Configs: chuyển sang
    ///   Assets/_Project/Configs (giữ GUID nên scene / prefab vẫn trỏ đúng), rồi chép 1 bản lại chỗ cũ làm bản gốc lưu trữ.
    ///   Từ đây game và các builder dùng bản trong Configs.
    /// - Tạo EconomyConfig, LivesEnergyConfig trong Configs (nếu chưa có).
    /// - Tạo Configs/GameConfig trỏ tới tất cả (GameBootstrap giữ tham chiếu).
    /// </summary>
    public static class GameConfigSetup
    {
        public const string Folder = "Assets/_Project/Configs/";
        private const string OldFolder = "Assets/_Project/GameAssets/10_Data_Configs/";
        private const string GameConfigPath = "Assets/_Project/Configs/GameConfig.asset";
        private const string VisualsPath = "Assets/_Project/Configs/GameplayVisuals.asset";

        private static readonly string[] Databases =
        {
            "BoosterDatabase.asset", "ShopDatabase.asset", "DailyRewardDatabase.asset", "AudioDatabase.asset", "DifficultyTheme.asset"
        };

        [MenuItem("Tools/PawBlock/Builders/Setup Game Config", priority = 99)]
        public static void Setup()
        {
            EnsureFolder();
            foreach (string file in Databases) MoveKeepingCopy(file);

            GameConfig config = LoadOrCreate<GameConfig>(GameConfigPath);
            config.economy = LoadOrCreate<EconomyConfig>(Folder + "EconomyConfig.asset");
            config.livesEnergy = LoadOrCreate<LivesEnergyConfig>(Folder + "LivesEnergyConfig.asset");
            config.boosters = AssetDatabase.LoadAssetAtPath<BoosterDatabase>(Folder + "BoosterDatabase.asset");
            config.shop = AssetDatabase.LoadAssetAtPath<ShopDatabase>(Folder + "ShopDatabase.asset");
            config.dailyReward = AssetDatabase.LoadAssetAtPath<DailyRewardDatabase>(Folder + "DailyRewardDatabase.asset");
            config.audio = AssetDatabase.LoadAssetAtPath<AudioDatabase>(Folder + "AudioDatabase.asset");
            config.difficultyTheme = AssetDatabase.LoadAssetAtPath<DifficultyTheme>(Folder + "DifficultyTheme.asset");
            config.visuals = AssetDatabase.LoadAssetAtPath<GameplayVisuals>(VisualsPath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            Debug.Log($"[GameConfigSetup] Xong: {GameConfigPath} trỏ tới config trong {Folder}.");
        }

        private static void EnsureFolder()
        {
            string path = Folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/_Project", "Configs");
        }

        /// <summary>Chuyển asset (giữ GUID) sang Configs rồi chép bản sao về chỗ cũ. Đã chuyển rồi thì bỏ qua.</summary>
        private static void MoveKeepingCopy(string file)
        {
            string from = OldFolder + file, to = Folder + file;
            if (AssetDatabase.LoadMainAssetAtPath(to) != null) return;
            if (AssetDatabase.LoadMainAssetAtPath(from) == null)
            {
                Debug.LogWarning($"[GameConfigSetup] Không có {from}, bỏ qua (builder tương ứng sẽ tạo mới trong {Folder}).");
                return;
            }
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[GameConfigSetup] Không chuyển được {from}: {error}");
                return;
            }
            AssetDatabase.CopyAsset(to, from);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
