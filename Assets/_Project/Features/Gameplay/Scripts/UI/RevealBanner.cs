using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Banner "TAP A NUMBER" + nút X khi đang chọn số cho booster Reveal (BoosterManager.onModeChanged). Trượt lên từ dưới khi hiện.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class RevealBanner : MonoBehaviour
    {
        private BoosterManager _boosterManager;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(BoosterManager boosterManager)
        {
            _boosterManager = boosterManager;
        }

        [SerializeField] private RectTransform content;
        [SerializeField] private Button cancelButton;
        [SerializeField] private float slide = -60f; // âm: bắt đầu thấp hơn rồi trượt lên
        [SerializeField] private float duration = 0.2f;

        private CanvasGroup _group;
        private Vector2 _basePos;
        private Sequence _anim;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (content != null) _basePos = content.anchoredPosition;
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            Apply(false, true);
        }

        private void Start()
        {
            if (_boosterManager != null) _boosterManager.onModeChanged.AddListener(OnModeChanged);
        }

        private void OnDestroy()
        {
            if (_boosterManager != null) _boosterManager.onModeChanged.RemoveListener(OnModeChanged);
        }

        private void OnDisable()
        {
            _anim?.Kill();
            _anim = null;
        }

        private void OnModeChanged(BoosterType type, bool on)
        {
            if (type == BoosterType.Reveal) Apply(on, false);
        }

        private void OnCancel()
        {
            if (_boosterManager != null) _boosterManager.CancelModes();
        }

        private void Apply(bool on, bool instant)
        {
            _anim?.Kill();
            _anim = null;
            _group.interactable = on;
            _group.blocksRaycasts = on;

            if (instant)
            {
                _group.alpha = on ? 1f : 0f;
                if (content != null) content.anchoredPosition = _basePos;
                return;
            }

            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.Join(_group.DOFade(on ? 1f : 0f, duration));
            if (content != null)
            {
                if (on) content.anchoredPosition = _basePos + Vector2.up * slide;
                seq.Join(content.DOAnchorPos(on ? _basePos : _basePos + Vector2.up * slide, duration).SetEase(on ? Ease.OutBack : Ease.InQuad));
            }
            seq.OnKill(() => { if (_anim == seq) _anim = null; });
            _anim = seq;
        }
    }
}
