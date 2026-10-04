using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 thẻ / dòng / ô trong Shop: đọc ShopProduct (ShopDatabase) lúc chạy để hiện tên, giá, hình, coin, vật phẩm
/// (sửa ShopDatabase.asset không cần dựng lại UI). Ô vật phẩm cố định 2x2: Hint trên trái, Shield trên phải,
/// Reveal dưới trái, năng lượng vô hạn dưới phải; món = 0 thì ẩn. Bấm giá -> ShopService.Buy.
/// Gói Remove Ads / No Ads Bundle đã có noAds thì ẩn dòng Remove Ads (hideWhenNoAds).
/// </summary>
public class ShopCardView : MonoBehaviour
{
    [SerializeField] private ShopDatabase database;
    [SerializeField] private string productId;
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

    private ShopProduct _product;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
    }

    private void OnEnable()
    {
        ShopService.Purchased += OnPurchased;
        Refresh();
    }

    private void OnDisable()
    {
        ShopService.Purchased -= OnPurchased;
    }

    public void Refresh()
    {
        _product = database != null ? database.Get(productId) : null;
        if (_product == null) return;

        if (titleText != null) titleText.SetText(_product.title);
        if (priceText != null) priceText.SetText(_product.PriceText);
        if (icon != null && _product.icon != null) icon.sprite = _product.icon;
        if (coinText != null) coinText.SetText("{0}", _product.coin);
        SetItem(hintItem, hintText, _product.hint, "x" + _product.hint);
        SetItem(shieldItem, shieldText, _product.shield, "x" + _product.shield);
        SetItem(revealItem, revealText, _product.reveal, "x" + _product.reveal);
        SetItem(infiniteItem, infiniteText, _product.infiniteMinutes, ShopDatabase.FormatMinutes(_product.infiniteMinutes));
        if (saleText != null) saleText.SetText(_product.saleTag ?? "");
        if (hideWhenNoAds && ShopService.NoAds) gameObject.SetActive(false);
    }

    private static void SetItem(GameObject item, TMP_Text text, int amount, string label)
    {
        if (item != null) item.SetActive(amount > 0);
        if (text != null) text.SetText(label);
    }

    private void OnBuy()
    {
        if (_product != null) ShopService.Buy(_product);
    }

    private void OnPurchased(ShopProduct p)
    {
        if (hideWhenNoAds && ShopService.NoAds) gameObject.SetActive(false);
    }
}
