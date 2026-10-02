using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD trên: chữ "LEVEL n" và nút chơi lại.</summary>
public class LevelHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button replayButton;

    private void Start()
    {
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.onLevelLoaded.AddListener(SetLevel);
            SetLevel(LevelManager.Instance.CurrentLevelNumber);
        }
    }

    private void OnDestroy()
    {
        if (replayButton != null) replayButton.onClick.RemoveListener(OnReplayClicked);
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(SetLevel);
    }

    private void SetLevel(int number)
    {
        if (levelText != null) levelText.SetText($"LEVEL {number}");
    }

    private void OnReplayClicked()
    {
        // Popup xác nhận chơi lại sẽ thêm ở giai đoạn 3
        if (LevelManager.Instance != null) LevelManager.Instance.Retry();
    }
}
