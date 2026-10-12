using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Phần chung của mọi booster: khóa / mở, đếm lượt, mua, lượt miễn phí của tutorial, khóa lưới khi tự đặt khối.
    /// Luật riêng từng booster nằm ở BoosterEffect (Strategy: HintBooster, FreezeBooster, RevealBooster), tạo bằng BoosterEffectFactory.
    /// Nút trên HUD chỉ gọi Press(type); popup mua nghe onBuyRequested. Lượt chỉ bị trừ khi booster thực sự có tác dụng.
    /// Tutorial mở khóa (BoosterTutorial): BeginTutorial(type) -> chỉ nút đó bấm được; bấm -> TutorialPressed, lần dùng này miễn phí.
    /// </summary>
    public class BoosterManager : MonoBehaviour, IBoosterHost, IBoosterInventory, IBoosterCatalog
    {
        private LevelTimer _timer;
        private GridManager _grid;
        private ISaveService _save;
        private GameplaySave _data;
        private BoosterInventory _inventory;
        private ICurrencyWallet _coins;

        /// <summary>GameBootstrap gọi trước Start: section "gameplay" (booster đã mở), kho lượt, ví coin (mua lượt).</summary>
        public void Init(ISaveService save, BoosterInventory inventory, ICurrencyWallet coins, GridManager grid, LevelTimer timer)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
            _inventory = inventory;
            _coins = coins;
            _grid = grid;
            _timer = timer;
        }

        /// <summary>Kho lượt booster (BoosterButton, Debug).</summary>
        public BoosterInventory Inventory => _inventory;

        [SerializeField] private BoosterDatabase database;
        [Tooltip("Khoảng cách giữa các khối Reveal tự đặt")]
        [SerializeField] private float revealStagger = 0.12f;
        [Tooltip("Chờ icon booster bay từ giữa màn hình tới mục tiêu (BoosterUseFx) rồi mới đặt khối Reveal / hiện gợi ý Hint")]
        [SerializeField, Min(0f)] private float effectDelay = 1.2f;
        [Tooltip("Freeze: từ lúc bấm tới sự kiện Spine \"Press\" (Freeze.skel, 1.67 s – FreezeOverlayFX) thì đồng hồ mới đóng băng")]
        [SerializeField, Min(0f)] private float freezeEffectDelay = 1.67f;

        [Header("Events")]
        public UnityEvent<BoosterType> onBuyRequested = new UnityEvent<BoosterType>();        // hết lượt -> mở popup mua
        public UnityEvent<BoosterType, bool> onModeChanged = new UnityEvent<BoosterType, bool>(); // bật / tắt chế độ chờ (Reveal)
        public UnityEvent<BoosterType> onBoosterUsed = new UnityEvent<BoosterType>();
        public UnityEvent onTutorialChanged = new UnityEvent();                                  // bắt đầu / xong tutorial mở khóa

        /// <summary>Người chơi bấm đúng nút tutorial đang chỉ (BoosterTutorial mở khóa, rồi booster chạy luôn).</summary>
        public event Action<BoosterType> TutorialPressed;

        private readonly Dictionary<BoosterType, BoosterEffect> _effects = new Dictionary<BoosterType, BoosterEffect>();
        private Sequence _placing;      // đang tự đặt lần lượt từng khối (Reveal)
        private BoosterType? _freeUse;  // lần dùng trong tutorial mở khóa: không trừ lượt (Reveal: tới khi chọn số)

        public BoosterDatabase Database => database;
        public bool IsBusy => _placing != null;
        public float EffectDelay => effectDelay;

        /// <summary>Thời gian từ lúc bấm tới lúc tác dụng hiện ra, theo hiệu ứng của từng booster.</summary>
        public float GetEffectDelay(BoosterType type) => type == BoosterType.Freeze ? freezeEffectDelay : effectDelay;

        /// <summary>Điểm world mà lần dùng booster gần nhất nhắm tới (đọc trong onBoosterUsed); null = mặc định của booster.</summary>
        public Vector3? LastTarget { get; private set; }

        private HintMarker _hint;
        private Tween _afterEffect;

        /// <summary>Booster tutorial đang chỉ vào (chỉ nút này bấm được), null nếu không có tutorial.</summary>
        public BoosterType? TutorialTarget { get; private set; }

        // IBoosterHost
        public GridManager Grid => _grid;
        public LevelTimer Timer => _timer;

        private void Awake()
        {
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                BoosterEffect effect = BoosterEffectFactory.Create(type);
                if (effect == null) continue;
                effect.Bind(this);
                _effects[type] = effect;
            }
            _hint = GetComponent<HintMarker>();
            if (_hint == null) _hint = gameObject.AddComponent<HintMarker>();
            _hint.Changed += () => onModeChanged?.Invoke(BoosterType.Hint, _hint.IsShowing); // nút Hint sáng khi gợi ý đang hiện
        }

        private void OnEnable()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            KillPlacing();
        }

        // ---------- Trạng thái nút ----------

        public BoosterData Get(BoosterType type) => database != null ? database.Get(type) : null;

        // ---------- IBoosterInventory (Shop, DailyReward) ----------

        public int GetCount(BoosterType type)
        {
            BoosterData data = Get(type);
            return data != null ? _inventory.Get(data) : 0;
        }

        public void Add(BoosterType type, int amount)
        {
            BoosterData data = Get(type);
            if (data != null) _inventory.Add(data, amount);
        }

        public Sprite ItemIcon(BoosterType type)
        {
            BoosterData data = Get(type);
            return data != null ? data.ItemIcon : null;
        }

        /// <summary>Đã qua tutorial mở khóa, hoặc đang trong tutorial của chính booster này.</summary>
        public bool IsUnlocked(BoosterType type)
        {
            if (Get(type) == null) return false;
            return _data.IsBoosterUnlocked(type) || TutorialTarget == type;
        }

        public int Count(BoosterType type)
        {
            BoosterData data = Get(type);
            return data != null ? _inventory.Get(data) : 0;
        }

        public bool IsActive(BoosterType type) => _effects.TryGetValue(type, out BoosterEffect e) && e.IsActive;

        /// <summary>Bấm nút booster trên HUD.</summary>
        public void Press(BoosterType type)
        {
            if (TutorialTarget.HasValue)
            {
                if (type != TutorialTarget.Value) return;
                TutorialTarget = null;
                _freeUse = type; // dùng được cả khi hết lượt
                onTutorialChanged?.Invoke();
                TutorialPressed?.Invoke(type);
            }
            if (!IsUnlocked(type) || !CanPlay() || !_effects.TryGetValue(type, out BoosterEffect effect)) return;

            if (effect.IsActive)
            {
                effect.PressWhileActive(); // vd bấm lại Reveal = hủy chọn số
                return;
            }

            CancelModes();
            if (Count(type) <= 0 && _freeUse != type)
            {
                onBuyRequested?.Invoke(type);
                return;
            }
            effect.Activate();
        }

        /// <summary>Thoát mọi chế độ chờ (nút X trên banner Reveal...).</summary>
        public void CancelModes()
        {
            foreach (BoosterEffect effect in _effects.Values) effect.Cancel();
        }

        /// <summary>DragAndDropController gọi khi chạm 1 ô. Trả về true nếu 1 booster đã xử lý chạm.</summary>
        public bool HandleCellTap(Vector2Int pos)
        {
            foreach (BoosterEffect effect in _effects.Values)
            {
                if (effect.HandleCellTap(pos)) return true;
            }
            return false;
        }

        /// <summary>Mua valueOneBuy lượt bằng coin.</summary>
        public bool Buy(BoosterType type)
        {
            BoosterData data = Get(type);
            if (data == null || !_coins.TrySpend(data.priceOneBuy)) return false;
            _inventory.Add(data, data.valueOneBuy);
            return true;
        }

        // ---------- Tutorial mở khóa ----------

        public void BeginTutorial(BoosterType type)
        {
            CancelModes();
            if (Grid != null) Grid.CancelSelection();
            TutorialTarget = type;
            onTutorialChanged?.Invoke();
        }

        public void EndTutorial()
        {
            if (!TutorialTarget.HasValue) return;
            TutorialTarget = null;
            onTutorialChanged?.Invoke();
        }

        // ---------- IBoosterHost ----------

        public void Consume(BoosterType type, Vector3? target = null)
        {
            BoosterData data = Get(type);
            if (_freeUse == type) _freeUse = null;
            else if (data != null) _inventory.TryConsume(data);
            LastTarget = target;
            onBoosterUsed?.Invoke(type);
            EventBus<BoosterUsedEvent>.Raise(new BoosterUsedEvent(type));
        }

        public bool IsHintShowing => _hint != null && _hint.IsShowing;

        public void AfterEffect(BoosterType type, Action action)
        {
            _afterEffect?.Kill();
            _afterEffect = DOVirtual.DelayedCall(GetEffectDelay(type), () =>
                {
                    _afterEffect = null;
                    action?.Invoke();
                })
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void ShowHint(RectInt rect)
        {
            if (_hint != null) _hint.Show(rect, effectDelay);
        }

        /// <summary>
        /// Đặt lần lượt các khối, cách nhau revealStagger, bắt đầu sau effectDelay (icon booster bay tới);
        /// thao tác trên lưới bị khóa từ lúc bấm tới khi đặt xong.
        /// </summary>
        public void PlaceInSequence(List<RectInt> rects)
        {
            KillPlacing();
            InputGate.Lock(this);
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.AppendInterval(effectDelay + revealStagger * Mathf.Max(0, rects.Count - 1)); // giữ sequence sống tới khối cuối
            for (int i = 0; i < rects.Count; i++)
            {
                RectInt rect = rects[i];
                seq.InsertCallback(effectDelay + i * revealStagger, () =>
                {
                    if (Grid != null) Grid.PlaceSolved(rect);
                });
            }
            seq.OnKill(() =>
            {
                if (_placing != seq) return;
                _placing = null;
                InputGate.Unlock(this);
            });
            _placing = seq;
        }

        public void NotifyModeChanged(BoosterType type, bool on)
        {
            if (on && Grid != null) Grid.CancelSelection();
            onModeChanged?.Invoke(type, on);
        }

        // ---------- Chung ----------

        private bool CanPlay()
        {
            GridManager gm = Grid;
            return gm != null && gm.Board != null && !gm.IsInputLocked;
        }

        private void KillPlacing()
        {
            _placing?.Kill(); // OnKill mở khóa lưới
            _placing = null;
            InputGate.Unlock(this);
        }

        private void OnLevelLoaded(LevelLoadedEvent _)
        {
            _freeUse = null;
            _afterEffect?.Kill(); // đổi level giữa hiệu ứng: không đóng băng level mới
            _afterEffect = null;
            KillPlacing();
            CancelModes();
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State != GameState.Playing) CancelModes();
        }
    }
}
