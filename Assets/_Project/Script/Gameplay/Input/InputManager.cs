using System;
using UnityEngine;

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
            dragAndDropController.StartDrag(Input.mousePosition);
        else if (Input.GetMouseButton(0))
            dragAndDropController.Drag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            dragAndDropController.EndDrag();
    }
}
