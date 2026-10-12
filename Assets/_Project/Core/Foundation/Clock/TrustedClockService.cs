using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Đồng hồ chống chỉnh giờ máy cho năng lượng và điểm danh (luật ở TrustedClockModel).
    /// - Lấy giờ server bằng header "Date" của 1 request HEAD HTTPS (không cần server riêng, không cần quyền gì thêm),
    ///   bù nửa thời gian khứ hồi. Thử lần lượt các URL, lỗi hết thì giữ chế độ offline (chặn lùi giờ).
    /// - GameBootstrap tạo 1 instance và gọi RequestSync(true) lúc mở game; quay lại app thì đồng bộ lại
    ///   (cách nhau tối thiểu ResyncInterval). Đồng bộ xong phát Synced.
    /// - Mốc tin cậy lưu ở section "clock".
    /// </summary>
    public sealed class TrustedClockService : IClock
    {
        private static readonly string[] TimeUrls = { "https://www.google.com", "https://www.cloudflare.com", "https://www.microsoft.com" };
        private const float RequestTimeout = 5f;
        private const float ResyncInterval = 120f;
        private const long SaveEvery = 60; // giây: ghi mốc tin cậy vào save không quá thường xuyên

        private readonly ISaveService _save;
        private readonly ClockSave _clock;
        private readonly TrustedClockModel _model;
        private readonly TrustedClockRunner _runner;
        private float _lastSyncAttempt = -1000f;

        public event Action Synced;

        /// <summary><paramref name="runner"/>: component đặt sẵn trong scene chạy coroutine đồng bộ (null: không đồng bộ, dùng giờ máy).</summary>
        public TrustedClockService(ISaveService save, TrustedClockRunner runner)
        {
            _save = save;
            _runner = runner;
            if (_runner != null) _runner.Init(this);
            _clock = save.Get<ClockSave>(ClockSave.Key);
            _model = new TrustedClockModel(_clock.lastTrustedUtc);
        }

        public bool IsSynced => _model.IsSynced;
        public bool IsDeviceClockOff => _model.IsDeviceClockOff;
        public long DeviceOffsetSeconds => _model.DeviceOffset;

        public long UtcNowSeconds
        {
            get
            {
                long now = _model.Now(DeviceUtc, Mono);
                if (now - _clock.lastTrustedUtc >= SaveEvery || now < _clock.lastTrustedUtc)
                {
                    _clock.lastTrustedUtc = now;
                    _save.MarkDirty();
                }
                return now;
            }
        }

        public DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(UtcNowSeconds).ToLocalTime().DateTime;

        private static long DeviceUtc => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private static double Mono => Time.realtimeSinceStartupAsDouble;

        /// <summary>Đồng bộ giờ server (bỏ qua nếu vừa thử trong ResyncInterval, trừ khi force). Chỉ chạy trong Play mode.</summary>
        public void RequestSync(bool force = false)
        {
            if (!Application.isPlaying || _runner == null)
            {
                return;
            }
            if (!force && Time.realtimeSinceStartup - _lastSyncAttempt < ResyncInterval)
            {
                return;
            }
            _lastSyncAttempt = Time.realtimeSinceStartup;
            _runner.StartCoroutine(SyncRoutine());
        }

        private IEnumerator SyncRoutine()
        {
            foreach (string url in TimeUrls)
            {
                double sent = Mono;
                using (UnityWebRequest req = UnityWebRequest.Head(url))
                {
                    req.timeout = (int)RequestTimeout;
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        continue;
                    }
                    if (!TryParseHttpDate(req.GetResponseHeader("Date"), out long serverUtc))
                    {
                        continue;
                    }

                    double received = Mono;
                    long corrected = serverUtc + (long)Math.Round((received - sent) * 0.5); // bù nửa thời gian khứ hồi
                    _model.ApplyServerTime(corrected, DeviceUtc, received);
                    _clock.lastTrustedUtc = corrected;
                    _save.MarkDirty();
                    if (_model.IsDeviceClockOff)
                    {
                        Debug.LogWarning($"[TrustedClock] Giờ máy lệch {_model.DeviceOffset} s so với server: dùng giờ server");
                    }
                    else
                    {
                        Debug.Log($"[TrustedClock] Đồng bộ giờ server qua {url} (lệch {_model.DeviceOffset} s)");
                    }
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
            if (string.IsNullOrEmpty(header))
            {
                return false;
            }
            if (!DateTimeOffset.TryParseExact(header.Trim(), "r", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset date))
            {
                return false;
            }
            unixSeconds = date.ToUnixTimeSeconds();
            return true;
        }
    }
}
