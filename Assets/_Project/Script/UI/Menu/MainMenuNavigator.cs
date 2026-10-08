using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Điều hướng Main Menu bằng thanh tab dưới (Shop | Home | Progress) và vuốt ngang trên panel (MenuSwipe).
/// - Panel xếp ngang theo thứ tự tab, đổi tab thì trượt ngang; xong thì tắt các panel khuất màn hình.
/// - Tab đang chọn rộng selectedWidth, nền home_tab1 (selectedBackground) trượt theo; các tab còn lại chia đều phần còn lại.
/// - Tab và nền tab chọn neo bottom center (anchor + pivot (0.5, 0)); anchoredPosition.x là tâm tab so với giữa thanh.
/// - Vuốt: panel chạy theo ngón tay (đang ở tab đầu / cuối thì không kéo ra ngoài được), thả tay thì sang tab kề nếu kéo quá swipeThreshold
///   bề rộng hoặc vuốt nhanh hơn swipeVelocity, không thì trượt về; thời gian trượt theo quãng còn lại.
/// - LayoutInEditor: xếp sẵn trong Editor (builder gọi, hoặc chuột phải component) để scene đúng vị trí khi chưa chạy.
/// Tween DOTween giữ trong field, kill khi tắt.
/// </summary>
public class MainMenuNavigator : MonoBehaviour
{
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform[] panels;   // cùng thứ tự với tabs
    [SerializeField] private RectTransform tabBar;
    [SerializeField] private MenuTabButton[] tabs;
    [SerializeField] private RectTransform selectedBackground;
    [SerializeField] private int startIndex = 1;       // Home

    /// <summary>Thứ tự tab Shop trong thanh tab (CoinHUD, ScreenFlow mở Shop bằng số này).</summary>
    public const int ShopTab = 0;

    [Header("Chuyển động")]
    [SerializeField] private float selectedWidth = 408f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Vuốt")]
    [Tooltip("Kéo quá tỉ lệ này của bề rộng panel thì sang tab kề")]
    [SerializeField, Range(0.05f, 0.5f)] private float swipeThreshold = 0.2f;
    [Tooltip("Vuốt nhanh hơn (đơn vị canvas / giây) thì sang tab kề dù kéo ngắn")]
    [SerializeField] private float swipeVelocity = 900f;
    [Tooltip("Thời gian trượt ngắn nhất sau khi thả tay (tỉ lệ duration)")]
    [SerializeField, Range(0.1f, 1f)] private float minReleaseRatio = 0.45f;

    [Header("Events")]
    public UnityEvent<int> onTabChanged = new UnityEvent<int>();

    private int _current = -1;
    private Sequence _anim;
    private bool _swiping;
    private float _swipeBase; // vị trí panel đang chọn lúc bắt đầu vuốt (khác 0 nếu đang trượt dở)

    public int Current => _current;

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i] != null) tabs[i].Button.onClick.AddListener(() => Select(index));
        }
    }

    private void Start()
    {
        Select(startIndex, false);
    }

    private void OnDisable()
    {
        _swiping = false;
        // Hủy giữa chừng: nhảy tới trạng thái của tab đang chọn
        if (_anim != null)
        {
            _anim.Kill();
            _anim = null;
        }
        if (_current >= 0) LayoutInstant(_current);
    }

    /// <summary>Chuyển sang tab <paramref name="index"/>.</summary>
    public void Select(int index, bool animate = true)
    {
        if (tabs == null || tabs.Length == 0) return;
        index = Mathf.Clamp(index, 0, tabs.Length - 1);
        if (index == _current && _anim == null && !_swiping && IsSettled(index)) return;
        _swiping = false;
        Go(index, animate && _current >= 0 && isActiveAndEnabled ? duration : 0f);
    }

    // ---------- Vuốt (MenuSwipe gọi) ----------

    /// <summary>Bắt đầu vuốt: dừng hiệu ứng đang chạy tại chỗ, bật panel 2 bên.</summary>
    public bool BeginSwipe()
    {
        if (_current < 0 || panels == null || panels.Length == 0) return false;
        _anim?.Kill();
        _anim = null;
        _swiping = true;
        _swipeBase = panels[_current] != null ? panels[_current].anchoredPosition.x : 0f;
        SetPanelsActive(_current - 1, _current + 1);
        return true;
    }

    /// <summary>Ngón tay đã kéo <paramref name="deltaX"/> (đơn vị canvas) từ lúc bắt đầu vuốt.</summary>
    public void Swipe(float deltaX)
    {
        if (!_swiping) return;
        float offset = _swipeBase + deltaX;
        // Không còn tab phía đó: không cho kéo ra ngoài (kéo dương = lộ tab bên trái)
        if (_current == 0) offset = Mathf.Min(offset, 0f);
        if (_current == panels.Length - 1) offset = Mathf.Max(offset, 0f);
        SetPanelOffset(offset);
    }

    /// <summary>Thả tay với vận tốc <paramref name="velocityX"/> (đơn vị canvas / giây, dương = sang phải).</summary>
    public void EndSwipe(float velocityX)
    {
        if (!_swiping) return;
        _swiping = false;

        float width = PanelWidth();
        float offset = panels[_current] != null ? panels[_current].anchoredPosition.x : 0f;
        int target = _current;
        // Kéo sang trái (offset âm) = xem tab bên phải
        if (offset < -width * swipeThreshold || velocityX < -swipeVelocity) target = _current + 1;
        else if (offset > width * swipeThreshold || velocityX > swipeVelocity) target = _current - 1;
        target = Mathf.Clamp(target, 0, tabs.Length - 1);

        // Quãng còn lại ngắn thì trượt nhanh hơn (đã kéo gần tới)
        float remaining = Mathf.Abs(offset + (target - _current) * width);
        float d = duration * Mathf.Clamp(remaining / Mathf.Max(1f, width), minReleaseRatio, 1f);
        Go(target, d);
    }

    // ---------- Chuyển tab ----------

    private void Go(int index, float d)
    {
        int previous = _current;
        _current = index;
        _anim?.Kill();
        _anim = null;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            if (i == index || i == previous || d <= 0f) tabs[i].SetSelected(i == index, d);
            // Vạch ngăn đổi ngay từ lúc bắt đầu trượt: vạch mới hiện dần, vạch cũ mờ nhanh trước khi nền tab chọn lướt tới
            tabs[i].SetDividerVisible(DividerVisible(i, index), d);
        }

        if (d <= 0f)
        {
            LayoutInstant(index);
        }
        else
        {
            SetPanelsActive(VisibleFrom(index), VisibleTo(index)); // các panel đang hiện / sẽ lướt qua
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null) seq.Join(panels[i].DOAnchorPosX(PanelX(i, index), d).SetEase(ease));
            }
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                TabSlot(i, index, out float x, out float w);
                seq.Join(tabs[i].Rect.DOAnchorPosX(x, d).SetEase(ease));
                seq.Join(tabs[i].Rect.DOSizeDelta(new Vector2(w, tabs[i].Rect.sizeDelta.y), d).SetEase(ease));
            }
            if (selectedBackground != null)
            {
                TabSlot(index, index, out float bx, out _);
                seq.Join(selectedBackground.DOAnchorPosX(bx, d).SetEase(ease));
            }
            seq.OnComplete(() => SetPanelsActive(index, index));
            seq.OnKill(() => { if (_anim == seq) _anim = null; });
            _anim = seq;
        }

        if (previous != index) onTabChanged?.Invoke(index);
    }

    /// <summary>Xếp tab / panel về trạng thái tab mặc định ngay trong Editor (không tắt panel nào).</summary>
    [ContextMenu("Layout In Editor")]
    public void LayoutInEditor()
    {
        int index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, tabs.Length - 1));
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            tabs[i].SetSelected(i == index, 0f);
        }
        UpdateDividers(index);
        LayoutRects(index);
    }

    private void LayoutInstant(int index)
    {
        LayoutRects(index);
        SetPanelsActive(index, index);
        UpdateDividers(index);
    }

    private void LayoutRects(int index)
    {
        SetPanelOffset(0f, index);
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            TabSlot(i, index, out float x, out float w);
            RectTransform rt = tabs[i].Rect;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        }
        if (selectedBackground != null)
        {
            TabSlot(index, index, out float bx, out _);
            selectedBackground.anchoredPosition = new Vector2(bx, selectedBackground.anchoredPosition.y);
            selectedBackground.sizeDelta = new Vector2(selectedWidth, selectedBackground.sizeDelta.y);
        }
    }

    /// <summary>Đặt panel <paramref name="selected"/> lệch <paramref name="offset"/> khỏi giữa, các panel khác xếp liền kề.</summary>
    private void SetPanelOffset(float offset, int selected = -1)
    {
        if (selected < 0) selected = _current;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            Vector2 p = panels[i].anchoredPosition;
            p.x = PanelX(i, selected) + offset;
            panels[i].anchoredPosition = p;
        }
    }

    private bool IsSettled(int index)
    {
        return panels == null || index >= panels.Length || panels[index] == null
            || Mathf.Approximately(panels[index].anchoredPosition.x, 0f);
    }

    /// <summary>Vạch ngăn chỉ giữa 2 tab đều không được chọn.</summary>
    private void UpdateDividers(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null) tabs[i].SetDividerVisible(DividerVisible(i, index));
        }
    }

    /// <summary>Vạch ở mép phải tab <paramref name="tab"/>: chỉ hiện khi tab đó và tab bên phải đều không được chọn.</summary>
    private bool DividerVisible(int tab, int selected) => tab < tabs.Length - 1 && tab != selected && tab + 1 != selected;

    private float PanelWidth() => panelRoot != null ? panelRoot.rect.width : ((RectTransform)transform).rect.width;

    private float PanelX(int panel, int selected) => (panel - selected) * PanelWidth();

    // Panel đang lọt vào màn hình (kể cả đang trượt dở / đang kéo) và panel đích: giữ bật trong lúc trượt
    private int VisibleFrom(int target)
    {
        int from = target;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject.activeSelf && IsOnScreen(i)) from = Mathf.Min(from, i);
        }
        return from;
    }

    private int VisibleTo(int target)
    {
        int to = target;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject.activeSelf && IsOnScreen(i)) to = Mathf.Max(to, i);
        }
        return to;
    }

    private bool IsOnScreen(int i) => Mathf.Abs(panels[i].anchoredPosition.x) < PanelWidth() - 0.5f;

    /// <summary>
    /// Tâm (so với giữa thanh, neo bottom center) và độ rộng của tab <paramref name="tab"/>
    /// khi tab <paramref name="selected"/> đang chọn.
    /// </summary>
    private void TabSlot(int tab, int selected, out float x, out float width)
    {
        float total = tabBar != null ? tabBar.rect.width : ((RectTransform)transform).rect.width;
        float sel = Mathf.Min(selectedWidth, total);
        float other = tabs.Length > 1 ? (total - sel) / (tabs.Length - 1) : total;
        float left = 0f;
        for (int i = 0; i < tab; i++) left += i == selected ? sel : other;
        width = tab == selected ? sel : other;
        x = left + width / 2f - total / 2f;
    }

    private void SetPanelsActive(int from, int to)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].gameObject.SetActive(i >= from && i <= to);
        }
    }
}
