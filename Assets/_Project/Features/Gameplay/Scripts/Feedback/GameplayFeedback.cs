using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Phản hồi trong màn chơi:
    /// - Đặt đúng: chữ khen (NICE / AMAZING...) bật lên ở giữa khối. Combo (đặt nhanh liên tiếp): lần nào cũng khen,
    ///   chữ mạnh dần (GREAT → AMAZING → PERFECT) và to dần.
    /// - Khối cuối (thắng): slow-motion ngắn + camera zoom nhẹ rồi trả lại.
    /// - Đặt khối: các ô trống quanh khối nảy nhẹ, lan ra như sóng (gần nảy trước, mạnh hơn).
    /// - Đặt sai: camera rung (mèo rung đỏ do PlacedBlockView tự chạy; rung điện thoại do GameHapticEvents).
    /// Chữ khen lấy từ pool (prefab), không tạo GameObject mới.
    /// </summary>
    public class GameplayFeedback : MonoBehaviour
    {
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid)
        {
            _grid = grid;
        }

        [Header("Prefab")]
        [SerializeField] private SpriteRenderer praisePrefab;

        [Header("Khen")]
        [SerializeField, Range(0f, 1f)] private float praiseChance = 0.6f;
        [SerializeField] private int praiseMinCells = 2;
        [SerializeField] private float praiseWidth = 2.6f;
        [SerializeField] private float praiseDuration = 0.9f;
        [Tooltip("Mỗi bậc combo chữ khen to thêm bao nhiêu (0.12 = 12%)")]
        [SerializeField] private float comboPraiseScaleStep = 0.12f;

        [Header("Sóng ô quanh khối vừa đặt")]
        [SerializeField] private bool ripple = true;
        [SerializeField, Range(1, 4)] private int rippleRadius = 2;       // số vòng ô quanh khối
        [SerializeField, Range(0f, 0.2f)] private float rippleAmount = 0.06f; // vòng 1 phồng 6%, vòng sau nhỏ dần
        [SerializeField] private float rippleStartDelay = 0.04f;          // chờ khối "đáp" xuống
        [SerializeField] private float rippleStepDelay = 0.05f;           // mỗi vòng ra ngoài trễ thêm
        [SerializeField] private float rippleDuration = 0.22f;

        [Header("Khối cuối (thắng): slow-motion + zoom")]
        [SerializeField] private bool winSlowMo = true;
        [SerializeField, Range(0.1f, 1f)] private float slowMoScale = 0.35f;
        [SerializeField] private float slowMoDuration = 0.6f;      // thời gian thật để tốc độ hồi về bình thường
        [SerializeField, Range(0f, 0.1f)] private float winZoom = 0.04f; // camera zoom vào 4% rồi trả lại
        [SerializeField] private float winZoomDuration = 0.7f;

        [Header("Sai")]
        [SerializeField] private float shakeDuration = 0.25f;
        [SerializeField] private float shakeStrength = 0.15f;

        private Camera _cam;
        private Tween _shake;
        private Vector3 _shakeOrigin;
        private readonly List<Tween> _praiseTweens = new List<Tween>();
        private ObjectPool<SpriteRenderer> _praisePool;
        private GridSpawner _spawner;
        private Tween _slowMo;
        private float _normalTimeScale = 1f;
        private Tween _zoom;
        private float _baseOrtho;

        // Bậc lời khen theo combo (tên sprite trong GameplayVisuals.praiseSprites): đặt lẻ chỉ khen nhẹ để combo mạnh dần rõ
        private static readonly string[][] PraiseTiers =
        {
            new[] { "Nice", "Like" },
            new[] { "Great", "Wow" },
            new[] { "Beauty", "Amazing" },
            new[] { "Perfect" }
        };
        private readonly List<Sprite> _tierBuffer = new List<Sprite>();

        private void Awake()
        {
            _cam = Camera.main;
            _praisePool = CreatePool(praisePrefab, 4);
        }

        private ObjectPool<SpriteRenderer> CreatePool(SpriteRenderer prefab, int capacity)
        {
            if (prefab == null) Debug.LogError($"[GameplayFeedback] Chưa gán prefab trên {name}");
            return new ObjectPool<SpriteRenderer>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: sr => sr.gameObject.SetActive(true),
                actionOnRelease: sr => sr.gameObject.SetActive(false),
                // Thoát Play mode: scene hủy object trước, rồi Editor mới Clear() pool -> object có thể đã null
                actionOnDestroy: sr => { if (sr != null) Destroy(sr.gameObject); },
                collectionCheck: false,
                defaultCapacity: capacity);
        }

        private void Start()
        {
            _spawner = FindFirstObjectByType<GridSpawner>();
            if (_grid == null) return;
            _grid.onBlockPlaced.AddListener(OnBlockPlaced);
            _grid.onMistake.AddListener(OnMistake);
            _grid.onGameWin.AddListener(OnGameWin);
        }

        // Tắt component: hủy mọi tween. Chữ khen chạy nốt tới cuối (complete) để trả về pool; camera về vị trí gốc.
        private void OnDisable()
        {
            for (int i = _praiseTweens.Count - 1; i >= 0; i--) _praiseTweens[i]?.Kill(true);
            _praiseTweens.Clear();
            _shake?.Kill();
            _shake = null;
            _slowMo?.Kill(); // OnKill trả lại timeScale
            _zoom?.Kill();   // OnKill trả lại cỡ camera
        }

        private void OnDestroy()
        {
            if (_grid == null) return;
            _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
            _grid.onMistake.RemoveListener(OnMistake);
            _grid.onGameWin.RemoveListener(OnGameWin);
        }

        private void OnBlockPlaced(BlockModel block)
        {
            if (block == null) return;
            PlayRipple(block.Rect);

            // Combo: lần nào cũng khen, chữ mạnh / to dần; đặt lẻ: như cũ (khối đủ lớn, theo xác suất)
            int combo = _grid.Combo;
            int tier = FeelCurves.PraiseTier(combo, PraiseTiers.Length);
            if (tier == 0 && (block.Area < praiseMinCells || Random.value > praiseChance)) return;

            Sprite praise = PickPraise(tier);
            if (praise == null) return;

            SpriteRenderer sr = _praisePool.Get();
            sr.sprite = praise;
            sr.color = Color.white;
            sr.transform.position = _grid.Board.Geometry.RectCenter(block.Rect) + new Vector3(0f, 0f, -1f);
            PlayPraise(sr, 1f + tier * comboPraiseScaleStep);
        }

        private Sprite PickPraise(int tier)
        {
            GameplayVisuals visuals = GameplayVisuals.Instance;
            if (visuals == null) return null;
            string[] names = tier >= 0 && tier < PraiseTiers.Length ? PraiseTiers[tier] : null;
            if (names == null || visuals.praiseSprites == null) return visuals.GetRandomPraise();

            _tierBuffer.Clear();
            foreach (Sprite s in visuals.praiseSprites)
            {
                if (s != null && System.Array.IndexOf(names, s.name) >= 0) _tierBuffer.Add(s);
            }
            return _tierBuffer.Count > 0 ? _tierBuffer[Random.Range(0, _tierBuffer.Count)] : visuals.GetRandomPraise();
        }

        // ---------- Khối cuối ----------

        private void OnGameWin()
        {
            if (!winSlowMo) return;
            PlaySlowMo();
            PlayWinZoom();
        }

        /// <summary>Chậm lại ngay khi khối cuối đặt xuống rồi nhanh dần về tốc độ cũ (tính bằng thời gian thật).</summary>
        private void PlaySlowMo()
        {
            if (_slowMo == null)
            {
                if (Time.timeScale <= 0f) return; // đang dừng game: không đụng
                _normalTimeScale = Time.timeScale; // giữ tốc độ cheat x2 / x5 nếu đang bật
            }
            _slowMo?.Kill();

            Tween tween = null;
            tween = DOVirtual.Float(slowMoScale, 1f, slowMoDuration, k => Time.timeScale = _normalTimeScale * k)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    Time.timeScale = _normalTimeScale;
                    if (_slowMo == tween) _slowMo = null;
                });
            Time.timeScale = _normalTimeScale * slowMoScale;
            _slowMo = tween;
        }

        /// <summary>Camera (orthographic) zoom vào winZoom rồi trả lại đúng cỡ cũ.</summary>
        private void PlayWinZoom()
        {
            if (_cam == null || !_cam.orthographic || winZoom <= 0f) return;
            if (_zoom == null) _baseOrtho = _cam.orthographicSize;
            _zoom?.Kill();

            Camera cam = _cam;
            float baseSize = _baseOrtho;
            Tween tween = null;
            tween = DOVirtual.Float(0f, 1f, winZoomDuration, p => cam.orthographicSize = baseSize * (1f - winZoom * Mathf.Sin(p * Mathf.PI)))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    if (cam != null) cam.orthographicSize = baseSize;
                    if (_zoom == tween) _zoom = null;
                });
            _zoom = tween;
        }

        /// <summary>Ô trống cách khối 1..rippleRadius ô (tính cả chéo) nảy theo vòng: vòng càng xa càng trễ, càng nhẹ.</summary>
        private void PlayRipple(RectInt rect)
        {
            if (!ripple || _spawner == null || _spawner.GetGrid == null) return;
            foreach (CellView cell in _spawner.GetGrid)
            {
                if (cell == null || cell.Model == null || cell.Model.IsOccupied) continue;
                int ring = FeelCurves.RingDistance(rect, cell.Position);
                float amount = FeelCurves.RippleAmount(ring, rippleRadius, rippleAmount);
                if (amount <= 0f) continue;
                cell.PlayBump(FeelCurves.RippleDelay(ring, rippleStartDelay, rippleStepDelay), amount, rippleDuration);
            }
        }

        /// <summary>Bật to nhanh (0.3 → 1.15 → 1), giữ, rồi bay lên và mờ dần; xong trả về pool.</summary>
        private void PlayPraise(SpriteRenderer sr, float sizeMul = 1f)
        {
            Transform t = sr.transform;
            float baseScale = praiseWidth * sizeMul / sr.sprite.bounds.size.x;
            Vector3 start = t.position;
            float d = praiseDuration;
            t.localScale = Vector3.one * baseScale * 0.3f;

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(baseScale * 1.15f, d * 0.2f).SetEase(Ease.Linear));
            seq.Append(t.DOScale(baseScale, d * 0.1f).SetEase(Ease.Linear));
            seq.Insert(d * 0.5f, t.DOMove(start + Vector3.up * 0.6f, d * 0.5f).SetEase(Ease.Linear));
            seq.Insert(d * 0.6f, sr.DOFade(0f, d * 0.4f).SetEase(Ease.Linear));
            seq.SetLink(sr.gameObject);
            seq.OnKill(() =>
            {
                // Complete hay bị Kill(true) đều qua đây đúng 1 lần -> trả về pool
                _praiseTweens.Remove(seq);
                if (sr != null) _praisePool.Release(sr);
            });
            _praiseTweens.Add(seq);
        }

        private void OnMistake(Vector3 center, Vector2 size)
        {
            ShakeCamera();
        }

        /// <summary>Rung ngang, biên độ giảm dần; bị hủy giữa chừng thì camera vẫn về đúng vị trí gốc.</summary>
        private void ShakeCamera()
        {
            if (_cam == null) return;
            if (_shake == null) _shakeOrigin = _cam.transform.position; // đang rung dở thì giữ gốc cũ
            _shake?.Kill();

            Transform cam = _cam.transform;
            Tween tween = null;
            tween = DOVirtual.Float(0f, 1f, shakeDuration, p =>
                {
                    float t = p * shakeDuration;
                    cam.position = _shakeOrigin + new Vector3(Mathf.Sin(t * 90f) * shakeStrength * (1f - p), 0f, 0f);
                })
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    if (cam != null) cam.position = _shakeOrigin;
                    if (_shake == tween) _shake = null;
                });
            _shake = tween;
        }
    }
}
