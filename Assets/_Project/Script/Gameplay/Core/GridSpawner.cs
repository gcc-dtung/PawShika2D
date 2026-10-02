using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class GridSpawner : MonoBehaviour
{
    public const float CellSpacing = 1.2f; // cellGap + cellSize

    [SerializeField] private CellView cellPrefab;
    [SerializeField] private LevelDefinition levelData;
    [SerializeField] private int cellPoolCapacity = 144; // lưới lớn nhất 12x12
    public UnityEvent<List<CellView>> gridCellEvent = new UnityEvent<List<CellView>>();

    private readonly List<CellView> gridCells = new List<CellView>();
    private readonly Dictionary<Vector2Int, ClueData> _clueLookup = new Dictionary<Vector2Int, ClueData>();
    private readonly HashSet<Vector2Int> _voidLookup = new HashSet<Vector2Int>();
    private ObjectPool<CellView> _cellPool;

    public LevelDefinition LevelData => levelData;
    public List<CellView> GetGrid => gridCells;
    public GridGeometry Geometry => levelData != null
        ? new GridGeometry(levelData.size, levelData.Height, CellSpacing)
        : new GridGeometry(0, 0, CellSpacing);

    private void Awake()
    {
        // Ô được tái sử dụng giữa các level thay vì Instantiate / Destroy mỗi lần
        _cellPool = new ObjectPool<CellView>(
            createFunc: () => Instantiate(cellPrefab, transform),
            actionOnGet: c => c.gameObject.SetActive(true),
            actionOnRelease: c =>
            {
                c.Unbind();
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

        GridGeometry geometry = Geometry;
        for (int i = 0; i < data.Height; i++)
        {
            for (int j = 0; j < data.size; j++)
            {
                Vector2Int pos = new Vector2Int(j, i);
                if (_voidLookup.Contains(pos)) continue;

                CellModel model = _clueLookup.TryGetValue(pos, out ClueData clue)
                    ? new CellModel(pos, clue)
                    : new CellModel(pos);

                CellView cell = _cellPool.Get();
                cell.transform.position = geometry.CellToWorld(pos);
                cell.Bind(model);

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
        foreach (CellView cell in gridCells)
        {
            if (cell != null) _cellPool.Release(cell);
        }
        gridCells.Clear();
    }
}
