using DG.Tweening;
using UnityEngine;

/// <summary>
/// Bước 2 tutorial Khiên (tách khỏi BoosterTutorial): nghe BoosterTutorialCompletedEvent, nếu là Khiên thì
/// nền tối đã tắt, lời nhắc "kéo sai thử", bàn tay (TutorialHand) kéo mẫu 1 vùng đúng số ô nhưng sai lời giải;
/// người chơi kéo sai -> khiên chặn (LivesManager.onShieldBlocked) -> "The Shield saved you" vài giây rồi tắt.
/// Booster khác muốn có bước riêng sau tutorial: viết 1 component tương tự nghe cùng sự kiện.
/// </summary>
public class ShieldTutorialDemo : MonoBehaviour
{
    [SerializeField] private BoosterTutorialView view;
    [SerializeField] private string wrongMoveMessage = "Now make a wrong move like this - the Shield will block it!";
    [SerializeField] private string savedMessage = "The Shield saved you - no life lost!";
    [Tooltip("Chờ sau khi bấm nút Khiên rồi mới chỉ cách kéo sai")]
    [SerializeField] private float startDelay = 0.2f;
    [Tooltip("Thời gian hiện lời khen sau khi khiên chặn lỗi")]
    [SerializeField] private float savedMessageTime = 2f;

    private TutorialHand _hand;
    private Tween _demoStart;
    private Tween _demoEnd;
    private bool _showingMessage;

    /// <summary>Đang chờ người chơi kéo sai (lưới mở, bàn tay chỉ 1 cách kéo sai).</summary>
    public static bool Demonstrating { get; private set; }

    private void Awake()
    {
        if (view == null) view = FindFirstObjectByType<BoosterTutorialView>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        EventBus<BoosterTutorialCompletedEvent>.Subscribe(OnTutorialCompleted);
        EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
    }

    private void Start()
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
        EventBus<BoosterTutorialCompletedEvent>.Unsubscribe(OnTutorialCompleted);
        EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
        StopDemo();
    }

    private void OnDestroy()
    {
        if (LivesManager.Instance != null) LivesManager.Instance.onShieldBlocked.RemoveListener(OnShieldBlocked);
        if (GridManager.Instance != null)
        {
            GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
            GridManager.Instance.onGameWin.RemoveListener(StopDemo);
        }
    }

    private void OnTutorialCompleted(BoosterTutorialCompletedEvent e)
    {
        if (e.Type != BoosterType.Shield) return;
        // Khiên bật ngay sau sự kiện này (BoosterManager), chờ 1 nhịp rồi mới kiểm tra
        _demoStart?.Kill();
        _demoStart = DOVirtual.DelayedCall(startDelay, StartDemo, false).SetLink(gameObject);
    }

    private void OnLevelLoaded(LevelLoadedEvent _) => StopDemo();

    private void StartDemo()
    {
        _demoStart = null;
        if (LivesManager.Instance == null || !LivesManager.Instance.HasShield) return; // khiên không bật được: bỏ bước này
        Demonstrating = true;
        ShowMessage(wrongMoveMessage);
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
            // [MECHANIC] Tutorial Khiên chỉ mẫu kéo sai trên ô số người chơi đọc được
            if (!cell.HasClue || cell.IsOccupied || cell.Clue.IsLocked || cell.Clue.IsHidden) continue;
            int value = cell.Clue.Value;
            if (value >= bestValue || value < 2) continue;
            RectInt? rect = FindWrongRectFor(board, cell, value);
            if (rect == null) continue;
            best = rect;
            bestValue = value;
        }
        return best;
    }

    private static RectInt? FindWrongRectFor(BoardModel board, CellModel cell, int value)
    {
        for (int w = 1; w <= value; w++)
        {
            if (value % w != 0) continue;
            int h = value / w;
            for (int x = cell.Position.x - w + 1; x <= cell.Position.x; x++)
            {
                for (int y = cell.Position.y - h + 1; y <= cell.Position.y; y++)
                {
                    RectInt rect = new RectInt(x, y, w, h);
                    if (board.Check(rect).IsReady && !board.IsSolvableWith(rect)) return rect;
                }
            }
        }
        return null;
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
        ShowMessage(savedMessage);
        _demoEnd?.Kill();
        _demoEnd = DOVirtual.DelayedCall(savedMessageTime, HideMessage).SetLink(gameObject);
    }

    private void StopDemo()
    {
        _demoStart?.Kill();
        _demoStart = null;
        _demoEnd?.Kill();
        _demoEnd = null;
        if (Demonstrating && _hand != null) _hand.Stop();
        Demonstrating = false;
        HideMessage();
    }

    private void ShowMessage(string text)
    {
        if (view == null) return;
        view.ShowMessage(text);
        _showingMessage = true;
    }

    private void HideMessage()
    {
        if (!_showingMessage) return;
        _showingMessage = false;
        if (view != null) view.Hide();
    }
}
