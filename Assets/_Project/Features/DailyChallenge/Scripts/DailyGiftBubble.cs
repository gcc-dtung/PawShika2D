using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Gameplay;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>
    /// Bong thoại xem quà của 1 mốc Daily Challenge (GiftMess gốc: khung dialog.png + mũi ArrowDialog chỉ lên hộp quà).
    /// Hiện ngay dưới hộp quà (offset), mỗi món 1 icon + số ("x2", "15m").
    /// Chạm ra ngoài bong thoại thì ẩn nhưng KHÔNG chặn chạm: chạm hộp quà khác thì đóng cái này và mở cái kia trong 1 lần,
    /// chạm ô ngày / nút thì nút đó chạy luôn.
    /// </summary>
    public class DailyGiftBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform itemRow;
        [SerializeField] private Image itemIconTemplate; // con: TMP_Text số lượng
        [SerializeField] private Vector2 offset = new Vector2(0f, -120f);
        [Tooltip("Cách mép màn hình tối thiểu (bong thoại không tràn ra ngoài)")]
        [SerializeField] private float edgeMargin = 24f;

        private readonly List<Image> _items = new List<Image>();

        private void Awake()
        {
            if (itemIconTemplate != null) itemIconTemplate.gameObject.SetActive(false);
            // Không tự tắt ở đây: object dựng sẵn ở trạng thái tắt, Awake chạy lần đầu đúng lúc Show bật nó lên
        }

        /// <summary>Hiện dưới <paramref name="anchor"/> với các món quà; icons lấy từ DailyRewardDatabase (coin, năng lượng vô hạn).</summary>
        public void Show(RectTransform anchor, string heading, IList<DailyRewardItem> items, DailyRewardDatabase icons)
        {
            gameObject.SetActive(true);
            if (title != null) title.SetText(heading);
            Fill(items, icons);

            // Đặt dưới hộp quà, kẹp trong màn hình; mũi tên vẫn chỉ đúng hộp
            RectTransform parent = (RectTransform)bubble.parent;
            Vector2 target = (Vector2)parent.InverseTransformPoint(anchor.position) + offset;
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            float half = bubble.rect.width * 0.5f;
            float x = Mathf.Clamp(target.x, parent.rect.xMin + edgeMargin + half, parent.rect.xMax - edgeMargin - half);
            bubble.anchoredPosition = new Vector2(x, target.y);
            if (arrow != null) arrow.anchoredPosition = new Vector2(target.x - x, arrow.anchoredPosition.y);

            bubble.DOKill();
            bubble.localScale = Vector3.one * 0.6f;
            bubble.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(bubble.gameObject);
        }

        // Ngón tay vừa chạm xuống ngoài bong thoại -> ẩn (chạm vẫn tới nút bên dưới; mở bong thoại mới xảy ra lúc nhả tay, sau bước này)
        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || bubble == null) return;
            Canvas canvas = bubble.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(bubble, pointer.position.ReadValue(), cam)) Hide();
        }

        public void Hide()
        {
            if (bubble != null) bubble.DOKill();
            gameObject.SetActive(false);
        }

        private void Fill(IList<DailyRewardItem> items, DailyRewardDatabase icons)
        {
            foreach (Image i in _items) i.gameObject.SetActive(false);
            if (items == null || itemIconTemplate == null) return;
            for (int n = 0; n < items.Count; n++)
            {
                if (n == _items.Count)
                {
                    Image clone = Instantiate(itemIconTemplate, itemRow);
                    _items.Add(clone);
                }
                Image img = _items[n];
                img.gameObject.SetActive(true);
                img.sprite = IconOf(items[n], icons);
                TMP_Text amount = img.GetComponentInChildren<TMP_Text>(true);
                if (amount != null) amount.SetText(AmountOf(items[n]));
            }
        }

        public static Sprite IconOf(DailyRewardItem item, DailyRewardDatabase icons)
        {
            switch (item.kind)
            {
                case DailyRewardKind.Coin: return icons != null ? icons.coinIcon : null;
                case DailyRewardKind.InfiniteEnergy: return icons != null ? icons.infiniteEnergyIcon : null;
                default:
                    BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(item.booster) : null;
                    return data != null ? data.ItemIcon : null;
            }
        }

        /// <summary>"15m" cho năng lượng vô hạn (phút), "x2" cho booster / coin.</summary>
        public static string AmountOf(DailyRewardItem item) =>
            item.kind == DailyRewardKind.InfiniteEnergy ? $"{item.amount}m" : $"x{item.amount}";
    }
}
