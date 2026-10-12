using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Đồng hồ đếm ngược của 1 level (cơ chế THỜI GIAN như bản gốc Paw Block).
    /// Thời gian = LevelDefinition.rules.timeLimit nếu > 0 (đặt trong Level Editor), không thì tự tính:
    /// baseSeconds + secondsPerCell x số ô + (5.5–6.5 s) x số khối, tối thiểu minSeconds (LivesEnergyConfig).
    /// Chỉ chạy khi người chơi thao tác được (ván Playing, lưới đã hiện, không có intro / popup / booster tự đặt khối).
    /// Booster Đóng băng (Freeze) dừng đồng hồ freezeSeconds giây. Hết giờ -> onTimeUp.
    /// Level mới / chơi lại: đồng hồ đứng ở tổng thời gian tới khi người chơi chạm màn hình lần đầu (chạm lúc intro / popup không tính).
    /// </summary>
    public class LevelTimer : MonoBehaviour
    {
        private GridManager _grid;
        private LevelManager _levels;
        private IScreenNavigator _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IScreenNavigator screens, LevelManager levels, GridManager grid)
        {
            _screens = screens;
            _levels = levels;
            _grid = grid;
        }

        [SerializeField] private GridSpawner gridSpawner;
        [Tooltip("Level mới: đồng hồ chỉ bắt đầu chạy khi người chơi chạm màn hình lần đầu")]
        [SerializeField] private bool waitForFirstTouch = true;

        [Header("Events")]
        public UnityEvent<float, float> onTimeChanged = new UnityEvent<float, float>(); // (còn lại, tổng) – mỗi frame đồng hồ chạy / khi cộng giờ
        public UnityEvent<float> onTimeAdded = new UnityEvent<float>();                  // số giây vừa cộng (popup thêm giờ, cheat)
        public UnityEvent onTimeWarning = new UnityEvent();              // vừa xuống dưới warningSeconds
        public UnityEvent<int> onLowTick = new UnityEvent<int>();        // mỗi giây trôi qua khi đang dưới warningSeconds (số giây còn lại)
        public UnityEvent onTimeUp = new UnityEvent();
        public UnityEvent<bool> onFreezeChanged = new UnityEvent<bool>(); // bật / tắt đóng băng
        public UnityEvent onFreezeEnded = new UnityEvent();               // đóng băng hết hạn (băng vỡ)
        public UnityEvent<bool> onWaitingForTouchChanged = new UnityEvent<bool>(); // true: đang chờ chạm đầu tiên; false: vừa bắt đầu chạy

        private float _total;
        private float _remaining;
        private float _freezeLeft;
        private bool _timeUp;
        private bool _waitingForTouch;

        public float Total => _total;
        public float Remaining => _remaining;
        public float FreezeLeft => _freezeLeft;
        public bool IsFrozen => _freezeLeft > 0f;
        public bool IsTimeUp => _timeUp;

        /// <summary>Level vừa nạp, đồng hồ chưa chạy: chờ người chơi chạm màn hình lần đầu.</summary>
        public bool IsWaitingForTouch => _waitingForTouch;

        /// <summary>Đang dưới ngưỡng cảnh báo (chữ đỏ, nhịp tim đồng hồ).</summary>
        public bool IsLow => !_timeUp && _remaining <= GameConfig.LivesEnergy.warningSeconds;

        /// <summary>Màn luyện tập (tutorial lần đầu): đồng hồ không chạy. LevelManager đặt khi nạp màn.</summary>
        public bool IsPractice
        {
            get => _isPractice;
            set
            {
                _isPractice = value;
                if (value) SetWaitingForTouch(false); // màn luyện tập không tính giờ: không chờ chạm (đồng hồ khỏi nhấp nháy)
            }
        }
        private bool _isPractice;

        /// <summary>Tạm dừng thêm (Cheat: dừng đồng hồ).</summary>
        public bool DebugPaused { get; set; }

        private void Awake()
        {
            if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        }

        private void OnEnable()
        {
            if (gridSpawner != null) gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void OnDisable()
        {
            if (gridSpawner != null) gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        }

        // Ván kết thúc (thắng / hết giờ / thua) mà còn đóng băng: đồng hồ không chạy nữa nên đóng băng không tự hết,
        // hiệu ứng băng sẽ đứng mãi -> tắt đóng băng (tan, không vỡ). Tạm dừng (Paused) thì giữ để chơi tiếp.
        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State == GameState.Won || e.State == GameState.OutOfTime || e.State == GameState.Lost) SetFrozen(0f);
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
            SetWaitingForTouch(waitForFirstTouch && !IsPractice);
            onTimeChanged?.Invoke(_remaining, _total);
        }

        /// <summary>Bắt đầu chạy ngay, không chờ chạm (vd. cheat, tutorial muốn tính giờ).</summary>
        public void StartNow() => SetWaitingForTouch(false);

        private void SetWaitingForTouch(bool waiting)
        {
            if (waiting == _waitingForTouch) return;
            _waitingForTouch = waiting;
            onWaitingForTouchChanged?.Invoke(waiting);
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
            if (_waitingForTouch)
            {
                // Chỉ tính chạm khi người chơi đã thao tác được (qua intro, popup đã đóng); chạm trúng nút cũng tính
                Pointer pointer = Pointer.current;
                if (pointer == null || !pointer.press.wasPressedThisFrame) return;
                SetWaitingForTouch(false);
            }
            float dt = Time.deltaTime;

            if (_freezeLeft > 0f)
            {
                float left = _freezeLeft - dt;
                if (left > 0f)
                {
                    _freezeLeft = left;
                    return;
                }
                // Không trừ thẳng _freezeLeft về <= 0 rồi mới SetFrozen: lúc đó IsFrozen đã false nên SetFrozen
                // tưởng không đổi và không phát onFreezeChanged(false) -> tuyết / đếm ngược trên nút không tắt
                SetFrozen(0f);
                onFreezeEnded?.Invoke();
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
            if (_levels != null && !_levels.IsPlaying) return false;
            if (_screens != null && _screens.IsMenu) return false;
            GridManager gm = _grid;
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
}
