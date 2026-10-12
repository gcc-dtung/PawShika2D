using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Features.Economy;
using PawBlock.Game;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>
    /// Nút "Lucky" trên Home (cạnh Daily): bấm mở Vòng quay may mắn. Đủ sao để quay thì lắc mời bấm + chấm đỏ
    /// (MenuShortcutButton.SetAttention); ô nhỏ dưới nút hiện số sao đang có.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LuckyWheelButton : MonoBehaviour
    {
        [SerializeField] private LuckyWheelPopup popup;
        [SerializeField] private LuckyWheelDatabase database;
        [SerializeField] private MenuShortcutButton shortcut;
        [SerializeField] private TMP_Text starsText;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            if (shortcut == null) shortcut = GetComponent<MenuShortcutButton>();
        }

        private void OnEnable()
        {
            StarWallet.Changed += OnChanged;
            Refresh();
        }

        private void OnDisable() => StarWallet.Changed -= OnChanged;

        private void OnChanged(int balance, int delta) => Refresh();

        private void Refresh()
        {
            if (starsText != null) starsText.SetText("{0}", StarWallet.Balance);
            bool canSpin = database != null && StarWallet.CanAfford(database.spinCost);
            if (shortcut != null) shortcut.SetAttention(canSpin);
        }

        private void OnClick()
        {
            if (popup != null && !popup.IsOpen) popup.Open();
        }
    }
}
