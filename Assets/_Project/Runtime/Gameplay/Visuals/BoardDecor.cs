using System;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Đồ trang trí bám theo góc khung bảng (hồn ma ló sau góc, mạng nhện...). Khung đổi cỡ theo lưới mỗi level
    /// (GridSpawner), nên mỗi khi cỡ khung đổi thì đặt lại vị trí; có thể nhấp nhô nhẹ (bob).
    /// Lưới nhỏ (vd 1x3 của tutorial) thì thu nhỏ đồ trang trí theo cạnh ngắn của khung để không che mất ô.
    /// </summary>
    public class BoardDecor : MonoBehaviour
    {
        [Serializable]
        private class Item
        {
            public Transform target;
            [Tooltip("Góc / cạnh khung: -1..1 theo mỗi trục (vd (-1, 1) = góc trên trái)")]
            public Vector2 anchor = new Vector2(-1f, 1f);
            [Tooltip("Lệch thêm (world) tính từ điểm neo, ở cỡ gốc (thu nhỏ thì lệch thu theo)")]
            public Vector2 offset;
            [Tooltip("Nhấp nhô lên xuống (world), 0 = đứng yên")]
            public float bob;
            public float bobPeriod = 2.6f;
            [Tooltip("Cạnh dài của đồ trang trí không quá tỉ lệ này × cạnh ngắn của khung (0 = không giới hạn). Chỉ thu nhỏ, không phóng to")]
            [Range(0f, 1f)] public float maxSizeRatio = 0.4f;
        }

        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private Item[] items = new Item[0];

        private Vector2 _size = new Vector2(-1f, -1f);
        private Vector3[] _base = new Vector3[0];
        private Vector3[] _baseScale = new Vector3[0];
        private float[] _shrink = new float[0];

        private void Awake()
        {
            // Scale đặt trong scene là cỡ gốc (lớn nhất)
            _baseScale = new Vector3[items.Length];
            for (int i = 0; i < items.Length; i++)
                _baseScale[i] = items[i].target != null ? items[i].target.localScale : Vector3.one;
        }

        private void LateUpdate()
        {
            if (frame == null) return;
            if (frame.size != _size) Place();
            float t = Time.time;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it.target == null) continue;
                float dy = it.bob > 0f ? Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(0.1f, it.bobPeriod)) * it.bob * _shrink[i] : 0f;
                it.target.position = _base[i] + new Vector3(0f, dy, 0f);
            }
        }

        private void Place()
        {
            _size = frame.size;
            if (_base.Length != items.Length) _base = new Vector3[items.Length];
            if (_shrink.Length != items.Length) _shrink = new float[items.Length];
            Vector3 center = frame.transform.position;
            Vector2 half = _size * 0.5f;
            float boardMin = Mathf.Min(_size.x, _size.y);
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it.target == null) continue;

                // Thu nhỏ theo cạnh ngắn của khung (giữ cỡ gốc khi khung đủ lớn)
                float k = 1f;
                SpriteRenderer sr = it.target.GetComponent<SpriteRenderer>();
                if (it.maxSizeRatio > 0f && sr != null && sr.sprite != null)
                {
                    Vector3 s = Vector3.Scale(sr.sprite.bounds.size, _baseScale[i]);
                    float decorMax = Mathf.Max(s.x, s.y);
                    if (decorMax > 0f) k = Mathf.Min(1f, it.maxSizeRatio * boardMin / decorMax);
                }
                _shrink[i] = k;
                it.target.localScale = _baseScale[i] * k;

                Vector2 p = (Vector2)center + Vector2.Scale(half, it.anchor) + it.offset * k;
                _base[i] = new Vector3(p.x, p.y, it.target.position.z);
            }
        }
    }
}
