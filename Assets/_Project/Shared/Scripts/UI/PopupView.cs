using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Shared
{
    /// <summary>
    /// Base popup: nền tối mờ dần + bảng bật lên (scale 0.6 -> vượt nhẹ -> 1, Ease.OutBack). Tween chạy theo thời gian thực
    /// nên không phụ thuộc timeScale. Popup tắt thì hủy tween.
    /// Cấu trúc: root (CanvasGroup) / Dim (Image chắn raycast) / Panel.
    /// Nạp level mới (LevelLoadedEvent) thì popup đang mở tự đóng ngay, trừ khi lớp con đặt CloseOnLevelLoad = false.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupView : MonoBehaviour
    {
        [SerializeField] protected RectTransform panel;
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.15f;

        private CanvasGroup _group;
        private Sequence _anim;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        protected virtual void OnEnable()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
        }

        protected virtual void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            KillAnim();
        }

        /// <summary>Đổi level (kể cả bằng menu debug) thì đóng ngay; popup đang chạy hiệu ứng đóng thì để chạy nốt.</summary>
        protected virtual bool CloseOnLevelLoad => true;

        private void OnLevelLoaded(LevelLoadedEvent _)
        {
            if (IsOpen && CloseOnLevelLoad) CloseImmediate();
        }

        public void Open()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            IsOpen = true;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            OnOpen();
            AudioManager.Play(SoundId.PopupOpen);

            _group.alpha = 0f;
            if (panel != null) panel.localScale = Vector3.one * 0.6f;

            Sequence seq = NewSequence();
            seq.Join(_group.DOFade(1f, showDuration * 0.5f).SetEase(Ease.Linear));
            if (panel != null) seq.Join(panel.DOScale(1f, showDuration).SetEase(Ease.OutBack));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _group.interactable = false;
            AudioManager.Play(SoundId.PopupClose);

            Sequence seq = NewSequence();
            seq.Join(_group.DOFade(0f, hideDuration).SetEase(Ease.Linear));
            if (panel != null) seq.Join(panel.DOScale(0.85f, hideDuration).SetEase(Ease.Linear));
            seq.OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>Tắt ngay, không chạy hiệu ứng (đổi level...).</summary>
        public void CloseImmediate()
        {
            IsOpen = false;
            KillAnim();
            gameObject.SetActive(false);
        }

        protected virtual void OnOpen() { }

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
    }
}
