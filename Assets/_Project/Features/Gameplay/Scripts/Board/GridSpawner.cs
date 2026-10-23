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

        public const float CellSpacing = 1.1f; // cellGap + cellSize
        /// <summary>
        /// Khe giữa 2 ô nền (world). Ô nền vẽ đúng CellSpacing - CellGap, nên đổi CellSpacing thì ô co giãn theo.
        /// Phải lớn hơn khe giữa 2 khối (PlacedBlockView.BodyFill) để khối luôn phủ kín ô nền bên dưới.
        /// </summary>
        public const float CellGap = 0.08f;
        /// <summary>Khoảng từ mép ô ngoài cùng tới mép khung nền khay (world). BoardCameraFitter cộng vào để khung nằm trọn vùng chơi.</summary>
        public const float BoardPadding = 0.22f;

        [SerializeField] private CellView cellPrefab;
        [SerializeField] private LevelDefinition levelData;
        [SerializeField] private int cellPoolCapacity = 144; // lưới lớn nhất 12x12
        [Tooltip("Khung nền khay dưới các ô (SpriteRenderer Sliced), co giãn theo kích thước lưới mỗi level")]
        [SerializeField] private SpriteRenderer boardFrame;
        [Tooltip("Tường giữa 2 ô (LevelDefinition.walls)")]
        [SerializeField] private WallView wallPrefab;

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
        private readonly List<WallView> _walls = new List<WallView>();
        private readonly Stack<WallView> _freeWalls = new Stack<WallView>();
        private readonly Dictionary<Vector2Int, ClueData> _clueLookup = new Dictionary<Vector2Int, ClueData>();
        private readonly HashSet<Vector2Int> _voidLookup = new HashSet<Vector2Int>();
        private ObjectPool<CellView> _cellPool;
        private readonly Dictionary<Collider2D, CellView> _cellsByCollider = new Dictionary<Collider2D, CellView>();
        private float _introEndTime;

        public LevelDefinition LevelData => levelData;
        public List<CellView> GetGrid => _gridCells;
        public IReadOnlyList<WallView> Walls => _walls;
        /// <summary>Hiệu ứng xuất hiện đang chạy: GridManager khóa thao tác.</summary>
        public bool IsIntroPlaying => Time.time < _introEndTime;
        public GridGeometry Geometry => levelData != null
            ? new GridGeometry(levelData.size, levelData.Height, CellSpacing)
            : new GridGeometry(0, 0, CellSpacing);

        private void Awake()
        {
            // Ô được tái sử dụng giữa các level thay vì Instantiate / Destroy mỗi lần
            _cellPool = new ObjectPool<CellView>(
                createFunc: CreateCell,
                actionOnGet: c => c.gameObject.SetActive(true),
                actionOnRelease: c =>
                {
                    c.Unbind();
                    c.gameObject.SetActive(false);
                },
                // Thoát Play mode: scene hủy object trước, rồi Editor mới Clear() pool -> object có thể đã null
                actionOnDestroy: c =>
                {
                    if (c == null) return;
                    if (c.HitBox != null) _cellsByCollider.Remove(c.HitBox);
                    Destroy(c.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: cellPoolCapacity,
                maxSize: cellPoolCapacity * 2);
        }

        private CellView CreateCell()
        {
            CellView cell = Instantiate(cellPrefab, transform);
            cell.SetVisualSize(CellSpacing - CellGap);
            if (cell.HitBox != null) _cellsByCollider[cell.HitBox] = cell; // đăng ký 1 lần khi tạo (guide 8.3)
            return cell;
        }

        /// <summary>Ô của collider vừa raycast trúng (thay cho GetComponent trên kết quả raycast).</summary>
        public bool TryGetCell(Collider2D collider, out CellView cell)
        {
            cell = null;
            return collider != null && _cellsByCollider.TryGetValue(collider, out cell) && cell != null;
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
            SpawnWalls(data, geometry);
            FitBoardFrame(geometry);
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
            foreach (WallView wall in _walls) wall.PlayIntro(contentStart, contentDuration);
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

        /// <summary>Khung nền phủ cả lưới (kể cả ô Void): mép ô ngoài cùng + BoardPadding mỗi phía, tâm ở gốc như lưới.</summary>
        private void FitBoardFrame(GridGeometry geometry)
        {
            if (boardFrame == null) return;
            boardFrame.transform.position = new Vector3(0f, 0f, boardFrame.transform.position.z);
            boardFrame.size = BoardSize(geometry.Width, geometry.Height);
        }

        /// <summary>Kích thước khung nền khay (world) cho lưới width x height.</summary>
        public static Vector2 BoardSize(int width, int height) =>
            new Vector2(width, height) * CellSpacing - Vector2.one * CellGap + Vector2.one * (BoardPadding * 2f);

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

        private void SpawnWalls(LevelDefinition data, GridGeometry geometry)
        {
            if (data.walls == null || wallPrefab == null) return;
            foreach (WallData w in data.walls)
            {
                WallView wall = _freeWalls.Count > 0 ? _freeWalls.Pop() : Instantiate(wallPrefab, transform);
                wall.gameObject.SetActive(true);
                wall.Show(w, geometry);
                _walls.Add(wall);
            }
        }

        /// <summary>Tường giữa 2 ô kề nhau (null nếu không có).</summary>
        public WallView GetWallBetween(Vector2Int a, Vector2Int b)
        {
            if (levelData == null || !WallRules.TryFind(levelData.walls, a, b, out WallData data)) return null;
            foreach (WallView wall in _walls)
            {
                if (wall.Data.Equals(data)) return wall;
            }
            return null;
        }

        public void DeleteGrid()
        {
            foreach (CellView cell in _gridCells)
            {
                if (cell != null) _cellPool.Release(cell);
            }
            _gridCells.Clear();
            foreach (WallView wall in _walls)
            {
                if (wall == null) continue;
                wall.gameObject.SetActive(false);
                _freeWalls.Push(wall);
            }
            _walls.Clear();
        }
    }
}
