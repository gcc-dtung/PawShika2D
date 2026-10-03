using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Luật 3 booster (Giai đoạn 4). Nút trên HUD chỉ gọi Press(type); popup mua nghe onBuyRequested.
/// - Hint: tự đặt 1 khối đúng, lấy từ lời giải khớp với các khối đã đặt.
/// - Khiên (thay Freeze Time): chặn lần sai kế tiếp trong level này (LivesManager bỏ qua 1 lần trừ mạng).
/// - Reveal: vào chế độ chọn số; chạm 1 ô số -> giải mọi khối cùng số (ô còn khóa thì bỏ qua).
/// Lượt chỉ bị trừ khi booster thực sự có tác dụng.
/// </summary>
public class BoosterManager : Singleton<BoosterManager>
{
    [SerializeField] private BoosterDatabase database;
    [Tooltip("Khoảng cách giữa các khối Reveal tự đặt")]
    [SerializeField] private float revealStagger = 0.12f;

    [Header("Events")]
    public UnityEvent<BoosterType> onBuyRequested = new UnityEvent<BoosterType>(); // hết lượt -> mở popup mua
    public UnityEvent<bool> onRevealModeChanged = new UnityEvent<bool>();
    public UnityEvent<BoosterType> onBoosterUsed = new UnityEvent<BoosterType>();

    private bool _revealMode;
    private Sequence _revealPlacing; // Reveal đang đặt lần lượt từng khối

    public BoosterDatabase Database => database;
    public bool IsRevealMode => _revealMode;
    public bool IsBusy => _revealPlacing != null;

    private void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
            LevelManager.Instance.onStateChanged.AddListener(OnStateChanged);
        }
    }

    private void OnDisable()
    {
        KillRevealPlacing();
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
        LevelManager.Instance.onStateChanged.RemoveListener(OnStateChanged);
    }

    // ---------- Trạng thái nút ----------

    public BoosterData Get(BoosterType type) => database != null ? database.Get(type) : null;

    public bool IsUnlocked(BoosterType type)
    {
        BoosterData data = Get(type);
        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : int.MaxValue;
        return data != null && level >= data.levelUnlock;
    }

    public int Count(BoosterType type)
    {
        BoosterData data = Get(type);
        return data != null ? BoosterInventory.Get(data) : 0;
    }

    public bool IsActive(BoosterType type) =>
        type == BoosterType.Shield ? LivesManager.Instance != null && LivesManager.Instance.HasShield
        : type == BoosterType.Reveal && _revealMode;

    /// <summary>Bấm nút booster trên HUD.</summary>
    public void Press(BoosterType type)
    {
        if (!IsUnlocked(type) || !CanPlay()) return;

        // Bấm lại Reveal khi đang chọn số = hủy
        if (type == BoosterType.Reveal && _revealMode)
        {
            SetRevealMode(false);
            return;
        }
        if (IsActive(type)) return;

        if (Count(type) <= 0)
        {
            SetRevealMode(false);
            onBuyRequested?.Invoke(type);
            return;
        }

        switch (type)
        {
            case BoosterType.Hint: UseHint(); break;
            case BoosterType.Shield: UseShield(); break;
            case BoosterType.Reveal: SetRevealMode(true); break;
        }
    }

    /// <summary>Mua valueOneBuy lượt bằng coin.</summary>
    public bool Buy(BoosterType type)
    {
        BoosterData data = Get(type);
        if (data == null || !CoinWallet.TrySpend(data.priceOneBuy)) return false;
        BoosterInventory.Add(data, data.valueOneBuy);
        return true;
    }

    // ---------- Hint ----------

    private void UseHint()
    {
        SetRevealMode(false);
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.Board == null || !gm.Board.TrySolveRemaining(out List<RectInt> remaining)) return;

        // Chọn khối nhỏ nhất đang đặt được (ô số không còn khóa) cho dễ thấy
        RectInt? pick = null;
        foreach (RectInt rect in remaining)
        {
            if (!gm.Board.Check(rect).IsReady) continue;
            if (pick == null || rect.width * rect.height < pick.Value.width * pick.Value.height) pick = rect;
        }
        if (pick == null || !gm.PlaceSolved(pick.Value)) return;

        Consume(BoosterType.Hint);
    }

    // ---------- Khiên ----------

    private void UseShield()
    {
        SetRevealMode(false);
        if (LivesManager.Instance == null || !LivesManager.Instance.ActivateShield()) return;
        Consume(BoosterType.Shield);
    }

    // ---------- Reveal ----------

    /// <summary>DragAndDropController gọi khi chạm 1 ô lúc đang ở chế độ Reveal. Trả về true nếu đã xử lý chạm.</summary>
    public bool HandleCellTap(Vector2Int pos)
    {
        if (!_revealMode) return false;

        GridManager gm = GridManager.Instance;
        CellModel cell = gm != null && gm.Board != null ? gm.Board.GetCell(pos) : null;
        // Chạm ô không phải số / đã có khối / còn khóa: giữ chế độ chọn, chờ chạm lại
        if (cell == null || !cell.HasClue || cell.IsOccupied || cell.Clue.IsLocked) return true;
        if (!gm.Board.TrySolveRemaining(out List<RectInt> remaining)) return true;

        int value = cell.Clue.Value;
        List<RectInt> targets = new List<RectInt>();
        foreach (RectInt rect in remaining)
        {
            PlacementCheck check = gm.Board.Check(rect);
            if (check.IsReady && check.ClueCell.Clue.Value == value) targets.Add(rect);
        }
        if (targets.Count == 0) return true;

        SetRevealMode(false);
        Consume(BoosterType.Reveal);
        PlaceInSequence(targets);
        return true;
    }

    /// <summary>Đặt lần lượt các khối, cách nhau revealStagger; thao tác trên lưới bị khóa trong lúc đặt.</summary>
    private void PlaceInSequence(List<RectInt> rects)
    {
        KillRevealPlacing();
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        for (int i = 0; i < rects.Count; i++)
        {
            RectInt rect = rects[i];
            seq.InsertCallback(i * revealStagger, () =>
            {
                GridManager gm = GridManager.Instance;
                if (gm != null) gm.PlaceSolved(rect);
            });
        }
        seq.OnKill(() => { if (_revealPlacing == seq) _revealPlacing = null; });
        _revealPlacing = seq;
    }

    private void SetRevealMode(bool on)
    {
        if (_revealMode == on) return;
        _revealMode = on;
        if (on && GridManager.Instance != null) GridManager.Instance.CancelSelection();
        onRevealModeChanged?.Invoke(on);
    }

    // ---------- Chung ----------

    private void Consume(BoosterType type)
    {
        BoosterData data = Get(type);
        if (data != null) BoosterInventory.TryConsume(data);
        onBoosterUsed?.Invoke(type);
    }

    private bool CanPlay()
    {
        GridManager gm = GridManager.Instance;
        return gm != null && gm.Board != null && !gm.IsInputLocked;
    }

    private void KillRevealPlacing()
    {
        _revealPlacing?.Kill();
        _revealPlacing = null;
    }

    private void OnLevelLoaded(int _)
    {
        KillRevealPlacing();
        SetRevealMode(false);
    }

    private void OnStateChanged(GameState state)
    {
        if (state != GameState.Playing) SetRevealMode(false);
    }
}
