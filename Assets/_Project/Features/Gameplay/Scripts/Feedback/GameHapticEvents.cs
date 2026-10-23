using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Nối rung điện thoại vào các sự kiện có sẵn của game (giống GameAudioEvents cho âm thanh). Kiểu rung xem HapticType:
    /// - Thao tác liên tục rung rất nhẹ: ngón tay kéo sang ô khác (Selection), đặt khối (Light), gỡ khối (Soft), mở khóa ô (Rigid).
    /// - Coin bay vào ô coin ở màn thắng: mỗi đồng 1 tick (Coin, gọi trong WinPopup).
    /// - Kéo sai: vùng kéo vừa chuyển sang "chắc chắn sai" (Tick). Thả tay bị từ chối (Warning); thiếu ô chỉ Selection, không có ô số thì Soft.
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
        public void Init(GridManager grid, BoosterManager boosterManager, LevelTimer timer)
        {
            _grid = grid;
            _boosterManager = boosterManager;
            _timer = timer;
        }

        [SerializeField] private bool onBlockPlaced = true;
        [SerializeField] private bool onClueUnlocked = true;
        [Tooltip("Tick rất nhẹ mỗi khi ngón tay đang kéo chạm sang ô khác")]
        [SerializeField] private bool onSelection = true;
        [Tooltip("Tick khi vùng đang kéo vừa chuyển sang sai")]
        [SerializeField] private bool onInvalidDrag = true;
        [Tooltip("Tick mỗi giây khi sắp hết giờ")]
        [SerializeField] private bool onLowTimeTick = true;

        private LevelTimer _timer;
        private BoosterManager _booster;
        private SelectionHint _dragHint;

        private void Start()
        {
            _booster = _boosterManager;

            if (_grid != null)
            {
                _grid.onBlockPlaced.AddListener(OnBlockPlaced);
                _grid.onBlockRemoved.AddListener(OnBlockRemoved);
                _grid.onMistake.AddListener(OnMistake);
                _grid.onClueUnlocked.AddListener(OnClueUnlocked);
                _grid.onDragCellChanged.AddListener(OnDragCellChanged);
                _grid.onInvalidMove.AddListener(OnInvalidMove);
                _grid.onSelectionChanged.AddListener(OnSelectionChanged);
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
                _grid.onSelectionChanged.RemoveListener(OnSelectionChanged);
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

        private void OnSelectionChanged(int count, SelectionHint hint)
        {
            SelectionHint prev = _dragHint;
            _dragHint = count == 0 ? SelectionHint.Neutral : hint;
            if (onInvalidDrag && _dragHint == SelectionHint.Invalid && prev != SelectionHint.Invalid) AudioManager.Vibrate(HapticType.Tick);
        }

        // Thả tay ở vùng sai: Warning (2 nhịp, vẫn nhẹ hơn lưới kín mà sai). Thiếu ô (hay do nhấc tay sớm) rung rất nhẹ;
        // không có ô số nào thì không có gì để chỉ -> rung mềm
        private void OnInvalidMove(PlacementResult result, Vector3 center, Vector2 size)
        {
            PlacementCheck check = _grid != null ? _grid.LastRejected : default;
            if (result == PlacementResult.WrongSize && check.ClueCell != null && check.Cells != null
                && check.Cells.Count < check.ClueCell.Clue.MinArea)
                AudioManager.Vibrate(HapticType.Selection);
            else if (result == PlacementResult.ClueCount && CountClues(check) == 0)
                AudioManager.Vibrate(HapticType.Soft);
            else
                AudioManager.Vibrate(HapticType.Warning);
        }

        private static int CountClues(PlacementCheck check)
        {
            int n = 0;
            if (check.Cells != null) foreach (CellModel cell in check.Cells) if (cell.HasClue) n++;
            return n;
        }

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
