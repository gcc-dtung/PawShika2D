using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Trôi nhẹ quanh vị trí gốc theo hình sin, lặp mãi (mây, con ma, nhà ma Halloween ở Home).
    /// Lệch vị trí (anchoredPosition), lắc góc và co giãn có chu kỳ riêng; phase lệch nhau để nhiều vật không chạy đồng bộ.
    /// Dùng thời gian thật (unscaled) như UIPulse.
    /// </summary>
    public class UIFloat : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [Tooltip("Biên độ lệch (đơn vị canvas)")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 12f);
        [SerializeField] private float period = 3f;
        [Tooltip("Lắc góc ± độ (0 = không lắc)")]
        [SerializeField] private float tilt;
        [SerializeField] private float tiltPeriod = 4f;
        [Tooltip("Co giãn: x = ngang, y = dọc (thở / nhún). 0 = không")]
        [SerializeField] private Vector2 squash;
        [SerializeField] private float squashPeriod = 1.5f;
        [Tooltip("Lệch pha 0..1 (mỗi vật một số)")]
        [SerializeField, Range(0f, 1f)] private float phase;

        private Vector2 _basePos;
        private Vector3 _baseScale;
        private Quaternion _baseRot;

        private void Awake()
        {
            _basePos = rect.anchoredPosition;
            _baseScale = rect.localScale;
            _baseRot = rect.localRotation;
        }

        private void OnDisable()
        {
            rect.anchoredPosition = _basePos;
            rect.localScale = _baseScale;
            rect.localRotation = _baseRot;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float k = Mathf.Sin((t / period + phase) * 2f * Mathf.PI);
            rect.anchoredPosition = _basePos + offset * k;
            if (tilt != 0f) rect.localRotation = _baseRot * Quaternion.Euler(0f, 0f, tilt * Mathf.Sin((t / tiltPeriod + phase) * 2f * Mathf.PI));
            if (squash != Vector2.zero)
            {
                float s = Mathf.Sin((t / squashPeriod + phase) * 2f * Mathf.PI);
                rect.localScale = Vector3.Scale(_baseScale, new Vector3(1f + squash.x * s, 1f - squash.y * s, 1f));
            }
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (rect == null) rect = transform as RectTransform;
        }
#endif
    }
}
