using System;

namespace PawBlock.Features.DailyReward
{
    /// <summary>Section "dailyReward" của save v2: điểm danh 7 ngày.</summary>
    [Serializable]
    public class DailyRewardSave
    {
        public const string Key = "dailyReward";

        public long cycleStart = -1; // ngày bắt đầu chu kỳ điểm danh (số ngày từ 1/1/1970 theo lịch máy), -1 = chưa có
        public int claimedMask;      // bit i = đã nhận quà ngày i + 1
    }
}
