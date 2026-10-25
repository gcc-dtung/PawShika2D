using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// ScrollRect cuộn dọc nằm trong vùng vuốt ngang (MenuSwipe): bắt đầu kéo mà ngang nhiều hơn dọc thì chuyển cả lượt kéo
    /// lên cha (vuốt chuyển tab), không thì cuộn như thường.
    /// </summary>
    public class NestedScrollRect : ScrollRect
    {
        private bool _routeToParent;

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 d = eventData.position - eventData.pressPosition;
            _routeToParent = Mathf.Abs(d.x) > Mathf.Abs(d.y);
            if (_routeToParent) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
            else base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_routeToParent) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.dragHandler);
            else base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToParent) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.endDragHandler);
            else base.OnEndDrag(eventData);
            _routeToParent = false;
        }
    }
}
