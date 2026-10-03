using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Thua level (đã từ chối hồi mạng): chơi lại từ đầu.</summary>
public class LosePopup : PopupView
{
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
        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : 0;
        if (titleText != null) titleText.SetText($"LEVEL {level}");
    }

    private void OnHome()
    {
        Close();
        if (ScreenFlow.Instance != null) ScreenFlow.Instance.ShowMenu();
    }

    private void OnRetry()
    {
        Close();
        if (RetryHandler != null) RetryHandler();
        else if (LevelManager.Instance != null) LevelManager.Instance.Retry();
    }
}
