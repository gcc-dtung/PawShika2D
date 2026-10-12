using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Nối rung điện thoại vào các sự kiện có sẵn của game (giống GameAudioEvents cho âm thanh). Kiểu rung xem HapticType:
    /// - Thao tác liên tục rung rất nhẹ: ngón tay kéo sang ô khác (Selection), đặt khối (Light), gỡ khối (Soft), mở khóa ô (Rigid).
    /// - Coin bay vào ô coin ở màn thắng: mỗi đồng 1 tick (Coin, gọi trong WinPopup).
    /// - Booster (Medium); đóng băng bật (Rise, mạnh dần) / băng vỡ (Thud); lưới kín mà sai (Heavy).
    /// - Sắp hết giờ: vừa xuống dưới ngưỡng (Warning) rồi mỗi giây (Tick). Thắng (Success), hết giờ / thua (Failure).
    /// Đi qua AudioManager.Vibrate nên tắt "Rung" trong Cài đặt là tắt hết. Từng nhóm tắt riêng được ở Inspector.
    /// Đặt cùng AudioManager / GameAudioEvents trong scene.
    /// </summary>
    public class GameHapticEvents : MonoBehaviour
    {
        private BoosterManager _boosterManager;
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid, BoosterManager boosterManager)
        {
            _grid = grid;
            _boosterManager = boosterManager;
        }

        [SerializeField] private bool onBlockPlaced = true;
        [SerializeField] private bool onClueUnlocked = true;
        [Tooltip("Tick rất nhẹ mỗi khi ngón tay đang kéo chạm sang ô khác")]
        [SerializeField] private bool onSelection = true;
        [Tooltip("Tick mỗi giây khi sắp hết giờ")]
        [SerializeField] private bool onLowTimeTick = true;

        private LevelTimer _timer;
        private BoosterManager _booster;

        private void Start()
        {
            _timer = LevelTimer.Instance;
            _booster = _boosterManager;

            if (_grid != null)
            {
                _grid.onBlockPlaced.AddListener(OnBlockPlaced);
                _grid.onBlockRemoved.AddListener(OnBlockRemoved);
                _grid.onMistake.AddListener(OnMistake);
                _grid.onClueUnlocked.AddListener(OnClueUnlocked);
                _grid.onDragCellChanged.AddListener(OnDragCellChanged);
                _grid.onInvalidMove.AddListener(OnInvalidMove);
            }
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            if (_timer != null)
            {
                _timer.onFreezeChanged.AddListener(OnFreezeChanged);
                _timer.onFreezeEnded.AddListener(OnFreezeEnded);
                _timer.onTimeWarning.AddListener(OnTimeWarning);
                _timer.onLowTick.AddListener(OnLowTick);
            }
            if (_booster != null) _booster.onBoosterUsed.AddListener(OnBoosterUsed);
        }

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
                _grid.onBlockRemoved.RemoveListener(OnBlockRemoved);
                _grid.onMistake.RemoveListener(OnMistake);
                _grid.onClueUnlocked.RemoveListener(OnClueUnlocked);
                _grid.onDragCellChanged.RemoveListener(OnDragCellChanged);
                _grid.onInvalidMove.RemoveListener(OnInvalidMove);
            }
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            if (_timer != null)
            {
                _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
                _timer.onFreezeEnded.RemoveListener(OnFreezeEnded);
                _timer.onTimeWarning.RemoveListener(OnTimeWarning);
                _timer.onLowTick.RemoveListener(OnLowTick);
            }
            if (_booster != null) _booster.onBoosterUsed.RemoveListener(OnBoosterUsed);
        }

        private void OnBlockPlaced(BlockModel block)
        {
            if (onBlockPlaced) AudioManager.Vibrate(HapticType.Light);
        }

        private void OnBlockRemoved(BlockModel block) => AudioManager.Vibrate(HapticType.Soft);

        private void OnMistake(Vector3 center, Vector2 size) => AudioManager.Vibrate(HapticType.Heavy);

        private void OnClueUnlocked(CellModel cell)
        {
            if (onClueUnlocked) AudioManager.Vibrate(HapticType.Rigid);
        }

        private void OnDragCellChanged(Vector2Int cell)
        {
            if (onSelection) AudioManager.Vibrate(HapticType.Selection);
        }

        // Thả tay ở vùng sai: rung mềm (nhẹ hơn lưới kín mà sai)
        private void OnInvalidMove(PlacementResult result, Vector3 center, Vector2 size) => AudioManager.Vibrate(HapticType.Soft);

        private void OnStateChanged(GameStateChangedEvent e)
        {
            switch (e.State)
            {
                case GameState.Won: AudioManager.Vibrate(HapticType.Success); break;
                case GameState.OutOfTime:
                case GameState.Lost: AudioManager.Vibrate(HapticType.Failure); break;
            }
        }

        private void OnFreezeChanged(bool on)
        {
            if (on) AudioManager.Vibrate(HapticType.Rise);
        }

        private void OnFreezeEnded() => AudioManager.Vibrate(HapticType.Thud);

        private void OnTimeWarning() => AudioManager.Vibrate(HapticType.Warning);

        private void OnLowTick(int seconds)
        {
            if (onLowTimeTick) AudioManager.Vibrate(HapticType.Tick);
        }

        // Đóng băng đã có Rise (onFreezeChanged), không rung chồng thêm
        private void OnBoosterUsed(BoosterType type)
        {
            if (type != BoosterType.Freeze) AudioManager.Vibrate(HapticType.Medium);
        }
    }
}
