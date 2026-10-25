using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Sao của ván đang chơi, tính theo LevelTimer (StarRating / StarTracker): lưới mới = 3 sao,
    /// thời gian còn lại tụt qua mốc threeStarRatio / twoStarRatio / 0 (LivesEnergyConfig) thì mất 1 sao.
    /// Phát StarsChangedEvent; thanh thời gian HUD (TimeStarBar) và màn thắng (WinPopup) đọc từ đây.
    /// Thắng (không tính màn luyện tập) thì cộng số sao vào ví sao (StarWallet) để quay Vòng quay may mắn.
    /// Đặt cùng LevelTimer.
    /// </summary>
    public class LevelStars : MonoBehaviour
    {
        private LevelTimer _timer;
        private GridSpawner _gridSpawner;
        private LivesEnergyConfig _lives;
        private LevelManager _levels;
        private ICurrencyWallet _stars;

        /// <summary>GameBootstrap gọi: ví sao (Economy, qua contract) – thắng thì cộng số sao của ván.</summary>
        public void Init(ICurrencyWallet stars, LevelManager levels, LivesEnergyConfig lives, LevelTimer timer, GridSpawner gridSpawner)
        {
            _stars = stars;
            _levels = levels;
            _lives = lives;
            _timer = timer;
            _gridSpawner = gridSpawner;
        }

        private readonly StarTracker _tracker = new StarTracker();

        public int Stars => _tracker.Stars;

        private void OnEnable()
        {
            if (_timer != null) _timer.onTimeChanged.AddListener(OnTimeChanged);
            if (_gridSpawner != null) _gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void OnDisable()
        {
            if (_timer != null) _timer.onTimeChanged.RemoveListener(OnTimeChanged);
            if (_gridSpawner != null) _gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State != GameState.Won) return;
            if (_levels != null && _levels.IsPractice) return;
            _stars?.Add(Stars);
        }

        private void OnGridSpawned(List<CellView> cells)
        {
            _tracker.Reset();
            EventBus<StarsChangedEvent>.Raise(new StarsChangedEvent(Stars, false));
        }

        private void OnTimeChanged(float remaining, float total)
        {
            LivesEnergyConfig cfg = _lives;
            if (!_tracker.Update(remaining, total, cfg.twoStarRatio, cfg.threeStarRatio)) return;
            EventBus<StarsChangedEvent>.Raise(new StarsChangedEvent(Stars, true));
        }
    }
}
