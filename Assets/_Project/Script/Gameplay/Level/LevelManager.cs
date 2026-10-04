using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Luồng 1 level và trạng thái ván chơi:
/// Playing -> Won (WinPopup) -> level kế
/// Playing -> OutOfLives (ReviveLivesPopup) -> hồi mạng: Playing / bỏ cuộc: Lost (LosePopup) -> chơi lại.
/// Popup nghe onStateChanged; scene không có popup (GameFlowUI) thì tự chuyển như cũ sau autoAdvanceDelay.
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    [SerializeField] private int currentLevel = 0;
    
    [SerializeField] private LevelDatabase database;
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private float autoAdvanceDelay = 1.5f; // chỉ dùng khi không có GameFlowUI
    [Tooltip("Chờ hiệu ứng đặt khối / mèo rung xong rồi mới đổi trạng thái (popup hiện sau khoảng này)")]
    [SerializeField] private float stateChangeDelay = 0.8f;

    [Header("Camera fit")]
    [Tooltip("Vùng chơi trong Canvas_HUD: lưới luôn nằm trọn trong vùng này. Không có (hoặc đang tắt) thì dùng tỉ lệ bên dưới.")]
    [SerializeField] private PlayZone playZone;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0.3f, 1f)] private float boardHeightRatio = 0.62f;
    [SerializeField, Range(0.5f, 1f)] private float boardWidthRatio = 0.92f;
    [SerializeField, Range(-0.5f, 0.5f)] private float boardCenterOffset = 0.02f; // >0: lưới lệch lên trên
    [SerializeField] private float minOrthoSize = 5f;

    [Header("Events")]
    public UnityEvent<int> onLevelLoaded = new UnityEvent<int>(); // số level hiển thị (bắt đầu từ 1)
    public UnityEvent<GameState> onStateChanged = new UnityEvent<GameState>();

    private int _currentIndex;
    private Tween _pending; // hẹn giờ đổi trạng thái / tự chuyển level
    private GameState _state = GameState.Playing;
    private bool _hasRevived; // đã hồi mạng trong lượt chơi này (lượt sau hết mạng có thể giới hạn)

    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;
    public bool HasRevived => _hasRevived;

    public int CurrentIndex => _currentIndex;
    public int LevelCount => database != null ? database.Count : 0;
    public int CurrentLevelNumber => _currentIndex + 1;
    public LevelDefinition CurrentLevel => database != null ? database.Get(_currentIndex) : null;

    protected override void Awake()
    {
        base.Awake();
        if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        if (targetCamera == null) targetCamera = Camera.main;
        _currentIndex = SaveSystem.Data.currentLevel;
    }

    private void Start()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.AddListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.AddListener(OnOutOfLives);
        LoadLevel(_currentIndex);
    }

    private void OnDisable() => CancelPending();

    private void OnDestroy()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.RemoveListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.RemoveListener(OnOutOfLives);
    }

    public void LoadLevel(int index)
    {
        if (database == null || database.Count == 0)
        {
            Debug.LogError("[LevelManager] Chưa có LevelDatabase. Chạy menu Tools/PawBlock/Import ShikaCat Levels.");
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
        gridSpawner.Spawn(level);
        FitCamera(level);
        _hasRevived = false;
        SetState(GameState.Playing);
        onLevelLoaded?.Invoke(CurrentLevelNumber);
        Debug.Log($"[LevelManager] Level {CurrentLevelNumber}/{database.Count}: {level.name} ({level.size}x{level.Height})");
    }

    public void Retry() => LoadLevel(_currentIndex);

    public void NextLevel()
    {
        // Hết level thì quay vòng về level 1 (tạm thời, chưa có màn "Coming soon")
        int next = _currentIndex + 1 >= database.Count ? 0 : _currentIndex + 1;
        LoadLevel(next);
    }

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
        CancelPending();
        After(stateChangeDelay, () =>
        {
            SetState(GameState.Won);
            if (!HasFlowUI) After(autoAdvanceDelay, NextLevel);
        });
    }

    private void OnOutOfLives()
    {
        CancelPending();
        After(stateChangeDelay, () =>
        {
            SetState(GameState.OutOfLives);
            if (!HasFlowUI) After(autoAdvanceDelay, Retry);
        });
    }

    private void SetState(GameState state)
    {
        if (_state == state) return;
        _state = state;
        Debug.Log($"[LevelManager] State -> {state}");
        onStateChanged?.Invoke(state);
    }

    private static bool HasFlowUI => FindFirstObjectByType<GameFlowUI>() != null;

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

    /// <summary>
    /// Phóng camera cho lưới vừa vùng chơi. Có PlayZone: PlayZone căn (và tự căn lại khi màn hình đổi).
    /// PlayZone đang tắt (ở Menu) thì căn tạm theo tỉ lệ màn hình; vào màn chơi PlayZone căn lại.
    /// </summary>
    private void FitCamera(LevelDefinition level)
    {
        if (level == null) return;
        float gridW = level.size * GridSpawner.CellSpacing;
        float gridH = level.Height * GridSpawner.CellSpacing;

        if (playZone != null)
        {
            playZone.SetContent(new Vector2(gridW, gridH));
            if (playZone.isActiveAndEnabled) return;
        }

        if (targetCamera == null || !targetCamera.orthographic) return;
        float aspect = targetCamera.aspect;

        float sizeByHeight = gridH / 2f / boardHeightRatio;
        float sizeByWidth = gridW / 2f / (aspect * boardWidthRatio);
        float ortho = Mathf.Max(minOrthoSize, sizeByHeight, sizeByWidth);
        targetCamera.orthographicSize = ortho;

        // Dời camera xuống để lưới nằm hơi cao hơn giữa màn hình
        Vector3 pos = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(0f, -boardCenterOffset * 2f * ortho, pos.z);
    }
    
    [ContextMenu("Load Level")]
    public void GoToLevel()
    {
        LoadLevel(currentLevel); // LoadLevel tự lưu level hiện tại
    }
}
