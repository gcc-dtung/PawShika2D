using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Game;

namespace PawBlock.Features.Economy
{
    /// <summary>Số coin trên thanh trên của Menu; đổi số thì icon nảy. Bấm (nút "+") -> sang tab Shop.</summary>
    public class CoinHUD : MonoBehaviour
    {
        private CoinWallet _coins;

        /// <summary>GameBootstrap gọi trước OnEnable.</summary>
        public void Init(CoinWallet coins) => _coins = coins;

        [SerializeField] private TMP_Text amountText;
        [SerializeField] private RectTransform icon;
        [SerializeField] private Button button;
        [SerializeField] private MainMenuNavigator navigator;

        private Tween _punch;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            if (_coins != null) _coins.Changed += OnChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_coins != null) _coins.Changed -= OnChanged;
            _punch?.Kill();
            _punch = null;
        }

        private void OnClick()
        {
            if (navigator != null) navigator.Select(MainMenuNavigator.ShopTab);
        }

        private void OnChanged(int balance, int delta)
        {
            Refresh();
            if (icon == null) return;
            _punch?.Kill();
            icon.localScale = Vector3.one;
            _punch = icon.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f)
                .SetLink(gameObject)
                .OnKill(() => { if (icon != null) icon.localScale = Vector3.one; });
        }

        private void Refresh()
        {
            if (amountText != null) amountText.SetText("{0}", _coins != null ? _coins.Balance : 0);
        }
    }
}
