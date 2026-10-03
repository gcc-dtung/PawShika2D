using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Điều hướng Main Menu bằng thanh tab dưới (Shop | Home | Progress).
/// - Panel xếp ngang theo thứ tự tab, đổi tab thì trượt ngang; xong thì tắt các panel khuất màn hình.
/// - Tab đang chọn rộng selectedWidth, nền home_tab1 (selectedBackground) trượt theo; các tab còn lại chia đều phần còn lại.
/// - Tab và nền tab chọn neo bottom center (anchor + pivot (0.5, 0)); anchoredPosition.x là tâm tab so với giữa thanh.
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

    [Header("Chuyển động")]
    [SerializeField] private float selectedWidth = 408f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Events")]
    public UnityEvent<int> onTabChanged = new UnityEvent<int>();

    private int _current = -1;
    private Sequence _anim;

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
        // Hủy giữa chừng: nhảy tới trạng thái của tab đang chọn
        if (_anim != null)
        {
            _anim.Kill();
            _anim = null;
            if (_current >= 0) LayoutInstant(_current);
        }
    }

    /// <summary>Chuyển sang tab <paramref name="index"/>.</summary>
    public void Select(int index, bool animate = true)
    {
        if (tabs == null || tabs.Length == 0) return;
        index = Mathf.Clamp(index, 0, tabs.Length - 1);
        if (index == _current && _anim == null) return;

        int previous = _current;
        _current = index;
        _anim?.Kill();
        _anim = null;

        float d = animate && previous >= 0 && isActiveAndEnabled ? duration : 0f;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            tabs[i].SetSelected(i == index, d, ease);
            // Vạch ngăn đổi ngay từ lúc bắt đầu trượt: vạch mới hiện dần, vạch cũ mờ nhanh trước khi nền tab chọn lướt tới
            tabs[i].SetDividerVisible(DividerVisible(i, index), d);
        }

        if (d <= 0f)
        {
            LayoutInstant(index);
        }
        else
        {
            SetPanelsActive(Mathf.Min(previous, index), Mathf.Max(previous, index)); // các panel sẽ lướt qua
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
            tabs[i].SetSelected(i == index, 0f, ease);
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
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            Vector2 p = panels[i].anchoredPosition;
            p.x = PanelX(i, index);
            panels[i].anchoredPosition = p;
        }
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

    private float PanelX(int panel, int selected)
    {
        float width = panelRoot != null ? panelRoot.rect.width : ((RectTransform)transform).rect.width;
        return (panel - selected) * width;
    }

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
