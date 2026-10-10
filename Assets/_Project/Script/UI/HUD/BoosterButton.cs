using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 nút booster trên thanh dưới. Hiện: icon + số lượt; hết lượt thì dấu "+"; chưa mở khóa (BoosterTutorial) thì ổ khóa + "LV n";
/// đang bật (đang đóng băng / đang chọn số Reveal) thì quầng sáng xoay sau nút. Bấm -> BoosterManager.Press.
/// </summary>
[RequireComponent(typeof(Button))]
public class BoosterButton : MonoBehaviour
{
    [SerializeField] private BoosterType type;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject countBadge;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject addBadge;
    [SerializeField] private GameObject lockGroup;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private Image glow;
    [SerializeField] private float glowSpin = 90f; // độ / giây

    private Button _button;
    private Tween _glowTween;
    private Tween _punch;
    private bool _highlight; // tutorial mở khóa đang chỉ vào nút này

    /// <summary>BoosterTutorialView: bật quầng sáng xoay khi tutorial chỉ vào nút.</summary>
    public void SetHighlight(bool on)
    {
        _highlight = on;
        Refresh();
    }

    public BoosterType Type => type;

    /// <summary>Nút của booster <paramref name="boosterType"/> (kể cả đang ẩn), null nếu không có.</summary>
    public static BoosterButton Find(BoosterType boosterType)
    {
        foreach (BoosterButton b in FindObjectsByType<BoosterButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.Type == boosterType) return b;
        }
        return null;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        BoosterInventory.Changed += OnInventoryChanged;
        BoosterManager bm = BoosterManager.Instance;
        if (bm != null)
        {
            bm.onModeChanged.AddListener(OnModeChanged);
            bm.onBoosterUsed.AddListener(OnUsed);
            bm.onTutorialChanged.AddListener(Refresh);
        }
        EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
        EventBus<BoosterLockChangedEvent>.Subscribe(OnLockChanged);
        if (LevelTimer.Instance != null) LevelTimer.Instance.onFreezeChanged.AddListener(OnFreeze);
        Refresh();
    }

    private void OnDisable()
    {
        BoosterInventory.Changed -= OnInventoryChanged;
        BoosterManager bm = BoosterManager.Instance;
        if (bm != null)
        {
            bm.onModeChanged.RemoveListener(OnModeChanged);
            bm.onBoosterUsed.RemoveListener(OnUsed);
            bm.onTutorialChanged.RemoveListener(Refresh);
        }
        EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
        EventBus<BoosterLockChangedEvent>.Unsubscribe(OnLockChanged);
        if (LevelTimer.Instance != null) LevelTimer.Instance.onFreezeChanged.RemoveListener(OnFreeze);

        _glowTween?.Kill();
        _glowTween = null;
        _punch?.Kill();
        _punch = null;
    }

    private void OnClick()
    {
        if (BoosterManager.Instance != null) BoosterManager.Instance.Press(type);
    }

    private void OnInventoryChanged(BoosterType changed, int count)
    {
        if (changed == type) Refresh();
    }

    private void OnLevelLoaded(LevelLoadedEvent _) => Refresh();
    private void OnModeChanged(BoosterType _, bool __) => Refresh();
    private void OnLockChanged(BoosterLockChangedEvent e)
    {
        if (e.Type == type) Refresh();
    }
    private void OnFreeze(bool _) => Refresh();

    private void OnUsed(BoosterType used)
    {
        if (used != type) return;
        if (TryGetComponent(out ButtonPress press) && press.IsPlaying) return; // vừa bấm nút: đang có hiệu ứng nhấn của nút
        _punch?.Kill();
        transform.localScale = Vector3.one;
        _punch = transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.6f)
            .SetLink(gameObject)
            .OnKill(() => { if (this != null) transform.localScale = Vector3.one; });
    }

    public void Refresh()
    {
        BoosterManager bm = BoosterManager.Instance;
        BoosterData data = bm != null ? bm.Get(type) : null;
        bool unlocked = bm != null && bm.IsUnlocked(type);
        int count = bm != null ? bm.Count(type) : 0;
        bool active = bm != null && bm.IsActive(type);

        if (icon != null)
        {
            if (data != null && data.icon != null) icon.sprite = data.icon;
            icon.gameObject.SetActive(unlocked);
        }
        if (lockGroup != null) lockGroup.SetActive(!unlocked);
        if (lockText != null && data != null) lockText.SetText("LV {0}", data.levelUnlock);
        if (countBadge != null) countBadge.SetActive(unlocked && count > 0);
        if (countText != null) countText.SetText("{0}", count);
        if (addBadge != null) addBadge.SetActive(unlocked && count <= 0);
        SetGlow(unlocked && (active || _highlight));
    }

    private void SetGlow(bool on)
    {
        if (glow == null) return;
        if (glow.gameObject.activeSelf == on && (!on || _glowTween != null)) return;

        _glowTween?.Kill();
        _glowTween = null;
        glow.gameObject.SetActive(on);
        if (!on) return;

        RectTransform rt = glow.rectTransform;
        rt.localRotation = Quaternion.identity;
        _glowTween = rt.DOLocalRotate(new Vector3(0f, 0f, -360f), 360f / glowSpin, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetLink(gameObject);
    }
}
