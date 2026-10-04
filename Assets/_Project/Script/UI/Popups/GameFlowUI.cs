using System;
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
    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private QuitLevelPopup quitLevelPopup;
    [SerializeField] private NoticePopup noticePopup;
    [SerializeField] private DailyRewardPopup dailyRewardPopup;
    [SerializeField] private BuyBoosterPopup buyBoosterPopup;
    [SerializeField] private RefillEnergyPopup refillEnergyPopup;

    private void Start()
    {
        CloseAll();
        if (losePopup != null) losePopup.RetryHandler = RetryAfterLose;
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.AddListener(OnBuyBooster);
        IAPService.PurchaseFailed += OnPurchaseFailed;
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.onStateChanged.AddListener(OnStateChanged);
        LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
    }

    private void OnDestroy()
    {
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.RemoveListener(OnBuyBooster);
        IAPService.PurchaseFailed -= OnPurchaseFailed;
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.onStateChanged.RemoveListener(OnStateChanged);
        LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
    }

    /// <summary>Nút cài đặt trên HUD / trên Menu. Trong ván chỉ mở khi đang chơi (không chồng lên popup khác).</summary>
    public void OpenSettings()
    {
        if (settingsPopup == null || settingsPopup.IsOpen) return;
        bool inMenu = ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
        if (!inMenu && LevelManager.Instance != null && !LevelManager.Instance.IsPlaying) return;
        settingsPopup.Open();
    }

    /// <summary>Nút Daily trên Menu (và tự mở khi vào game lần đầu trong ngày).</summary>
    public void OpenDailyReward()
    {
        if (dailyRewardPopup == null || dailyRewardPopup.IsOpen) return;
        dailyRewardPopup.Open();
    }

    /// <summary>Thông báo chung (popup NOTICE).</summary>
    public void ShowNotice(string message, string title = "NOTICE", Action onClose = null)
    {
        if (noticePopup != null) noticePopup.Show(message, title, onClose);
        else Debug.Log($"[GameFlowUI] {title}: {message}");
    }

    private void OnPurchaseFailed(string productId, string reason) => ShowNotice(reason);

    /// <summary>Nút năng lượng trên Menu.</summary>
    public void OpenEnergyRefill()
    {
        if (refillEnergyPopup == null || refillEnergyPopup.IsOpen) return;
        bool inMenu = ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
        if (!inMenu && LevelManager.Instance != null && !LevelManager.Instance.IsPlaying) return; // đang có popup khác
        refillEnergyPopup.ShowOptional();
    }

    /// <summary>
    /// Nút PLAY trên Menu: còn năng lượng thì <paramref name="start"/> ngay; hết thì mở popup nạp (có X để thôi),
    /// có năng lượng thì tự vào chơi.
    /// </summary>
    public void RequestPlay(Action start)
    {
        if (EnergyManager.Instance == null || EnergyManager.Instance.CanPlay) start?.Invoke();
        else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(start, () => { });
        else start?.Invoke();
    }

    /// <summary>Thua rồi bấm Retry: còn năng lượng thì chơi lại, hết thì chờ / nạp rồi mới chơi lại.</summary>
    private void RetryAfterLose()
    {
        LevelManager lm = LevelManager.Instance;
        if (lm == null) return;
        if (EnergyManager.Instance == null || EnergyManager.Instance.CanPlay) lm.Retry();
        else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(lm.Retry, () => Open(losePopup)); // X -> về lại popup Thua
        else lm.Retry();
    }

    private void OnBuyBooster(BoosterType type)
    {
        if (buyBoosterPopup != null) buyBoosterPopup.Show(type);
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
        CloseIfOpen(settingsPopup);
        CloseIfOpen(quitLevelPopup);
        CloseIfOpen(dailyRewardPopup);
        CloseIfOpen(buyBoosterPopup);
        // Popup chờ năng lượng (bắt buộc) giữ nguyên qua lần nạp level
        if (refillEnergyPopup != null && !refillEnergyPopup.IsRequired) CloseIfOpen(refillEnergyPopup);
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
        if (settingsPopup != null) settingsPopup.CloseImmediate();
        if (quitLevelPopup != null) quitLevelPopup.CloseImmediate();
        if (noticePopup != null) noticePopup.CloseImmediate();
        if (dailyRewardPopup != null) dailyRewardPopup.CloseImmediate();
        if (buyBoosterPopup != null) buyBoosterPopup.CloseImmediate();
        if (refillEnergyPopup != null) refillEnergyPopup.CloseImmediate();
    }
}
