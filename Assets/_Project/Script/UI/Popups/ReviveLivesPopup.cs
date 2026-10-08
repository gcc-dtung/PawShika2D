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

    protected override void Awake()
    {
        base.Awake();
        if (adsButton != null) adsButton.onClick.AddListener(OnAds);
        if (coinButton != null) coinButton.onClick.AddListener(OnCoin);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);
    }

    protected override void OnOpen()
    {
        if (coinCostText != null) coinCostText.SetText(GameConfig.Economy.reviveCoinCost.ToString());
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
            if (ok) Revive(GameConfig.Economy.reviveAdsLives);
            else Refresh();
        });
    }

    private void OnCoin()
    {
        if (!CoinWallet.TrySpend(GameConfig.Economy.reviveCoinCost))
        {
            // Chưa có Shop (Giai đoạn 6): chỉ báo thiếu coin
            Debug.Log($"[ReviveLivesPopup] Không đủ coin: {CoinWallet.Balance}/{GameConfig.Economy.reviveCoinCost}");
            Refresh();
            return;
        }
        Revive(GameConfig.Economy.reviveCoinLives);
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
        if (coinButton != null) coinButton.interactable = interactable && CoinWallet.CanAfford(GameConfig.Economy.reviveCoinCost);
        if (closeButton != null) closeButton.interactable = interactable;
    }
}
