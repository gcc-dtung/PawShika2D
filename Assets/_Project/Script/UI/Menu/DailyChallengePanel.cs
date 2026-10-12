using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab Daily Challenge trên Main Menu (DailyChallengeUI bản gốc), đọc / ghi qua DailyChallengeManager:
/// - Đầu trang: tên tháng + nút tháng trước / sau (từ tháng mở khóa tới tháng này), cúp.
/// - Thanh tiến trình số ngày thắng trong tháng (đầy ở 21), 3 hộp quà ở mốc 7 / 14 / 21: đủ thì bấm nhận, chưa đủ / đã nhận
///   thì bấm xem quà (DailyGiftBubble).
/// - Lịch 7 cột (Chủ nhật đầu tuần): đã thắng (khung cam) / lỡ (tím đậm) / hôm nay (xanh) / chưa tới (chữ mờ).
///   Bấm hôm nay = chơi; bấm ngày lỡ = xem quảng cáo rồi chơi bù.
/// - Nút Play xanh: hôm nay chưa thắng. Hết lượt hôm nay mà còn ngày lỡ: nút Play vàng (quảng cáo) chơi bù ngày lỡ gần nhất.
/// - Chưa tới level mở khóa: lớp khóa phủ lịch, ẩn nút.
/// - Vừa thắng 1 ngày rồi về tab: thanh tiến trình chạy lên + chữ "+1" bay, ô ngày nảy.
/// Dựng bởi Tools/PawBlock/Builders/Build Daily Challenge.
/// </summary>
public class DailyChallengePanel : MonoBehaviour
{
    [Header("Đầu trang")]
    [SerializeField] private TMP_Text monthLabel;
    [SerializeField] private Button prevMonthButton;
    [SerializeField] private Button nextMonthButton;

    [Header("Tiến trình + quà")]
    [SerializeField] private RectTransform progressArea;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text plusOneText;
    [SerializeField] private DailyGiftSlot[] gifts;
    [SerializeField] private DailyGiftBubble giftBubble;
    [Tooltip("Icon coin / năng lượng vô hạn cho bong thoại quà (dùng chung với điểm danh)")]
    [SerializeField] private DailyRewardDatabase rewardIcons;

    [Header("Lịch")]
    [SerializeField] private RectTransform grid;
    [SerializeField] private DailyDayCell cellTemplate;
    [SerializeField] private Sprite frameDone;
    [SerializeField] private Sprite frameMissed;
    [SerializeField] private Sprite frameToday;
    [SerializeField] private Color colorDone = Color.white;
    [SerializeField] private Color colorMissed = new Color(0.62f, 0.62f, 0.78f);
    [SerializeField] private Color colorToday = Color.white;
    [SerializeField] private Color colorUnavailable = new Color(1f, 1f, 1f, 0.4f);

    [Header("Nút")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button playAdsButton;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedText;

    private const int GridCells = 42; // 6 tuần x 7 ngày
    private readonly List<DailyDayCell> _cells = new List<DailyDayCell>();
    private DateTime _month; // ngày 1 của tháng đang xem
    private bool _waitingAd;
    private Sequence _celebrate;

    private static DailyChallengeManager Manager => DailyChallengeManager.Instance;

    private void Awake()
    {
        if (prevMonthButton != null) prevMonthButton.onClick.AddListener(() => ChangeMonth(-1));
        if (nextMonthButton != null) nextMonthButton.onClick.AddListener(() => ChangeMonth(1));
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (playAdsButton != null) playAdsButton.onClick.AddListener(OnPlayMissed);
        if (gifts != null)
        {
            foreach (DailyGiftSlot g in gifts) if (g != null) g.Clicked += OnGiftClicked;
        }
        if (cellTemplate != null) cellTemplate.gameObject.SetActive(false);
        if (plusOneText != null) plusOneText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        DailyChallengeManager m = Manager;
        if (m == null) return;
        m.Changed += Refresh;
        m.EnsureStarted();
        _month = MonthOf(m.Today);
        Refresh();
        Celebrate();
    }

    private void OnDisable()
    {
        if (Manager != null) Manager.Changed -= Refresh;
        _celebrate?.Kill();
        if (giftBubble != null) giftBubble.Hide();
    }

    // ---------- Vẽ ----------

    private void Refresh()
    {
        DailyChallengeManager m = Manager;
        if (m == null) return;
        if (monthLabel != null) monthLabel.SetText(_month.ToString("MMMM yyyy", CultureInfo.InvariantCulture));
        RefreshNav(m);
        RefreshCalendar(m);
        SetProgress(m.Progress(_month.Year, _month.Month));
        RefreshGifts(m);
        RefreshButtons(m);
    }

    private void RefreshNav(DailyChallengeManager m)
    {
        DateTime first = MonthOf(m.Today);
        if (m.IsUnlocked && SaveSystem.Data.challenge.startDay >= 0) first = MonthOf(SaveSystem.Data.challenge.startDay);
        if (prevMonthButton != null) prevMonthButton.interactable = _month > first;
        if (nextMonthButton != null) nextMonthButton.interactable = _month < MonthOf(m.Today);
    }

    private void RefreshCalendar(DailyChallengeManager m)
    {
        if (grid == null || cellTemplate == null) return;
        while (_cells.Count < GridCells)
        {
            DailyDayCell cell = Instantiate(cellTemplate, grid);
            cell.gameObject.SetActive(true);
            cell.Clicked += OnDayClicked;
            _cells.Add(cell);
        }

        int offset = (int)_month.DayOfWeek; // Chủ nhật = 0, lịch bắt đầu Chủ nhật như gốc (Su Mo Tu...)
        int days = DateTime.DaysInMonth(_month.Year, _month.Month);
        long firstDay = DailyChallengeModel.DayNumber(_month);
        for (int i = 0; i < _cells.Count; i++)
        {
            DailyDayCell cell = _cells[i];
            int dayOfMonth = i - offset + 1;
            bool inMonth = dayOfMonth >= 1 && dayOfMonth <= days;
            cell.gameObject.SetActive(i < offset + days || i < 35); // đủ 5 hàng cho gọn; tháng cần 6 hàng thì hiện hàng 6
            if (!inMonth)
            {
                cell.SetEmpty();
                continue;
            }
            long day = firstDay + dayOfMonth - 1;
            DailyDayState state = m.IsUnlocked ? m.StateOf(day) : DailyDayState.Unavailable;
            cell.Setup(day, dayOfMonth, state, FrameOf(state), ColorOf(state), CanTap(m, state));
        }
    }

    private bool CanTap(DailyChallengeManager m, DailyDayState state) =>
        state == DailyDayState.Today || (state == DailyDayState.Missed && m.Database != null && m.Database.replayMissedWithAd);

    private Sprite FrameOf(DailyDayState s) => s switch
    {
        DailyDayState.Done => frameDone,
        DailyDayState.Missed => frameMissed,
        DailyDayState.Today => frameToday,
        _ => null
    };

    private Color ColorOf(DailyDayState s) => s switch
    {
        DailyDayState.Done => colorDone,
        DailyDayState.Missed => colorMissed,
        DailyDayState.Today => colorToday,
        _ => colorUnavailable
    };

    private void SetProgress(float ratio)
    {
        if (progressFill == null || progressArea == null) return;
        RectTransform rt = progressFill.rectTransform;
        rt.sizeDelta = new Vector2(Mathf.Clamp01(ratio) * progressArea.rect.width, rt.sizeDelta.y);
        progressFill.enabled = ratio > 0f;
    }

    private void RefreshGifts(DailyChallengeManager m)
    {
        if (gifts == null) return;
        foreach (DailyGiftSlot g in gifts)
        {
            if (g != null) g.SetState(m.IsUnlocked ? m.GiftState(_month.Year, _month.Month, g.Milestone) : DailyGiftState.Locked);
        }
    }

    private void RefreshButtons(DailyChallengeManager m)
    {
        bool unlocked = m.IsUnlocked;
        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
        if (lockedText != null && m.Database != null) lockedText.SetText("Reach Level {0} to unlock", m.Database.unlockLevel);

        bool today = unlocked && m.IsTodayPlayable;
        bool missed = unlocked && !today && m.Database != null && m.Database.replayMissedWithAd && m.TryGetNearestMissed(out _);
        if (playButton != null)
        {
            playButton.gameObject.SetActive(unlocked && !missed);
            playButton.interactable = today && !_waitingAd; // đã thắng hôm nay, không còn ngày lỡ: nút xám
        }
        if (playAdsButton != null)
        {
            playAdsButton.gameObject.SetActive(missed);
            playAdsButton.interactable = !_waitingAd;
        }
    }

    // ---------- Thao tác ----------

    private void ChangeMonth(int delta)
    {
        _month = _month.AddMonths(delta);
        if (giftBubble != null) giftBubble.Hide();
        Refresh();
    }

    private void OnPlay()
    {
        DailyChallengeManager m = Manager;
        if (m != null && m.IsTodayPlayable) Play(m.Today);
    }

    private void OnPlayMissed()
    {
        DailyChallengeManager m = Manager;
        if (m != null && m.TryGetNearestMissed(out long day)) PlayMissed(day);
    }

    private void OnDayClicked(DailyDayCell cell)
    {
        if (cell.State == DailyDayState.Today) Play(cell.Day);
        else if (cell.State == DailyDayState.Missed) PlayMissed(cell.Day);
    }

    private void Play(long day)
    {
        DailyChallengeManager m = Manager;
        if (m == null || !m.CanPlay(day) || ScreenFlow.Instance == null) return;
        ScreenFlow.Instance.PlayDaily(() => m.Load(day));
    }

    /// <summary>Chơi bù ngày đã lỡ: xem quảng cáo có thưởng trước (như PlayWithAdsButton gốc).</summary>
    private void PlayMissed(long day)
    {
        DailyChallengeManager m = Manager;
        if (m == null || _waitingAd || m.StateOf(day) != DailyDayState.Missed) return;
        _waitingAd = true;
        RefreshButtons(m);
        RewardedAds.Show("daily_missed", ok =>
        {
            _waitingAd = false;
            if (this == null) return;
            RefreshButtons(m);
            if (ok) Play(day);
        });
    }

    private void OnGiftClicked(DailyGiftSlot slot)
    {
        DailyChallengeManager m = Manager;
        if (m == null || !m.IsUnlocked) return;
        DailyChallengeGift gift = m.Database != null ? m.Database.GetGift(slot.Milestone) : null;
        IList<DailyRewardItem> items = gift != null ? gift.items : null;

        if (m.GiftState(_month.Year, _month.Month, slot.Milestone) == DailyGiftState.Ready
            && m.ClaimGift(_month.Year, _month.Month, slot.Milestone))
        {
            slot.Pop();
            AudioManager.Play(SoundId.Reward);
            AudioManager.Vibrate(HapticType.Success);
            if (giftBubble != null) giftBubble.Show(slot.Rect, "Claimed!", items, rewardIcons);
            return;
        }
        if (giftBubble != null) giftBubble.Show(slot.Rect, $"Win {slot.Milestone} days", items, rewardIcons);
    }

    // ---------- Vừa thắng 1 ngày ----------

    /// <summary>Về tab sau khi thắng Daily: thanh chạy từ số ngày cũ lên, "+1" bay lên rồi mờ, ô ngày nảy, mốc vừa đủ thì hộp sáng.</summary>
    private void Celebrate()
    {
        DailyChallengeManager m = Manager;
        long day = m != null ? m.PendingCelebrateDay : -1;
        if (day < 0) return;
        m.ConsumeCelebration();
        DateTime date = DailyChallengeModel.DateOf(day);
        _month = MonthOf(day);
        Refresh();

        int done = m.DoneInMonth(date.Year, date.Month);
        float from = Mathf.Clamp01((done - 1) / (float)DailyChallengeModel.MaxMilestone);
        float to = Mathf.Clamp01(done / (float)DailyChallengeModel.MaxMilestone);
        SetProgress(from);

        _celebrate?.Kill();
        _celebrate = DOTween.Sequence().SetLink(gameObject)
            .AppendInterval(0.35f)
            .Append(DOVirtual.Float(from, to, 0.5f, SetProgress).SetEase(Ease.OutCubic))
            .InsertCallback(0.35f, () =>
            {
                PlusOne(to);
                foreach (DailyDayCell c in _cells) if (c.Day == day) c.Pop();
                AudioManager.Play(SoundId.StarEarn);
            });
    }

    private void PlusOne(float ratio)
    {
        if (plusOneText == null || progressArea == null) return;
        RectTransform rt = plusOneText.rectTransform;
        plusOneText.gameObject.SetActive(true);
        rt.DOKill();
        plusOneText.DOKill();
        rt.anchoredPosition = new Vector2(ratio * progressArea.rect.width, 0f);
        rt.localScale = Vector3.one * 0.5f;
        plusOneText.alpha = 1f;
        DOTween.Sequence().SetLink(plusOneText.gameObject)
            .Append(rt.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack))
            .Join(rt.DOAnchorPosY(70f, 0.9f).SetEase(Ease.OutCubic))
            .Insert(0.55f, plusOneText.DOFade(0f, 0.35f))
            .OnComplete(() => plusOneText.gameObject.SetActive(false));
    }

    private static DateTime MonthOf(long day) => MonthOf(DailyChallengeModel.DateOf(day));
    private static DateTime MonthOf(DateTime d) => new DateTime(d.Year, d.Month, 1);
}
