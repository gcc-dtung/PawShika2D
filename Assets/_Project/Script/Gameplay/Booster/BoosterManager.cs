using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Phần chung của mọi booster: khóa / mở, đếm lượt, mua, lượt miễn phí của tutorial, khóa lưới khi tự đặt khối.
/// Luật riêng từng booster nằm ở BoosterEffect (Strategy: HintBooster, ShieldBooster, RevealBooster), tạo bằng BoosterEffectFactory.
/// Nút trên HUD chỉ gọi Press(type); popup mua nghe onBuyRequested. Lượt chỉ bị trừ khi booster thực sự có tác dụng.
/// Tutorial mở khóa (BoosterTutorial): BeginTutorial(type) -> chỉ nút đó bấm được; bấm -> TutorialPressed, lần dùng này miễn phí.
/// </summary>
public class BoosterManager : Singleton<BoosterManager>, IBoosterHost
{
    [SerializeField] private BoosterDatabase database;
    [Tooltip("Khoảng cách giữa các khối Reveal tự đặt")]
    [SerializeField] private float revealStagger = 0.12f;

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

    /// <summary>Booster tutorial đang chỉ vào (chỉ nút này bấm được), null nếu không có tutorial.</summary>
    public BoosterType? TutorialTarget { get; private set; }

    // IBoosterHost
    public GridManager Grid => GridManager.Instance;
    public LivesManager Lives => LivesManager.Instance;

    protected override void Awake()
    {
        base.Awake();
        foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
        {
            BoosterEffect effect = BoosterEffectFactory.Create(type);
            if (effect == null) continue;
            effect.Bind(this);
            _effects[type] = effect;
        }
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

    /// <summary>Đã qua tutorial mở khóa, hoặc đang trong tutorial của chính booster này.</summary>
    public bool IsUnlocked(BoosterType type)
    {
        if (Get(type) == null) return false;
        return SaveSystem.Data.IsBoosterUnlocked(type) || TutorialTarget == type;
    }

    public int Count(BoosterType type)
    {
        BoosterData data = Get(type);
        return data != null ? BoosterInventory.Get(data) : 0;
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
        if (data == null || !CoinWallet.TrySpend(data.priceOneBuy)) return false;
        BoosterInventory.Add(data, data.valueOneBuy);
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

    public void Consume(BoosterType type)
    {
        BoosterData data = Get(type);
        if (_freeUse == type) _freeUse = null;
        else if (data != null) BoosterInventory.TryConsume(data);
        onBoosterUsed?.Invoke(type);
    }

    /// <summary>Đặt lần lượt các khối, cách nhau revealStagger; thao tác trên lưới bị khóa trong lúc đặt.</summary>
    public void PlaceInSequence(List<RectInt> rects)
    {
        KillPlacing();
        InputGate.Lock(this);
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        for (int i = 0; i < rects.Count; i++)
        {
            RectInt rect = rects[i];
            seq.InsertCallback(i * revealStagger, () =>
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
        KillPlacing();
        CancelModes();
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        if (e.State != GameState.Playing) CancelModes();
    }
}
