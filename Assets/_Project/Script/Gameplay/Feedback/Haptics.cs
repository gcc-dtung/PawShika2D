using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Kiểu rung, từ nhẹ (chạm) tới mạnh (sai) và 2 nhịp thắng / thua.</summary>
public enum HapticType
{
    Light,   // đặt khối, mở khóa ô
    Medium,  // dùng booster, khiên đỡ
    Heavy,   // đặt sai
    Success, // thắng
    Failure  // hết mạng / thua
}

/// <summary>
/// Rung ngắn kiểu haptic thay cho Handheld.Vibrate (rung dài ~0.5 s, không chỉnh được cường độ).
/// - Android: Vibrator + VibrationEffect (API 26+: chỉnh thời gian + cường độ, nhịp đôi); máy cũ chỉ chỉnh thời gian.
///   Máy không chỉnh được cường độ (motor rung kiểu cũ) cần rung lâu hơn mới cảm nhận được nên thời gian dài hơn.
/// - iOS: UIImpactFeedbackGenerator / UINotificationFeedbackGenerator (Plugins/iOS/HapticsIOS.mm).
/// - Editor / PC: không làm gì.
/// Không tự kiểm tra cài đặt Rung: gọi qua AudioManager.Vibrate(type) để theo cài đặt của người chơi.
/// Status / LastError hiện trong Cheat Panel (tab Khác) để dò lỗi trên máy thật.
/// </summary>
public static class Haptics
{
    private const float MinInterval = 0.03f; // nhiều sự kiện cùng frame chỉ rung 1 lần
    private static float _lastTime = -1f;

    /// <summary>Lỗi gần nhất khi gọi rung (rỗng = chưa lỗi).</summary>
    public static string LastError { get; private set; } = "";

    /// <summary>Số lần đã gọi rung thật (sau khi qua cài đặt Rung và giới hạn tần suất).</summary>
    public static int PlayCount { get; private set; }

    public static void Play(HapticType type)
    {
        float now = Time.unscaledTime;
        if (now - _lastTime < MinInterval) return;
        _lastTime = now;
        PlayCount++;

#if UNITY_ANDROID && !UNITY_EDITOR
        PlayAndroid(type);
#elif UNITY_IOS && !UNITY_EDITOR
        _HapticsPlay((int)type);
#endif
    }

    /// <summary>Rung dài kiểu cũ (Handheld.Vibrate, ~0.5 s) để thử máy có rung được không.</summary>
    public static void PlayLegacy()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _HapticsPlay(int type);

    public static string Status => "iOS";
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static bool _init;
    private static AndroidJavaObject _vibrator;
    private static AndroidJavaClass _effectClass; // android.os.VibrationEffect (API 26+)
    private static bool _amplitude;
    private static int _sdk;

    public static string Status
    {
        get
        {
            bool ok = Init();
            return $"Android API {_sdk} · có motor rung: {(ok ? "có" : "không")} · chỉnh cường độ: {(_amplitude ? "có" : "không")}";
        }
    }

    private static void PlayAndroid(HapticType type)
    {
        if (!Init())
        {
            Handheld.Vibrate(); // không lấy được Vibrator: rung kiểu cũ (giữ tham chiếu để Unity tự thêm quyền VIBRATE)
            return;
        }

        try
        {
            // Không chỉnh được cường độ: rung lâu hơn cho mạnh hơn (motor cần ~25 ms mới quay đủ để cảm thấy)
            switch (type)
            {
                case HapticType.Light: OneShot(_amplitude ? 25 : 30, 90); break;
                case HapticType.Medium: OneShot(_amplitude ? 40 : 50, 170); break;
                case HapticType.Heavy: OneShot(_amplitude ? 70 : 90, 255); break;
                case HapticType.Success: Waveform(new long[] { 0, 40, 80, 70 }, new[] { 0, 140, 0, 255 }); break;
                case HapticType.Failure: Waveform(new long[] { 0, 90, 100, 130 }, new[] { 0, 255, 0, 220 }); break;
            }
            LastError = "";
        }
        catch (System.Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("[Haptics] " + e.Message);
        }
    }

    private static bool Init()
    {
        if (_init) return _vibrator != null;
        _init = true;
        try
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = version.GetStatic<int>("SDK_INT");
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator"))
            {
                _vibrator = null;
                LastError = "Máy báo không có motor rung";
                return false;
            }
            if (_sdk >= 26)
            {
                _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                _amplitude = _vibrator.Call<bool>("hasAmplitudeControl");
            }
        }
        catch (System.Exception e)
        {
            LastError = "Không lấy được Vibrator: " + e.Message;
            Debug.LogWarning("[Haptics] " + LastError);
            _vibrator = null;
        }
        return _vibrator != null;
    }

    private static void OneShot(long ms, int amplitude)
    {
        if (_effectClass == null)
        {
            _vibrator.Call("vibrate", ms);
            return;
        }
        // Máy không chỉnh được cường độ: DEFAULT_AMPLITUDE (-1), độ mạnh chỉ còn khác nhau theo thời gian
        using (AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, _amplitude ? amplitude : -1))
            _vibrator.Call("vibrate", effect);
    }

    // timings: [nghỉ, rung, nghỉ, rung...] (ms); amplitudes cùng độ dài, 0 = nghỉ
    private static void Waveform(long[] timings, int[] amplitudes)
    {
        if (_effectClass == null)
        {
            _vibrator.Call("vibrate", timings, -1);
            return;
        }
        AndroidJavaObject effect = _amplitude
            ? _effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1)
            : _effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1);
        using (effect) _vibrator.Call("vibrate", effect);
    }
#else
    public static string Status => "Editor / PC: không rung";
#endif
}
