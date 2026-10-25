namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Luật đồng hồ tin cậy (class C# thuần, test được không cần Unity), mọi thời gian tính bằng unix giây (UTC).
    /// - Đã lấy được giờ server: giờ = giờ server lúc đồng bộ + thời gian trôi theo đồng hồ đơn điệu (không đổi khi người chơi
    ///   chỉnh giờ máy). Server là chuẩn, kể cả khi lùi so với mốc đã lưu (sửa lại lần chỉnh giờ tới trước lúc offline).
    /// - Chưa có mạng: dùng giờ máy nhưng không bao giờ nhỏ hơn mốc tin cậy đã lưu (chặn lùi giờ để hồi năng lượng / điểm danh lại).
    /// </summary>
    public class TrustedClockModel
    {
        /// <summary>Máy lệch giờ server quá ngưỡng này (giây) thì coi là đã chỉnh giờ.</summary>
        public const long SuspiciousOffset = 5 * 60;

        private bool _synced;
        private long _serverAtSync;
        private double _monoAtSync;

        /// <summary>Mốc thời gian tin cậy lớn nhất đã thấy (lưu vào save để chặn lùi giờ giữa các lần mở game).</summary>
        public long LastTrusted { get; private set; }

        public bool IsSynced => _synced;

        /// <summary>Giờ server - giờ máy lúc đồng bộ gần nhất (giây). Dương = máy chạy chậm, âm = máy chạy nhanh.</summary>
        public long DeviceOffset { get; private set; }

        /// <summary>Đã đồng bộ và giờ máy lệch quá SuspiciousOffset.</summary>
        public bool IsDeviceClockOff => _synced && (DeviceOffset > SuspiciousOffset || DeviceOffset < -SuspiciousOffset);

        public TrustedClockModel(long lastTrusted)
        {
            LastTrusted = lastTrusted;
        }

        /// <summary>Nhận giờ server <paramref name="serverUtc"/> tại thời điểm đơn điệu <paramref name="mono"/> (giây).</summary>
        public void ApplyServerTime(long serverUtc, long deviceUtc, double mono)
        {
            _synced = true;
            _serverAtSync = serverUtc;
            _monoAtSync = mono;
            DeviceOffset = serverUtc - deviceUtc;
            LastTrusted = serverUtc;
        }

        /// <summary>Giờ tin cậy hiện tại.</summary>
        public long Now(long deviceUtc, double mono)
        {
            long now;
            if (_synced)
            {
                double elapsed = mono - _monoAtSync;
                now = _serverAtSync + (long)(elapsed > 0 ? elapsed : 0);
            }
            else
            {
                now = deviceUtc > LastTrusted ? deviceUtc : LastTrusted; // chặn lùi giờ khi offline
            }
            if (now > LastTrusted || _synced) LastTrusted = now;
            return now;
        }
    }
}
