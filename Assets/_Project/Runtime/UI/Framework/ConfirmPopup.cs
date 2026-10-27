using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Hỏi lại trước thao tác khó hoàn tác (vd "Leave Team?" như bản gốc): tiêu đề, câu hỏi, nút Có (xanh) / Không (đỏ) và X.
    /// Show(message, title, onYes): chỉ nút Có gọi onYes; Không / X chỉ đóng.
    /// </summary>
    public class ConfirmPopup : PopupView
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private TMP_Text yesLabel;
        [SerializeField] private Button noButton;
        [SerializeField] private TMP_Text noLabel;
        [SerializeField] private Button closeButton;

        private Action _onYes;

        protected override void Awake()
        {
            base.Awake();
            if (yesButton != null) yesButton.onClick.AddListener(OnYes);
            if (noButton != null) noButton.onClick.AddListener(OnNo);
            if (closeButton != null) closeButton.onClick.AddListener(OnNo);
        }

        public void Show(string message, string title, Action onYes, string yes = "Yes", string no = "No")
        {
            if (titleText != null) titleText.SetText(title);
            if (messageText != null) messageText.SetText(message);
            if (yesLabel != null) yesLabel.SetText(yes);
            if (noLabel != null) noLabel.SetText(no);
            _onYes = onYes;
            Open();
        }

        private void OnYes()
        {
            Action cb = _onYes;
            _onYes = null;
            Close();
            cb?.Invoke();
        }

        private void OnNo()
        {
            _onYes = null;
            Close();
        }
    }
}
