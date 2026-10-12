using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Điều hướng Main Menu (MainMenu.prefab): thanh tab dưới (MenuTabBar) và vuốt ngang trên panel (MenuSwipe).
    /// Mỗi tab là 1 prefab có script gốc riêng (MenuPanel: ShopPanel, HomePanel, DailyChallengePanel) tự lo nội dung;
    /// ở đây chỉ xếp / trượt panel, báo tab đang chọn (MenuPanel.SetSelected) và nhờ MenuTabBar vẽ thanh tab.
    /// - Panel xếp ngang theo thứ tự tab, đổi tab thì trượt ngang; xong thì tắt các panel khuất màn hình.
    /// - Vuốt: panel chạy theo ngón tay (đang ở tab đầu / cuối thì không kéo ra ngoài được), thả tay thì sang tab kề nếu kéo quá swipeThreshold
    ///   bề rộng hoặc vuốt nhanh hơn swipeVelocity, không thì trượt về; thời gian trượt theo quãng còn lại.
    /// - LayoutInEditor: xếp sẵn trong Editor (builder gọi, hoặc chuột phải component) để scene đúng vị trí khi chưa chạy.
    /// Tween DOTween giữ trong field, kill khi tắt.
    /// </summary>
    public class MainMenuNavigator : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private MenuPanel[] menuPanels;   // cùng thứ tự với tab
        [SerializeField] private MenuTabBar menuTabBar;
        [SerializeField] private int startIndex = 1;       // Home

        /// <summary>Thứ tự tab Shop trong thanh tab (CoinHUD, ScreenFlow mở Shop bằng số này).</summary>
        public const int ShopTab = 0;
        public const int HomeTab = 1;
        /// <summary>Tab Daily Challenge (về Menu sau ván Daily thì mở tab này).</summary>
        public const int DailyTab = 2;

        [Header("Chuyển động")]
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

        private int TabCount => menuPanels != null ? menuPanels.Length : 0;

        private RectTransform PanelRect(int i) => menuPanels[i] != null ? menuPanels[i].Rect : null;

        private void Awake()
        {
            if (menuTabBar != null) menuTabBar.TabClicked += OnTabClicked;
        }

        private void OnDestroy()
        {
            if (menuTabBar != null) menuTabBar.TabClicked -= OnTabClicked;
        }

        private void OnTabClicked(int index) => Select(index);

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
            if (TabCount == 0) return;
            index = Mathf.Clamp(index, 0, TabCount - 1);
            if (index == _current && _anim == null && !_swiping && IsSettled(index)) return;
            _swiping = false;
            Go(index, animate && _current >= 0 && isActiveAndEnabled ? duration : 0f);
        }

        // ---------- Vuốt (MenuSwipe gọi) ----------

        /// <summary>Bắt đầu vuốt: dừng hiệu ứng đang chạy tại chỗ, bật panel 2 bên.</summary>
        public bool BeginSwipe()
        {
            if (_current < 0 || TabCount == 0) return false;
            _anim?.Kill();
            _anim = null;
            _swiping = true;
            _swipeBase = PanelRect(_current) != null ? PanelRect(_current).anchoredPosition.x : 0f;
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
            if (_current == TabCount - 1) offset = Mathf.Max(offset, 0f);
            SetPanelOffset(offset);
        }

        /// <summary>Thả tay với vận tốc <paramref name="velocityX"/> (đơn vị canvas / giây, dương = sang phải).</summary>
        public void EndSwipe(float velocityX)
        {
            if (!_swiping) return;
            _swiping = false;

            float width = PanelWidth();
            float offset = PanelRect(_current) != null ? PanelRect(_current).anchoredPosition.x : 0f;
            int target = _current;
            // Kéo sang trái (offset âm) = xem tab bên phải
            if (offset < -width * swipeThreshold || velocityX < -swipeVelocity) target = _current + 1;
            else if (offset > width * swipeThreshold || velocityX > swipeVelocity) target = _current - 1;
            target = Mathf.Clamp(target, 0, TabCount - 1);

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

            if (menuTabBar != null) menuTabBar.SetSelected(index, previous, d);

            if (d <= 0f)
            {
                LayoutInstant(index);
            }
            else
            {
                SetPanelsActive(VisibleFrom(index), VisibleTo(index)); // các panel đang hiện / sẽ lướt qua
                Sequence seq = DOTween.Sequence().SetLink(gameObject);
                for (int i = 0; i < TabCount; i++)
                {
                    if (PanelRect(i) != null) seq.Join(PanelRect(i).DOAnchorPosX(PanelX(i, index), d).SetEase(ease));
                }
                if (menuTabBar != null) menuTabBar.Animate(seq, index, d, ease);
                seq.OnComplete(() => SetPanelsActive(index, index));
                seq.OnKill(() => { if (_anim == seq) _anim = null; });
                _anim = seq;
            }

            for (int i = 0; i < TabCount; i++)
            {
                if (menuPanels[i] != null) menuPanels[i].SetSelected(i == index);
            }
            if (previous != index) onTabChanged?.Invoke(index);
        }

        /// <summary>Xếp tab / panel về trạng thái tab mặc định ngay trong Editor (không tắt panel nào).</summary>
        [ContextMenu("Layout In Editor")]
        public void LayoutInEditor()
        {
            int index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, TabCount - 1));
#if UNITY_EDITOR
            if (menuTabBar != null) menuTabBar.EditorLayout(index);
#endif
            SetPanelOffset(0f, index);
        }

        private void LayoutInstant(int index)
        {
            SetPanelOffset(0f, index);
            if (menuTabBar != null) menuTabBar.Layout(index);
            SetPanelsActive(index, index);
        }

        /// <summary>Đặt panel <paramref name="selected"/> lệch <paramref name="offset"/> khỏi giữa, các panel khác xếp liền kề.</summary>
        private void SetPanelOffset(float offset, int selected = -1)
        {
            if (selected < 0) selected = _current;
            for (int i = 0; i < TabCount; i++)
            {
                RectTransform rt = PanelRect(i);
                if (rt == null) continue;
                Vector2 p = rt.anchoredPosition;
                p.x = PanelX(i, selected) + offset;
                rt.anchoredPosition = p;
            }
        }

        private bool IsSettled(int index)
        {
            return index >= TabCount || PanelRect(index) == null
                || Mathf.Approximately(PanelRect(index).anchoredPosition.x, 0f);
        }

        private float PanelWidth() => panelRoot != null ? panelRoot.rect.width : ((RectTransform)transform).rect.width;

        private float PanelX(int panel, int selected) => (panel - selected) * PanelWidth();

        // Panel đang lọt vào màn hình (kể cả đang trượt dở / đang kéo) và panel đích: giữ bật trong lúc trượt
        private int VisibleFrom(int target)
        {
            int from = target;
            for (int i = 0; i < TabCount; i++)
            {
                if (PanelRect(i) != null && PanelRect(i).gameObject.activeSelf && IsOnScreen(i)) from = Mathf.Min(from, i);
            }
            return from;
        }

        private int VisibleTo(int target)
        {
            int to = target;
            for (int i = 0; i < TabCount; i++)
            {
                if (PanelRect(i) != null && PanelRect(i).gameObject.activeSelf && IsOnScreen(i)) to = Mathf.Max(to, i);
            }
            return to;
        }

        private bool IsOnScreen(int i) => Mathf.Abs(PanelRect(i).anchoredPosition.x) < PanelWidth() - 0.5f;

        private void SetPanelsActive(int from, int to)
        {
            for (int i = 0; i < TabCount; i++)
            {
                if (PanelRect(i) != null) PanelRect(i).gameObject.SetActive(i >= from && i <= to);
            }
        }
    }
}
