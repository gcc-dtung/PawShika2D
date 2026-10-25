using TMPro;
using UnityEngine;
using PawBlock.Core.Contracts;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Shared;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hết giờ (MoreTimePopup của bản gốc): xem quảng cáo +moreTimeAdsSeconds, trả coin +moreTimeCoinSeconds, đóng = thua.
    /// Thêm giờ thì chơi tiếp đúng lưới đang dở.
    /// </summary>
    public class MoreTimePopup : PopupView
    {
        private EconomyConfig _economy;
        private LevelManager _levels;
        private ICurrencyWallet _coins;
        private IAdsService _ads;

        /// <summary>GameBootstrap gọi: ví coin (Economy, qua contract), quảng cáo có thưởng.</summary>
        public void Init(ICurrencyWallet coins, IAdsService ads, LevelManager levels, EconomyConfig economy)
        {
            _coins = coins;
            _ads = ads;
            _levels = levels;
            _economy = economy;
        }

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

        protected override void OnOpened()
        {
            EconomyConfig eco = _economy;
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
            _ads.Show("more_time", ok =>
            {
                if (ok) Revive(_economy.moreTimeAdsSeconds);
                else Refresh();
            });
        }

        private void OnCoin()
        {
            if (!_coins.TrySpend(_economy.moreTimeCoinCost))
            {
                Debug.Log($"[MoreTimePopup] Không đủ coin: {_coins.Balance}/{_economy.moreTimeCoinCost}");
                Refresh();
                return;
            }
            Revive(_economy.moreTimeCoinSeconds);
        }

        private void OnClose()
        {
            Close();
            if (_levels != null) _levels.GiveUp();
        }

        private void Revive(int seconds)
        {
            Close();
            if (_levels != null) _levels.Revive(seconds);
        }

        private void SetButtons(bool interactable)
        {
            if (adsButton != null) adsButton.interactable = interactable;
            if (coinButton != null) coinButton.interactable = interactable && _coins.Balance >= _economy.moreTimeCoinCost;
            if (closeButton != null) closeButton.interactable = interactable;
        }
    }
}
