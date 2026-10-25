using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Ảnh nền UI luôn phủ kín RectTransform cha (panel / canvas) trên mọi tỉ lệ màn hình.
    /// Kích thước đặt trong scene là mức tối thiểu: đã phủ kín thì giữ nguyên, chưa kín (máy dài như iPhone X)
    /// thì phóng đều 2 chiều vừa đủ để phủ kín, giữ tỉ lệ ảnh. Tính cả độ lệch tâm của ảnh so với cha.
    /// Màn hình / kích thước cha đổi thì tự tính lại.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BackgroundCover : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _parent;
        private Vector3 _baseScale;
        private Vector2 _lastParentSize = new Vector2(-1f, -1f);

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _parent = _rect.parent as RectTransform;
            _baseScale = _rect.localScale;
        }

        private void OnEnable()
        {
            _lastParentSize = new Vector2(-1f, -1f);
        }

        private void LateUpdate()
        {
            if (_parent == null) return;
            Vector2 parentSize = _parent.rect.size;
            if (parentSize == _lastParentSize) return;
            _lastParentSize = parentSize;
            Fit(parentSize);
        }

        private void Fit(Vector2 parentSize)
        {
            Vector2 size = Vector2.Scale(_rect.rect.size, _baseScale);
            if (size.x <= 0f || size.y <= 0f || parentSize.x <= 0f || parentSize.y <= 0f) return;

            // Tâm ảnh so với tâm cha (ảnh lệch tâm thì cần to hơn để phủ mép xa)
            Vector2 center = (Vector2)_rect.localPosition + Vector2.Scale(_rect.rect.center, _baseScale);
            Vector2 offset = center - _parent.rect.center;
            float kx = (parentSize.x + 2f * Mathf.Abs(offset.x)) / size.x;
            float ky = (parentSize.y + 2f * Mathf.Abs(offset.y)) / size.y;
            float k = Mathf.Max(1f, kx, ky); // chỉ phóng to, không thu nhỏ

            _rect.localScale = _baseScale * k;
        }
    }
}
