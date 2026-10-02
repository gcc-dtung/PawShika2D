using UnityEngine;

/// <summary>
/// Nối trạng thái ván (LevelManager.onStateChanged) với các popup.
/// Có component này trong scene thì LevelManager không tự chuyển level / chơi lại nữa.
/// </summary>
public class GameFlowUI : MonoBehaviour
{
    [SerializeField] private WinPopup winPopup;
    [SerializeField] private ReviveLivesPopup revivePopup;
    [SerializeField] private LosePopup losePopup;
    [SerializeField] private ConfirmReplayPopup confirmReplayPopup;

    private void Start()
    {
        CloseAll();
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.onStateChanged.AddListener(OnStateChanged);
        LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.onStateChanged.RemoveListener(OnStateChanged);
        LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
    }

    /// <summary>Nút chơi lại trên HUD.</summary>
    public void RequestReplay()
    {
        LevelManager lm = LevelManager.Instance;
        if (lm == null || !lm.IsPlaying) return;
        if (confirmReplayPopup != null) confirmReplayPopup.Open();
        else lm.Retry();
    }

    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Won: Open(winPopup); break;
            case GameState.OutOfLives: Open(revivePopup); break;
            case GameState.Lost: Open(losePopup); break;
        }
    }

    // Đổi level (kể cả bằng menu debug) thì đóng popup còn mở; popup đang chạy hiệu ứng đóng thì để chạy nốt
    private void OnLevelLoaded(int _)
    {
        CloseIfOpen(winPopup);
        CloseIfOpen(revivePopup);
        CloseIfOpen(losePopup);
        CloseIfOpen(confirmReplayPopup);
    }

    private static void CloseIfOpen(PopupView popup)
    {
        if (popup != null && popup.IsOpen) popup.CloseImmediate();
    }

    private static void Open(PopupView popup)
    {
        if (popup != null) popup.Open();
    }

    private void CloseAll()
    {
        if (winPopup != null) winPopup.CloseImmediate();
        if (revivePopup != null) revivePopup.CloseImmediate();
        if (losePopup != null) losePopup.CloseImmediate();
        if (confirmReplayPopup != null) confirmReplayPopup.CloseImmediate();
    }
}
