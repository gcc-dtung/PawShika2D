using System;
using UnityEngine;
using UnityEngine.Events;
using PawBlock.Core.Foundation;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.DailyReward
{
    /// <summary>
    /// Điểm danh hằng ngày: giữ DailyRewardModel (dữ liệu trong section save "dailyReward"), trao quà, báo đổi trạng thái.
    /// - Claim(day): ô hôm nay nhận luôn; ô đã lỡ (nếu database.recoverMissedWithAd) xem quảng cáo rồi nhận.
    /// - Sang ngày mới (qua nửa đêm khi đang mở game, quay lại app) thì tự cập nhật.
    /// - autoOpen: mở game lần đầu trong ngày mà chưa nhận thì tự mở popup (IMenuPopups) khi đang ở Main Menu.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour, IRewardGranter, IRewardIcons
    {
        private IMenuPopups _popups;
        private IBoosterCatalog _boosterIcons;
        private IScreenNavigator _screens;
        private ISaveService _save;
        private DailyRewardSave _data;
        private ICurrencyWallet _coins;
        private IBoosterInventory _boosters;
        private IEnergyService _energy;
        private IClock _clock;
        private IAdsService _ads;

        /// <summary>
        /// GameBootstrap gọi trước Start: section "dailyReward", nơi nhận quà (ví coin, kho booster, năng lượng – đều qua contract).
        /// </summary>
        public void Init(ISaveService save, ICurrencyWallet coins, IBoosterInventory boosters, IEnergyService energy, IClock clock, IAdsService ads, IScreenNavigator screens, IBoosterCatalog boosterIcons, IMenuPopups popups)
        {
            _clock = clock;
            _ads = ads;
            _save = save;
            _data = save.Get<DailyRewardSave>(DailyRewardSave.Key);
            _coins = coins;
            _boosters = boosters;
            _energy = energy;
            _model = null;
            _screens = screens;
            _boosterIcons = boosterIcons;
            _popups = popups;
        }

        [SerializeField] private DailyRewardDatabase database;
        [SerializeField] private bool autoOpen = true;
        [SerializeField] private float autoOpenDelay = 0.5f;

        [Header("Events")]
        public UnityEvent onChanged = new UnityEvent();
        public UnityEvent<int> onClaimed = new UnityEvent<int>(); // ô vừa nhận

        private DailyRewardModel _model;
        private long _today = -1;
        private float _checkTimer;
        private bool _waitingAd;

        public DailyRewardDatabase Database => database;
        private DateTime LocalNow => _clock != null ? _clock.LocalNow : DateTime.Now;
        public int TodayIndex => Model.TodayIndex;
        public bool CanClaimToday => Model.CanClaimToday;
        public bool IsBusy => _waitingAd;

        private DailyRewardModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = new DailyRewardModel(_data ?? new DailyRewardSave(), DailyRewardDatabase.CycleDays);
                    RefreshDay(false);
                }
                return _model;
            }
        }

        private void Start()
        {
            if (autoOpen && CanClaimToday) Invoke(nameof(AutoOpen), autoOpenDelay);
        }

        private void Update()
        {
            // Qua nửa đêm khi đang mở game
            _checkTimer += Time.unscaledDeltaTime;
            if (_checkTimer < 5f) return;
            _checkTimer = 0f;
            RefreshDay(true);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && _model != null) RefreshDay(true);
        }

        private void OnEnable()
        {
            if (_clock != null) _clock.Synced += OnClockSynced;
        }

        private void OnDisable()
        {
            if (_clock != null) _clock.Synced -= OnClockSynced;
        }

        // Vừa có giờ server: ngày có thể khác ngày theo giờ máy
        private void OnClockSynced()
        {
            if (_model != null) RefreshDay(true);
        }

        public DailySlotState StateOf(int day) => Model.StateOf(day);

        /// <summary>Ô đã lỡ có bù được không (bằng quảng cáo).</summary>
        public bool CanRecover(int day) => database != null && database.recoverMissedWithAd && Model.StateOf(day) == DailySlotState.Missed;

        /// <summary>Thời gian tới ngày mới (đồng hồ "quay lại sau").</summary>
        public TimeSpan TimeToTomorrow
        {
            get
            {
                DateTime now = LocalNow;
                return now.Date.AddDays(1) - now;
            }
        }

        /// <summary>Nhận quà ô <paramref name="day"/>. onDone(true) khi đã trao quà.</summary>
        public void Claim(int day, Action<bool> onDone = null)
        {
            RefreshDay(true);
            DailySlotState state = Model.StateOf(day);
            if (state == DailySlotState.Today)
            {
                Grant(day);
                onDone?.Invoke(true);
                return;
            }
            // Đang có quảng cáo khác: IAdsService.Show sẽ bỏ qua và không gọi lại -> đừng chờ
            bool adBusy = _ads == null || _ads.IsShowing;
            if (CanRecover(day) && !_waitingAd && !adBusy)
            {
                _waitingAd = true;
                onChanged?.Invoke();
                _ads.Show("daily_recover", ok =>
                {
                    _waitingAd = false;
                    if (ok && Model.StateOf(day) == DailySlotState.Missed) Grant(day);
                    else onChanged?.Invoke();
                    onDone?.Invoke(ok);
                });
                return;
            }
            onDone?.Invoke(false);
        }

        private void Grant(int day)
        {
            if (!Model.MarkClaimed(day)) return;
            _save.MarkDirty();

            DailyRewardDay reward = database != null ? database.Get(day) : null;
            if (reward != null)
            {
                foreach (DailyRewardItem item in reward.items) GrantItem(item);
            }
            Debug.Log($"[DailyReward] Nhận quà ngày {day + 1}");
            onClaimed?.Invoke(day);
            onChanged?.Invoke();
        }

        void IRewardGranter.Grant(DailyRewardItem item) => GrantItem(item);

        /// <summary>Icon 1 món quà: coin / năng lượng vô hạn từ database, booster từ Gameplay (IBoosterCatalog).</summary>
        public Sprite IconOf(DailyRewardItem item)
        {
            switch (item.kind)
            {
                case DailyRewardKind.Coin: return database != null ? database.coinIcon : null;
                case DailyRewardKind.InfiniteEnergy: return database != null ? database.infiniteEnergyIcon : null;
                default: return _boosterIcons != null ? _boosterIcons.ItemIcon(item.booster) : null;
            }
        }

        /// <summary>Trao 1 món quà (coin / booster / năng lượng vô hạn). Daily Challenge, Vòng quay dùng qua IRewardGranter.</summary>
        public void GrantItem(DailyRewardItem item)
        {
            switch (item.kind)
            {
                case DailyRewardKind.Coin:
                    _coins?.Add(item.amount);
                    break;
                case DailyRewardKind.Booster:
                    _boosters?.Add(item.booster, item.amount);
                    break;
                case DailyRewardKind.InfiniteEnergy:
                    _energy?.AddInfinite(item.amount * 60);
                    break;
            }
        }

        private void RefreshDay(bool notify)
        {
            long today = DailyRewardModel.DayNumber(LocalNow); // giờ server nếu có mạng, chặn chỉnh giờ máy
            bool newDay = today != _today;
            _today = today;
            if (_model == null) return;
            if (_model.Refresh(today)) _save.MarkDirty();
            if (newDay && notify) onChanged?.Invoke();
        }

        private void AutoOpen()
        {
            if (!CanClaimToday || _screens == null || !_screens.IsMenu) return;
            _popups?.OpenDailyReward();
        }

        [ContextMenu("Debug/Next Day")]
        public void DebugNextDay()
        {
            _data.cycleStart -= 1;
            _today = -1;
            RefreshDay(true);
            onChanged?.Invoke();
        }

        [ContextMenu("Debug/Reset")]
        public void DebugReset()
        {
            _data.cycleStart = -1;
            _data.claimedMask = 0;
            _today = -1;
            RefreshDay(true);
            onChanged?.Invoke();
        }
    }
}
