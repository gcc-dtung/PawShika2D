using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phản hồi trên lưới trong lúc kéo: ô số nằm trong vùng kéo phóng to (biết mình đang "ôm" ô số nào);
/// vùng vừa phủ từ 2 ô số trở lên thì các số đó lắc ngang 1 nhịp (báo không được).
/// Bong bóng đếm ô / khung chọn / âm thanh do DragCountBubble, SelectionBoxView, GameAudioEvents lo.
/// </summary>
public class DragFeedback : MonoBehaviour
{
    [SerializeField] private bool clueHighlight = true;

    private GridSpawner _spawner;
    private List<CellView> _lit = new List<CellView>();
    private List<CellView> _next = new List<CellView>();
    private bool _conflict;

    private void Start()
    {
        _spawner = FindFirstObjectByType<GridSpawner>();
        if (_spawner != null) _spawner.gridCellEvent.AddListener(OnGridSpawned);
        if (GridManager.Instance != null) GridManager.Instance.onSelectionChanged.AddListener(OnSelectionChanged);
    }

    private void OnDestroy()
    {
        if (_spawner != null) _spawner.gridCellEvent.RemoveListener(OnGridSpawned);
        if (GridManager.Instance != null) GridManager.Instance.onSelectionChanged.RemoveListener(OnSelectionChanged);
    }

    private void OnDisable() => ClearAll();

    private void OnGridSpawned(List<CellView> cells)
    {
        _lit.Clear(); // ô cũ đã bị xóa / Unbind (tự trả số về cỡ gốc)
        _conflict = false;
    }

    private void OnSelectionChanged(int count, SelectionHint hint)
    {
        GridManager grid = GridManager.Instance;
        if (!clueHighlight || count == 0 || grid == null || !grid.IsSelecting || _spawner == null || _spawner.GetGrid == null)
        {
            ClearAll();
            return;
        }

        RectInt rect = grid.SelectionRect;
        _next.Clear();
        foreach (CellView cell in _spawner.GetGrid)
        {
            if (cell != null && cell.Model != null && cell.Model.HasClue && rect.Contains(cell.Position)) _next.Add(cell);
        }

        foreach (CellView cell in _lit)
        {
            if (cell != null && !_next.Contains(cell)) cell.SetHighlighted(false);
        }
        foreach (CellView cell in _next) cell.SetHighlighted(true);
        (_lit, _next) = (_next, _lit);

        bool conflict = _lit.Count > 1;
        if (conflict && !_conflict)
        {
            foreach (CellView cell in _lit) cell.PlayConflictShake();
        }
        _conflict = conflict;
    }

    private void ClearAll()
    {
        foreach (CellView cell in _lit)
        {
            if (cell != null) cell.SetHighlighted(false);
        }
        _lit.Clear();
        _conflict = false;
    }
}
