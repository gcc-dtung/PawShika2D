using System;

namespace PawBlock.Features.Shop
{
    /// <summary>Section "shop" của save v2: món mua vĩnh viễn.</summary>
    [Serializable]
    public class ShopSave
    {
        public const string Key = "shop";

        public bool noAds; // đã mua Remove Ads / No Ads Bundle
    }
}
