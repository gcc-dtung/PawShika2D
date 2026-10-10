using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Kiểu rung. Giá trị số khớp Plugins/iOS/HapticsIOS.mm, thêm mới thì thêm ở cuối.
/// Nhẹ nhất: Selection / Tick / Coin / Soft; vừa: Light / Rigid / Medium; mạnh: Heavy / Thud; chuỗi: Success / Failure / Warning / Rise.
/// </summary>
public enum HapticType
{
    Light = 0,     // đặt khối
    Medium = 1,    // dùng booster
    Heavy = 2,     // lưới kín mà còn khối sai
    Success = 3,   // thắng
    Failure = 4,   // hết giờ / thua
    Selection = 5, // khung chọn giãn qua từng ô (rất nhẹ)
    Soft = 6,      // gỡ khối (mềm)
    Rigid = 7,     // mở khóa ô số (khô, gọn)
    Warning = 8,   // vừa xuống dưới ngưỡng sắp hết giờ (2 nhịp)
    Tick = 9,      // mỗi giây khi sắp hết giờ
    Rise = 10,     // bật đóng băng (mạnh dần)
    Thud = 11,     // băng vỡ (thụp rồi tắt dần)
    Coin = 12,     // mỗi đồng coin bay chạm ô coin (tick nhẹ)
    Button = 13    // bấm nút UI (UIButtonSounds)
}

/// <summary>
/// Rung ngắn kiểu haptic thay cho Handheld.Vibrate (rung dài ~0.5 s, không chỉnh được cường độ).
/// - Android, chọn cách tốt nhất máy hỗ trợ:
///   1. API 30+ có primitive: VibrationEffect.Composition (CLICK, TICK, LOW_TICK, THUD, SLOW_RISE, QUICK_FALL...) – rung sắc, nhẹ, chuẩn hãng.
///   2. API 29+: VibrationEffect.createPredefined (EFFECT_TICK / CLICK / DOUBLE_CLICK / HEAVY_CLICK).
///   3. API 26+: createOneShot / createWaveform tự chỉnh thời gian + cường độ (máy không chỉnh được cường độ thì chỉ khác thời gian).
///   4. Máy cũ: vibrate(ms). Kiểu quá nhẹ (Selection / Tick / Coin) chỉ rung ở bước 3 khi máy chỉnh được cường độ,
///      máy không chỉnh được thì bỏ qua để không thành tiếng "rè".
/// - iOS: UIImpactFeedbackGenerator (Light / Medium / Heavy / Soft / Rigid, có mức mạnh), UINotificationFeedbackGenerator
///   (Success / Warning / Error), UISelectionFeedbackGenerator (Plugins/iOS/HapticsIOS.mm).
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

    /// <summary>Cách rung vừa dùng cho lần gọi gần nhất (Cheat Panel), vd "Composition", "Predefined", "OneShot".</summary>
    public static string LastMethod { get; private set; } = "";

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
        LastMethod = "iOS";
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
    // android.os.VibrationEffect.Composition.PRIMITIVE_*
    private const int Click = 1, ThudP = 2, QuickRise = 4, SlowRise = 5, QuickFall = 6, TickP = 7, LowTick = 8;
    // android.os.VibrationEffect.EFFECT_*
    private const int EffectClick = 0, EffectDoubleClick = 1, EffectTick = 2, EffectHeavyClick = 5;

    private static bool _init;
    private static AndroidJavaObject _vibrator;
    private static AndroidJavaClass _effectClass; // android.os.VibrationEffect (API 26+)
    private static bool _amplitude;
    private static int _sdk;
    private static readonly bool[] _primitive = new bool[9]; // _primitive[id]: máy có primitive này

    public static string Status
    {
        get
        {
            bool ok = Init();
            int prims = 0;
            foreach (bool p in _primitive) if (p) prims++;
            return $"Android API {_sdk} · có motor rung: {(ok ? "có" : "không")} · chỉnh cường độ: {(_amplitude ? "có" : "không")}" +
                   $" · primitive: {prims}/8 · predefined: {(_sdk >= 29 ? "có" : "không")}";
        }
    }

    /// <summary>1 bước trong chuỗi primitive: loại, mức mạnh 0–1, chờ bao lâu (ms) trước khi chạy.</summary>
    private readonly struct Prim
    {
        public readonly int Id;
        public readonly float Scale;
        public readonly int Delay;

        public Prim(int id, float scale, int delay = 0)
        {
            Id = id;
            Scale = scale;
            Delay = delay;
        }
    }

    /// <summary>
    /// Cách rung của 1 kiểu, thử lần lượt: primitives (API 30+) -> predefined (API 29+) -> oneShot / waveform tự chỉnh.
    /// primitives null = bỏ bước 1; predefined -1 = bỏ bước 2; oneShotMs 0 và không có waveform = bỏ qua (kiểu quá nhẹ).
    /// </summary>
    private readonly struct Pattern
    {
        public readonly Prim[] Primitives;
        public readonly int Predefined;
        public readonly long OneShotMs;
        public readonly int Amplitude;
        public readonly long[] Timings;   // [nghỉ, rung, nghỉ, rung...] (ms)
        public readonly int[] Amplitudes; // cùng độ dài, 0 = nghỉ

        public Pattern(Prim[] primitives, int predefined, long oneShotMs, int amplitude, long[] timings = null, int[] amplitudes = null)
        {
            Primitives = primitives;
            Predefined = predefined;
            OneShotMs = oneShotMs;
            Amplitude = amplitude;
            Timings = timings;
            Amplitudes = amplitudes;
        }
    }

    // Mức mạnh chỉnh ở đây. Giữ nhẹ: thao tác liên tục (đặt / gỡ / kéo) chỉ tick nhẹ, rung rõ dành cho sự kiện lớn.
    private static Pattern Get(HapticType type)
    {
        switch (type)
        {
            case HapticType.Light: return new Pattern(new[] { new Prim(Click, 0.45f) }, EffectTick, 18, 70);
            case HapticType.Medium: return new Pattern(new[] { new Prim(Click, 0.75f) }, EffectClick, 28, 120);
            case HapticType.Heavy: return new Pattern(new[] { new Prim(ThudP, 0.8f) }, EffectHeavyClick, 45, 190);
            case HapticType.Selection: return new Pattern(new[] { new Prim(LowTick, 0.5f) }, -1, 10, 45);
            case HapticType.Coin: return new Pattern(new[] { new Prim(TickP, 0.55f) }, EffectTick, 12, 60);
            case HapticType.Button: return new Pattern(new[] { new Prim(Click, 0.35f) }, EffectTick, 15, 70);
            case HapticType.Soft: return new Pattern(new[] { new Prim(LowTick, 0.8f) }, EffectTick, 14, 60);
            case HapticType.Rigid: return new Pattern(new[] { new Prim(Click, 0.6f) }, EffectClick, 20, 140);
            case HapticType.Tick: return new Pattern(new[] { new Prim(TickP, 0.5f) }, EffectTick, 12, 60);
            case HapticType.Warning:
                return new Pattern(new[] { new Prim(Click, 0.6f), new Prim(Click, 0.6f, 90) }, EffectDoubleClick, 0, 0,
                    new long[] { 0, 25, 90, 25 }, new[] { 0, 130, 0, 130 });
            case HapticType.Rise:
                return new Pattern(new[] { new Prim(SlowRise, 0.6f) }, -1, 0, 0,
                    new long[] { 0, 60, 0, 60, 0, 60 }, new[] { 0, 40, 0, 90, 0, 150 });
            case HapticType.Thud:
                return new Pattern(new[] { new Prim(ThudP, 0.7f), new Prim(QuickFall, 0.5f, 30) }, EffectHeavyClick, 40, 170);
            case HapticType.Success:
                return new Pattern(new[] { new Prim(Click, 0.5f), new Prim(QuickRise, 0.7f, 70) }, EffectDoubleClick, 0, 0,
                    new long[] { 0, 30, 70, 50 }, new[] { 0, 110, 0, 200 });
            case HapticType.Failure:
                return new Pattern(new[] { new Prim(ThudP, 0.8f), new Prim(QuickFall, 0.6f, 100) }, EffectHeavyClick, 0, 0,
                    new long[] { 0, 60, 100, 90 }, new[] { 0, 200, 0, 150 });
            default: return new Pattern(null, -1, 0, 0);
        }
    }

    private static void PlayAndroid(HapticType type)
    {
        if (!Init())
        {
            if (!IsTiny(type)) Handheld.Vibrate(); // giữ tham chiếu để Unity tự thêm quyền VIBRATE
            LastMethod = "Handheld.Vibrate";
            return;
        }

        try
        {
            Pattern p = Get(type);
            if (TryComposition(p.Primitives)) LastMethod = "Composition";
            else if (TryPredefined(p.Predefined)) LastMethod = "Predefined";
            // Rise là các nấc liền nhau: máy không chỉnh được cường độ sẽ thành 1 lần rè dài -> bỏ
            else if (p.Timings != null && (_amplitude || type != HapticType.Rise)) { Waveform(p.Timings, p.Amplitudes); LastMethod = "Waveform"; }
            else if (p.OneShotMs > 0 && (_amplitude || !IsTiny(type))) { OneShot(p.OneShotMs, p.Amplitude); LastMethod = "OneShot"; }
            else LastMethod = "Bỏ qua (máy không hỗ trợ kiểu nhẹ này)";
            LastError = "";
        }
        catch (System.Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("[Haptics] " + e.Message);
        }
    }

    /// <summary>Kiểu rất nhẹ, gọi dồn dập: chỉ rung khi máy rung được nhẹ thật sự.</summary>
    private static bool IsTiny(HapticType type) => type == HapticType.Selection || type == HapticType.Tick || type == HapticType.Coin;

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
            if (_sdk >= 30)
            {
                for (int id = 1; id < _primitive.Length; id++)
                {
                    if (id == LowTick && _sdk < 31) continue;
                    _primitive[id] = _vibrator.Call<bool>("areAllPrimitivesSupported", new[] { id });
                }
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

    /// <summary>Chuỗi primitive (API 30+). Máy thiếu 1 primitive nào thì trả về false để dùng cách khác.</summary>
    private static bool TryComposition(Prim[] prims)
    {
        if (prims == null || _sdk < 30) return false;
        foreach (Prim p in prims)
        {
            if (!_primitive[p.Id]) return false;
        }

        AndroidJavaObject comp = _effectClass.CallStatic<AndroidJavaObject>("startComposition");
        foreach (Prim p in prims)
        {
            AndroidJavaObject next = comp.Call<AndroidJavaObject>("addPrimitive", p.Id, p.Scale, p.Delay);
            if (next != comp) comp.Dispose();
            comp = next; // addPrimitive trả về chính Composition (builder)
        }
        using (comp)
        using (AndroidJavaObject effect = comp.Call<AndroidJavaObject>("compose"))
            _vibrator.Call("vibrate", effect);
        return true;
    }

    /// <summary>Mẫu rung có sẵn của hãng (API 29+).</summary>
    private static bool TryPredefined(int effectId)
    {
        if (effectId < 0 || _sdk < 29) return false;
        using (AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>("createPredefined", effectId))
            _vibrator.Call("vibrate", effect);
        return true;
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
