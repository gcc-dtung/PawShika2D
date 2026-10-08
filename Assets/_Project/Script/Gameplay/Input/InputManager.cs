using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Đọc con trỏ (chuột / ngón chạm đầu tiên) bằng Input System mới và chuyển cho DragAndDropController.
/// Pointer.current là thiết bị trỏ dùng gần nhất nên chuột và cảm ứng chạy chung một nhánh.
/// </summary>
public class InputManager : MonoBehaviour
{
    [SerializeField] private DragAndDropController dragAndDropController;

    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    private PointerEventData _uiPointer;
    private EventSystem _uiPointerOwner;

    private void Awake()
    {
        if (dragAndDropController == null)
        {
            dragAndDropController = GetComponent<DragAndDropController>();
            if (dragAndDropController == null)
            {
                dragAndDropController = FindObjectOfType<DragAndDropController>();
            }
        }
    }

    private void Update()
    {
        if (dragAndDropController == null) return;
        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        var press = pointer.press;
        Vector2 pos = pointer.position.ReadValue();
        if (press.wasPressedThisFrame)
        {
            // Bấm trúng nút / popup trên Canvas thì không bắt đầu kéo trên lưới
            if (!IsPointerOverUI(pos)) dragAndDropController.StartDrag(pos);
        }
        else if (press.isPressed)
            dragAndDropController.Drag(pos);
        else if (press.wasReleasedThisFrame)
            dragAndDropController.EndDrag();
    }

    // Tự raycast UI thay vì EventSystem.IsPointerOverGameObject: với InputSystemUIInputModule, ở frame vừa chạm
    // module có thể chưa cập nhật con trỏ nên trả sai
    private bool IsPointerOverUI(Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (!es) return false;
        if (_uiPointer == null || _uiPointerOwner != es)
        {
            _uiPointer = new PointerEventData(es);
            _uiPointerOwner = es;
        }
        _uiPointer.position = screenPos;
        es.RaycastAll(_uiPointer, _uiHits);
        bool hit = false;
        foreach (RaycastResult r in _uiHits)
        {
            if (r.module is UnityEngine.UI.GraphicRaycaster) { hit = true; break; } // bỏ qua Physics raycaster (ô lưới)
        }
        _uiHits.Clear();
        return hit;
    }
}
