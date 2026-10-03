using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Hết lượt booster: mua valueOneBuy lượt bằng coin. Mở popup thì ván tạm dừng.</summary>
public class BuyBoosterPopup : PopupView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    private BoosterType _type;

    protected override void Awake()
    {
        base.Awake();
        if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);
    }

    public void Show(BoosterType type)
    {
        _type = type;
        Open();
    }

    protected override void OnOpen()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.Pause();

        BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(_type) : null;
        if (data == null) return;
        if (titleText != null) titleText.SetText(data.name.ToUpperInvariant());
        if (detailText != null) detailText.SetText(data.detail);
        if (icon != null && data.icon != null) icon.sprite = data.icon;
        if (amountText != null) amountText.SetText("x{0}", data.valueOneBuy);
        if (priceText != null) priceText.SetText("{0}", data.priceOneBuy);
        if (balanceText != null) balanceText.SetText("{0}", CoinWallet.Balance);
        if (buyButton != null) buyButton.interactable = CoinWallet.CanAfford(data.priceOneBuy);
    }

    private void OnBuy()
    {
        if (BoosterManager.Instance == null || !BoosterManager.Instance.Buy(_type))
        {
            // Chưa có Shop (Giai đoạn 6): chỉ báo thiếu coin
            Debug.Log($"[BuyBoosterPopup] Không đủ coin mua {_type}");
            return;
        }
        OnClose();
    }

    private void OnClose()
    {
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.Resume();
    }
}
