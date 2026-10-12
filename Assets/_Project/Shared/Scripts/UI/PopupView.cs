using System;
using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Shared
{
    /// <summary>
    /// Base popup (guide 10.6): nền tối mờ dần + bảng bật lên (scale 0.6 -> vượt nhẹ -> 1, Ease.OutBack). Tween chạy theo thời gian thực
    /// nên không phụ thuộc timeScale. Popup tắt thì hủy tween.
    /// Cấu trúc: root (CanvasGroup) / Dim (Image chắn raycast) / Panel.
    /// Lớp con nhận dữ liệu bằng hàm riêng rồi gọi Open(); dùng hook OnOpened / OnClosed, không override Open / Close.
    /// Không biết luật game: nạp level mới thì GameFlowUI (Game) đóng popup đang mở có CloseOnLevelLoad.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupView : MonoBehaviour
    {
        [SerializeField] protected RectTransform panel;
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.15f;
        [SerializeField] private CanvasGroup group;

        private Sequence _anim;

        public bool IsOpen { get; private set; }

        /// <summary>Đã đóng hẳn (hết hiệu ứng đóng, hoặc CloseImmediate khi đang mở).</summary>
        public event Action<PopupView> Closed;

        /// <summary>Nạp level mới (kể cả bằng menu debug) thì đóng ngay khi đang mở. Popup tự quyết, vd Notice giữ lại.</summary>
        public virtual bool CloseOnLevelLoad => true;

        protected virtual void Awake()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            KillAnim();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            IsOpen = true;
            group.interactable = true;
            group.blocksRaycasts = true;
            OnOpened();
            AudioManager.Play(SoundId.PopupOpen);

            group.alpha = 0f;
            if (panel != null) panel.localScale = Vector3.one * 0.6f;

            Sequence seq = NewSequence();
            seq.Join(group.DOFade(1f, showDuration * 0.5f).SetEase(Ease.Linear));
            if (panel != null) seq.Join(panel.DOScale(1f, showDuration).SetEase(Ease.OutBack));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            group.interactable = false;
            AudioManager.Play(SoundId.PopupClose);

            Sequence seq = NewSequence();
            seq.Join(group.DOFade(0f, hideDuration).SetEase(Ease.Linear));
            if (panel != null) seq.Join(panel.DOScale(0.85f, hideDuration).SetEase(Ease.Linear));
            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
                NotifyClosed();
            });
        }

        /// <summary>Tắt ngay, không chạy hiệu ứng (đổi level...).</summary>
        public void CloseImmediate()
        {
            bool pending = IsOpen || _anim != null; // đang mở, hoặc đang chạy hiệu ứng đóng dở
            IsOpen = false;
            KillAnim();
            gameObject.SetActive(false);
            if (pending) NotifyClosed();
        }

        /// <summary>Vừa mở (trước hiệu ứng mở): lớp con vẽ dữ liệu, chọn nút...</summary>
        protected virtual void OnOpened() { }

        /// <summary>Đã đóng hẳn.</summary>
        protected virtual void OnClosed() { }

        private void NotifyClosed()
        {
            OnClosed();
            Closed?.Invoke(this);
        }

        private Sequence NewSequence()
        {
            KillAnim();
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            seq.OnKill(() => { if (_anim == seq) _anim = null; });
            _anim = seq;
            return seq;
        }

        private void KillAnim()
        {
            _anim?.Kill();
            _anim = null;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
        }
#endif
    }
}
