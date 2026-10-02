using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragAndDropController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayer;
    [SerializeField] private DragCountBubble countBubble;

    private Cell _currentCell;
    private Cell _targetCell;
    private bool _isDragging = false;

    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        // Nếu cellLayer đang là Nothing (0), tự động lấy Layer "Cell" hoặc toàn bộ layers
        if (cellLayer.value == 0)
        {
            int mask = LayerMask.GetMask("Cell");
            cellLayer = mask != 0 ? mask : ~0;
        }

        if (countBubble == null)
        {
            Debug.LogError("[DragAndDropController] Chưa gán DragCountBubble");
        }
    }

    private Cell GetCellAtPosition(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        // 1. Thử Physics2D (nếu dùng BoxCollider2D)
        Collider2D hit2D = Physics2D.OverlapPoint(worldPos, cellLayer);
        if (hit2D != null && hit2D.TryGetComponent(out Cell cell2D))
        {
            return cell2D;
        }

        // 2. Thử Physics 3D (nếu Cell đang gắn BoxCollider 3D)
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit3D, Mathf.Infinity, cellLayer))
        {
            if (hit3D.collider.TryGetComponent(out Cell cell3D))
            {
                return cell3D;
            }
        }

        return null;
    }

    public void StartDrag(Vector2 pos)
    {
        if (_isDragging) return;
        if (GridManager.Instance != null && GridManager.Instance.IsInputLocked) return;

        Cell cell = GetCellAtPosition(pos);
        if (cell == null) return;

        _currentCell = cell;
        _targetCell = cell;
        _isDragging = true;

        GridManager.Instance.OnDrawGrid(_currentCell, _targetCell);
        UpdateBubble(pos);
    }

    public void Drag(Vector2 pos)
    {
        if (!_currentCell || !_isDragging) return;
        if (GridManager.Instance != null && GridManager.Instance.IsInputLocked)
        {
            CancelDrag();
            return;
        }

        Cell cell = GetCellAtPosition(pos);
        if (cell != null && cell != _targetCell)
        {
            _targetCell = cell;
            GridManager.Instance.OnDrawGrid(_currentCell, _targetCell);
        }
        UpdateBubble(pos);
    }

    public void EndDrag()
    {
        if (!_currentCell || !_isDragging) return;

        if (GridManager.Instance != null && GridManager.Instance.IsInputLocked)
        {
            CancelDrag();
            return;
        }

        if (_targetCell != null)
        {
            GridManager.Instance.OnSelect(_currentCell, _targetCell);
        }
        else
        {
            GridManager.Instance.HideSelectionBox();
        }

        ResetDrag();
    }

    private void CancelDrag()
    {
        if (GridManager.Instance != null) GridManager.Instance.HideSelectionBox();
        ResetDrag();
    }

    private void ResetDrag()
    {
        _currentCell = null;
        _targetCell = null;
        _isDragging = false;
        if (countBubble != null) countBubble.Hide();
    }

    private void UpdateBubble(Vector2 screenPos)
    {
        if (countBubble == null || cam == null || GridManager.Instance == null) return;
        int count = GridManager.Instance.CountCellsInRange(_currentCell, _targetCell);
        countBubble.Show(cam.ScreenToWorldPoint(screenPos), count);
    }
}
