using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>
    /// Daily Challenge (tab thứ 3 của Menu, như DailyChallengeUI bản gốc): giữ DailyChallengeModel (SaveData.challenge),
    /// level mỗi ngày do Gameplay sinh và nạp (IDailyLevelLoader: seed = ngày, cùng ngày mọi máy cùng level),
    /// thắng thì đánh dấu ngày, trao quà mốc 7 / 14 / 21 ngày trong tháng.
    /// Ngày lấy theo IClock (giờ server nếu có mạng, chặn chỉnh giờ máy).
    /// Mở khóa khi tới DailyChallengeDatabase.unlockLevel; ngày mở khóa là ngày bắt đầu (ngày trước đó không tính lỡ).
    /// </summary>
    public class DailyChallengeManager : MonoBehaviour
    {
        private IDailyLevelLoader _levelLoader;
        private ISaveService _save;
        private DailyChallengeSave _data;
        private IRewardGranter _rewards;
        private Func<int> _reachedLevel;
        private IClock _clock;

        /// <summary>
        /// GameBootstrap gọi trước Start: section "dailyChallenge", nơi trao quà mốc (DailyReward, qua contract),
        /// <paramref name="reachedLevel"/> = số level thường đang tới (bắt đầu từ 1; đã qua hết = int.MaxValue) để mở khóa tab.
        /// </summary>
        public void Init(ISaveService save, IRewardGranter rewards, Func<int> reachedLevel, IClock clock, IDailyLevelLoader levelLoader)
        {
            _clock = clock;
            _save = save;
            _data = save.Get<DailyChallengeSave>(DailyChallengeSave.Key);
            _rewards = rewards;
            _reachedLevel = reachedLevel;
            _model = null;
            _levelLoader = levelLoader;
        }

        private DateTime LocalNow => _clock != null ? _clock.LocalNow : DateTime.Now;

        /// <summary>Ngày mở khóa Daily (-1 = chưa mở).</summary>
        public long StartDay => _data != null ? _data.startDay : -1;

        [SerializeField] private DailyChallengeDatabase database;

        /// <summary>Dữ liệu đổi (sang ngày mới, thắng 1 ngày, nhận quà, vừa mở khóa): tab Daily vẽ lại.</summary>
        public event Action Changed;

        private DailyChallengeModel _model;
        private long _today = -1;
        private float _checkTimer;

        public DailyChallengeDatabase Database => database;

        /// <summary>Hôm nay theo lịch (số ngày từ 1/1/1970).</summary>
        public long Today
        {
            get
            {
                if (_today < 0) _today = DailyChallengeModel.DayNumber(LocalNow);
                return _today;
            }
        }

        public bool IsUnlocked
        {
            get
            {
                if (database == null || _reachedLevel == null) return false;
                return _reachedLevel() >= database.unlockLevel;
            }
        }

        /// <summary>Ngày vừa thắng mà tab Daily chưa diễn "+1" trên thanh tiến trình (-1 = không có).</summary>
        public long PendingCelebrateDay { get; private set; } = -1;

        private DailyChallengeModel Model
        {
            get
            {
                if (_model == null) _model = new DailyChallengeModel(_data ?? new DailyChallengeSave());
                return _model;
            }
        }

        private void OnEnable()
        {
            if (_clock != null) _clock.Synced += OnClockSynced;
            EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);
        }

        private void OnDisable()
        {
            if (_clock != null) _clock.Synced -= OnClockSynced;
            EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
        }

        private void Update()
        {
            // Qua nửa đêm khi đang mở game
            _checkTimer += Time.unscaledDeltaTime;
            if (_checkTimer < 5f) return;
            _checkTimer = 0f;
            RefreshDay();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus) RefreshDay();
        }

        private void OnClockSynced() => RefreshDay(); // vừa có giờ server: ngày có thể khác ngày theo giờ máy

        private void RefreshDay()
        {
            long today = DailyChallengeModel.DayNumber(LocalNow);
            if (today == _today) return;
            _today = today;
            Changed?.Invoke();
        }

        // ---------- Đọc trạng thái ----------

        public DailyDayState StateOf(long day) => Model.StateOf(day, Today);
        public int DoneInMonth(int year, int month) => Model.DoneInMonth(year, month);
        public float Progress(int year, int month) => Model.Progress(year, month);
        public DailyGiftState GiftState(int year, int month, int milestone) => Model.GiftState(year, month, milestone);
        public bool IsTodayPlayable => IsUnlocked && StateOf(Today) == DailyDayState.Today;
        public bool TryGetNearestMissed(out long day) => Model.TryGetNearestMissed(Today, out day);

        /// <summary>Lần đầu thấy tab Daily sau khi mở khóa: lấy hôm nay làm ngày bắt đầu.</summary>
        public void EnsureStarted()
        {
            if (!IsUnlocked || !Model.EnsureStart(Today)) return;
            _save.MarkDirty();
            Changed?.Invoke();
        }

        public void ConsumeCelebration() => PendingCelebrateDay = -1;

        // ---------- Chơi ----------

        /// <summary>Ngày chơi được: hôm nay chưa thắng, hoặc ngày đã lỡ.</summary>
        public bool CanPlay(long day)
        {
            if (!IsUnlocked) return false;
            DailyDayState s = StateOf(day);
            return s == DailyDayState.Today || s == DailyDayState.Missed;
        }

        /// <summary>Sinh level của ngày <paramref name="day"/> rồi nạp vào màn chơi (gọi sau khi đã vào màn chơi).</summary>
        public void Load(long day)
        {
            _levelLoader?.LoadDaily(day);
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (!e.IsDaily || !Model.MarkDone(e.DailyDay)) return;
            _save.MarkDirty();
            PendingCelebrateDay = e.DailyDay;
            Debug.Log($"[DailyChallenge] Thắng ngày {DailyChallengeModel.DateOf(e.DailyDay):yyyy-MM-dd}");
            Changed?.Invoke();
        }

        // ---------- Quà mốc ----------

        /// <summary>Nhận quà mốc của tháng (đủ ngày, chưa nhận). Trả về false nếu chưa nhận được.</summary>
        public bool ClaimGift(int year, int month, int milestone)
        {
            if (!Model.MarkGiftClaimed(year, month, milestone)) return false;
            DailyChallengeGift gift = database != null ? database.GetGift(milestone) : null;
            if (gift != null)
            {
                foreach (DailyRewardItem item in gift.items) _rewards?.Grant(item);
            }
            _save.MarkDirty();
            Debug.Log($"[DailyChallenge] Nhận quà mốc {milestone} tháng {month}/{year}");
            Changed?.Invoke();
            return true;
        }

        // ---------- Debug ----------

        /// <summary>Cheat: đánh dấu thắng ngày (không cần chơi).</summary>
        public void DebugMarkDone(long day)
        {
            if (!Model.MarkDone(day)) return;
            _save.MarkDirty();
            Changed?.Invoke();
        }

        /// <summary>Cheat: xóa tiến độ Daily (ngày mở khóa, ngày thắng, quà).</summary>
        public void DebugReset()
        {
            _data.startDay = -1;
            _data.doneDays.Clear();
            _data.claimedGifts.Clear();
            _model = null;
            _save.MarkDirty();
            Changed?.Invoke();
        }
    }
}
