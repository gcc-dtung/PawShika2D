using System;
using DG.Tweening;
using UnityEngine;
using PawBlock.Features.DailyChallenge;

namespace PawBlock.Game
{
    /// <summary>
    /// Thanh tab dưới của Main Menu (MenuTabBar.prefab): Shop | Home | Daily.
    /// - Tab đang chọn rộng selectedWidth, nền tab chọn (selectedBackground) trượt theo; các tab còn lại chia đều phần còn lại.
    /// - Tab và nền neo bottom center (anchor + pivot (0.5, 0)); anchoredPosition.x là tâm tab so với giữa thanh.
    /// - Vạch ngăn chỉ hiện giữa 2 tab đều không được chọn.
    /// Bấm tab -> TabClicked; MainMenuNavigator quyết định chuyển và gọi Layout / Animate.
    /// </summary>
    public class MenuTabBar : MonoBehaviour
    {
        [SerializeField] private RectTransform bar;
        [SerializeField] private MenuTabButton[] tabs;
        [SerializeField] private RectTransform selectedBackground;
        [SerializeField] private float selectedWidth = 408f;
        [Tooltip("Chấm đỏ tab Daily (Editor tự điền)")]
        [SerializeField] private DailyTabBadge dailyBadge;

        /// <summary>Người chơi bấm tab (chỉ số theo thứ tự tabs).</summary>
        public event Action<int> TabClicked;

        public int Count => tabs != null ? tabs.Length : 0;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(DailyChallengeManager dailyChallenge)
        {
            if (dailyBadge != null) dailyBadge.Init(dailyChallenge);
        }

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i] != null) tabs[i].Button.onClick.AddListener(() => TabClicked?.Invoke(index));
            }
        }

        /// <summary>
        /// Đổi tab chọn từ <paramref name="previous"/> sang <paramref name="selected"/>: icon tab, vạch ngăn (có hiệu ứng <paramref name="duration"/>).
        /// Vị trí / bề rộng tab do Layout (ngay) hoặc Animate (trong sequence của điều hướng).
        /// </summary>
        public void SetSelected(int selected, int previous, float duration)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == selected || i == previous || duration <= 0f) tabs[i].SetSelected(i == selected, duration);
                // Vạch ngăn đổi ngay từ lúc bắt đầu trượt: vạch mới hiện dần, vạch cũ mờ nhanh trước khi nền tab chọn lướt tới
                tabs[i].SetDividerVisible(DividerVisible(i, selected), duration);
            }
        }

        /// <summary>Xếp tab + nền tab chọn ngay về trạng thái tab <paramref name="selected"/>.</summary>
        public void Layout(int selected)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                TabSlot(i, selected, out float x, out float w);
                RectTransform rt = tabs[i].Rect;
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                tabs[i].SetDividerVisible(DividerVisible(i, selected));
            }
            if (selectedBackground != null)
            {
                TabSlot(selected, selected, out float bx, out _);
                selectedBackground.anchoredPosition = new Vector2(bx, selectedBackground.anchoredPosition.y);
                selectedBackground.sizeDelta = new Vector2(selectedWidth, selectedBackground.sizeDelta.y);
            }
        }

        /// <summary>Thêm vào <paramref name="seq"/> tween tab + nền tab chọn trượt tới trạng thái tab <paramref name="selected"/>.</summary>
        public void Animate(Sequence seq, int selected, float duration, Ease ease)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                TabSlot(i, selected, out float x, out float w);
                seq.Join(tabs[i].Rect.DOAnchorPosX(x, duration).SetEase(ease));
                seq.Join(tabs[i].Rect.DOSizeDelta(new Vector2(w, tabs[i].Rect.sizeDelta.y), duration).SetEase(ease));
            }
            if (selectedBackground != null)
            {
                TabSlot(selected, selected, out float bx, out _);
                seq.Join(selectedBackground.DOAnchorPosX(bx, duration).SetEase(ease));
            }
        }

        /// <summary>Vạch ở mép phải tab <paramref name="tab"/>: chỉ hiện khi tab đó và tab bên phải đều không được chọn.</summary>
        private bool DividerVisible(int tab, int selected) => tab < tabs.Length - 1 && tab != selected && tab + 1 != selected;

        /// <summary>
        /// Tâm (so với giữa thanh, neo bottom center) và độ rộng của tab <paramref name="tab"/>
        /// khi tab <paramref name="selected"/> đang chọn.
        /// </summary>
        private void TabSlot(int tab, int selected, out float x, out float width)
        {
            float total = bar != null ? bar.rect.width : ((RectTransform)transform).rect.width;
            float sel = Mathf.Min(selectedWidth, total);
            float other = tabs.Length > 1 ? (total - sel) / (tabs.Length - 1) : total;
            float left = 0f;
            for (int i = 0; i < tab; i++) left += i == selected ? sel : other;
            width = tab == selected ? sel : other;
            x = left + width / 2f - total / 2f;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (dailyBadge == null) dailyBadge = GetComponentInChildren<DailyTabBadge>(true);
        }

        /// <summary>Editor (MainMenuNavigator.LayoutInEditor): xếp tab về trạng thái tab <paramref name="selected"/>, kèm icon tab.</summary>
        public void EditorLayout(int selected)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null) tabs[i].SetSelected(i == selected, 0f);
            }
            Layout(selected);
        }
#endif
    }
}
