using System;
using DG.Tweening;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Khung màu phủ lên vùng đang kéo. Khi vùng đổi, tâm và kích thước trượt tới vùng mới trong resizeDuration
    /// (tween DOTween, các mép giãn / co ra) thay vì nhảy ngay. Giãn xong thì báo "settled" (WhenSettled) để
    /// GridManager mới biến khung thành mèo xem trước. Khi đang hiện mèo xem trước thì khung mờ đi.
    /// Bình thường khung ôm sát các ô (mỗi mép thụt vào `inset`). Ngón tay đẩy vào khối đã đặt / ô Void /
    /// ra ngoài lưới thì mép phía đó trượt sang lấp khe rồi dừng ngay trước vật cản (như bị kẹt).
    /// Thả tay ở vùng sai (HideRejected): khung co lại, xám đi và mờ dần rồi mới tắt.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SelectionBoxView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.5f;

        [Header("Chuyển động")]
        [Tooltip("Thời gian khung giãn / co sang vùng mới. Đủ value thì giãn xong mới hiện mèo")]
        [SerializeField, Min(0f)] private float resizeDuration = 0.15f;
        [SerializeField] private Ease resizeEase = Ease.OutQuad;
        [SerializeField] private float fadeSpeed = 12f;          // tốc độ mờ / hiện khi bật tắt mèo xem trước

        [Header("Ôm ô / bị chặn")]
        [SerializeField] private float inset = 0.1f;          // bình thường mỗi mép thụt vào so với ranh giới ô
        [SerializeField] private float blockedExtend = 0.1f;  // bị chặn: mép phía đó trượt ra thêm bao nhiêu (từ vị trí ôm ô)

        [Header("Thả tay ở vùng sai")]
        [SerializeField, Min(0f)] private float rejectDuration = 0.24f;
        [SerializeField, Range(0.5f, 1f)] private float rejectShrink = 0.82f;
        [SerializeField] private Color rejectGray = new Color(0.55f, 0.55f, 0.62f);

        private ColorType _colorType;
        private bool _isSelecting;

        private Vector3 _rectCenter;   // vùng ô (chưa thụt / chưa trượt)
        private Vector2 _rectSize;
        private Vector2Int _blockDir;
        private Vector3 _targetCenter;
        private Vector2 _targetSize;
        private Vector3 _center;
        private Vector2 _size;
        private Vector3 _fromCenter;
        private Vector2 _fromSize;
        private Tween _resize;      // đang giãn / co tới vùng đích
        private Action _onSettled;  // gọi 1 lần khi khung tới vùng đích
        private Tween _reject;      // đang co + xám đi sau khi thả tay ở vùng sai
        private float _gray;        // 0..1 pha xám

        private float _alpha;        // 0..1, nhân với opacity
        private float _targetAlpha = 1f;

        /// <summary>Màu của lượt kéo hiện tại; khối mèo tạo từ lượt kéo này dùng đúng màu này.</summary>
        public ColorType ColorType => _colorType;

        /// <summary>Khung đã nằm đúng vùng đích (không còn giãn / co).</summary>
        public bool IsSettled => _resize == null;

        private void Awake()
        {
            EnsureSpriteRenderer();

            // Luôn đè lên ô và khối mèo đã đặt (thân 20, mặt 21, số 22); dưới bong bóng đếm ô (40)
            spriteRenderer.sortingOrder = 30;

            // Object để tắt sẵn trong scene thì Awake chạy lần đầu ngay trong Show() -> không được ẩn lại
            if (!_isSelecting) Hide();
        }

        private void EnsureSpriteRenderer()
        {
            // Tạo 1 sprite vuông trắng mặc định nếu chưa gán sprite nào
            if (spriteRenderer.sprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                spriteRenderer.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
            }
        }

        /// <summary>Đặt vùng đích (world). Lượt kéo mới thì nhảy ngay tới vùng, các lần sau trượt mượt.</summary>
        public void Show(Vector3 center, Vector2 size)
        {
            EnsureSpriteRenderer();
            center.z = -0.1f; // Nổi lên trên các ô
            _rectCenter = center;
            _rectSize = size;

            if (!_isSelecting || !gameObject.activeSelf)
            {
                // Mỗi lượt kéo chọn mới random 1 màu theo ColorType
                _isSelecting = true;
                KillReject();
                _colorType = ColorTypeExtensions.GetRandomColorType();
                _blockDir = Vector2Int.zero;
                KillResize();
                UpdateTarget();
                _center = _targetCenter;
                _size = _targetSize;
                _alpha = _targetAlpha = 1f;
                gameObject.SetActive(true);
                Apply();
            }
            else
            {
                RetargetResize();
            }
        }

        /// <summary>Hướng ngón tay đang đẩy vào vật cản (-1/0/1 mỗi trục); (0, 0) khi không bị chặn.</summary>
        public void SetBlocked(Vector2Int dir)
        {
            if (dir == _blockDir) return;
            _blockDir = dir;
            if (_isSelecting) RetargetResize();
            else UpdateTarget();
        }

        /// <summary>
        /// Gọi <paramref name="onSettled"/> khi khung tới vùng đích (ngay lập tức nếu đã tới). Chỉ giữ 1 callback:
        /// gọi lại thì thay callback cũ; null = bỏ. Khung bị ẩn / tắt thì callback bị bỏ, không gọi.
        /// </summary>
        public void WhenSettled(Action onSettled)
        {
            _onSettled = onSettled;
            if (onSettled != null && IsSettled) NotifySettled();
        }

        /// <summary>Đích đổi -> giãn / co từ vị trí đang hiển thị tới đích mới.</summary>
        private void RetargetResize()
        {
            Vector3 oldCenter = _targetCenter;
            Vector2 oldSize = _targetSize;
            UpdateTarget();
            if (_resize != null && oldCenter == _targetCenter && oldSize == _targetSize) return; // đang tới đúng đích này

            KillResize();
            if ((_center - _targetCenter).sqrMagnitude < 1e-8f && (_size - _targetSize).sqrMagnitude < 1e-8f) return;

            _fromCenter = _center;
            _fromSize = _size;
            Tween tween = null;
            tween = DOVirtual.Float(0f, 1f, resizeDuration, p =>
                {
                    _center = Vector3.LerpUnclamped(_fromCenter, _targetCenter, p);
                    _size = Vector2.LerpUnclamped(_fromSize, _targetSize, p);
                    Apply();
                })
                .SetEase(resizeEase)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _resize = null;
                    NotifySettled();
                })
                .OnKill(() => { if (_resize == tween) _resize = null; });
            _resize = tween;
        }

        private void NotifySettled()
        {
            Action callback = _onSettled;
            _onSettled = null;
            callback?.Invoke();
        }

        private void KillResize()
        {
            _resize?.Kill();
            _resize = null;
        }

        /// <summary>Vùng ô thụt vào inset mỗi mép; mép phía bị chặn trượt ra thêm blockedExtend.</summary>
        private void UpdateTarget()
        {
            float left = -_rectSize.x / 2f + inset;
            float right = _rectSize.x / 2f - inset;
            float bottom = -_rectSize.y / 2f + inset;
            float top = _rectSize.y / 2f - inset;

            if (_blockDir.x < 0) left -= blockedExtend;
            if (_blockDir.x > 0) right += blockedExtend;
            if (_blockDir.y < 0) bottom -= blockedExtend;
            if (_blockDir.y > 0) top += blockedExtend;

            _targetCenter = _rectCenter + new Vector3((left + right) / 2f, (bottom + top) / 2f, 0f);
            _targetSize = new Vector2(right - left, top - bottom);
        }

        /// <summary>Mờ khung đi khi mèo xem trước đang hiện, hiện lại khi vùng không còn đủ điều kiện.</summary>
        public void SetFilled(bool filled)
        {
            _targetAlpha = filled ? 1f : 0f;
        }

        /// <summary>Thả tay ở vùng sai: khung co lại, chuyển xám và mờ dần rồi tắt (thay vì tắt ngay).</summary>
        public void HideRejected()
        {
            _onSettled = null;
            KillResize();
            if (!_isSelecting || !gameObject.activeSelf || rejectDuration <= 0f)
            {
                Hide();
                return;
            }

            _isSelecting = false; // Update thôi chỉnh alpha; tween dưới tự Apply
            _blockDir = Vector2Int.zero;
            KillReject();
            Vector2 fromSize = _size;
            float fromAlpha = _alpha;
            Tween tween = null;
            tween = DOVirtual.Float(0f, 1f, rejectDuration, p =>
                {
                    _gray = Mathf.Clamp01(p * 3f); // xám ngay đầu, rồi co + mờ
                    _size = fromSize * Mathf.Lerp(1f, rejectShrink, p);
                    _alpha = fromAlpha * (1f - p);
                    Apply();
                })
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _reject = null;
                    gameObject.SetActive(false);
                })
                .OnKill(() => { if (_reject == tween) _reject = null; });
            _reject = tween;
        }

        private void KillReject()
        {
            _reject?.Kill();
            _reject = null;
            _gray = 0f;
        }

        public void Hide()
        {
            _onSettled = null;
            KillReject();
            KillResize();
            _isSelecting = false;
            _blockDir = Vector2Int.zero;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isSelecting) return;

            _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            Apply();
        }

        private void OnDisable()
        {
            _onSettled = null;
            KillResize();
            KillReject();
        }

        private void Apply()
        {
            transform.position = _center;

            if (spriteRenderer.drawMode == SpriteDrawMode.Sliced || spriteRenderer.drawMode == SpriteDrawMode.Tiled)
            {
                spriteRenderer.size = _size;
            }
            else
            {
                transform.localScale = new Vector3(_size.x, _size.y, 1f);
            }

            Color color = _colorType.ToColor(opacity * _alpha);
            if (_gray > 0f)
            {
                Color gray = rejectGray;
                gray.a = color.a;
                color = Color.Lerp(color, gray, _gray);
            }
            spriteRenderer.color = color;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }
#endif
    }
}
