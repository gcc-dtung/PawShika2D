using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PawBlock.Features.Shop
{
    /// <summary>
    /// Hiệu ứng mở Shop lần đầu trong mỗi lần chạy game: thẻ có Body trượt từ ngoài vào (xen kẽ trái / phải),
    /// ô không có Body (ô coin) phóng to từ 0; lần lượt cách nhau stagger. Các lần mở sau trong cùng lần chạy không chạy lại.
    /// Cờ "đã chạy" là static (không lưu), reset khi vào Play (project tắt domain reload).
    /// Tắt Shop giữa chừng (sang tab khác) thì nhảy tới cuối hiệu ứng.
    /// </summary>
    public class ShopIntro : MonoBehaviour
    {
        [SerializeField] private float slideDuration = 0.45f;
        [SerializeField] private float popDuration = 0.3f;
        [SerializeField] private float stagger = 0.06f;
        [Tooltip("Chỉ giãn delay cho chừng này thẻ đầu, thẻ sau (thường nằm dưới màn hình) chạy cùng lúc thẻ cuối")]
        [SerializeField] private int maxStaggered = 8;
        [Tooltip("Thẻ trượt ra ngoài bao nhiêu lần bề rộng của nó")]
        [SerializeField] private float slideDistance = 1.15f;
        [SerializeField] private float overshoot = 1.2f;

        private static bool _played;

        private readonly List<ShopCardView> _cards = new List<ShopCardView>();
        private Sequence _anim;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRun() => _played = false;

        /// <summary>Đặt thẻ về vị trí bắt đầu (ngoài màn hình / scale 0) ngay sau khi spawn, để vuốt sang Shop không thấy thẻ đứng sẵn.</summary>
        public void Prepare(IReadOnlyList<ShopCardView> cards)
        {
            _cards.Clear();
            if (_played) return;
            _cards.AddRange(cards);

            int slide = 0;
            foreach (ShopCardView card in _cards)
            {
                if (card.HasBody)
                {
                    RectTransform body = card.Body;
                    float side = slide++ % 2 == 0 ? -1f : 1f;
                    body.anchoredPosition = new Vector2(side * body.rect.width * slideDistance, body.anchoredPosition.y);
                }
                else
                {
                    card.transform.localScale = Vector3.zero;
                }
            }
        }

        /// <summary>ShopPanel gọi khi Shop thành tab đang chọn.</summary>
        public void Play()
        {
            if (_played || _cards.Count == 0) return;
            _played = true;

            _anim = DOTween.Sequence().SetLink(gameObject);
            for (int i = 0; i < _cards.Count; i++)
            {
                ShopCardView card = _cards[i];
                if (card == null) continue;
                float delay = Mathf.Min(i, maxStaggered) * stagger;
                if (card.HasBody)
                    _anim.Insert(delay, card.Body.DOAnchorPosX(0f, slideDuration).SetEase(Ease.OutBack, overshoot));
                else
                    _anim.Insert(delay, card.transform.DOScale(1f, popDuration).SetEase(Ease.OutBack, overshoot));
            }
            _anim.OnKill(() => _anim = null);
        }

        private void OnDisable()
        {
            if (_anim != null) _anim.Complete();
            // Vuốt thử sang Shop rồi thôi: thẻ vẫn chờ ngoài màn hình cho lần chọn thật
        }
    }
}
