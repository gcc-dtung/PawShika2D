using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] private DragAndDropController dragAndDropController;

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

        if (Input.GetMouseButtonDown(0))
        {
            // Bấm trúng nút / popup trên Canvas thì không bắt đầu kéo trên lưới
            if (!IsPointerOverUI()) dragAndDropController.StartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
            dragAndDropController.Drag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            dragAndDropController.EndDrag();
    }

    private static bool IsPointerOverUI()
    {
        EventSystem es = EventSystem.current;
        if (!es) return false;
        if (Input.touchCount > 0) return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return es.IsPointerOverGameObject();
    }
}
