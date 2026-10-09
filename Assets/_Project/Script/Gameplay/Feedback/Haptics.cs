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
/// - iOS: UIImpactFeedbackGenerator / UINotificationFeedbackGenerator (Plugins/iOS/HapticsIOS.mm).
/// - Editor / PC: không làm gì.
/// Không tự kiểm tra cài đặt Rung: gọi qua AudioManager.Vibrate(type) để theo cài đặt của người chơi.
/// </summary>
public static class Haptics
{
    private const float MinInterval = 0.03f; // nhiều sự kiện cùng frame chỉ rung 1 lần
    private static float _lastTime = -1f;

    public static void Play(HapticType type)
    {
        float now = Time.unscaledTime;
        if (now - _lastTime < MinInterval) return;
        _lastTime = now;

#if UNITY_ANDROID && !UNITY_EDITOR
        PlayAndroid(type);
#elif UNITY_IOS && !UNITY_EDITOR
        _HapticsPlay((int)type);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _HapticsPlay(int type);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool _init;
    private static AndroidJavaObject _vibrator;
    private static AndroidJavaClass _effectClass; // android.os.VibrationEffect (API 26+)
    private static bool _amplitude;

    private static void PlayAndroid(HapticType type)
    {
        if (!Init())
        {
            Handheld.Vibrate(); // không lấy được Vibrator: rung kiểu cũ (giữ tham chiếu để Unity tự thêm quyền VIBRATE)
            return;
        }

        try
        {
            switch (type)
            {
                case HapticType.Light: OneShot(15, 70); break;
                case HapticType.Medium: OneShot(25, 150); break;
                case HapticType.Heavy: OneShot(45, 255); break;
                case HapticType.Success: Waveform(new long[] { 0, 30, 70, 45 }, new[] { 0, 120, 0, 255 }); break;
                case HapticType.Failure: Waveform(new long[] { 0, 70, 90, 110 }, new[] { 0, 255, 0, 200 }); break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Haptics] " + e.Message);
        }
    }

    private static bool Init()
    {
        if (_init) return _vibrator != null;
        _init = true;
        try
        {
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator"))
            {
                _vibrator = null;
                return false;
            }

            int sdk;
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
            if (sdk >= 26)
            {
                _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                _amplitude = _vibrator.Call<bool>("hasAmplitudeControl");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Haptics] Không lấy được Vibrator: " + e.Message);
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
#endif
}
