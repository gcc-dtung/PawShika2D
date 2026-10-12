using UnityEngine;
using UnityEngine.EventSystems;

namespace PawBlock.Game
{
    /// <summary>
    /// Đặt trên Panels (cha của các panel Main Menu): vuốt ngang để chuyển tab qua MainMenuNavigator.
    /// Nhận kéo từ mọi thứ bên trong (nền panel, nút...): kéo quá ngưỡng thì EventSystem hủy click của nút đang ấn.
    /// Kéo dọc nhiều hơn ngang thì bỏ qua (để dành cho danh sách cuộn dọc). Vận tốc tính theo thời gian thực, làm mượt.
    /// </summary>
    public class MenuSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private MainMenuNavigator navigator;
        [Tooltip("Làm mượt vận tốc (0 = chỉ lấy frame cuối)")]
        [SerializeField, Range(0f, 0.95f)] private float velocitySmoothing = 0.6f;

        private RectTransform _rect;
        private bool _active;
        private Vector2 _start;
        private float _lastX;
        private float _velocity;
        private float _lastTime;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnDisable()
        {
            if (_active && navigator != null) navigator.EndSwipe(0f);
            _active = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _active = false;
            if (navigator == null || eventData.button != PointerEventData.InputButton.Left) return;

            Vector2 delta = eventData.position - eventData.pressPosition;
            if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return; // kéo dọc

            if (!ToLocal(eventData.pressPosition, eventData.pressEventCamera, out _start)) return;
            if (!navigator.BeginSwipe()) return;
            _active = true;
            _lastX = _start.x;
            _lastTime = Time.unscaledTime;
            _velocity = 0f;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_active || !ToLocal(eventData.position, eventData.pressEventCamera, out Vector2 local)) return;

            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            float v = (local.x - _lastX) / dt;
            _velocity = Mathf.Lerp(v, _velocity, velocitySmoothing);
            _lastX = local.x;
            _lastTime = Time.unscaledTime;
            navigator.Swipe(local.x - _start.x);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_active) return;
            _active = false;
            // Dừng tay một lúc rồi mới thả: không tính là vuốt nhanh
            float v = Time.unscaledTime - _lastTime > 0.08f ? 0f : _velocity;
            navigator.EndSwipe(v);
        }

        private bool ToLocal(Vector2 screen, Camera cam, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, cam, out local);
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
        }
#endif
    }
}
