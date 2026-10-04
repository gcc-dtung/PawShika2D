using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "QUIT LEVEL": đang chơi mà về Home (Setting → Home) thì hỏi lại – bỏ dở level mất 1 năng lượng (như thua).
/// Quit: trừ 1 năng lượng (vô hạn thì không trừ) rồi về Main Menu. X: chơi tiếp ván đang dở.
/// Mở thì ván tạm dừng (nếu đang chơi).
/// </summary>
public class QuitLevelPopup : PopupView
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (closeButton != null) closeButton.onClick.AddListener(OnCancel);
    }

    protected override void OnOpen()
    {
        LevelManager lm = LevelManager.Instance;
        if (lm != null && lm.IsPlaying)
        {
            if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
            lm.Pause();
        }
    }

    private void OnQuit()
    {
        Close();
        if (EnergyManager.Instance != null) EnergyManager.Instance.ConsumeOne("Bỏ dở level");
        if (ScreenFlow.Instance != null) ScreenFlow.Instance.ShowMenu();
    }

    private void OnCancel()
    {
        Close();
        // Ván đang tạm dừng (bởi popup này hoặc Setting trước đó) -> chơi tiếp
        LevelManager lm = LevelManager.Instance;
        if (lm != null && lm.State == GameState.Paused) lm.Resume();
    }
}
