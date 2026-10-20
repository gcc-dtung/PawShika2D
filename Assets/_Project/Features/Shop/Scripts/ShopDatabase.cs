using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Shop
{
    public enum ShopSection
    {
        Starter,    // thẻ Starter Pack (tem giảm giá)
        RemoveAds,  // dòng "No more disruptive ads"
        NoAdsBundle,
        Bundle,     // Special Bundle
        Coin        // ô coin (lưới 3 cột)
    }

    /// <summary>1 gói trong Shop. Số lượng lấy từ *PackShop.asset của bản gốc (freeze = booster Freeze; field vẫn tên shield để giữ số trong asset).</summary>
    [Serializable]
    public class ShopProduct
    {
        public string productId;       // mã sản phẩm IAP (com.phoenix.pawblock.puzzle.*)
        public string title;
        public ShopSection section;
        public int priceVnd;
        public Sprite icon;
        [Header("Quà")]
        public int coin;
        public int hint;
        public int shield;
        public int reveal;
        public int infiniteMinutes;    // năng lượng vô hạn
        public bool removeAds;
        [Header("Hiển thị")]
        public Sprite background;      // nền thẻ (trống: giữ nền của prefab)
        public string saleTag;         // vd "90%\nOff" (thẻ Starter)

        public string PriceText => priceVnd + " VND";

        /// <summary>Số lượt booster <paramref name="type"/> trong gói. Thêm booster mới: thêm field + 1 dòng ở đây.</summary>
        public int BoosterAmount(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hint: return hint;
                case BoosterType.Freeze: return shield;
                case BoosterType.Reveal: return reveal;
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Danh sách gói trong Shop (thứ tự trong list = thứ tự hiện trong từng mục). ShopPanel spawn 1 thẻ cho mỗi gói
    /// theo prefab của section: thêm gói = bấm + rồi điền, bỏ gói = bấm -.
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Shop Database", fileName = "ShopDatabase")]
    public class ShopDatabase : ScriptableObject
    {
        public List<ShopProduct> products = new List<ShopProduct>();

        public ShopProduct Get(string productId) => products.Find(p => p.productId == productId);

        /// <summary>"30m", "1h", "1h30m".</summary>
        public static string FormatMinutes(int minutes)
        {
            if (minutes < 60) return minutes + "m";
            int h = minutes / 60, m = minutes % 60;
            return m == 0 ? h + "h" : h + "h" + m + "m";
        }
    }
}
