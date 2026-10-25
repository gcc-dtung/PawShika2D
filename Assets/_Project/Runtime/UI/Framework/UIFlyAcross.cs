using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Bay / trôi từ <c>from</c> tới <c>to</c> (anchoredPosition) kèm lượn sóng dọc, nghỉ ngẫu nhiên rồi lặp (dơi trên trời, hồn ma nhỏ đi ngang sân).
    /// - flap: vỗ cánh bằng co dọc nhanh (dơi); 0 = không.
    /// - pingPong: lượt sau đi ngược lại và lật ngang (ma đi qua đi lại); không thì luôn bay một chiều và ẩn khi nghỉ.
    /// Dùng thời gian thật (unscaled) như UIPulse.
    /// </summary>
    public class UIFlyAcross : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Vector2 from = new Vector2(-700f, 0f);
        [SerializeField] private Vector2 to = new Vector2(700f, 150f);
        [SerializeField] private float duration = 6f;
        [Tooltip("Nghỉ ngẫu nhiên giữa 2 lượt (giây)")]
        [SerializeField] private Vector2 restRange = new Vector2(2f, 6f);
        [Tooltip("Lượn sóng: x = biên độ dọc, y = số sóng mỗi lượt")]
        [SerializeField] private Vector2 wave = new Vector2(30f, 2f);
        [Tooltip("Co dọc khi vỗ cánh (0 = không), flapRate lần / giây")]
        [SerializeField] private float flap;
        [SerializeField] private float flapRate = 6f;
        [Tooltip("Nhún nhẹ khi đi (ma): biên độ dọc, số nhún / giây")]
        [SerializeField] private Vector2 hop;
        [SerializeField] private bool pingPong;
        [Tooltip("Lượt đầu chờ ngẫu nhiên trong khoảng này")]
        [SerializeField] private Vector2 firstDelay = new Vector2(0f, 3f);
        [Tooltip("Mờ dần ở đầu / cuối lượt (tỉ lệ thời gian)")]
        [SerializeField, Range(0f, 0.5f)] private float fade = 0.1f;

        private Vector3 _baseScale;
        private float _start;
        private float _restUntil;
        private bool _reverse;

        private void Awake()
        {
            _baseScale = rect.localScale;
        }

        private void OnEnable()
        {
            _restUntil = Time.unscaledTime + Random.Range(firstDelay.x, firstDelay.y);
            _start = -1f;
            Hide();
        }

        private void OnDisable()
        {
            rect.localScale = _baseScale;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (_start < 0f)
            {
                if (now < _restUntil) return;
                _start = now;
            }

            float p = (now - _start) / duration;
            if (p >= 1f)
            {
                _start = -1f;
                _restUntil = now + Random.Range(restRange.x, restRange.y);
                if (pingPong) _reverse = !_reverse;
                else Hide();
                return;
            }

            Vector2 a = _reverse ? to : from;
            Vector2 b = _reverse ? from : to;
            Vector2 pos = Vector2.Lerp(a, b, p);
            pos.y += wave.x * Mathf.Sin(p * wave.y * 2f * Mathf.PI);
            if (hop.x != 0f) pos.y += hop.x * Mathf.Abs(Mathf.Sin((now - _start) * hop.y * Mathf.PI));
            rect.anchoredPosition = pos;

            float dir = (b.x >= a.x) ? 1f : -1f;
            float sy = flap != 0f ? 1f - flap * (0.5f + 0.5f * Mathf.Sin((now - _start) * flapRate * 2f * Mathf.PI)) : 1f;
            rect.localScale = new Vector3(Mathf.Abs(_baseScale.x) * dir, _baseScale.y * sy, _baseScale.z);

            if (group != null)
            {
                float f = fade > 0f ? Mathf.Clamp01(Mathf.Min(p, 1f - p) / fade) : 1f;
                group.alpha = pingPong ? 1f : f;
            }
        }

        private void Hide()
        {
            if (!pingPong && group != null) group.alpha = 0f;
            if (!pingPong) rect.anchoredPosition = from;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (rect == null) rect = transform as RectTransform;
            if (group == null) group = GetComponent<CanvasGroup>();
        }
#endif
    }
}
