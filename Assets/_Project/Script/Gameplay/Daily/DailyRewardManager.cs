using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Điểm danh hằng ngày: giữ DailyRewardModel (dữ liệu trong SaveData.daily), trao quà, báo đổi trạng thái.
/// - Claim(day): ô hôm nay nhận luôn; ô đã lỡ (nếu database.recoverMissedWithAd) xem quảng cáo rồi nhận.
/// - Sang ngày mới (qua nửa đêm khi đang mở game, quay lại app) thì tự cập nhật.
/// - autoOpen: mở game lần đầu trong ngày mà chưa nhận thì tự mở popup (GameFlowUI) khi đang ở Main Menu.
/// </summary>
public class DailyRewardManager : Singleton<DailyRewardManager>
{
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
    public int TodayIndex => Model.TodayIndex;
    public bool CanClaimToday => Model.CanClaimToday;
    public bool IsBusy => _waitingAd;

    private DailyRewardModel Model
    {
        get
        {
            if (_model == null)
            {
                _model = new DailyRewardModel(SaveSystem.Data.daily, DailyRewardDatabase.CycleDays);
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

    private void OnEnable() => TrustedClock.Synced += OnClockSynced;
    private void OnDisable() => TrustedClock.Synced -= OnClockSynced;

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
            DateTime now = TrustedClock.LocalNow;
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
        // Đang có quảng cáo khác: RewardedAds.Show sẽ bỏ qua và không gọi lại -> đừng chờ
        bool adBusy = RewardedAds.Instance != null && RewardedAds.Instance.IsShowing;
        if (CanRecover(day) && !_waitingAd && !adBusy)
        {
            _waitingAd = true;
            onChanged?.Invoke();
            RewardedAds.Show("daily_recover", ok =>
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
        SaveSystem.MarkDirty();

        DailyRewardDay reward = database != null ? database.Get(day) : null;
        if (reward != null)
        {
            foreach (DailyRewardItem item in reward.items) GrantItem(item);
        }
        Debug.Log($"[DailyReward] Nhận quà ngày {day + 1}");
        onClaimed?.Invoke(day);
        onChanged?.Invoke();
    }

    /// <summary>Trao 1 món quà (coin / booster / năng lượng vô hạn). Daily Challenge dùng chung.</summary>
    public static void GrantItem(DailyRewardItem item)
    {
        switch (item.kind)
        {
            case DailyRewardKind.Coin:
                CoinWallet.Add(item.amount);
                break;
            case DailyRewardKind.Booster:
                BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(item.booster) : null;
                if (data != null) BoosterInventory.Add(data, item.amount);
                break;
            case DailyRewardKind.InfiniteEnergy:
                if (EnergyManager.Instance != null) EnergyManager.Instance.AddInfinite(item.amount * 60);
                break;
        }
    }

    private void RefreshDay(bool notify)
    {
        long today = DailyRewardModel.DayNumber(TrustedClock.LocalNow); // giờ server nếu có mạng, chặn chỉnh giờ máy
        bool newDay = today != _today;
        _today = today;
        if (_model == null) return;
        if (_model.Refresh(today)) SaveSystem.MarkDirty();
        if (newDay && notify) onChanged?.Invoke();
    }

    private void AutoOpen()
    {
        if (!CanClaimToday || ScreenFlow.Instance == null || !ScreenFlow.Instance.IsMenu) return;
        GameFlowUI flow = FindFirstObjectByType<GameFlowUI>();
        if (flow != null) flow.OpenDailyReward();
    }

    [ContextMenu("Debug/Next Day")]
    public void DebugNextDay()
    {
        SaveSystem.Data.daily.cycleStart -= 1;
        _today = -1;
        RefreshDay(true);
        onChanged?.Invoke();
    }

    [ContextMenu("Debug/Reset")]
    public void DebugReset()
    {
        SaveSystem.Data.daily.cycleStart = -1;
        SaveSystem.Data.daily.claimedMask = 0;
        _today = -1;
        RefreshDay(true);
        onChanged?.Invoke();
    }
}
