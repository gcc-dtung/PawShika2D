using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Features.Gameplay;
using PawBlock.Game;
using PawBlock.Shared;

namespace PawBlock.Features.Settings
{
    /// <summary>
    /// Popup cài đặt: Nhạc / Âm thanh / Rung (SettingToggleView), nút X, nút Home, chữ phiên bản (Application.version).
    /// Mở trong ván thì ván tạm dừng, đóng thì chơi tiếp. Mở ở Main Menu thì ẩn nút Home và thu ngắn đáy thân popup
    /// (con của Panel neo mép trên, chữ phiên bản neo mép dưới nên đi theo đáy).
    /// </summary>
    public class SettingsPopup : PopupView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private TMP_Text versionText;
        [Tooltip("Đang chơi mà bấm Home: hỏi lại (bỏ dở level mất 1 năng lượng). Trống thì về Home luôn")]
        [SerializeField] private QuitLevelPopup quitPopup;
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
            bool inMenu = ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
            if (homeButton != null) homeButton.gameObject.SetActive(!inMenu);
            if (panel != null)
            {
                float cut = inMenu ? menuHeightCut : 0f;
                panel.sizeDelta = _panelSize - new Vector2(0f, cut);
                panel.anchoredPosition = _panelPos + new Vector2(0f, cut * 0.5f);
            }

            LevelManager lm = LevelManager.Instance;
            _pausedGame = !inMenu && lm != null && lm.IsPlaying;
            if (_pausedGame)
            {
                if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
                lm.Pause();
            }
        }

        private void OnClose()
        {
            Close();
            if (_pausedGame && LevelManager.Instance != null) LevelManager.Instance.Resume();
            _pausedGame = false;
        }

        private void OnHome()
        {
            Close();
            _pausedGame = false;
            // Đang trong ván: cảnh báo mất năng lượng; ván vẫn tạm dừng, popup Quit lo chơi tiếp / về Home
            bool inGame = ScreenFlow.Instance != null && !ScreenFlow.Instance.IsMenu;
            if (inGame && quitPopup != null)
            {
                quitPopup.Open();
                return;
            }
            if (ScreenFlow.Instance != null) ScreenFlow.Instance.ShowMenu();
        }
    }
}
