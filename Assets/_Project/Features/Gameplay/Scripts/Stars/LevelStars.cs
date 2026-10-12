using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Sao của ván đang chơi, tính theo LevelTimer (StarRating / StarTracker): lưới mới = 3 sao,
    /// thời gian còn lại tụt qua mốc threeStarRatio / twoStarRatio / 0 (LivesEnergyConfig) thì mất 1 sao.
    /// Phát StarsChangedEvent; thanh thời gian HUD (TimeStarBar) và màn thắng (WinPopup) đọc từ đây.
    /// Thắng (không tính màn luyện tập) thì cộng số sao vào ví sao (StarWallet) để quay Vòng quay may mắn.
    /// Đặt cùng LevelTimer.
    /// </summary>
    public class LevelStars : Singleton<LevelStars>
    {
        [SerializeField] private LevelTimer timer;
        [SerializeField] private GridSpawner gridSpawner;

        private readonly StarTracker _tracker = new StarTracker();

        public int Stars => _tracker.Stars;

        protected override void Awake()
        {
            base.Awake();
            if (timer == null) timer = GetComponent<LevelTimer>();
            if (timer == null) timer = FindFirstObjectByType<LevelTimer>();
            if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        }

        private void OnEnable()
        {
            if (timer != null) timer.onTimeChanged.AddListener(OnTimeChanged);
            if (gridSpawner != null) gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void OnDisable()
        {
            if (timer != null) timer.onTimeChanged.RemoveListener(OnTimeChanged);
            if (gridSpawner != null) gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State != GameState.Won) return;
            if (LevelManager.Instance != null && LevelManager.Instance.IsPractice) return;
            StarWallet.Add(Stars);
        }

        private void OnGridSpawned(List<CellView> cells)
        {
            _tracker.Reset();
            EventBus<StarsChangedEvent>.Raise(new StarsChangedEvent(Stars, false));
        }

        private void OnTimeChanged(float remaining, float total)
        {
            LivesEnergyConfig cfg = GameConfig.LivesEnergy;
            if (!_tracker.Update(remaining, total, cfg.twoStarRatio, cfg.threeStarRatio)) return;
            EventBus<StarsChangedEvent>.Raise(new StarsChangedEvent(Stars, true));
        }
    }
}
