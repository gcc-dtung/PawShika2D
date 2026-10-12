using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Shared;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>
    /// Nút "Lucky" trên Home (cạnh Daily): bấm mở Vòng quay may mắn. Đủ sao để quay thì lắc mời bấm + chấm đỏ
    /// (MenuShortcutButton.SetAttention); ô nhỏ dưới nút hiện số sao đang có.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LuckyWheelButton : MonoBehaviour
    {
        private ICurrencyWallet _stars;

        /// <summary>GameBootstrap gọi trước OnEnable: ví sao (Economy, qua contract).</summary>
        public void Init(ICurrencyWallet stars) => _stars = stars;

        [SerializeField] private LuckyWheelPopup popup;
        [SerializeField] private LuckyWheelDatabase database;
        [SerializeField] private MenuShortcutButton shortcut;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private Button button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            EventBus<StarsBalanceChangedEvent>.Subscribe(OnChanged);
            Refresh();
        }

        private void OnDisable() => EventBus<StarsBalanceChangedEvent>.Unsubscribe(OnChanged);

        private void OnChanged(StarsBalanceChangedEvent e) => Refresh();

        private void Refresh()
        {
            if (starsText != null) starsText.SetText("{0}", Stars);
            bool canSpin = database != null && Stars >= database.spinCost;
            if (shortcut != null) shortcut.SetAttention(canSpin);
        }

        private int Stars => _stars != null ? _stars.Balance : 0;

        private void OnClick()
        {
            if (popup != null && !popup.IsOpen) popup.Open();
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }
#endif
    }
}
