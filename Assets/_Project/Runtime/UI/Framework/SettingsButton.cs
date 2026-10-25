using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Shared
{
    /// <summary>Nút bánh răng (HUD trong ván, thanh trên Main Menu): bấm -> mở popup cài đặt (IMenuPopups).</summary>
    [RequireComponent(typeof(Button))]
    public class SettingsButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private IMenuPopups _popups;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IMenuPopups popups)
        {
            _popups = popups;
        }

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _popups?.OpenSettings();
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
