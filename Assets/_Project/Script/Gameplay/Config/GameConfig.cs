using UnityEngine;

/// <summary>
/// File cấu hình gốc, đặt tại Resources/GameConfig: mở 1 file là thấy mọi config của game.
/// Config mới (Economy, LivesEnergy) đọc qua đây. Các database cũ (Booster, Shop, Daily, Audio, DifficultyTheme)
/// vẫn gán thẳng vào component như trước; ở đây chỉ trỏ cùng asset để tìm / sửa nhanh.
/// Mọi asset config nằm ở Assets/_Project/Configs (dựng bằng Tools/PawBlock/Builders/Setup Game Config).
/// </summary>
[CreateAssetMenu(menuName = "PawBlock/Config/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    public const string ResourcePath = "GameConfig";

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

    private static GameConfig _instance;
    private static EconomyConfig _defaultEconomy;
    private static LivesEnergyConfig _defaultLivesEnergy;

    public static GameConfig Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameConfig>(ResourcePath);
            return _instance;
        }
    }

    /// <summary>Thiếu asset thì dùng giá trị mặc định trong code (có cảnh báo) để game vẫn chạy.</summary>
    public static EconomyConfig Economy
    {
        get
        {
            GameConfig c = Instance;
            if (c != null && c.economy != null) return c.economy;
            return _defaultEconomy != null ? _defaultEconomy : _defaultEconomy = Fallback<EconomyConfig>();
        }
    }

    public static LivesEnergyConfig LivesEnergy
    {
        get
        {
            GameConfig c = Instance;
            if (c != null && c.livesEnergy != null) return c.livesEnergy;
            return _defaultLivesEnergy != null ? _defaultLivesEnergy : _defaultLivesEnergy = Fallback<LivesEnergyConfig>();
        }
    }

    private static T Fallback<T>() where T : ScriptableObject
    {
        Debug.LogWarning($"[GameConfig] Thiếu {typeof(T).Name} trong Resources/{ResourcePath} (chạy Tools/PawBlock/Builders/Setup Game Config). Dùng giá trị mặc định.");
        return CreateInstance<T>();
    }
}
