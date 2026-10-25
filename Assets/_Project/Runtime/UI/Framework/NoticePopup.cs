using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Shared
{
    /// <summary>
    /// Thông báo chung ("NOTICE" như bản gốc): tiêu đề, nội dung trong khung trắng, nút Continue và X (cả hai chỉ đóng).
    /// Show(message, title, onClose). Mở trong ván thì ván tạm dừng, đóng thì chơi tiếp.
    /// </summary>
    public class NoticePopup : PopupView
    {
        private IGamePause _pause;
        private IScreenNavigator _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IScreenNavigator screens, IGamePause pause)
        {
            _screens = screens;
            _pause = pause;
        }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button closeButton;

        private Action _onClose;
        private bool _pausedGame;

        protected override void Awake()
        {
            base.Awake();
            if (continueButton != null) continueButton.onClick.AddListener(OnClose);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        public void Show(string message, string title = "NOTICE", Action onClose = null)
        {
            if (titleText != null) titleText.SetText(title);
            if (messageText != null) messageText.SetText(message);
            _onClose = onClose;
            Open();
        }

        // Thông báo (vd mua thất bại) giữ qua lần nạp level
        public override bool CloseOnLevelLoad => false;

        protected override void OnOpened()
        {
            IGamePause lm = _pause;
            _pausedGame = lm != null && lm.IsPlaying && (_screens == null || !_screens.IsMenu);
            if (_pausedGame) lm.Pause();
        }

        private void OnClose()
        {
            Close();
            if (_pausedGame && _pause != null) _pause.Resume();
            _pausedGame = false;
            Action cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }
    }
}
