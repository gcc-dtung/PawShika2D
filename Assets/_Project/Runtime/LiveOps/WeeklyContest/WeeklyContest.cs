using System;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Game
{
    /// <summary>Section save "weeklyContest": điểm tuần của người chơi.</summary>
    [Serializable]
    public class WeeklyContestSave
    {
        public int week = -1;       // số tuần (từ thứ Hai 1/1/2024, UTC) của điểm đang lưu
        public int score;           // số màn thắng trong tuần
        public long lastPlayUnix;   // lần thắng gần nhất (0 = chưa từng)
        public int debt;            // số hạng còn nợ bot sau khi bỏ chơi (mỗi màn thắng lấy lại 1)
    }

    /// <summary>
    /// Cuộc thi tuần offline: đếm màn thắng trong tuần (LevelCompletedEvent, cả Daily), sang tuần mới thì về 0.
    /// Điểm bot tính bằng <see cref="WeeklyBots"/> theo giờ hiện tại, điểm và thời gian bỏ chơi của người chơi.
    /// Bỏ chơi rồi quay lại: số bot đang đứng trên thành "nợ", mỗi màn thắng trừ 1 (leo lại từng hạng, không nhảy vọt lên top).
    /// LeaderboardPanel tạo và giữ; đăng ký event từ lúc tạo (panel có thể đang tắt) tới Dispose.
    /// </summary>
    public sealed class WeeklyContest : IDisposable
    {
        public const string SaveKey = "weeklyContest";
        private static readonly DateTime WeekZero = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // thứ Hai

        private readonly ISaveService _save;
        private readonly IClock _clock;
        private readonly WeeklyBots.Settings _settings;
        private readonly WeeklyContestSave _data;

        public WeeklyContest(ISaveService save, IClock clock, WeeklyBots.Settings settings)
        {
            _save = save;
            _clock = clock;
            _settings = settings;
            _data = save.Get<WeeklyContestSave>(SaveKey);
            EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);
        }

        public void Dispose()
        {
            EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
        }

        private DateTime Now => DateTimeOffset.FromUnixTimeSeconds(_clock.UtcNowSeconds).UtcDateTime;

        public int Week => (int)((Now - WeekZero).TotalDays / 7);

        /// <summary>Số giờ đã qua từ đầu tuần (thứ Hai 00:00 UTC).</summary>
        public double HoursIntoWeek => (Now - WeekZero.AddDays(Week * 7)).TotalHours;

        /// <summary>Thời gian còn lại tới hết tuần.</summary>
        public TimeSpan TimeLeft => WeekZero.AddDays((Week + 1) * 7) - Now;

        public int PlayerScore
        {
            get
            {
                Roll();
                return _data.score;
            }
        }

        /// <summary>Điểm bot (cùng thứ tự với <paramref name="rivals"/> tên) tại lúc gọi.</summary>
        public int[] RivalScores(int rivals)
        {
            Roll();
            return WeeklyBots.Scores(Week, rivals, HoursIntoWeek, _data.score, Above(), _settings);
        }

        private int Above()
        {
            // Chưa thắng màn nào thì tính bỏ chơi từ đầu tuần
            long weekStart = new DateTimeOffset(WeekZero.AddDays(Week * 7)).ToUnixTimeSeconds();
            long since = Math.Max(_data.lastPlayUnix, weekStart);
            double idle = (_clock.UtcNowSeconds - since) / 3600.0;
            return WeeklyBots.AboveCount(_data.debt, idle, _settings);
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            Roll();
            _data.debt = Math.Max(0, Above() - 1);
            _data.score++;
            _data.lastPlayUnix = _clock.UtcNowSeconds;
            _save.MarkDirty();
        }

        // Sang tuần mới: điểm và nợ về 0
        private void Roll()
        {
            int week = Week;
            if (_data.week == week) return;
            _data.week = week;
            _data.score = 0;
            _data.debt = 0;
            _save.MarkDirty();
        }
    }
}
