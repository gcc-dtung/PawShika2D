using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nạp năng lượng ("REFILL LIVES?" như bản gốc): xem quảng cáo +1 (Free live), trả coin nạp đầy (Refill), hoặc chờ đồng hồ.
/// Thiếu coin mà bấm Refill: đóng popup (thôi chơi) rồi sang tab Shop.
/// Tim to hiện số năng lượng hiện có, ô đồng hồ đếm tới lần hồi kế (đầy thì "FULL").
/// - ShowOptional: mở từ HUD, có nút X; ván tạm dừng khi mở.
/// - ShowRequired(onReady, onCancel): hết năng lượng mà cần chơi (PLAY, Retry sau khi thua) -> có năng lượng
///   (ads / coin / hết giờ chờ) thì tự đóng rồi gọi onReady. Có onCancel thì hiện X (bấm X -> onCancel), không có thì ẩn X.
/// </summary>
public class RefillEnergyPopup : PopupView
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text coinCostText;
    [SerializeField] private TMP_Text balanceText; // tùy chọn (bản theo ref không hiện số dư)
    [SerializeField] private Button adsButton;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button closeButton;

    private Action _onReady;
    private Action _onCancel;
    private bool _required;
    private bool _pausedByMe;
    private bool _waitingAd;

    public bool IsRequired => IsOpen && _required;

    // Popup chờ năng lượng (bắt buộc) giữ nguyên qua lần nạp level
    protected override bool CloseOnLevelLoad => !_required;

    protected override void Awake()
    {
        base.Awake();
        if (adsButton != null) adsButton.onClick.AddListener(OnAds);
        if (coinButton != null) coinButton.onClick.AddListener(OnCoin);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);
    }

    public void ShowOptional()
    {
        _required = false;
        _onReady = null;
        _onCancel = null;
        Open();
    }

    public void ShowRequired(Action onReady, Action onCancel = null)
    {
        _required = true;
        _onReady = onReady;
        _onCancel = onCancel;
        Open();
    }

    protected override void OnOpen()
    {
        LevelManager lm = LevelManager.Instance;
        _pausedByMe = lm != null && lm.IsPlaying;
        if (_pausedByMe) lm.Pause();
        EventBus<GameStateChangedEvent>.Unsubscribe(OnGameState); // Open lại khi đang mở: không đăng ký trùng
        EventBus<GameStateChangedEvent>.Subscribe(OnGameState);

        if (closeButton != null) closeButton.gameObject.SetActive(!_required || _onCancel != null);
        if (coinCostText != null) coinCostText.SetText("{0}", GameConfig.Economy.refillCoinCost);

        EnergyManager em = EnergyManager.Instance;
        if (em != null)
        {
            em.onEnergyChanged.RemoveListener(OnEnergyChanged); // Open lại khi đang mở: không đăng ký trùng
            em.onTick.RemoveListener(Refresh);
            em.onEnergyChanged.AddListener(OnEnergyChanged);
            em.onTick.AddListener(Refresh);
        }
        Refresh();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventBus<GameStateChangedEvent>.Unsubscribe(OnGameState);
        EnergyManager em = EnergyManager.Instance;
        if (em == null) return;
        em.onEnergyChanged.RemoveListener(OnEnergyChanged);
        em.onTick.RemoveListener(Refresh);
    }

    // Mở lúc vào game: LevelManager nạp level xong đặt lại Playing -> tạm dừng lại
    private void OnGameState(GameStateChangedEvent e)
    {
        if (!IsOpen || e.State != GameState.Playing || LevelManager.Instance == null) return;
        _pausedByMe = true;
        LevelManager.Instance.Pause();
    }

    private void Refresh()
    {
        if (!IsOpen) return;
        EnergyManager em = EnergyManager.Instance;
        if (em == null) return;

        if (countText != null) countText.SetText("{0}", em.Current);
        if (timerText != null) timerText.SetText(em.IsFull ? "FULL" : EnergyManager.FormatTime(em.SecondsToNext));
        if (balanceText != null) balanceText.SetText("{0}", CoinWallet.Balance);
        if (adsButton != null) adsButton.interactable = !_waitingAd && !em.IsFull;
        if (coinButton != null) coinButton.interactable = !_waitingAd && !em.IsFull; // thiếu coin vẫn bấm được -> Shop
    }

    private void OnEnergyChanged(int current, int max)
    {
        if (!IsOpen) return;
        // Chờ năng lượng để chơi tiếp: có rồi thì đóng. Mở từ HUD: đầy rồi thì đóng.
        EnergyManager em = EnergyManager.Instance;
        bool canPlay = em != null ? em.CanPlay : current > 0; // tính cả năng lượng vô hạn
        if (_required ? canPlay : current >= max) Finish();
        else Refresh();
    }

    private void OnAds()
    {
        _waitingAd = true;
        Refresh();
        RewardedAds.Show("refill_energy", ok =>
        {
            _waitingAd = false;
            if (ok && EnergyManager.Instance != null) EnergyManager.Instance.Add(GameConfig.Economy.refillAdsEnergy);
            Refresh();
        });
    }

    private void OnCoin()
    {
        EnergyManager em = EnergyManager.Instance;
        if (em == null || em.IsFull) return;
        if (!CoinWallet.TrySpend(GameConfig.Economy.refillCoinCost))
        {
            Debug.Log($"[RefillEnergyPopup] Không đủ coin: {CoinWallet.Balance}/{GameConfig.Economy.refillCoinCost} -> Shop");
            GoShop();
            return;
        }
        em.RefillFull();
    }

    private void OnClose()
    {
        if (_required && _onCancel == null) return;
        Action cancel = _onCancel;
        _onReady = null; // đóng = thôi, không chơi
        Finish();
        cancel?.Invoke();
    }

    /// <summary>Đóng popup như bấm X nhưng không gọi onCancel (không mở lại popup Thua...), rồi sang Shop.</summary>
    private void GoShop()
    {
        _onReady = null;
        Finish();
        if (ScreenFlow.Instance != null) ScreenFlow.Instance.ShowShop();
    }

    private void Finish()
    {
        Action ready = _onReady;
        _onReady = null;
        _onCancel = null;
        Close();
        if (_pausedByMe && LevelManager.Instance != null) LevelManager.Instance.Resume();
        _pausedByMe = false;
        ready?.Invoke();
    }
}
