using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class GridSpawner : MonoBehaviour
{
    public const float CellSpacing = 1.2f; // cellGap + cellSize

    [SerializeField] private Cell cellPrefab;
    [SerializeField] private LevelDefinition levelData;
    [SerializeField] private int cellPoolCapacity = 144; // lưới lớn nhất 12x12
    public UnityEvent<List<Cell>> gridCellEvent = new UnityEvent<List<Cell>>();

    private readonly List<Cell> gridCells = new List<Cell>();
    private readonly Dictionary<Vector2Int, ClueData> _clueLookup = new Dictionary<Vector2Int, ClueData>();
    private readonly HashSet<Vector2Int> _voidLookup = new HashSet<Vector2Int>();
    private ObjectPool<Cell> _cellPool;

    public LevelDefinition LevelData => levelData;
    public List<Cell> GetGrid => gridCells;

    private void Awake()
    {
        // Ô được tái sử dụng giữa các level thay vì Instantiate / Destroy mỗi lần
        _cellPool = new ObjectPool<Cell>(
            createFunc: () => Instantiate(cellPrefab, transform),
            actionOnGet: c => c.gameObject.SetActive(true),
            actionOnRelease: c =>
            {
                c.ClearClue();
                c.ClearPlacedBlock();
                c.gameObject.SetActive(false);
            },
            // Thoát Play mode: scene hủy object trước, rồi Editor mới Clear() pool -> object có thể đã null
            actionOnDestroy: c => { if (c != null) Destroy(c.gameObject); },
            collectionCheck: false,
            defaultCapacity: cellPoolCapacity,
            maxSize: cellPoolCapacity * 2);
    }

    private void Start()
    {
        // Có LevelManager thì để LevelManager quyết định level nào được spawn
        if (LevelManager.Instance == null)
        {
            Spawn(levelData);
        }
    }

    public void SetLevelData(LevelDefinition data)
    {
        levelData = data;
    }

    public void Spawn(LevelDefinition data)
    {
        if (data == null) return;

        levelData = data;
        DeleteGrid();
        BuildLookups(data);

        int height = data.Height;
        int width = data.size;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Vector2Int pos = new Vector2Int(j, i);
                if (_voidLookup.Contains(pos)) continue;

                float xPos = (j - (float)(width - 1) / 2) * CellSpacing;
                float yPos = (i - (float)(height - 1) / 2) * CellSpacing;

                Cell cell = _cellPool.Get();
                cell.transform.position = new Vector3(xPos, yPos, 0f);
                cell.SetPositionInCell(j, i);

                if (_clueLookup.TryGetValue(pos, out ClueData clue)) cell.SetClue(clue);
                else cell.ClearClue();

                gridCells.Add(cell);
            }
        }
        gridCellEvent?.Invoke(gridCells);
    }

    private void BuildLookups(LevelDefinition data)
    {
        _clueLookup.Clear();
        _voidLookup.Clear();

        if (data.clues != null)
        {
            foreach (ClueData c in data.clues) _clueLookup[new Vector2Int(c.x, c.y)] = c;
        }
        if (data.cellOverrides != null)
        {
            foreach (CellOverrideData o in data.cellOverrides)
            {
                if (o.type == CellType.Void) _voidLookup.Add(new Vector2Int(o.x, o.y));
            }
        }
    }

    public void DeleteGrid()
    {
        foreach (Cell cell in gridCells)
        {
            if (cell != null) _cellPool.Release(cell);
        }
        gridCells.Clear();
    }
}
