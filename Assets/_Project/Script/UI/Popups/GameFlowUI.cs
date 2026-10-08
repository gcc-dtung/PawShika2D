using System;
using UnityEngine;

/// <summary>
/// Nối trạng thái ván (GameStateChangedEvent) với các popup.
/// Có component này trong scene thì LevelManager không tự chuyển level / chơi lại nữa (AutoAdvance = false).
/// Đổi level thì popup tự đóng (PopupView nghe LevelLoadedEvent), ở đây không cần liệt kê lại.
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

    private PopupView[] AllPopups => new PopupView[]
    {
        winPopup, revivePopup, losePopup, settingsPopup, quitLevelPopup,
        noticePopup, dailyRewardPopup, buyBoosterPopup, refillEnergyPopup
    };

    private void Awake()
    {
        EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        if (LevelManager.Instance != null) LevelManager.Instance.AutoAdvance = false; // popup quyết định sang level kế / chơi lại
    }

    private void Start()
    {
        foreach (PopupView popup in AllPopups)
        {
            if (popup != null) popup.CloseImmediate();
        }
        if (losePopup != null) losePopup.RetryHandler = RetryAfterLose;
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.AddListener(OnBuyBooster);
        IAPService.PurchaseFailed += OnPurchaseFailed;
    }

    private void OnDestroy()
    {
        EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        if (LevelManager.Instance != null) LevelManager.Instance.AutoAdvance = true;
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.RemoveListener(OnBuyBooster);
        IAPService.PurchaseFailed -= OnPurchaseFailed;
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

    private void OnStateChanged(GameStateChangedEvent e)
    {
        switch (e.State)
        {
            case GameState.Won: Open(winPopup); break;
            case GameState.OutOfLives: Open(revivePopup); break;
            case GameState.Lost: Open(losePopup); break;
        }
    }

    private static void Open(PopupView popup)
    {
        if (popup != null) popup.Open();
    }
}
