using UnityEngine;
using UnityEngine.UI;
using PawBlock.Game;

namespace PawBlock.Features.Settings
{
    /// <summary>Nút bánh răng (HUD trong ván, thanh trên Main Menu): bấm -> mở popup cài đặt qua GameFlowUI.</summary>
    [RequireComponent(typeof(Button))]
    public class SettingsButton : MonoBehaviour
    {
        [SerializeField] private GameFlowUI gameFlow;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (gameFlow != null) gameFlow.OpenSettings();
        }
    }
}
