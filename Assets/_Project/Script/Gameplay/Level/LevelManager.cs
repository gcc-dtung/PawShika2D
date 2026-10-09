using DG.Tweening;
using UnityEngine;

/// <summary>
/// Luồng 1 level và trạng thái ván chơi:
/// Playing -> Won (WinPopup) -> level kế
/// Playing -> OutOfLives (ReviveLivesPopup) -> hồi mạng: Playing / bỏ cuộc: Lost (LosePopup) -> chơi lại.
/// Phát LevelLoadedEvent / GameStateChangedEvent qua EventBus; ván không Playing thì khóa lưới (InputGate).
/// Có bên xử lý luồng (GameFlowUI đặt AutoAdvance = false) thì chờ popup; không có thì tự chuyển sau autoAdvanceDelay.
/// Căn camera theo lưới: BoardCameraFitter.
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    [SerializeField] private int currentLevel = 0;
    
    [SerializeField] private LevelDatabase database;
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private float autoAdvanceDelay = 1.5f; // chỉ dùng khi AutoAdvance (không có GameFlowUI)
    [Tooltip("Chờ hiệu ứng đặt khối / mèo rung xong rồi mới đổi trạng thái (popup hiện sau khoảng này)")]
    [SerializeField] private float stateChangeDelay = 0.8f;

    private int _currentIndex;
    private Tween _pending; // hẹn giờ đổi trạng thái / tự chuyển level
    private GameState _state = GameState.Playing;
    private bool _hasRevived; // đã hồi mạng trong lượt chơi này (lượt sau hết mạng có thể giới hạn)
    private bool _isPractice; // màn tutorial: thắng không sang WinPopup, sai không trừ mạng

    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;
    public bool HasRevived => _hasRevived;
    public bool IsPractice => _isPractice;

    /// <summary>Thắng / hết mạng thì tự sang level kế / chơi lại. GameFlowUI tắt đi để popup quyết định.</summary>
    public bool AutoAdvance { get; set; } = true;

    public int CurrentIndex => _currentIndex;
    public int LevelCount => database != null ? database.Count : 0;
    public int CurrentLevelNumber => _currentIndex + 1;
    public LevelDefinition CurrentLevel => database != null ? database.Get(_currentIndex) : null;

    protected override void Awake()
    {
        base.Awake();
        if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        _currentIndex = SaveSystem.Data.currentLevel;
    }

    private void Start()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.AddListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.AddListener(OnOutOfLives);
        if (!TutorialController.Pending) LoadLevel(_currentIndex); // tutorial lần đầu: ScreenFlow chạy tutorial thay vì nạp level
    }

    private void OnDisable()
    {
        CancelPending();
        InputGate.Unlock(this);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.RemoveListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.RemoveListener(OnOutOfLives);
    }

    public void LoadLevel(int index)
    {
        if (database == null || database.Count == 0)
        {
            Debug.LogError("[LevelManager] Chưa có LevelDatabase. Chạy menu Tools/PawBlock/Builders/Import ShikaCat Levels.");
            return;
        }

        CancelPending();
        _currentIndex = Mathf.Clamp(index, 0, database.Count - 1);
        if (SaveSystem.Data.currentLevel != _currentIndex)
        {
            SaveSystem.Data.currentLevel = _currentIndex;
            SaveSystem.MarkDirty();
        }

        LevelDefinition level = database.Get(_currentIndex);
        SetPractice(false);
        gridSpawner.Spawn(level);
        _hasRevived = false;
        SetState(GameState.Playing);
        EventBus<LevelLoadedEvent>.Raise(new LevelLoadedEvent(CurrentLevelNumber, level));
        Debug.Log($"[LevelManager] Level {CurrentLevelNumber}/{database.Count}: {level.name} ({level.size}x{level.Height})");
    }

    /// <summary>
    /// Màn tutorial (TutorialController): dựng lưới như level thường nhưng không lưu level hiện tại,
    /// không phát LevelLoadedEvent (HUD level / intro / booster không đổi), thắng không sang WinPopup, sai không trừ mạng.
    /// </summary>
    public void LoadTutorial(LevelDefinition level)
    {
        if (level == null) return;
        CancelPending();
        SetPractice(true);
        gridSpawner.Spawn(level);
        _hasRevived = false;
        SetState(GameState.Playing);
    }

    public void Retry() => LoadLevel(_currentIndex);

    /// <summary>Đang ở level cuối của database.</summary>
    public bool IsLastLevel => database != null && _currentIndex >= database.Count - 1;

    /// <summary>
    /// Sang level kế. Level cuối: phát AllLevelsCompletedEvent; có bên xử lý (GameFlowUI hiện "Coming soon")
    /// thì chờ bên đó gọi RestartFromFirst, không có thì quay vòng về level 1 luôn.
    /// </summary>
    public void NextLevel()
    {
        if (database == null || database.Count == 0) return;
        if (!IsLastLevel)
        {
            LoadLevel(_currentIndex + 1);
            return;
        }
        if (!SaveSystem.Data.allLevelsCleared)
        {
            SaveSystem.Data.allLevelsCleared = true;
            SaveSystem.MarkDirty();
        }
        EventBus<AllLevelsCompletedEvent>.Raise(new AllLevelsCompletedEvent(database.Count));
        if (AutoAdvance) RestartFromFirst();
    }

    /// <summary>Chơi lại từ level 1 sau khi đã qua hết level.</summary>
    public void RestartFromFirst() => LoadLevel(0);

    public void PreviousLevel() => LoadLevel(Mathf.Max(0, _currentIndex - 1));

    // ---------- Trạng thái ----------

    /// <summary>Popup mở giữa ván (xác nhận chơi lại...): khóa thao tác trên lưới.</summary>
    public void Pause()
    {
        if (_state == GameState.Playing) SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (_state == GameState.Paused) SetState(GameState.Playing);
    }

    /// <summary>Popup Hồi mạng: cộng mạng rồi chơi tiếp đúng lưới đang dở.</summary>
    public void Revive(int lives)
    {
        if (_state != GameState.OutOfLives) return;
        _hasRevived = true;
        if (LivesManager.Instance != null) LivesManager.Instance.AddLives(lives);
        SetState(GameState.Playing);
    }

    /// <summary>Đóng popup Hồi mạng -> thua.</summary>
    public void GiveUp()
    {
        if (_state == GameState.OutOfLives) SetState(GameState.Lost);
    }

    private void OnWin()
    {
        if (_isPractice) return; // TutorialController tự sang bước kế
        CancelPending();
        After(stateChangeDelay, () =>
        {
            SetState(GameState.Won);
            if (AutoAdvance) After(autoAdvanceDelay, NextLevel);
        });
    }

    private void OnOutOfLives()
    {
        CancelPending();
        After(stateChangeDelay, () =>
        {
            SetState(GameState.OutOfLives);
            if (AutoAdvance) After(autoAdvanceDelay, Retry);
        });
    }

    private void SetState(GameState state)
    {
        if (_state == state) return;
        _state = state;
        Debug.Log($"[LevelManager] State -> {state}");
        InputGate.Set(this, state != GameState.Playing);
        EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(state));
    }

    private void SetPractice(bool practice)
    {
        _isPractice = practice;
        if (LivesManager.Instance != null) LivesManager.Instance.IsPractice = practice;
    }

    /// <summary>Hẹn gọi action sau delay giây (thay hẹn cũ nếu có). Manager tắt thì hủy, action không chạy.</summary>
    private void After(float delay, System.Action action)
    {
        CancelPending();
        Tween tween = null;
        tween = DOVirtual.DelayedCall(delay, () =>
            {
                if (_pending == tween) _pending = null;
                action();
            })
            .SetLink(gameObject);
        _pending = tween;
    }

    private void CancelPending()
    {
        _pending?.Kill();
        _pending = null;
    }

    [ContextMenu("Load Level")]
    public void GoToLevel()
    {
        LoadLevel(currentLevel); // LoadLevel tự lưu level hiện tại
    }
}
