using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Settings
{
    /// <summary>Nút bánh răng (HUD trong ván, thanh trên Main Menu): bấm -> mở popup cài đặt (IMenuPopups).</summary>
    [RequireComponent(typeof(Button))]
    public class SettingsButton : MonoBehaviour
    {
        private IMenuPopups _popups;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(IMenuPopups popups)
        {
            _popups = popups;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _popups?.OpenSettings();
        }
    }
}
