using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Shop
{
    /// <summary>
    /// 1 thẻ / dòng / ô trong Shop (prefab theo section, ShopPanel spawn rồi Bind 1 ShopProduct): hiện tên, giá, hình, nền,
    /// coin, vật phẩm (sửa ShopDatabase.asset không cần dựng lại UI). Thẻ gói chia 2 ô như bản gốc: ô booster (lưới tự xếp,
    /// thêm booster chỉ cần thêm con) và ô coin (coinItem); món = 0 thì ẩn. Bấm giá -> ShopService.Buy.
    /// Giá chỉ là số ("289.000"), ký hiệu tiền là Image cạnh chữ trong prefab.
    /// Gói Remove Ads / No Ads Bundle đã có noAds thì ẩn dòng Remove Ads (hideWhenNoAds).
    /// Body: con chứa toàn bộ hình của thẻ để ShopIntro trượt mà không đụng vị trí do LayoutGroup đặt (trống = chính thẻ).
    /// </summary>
    public class ShopCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private Image background;
        [Tooltip("Nền các ô vật phẩm (ô booster, ô coin): đổi theo ShopProduct.itemsBackground")]
        [SerializeField] private Image[] itemsBackgrounds;
        [Tooltip("Ô coin (ẩn khi gói không có coin)")]
        [SerializeField] private GameObject coinItem;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private GameObject hintItem;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private GameObject shieldItem;
        [SerializeField] private TMP_Text shieldText;
        [SerializeField] private GameObject revealItem;
        [SerializeField] private TMP_Text revealText;
        [SerializeField] private GameObject infiniteItem;
        [SerializeField] private TMP_Text infiniteText;
        [SerializeField] private TMP_Text saleText;
        [SerializeField] private bool hideWhenNoAds;

        private ShopService _shop;
        private ShopProduct _product;

        public ShopProduct Product => _product;

        /// <summary>Phần hình để tween (khác chính thẻ thì thẻ trượt được; bằng chính thẻ thì chỉ phóng to được).</summary>
        public RectTransform Body => body != null ? body : (RectTransform)transform;

        public bool HasBody => body != null && body != transform;

        /// <summary>ShopPanel gọi ngay sau khi spawn.</summary>
        public void Bind(ShopProduct product, ShopService shop)
        {
            if (isActiveAndEnabled && _shop != null) _shop.Purchased -= OnPurchased;
            _product = product;
            _shop = shop;
            if (isActiveAndEnabled && _shop != null) _shop.Purchased += OnPurchased;
            Refresh();
        }

        private void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
        }

        private void OnEnable()
        {
            if (_shop != null) _shop.Purchased += OnPurchased;
            Refresh();
        }

        private void OnDisable()
        {
            if (_shop != null) _shop.Purchased -= OnPurchased;
        }

        public void Refresh()
        {
            if (_product == null) return;

            if (titleText != null) titleText.SetText(_product.title);
            if (priceText != null) priceText.SetText(_product.PriceText);
            if (icon != null && _product.icon != null) icon.sprite = _product.icon;
            if (background != null && _product.background != null) background.sprite = _product.background;
            if (_product.itemsBackground != null && itemsBackgrounds != null)
            {
                foreach (Image bg in itemsBackgrounds)
                {
                    if (bg != null) bg.sprite = _product.itemsBackground;
                }
            }
            if (coinItem != null) coinItem.SetActive(_product.coin > 0);
            if (coinText != null) coinText.SetText("{0}", _product.coin);
            SetItem(hintItem, hintText, _product.hint, _product.hint.ToString());
            SetItem(shieldItem, shieldText, _product.shield, _product.shield.ToString());
            SetItem(revealItem, revealText, _product.reveal, _product.reveal.ToString());
            SetItem(infiniteItem, infiniteText, _product.infiniteMinutes, ShopDatabase.FormatMinutes(_product.infiniteMinutes));
            if (saleText != null) saleText.SetText(_product.saleTag ?? "");
            if (hideWhenNoAds && _shop != null && _shop.NoAds) gameObject.SetActive(false);
        }

        private static void SetItem(GameObject item, TMP_Text text, int amount, string label)
        {
            if (item != null) item.SetActive(amount > 0);
            if (text != null) text.SetText(label);
        }

        private void OnBuy()
        {
            if (_product != null && _shop != null) _shop.Buy(_product);
        }

        private void OnPurchased(ShopProduct p)
        {
            if (hideWhenNoAds && _shop != null && _shop.NoAds) gameObject.SetActive(false);
        }
    }
}
