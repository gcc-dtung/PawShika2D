using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hiển thị 1 ô và là đích raycast khi kéo. Dữ liệu nằm ở CellModel (Model).
    /// PlayIntro: hiệu ứng xuất hiện khi vào level (ô nảy từ chấm nhỏ lên, sau đó số / khóa / ô hướng hiện mờ dần).
    /// </summary>
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Hiển thị ô số (object con có sẵn trong Cell.prefab)")]
        [SerializeField] private TextMeshPro valueText;
        [SerializeField] private GameObject lockRoot;
        [SerializeField] private TextMeshPro lockText;
        [SerializeField] private SpriteRenderer orientationBadge; // ô nền sau con số: cao = dọc, vuông = vuông, dẹt = ngang
        [Tooltip("Dấu góc / cạnh: ô số phải nằm ở góc / cạnh này của khối. Sprite phủ cả ô, vẽ ở góc trên trái / cạnh trên; xoay cho các phía khác")]
        [SerializeField] private SpriteRenderer cornerMark;
        [SerializeField] private Sprite cornerSprite;
        [SerializeField] private Sprite edgeSprite;

        [Header("Art ô hướng (bản gốc khung_direc_1/2/3). Để trống thì dùng sprite 9-slice có sẵn của badge")]
        [SerializeField] private Sprite badgeSquareSprite;
        [SerializeField] private Sprite badgeTallSprite;
        [SerializeField] private Sprite badgeWideSprite;
        [SerializeField] private SpriteRenderer[] lockSprites;
        [SerializeField] private Collider2D hitBox;

        // Kích thước ô nền (đơn vị world). Sprite 9-slice có viền lớn nên vẽ to rồi thu nhỏ transform.
        private static readonly Vector2 BadgeTall = new Vector2(0.5f, 0.74f);
        private static readonly Vector2 BadgeSquare = new Vector2(0.64f, 0.64f);
        private static readonly Vector2 BadgeWide = new Vector2(0.74f, 0.5f);
        private const float BadgeScale = 0.25f;

        // Ô số nằm trong vùng đang kéo: số phóng to; kéo / thả sai: số lắc (DragFeedback chọn trục, độ mạnh)
        private const float HighlightScale = 1.22f;
        private const float ShakeDuration = 0.28f;
        private const float ShakeDistance = 0.07f;
        private const float PunchDuration = 0.3f;

        public CellModel Model { get; private set; }
        /// <summary>Collider để raycast chạm (GridSpawner đăng ký vào registry).</summary>
        public Collider2D HitBox => hitBox;
        public Vector2Int Position => Model.Position;

        private Sequence _intro;
        private Tween _bump;
        private Tween _highlight;
        private Tween _shake;
        private Tween _punch;
        private bool _highlighted;
        private float _highlightK = 1f;
        private float _punchK = 1f;
        private Transform _clueRoot;      // số đang hiện, hoặc ổ khóa khi ô còn khóa
        private Vector3 _valueScale, _valuePos, _lockScale, _lockPos, _badgePos, _cornerPos;
        private Color _bodyColor = Color.white;
        private readonly List<TMP_Text> _contentTexts = new List<TMP_Text>();
        private readonly List<SpriteRenderer> _contentSprites = new List<SpriteRenderer>();
        private readonly List<float> _contentAlpha = new List<float>(); // alpha gốc: chữ trước, sprite sau

        private void Awake()
        {
            if (spriteRenderer != null) _bodyColor = spriteRenderer.color;
            if (lockText != null) lockText.color = TextColor.Display(lockText.color); // số trên khóa không bị nhạt (Linear)
            // Nội dung hiện sau ô nền: số, khóa (chữ + hình), ô hướng
            if (valueText != null) _contentTexts.Add(valueText);
            if (lockText != null) _contentTexts.Add(lockText);
            if (lockSprites != null) _contentSprites.AddRange(lockSprites);
            if (orientationBadge != null) _contentSprites.Add(orientationBadge);
            if (cornerMark != null) _contentSprites.Add(cornerMark);
            foreach (TMP_Text t in _contentTexts) _contentAlpha.Add(t.alpha);
            foreach (SpriteRenderer sr in _contentSprites) _contentAlpha.Add(sr.color.a);
            if (valueText != null) { _valueScale = valueText.transform.localScale; _valuePos = valueText.transform.localPosition; }
            if (lockRoot != null) { _lockScale = lockRoot.transform.localScale; _lockPos = lockRoot.transform.localPosition; }
            if (orientationBadge != null) _badgePos = orientationBadge.transform.localPosition;
            if (cornerMark != null) _cornerPos = cornerMark.transform.localPosition;
        }

        private void OnDisable()
        {
            StopIntro();
            StopBump();
            StopClueFeedback();
        }

        /// <summary>Phóng ô nền (giữ tỉ lệ ảnh) cho cạnh dài bằng <paramref name="size"/> (world). GridSpawner gọi 1 lần khi tạo ô.</summary>
        public void SetVisualSize(float size)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            Vector2 bounds = spriteRenderer.drawMode == SpriteDrawMode.Simple ? (Vector2)spriteRenderer.sprite.bounds.size : spriteRenderer.size;
            float k = size / Mathf.Max(bounds.x, bounds.y);
            spriteRenderer.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>Ô số nằm trong / ra khỏi vùng đang kéo: số (hoặc ổ khóa) phóng to nảy nhẹ / thu về.</summary>
        public void SetHighlighted(bool on)
        {
            if (on == _highlighted) return;
            _highlighted = on;
            Transform target = ClueRoot();
            if (target == null) return;

            _highlight?.Kill();
            _highlight = DOVirtual.Float(_highlightK, on ? HighlightScale : 1f, on ? 0.16f : 0.12f, k =>
                {
                    _highlightK = k;
                    ApplyScale(target);
                })
                .SetEase(on ? Ease.OutBack : Ease.OutQuad)
                .SetLink(gameObject);
        }

        /// <summary>Vùng kéo phủ ≥ 2 ô số: số lắc ngang 1 nhịp (báo "không được").</summary>
        public void PlayConflictShake() => PlayShake(Vector2.right);

        /// <summary>
        /// Số (hoặc ổ khóa) cùng ô hướng, dấu góc lắc 1 nhịp theo <paramref name="axis"/>, biên độ tắt dần (báo "không được").
        /// <paramref name="strength"/> nhân với biên độ / thời gian gốc.
        /// </summary>
        public void PlayShake(Vector2 axis, float strength = 1f)
        {
            Transform target = ClueRoot();
            if (target == null || strength <= 0f) return;
            _shake?.Kill();
            Vector3 basePos = IsLock(target) ? _lockPos : _valuePos;
            Transform badge = orientationBadge != null && orientationBadge.gameObject.activeSelf ? orientationBadge.transform : null;
            Transform corner = cornerMark != null && cornerMark.gameObject.activeSelf ? cornerMark.transform : null;
            Vector3 dir = new Vector3(axis.x, axis.y, 0f) * (ShakeDistance * strength);
            _shake = DOVirtual.Float(0f, 1f, ShakeDuration * Mathf.Max(1f, strength), p =>
                {
                    Vector3 offset = dir * (Mathf.Sin(p * Mathf.PI * 6f) * (1f - p));
                    target.localPosition = basePos + offset;
                    if (badge != null) badge.localPosition = _badgePos + offset;
                    if (corner != null) corner.localPosition = _cornerPos + offset;
                })
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    if (target != null) target.localPosition = basePos;
                    if (badge != null) badge.localPosition = _badgePos;
                    if (corner != null) corner.localPosition = _cornerPos;
                });
        }

        /// <summary>Số (hoặc ổ khóa) nảy to thêm <paramref name="amount"/> rồi về (cộng dồn với phóng to khi đang kéo).</summary>
        public void PlayPunch(float amount)
        {
            Transform target = ClueRoot();
            if (target == null || amount <= 0f) return;
            _punch?.Kill();
            _punch = DOVirtual.Float(0f, 1f, PunchDuration, p =>
                {
                    _punchK = FeelCurves.Bump(p, amount);
                    ApplyScale(target);
                })
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    _punchK = 1f;
                    if (target != null) ApplyScale(target);
                });
        }

        private Transform ClueRoot()
        {
            if (valueText != null && valueText.gameObject.activeSelf) return valueText.transform;
            if (lockRoot != null && lockRoot.activeSelf) return lockRoot.transform;
            return null;
        }

        private bool IsLock(Transform t) => lockRoot != null && t == lockRoot.transform;

        private Vector3 BaseScale(Transform t) => IsLock(t) ? _lockScale : _valueScale;

        private void ApplyScale(Transform t) => t.localScale = BaseScale(t) * (_highlightK * _punchK);

        private void StopClueFeedback()
        {
            _highlight?.Kill();
            _highlight = null;
            _shake?.Kill();
            _shake = null;
            _punch?.Kill();
            _punch = null;
            _highlighted = false;
            _highlightK = 1f;
            _punchK = 1f;
            if (valueText != null) { valueText.transform.localScale = _valueScale; valueText.transform.localPosition = _valuePos; }
            if (lockRoot != null) { lockRoot.transform.localScale = _lockScale; lockRoot.transform.localPosition = _lockPos; }
            if (orientationBadge != null) orientationBadge.transform.localPosition = _badgePos;
            if (cornerMark != null) cornerMark.transform.localPosition = _cornerPos;
        }

        /// <summary>
        /// Khối vừa đặt gần đây: sau <paramref name="delay"/>, ô phồng thêm <paramref name="amount"/> rồi về lại
        /// (lên nhanh, xuống chậm). Đang chạy intro thì bỏ qua.
        /// </summary>
        public void PlayBump(float delay, float amount, float duration)
        {
            if (_intro != null || amount <= 0f || duration <= 0f) return;
            StopBump();
            Tween tween = null;
            tween = DOVirtual.Float(0f, 1f, duration, p => transform.localScale = Vector3.one * FeelCurves.Bump(p, amount))
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnKill(() => { if (_bump == tween) _bump = null; });
            _bump = tween;
        }

        private void StopBump()
        {
            if (_bump == null) return;
            _bump.Kill();
            _bump = null;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Ô nền: chờ <paramref name="cellDelay"/>, phóng 0 -> 1 (OutBack) và hiện dần trong <paramref name="cellDuration"/>.
        /// Nội dung: chờ <paramref name="contentDelay"/> (tính từ lúc gọi), hiện mờ dần trong <paramref name="contentDuration"/>.
        /// </summary>
        public void PlayIntro(float cellDelay, float cellDuration, float contentDelay, float contentDuration)
        {
            StopIntro();
            transform.localScale = Vector3.zero;
            SetBodyAlpha(0f);
            SetContentAlpha(0f);

            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.Insert(cellDelay, transform.DOScale(1f, cellDuration).SetEase(Ease.OutBack, 1.6f));
            seq.Insert(cellDelay, DOVirtual.Float(0f, 1f, cellDuration * 0.6f, SetBodyAlpha));
            seq.Insert(contentDelay, DOVirtual.Float(0f, 1f, contentDuration, SetContentAlpha).SetEase(Ease.OutQuad));
            seq.OnKill(() => { if (_intro == seq) _intro = null; });
            _intro = seq;
        }

        /// <summary>Dừng hiệu ứng, ô về trạng thái hiển thị bình thường.</summary>
        public void StopIntro()
        {
            if (_intro == null) return;
            _intro.Kill();
            _intro = null;
            transform.localScale = Vector3.one;
            SetBodyAlpha(1f);
            SetContentAlpha(1f);
        }

        private void SetBodyAlpha(float a)
        {
            if (spriteRenderer == null) return;
            Color c = _bodyColor;
            c.a *= a;
            spriteRenderer.color = c;
        }

        private void SetContentAlpha(float a)
        {
            int i = 0;
            foreach (TMP_Text t in _contentTexts) t.alpha = _contentAlpha[i++] * a;
            foreach (SpriteRenderer sr in _contentSprites)
            {
                Color c = sr.color;
                c.a = _contentAlpha[i++] * a;
                sr.color = c;
            }
        }

        public void Bind(CellModel model)
        {
            Unbind();
            Model = model;
            Model.Changed += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            StopIntro();
            StopBump();
            StopClueFeedback();
            if (Model != null) Model.Changed -= Refresh;
            Model = null;
        }

        /// <summary>
        /// Có art riêng: vẽ Simple, phóng sprite (giữ tỉ lệ) cho vừa trong <paramref name="size"/> (world).
        /// Không có: sprite 9-slice của prefab, vẽ to rồi thu nhỏ transform.
        /// </summary>
        private void SetBadge(Sprite art, Vector2 size)
        {
            if (art != null)
            {
                orientationBadge.sprite = art;
                orientationBadge.drawMode = SpriteDrawMode.Simple;
                Vector2 bounds = art.bounds.size;
                float k = Mathf.Min(size.x / bounds.x, size.y / bounds.y); // giữ tỉ lệ ảnh (góc bo không méo)
                orientationBadge.transform.localScale = new Vector3(k, k, 1f);
                return;
            }
            orientationBadge.transform.localScale = new Vector3(BadgeScale, BadgeScale, 1f);
            orientationBadge.size = size / BadgeScale;
        }

        // [MECHANIC] Hiển thị cơ chế trên ô: object con trong Cell.prefab, bật / tắt theo ClueCell; nhớ thêm vào _contentSprites (Awake) để hiện dần
        private void Refresh()
        {
            ClueCell clue = Model != null ? Model.Clue : null;
            bool hasClue = clue != null;
            bool locked = hasClue && clue.IsLocked;

            if (valueText != null)
            {
                bool showValue = hasClue && !locked;
                valueText.gameObject.SetActive(showValue);
                if (showValue)
                {
                    valueText.SetText(clue.Label); // "?", số, hoặc nhãn số mơ hồ
                }
            }

            if (lockRoot != null)
            {
                lockRoot.SetActive(locked);
                if (locked && lockText != null) lockText.SetText("{0}", clue.LockSteps);
            }

            if (orientationBadge != null)
            {
                // Ô còn khóa: chưa biết gì về clue, kể cả hướng
                bool showBadge = hasClue && !locked && clue.Constraint != ClueConstraint.None;
                orientationBadge.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    Vector2 size = clue.Constraint switch
                    {
                        ClueConstraint.Vertical => BadgeTall,
                        ClueConstraint.Horizontal => BadgeWide,
                        _ => BadgeSquare
                    };
                    Sprite art = clue.Constraint switch
                    {
                        ClueConstraint.Vertical => badgeTallSprite,
                        ClueConstraint.Horizontal => badgeWideSprite,
                        _ => badgeSquareSprite
                    };
                    SetBadge(art, size);
                }
            }

            if (cornerMark != null)
            {
                bool showCorner = hasClue && !locked && clue.Corner != ClueCorner.None;
                cornerMark.gameObject.SetActive(showCorner);
                if (showCorner)
                {
                    bool edge = CornerRules.IsEdge(clue.Corner);
                    Sprite art = edge ? edgeSprite : cornerSprite;
                    if (art != null) cornerMark.sprite = art;
                    cornerMark.transform.localRotation = Quaternion.Euler(0f, 0f, CornerAngle(clue.Corner));
                }
            }
        }

        // Sprite vẽ ở góc trên trái / cạnh trên; xoay ngược chiều kim đồng hồ: 90° -> trái (dưới trái), 180° -> dưới (dưới phải),
        // 270° -> phải (trên phải)
        private static float CornerAngle(ClueCorner corner)
        {
            switch (corner)
            {
                case ClueCorner.BottomLeft:
                case ClueCorner.Left: return 90f;
                case ClueCorner.BottomRight:
                case ClueCorner.Bottom: return 180f;
                case ClueCorner.TopRight:
                case ClueCorner.Right: return 270f;
                default: return 0f;
            }
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (hitBox == null) hitBox = GetComponent<Collider2D>();
            if (lockSprites == null || lockSprites.Length == 0) lockSprites = lockRoot != null ? lockRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        }
#endif
    }
}
