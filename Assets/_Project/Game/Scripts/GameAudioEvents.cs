using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Nối âm thanh vào các sự kiện có sẵn của game (không phải sửa từng script gameplay):
    /// đặt / gỡ khối, sai, mở khóa, thắng / hết giờ / thua, sắp hết giờ, mất sao, lưới xuất hiện, booster, đóng băng, nhận coin.
    /// Đặt cùng AudioManager. Popup tự phát âm mở / đóng (PopupView), nút UI do UIButtonSounds.
    /// </summary>
    public class GameAudioEvents : MonoBehaviour
    {
        private GridManager _grid;
        private LevelManager _levels;
        private ScreenFlow _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(ScreenFlow screens, LevelManager levels, GridManager grid)
        {
            _screens = screens;
            _levels = levels;
            _grid = grid;
        }

        [Header("Tiếng mèo kèm theo (xác suất mỗi lần)")]
        [SerializeField, Range(0f, 1f)] private float catHappyChance = 0.35f;
        [SerializeField, Range(0f, 1f)] private float catSadChance = 0.8f;

        [Header("Kéo: tick cao dần theo số ô, đủ điều kiện thì ding")]
        [SerializeField] private bool dragSounds = true;
        [SerializeField] private float dragBasePitch = 0.9f;
        [Tooltip("Số nửa cung cộng thêm cho ô thứ 2, 3, ... (ngũ cung cho dễ nghe); quá cuối mảng thì giữ nốt cuối")]
        [SerializeField] private int[] dragSemitones = { 0, 2, 4, 7, 9, 12, 14, 16 };

        [Header("Combo: âm đặt khối cao dần")]
        [SerializeField] private float comboPitchStep = 0.06f;
        [SerializeField] private float comboPitchMax = 1.3f;

        [Header("Sắp hết giờ: nhạc nền nhanh lên")]
        [SerializeField] private float lowTimeMusicPitch = 1.08f;

        private GridSpawner _spawner;
        private LevelTimer _timer;
        private BoosterManager _booster;
        private int _dragCount;
        private SelectionHint _dragHint;

        private void Start()
        {
            _timer = LevelTimer.Instance;
            _booster = BoosterManager.Instance;
            _spawner = FindFirstObjectByType<GridSpawner>();

            if (_grid != null)
            {
                _grid.onBlockPlaced.AddListener(OnBlockPlaced);
                _grid.onBlockRemoved.AddListener(OnBlockRemoved);
                _grid.onMistake.AddListener(OnMistake);
                _grid.onClueUnlocked.AddListener(OnClueUnlocked);
                _grid.onSelectionChanged.AddListener(OnSelectionChanged);
                _grid.onInvalidMove.AddListener(OnInvalidMove);
            }
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<StarsChangedEvent>.Subscribe(OnStarsChanged);
            if (_spawner != null) _spawner.gridCellEvent.AddListener(OnGridSpawned);
            if (_timer != null)
            {
                _timer.onFreezeChanged.AddListener(OnFreezeChanged);
                _timer.onFreezeEnded.AddListener(OnFreezeEnded);
                _timer.onTimeWarning.AddListener(OnTimeWarning);
                _timer.onTimeAdded.AddListener(OnTimeAdded);
                _timer.onTimeChanged.AddListener(OnTimeChanged);
            }
            if (_booster != null) _booster.onBoosterUsed.AddListener(OnBoosterUsed);
            EventBus<CoinsChangedEvent>.Subscribe(OnCoinChanged);
        }

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
                _grid.onBlockRemoved.RemoveListener(OnBlockRemoved);
                _grid.onMistake.RemoveListener(OnMistake);
                _grid.onClueUnlocked.RemoveListener(OnClueUnlocked);
                _grid.onSelectionChanged.RemoveListener(OnSelectionChanged);
                _grid.onInvalidMove.RemoveListener(OnInvalidMove);
            }
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<StarsChangedEvent>.Unsubscribe(OnStarsChanged);
            if (_spawner != null) _spawner.gridCellEvent.RemoveListener(OnGridSpawned);
            if (_timer != null)
            {
                _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
                _timer.onFreezeEnded.RemoveListener(OnFreezeEnded);
                _timer.onTimeWarning.RemoveListener(OnTimeWarning);
                _timer.onTimeAdded.RemoveListener(OnTimeAdded);
                _timer.onTimeChanged.RemoveListener(OnTimeChanged);
            }
            if (_booster != null) _booster.onBoosterUsed.RemoveListener(OnBoosterUsed);
            EventBus<CoinsChangedEvent>.Unsubscribe(OnCoinChanged);
        }

        private void OnBlockPlaced(BlockModel block)
        {
            int combo = _grid != null ? _grid.Combo : 1;
            AudioManager.PlayPitched(SoundId.BlockPlace, FeelCurves.ComboPitch(combo, comboPitchStep, comboPitchMax));
            AudioManager.PlayChance(SoundId.CatHappy, catHappyChance);
        }

        private void OnBlockRemoved(BlockModel block) => AudioManager.Play(SoundId.BlockRemove);

        private void OnMistake(Vector3 center, Vector2 size)
        {
            AudioManager.Play(SoundId.Mistake);
            AudioManager.PlayChance(SoundId.CatSad, catSadChance);
        }

        private void OnClueUnlocked(CellModel cell) => AudioManager.Play(SoundId.Unlock);

        // Vừa đủ điều kiện -> ding (cả khi chạm vào ô số 1); số ô đổi -> tick cao / thấp theo số ô (không tick lúc bắt đầu kéo)
        private void OnSelectionChanged(int count, SelectionHint hint)
        {
            int prevCount = _dragCount;
            SelectionHint prevHint = _dragHint;
            _dragCount = count;
            _dragHint = hint;
            if (!dragSounds || count == 0) return;

            if (hint == SelectionHint.Ready && prevHint != SelectionHint.Ready)
                AudioManager.Play(SoundId.DragReady);
            else if (prevCount > 0 && count != prevCount)
                AudioManager.PlayPitched(SoundId.DragTick, FeelCurves.DragPitch(count, dragBasePitch, dragSemitones));
        }

        // Thả tay ở vùng sai luật (không phải chạm 1 ô trống)
        private void OnInvalidMove(PlacementResult result, Vector3 center, Vector2 size) => AudioManager.Play(SoundId.DragCancel);

        private void OnTimeAdded(float seconds) => RefreshMusicPitch();
        private void OnTimeChanged(float remaining, float total) => RefreshMusicPitch(); // cùng đích thì SetMusicPitch bỏ qua

        /// <summary>Nhạc nhanh lên khi đang chơi mà sắp hết giờ (không đóng băng); còn lại về bình thường.</summary>
        private void RefreshMusicPitch()
        {
            if (AudioManager.Instance == null) return;
            bool playing = _levels == null || _levels.State == GameState.Playing;
            bool hurry = playing && _timer != null && _timer.IsLow && !_timer.IsFrozen && !_timer.IsTimeUp;
            AudioManager.Instance.SetMusicPitch(hurry ? lowTimeMusicPitch : 1f);
        }

        // Mất sao lúc hết giờ (còn 0 sao) đã có âm hết giờ: không phát chồng
        private void OnStarsChanged(StarsChangedEvent e)
        {
            if (e.Lost && e.Stars > 0) AudioManager.Play(SoundId.StarLost);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            RefreshMusicPitch();
            switch (e.State)
            {
                case GameState.Won: AudioManager.Play(SoundId.Win); break;
                case GameState.OutOfTime: AudioManager.Play(SoundId.OutOfTime); break;
                case GameState.Lost: AudioManager.Play(SoundId.Lose); break;
            }
        }

        // Lưới nạp lúc đang ở Menu (mở game) thì không kêu: chưa nhìn thấy
        private void OnGridSpawned(List<CellView> cells)
        {
            RefreshMusicPitch();
            if (_screens != null && _screens.IsMenu) return;
            AudioManager.Play(SoundId.LevelStart);
        }

        private void OnFreezeChanged(bool on)
        {
            if (on) AudioManager.Play(SoundId.FreezeOn);
            RefreshMusicPitch();
        }

        private void OnFreezeEnded() => AudioManager.Play(SoundId.FreezeEnd);

        private void OnTimeWarning()
        {
            AudioManager.Play(SoundId.TimeWarning);
            RefreshMusicPitch();
        }

        private void OnBoosterUsed(BoosterType type) => AudioManager.Play(SoundId.Booster);

        private void OnCoinChanged(CoinsChangedEvent e)
        {
            if (e.Delta > 0) AudioManager.Play(SoundId.Coin);
        }
    }
}
