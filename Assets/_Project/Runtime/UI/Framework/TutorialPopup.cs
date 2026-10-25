using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Base popup hướng dẫn sự kiện / liveops (nút "i" xanh): nền tối, băng-rôn tiêu đề treo dây bật lên,
    /// rồi lần lượt từng bước bật lên (ảnh + chú thích, mũi tên dẫn sang bước sau), cuối cùng "Chạm để tiếp tục".
    /// Chạm khi đang chạy = hiện hết ngay; chạm lần nữa = đóng. Chạm bắt bằng IPointerClickHandler trên gốc (Dim chỉ là Image chặn raycast,
    /// không có Button) để UIAutoSetup không gắn ButtonPress làm nền tối lún khi nhấn.
    /// Mỗi sự kiện là 1 prefab variant của TutorialPopup.prefab: chỉ đổi chữ tiêu đề và bố trí các bước (con của Panel).
    /// </summary>
    public class TutorialPopup : PopupView, IPointerClickHandler
    {
        [Serializable]
        public class Step
        {
            [Tooltip("Ảnh + chú thích của bước, bật lên từ scale 0")]
            public RectTransform content;
            [Tooltip("Mũi tên dẫn sang bước sau (có thể trống), bật lên sau content")]
            public Image arrow;
        }

        [SerializeField] private RectTransform banner;
        [SerializeField] private float bannerDuration = 0.4f;
        [SerializeField] private Step[] steps = new Step[0];
        [SerializeField] private float stepDuration = 0.35f;
        [Tooltip("Nghỉ giữa 2 bước")]
        [SerializeField] private float stepGap = 0.2f;
        [SerializeField] private CanvasGroup tapHint;

        private Sequence _reveal;
        private Tween _hintPulse;
        private bool _revealed;
        private Action _onClose;

        /// <summary>Mở popup; onClose gọi khi người chơi chạm đóng.</summary>
        public void Show(Action onClose = null)
        {
            _onClose = onClose;
            Open();
        }

        protected override void OnOpened()
        {
            KillTweens();
            _revealed = false;
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            AppendPop(seq, banner, bannerDuration);
            foreach (Step step in steps)
            {
                if (step.content != null) seq.AppendInterval(stepGap);
                AppendPop(seq, step.content, stepDuration);
                AppendPop(seq, step.arrow != null ? step.arrow.rectTransform : null, stepDuration);
            }

            if (tapHint != null)
            {
                tapHint.alpha = 0f;
                seq.AppendInterval(stepGap);
                seq.Append(tapHint.DOFade(1f, 0.25f));
            }
            seq.OnComplete(OnRevealed);
            _reveal = seq;
        }

        // Bật lên từ scale 0, vượt nhẹ rồi về 1
        private static void AppendPop(Sequence seq, RectTransform target, float duration)
        {
            if (target == null) return;
            target.localScale = Vector3.zero;
            seq.Append(target.DOScale(1f, duration).SetEase(Ease.OutBack));
        }

        private void OnRevealed()
        {
            _reveal = null;
            _revealed = true;
            if (tapHint != null)
            {
                _hintPulse = tapHint.transform.DOScale(1.06f, 0.6f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
            }
        }

        // Chạm vào Dim (hoặc bất kỳ con nào chặn raycast) nổi lên tới gốc
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsOpen || eventData.button != PointerEventData.InputButton.Left) return;
            if (!_revealed)
            {
                _reveal?.Complete(true); // hiện hết ngay (gọi OnRevealed)
                return;
            }
            Close();
            Action cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }

        protected override void OnClosed()
        {
            KillTweens();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillTweens();
        }

        private void KillTweens()
        {
            _reveal?.Kill();
            _reveal = null;
            _hintPulse?.Kill();
            _hintPulse = null;
            if (tapHint != null) tapHint.transform.localScale = Vector3.one;
        }
    }
}
