using System;

public enum DailySlotState
{
    Claimed, // đã nhận
    Today,   // hôm nay, chưa nhận
    Missed,  // ngày đã qua trong chu kỳ mà chưa nhận
    Future   // chưa tới
}

/// <summary>
/// Luật điểm danh (class C# thuần): chu kỳ 7 ngày theo lịch, bắt đầu từ ngày đầu tiên mở game (cycleStart).
/// Ô thứ i ứng với ngày cycleStart + i; lỡ ngày thì ô đó thành Missed (không đánh dấu), vẫn đi tiếp ngày sau.
/// Hết 7 ngày (hoặc quay lại sau hơn 7 ngày) thì sang chu kỳ mới từ hôm nay. Đồng hồ máy lùi lại: coi như vẫn ngày đầu chu kỳ.
/// </summary>
public class DailyRewardModel
{
    private readonly DailySave _save;
    private readonly int _cycleDays;

    public DailyRewardModel(DailySave save, int cycleDays)
    {
        _save = save;
        _cycleDays = cycleDays;
    }

    /// <summary>Ô của hôm nay (0..cycleDays-1).</summary>
    public int TodayIndex { get; private set; }

    /// <summary>Cập nhật theo ngày <paramref name="today"/> (số ngày từ 1/1/1970 theo lịch máy). Trả về true nếu dữ liệu đổi (sang chu kỳ mới).</summary>
    public bool Refresh(long today)
    {
        bool changed = false;
        if (_save.cycleStart < 0 || today - _save.cycleStart >= _cycleDays)
        {
            _save.cycleStart = today;
            _save.claimedMask = 0;
            changed = true;
        }
        TodayIndex = (int)Math.Max(0, Math.Min(_cycleDays - 1, today - _save.cycleStart));
        return changed;
    }

    public bool IsClaimed(int day) => (_save.claimedMask & (1 << day)) != 0;

    public DailySlotState StateOf(int day)
    {
        if (IsClaimed(day)) return DailySlotState.Claimed;
        if (day == TodayIndex) return DailySlotState.Today;
        return day < TodayIndex ? DailySlotState.Missed : DailySlotState.Future;
    }

    public bool CanClaimToday => !IsClaimed(TodayIndex);

    /// <summary>Đánh dấu đã nhận (chỉ ô hôm nay hoặc ô đã lỡ).</summary>
    public bool MarkClaimed(int day)
    {
        DailySlotState s = StateOf(day);
        if (s != DailySlotState.Today && s != DailySlotState.Missed) return false;
        _save.claimedMask |= 1 << day;
        return true;
    }

    public static long DayNumber(DateTime local) => (long)(local.Date - new DateTime(1970, 1, 1)).TotalDays;
}
