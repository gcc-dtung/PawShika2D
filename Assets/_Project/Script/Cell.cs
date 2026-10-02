


using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ClueCell clueCell;
    private Vector2Int _cellPos;

    public PlacedBlock CurrentPlacedBlock { get; private set; }
    public bool IsOccupied => CurrentPlacedBlock != null;
    public ClueCell ClueCell => clueCell;
    public bool HasClue => clueCell != null && clueCell.HasData;

    private void Awake()
    {
        EnsureClueCell();
    }

    private void EnsureClueCell()
    {
        if (clueCell == null)
        {
            clueCell = GetComponent<ClueCell>();
            if (clueCell == null)
            {
                clueCell = GetComponentInChildren<ClueCell>(true);
            }
        }
    }

    public void SetPositionInCell(int x, int y)
    {
        _cellPos = new Vector2Int(x, y);
    }

    public Vector2Int GetPositionInCell() => _cellPos;

    public bool IsCellInRange(int xMin, int xMax, int yMin, int yMax)
    {
        return (_cellPos.x >= xMin && _cellPos.x <= xMax && _cellPos.y >= yMin && _cellPos.y <= yMax);
    }

    public void SetPlacedBlock(PlacedBlock block)
    {
        CurrentPlacedBlock = block;
    }

    public void ClearPlacedBlock()
    {
        CurrentPlacedBlock = null;
    }

    public void SetClue(ClueData clueData)
    {
        EnsureClueCell();
        if (clueCell == null)
        {
            clueCell = gameObject.AddComponent<ClueCell>();
        }
        clueCell.SetClueData(clueData);
    }

    public void ClearClue()
    {
        EnsureClueCell();
        if (clueCell != null)
        {
            clueCell.ClearClue();
        }
    }

    public void OnDeselect()
    {
        // Giữ nguyên màu gốc của Cell
    }
}
