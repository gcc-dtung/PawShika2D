/// <summary>
/// Các sự kiện toàn game đi qua EventBus. Thêm sự kiện mới: thêm 1 readonly struct : IGameEvent ở đây,
/// bên phát gọi EventBus&lt;X&gt;.Raise, bên nghe Subscribe / Unsubscribe.
/// </summary>

/// <summary>LevelManager vừa nạp 1 level thường (không phát cho màn tutorial).</summary>
public readonly struct LevelLoadedEvent : IGameEvent
{
    public readonly int Number;           // số level hiển thị (bắt đầu từ 1)
    public readonly LevelDefinition Level;

    public LevelLoadedEvent(int number, LevelDefinition level)
    {
        Number = number;
        Level = level;
    }
}

/// <summary>Trạng thái ván đổi (Playing / Paused / Won / OutOfLives / Lost).</summary>
public readonly struct GameStateChangedEvent : IGameEvent
{
    public readonly GameState State;

    public GameStateChangedEvent(GameState state)
    {
        State = state;
    }
}

/// <summary>Thắng level cuối cùng của database (màn "Coming soon").</summary>
public readonly struct AllLevelsCompletedEvent : IGameEvent
{
    public readonly int LevelCount;

    public AllLevelsCompletedEvent(int levelCount)
    {
        LevelCount = levelCount;
    }
}

/// <summary>ScreenFlow chuyển giữa Main Menu và màn chơi.</summary>
public readonly struct ScreenChangedEvent : IGameEvent
{
    public readonly bool IsMenu;

    public ScreenChangedEvent(bool isMenu)
    {
        IsMenu = isMenu;
    }
}

/// <summary>Booster vừa mở khóa / khóa lại (tutorial hoặc Debug): nút booster vẽ lại.</summary>
public readonly struct BoosterLockChangedEvent : IGameEvent
{
    public readonly BoosterType Type;
    public readonly bool Unlocked;

    public BoosterLockChangedEvent(BoosterType type, bool unlocked)
    {
        Type = type;
        Unlocked = unlocked;
    }
}

/// <summary>Người chơi bấm đúng nút trong tutorial mở khóa booster (bước tiếp theo riêng của từng booster nghe ở đây).</summary>
public readonly struct BoosterTutorialCompletedEvent : IGameEvent
{
    public readonly BoosterType Type;

    public BoosterTutorialCompletedEvent(BoosterType type)
    {
        Type = type;
    }
}
