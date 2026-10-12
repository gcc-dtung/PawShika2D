using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Gợi ý của booster Hint: mèo mờ nhấp nháy đúng vùng cần đặt + mỗi ô của vùng có khung xanh (khung_reveal,
    /// Cell/WaitSameHint gốc: hiện mờ dần rồi nhấp nháy cùng nhịp mèo) + bàn tay diễn động tác kéo từ góc này sang góc kia
    /// (bản sao TutorialHand trong scene). Người chơi tự kéo vào.
    /// Tắt khi: đặt đúng vùng đó (mèo thật thay chỗ), đặt khối khác đè lên vùng, nạp level, ván dừng.
    /// Đặt sẵn cùng GameObject với BoosterManager (Bootstrap truyền vào BoosterManager).
    /// </summary>
    public class HintMarker : MonoBehaviour
    {
        private TutorialHand _handTemplate;
        private GameplayVisuals _visuals;
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid, GameplayVisuals visuals, GridSpawner spawner, TutorialHand handTemplate)
        {
            _grid = grid;
            _visuals = visuals;
            _spawner = spawner;
            _handTemplate = handTemplate;
        }

        [SerializeField, Range(0f, 1f)] private float ghostAlphaMin = 0.25f;
        [SerializeField, Range(0f, 1f)] private float ghostAlphaMax = 0.6f;
        [SerializeField] private float pulsePeriod = 1f;
        [Header("Khung ô chờ (khung_reveal)")]
        [SerializeField] private float frameFadeIn = 0.25f;
        [SerializeField, Range(0f, 1f)] private float frameAlphaMin = 0.55f;
        [Tooltip("Cạnh khung / cạnh ô")]
        [SerializeField] private float frameSize = 0.96f;

        private const int FrameSortingOrder = 9; // trên ô lưới (0..8), dưới khối mèo (20+)

        private RectInt _rect;
        private bool _showing;
        private PlacedBlockView _ghost;
        private TutorialHand _hand;
        private Tween _delay;
        private Tween _pulse;
        private GridSpawner _spawner;
        private readonly List<SpriteRenderer> _frames = new List<SpriteRenderer>();
        private int _framesShown;
        private float _frameFade;
        private Tween _fade;

        /// <summary>Đã bấm Hint, gợi ý đang chờ hiện hoặc đang hiện (chưa được đặt).</summary>
        public bool IsShowing => _showing;
        public RectInt Rect => _rect;

        /// <summary>Bật / tắt gợi ý (nút Hint vẽ lại quầng sáng).</summary>
        public event Action Changed;

        private void OnEnable()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            Hide();
        }

        private void Start()
        {
            if (_grid != null) _grid.onBlockPlaced.AddListener(OnBlockPlaced);
            if (_spawner != null) _spawner.gridCellEvent.AddListener(OnGridSpawned); // cả màn tutorial (không phát LevelLoadedEvent)
        }

        private void OnDestroy()
        {
            if (_grid != null) _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
            if (_spawner != null) _spawner.gridCellEvent.RemoveListener(OnGridSpawned);
        }

        private void OnGridSpawned(System.Collections.Generic.List<CellView> _) => Hide();

        /// <summary>Gợi ý vùng <paramref name="rect"/>, hiện sau <paramref name="delay"/> giây (icon booster bay tới).</summary>
        public void Show(RectInt rect, float delay)
        {
            Hide();
            _rect = rect;
            _showing = true;
            Changed?.Invoke();
            _delay = DOVirtual.DelayedCall(delay, Appear).SetLink(gameObject);
        }

        public void Hide()
        {
            _delay?.Kill();
            _delay = null;
            _pulse?.Kill();
            _pulse = null;
            _fade?.Kill();
            _fade = null;
            for (int i = 0; i < _framesShown; i++) if (_frames[i] != null) _frames[i].gameObject.SetActive(false);
            _framesShown = 0;
            if (_ghost != null)
            {
                if (_grid != null) _grid.ReturnGhost(_ghost);
                _ghost = null;
            }
            if (_hand != null) _hand.Stop();
            if (!_showing) return;
            _showing = false;
            Changed?.Invoke();
        }

        private void Appear()
        {
            _delay = null;
            GridManager gm = _grid;
            if (!_showing || gm == null || gm.Board == null) return;

            _ghost = gm.RentGhost(_rect);
            ShowFrames(gm.Board.Geometry);
            // Mèo mờ và khung ô nhấp nháy cùng nhịp; khung còn mờ dần lúc mới hiện (_frameFade 0 -> 1)
            _frameFade = 0f;
            _fade = DOTween.To(() => _frameFade, v => _frameFade = v, 1f, frameFadeIn).SetLink(gameObject);
            _pulse = DOVirtual.Float(0f, 1f, pulsePeriod, p =>
                {
                    float wave = (1f - Mathf.Cos(p * Mathf.PI * 2f)) * 0.5f;
                    if (_ghost != null) _ghost.SetAlpha(Mathf.Lerp(ghostAlphaMax, ghostAlphaMin, wave));
                    SetFramesAlpha(_frameFade * Mathf.Lerp(1f, frameAlphaMin, wave));
                })
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(gameObject);

            TutorialHand hand = EnsureHand();
            if (hand != null)
            {
                // Kéo chéo từ góc trên trái tới góc dưới phải của vùng (vùng 1 ô: chạm tại chỗ)
                GridGeometry g = gm.Board.Geometry;
                Vector3 from = g.CellToWorld(new Vector2Int(_rect.xMin, _rect.yMax - 1));
                Vector3 to = g.CellToWorld(new Vector2Int(_rect.xMax - 1, _rect.yMin));
                hand.Play(from, to);
            }
        }

        /// <summary>Khung khung_reveal (9-slice) trên từng ô của vùng gợi ý; dùng lại SpriteRenderer giữa các lần gợi ý.</summary>
        private void ShowFrames(GridGeometry g)
        {
            GameplayVisuals visuals = _visuals;
            Sprite sprite = visuals != null ? visuals.hintCellFrame : null;
            if (sprite == null) return;
            for (int x = _rect.xMin; x < _rect.xMax; x++)
            {
                for (int y = _rect.yMin; y < _rect.yMax; y++)
                {
                    if (_framesShown == _frames.Count) _frames.Add(NewFrame());
                    SpriteRenderer sr = _frames[_framesShown++];
                    sr.sprite = sprite;
                    sr.size = Vector2.one * (g.Spacing * frameSize);
                    Vector3 pos = g.CellToWorld(new Vector2Int(x, y));
                    sr.transform.position = new Vector3(pos.x, pos.y, pos.z - 0.01f);
                    sr.color = new Color(1f, 1f, 1f, 0f);
                    sr.gameObject.SetActive(true);
                }
            }
        }

        private SpriteRenderer NewFrame()
        {
            GameObject go = new GameObject("HintFrame");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = FrameSortingOrder;
            return sr;
        }

        private void SetFramesAlpha(float alpha)
        {
            for (int i = 0; i < _framesShown; i++) _frames[i].color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Bản sao riêng của bàn tay tutorial (không đụng bàn tay của TutorialController).</summary>
        private TutorialHand EnsureHand()
        {
            if (_hand != null) return _hand;
            TutorialHand template = _handTemplate;
            if (template == null) return null;
            _hand = Instantiate(template, template.transform.parent);
            _hand.name = "HintHand";
            return _hand;
        }

        private void OnBlockPlaced(BlockModel block)
        {
            if (!_showing || block == null) return;
            // Đặt đúng vùng gợi ý, hoặc đặt khối khác đè lên vùng (gợi ý không còn đúng chỗ) -> tắt
            if (block.Rect.Equals(_rect) || block.Rect.Overlaps(_rect)) Hide();
        }

        private void OnLevelLoaded(LevelLoadedEvent _) => Hide();

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State != GameState.Playing) Hide();
        }
    }
}
