using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Vài ngôi sao lấp lánh hiện ngẫu nhiên trong vùng rect (quanh icon Shop): mỗi sao nghỉ một lúc,
    /// rồi hiện ở vị trí ngẫu nhiên, phóng to - xoay nhẹ - thu nhỏ biến mất, lặp mãi.
    /// Sao là các Image con (sprite sao trắng, tô màu bằng Image.color); Editor tự thu (OnValidate), lúc chạy không tìm.
    /// Dùng thời gian thật (unscaled), không cấp phát mỗi frame.
    /// </summary>
    public class UISparkles : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.7f;
        [SerializeField] private Vector2 delayRange = new Vector2(0.2f, 1.6f);
        [SerializeField] private Vector2 sizeRange = new Vector2(0.6f, 1.1f);
        [SerializeField] private float spin = 90f;

        [Header("Editor tự điền")]
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform[] stars;

        // < 0: đang nghỉ (đếm lên tới 0), 0..1: tiến độ đang hiện
        private float[] _time;
        private float[] _size;

        private void Awake()
        {
            _time = new float[stars.Length];
            _size = new float[stars.Length];
        }

        private void OnEnable()
        {
            if (_time == null) return;
            for (int i = 0; i < stars.Length; i++)
            {
                _time[i] = -Random.Range(0f, delayRange.y);
                stars[i].localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < stars.Length; i++)
            {
                RectTransform s = stars[i];
                if (_time[i] < 0f)
                {
                    _time[i] += dt;
                    if (_time[i] >= 0f) Spawn(i);
                    continue;
                }

                _time[i] += dt / lifetime;
                if (_time[i] >= 1f)
                {
                    _time[i] = -Random.Range(delayRange.x, delayRange.y);
                    s.localScale = Vector3.zero;
                    continue;
                }

                float k = Mathf.Sin(_time[i] * Mathf.PI);
                s.localScale = new Vector3(k, k, 1f) * _size[i];
                s.localEulerAngles = new Vector3(0f, 0f, _time[i] * spin);
            }
        }

        private void Spawn(int i)
        {
            _time[i] = 0f;
            _size[i] = Random.Range(sizeRange.x, sizeRange.y);
            Rect r = area.rect;
            stars[i].anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            area = (RectTransform)transform;
            if (stars != null && stars.Length == transform.childCount) return;
            stars = new RectTransform[transform.childCount];
            for (int i = 0; i < stars.Length; i++) stars[i] = (RectTransform)transform.GetChild(i);
        }
#endif
    }
}
