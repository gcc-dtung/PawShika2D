using TMPro;
using UnityEngine;
using PawBlock.Core.Contracts;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Game;
using PawBlock.Shared;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hết giờ (MoreTimePopup của bản gốc): xem quảng cáo +moreTimeAdsSeconds, trả coin +moreTimeCoinSeconds, đóng = thua.
    /// Thêm giờ thì chơi tiếp đúng lưới đang dở.
    /// </summary>
    public class MoreTimePopup : PopupView
    {
        private ICurrencyWallet _coins;

        /// <summary>GameBootstrap gọi: ví coin (Economy, qua contract).</summary>
        public void Init(ICurrencyWallet coins) => _coins = coins;

        [SerializeField] private Button adsButton;
        [SerializeField] private Button coinButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text coinCostText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text adsLabel;  // "+30s"
        [SerializeField] private TMP_Text coinLabel; // "+60s"

        protected override void Awake()
        {
            base.Awake();
            if (adsButton != null) adsButton.onClick.AddListener(OnAds);
            if (coinButton != null) coinButton.onClick.AddListener(OnCoin);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        protected override void OnOpen()
        {
            EconomyConfig eco = GameConfig.Economy;
            if (coinCostText != null) coinCostText.SetText(eco.moreTimeCoinCost.ToString());
            if (adsLabel != null) adsLabel.SetText("+{0}s", eco.moreTimeAdsSeconds);
            if (coinLabel != null) coinLabel.SetText("+{0}s", eco.moreTimeCoinSeconds);
            Refresh();
        }

        private void Refresh()
        {
            if (balanceText != null) balanceText.SetText(_coins.Balance.ToString());
            SetButtons(true);
        }

        private void OnAds()
        {
            SetButtons(false);
            RewardedAds.Show("more_time", ok =>
            {
                if (ok) Revive(GameConfig.Economy.moreTimeAdsSeconds);
                else Refresh();
            });
        }

        private void OnCoin()
        {
            if (!_coins.TrySpend(GameConfig.Economy.moreTimeCoinCost))
            {
                Debug.Log($"[MoreTimePopup] Không đủ coin: {_coins.Balance}/{GameConfig.Economy.moreTimeCoinCost}");
                Refresh();
                return;
            }
            Revive(GameConfig.Economy.moreTimeCoinSeconds);
        }

        private void OnClose()
        {
            Close();
            if (LevelManager.Instance != null) LevelManager.Instance.GiveUp();
        }

        private void Revive(int seconds)
        {
            Close();
            if (LevelManager.Instance != null) LevelManager.Instance.Revive(seconds);
        }

        private void SetButtons(bool interactable)
        {
            if (adsButton != null) adsButton.interactable = interactable;
            if (coinButton != null) coinButton.interactable = interactable && _coins.Balance >= GameConfig.Economy.moreTimeCoinCost;
            if (closeButton != null) closeButton.interactable = interactable;
        }
    }
}
