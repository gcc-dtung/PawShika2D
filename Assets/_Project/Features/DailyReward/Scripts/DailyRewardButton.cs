using UnityEngine;
using UnityEngine.UI;
using PawBlock.Game;

namespace PawBlock.Features.DailyReward
{
    /// <summary>
    /// Nút Daily trên Home: bấm mở popup điểm danh (GameFlowUI). Còn quà hôm nay thì lắc mời bấm + chấm đỏ
    /// (MenuShortcutButton.SetAttention), nhận rồi thì đứng yên.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DailyRewardButton : MonoBehaviour
    {
        [SerializeField] private GameFlowUI gameFlow;
        [SerializeField] private MenuShortcutButton shortcut;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            if (shortcut == null) shortcut = GetComponent<MenuShortcutButton>();
        }

        private void OnEnable()
        {
            if (DailyRewardManager.Instance != null) DailyRewardManager.Instance.onChanged.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            if (DailyRewardManager.Instance != null) DailyRewardManager.Instance.onChanged.RemoveListener(Refresh);
        }

        private void Refresh()
        {
            bool claimable = DailyRewardManager.Instance != null && DailyRewardManager.Instance.CanClaimToday;
            if (shortcut != null) shortcut.SetAttention(claimable);
        }

        private void OnClick()
        {
            if (gameFlow != null) gameFlow.OpenDailyReward();
        }
    }
}
