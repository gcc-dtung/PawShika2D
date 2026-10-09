using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Đồng hồ chống chỉnh giờ máy cho năng lượng và điểm danh (luật ở TrustedClockModel).
/// - Lấy giờ server bằng header "Date" của 1 request HEAD HTTPS (không cần server riêng, không cần quyền gì thêm),
///   bù nửa thời gian khứ hồi. Thử lần lượt các URL, lỗi hết thì giữ chế độ offline (chặn lùi giờ).
/// - Đồng bộ khi mở game và khi quay lại app (cách nhau tối thiểu resyncInterval). Đồng bộ xong phát Synced.
/// - Mốc tin cậy lưu ở SaveData.clock.lastTrustedUtc.
/// Dùng: TrustedClock.UtcNowSeconds (thay DateTimeOffset.UtcNow), TrustedClock.LocalNow (thay DateTime.Now).
/// </summary>
public static class TrustedClock
{
    private static readonly string[] TimeUrls = { "https://www.google.com", "https://www.cloudflare.com", "https://www.microsoft.com" };
    private const float RequestTimeout = 5f;
    private const float ResyncInterval = 120f;
    private const long SaveEvery = 60; // giây: ghi mốc tin cậy vào save không quá thường xuyên

    private static TrustedClockModel _model;
    private static TrustedClockRunner _runner;
    private static float _lastSyncAttempt = -1000f;

    /// <summary>Vừa lấy được giờ server (giờ tin cậy có thể nhảy): hệ thống theo thời gian nên tính lại.</summary>
    public static event Action Synced;

    public static bool IsSynced => Model.IsSynced;
    public static bool IsDeviceClockOff => Model.IsDeviceClockOff;
    public static long DeviceOffsetSeconds => Model.DeviceOffset;

    /// <summary>Giờ tin cậy, unix giây (UTC).</summary>
    public static long UtcNowSeconds
    {
        get
        {
            long now = Model.Now(DeviceUtc, Mono);
            ClockSave save = SaveSystem.Data.clock;
            if (now - save.lastTrustedUtc >= SaveEvery || now < save.lastTrustedUtc)
            {
                save.lastTrustedUtc = now;
                SaveSystem.MarkDirty();
            }
            return now;
        }
    }

    /// <summary>Giờ tin cậy theo múi giờ máy (dùng cho "sang ngày mới" của điểm danh).</summary>
    public static DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(UtcNowSeconds).ToLocalTime().DateTime;

    private static TrustedClockModel Model => _model ??= new TrustedClockModel(SaveSystem.Data.clock.lastTrustedUtc);
    private static long DeviceUtc => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private static double Mono => Time.realtimeSinceStartupAsDouble;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _model = null;
        _runner = null;
        _lastSyncAttempt = -1000f;
        Synced = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => RequestSync(true);

    /// <summary>Đồng bộ giờ server (bỏ qua nếu vừa thử trong resyncInterval, trừ khi force).</summary>
    public static void RequestSync(bool force = false)
    {
        if (!Application.isPlaying) return;
        if (!force && Time.realtimeSinceStartup - _lastSyncAttempt < ResyncInterval) return;
        _lastSyncAttempt = Time.realtimeSinceStartup;
        EnsureRunner().StartCoroutine(SyncRoutine());
    }

    private static TrustedClockRunner EnsureRunner()
    {
        if (_runner == null)
        {
            GameObject go = new GameObject("[TrustedClock]") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<TrustedClockRunner>();
        }
        return _runner;
    }

    private static IEnumerator SyncRoutine()
    {
        foreach (string url in TimeUrls)
        {
            double sent = Mono;
            using (UnityWebRequest req = UnityWebRequest.Head(url))
            {
                req.timeout = (int)RequestTimeout;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) continue;
                if (!TryParseHttpDate(req.GetResponseHeader("Date"), out long serverUtc)) continue;

                double received = Mono;
                long corrected = serverUtc + (long)Math.Round((received - sent) * 0.5); // bù nửa thời gian khứ hồi
                Model.ApplyServerTime(corrected, DeviceUtc, received);
                SaveSystem.Data.clock.lastTrustedUtc = corrected;
                SaveSystem.MarkDirty();
                if (Model.IsDeviceClockOff)
                    Debug.LogWarning($"[TrustedClock] Giờ máy lệch {Model.DeviceOffset} s so với server: dùng giờ server");
                else
                    Debug.Log($"[TrustedClock] Đồng bộ giờ server qua {url} (lệch {Model.DeviceOffset} s)");
                Synced?.Invoke();
                yield break;
            }
        }
        Debug.Log("[TrustedClock] Không lấy được giờ server (offline): dùng giờ máy, chặn lùi giờ");
    }

    /// <summary>Đọc header Date dạng RFC 1123 ("Thu, 08 Oct 2026 17:30:00 GMT").</summary>
    public static bool TryParseHttpDate(string header, out long unixSeconds)
    {
        unixSeconds = 0;
        if (string.IsNullOrEmpty(header)) return false;
        if (!DateTimeOffset.TryParseExact(header.Trim(), "r", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset date)) return false;
        unixSeconds = date.ToUnixTimeSeconds();
        return true;
    }
}

/// <summary>Object ẩn chạy coroutine đồng bộ của TrustedClock; quay lại app thì đồng bộ lại.</summary>
public class TrustedClockRunner : MonoBehaviour
{
    private void OnApplicationFocus(bool focus)
    {
        if (focus) TrustedClock.RequestSync();
    }
}
