using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 ô quà điểm danh: "Day n", tối đa itemIcons.Length món quà (icon + số), và trạng thái:
/// Hôm nay = viền vàng nhấp nháy, bấm để nhận; Đã nhận = mờ + dấu tick; Đã lỡ = xám (+ icon video nếu bù được);
/// Chưa tới = bình thường.
/// </summary>
public class DailyRewardSlot : MonoBehaviour
{
    [SerializeField] private int day;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private GameObject[] itemRoots;
    [SerializeField] private Image[] itemIcons;
    [SerializeField] private TMP_Text[] itemTexts;
    [SerializeField] private Image background;
    [SerializeField] private RectTransform highlight; // viền vàng ô hôm nay
    [SerializeField] private RectTransform tick;
    [SerializeField] private GameObject recover;      // icon video: bù ngày lỡ
    [SerializeField] private CanvasGroup content;
    [SerializeField] private Button button;

    [Header("Màu")]
    [SerializeField] private Color missedTint = new Color(0.6f, 0.6f, 0.65f, 1f);
    [SerializeField] private float claimedAlpha = 0.55f;

    private DailySlotState _state;
    private Color _bgColor = Color.white;
    private Tween _pulse;
    private Tween _pop;

    public int Day => day;
    public Button Button => button;

    private void Awake()
    {
        if (background != null) _bgColor = background.color; // ngày 7 có màu nền riêng
    }

    private void OnDisable()
    {
        _pulse?.Kill();
        _pulse = null;
        _pop?.Kill();
        _pop = null;
        transform.localScale = Vector3.one;
    }

    /// <summary>Hiện quà của ngày (icon lấy từ database / BoosterManager).</summary>
    public void Setup(DailyRewardDay reward, DailyRewardDatabase db)
    {
        if (dayText != null) dayText.SetText("Day {0}", day + 1);
        int n = reward != null ? reward.items.Count : 0;
        for (int i = 0; i < itemRoots.Length; i++)
        {
            bool show = i < n;
            itemRoots[i].SetActive(show);
            if (!show) continue;
            DailyRewardItem item = reward.items[i];
            if (itemIcons[i] != null) itemIcons[i].sprite = IconOf(item, db);
            if (itemTexts[i] != null) itemTexts[i].SetText(TextOf(item));
        }
    }

    public void SetState(DailySlotState state, bool canRecover)
    {
        _state = state;
        bool today = state == DailySlotState.Today;
        bool claimed = state == DailySlotState.Claimed;
        bool missed = state == DailySlotState.Missed;

        if (highlight != null) highlight.gameObject.SetActive(today);
        if (tick != null) tick.gameObject.SetActive(claimed);
        if (recover != null) recover.SetActive(missed && canRecover);
        if (content != null) content.alpha = claimed ? claimedAlpha : 1f;
        if (background != null) background.color = missed ? _bgColor * missedTint : _bgColor;
        if (button != null) button.interactable = today || (missed && canRecover);

        _pulse?.Kill();
        _pulse = null;
        if (today && highlight != null && isActiveAndEnabled)
        {
            highlight.localScale = Vector3.one;
            _pulse = highlight.DOScale(1.04f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(gameObject);
        }
    }

    /// <summary>Vừa nhận: ô nảy lên, dấu tick bật ra.</summary>
    public void PlayClaimed()
    {
        _pop?.Kill();
        transform.localScale = Vector3.one;
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.Append(transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f));
        if (tick != null)
        {
            tick.localScale = Vector3.zero;
            seq.Join(tick.DOScale(1f, 0.3f).SetEase(Ease.OutBack, 2.5f));
        }
        seq.OnKill(() => { if (this != null) transform.localScale = Vector3.one; });
        _pop = seq;
    }

    private static Sprite IconOf(DailyRewardItem item, DailyRewardDatabase db)
    {
        switch (item.kind)
        {
            case DailyRewardKind.Coin: return db != null ? db.coinIcon : null;
            case DailyRewardKind.InfiniteEnergy: return db != null ? db.infiniteEnergyIcon : null;
            default:
                BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(item.booster) : null;
                return data != null ? data.ItemIcon : null;
        }
    }

    private static string TextOf(DailyRewardItem item)
    {
        switch (item.kind)
        {
            case DailyRewardKind.Coin: return item.amount.ToString();
            case DailyRewardKind.InfiniteEnergy: return item.amount + "m";
            default: return "x" + item.amount;
        }
    }
}
