using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;

namespace PawBlock.Features.Shop
{
    /// <summary>
    /// Mua 1 gói Shop: thanh toán qua IAPService (chưa có SDK -> thất bại, popup "Purchase Failed!"),
    /// thành công thì trao quà: coin, booster, năng lượng vô hạn, tắt quảng cáo (SaveData.noAds).
    /// </summary>
    public static class ShopService
    {
        /// <summary>Gói vừa mua xong (UI cập nhật, vd ẩn dòng Remove Ads).</summary>
        public static event Action<ShopProduct> Purchased;

        public static bool NoAds => SaveSystem.Data.noAds;

        public static void Buy(ShopProduct product, Action<bool> onDone = null)
        {
            if (product == null) return;
            IAPService.Purchase(product.productId, ok =>
            {
                if (ok) Grant(product);
                onDone?.Invoke(ok);
            });
        }

        /// <summary>Trao quà của gói (dùng khi thanh toán thành công; Debug cũng gọi được).</summary>
        public static void Grant(ShopProduct product)
        {
            if (product.coin > 0) CoinWallet.Add(product.coin);
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType))) AddBooster(type, product.BoosterAmount(type));
            if (product.infiniteMinutes > 0 && EnergyManager.Instance != null) EnergyManager.Instance.AddInfinite(product.infiniteMinutes * 60);
            if (product.removeAds && !SaveSystem.Data.noAds)
            {
                SaveSystem.Data.noAds = true;
                SaveSystem.MarkDirty();
            }
            Debug.Log($"[ShopService] Đã trao gói {product.title}");
            Purchased?.Invoke(product);
        }

        private static void AddBooster(BoosterType type, int amount)
        {
            if (amount <= 0) return;
            BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(type) : null;
            if (data != null) BoosterInventory.Add(data, amount);
        }
    }
}
