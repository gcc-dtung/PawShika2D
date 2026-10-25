using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Features.Economy;
using PawBlock.Features.Shop;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Shop của Main Menu (ShopPanel.prefab): spawn 1 thẻ (ShopCardView) cho mỗi gói trong ShopDatabase theo prefab của
    /// section (slots), coin / năng lượng trên thanh trên. Lần đầu mở Shop trong lần chạy thì ShopIntro cho thẻ bay vào.
    /// Ghép Shop + Economy nên nằm ở Game. GameBootstrap chỉ gọi Init của tab, tab nối tiếp cho view con.
    /// </summary>
    public class ShopPanel : MenuPanel
    {
        /// <summary>Thẻ của 1 section spawn vào đâu.</summary>
        [Serializable]
        private class SectionSlot
        {
            public ShopSection section;
            public ShopCardView prefab;
            public Transform parent;
            [Tooltip("Chèn ngay sau object này trong parent (vd tiêu đề mục); trống = đầu parent. "
                     + "Nhiều section chung 1 chỗ thì xếp theo thứ tự trong ShopDatabase")]
            public Transform after;
        }

        [SerializeField] private ShopDatabase database;
        [SerializeField] private SectionSlot[] slots;
        [SerializeField] private ShopIntro intro;

        [Header("View con (Editor tự điền)")]
        [SerializeField] private CoinHUD coins;
        [SerializeField] private EnergyHUD energy;

        private readonly List<ShopCardView> _cards = new List<ShopCardView>();

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(ShopService shop, CoinWallet coinWallet, EnergyManager energyManager, IMenuPopups popups)
        {
            SpawnCards(shop);
            if (coins != null) coins.Init(coinWallet);
            if (energy != null) energy.Init(energyManager, popups);
            if (intro != null) intro.Prepare(_cards);
        }

        protected override void OnShown()
        {
            if (intro != null) intro.Play();
        }

        private void SpawnCards(ShopService shop)
        {
            if (database == null) return;
            var last = new Dictionary<Transform, Transform>(); // chỗ chèn -> thẻ vừa chèn ở đó
            foreach (ShopProduct product in database.products)
            {
                SectionSlot slot = FindSlot(product.section);
                if (slot == null || slot.prefab == null || slot.parent == null)
                {
                    Debug.LogWarning($"[ShopPanel] Chưa gán prefab / parent cho section {product.section} ({product.productId})", this);
                    continue;
                }

                Transform key = slot.after != null ? slot.after : slot.parent;
                last.TryGetValue(key, out Transform prev);
                int index = prev != null ? prev.GetSiblingIndex() + 1
                    : slot.after != null ? slot.after.GetSiblingIndex() + 1 : 0;

                ShopCardView card = Instantiate(slot.prefab, slot.parent);
                card.name = product.title.Replace('\n', ' ');
                card.transform.SetSiblingIndex(index);
                card.Bind(product, shop);
                last[key] = card.transform;
                _cards.Add(card);
            }
        }

        private SectionSlot FindSlot(ShopSection section)
        {
            if (slots == null) return null;
            foreach (SectionSlot s in slots)
            {
                if (s.section == section) return s;
            }
            return null;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (intro == null) intro = GetComponent<ShopIntro>();
            if (coins == null) coins = GetComponentInChildren<CoinHUD>(true);
            if (energy == null) energy = GetComponentInChildren<EnergyHUD>(true);
        }
#endif
    }
}
