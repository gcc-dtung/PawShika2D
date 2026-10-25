using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Đồng hồ chống chỉnh giờ máy (năng lượng, điểm danh, Daily). Cài đặt: TrustedClockService.</summary>
    public interface IClock
    {
        /// <summary>Giờ tin cậy, unix giây (UTC). Thay DateTimeOffset.UtcNow.</summary>
        long UtcNowSeconds { get; }

        /// <summary>Giờ tin cậy theo múi giờ máy (dùng cho "sang ngày mới"). Thay DateTime.Now.</summary>
        DateTime LocalNow { get; }

        /// <summary>Vừa lấy được giờ server (giờ tin cậy có thể nhảy): hệ thống theo thời gian nên tính lại.</summary>
        event Action Synced;
    }
}
