using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Phóng to / thu nhỏ và sáng / mờ nhẹ theo hình sin, lặp mãi (quầng sáng sau icon Shop).
    /// Dùng thời gian thật (unscaled) nên vẫn chạy khi game đang dừng.
    /// </summary>
    public class UIPulse : MonoBehaviour
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private float period = 2f;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.92f, 1.08f);
        [SerializeField] private Vector2 alphaRange = new Vector2(0.75f, 1f);

        private Transform _transform;
        private Vector3 _baseScale;
        private Color _baseColor;

        private void Awake()
        {
            _transform = transform;
            _baseScale = _transform.localScale;
            if (graphic != null) _baseColor = graphic.color;
        }

        private void OnDisable()
        {
            _transform.localScale = _baseScale;
            if (graphic != null) graphic.color = _baseColor;
        }

        private void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / period));
            _transform.localScale = _baseScale * Mathf.Lerp(scaleRange.x, scaleRange.y, k);
            if (graphic != null)
            {
                Color c = _baseColor;
                c.a *= Mathf.Lerp(alphaRange.x, alphaRange.y, k);
                graphic.color = c;
            }
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (graphic == null) graphic = GetComponent<Graphic>();
        }
#endif
    }
}
