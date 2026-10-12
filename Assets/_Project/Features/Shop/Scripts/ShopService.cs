using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Shop
{
    /// <summary>
    /// Mua 1 gói Shop: thanh toán qua IIAPService (chưa có SDK -> thất bại, popup "Purchase Failed!"),
    /// thành công thì trao quà: coin, booster, năng lượng vô hạn, tắt quảng cáo (section "shop").
    /// Chỉ biết contract (ví, kho booster, năng lượng); GameBootstrap tạo và truyền vào.
    /// </summary>
    public sealed class ShopService
    {
        private readonly ISaveService _save;
        private readonly ShopSave _data;
        private readonly ICurrencyWallet _coins;
        private readonly IBoosterInventory _boosters;
        private readonly IEnergyService _energy;
        private readonly IIAPService _iap;

        /// <summary>Gói vừa mua xong (UI cập nhật, vd ẩn dòng Remove Ads).</summary>
        public event Action<ShopProduct> Purchased;

        public ShopService(ISaveService save, ICurrencyWallet coins, IBoosterInventory boosters, IEnergyService energy, IIAPService iap)
        {
            _iap = iap;
            _save = save;
            _data = save.Get<ShopSave>(ShopSave.Key);
            _coins = coins;
            _boosters = boosters;
            _energy = energy;
        }

        public bool NoAds => _data.noAds;

        public void Buy(ShopProduct product, Action<bool> onDone = null)
        {
            if (product == null)
            {
                return;
            }
            _iap.Purchase(product.productId, ok =>
            {
                if (ok)
                {
                    Grant(product);
                }
                onDone?.Invoke(ok);
            });
        }

        /// <summary>Trao quà của gói (dùng khi thanh toán thành công; Debug cũng gọi được).</summary>
        public void Grant(ShopProduct product)
        {
            if (product.coin > 0)
            {
                _coins.Add(product.coin);
            }
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                _boosters?.Add(type, product.BoosterAmount(type));
            }
            if (product.infiniteMinutes > 0)
            {
                _energy?.AddInfinite(product.infiniteMinutes * 60);
            }
            if (product.removeAds && !_data.noAds)
            {
                _data.noAds = true;
                _save.MarkDirty();
            }
            Debug.Log($"[ShopService] Đã trao gói {product.title}");
            Purchased?.Invoke(product);
        }
    }
}
