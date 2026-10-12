using System;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>Trạng thái 1 ngày trên lịch Daily Challenge (khung daily_khung_1 / 2 / 3 của bản gốc).</summary>
    public enum DailyDayState
    {
        Unavailable, // ngày tương lai, hoặc trước ngày mở khóa: không chơi được, chữ mờ
        Today,       // hôm nay, chưa thắng: chơi được (daily_khung_3)
        Done,        // đã thắng (daily_khung_1)
        Missed       // đã qua mà chưa thắng: chơi bù bằng quảng cáo (daily_khung_2)
    }

    public enum DailyGiftState
    {
        Locked,  // chưa đủ số ngày thắng trong tháng
        Ready,   // đủ, chưa nhận
        Claimed
    }

    /// <summary>
    /// Luật Daily Challenge (class C# thuần, dữ liệu trong SaveData.challenge), theo DailyChallengeUI của bản gốc:
    /// - Mỗi ngày 1 level (sinh theo seed ngày), thắng thì ngày đó Done. Ngày tính theo lịch (số ngày từ 1/1/1970).
    /// - Ngày mở khóa (startDay) trở về sau mà đã qua, chưa thắng = Missed, chơi bù được (gốc: xem quảng cáo, gần nhất trước).
    /// - Quà theo tháng: thắng đủ 7 / 14 / 21 ngày trong 1 tháng dương lịch thì nhận quà mốc đó (mỗi tháng 1 lần).
    /// </summary>
    public class DailyChallengeModel
    {
        public static readonly int[] Milestones = { 7, 14, 21 };
        public const int MaxMilestone = 21;   // thanh tiến trình đầy ở mốc cuối (progressMaxDays gốc)
        public const int MissScanMaxDays = 400; // tìm ngày lỡ gần nhất tối đa bấy nhiêu ngày về trước (gốc)

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        private readonly DailyChallengeSave _save;

        public DailyChallengeModel(DailyChallengeSave save)
        {
            _save = save ?? new DailyChallengeSave();
        }

        public static long DayNumber(DateTime local) => (long)(local.Date - Epoch).TotalDays;
        public static DateTime DateOf(long day) => Epoch.AddDays(day);

        /// <summary>Khóa quà theo tháng + mốc, vd 2026-10 mốc 14 -> 20261014.</summary>
        public static int GiftKey(int year, int month, int milestone) => (year * 100 + month) * 100 + milestone;

        public bool HasStarted => _save.startDay >= 0;
        public long StartDay => _save.startDay;

        /// <summary>Lần đầu mở khóa: ngày bắt đầu = hôm nay (ngày trước đó không tính lỡ). Trả về true nếu vừa đặt.</summary>
        public bool EnsureStart(long today)
        {
            if (HasStarted) return false;
            _save.startDay = today;
            return true;
        }

        public bool IsDone(long day) => _save.doneDays.Contains((int)day);

        /// <summary>Đánh dấu đã thắng ngày <paramref name="day"/>. Trả về false nếu đã có.</summary>
        public bool MarkDone(long day)
        {
            if (IsDone(day)) return false;
            _save.doneDays.Add((int)day);
            return true;
        }

        public DailyDayState StateOf(long day, long today)
        {
            if (day > today) return DailyDayState.Unavailable;
            if (IsDone(day)) return DailyDayState.Done;
            if (day == today) return DailyDayState.Today;
            return HasStarted && day >= StartDay ? DailyDayState.Missed : DailyDayState.Unavailable;
        }

        /// <summary>Số ngày đã thắng trong tháng (dương lịch).</summary>
        public int DoneInMonth(int year, int month)
        {
            int count = 0;
            foreach (int day in _save.doneDays)
            {
                DateTime d = DateOf(day);
                if (d.Year == year && d.Month == month) count++;
            }
            return count;
        }

        /// <summary>Ngày đã lỡ gần hôm nay nhất (không tính hôm nay), trong MissScanMaxDays ngày và không trước ngày mở khóa.</summary>
        public bool TryGetNearestMissed(long today, out long day)
        {
            day = -1;
            if (!HasStarted) return false;
            long stop = Math.Max(StartDay, today - MissScanMaxDays);
            for (long d = today - 1; d >= stop; d--)
            {
                if (IsDone(d)) continue;
                day = d;
                return true;
            }
            return false;
        }

        public DailyGiftState GiftState(int year, int month, int milestone)
        {
            if (_save.claimedGifts.Contains(GiftKey(year, month, milestone))) return DailyGiftState.Claimed;
            return DoneInMonth(year, month) >= milestone ? DailyGiftState.Ready : DailyGiftState.Locked;
        }

        /// <summary>Đánh dấu đã nhận quà mốc; chỉ được khi đang Ready.</summary>
        public bool MarkGiftClaimed(int year, int month, int milestone)
        {
            if (GiftState(year, month, milestone) != DailyGiftState.Ready) return false;
            _save.claimedGifts.Add(GiftKey(year, month, milestone));
            return true;
        }

        /// <summary>Tỉ lệ thanh tiến trình của tháng (đầy ở mốc cuối 21 ngày).</summary>
        public float Progress(int year, int month) => Math.Min(1f, DoneInMonth(year, month) / (float)MaxMilestone);
    }
}
