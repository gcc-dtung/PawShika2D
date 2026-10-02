using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Thua level (đã từ chối hồi mạng): chơi lại từ đầu.</summary>
public class LosePopup : PopupView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button retryButton;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
    }

    protected override void OnOpen()
    {
        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : 0;
        if (titleText != null) titleText.SetText($"LEVEL {level}");
    }

    private void OnRetry()
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.Retry();
    }
}
