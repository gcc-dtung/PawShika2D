using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace PawBlock.Features.Gameplay
{
    public class GridSpawner : MonoBehaviour
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        public const float CellSpacing = 1.2f; // cellGap + cellSize

        [SerializeField] private CellView cellPrefab;
        [SerializeField] private LevelDefinition levelData;
        [SerializeField] private int cellPoolCapacity = 144; // lưới lớn nhất 12x12

        [Header("Hiệu ứng xuất hiện (ô nảy lần lượt, thứ tự ngẫu nhiên, rồi số hiện dần)")]
        [SerializeField] private bool playIntro = true;
        [Tooltip("Khoảng cách giữa 2 ô bắt đầu nảy (s); cả lưới không quá cellSpreadMax")]
        [SerializeField] private float cellInterval = 0.035f;
        [SerializeField] private float cellSpreadMax = 0.6f;
        [SerializeField] private float cellDuration = 0.28f;
        [Tooltip("Số bắt đầu hiện sau khi ô cuối bắt đầu nảy bao lâu (s)")]
        [SerializeField] private float contentGap = 0.12f;
        [SerializeField] private float contentSpread = 0.35f;
        [SerializeField] private float contentDuration = 0.25f;
        public UnityEvent<List<CellView>> gridCellEvent = new UnityEvent<List<CellView>>();

        private readonly List<CellView> _gridCells = new List<CellView>();
        private readonly Dictionary<Vector2Int, ClueData> _clueLookup = new Dictionary<Vector2Int, ClueData>();
        private readonly HashSet<Vector2Int> _voidLookup = new HashSet<Vector2Int>();
        private ObjectPool<CellView> _cellPool;
        private float _introEndTime;

        public LevelDefinition LevelData => levelData;
        public List<CellView> GetGrid => _gridCells;
        /// <summary>Hiệu ứng xuất hiện đang chạy: GridManager khóa thao tác.</summary>
        public bool IsIntroPlaying => Time.time < _introEndTime;
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
            if (_levels == null)
            {
                Spawn(levelData);
            }
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

                    _gridCells.Add(cell);
                }
            }
            if (playIntro) PlayIntro();
            gridCellEvent?.Invoke(_gridCells);
        }

        /// <summary>
        /// Các ô nảy lần lượt theo thứ tự ngẫu nhiên, nhịp đều; xong lượt ô thì số / khóa hiện dần, cũng ngẫu nhiên.
        /// </summary>
        private void PlayIntro()
        {
            int count = _gridCells.Count;
            if (count == 0) return;

            float interval = count > 1 ? Mathf.Min(cellInterval, cellSpreadMax / (count - 1)) : 0f;
            float contentStart = interval * (count - 1) + contentGap;
            float contentInterval = count > 1 ? contentSpread / (count - 1) : 0f;

            int[] cellOrder = Shuffled(count);
            int[] contentOrder = Shuffled(count);
            for (int i = 0; i < count; i++)
            {
                float cellDelay = cellOrder[i] * interval;
                float contentDelay = contentStart + contentOrder[i] * contentInterval;
                _gridCells[i].PlayIntro(cellDelay, cellDuration, contentDelay, contentDuration);
            }
            _introEndTime = Time.time + contentStart + contentSpread + contentDuration;
        }

        private static int[] Shuffled(int count)
        {
            int[] a = new int[count];
            for (int i = 0; i < count; i++) a[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
            return a;
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
                    // [MECHANIC] Cơ chế theo Ô (CellType mới) đọc ở đây
                    if (o.type == CellType.Void) _voidLookup.Add(new Vector2Int(o.x, o.y));
                }
            }
        }

        public void DeleteGrid()
        {
            foreach (CellView cell in _gridCells)
            {
                if (cell != null) _cellPool.Release(cell);
            }
            _gridCells.Clear();
        }
    }
}
