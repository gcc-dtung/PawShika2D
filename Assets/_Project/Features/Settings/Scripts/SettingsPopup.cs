using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Settings
{
    /// <summary>
    /// Popup cài đặt: Nhạc / Âm thanh / Rung (SettingToggleView), nút X, nút Home, chữ phiên bản (Application.version).
    /// Mở trong ván thì ván tạm dừng, đóng thì chơi tiếp. Mở ở Main Menu thì ẩn nút Home và thu ngắn đáy thân popup
    /// (con của Panel neo mép trên, chữ phiên bản neo mép dưới nên đi theo đáy).
    /// </summary>
    public class SettingsPopup : PopupView
    {
        private IGamePause _pause;
        private IMenuPopups _popups;
        private IScreenNavigator _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IScreenNavigator screens, IGamePause pause, IMenuPopups popups)
        {
            _popups = popups;
            _screens = screens;
            _pause = pause;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private TMP_Text versionText;
        [Tooltip("Ở Main Menu (không có nút Home): thân popup ngắn đi bao nhiêu, mép trên giữ nguyên")]
        [SerializeField] private float menuHeightCut = 180f;

        private bool _pausedGame; // popup này đã tạm dừng ván -> đóng thì chơi tiếp
        private Vector2 _panelSize;
        private Vector2 _panelPos;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
            if (homeButton != null) homeButton.onClick.AddListener(OnHome);
            if (versionText != null) versionText.SetText("Ver " + Application.version);
            if (panel != null)
            {
                _panelSize = panel.sizeDelta;
                _panelPos = panel.anchoredPosition;
            }
        }

        protected override void OnOpen()
        {
            bool inMenu = _screens != null && _screens.IsMenu;
            if (homeButton != null) homeButton.gameObject.SetActive(!inMenu);
            if (panel != null)
            {
                float cut = inMenu ? menuHeightCut : 0f;
                panel.sizeDelta = _panelSize - new Vector2(0f, cut);
                panel.anchoredPosition = _panelPos + new Vector2(0f, cut * 0.5f);
            }

            IGamePause lm = _pause;
            _pausedGame = !inMenu && lm != null && lm.IsPlaying;
            if (_pausedGame)
            {
                lm.CancelDrag();
                lm.Pause();
            }
        }

        private void OnClose()
        {
            Close();
            if (_pausedGame && _pause != null) _pause.Resume();
            _pausedGame = false;
        }

        private void OnHome()
        {
            Close();
            _pausedGame = false;
            // Đang trong ván: cảnh báo mất năng lượng; ván vẫn tạm dừng, popup Quit lo chơi tiếp / về Home
            bool inGame = _screens != null && !_screens.IsMenu;
            if (inGame && _popups != null && _popups.OpenQuitLevel()) return;
            if (_screens != null) _screens.ShowMenu();
        }
    }
}
