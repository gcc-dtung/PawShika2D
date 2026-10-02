using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hết mạng (thay MoreTimePopup của bản gốc): xem quảng cáo +1 mạng, trả coin +3 mạng, đóng = thua.
/// Hồi mạng thì chơi tiếp đúng lưới đang dở.
/// </summary>
public class ReviveLivesPopup : PopupView
{
    [SerializeField] private Button adsButton;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text coinCostText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private int adsLives = 1;
    [SerializeField] private int coinLives = 3;
    [SerializeField] private int coinCost = 900;

    protected override void Awake()
    {
        base.Awake();
        if (adsButton != null) adsButton.onClick.AddListener(OnAds);
        if (coinButton != null) coinButton.onClick.AddListener(OnCoin);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);
    }

    protected override void OnOpen()
    {
        if (coinCostText != null) coinCostText.SetText(coinCost.ToString());
        Refresh();
    }

    private void Refresh()
    {
        if (balanceText != null) balanceText.SetText(CoinWallet.Balance.ToString());
        SetButtons(true);
    }

    private void OnAds()
    {
        SetButtons(false);
        RewardedAds.Show("revive_lives", ok =>
        {
            if (ok) Revive(adsLives);
            else Refresh();
        });
    }

    private void OnCoin()
    {
        if (!CoinWallet.TrySpend(coinCost))
        {
            // Chưa có Shop (Giai đoạn 6): chỉ báo thiếu coin
            Debug.Log($"[ReviveLivesPopup] Không đủ coin: {CoinWallet.Balance}/{coinCost}");
            Refresh();
            return;
        }
        Revive(coinLives);
    }

    private void OnClose()
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.GiveUp();
    }

    private void Revive(int lives)
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.Revive(lives);
    }

    private void SetButtons(bool interactable)
    {
        if (adsButton != null) adsButton.interactable = interactable;
        if (coinButton != null) coinButton.interactable = interactable && CoinWallet.CanAfford(coinCost);
        if (closeButton != null) closeButton.interactable = interactable;
    }
}
