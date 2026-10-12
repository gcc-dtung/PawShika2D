using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Thua level (đã từ chối hồi mạng): chơi lại từ đầu.</summary>
    public class LosePopup : PopupView
    {
        private LevelManager _levels;
        private IScreenNavigator _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IScreenNavigator screens, LevelManager levels)
        {
            _screens = screens;
            _levels = levels;
        }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        /// <summary>GameFlowUI gắn vào để kiểm tra năng lượng trước khi chơi lại; null thì chơi lại luôn.</summary>
        public Action RetryHandler { get; set; }

        protected override void Awake()
        {
            base.Awake();
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (homeButton != null) homeButton.onClick.AddListener(OnHome);
        }

        protected override void OnOpen()
        {
            LevelManager lm = _levels;
            if (titleText == null || lm == null) return;
            if (lm.IsDaily) titleText.SetText(LevelHUD.DailyLabel(lm.DailyDay));
            else titleText.SetText($"LEVEL {lm.CurrentLevelNumber}");
        }

        private void OnHome()
        {
            Close();
            if (_screens != null) _screens.ShowMenu();
        }

        private void OnRetry()
        {
            Close();
            if (RetryHandler != null) RetryHandler();
            else if (_levels != null) _levels.Retry();
        }
    }
}
