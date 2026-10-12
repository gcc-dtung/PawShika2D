using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Features.Economy;
using PawBlock.Features.Shop;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Shop của Main Menu (ShopPanel.prefab): thẻ gói (ShopCardView), coin / năng lượng trên thanh trên.
    /// Ghép Shop + Economy nên nằm ở Game. GameBootstrap chỉ gọi Init của tab, tab nối tiếp cho view con.
    /// </summary>
    public class ShopPanel : MenuPanel
    {
        [Header("View con (Editor tự điền)")]
        [SerializeField] private ShopCardView[] cards;
        [SerializeField] private CoinHUD coins;
        [SerializeField] private EnergyHUD energy;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(ShopService shop, CoinWallet coinWallet, EnergyManager energyManager, IMenuPopups popups)
        {
            foreach (ShopCardView card in cards)
            {
                if (card != null) card.Init(shop);
            }
            if (coins != null) coins.Init(coinWallet);
            if (energy != null) energy.Init(energyManager, popups);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cards = GetComponentsInChildren<ShopCardView>(true);
            if (coins == null) coins = GetComponentInChildren<CoinHUD>(true);
            if (energy == null) energy = GetComponentInChildren<EnergyHUD>(true);
        }
#endif
    }
}
