using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD trên: chữ "LEVEL n" và nút chơi lại.</summary>
public class LevelHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button replayButton;
    [SerializeField] private GameFlowUI gameFlow; // có thì hỏi xác nhận trước khi chơi lại

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
        if (gameFlow != null) gameFlow.RequestReplay();
        else if (LevelManager.Instance != null) LevelManager.Instance.Retry();
    }
}
