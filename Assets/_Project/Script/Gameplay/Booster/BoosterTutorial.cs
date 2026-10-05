using DG.Tweening;
using UnityEngine;

/// <summary>
/// Mở khóa booster bằng tutorial (như bản gốc): booster khóa tới khi lần đầu vào level >= levelUnlock (BoosterDatabase).
/// Lúc đó, khi lưới đã hiện xong (và băng HARD đã chạy xong), màn hình tối lại, chỉ nút booster đó sáng + bàn tay chỉ vào,
/// lời nhắc lấy từ BoosterData.detail. Người chơi phải bấm nút: booster mở khóa (lưu SaveData.unlockedBoosters)
/// và lần dùng đầu tiên miễn phí (BoosterManager). Mỗi lần nạp level chỉ chạy tối đa 1 tutorial (booster mở sớm nhất trước).
/// Khiên có bước 2: bật xong thì nền tối tắt, lời nhắc "kéo sai thử", bàn tay (TutorialHand) kéo mẫu 1 vùng đúng số ô nhưng sai
/// lời giải; người chơi kéo sai -> khiên chặn (LivesManager.onShieldBlocked) -> "The Shield saved you" 2 s rồi tắt.
/// </summary>
public class BoosterTutorial : Singleton<BoosterTutorial>
{
    [SerializeField] private BoosterTutorialView view;
    [Tooltip("Chờ thêm sau khi lưới hiện xong rồi mới tối màn hình")]
    [SerializeField] private float startDelay = 0.3f;

    private GridSpawner _spawner;
    private bool _armed;     // vừa nạp level: chờ đủ điều kiện để xét tutorial
    private float _readyTime;

    /// <summary>Đang hiện tutorial: lưới bị khóa, chỉ nút Target bấm được.</summary>
    public static bool Active { get; private set; }
    public static BoosterType Target { get; private set; }

    private void Start()
    {
        _spawner = FindFirstObjectByType<GridSpawner>();
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
        Active = false;
    }

    private void OnLevelLoaded(int _)
    {
        StopDemo();
        if (view != null) view.Hide();
        if (Active) Close();
        _armed = true;
        _readyTime = -1f;
    }

    private void Update()
    {
        if (!_armed || Active) return;
        if (!CanStartNow())
        {
            _readyTime = -1f;
            return;
        }
        if (_readyTime < 0f) _readyTime = Time.time + startDelay;
        if (Time.time < _readyTime) return;

        _armed = false;
        BoosterData data = NextToUnlock();
        if (data != null) Open(data);
    }

    /// <summary>Đang chơi thật (không ở Menu, không popup, không tutorial lần đầu), lưới hiện xong, chưa đặt khối nào.</summary>
    private bool CanStartNow()
    {
        LevelManager lm = LevelManager.Instance;
        GridManager gm = GridManager.Instance;
        if (lm == null || gm == null || gm.Board == null || !lm.IsPlaying) return false;
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return false;
        if (TutorialController.Active || MechanicIntro.Active || DifficultyIntro.Playing || gm.IsSelecting) return false;
        if (_spawner != null && _spawner.IsIntroPlaying) return false;
        return true;
    }

    /// <summary>Booster chưa mở có levelUnlock nhỏ nhất mà level hiện tại đã đạt tới.</summary>
    private static BoosterData NextToUnlock()
    {
        BoosterManager bm = BoosterManager.Instance;
        if (bm == null || bm.Database == null || LevelManager.Instance == null) return null;
        int level = LevelManager.Instance.CurrentLevelNumber;
        BoosterData best = null;
        foreach (BoosterData d in bm.Database.boosterDatas)
        {
            if (d == null || SaveSystem.Data.IsBoosterUnlocked(d.boosterType) || level < d.levelUnlock) continue;
            if (best == null || d.levelUnlock < best.levelUnlock) best = d;
        }
        return best;
    }

    private void Open(BoosterData data)
    {
        BoosterButton button = FindButton(data.boosterType);
        if (button == null || view == null)
        {
            // Không có giao diện: mở khóa luôn để người chơi không bị kẹt booster
            Unlock(data.boosterType);
            return;
        }
        if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
        Target = data.boosterType;
        Active = true;
        button.Refresh(); // Target coi như đã mở: hiện icon + số lượt
        view.Show(button, Message(data));
    }

    /// <summary>
    /// Lời nhắc = BoosterData.detail (dùng chung với popup mua). Nếu Khiên mở ở level Hard (đổi levelUnlock):
    /// nói rõ vì sao nên bật ngay ở level này.
    /// </summary>
    private static string Message(BoosterData data)
    {
        LevelDefinition level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
        bool hard = level != null && level.difficulty != LevelDifficulty.Normal;
        return data.boosterType == BoosterType.Shield && hard ? "Tough level! " + data.detail : data.detail;
    }

    /// <summary>BoosterManager gọi khi người chơi bấm đúng nút: mở khóa, đóng lớp tối (booster chạy ngay sau đó).</summary>
    public void Complete()
    {
        if (!Active) return;
        BoosterType type = Target;
        Close();
        Unlock(type);
        // Khiên: bật xong (BoosterManager chạy ngay sau hàm này) thì bắt người chơi kéo sai để thấy khiên chặn lỗi
        if (type == BoosterType.Shield) _demoStart = Time.time + 0.2f;
    }

    private void Close()
    {
        Active = false;
        if (view != null) view.Hide();
    }

    // =====================================================================
    // Bước 2 tutorial Khiên: kéo sai thử

    [Header("Khiên: kéo sai thử")]
    [SerializeField] private string wrongMoveMessage = "Now make a wrong move like this - the Shield will block it!";
    [SerializeField] private string savedMessage = "The Shield saved you - no life lost!";
    [Tooltip("Thời gian hiện lời khen sau khi khiên chặn lỗi")]
    [SerializeField] private float savedMessageTime = 2f;

    private TutorialHand _hand;
    private float _demoStart = -1f;
    private Tween _demoEnd;

    /// <summary>Đang chờ người chơi kéo sai (lưới mở, bàn tay chỉ 1 cách kéo sai).</summary>
    public static bool Demonstrating { get; private set; }

    private void OnEnable()
    {
        if (LivesManager.Instance != null) LivesManager.Instance.onShieldBlocked.AddListener(OnShieldBlocked);
        if (GridManager.Instance != null)
        {
            GridManager.Instance.onBlockPlaced.AddListener(OnBlockPlaced);
            GridManager.Instance.onGameWin.AddListener(StopDemo);
        }
    }

    private void OnDisable()
    {
        if (LivesManager.Instance != null) LivesManager.Instance.onShieldBlocked.RemoveListener(OnShieldBlocked);
        if (GridManager.Instance != null)
        {
            GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
            GridManager.Instance.onGameWin.RemoveListener(StopDemo);
        }
        Demonstrating = false;
    }

    private void LateUpdate()
    {
        if (_demoStart < 0f || Time.time < _demoStart) return;
        _demoStart = -1f;
        if (LivesManager.Instance == null || !LivesManager.Instance.HasShield) return; // khiên không bật được: bỏ bước này
        Demonstrating = true;
        if (view != null) view.ShowMessage(wrongMoveMessage);
        PointWrongMove();
    }

    /// <summary>Bàn tay kéo mẫu 1 vùng đúng số ô nhưng làm lưới hết lời giải (số nhỏ nhất cho dễ kéo).</summary>
    private void PointWrongMove()
    {
        if (_hand == null) _hand = FindFirstObjectByType<TutorialHand>(FindObjectsInactive.Include);
        BoardModel board = GridManager.Instance != null ? GridManager.Instance.Board : null;
        if (_hand == null || board == null) return;

        RectInt? wrong = FindWrongMove(board);
        if (wrong == null)
        {
            _hand.Stop();
            return;
        }
        RectInt r = wrong.Value;
        _hand.Play(board.Geometry.CellToWorld(new Vector2Int(r.xMin, r.yMax - 1)),
                   board.Geometry.CellToWorld(new Vector2Int(r.xMax - 1, r.yMin)));
    }

    private static RectInt? FindWrongMove(BoardModel board)
    {
        RectInt? best = null;
        int bestValue = int.MaxValue;
        foreach (CellModel cell in board.Cells)
        {
            if (!cell.HasClue || cell.IsOccupied || cell.Clue.IsLocked || cell.Clue.IsHidden) continue;
            int value = cell.Clue.Value;
            if (value >= bestValue || value < 2) continue;
            for (int w = 1; w <= value; w++)
            {
                if (value % w != 0) continue;
                int h = value / w;
                for (int x = cell.Position.x - w + 1; x <= cell.Position.x; x++)
                {
                    for (int y = cell.Position.y - h + 1; y <= cell.Position.y; y++)
                    {
                        RectInt rect = new RectInt(x, y, w, h);
                        if (!board.Check(rect).IsReady || board.IsSolvableWith(rect)) continue;
                        best = rect;
                        bestValue = value;
                        goto NextCell;
                    }
                }
            }
            NextCell: ;
        }
        return best;
    }

    private void OnBlockPlaced(BlockModel _)
    {
        if (Demonstrating) PointWrongMove(); // đặt đúng thay vì sai: chỉ lại cách sai trên lưới mới
    }

    private void OnShieldBlocked()
    {
        if (!Demonstrating) return;
        Demonstrating = false;
        if (_hand != null) _hand.Stop();
        if (view != null) view.ShowMessage(savedMessage);
        _demoEnd?.Kill();
        _demoEnd = DOVirtual.DelayedCall(savedMessageTime, () => { if (view != null) view.Hide(); }).SetLink(gameObject);
    }

    private void StopDemo()
    {
        _demoStart = -1f;
        _demoEnd?.Kill();
        if (!Demonstrating) return;
        Demonstrating = false;
        if (_hand != null) _hand.Stop();
        if (view != null) view.Hide();
    }

    private static void Unlock(BoosterType type)
    {
        SaveSystem.Data.UnlockBooster(type);
        SaveSystem.MarkDirty();
        BoosterButton button = FindButton(type);
        if (button != null) button.Refresh();
    }

    private static BoosterButton FindButton(BoosterType type)
    {
        foreach (BoosterButton b in FindObjectsByType<BoosterButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.Type == type) return b;
        }
        return null;
    }
}
