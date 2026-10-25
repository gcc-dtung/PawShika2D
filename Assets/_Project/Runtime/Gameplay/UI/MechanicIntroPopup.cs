using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Popup giới thiệu cơ chế (như popup "Orientation" bản gốc): khung popup_tutorial, tiêu đề, 1 dòng giải thích,
    /// ảnh minh họa (popup_tutorial_2/3/4), nút Ok. Mở trong ván thì ván tạm dừng, Ok thì chơi tiếp.
    /// </summary>
    public class MechanicIntroPopup : PopupView
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image picture;
        [SerializeField] private Button okButton;

        private Action _onClose;
        private bool _pausedGame;

        protected override void Awake()
        {
            base.Awake();
            if (okButton != null) okButton.onClick.AddListener(OnOk);
        }

        public void Show(string title, string message, Sprite sprite, Action onClose)
        {
            if (titleText != null) titleText.SetText(title);
            if (messageText != null) messageText.SetText(message);
            if (picture != null)
            {
                picture.sprite = sprite;
                picture.gameObject.SetActive(sprite != null);
            }
            _onClose = onClose;
            Open();
        }

        protected override void OnOpened()
        {
            LevelManager lm = _levels;
            _pausedGame = lm != null && lm.IsPlaying;
            if (_pausedGame) lm.Pause();
        }

        private void OnOk()
        {
            if (!IsOpen) return;
            Close();
            if (_pausedGame && _levels != null) _levels.Resume();
            _pausedGame = false;
            Action cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }
    }
}
