using UnityEngine;
using UnityEngine.UI;

/// <summary>Nút chơi lại trên HUD: hỏi lại vì các khối đã đặt sẽ mất. Mở popup thì ván tạm dừng.</summary>
public class ConfirmReplayPopup : PopupView
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    protected override void Awake()
    {
        base.Awake();
        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
    }

    protected override void OnOpen()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.Pause();
    }

    private void OnYes()
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.Retry();
    }

    private void OnNo()
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.Resume();
    }
}
