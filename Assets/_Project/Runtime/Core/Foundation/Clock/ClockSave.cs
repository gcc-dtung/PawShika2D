using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Section "clock" của save v2 (TrustedClockService).</summary>
    [Serializable]
    public class ClockSave
    {
        public const string Key = "clock";

        public long lastTrustedUtc; // mốc giờ tin cậy lớn nhất đã thấy (unix giây), TrustedClock chặn lùi giờ dưới mốc này
    }
}
