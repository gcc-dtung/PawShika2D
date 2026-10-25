using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Ngày theo lịch dạng số (số ngày từ 1/1/1970), dùng chung cho Daily Challenge, LevelManager, HUD.</summary>
    public static class DayNumber
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>Số ngày của ngày (giờ địa phương) <paramref name="local"/>.</summary>
        public static long Of(DateTime local) => (long)(local.Date - Epoch).TotalDays;

        /// <summary>Ngày của số <paramref name="day"/>.</summary>
        public static DateTime ToDate(long day) => Epoch.AddDays(day);
    }
}
