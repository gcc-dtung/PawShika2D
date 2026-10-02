using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragAndDropController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayer;
    [SerializeField] private DragCountBubble countBubble;

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

    private CellView GetCellAtPosition(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        // 1. Thử Physics2D (nếu dùng BoxCollider2D)
        Collider2D hit2D = Physics2D.OverlapPoint(worldPos, cellLayer);
        if (hit2D != null && hit2D.TryGetComponent(out CellView cell2D))
        {
            return cell2D;
        }

        // 2. Thử Physics 3D (nếu CellView đang gắn BoxCollider 3D)
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit3D, Mathf.Infinity, cellLayer))
        {
            if (hit3D.collider.TryGetComponent(out CellView cell3D))
            {
                return cell3D;
            }
        }

        return null;
    }

    public void StartDrag(Vector2 pos)
    {
        if (_isDragging) return;
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.IsInputLocked) return;

        // Bắt đầu phải chạm trúng 1 ô; ô đã có khối thì GridManager từ chối
        CellView cell = GetCellAtPosition(pos);
        if (cell == null || !gm.BeginSelection(cell.Position)) return;

        _isDragging = true;
        UpdateBubble(pos);
    }

    public void Drag(Vector2 pos)
    {
        if (!_isDragging) return;
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.IsInputLocked)
        {
            CancelDrag();
            return;
        }

        // Không cần trúng ô: ngón tay trên ô Void, trên khối hay ra ngoài lưới vẫn được quy về ô gần nhất
        gm.UpdateSelection(ScreenToWorld(pos));
        UpdateBubble(pos);
    }

    public void EndDrag()
    {
        if (!_isDragging) return;
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.IsInputLocked)
        {
            CancelDrag();
            return;
        }

        gm.EndSelection();
        ResetDrag();
    }

    private void CancelDrag()
    {
        if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
        ResetDrag();
    }

    private void ResetDrag()
    {
        _isDragging = false;
        if (countBubble != null) countBubble.Hide();
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        return cam != null ? cam.ScreenToWorldPoint(screenPos) : Vector3.zero;
    }

    private void UpdateBubble(Vector2 screenPos)
    {
        if (countBubble == null || GridManager.Instance == null) return;
        countBubble.Show(ScreenToWorld(screenPos), GridManager.Instance.SelectionCellCount);
    }
}
