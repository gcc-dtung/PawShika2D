using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Shared;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Hết lượt booster: mua valueOneBuy lượt bằng coin. Mở popup thì ván tạm dừng.</summary>
    public class BuyBoosterPopup : PopupView
    {
        private BoosterManager _boosterManager;
        private LevelManager _levels;
        private ICurrencyWallet _coins;

        /// <summary>GameBootstrap gọi: ví coin (Economy, qua contract).</summary>
        public void Init(ICurrencyWallet coins, LevelManager levels, BoosterManager boosterManager)
        {
            _coins = coins;
            _levels = levels;
            _boosterManager = boosterManager;
        }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text priceText;
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

        protected override void OnOpened()
        {
            if (_levels != null) _levels.Pause();

            BoosterData data = _boosterManager != null ? _boosterManager.Get(_type) : null;
            if (data == null) return;
            if (titleText != null) titleText.SetText(data.name.ToUpperInvariant());
            if (detailText != null) detailText.SetText(data.detail);
            if (icon != null && data.icon != null) icon.sprite = data.icon;
            if (amountText != null) amountText.SetText("x{0}", data.valueOneBuy);
            if (priceText != null) priceText.SetText("{0}", data.priceOneBuy);
            if (buyButton != null) buyButton.interactable = _coins.Balance >= data.priceOneBuy;
        }

        private void OnBuy()
        {
            if (_boosterManager == null || !_boosterManager.Buy(_type))
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
            if (_levels != null) _levels.Resume();
        }
    }
}
