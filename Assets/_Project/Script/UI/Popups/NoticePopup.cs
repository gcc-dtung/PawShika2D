using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thông báo chung ("NOTICE" như bản gốc): tiêu đề, nội dung trong khung trắng, nút Continue và X (cả hai chỉ đóng).
/// Show(message, title, onClose). Mở trong ván thì ván tạm dừng, đóng thì chơi tiếp.
/// </summary>
public class NoticePopup : PopupView
{
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
    protected override bool CloseOnLevelLoad => false;

    protected override void OnOpen()
    {
        LevelManager lm = LevelManager.Instance;
        _pausedGame = lm != null && lm.IsPlaying && (ScreenFlow.Instance == null || !ScreenFlow.Instance.IsMenu);
        if (_pausedGame) lm.Pause();
    }

    private void OnClose()
    {
        Close();
        if (_pausedGame && LevelManager.Instance != null) LevelManager.Instance.Resume();
        _pausedGame = false;
        Action cb = _onClose;
        _onClose = null;
        cb?.Invoke();
    }
}
