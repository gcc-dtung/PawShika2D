using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Độ trong suốt dao động ngẫu nhiên mượt (Perlin), lặp mãi: ánh đèn cửa sổ nhà ma, quầng nến, quầng bí ngô.
    /// Có thể kèm co giãn nhẹ theo cùng nhịp. Dùng thời gian thật (unscaled) như UIPulse.
    /// </summary>
    public class UIFlicker : MonoBehaviour
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private Vector2 alphaRange = new Vector2(0.55f, 1f);
        [Tooltip("Nhịp đổi (lần / giây), càng lớn càng chập chờn")]
        [SerializeField] private float speed = 1.2f;
        [Tooltip("Co giãn ± theo cùng nhịp (0 = không)")]
        [SerializeField] private float scaleAmount;

        private Color _baseColor;
        private Vector3 _baseScale;
        private float _seed;

        private void Awake()
        {
            _baseColor = graphic.color;
            _baseScale = transform.localScale;
            _seed = Random.value * 100f;
        }

        private void OnDisable()
        {
            graphic.color = _baseColor;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            float k = Mathf.PerlinNoise(_seed, Time.unscaledTime * speed);
            Color c = _baseColor;
            c.a *= Mathf.Lerp(alphaRange.x, alphaRange.y, k);
            graphic.color = c;
            if (scaleAmount != 0f) transform.localScale = _baseScale * (1f + scaleAmount * (k * 2f - 1f));
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
