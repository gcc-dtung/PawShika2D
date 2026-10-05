using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Popup giới thiệu cơ chế lần đầu người chơi gặp (Hướng, Khóa, "?"), ảnh minh họa có sẵn của bản gốc.
/// Sau mỗi lần nạp level, khi lưới đã hiện xong (và băng HARD đã chạy xong): level có cơ chế chưa xem thì mở popup
/// (ván tạm dừng), Ok thì lưu SaveData.seenMechanics; level có nhiều cơ chế mới thì lần lượt từng popup.
/// Chạy trước BoosterTutorial (chờ ngắn hơn; popup tạm dừng ván nên tutorial booster đợi popup đóng).
/// Level giới thiệu cơ chế do Mechanic Injector xếp lịch, nên popup rơi đúng vào các level đó.
/// </summary>
public class MechanicIntro : Singleton<MechanicIntro>
{
    public enum Kind
    {
        Constraint,
        Lock,
        Hidden
    }

    [SerializeField] private MechanicIntroPopup popup;
    [SerializeField] private Sprite constraintSprite;
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Sprite hiddenSprite;
    [Tooltip("Chờ thêm sau khi lưới hiện xong rồi mới mở popup")]
    [SerializeField] private float startDelay = 0.15f;

    private GridSpawner _spawner;
    private bool _armed;
    private float _readyTime;
    private readonly Queue<Kind> _queue = new Queue<Kind>();

    /// <summary>Đang hiện (hoặc còn chờ hiện) popup cơ chế.</summary>
    public static bool Active { get; private set; }

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
        _queue.Clear();
        if (popup != null && popup.IsOpen) popup.CloseImmediate();
        Active = false;
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
        foreach (Kind kind in NewMechanics(GridManager.Instance.CurrentLevel)) _queue.Enqueue(kind);
        ShowNext();
    }

    private bool CanStartNow()
    {
        LevelManager lm = LevelManager.Instance;
        GridManager gm = GridManager.Instance;
        if (lm == null || gm == null || gm.Board == null || gm.CurrentLevel == null || !lm.IsPlaying) return false;
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return false;
        if (TutorialController.Active || BoosterTutorial.Active || DifficultyIntro.Playing || gm.IsSelecting) return false;
        if (_spawner != null && _spawner.IsIntroPlaying) return false;
        return true;
    }

    /// <summary>Cơ chế có trong level mà người chơi chưa xem, theo thứ tự Mechanic Injector giới thiệu.</summary>
    private static List<Kind> NewMechanics(LevelDefinition level)
    {
        List<Kind> result = new List<Kind>();
        if (level == null || level.clues == null) return result;
        bool constraint = false, locked = false, hidden = false;
        foreach (ClueData c in level.clues)
        {
            constraint |= c.mechanic.constraint != ClueConstraint.None;
            locked |= c.mechanic.lockSteps > 0;
            hidden |= c.mechanic.hidden;
        }
        if (constraint && !Seen(Kind.Constraint)) result.Add(Kind.Constraint);
        if (locked && !Seen(Kind.Lock)) result.Add(Kind.Lock);
        if (hidden && !Seen(Kind.Hidden)) result.Add(Kind.Hidden);
        return result;
    }

    private static bool Seen(Kind kind) => SaveSystem.Data.seenMechanics.Contains(kind.ToString());

    private void ShowNext()
    {
        if (_queue.Count == 0 || popup == null)
        {
            Active = false;
            return;
        }
        Kind kind = _queue.Dequeue();
        Active = true;
        switch (kind)
        {
            case Kind.Constraint:
                popup.Show("Orientation", "Blocks must be vertical, square, or horizontal, as the arrow shows.", constraintSprite, () => Done(kind));
                break;
            case Kind.Lock:
                popup.Show("Locked Number", "Place other blocks first. The lock opens after the shown number of blocks.", lockSprite, () => Done(kind));
                break;
            default:
                popup.Show("Hidden Number", "The \"?\" hides the number. Work out its rectangle from the cells around it.", hiddenSprite, () => Done(kind));
                break;
        }
    }

    private void Done(Kind kind)
    {
        if (!Seen(kind)) SaveSystem.Data.seenMechanics.Add(kind.ToString());
        SaveSystem.MarkDirty();
        ShowNext();
    }
}
