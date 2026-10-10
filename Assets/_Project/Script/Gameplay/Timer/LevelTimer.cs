using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Đồng hồ đếm ngược của 1 level (cơ chế THỜI GIAN như bản gốc Paw Block).
/// Thời gian = LevelDefinition.rules.timeLimit nếu > 0 (đặt trong Level Editor), không thì tự tính:
/// baseSeconds + secondsPerCell x số ô + (5.5–6.5 s) x số khối, tối thiểu minSeconds (LivesEnergyConfig).
/// Chỉ chạy khi người chơi thao tác được (ván Playing, lưới đã hiện, không có intro / popup / booster tự đặt khối).
/// Booster Đóng băng (Freeze) dừng đồng hồ freezeSeconds giây. Hết giờ -> onTimeUp.
/// </summary>
public class LevelTimer : Singleton<LevelTimer>
{
    [SerializeField] private GridSpawner gridSpawner;

    [Header("Events")]
    public UnityEvent<float, float> onTimeChanged = new UnityEvent<float, float>(); // (còn lại, tổng) – mỗi frame đồng hồ chạy / khi cộng giờ
    public UnityEvent<float> onTimeAdded = new UnityEvent<float>();                  // số giây vừa cộng (popup thêm giờ, cheat)
    public UnityEvent onTimeWarning = new UnityEvent();              // vừa xuống dưới warningSeconds
    public UnityEvent<int> onLowTick = new UnityEvent<int>();        // mỗi giây trôi qua khi đang dưới warningSeconds (số giây còn lại)
    public UnityEvent onTimeUp = new UnityEvent();
    public UnityEvent<bool> onFreezeChanged = new UnityEvent<bool>(); // bật / tắt đóng băng
    public UnityEvent onFreezeEnded = new UnityEvent();               // đóng băng hết hạn (băng vỡ)

    private float _total;
    private float _remaining;
    private float _freezeLeft;
    private bool _timeUp;

    public float Total => _total;
    public float Remaining => _remaining;
    public float FreezeLeft => _freezeLeft;
    public bool IsFrozen => _freezeLeft > 0f;
    public bool IsTimeUp => _timeUp;

    /// <summary>Đang dưới ngưỡng cảnh báo (chữ đỏ, nhịp tim đồng hồ).</summary>
    public bool IsLow => !_timeUp && _remaining <= GameConfig.LivesEnergy.warningSeconds;

    /// <summary>Màn luyện tập (tutorial lần đầu): đồng hồ không chạy. LevelManager đặt khi nạp màn.</summary>
    public bool IsPractice { get; set; }

    /// <summary>Tạm dừng thêm (Cheat: dừng đồng hồ).</summary>
    public bool DebugPaused { get; set; }

    protected override void Awake()
    {
        base.Awake();
        if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
    }

    private void OnEnable()
    {
        if (gridSpawner != null) gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
    }

    private void OnDisable()
    {
        if (gridSpawner != null) gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
    }

    private void OnGridSpawned(List<CellView> grid)
    {
        ResetTimer(gridSpawner != null ? gridSpawner.LevelData : null, grid.Count);
    }

    /// <summary>Level mới / chơi lại: tính lại tổng thời gian, đóng băng chưa hết cũng mất.</summary>
    public void ResetTimer(LevelDefinition level, int cellCount)
    {
        SetFrozen(0f);
        _timeUp = false;
        _total = ComputeSeconds(level, cellCount);
        _remaining = _total;
        onTimeChanged?.Invoke(_remaining, _total);
    }

    /// <summary>
    /// Thời gian của level: rules.timeLimit nếu > 0, không thì 8 s + 0.25 s/ô + 5.5–6.5 s/khối (mỗi ô số là 1 khối),
    /// tối thiểu 45 s – số của GameplayController gốc.
    /// </summary>
    public static float ComputeSeconds(LevelDefinition level, int cellCount)
    {
        if (level != null && level.rules.timeLimit > 0) return level.rules.timeLimit;
        LivesEnergyConfig cfg = GameConfig.LivesEnergy;
        int blocks = level != null && level.clues != null ? level.clues.Length : 0;
        float seconds = cfg.baseSeconds + cfg.secondsPerCell * cellCount;
        for (int i = 0; i < blocks; i++) seconds += Random.Range(cfg.secondsPerBlockMin, cfg.secondsPerBlockMax);
        return Mathf.Ceil(Mathf.Max(cfg.minSeconds, seconds));
    }

    /// <summary>Thời gian tự tính trung bình (giây/khối lấy giữa min – max), cho Level Editor hiện khi timeLimit = 0.</summary>
    public static int EstimateAutoSeconds(int blocks, int cellCount)
    {
        LivesEnergyConfig cfg = GameConfig.LivesEnergy;
        float perBlock = (cfg.secondsPerBlockMin + cfg.secondsPerBlockMax) * 0.5f;
        float seconds = cfg.baseSeconds + cfg.secondsPerCell * cellCount + perBlock * blocks;
        return Mathf.CeilToInt(Mathf.Max(cfg.minSeconds, seconds));
    }

    private void Update()
    {
        if (!IsRunning()) return;
        float dt = Time.deltaTime;

        if (_freezeLeft > 0f)
        {
            _freezeLeft -= dt;
            if (_freezeLeft <= 0f)
            {
                SetFrozen(0f);
                onFreezeEnded?.Invoke();
            }
            return;
        }

        bool wasLow = IsLow;
        int wasSeconds = Mathf.CeilToInt(_remaining);
        _remaining = Mathf.Max(0f, _remaining - dt);
        onTimeChanged?.Invoke(_remaining, _total);
        int seconds = Mathf.CeilToInt(_remaining);
        if (!wasLow && IsLow && _remaining > 0f) onTimeWarning?.Invoke();
        else if (wasLow && seconds < wasSeconds && seconds > 0) onLowTick?.Invoke(seconds);
        if (_remaining > 0f) return;

        _timeUp = true;
        Debug.Log("<color=red>✖ ✖ ✖ HẾT GIỜ -> GAME OVER ✖ ✖ ✖</color>");
        onTimeUp?.Invoke();
    }

    /// <summary>Đồng hồ chỉ chạy khi người chơi đang thao tác được trên lưới.</summary>
    private bool IsRunning()
    {
        if (_timeUp || IsPractice || DebugPaused || _total <= 0f) return false;
        if (LevelManager.Instance != null && !LevelManager.Instance.IsPlaying) return false;
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return false;
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.Board == null || gm.IsGameWon) return false;
        if (gridSpawner != null && gridSpawner.IsIntroPlaying) return false;
        return !InputGate.IsLocked;
    }

    /// <summary>Bật đóng băng <paramref name="seconds"/> giây. Trả về false nếu đang đóng băng hoặc đã hết giờ.</summary>
    public bool Freeze(float seconds)
    {
        if (IsFrozen || _timeUp || seconds <= 0f) return false;
        SetFrozen(seconds);
        return true;
    }

    /// <summary>Hủy đóng băng ngay, không có hiệu ứng vỡ (Cheat: hết giờ ngay).</summary>
    public void ResetFreeze() => SetFrozen(0f);

    private void SetFrozen(float seconds)
    {
        bool was = IsFrozen;
        _freezeLeft = Mathf.Max(0f, seconds);
        if (was != IsFrozen) onFreezeChanged?.Invoke(IsFrozen);
    }

    /// <summary>Cộng giờ (popup thêm giờ: xem ads / trả coin; cheat). Hết giờ rồi thì chơi tiếp được.</summary>
    public void AddTime(float seconds)
    {
        if (seconds <= 0f) return;
        _remaining += seconds;
        _total = Mathf.Max(_total, _remaining);
        _timeUp = false;
        onTimeAdded?.Invoke(seconds);
        onTimeChanged?.Invoke(_remaining, _total);
    }

    /// <summary>Đặt số giây còn lại (Cheat). 0 = hết giờ ngay ở frame kế.</summary>
    public void SetRemaining(float seconds)
    {
        _remaining = Mathf.Max(0f, seconds);
        _total = Mathf.Max(_total, _remaining);
        if (_remaining > 0f) _timeUp = false;
        onTimeChanged?.Invoke(_remaining, _total);
    }
}
