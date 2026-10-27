using System;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Đồ trang trí bám theo góc khung bảng (hồn ma ló sau góc, mạng nhện...). Khung đổi cỡ theo lưới mỗi level
    /// (GridSpawner), nên mỗi khi cỡ khung đổi thì đặt lại vị trí; có thể nhấp nhô nhẹ (bob).
    /// </summary>
    public class BoardDecor : MonoBehaviour
    {
        [Serializable]
        private class Item
        {
            public Transform target;
            [Tooltip("Góc / cạnh khung: -1..1 theo mỗi trục (vd (-1, 1) = góc trên trái)")]
            public Vector2 anchor = new Vector2(-1f, 1f);
            [Tooltip("Lệch thêm (world) tính từ điểm neo")]
            public Vector2 offset;
            [Tooltip("Nhấp nhô lên xuống (world), 0 = đứng yên")]
            public float bob;
            public float bobPeriod = 2.6f;
        }

        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private Item[] items = new Item[0];

        private Vector2 _size = new Vector2(-1f, -1f);
        private Vector3[] _base = new Vector3[0];

        private void LateUpdate()
        {
            if (frame == null) return;
            if (frame.size != _size) Place();
            float t = Time.time;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it.target == null) continue;
                float dy = it.bob > 0f ? Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(0.1f, it.bobPeriod)) * it.bob : 0f;
                it.target.position = _base[i] + new Vector3(0f, dy, 0f);
            }
        }

        private void Place()
        {
            _size = frame.size;
            if (_base.Length != items.Length) _base = new Vector3[items.Length];
            Vector3 center = frame.transform.position;
            Vector2 half = _size * 0.5f;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it.target == null) continue;
                Vector2 p = (Vector2)center + Vector2.Scale(half, it.anchor) + it.offset;
                _base[i] = new Vector3(p.x, p.y, it.target.position.z);
            }
        }
    }
}
