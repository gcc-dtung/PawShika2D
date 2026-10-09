using UnityEngine;

/// <summary>
/// Đặt FPS mục tiêu khi mở game. Mobile (Android / iOS) mặc định Application.targetFrameRate = -1
/// nghĩa là khóa 30 FPS và bỏ qua vSyncCount, nên phải tự đặt 60 thì game mới mượt.
/// Desktop / Editor vẫn theo VSync của Quality Settings.
/// Cheat Panel (bấm 10 lần góc trên trái) đổi được 30 / 60 / 90 / 120 để so sánh.
/// </summary>
public static class FrameRateSetup
{
    public const int DefaultFps = 60;
    private const string PrefKey = "Debug_TargetFps";

    public static int TargetFps => Application.targetFrameRate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (!Application.isMobilePlatform) return;
        Apply(PlayerPrefs.GetInt(PrefKey, DefaultFps), false);
    }

    /// <summary>Đặt FPS mục tiêu (mobile). <paramref name="save"/> = nhớ cho lần mở sau.</summary>
    public static void Apply(int fps, bool save = true)
    {
        QualitySettings.vSyncCount = 0; // targetFrameRate chỉ có tác dụng khi tắt VSync
        Application.targetFrameRate = fps;
        if (!save) return;
        PlayerPrefs.SetInt(PrefKey, fps);
        PlayerPrefs.Save();
    }
}
