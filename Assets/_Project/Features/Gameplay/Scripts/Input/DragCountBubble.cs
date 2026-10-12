using DG.Tweening;
using TMPro;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Bong bóng hiện số ô đang chọn, bám theo ngón tay khi kéo.
    /// Hình nền và chữ là object có sẵn trong DragCountBubble.prefab.
    /// Hiện ra thì phóng to từ 0 (nảy nhẹ), ẩn đi thì thu nhỏ về 0 rồi mới tắt; số đổi thì chữ nảy lên.
    /// Màu nền theo trạng thái vùng kéo: đang kéo (màu gốc) / đủ (xanh) / sai chắc chắn (đỏ).
    /// Bám ngón tay có làm mượt (lerp theo thời gian) để ngón tay rung không làm bong bóng giật.
    /// </summary>
    public class DragCountBubble : MonoBehaviour
    {
        [SerializeField] private TextMeshPro countText;
        [SerializeField] private SpriteRenderer background; // Editor tự điền SpriteRenderer trên chính object
        [SerializeField] private Vector3 offset = new Vector3(-0.6f, 0.9f, 0f);
        [Tooltip("Độ bám ngón tay: càng lớn càng sát (0 = dính cứng như cũ)")]
        [SerializeField, Min(0f)] private float followSharpness = 22f;

        [Header("Hiệu ứng")]
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float hideDuration = 0.14f;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [Tooltip("Số đổi: chữ to thêm bao nhiêu (0.2 = 20%) rồi về lại")]
        [SerializeField, Min(0f)] private float countPunch = 0.2f;
        [SerializeField, Min(0f)] private float countPunchDuration = 0.16f;

        [Header("Màu theo trạng thái (đang kéo = màu nền trong prefab)")]
        [SerializeField] private Color readyColor = new Color(0.36f, 0.78f, 0.45f, 0.95f);
        [SerializeField] private Color invalidColor = new Color(0.93f, 0.38f, 0.42f, 0.95f);
        [SerializeField, Min(0f)] private float colorDuration = 0.12f;

        // Object để tắt sẵn trong scene; không ẩn trong Awake vì Awake chạy lần đầu ngay trong Show()
        private int _lastCount = -1;
        private bool _visible;
        private bool _initialized;
        private Vector3 _baseScale;      // scale gốc của bong bóng (prefab / scene)
        private Vector3 _textBaseScale;
        private Tween _scaleTween;       // phóng to khi hiện / thu nhỏ khi ẩn
        private Tween _punchTween;       // chữ nảy khi số đổi
        private Tween _colorTween;
        private Color _neutralColor;
        private SelectionHint _hint;
        private Vector3 _targetPos;

        private void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;
            _baseScale = transform.localScale;
            _textBaseScale = countText.transform.localScale;
            if (background != null) _neutralColor = background.color;
        }

        public void Show(Vector3 worldPos, int count, SelectionHint hint = SelectionHint.Neutral)
        {
            EnsureInit();
            _targetPos = new Vector3(worldPos.x, worldPos.y, -0.5f) + offset;

            if (!_visible)
            {
                _visible = true;
                transform.position = _targetPos; // hiện ra ngay dưới ngón tay, không trượt từ chỗ cũ
                if (!gameObject.activeSelf)
                {
                    transform.localScale = Vector3.zero;
                    gameObject.SetActive(true);
                }

                // Đang thu nhỏ dở thì phóng to lại từ cỡ hiện tại
                KillScale();
                _scaleTween = transform.DOScale(_baseScale, showDuration).SetEase(showEase).SetLink(gameObject);

                // Số đầu tiên đi cùng hiệu ứng phóng to, không nảy thêm
                _lastCount = count;
                countText.SetText("{0}", count);
                SetHint(hint, true);
                return;
            }

            if (count != _lastCount)
            {
                _lastCount = count;
                countText.SetText("{0}", count);
                PunchText();
            }
            SetHint(hint, false);
        }

        private void Update()
        {
            if (!_visible) return; // đang thu nhỏ để ẩn: đứng yên chỗ cũ
            if (followSharpness <= 0f)
            {
                transform.position = _targetPos;
                return;
            }
            // Lerp không phụ thuộc FPS: sau 1 giây còn cách đích e^-sharpness
            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _targetPos, k);
        }

        private void SetHint(SelectionHint hint, bool instant)
        {
            if (background == null || (!instant && hint == _hint)) return;
            _hint = hint;
            Color target = hint == SelectionHint.Ready ? readyColor
                : hint == SelectionHint.Invalid ? invalidColor
                : _neutralColor;

            KillColor();
            if (instant || colorDuration <= 0f) background.color = target;
            else _colorTween = background.DOColor(target, colorDuration).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            _lastCount = -1;

            KillScale();
            _scaleTween = transform.DOScale(Vector3.zero, hideDuration).SetEase(hideEase).SetLink(gameObject)
                .OnComplete(() =>
                {
                    _scaleTween = null;
                    gameObject.SetActive(false);
                });
        }

        private void PunchText()
        {
            KillPunch();
            if (countPunch <= 0f || countPunchDuration <= 0f) return;
            _punchTween = countText.transform
                .DOPunchScale(_textBaseScale * countPunch, countPunchDuration, 1, 0.5f)
                .SetLink(gameObject);
        }

        private void KillScale()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        private void KillColor()
        {
            _colorTween?.Kill();
            _colorTween = null;
        }

        private void KillPunch()
        {
            _punchTween?.Kill();
            _punchTween = null;
            countText.transform.localScale = _textBaseScale;
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            // Bị tắt giữa chừng (vd. cha bị tắt): lần sau hiện lại từ đầu
            KillScale();
            KillPunch();
            KillColor();
            _visible = false;
            _lastCount = -1;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
        }
#endif
    }
}
