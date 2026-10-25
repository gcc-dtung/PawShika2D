using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Phản hồi quanh khối mèo:
    /// - Đặt khối: hạt bụi màu mèo bung ra từ mép khối; mèo kề bên nhảy nhẹ + đổi biểu cảm (giật mình nhìn sang).
    /// - Gỡ khối: các ô bên dưới nảy lại khi khối vừa nhảy đi.
    /// - Đóng băng: các mèo run lên lần lượt (viền băng + tuyết do FreezeOverlayFX).
    /// Hạt bụi lấy từ pool tạo bằng code (sprite chấm tròn ProceduralSprites.Dot), không cần prefab.
    /// </summary>
    public class BlockFeedback : MonoBehaviour
    {
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid, LevelTimer timer, GridSpawner spawner)
        {
            _grid = grid;
            _timer = timer;
            _spawner = spawner;
        }

        [Header("Hạt bụi khi đặt khối")]
        [SerializeField] private bool dust = true;
        [SerializeField, Range(0, 32)] private int dustCount = 12;
        [SerializeField] private float dustSize = 0.22f;
        [SerializeField] private float dustDistance = 0.55f;
        [SerializeField] private float dustDuration = 0.45f;
        [SerializeField, Range(0f, 1f)] private float dustWhiten = 0.35f; // màu mèo pha trắng cho nổi trên thân cùng màu
        [SerializeField] private int dustSortingOrder = 26;              // trên khối (20–22), dưới khung chọn (30)
        [Tooltip("Hạt bụi mẫu (con tắt sẵn, Editor dựng): pool nhân bản")]
        [SerializeField] private SpriteRenderer dustTemplate;

        [Header("Mèo kề bên phản ứng")]
        [SerializeField] private bool neighbourNudge = true;
        [SerializeField] private float neighbourDelay = 0.06f;
        [SerializeField] private float neighbourJitter = 0.06f;

        [Header("Gỡ khối: ô bên dưới nảy lại")]
        [SerializeField] private float removeBumpDelay = 0.12f;
        [SerializeField, Range(0f, 0.2f)] private float removeBumpAmount = 0.08f;
        [SerializeField] private float removeBumpDuration = 0.24f;

        [Header("Đóng băng: mèo run lần lượt")]
        [SerializeField] private float shiverStagger = 0.03f;

        private GridSpawner _spawner;
        private LevelTimer _timer;
        private ObjectPool<SpriteRenderer> _dustPool;
        private readonly List<Tween> _dustTweens = new List<Tween>();

        private void Awake()
        {
            if (dustTemplate == null)
            {
                Debug.LogError($"[BlockFeedback] Thiếu dustTemplate trên {name} (chuột phải component -> Dựng sẵn object con)", this);
                dust = false;
            }
            _dustPool = new ObjectPool<SpriteRenderer>(
                createFunc: CreateDust,
                actionOnGet: sr => sr.gameObject.SetActive(true),
                actionOnRelease: sr => sr.gameObject.SetActive(false),
                actionOnDestroy: sr => { if (sr != null) Destroy(sr.gameObject); },
                collectionCheck: false,
                defaultCapacity: 32);
        }

        private SpriteRenderer CreateDust()
        {
            SpriteRenderer sr = Instantiate(dustTemplate, transform);
            sr.name = "Dust";
            sr.sprite = ProceduralSprites.Dot;
            sr.sortingOrder = dustSortingOrder;
            return sr;
        }

        private void Start()
        {
            GridManager grid = _grid;
            if (grid != null)
            {
                grid.onBlockPlaced.AddListener(OnBlockPlaced);
                grid.onBlockRemoved.AddListener(OnBlockRemoved);
            }
            if (_timer != null) _timer.onFreezeChanged.AddListener(OnFreezeChanged);
        }

        private void OnDestroy()
        {
            GridManager grid = _grid;
            if (grid != null)
            {
                grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
                grid.onBlockRemoved.RemoveListener(OnBlockRemoved);
            }
            if (_timer != null) _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
        }

        // Tắt component: hạt bụi chạy nốt tới cuối để trả về pool
        private void OnDisable()
        {
            for (int i = _dustTweens.Count - 1; i >= 0; i--) _dustTweens[i]?.Kill(true);
            _dustTweens.Clear();
        }

        // ---------- Đặt khối ----------

        private void OnBlockPlaced(BlockModel block)
        {
            GridManager grid = _grid;
            if (block == null || grid == null || grid.Board == null) return;
            GridGeometry geometry = grid.Board.Geometry;

            if (dust) BurstDust(geometry.RectCenter(block.Rect), geometry.RectSize(block.Rect), block.Color.ToColor());
            if (neighbourNudge) NudgeNeighbours(grid, block);
        }

        /// <summary>Hạt bụi tỏa ra 4 phía từ mép khối: bay ra (chậm dần), nhỏ lại và mờ đi.</summary>
        private void BurstDust(Vector3 center, Vector2 size, Color color)
        {
            Color c = Color.Lerp(color, Color.white, dustWhiten);
            Vector2 half = size * 0.5f;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = (i + Random.Range(-0.35f, 0.35f)) / dustCount * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                // Điểm trên mép khối theo hướng dir
                float sx = Mathf.Abs(dir.x) > 1e-4f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
                float sy = Mathf.Abs(dir.y) > 1e-4f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
                Vector3 from = center + (Vector3)(dir * Mathf.Min(sx, sy) * 0.92f);
                from.z = -0.6f;
                Vector3 to = from + (Vector3)(dir * dustDistance * Random.Range(0.55f, 1f));
                float startSize = dustSize * Random.Range(0.7f, 1.25f);
                float duration = dustDuration * Random.Range(0.8f, 1.15f);

                SpriteRenderer sr = _dustPool.Get();
                Transform t = sr.transform;
                t.position = from;
                t.localScale = Vector3.one * startSize;
                sr.color = c;

                Tween tween = null;
                tween = DOVirtual.Float(0f, 1f, duration, p =>
                    {
                        float move = 1f - (1f - p) * (1f - p); // OutQuad
                        t.position = Vector3.LerpUnclamped(from, to, move);
                        t.localScale = Vector3.one * (startSize * (1f - p));
                        Color k = c;
                        k.a = c.a * (1f - p * p);
                        sr.color = k;
                    })
                    .SetEase(Ease.Linear)
                    .SetLink(sr.gameObject)
                    .OnKill(() =>
                    {
                        _dustTweens.Remove(tween);
                        if (sr != null) _dustPool.Release(sr);
                    });
                _dustTweens.Add(tween);
            }
        }

        /// <summary>Mèo nằm sát khối mới (kể cả chéo) nhảy nhẹ lần lượt và đổi biểu cảm.</summary>
        private void NudgeNeighbours(GridManager grid, BlockModel placed)
        {
            RectInt r = placed.Rect;
            RectInt around = new RectInt(r.xMin - 1, r.yMin - 1, r.width + 2, r.height + 2);
            foreach (BlockModel other in grid.Board.Blocks)
            {
                if (other == placed || !other.Rect.Overlaps(around)) continue;
                if (grid.TryGetView(other, out PlacedBlockView view))
                    view.PlayNudge(neighbourDelay + Random.Range(0f, neighbourJitter));
            }
        }

        // ---------- Gỡ khối ----------

        private void OnBlockRemoved(BlockModel block)
        {
            if (block == null || _spawner == null || _spawner.GetGrid == null) return;
            foreach (CellView cell in _spawner.GetGrid)
            {
                if (cell != null && cell.Model != null && block.Rect.Contains(cell.Position))
                    cell.PlayBump(removeBumpDelay, removeBumpAmount, removeBumpDuration);
            }
        }

        // ---------- Đóng băng ----------

        private void OnFreezeChanged(bool frozen)
        {
            if (!frozen) return;

            GridManager grid = _grid;
            if (grid == null || grid.Board == null) return;
            int i = 0;
            foreach (BlockModel block in grid.Board.Blocks)
            {
                if (grid.TryGetView(block, out PlacedBlockView view)) view.PlayShiver(i++ * shiverStagger);
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor: Dựng sẵn object con (thay cho tạo lúc chạy). Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            dustTemplate = EditorChildren.Child<SpriteRenderer>(transform, "DustTemplate", false);
        }
#endif
    }
}
