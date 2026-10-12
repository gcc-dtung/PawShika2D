using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Game;

namespace PawBlock.Features.DailyReward
{
    /// <summary>
    /// Nút Daily trên Home: bấm mở popup điểm danh (IMenuPopups). Còn quà hôm nay thì lắc mời bấm + chấm đỏ
    /// (MenuShortcutButton.SetAttention), nhận rồi thì đứng yên.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DailyRewardButton : MonoBehaviour
    {
        private IMenuPopups _popups;
        private DailyRewardManager _rewards;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(DailyRewardManager rewards, IMenuPopups popups)
        {
            _rewards = rewards;
            _popups = popups;
        }

        [SerializeField] private MenuShortcutButton shortcut;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            if (shortcut == null) shortcut = GetComponent<MenuShortcutButton>();
        }

        private void OnEnable()
        {
            if (_rewards != null) _rewards.onChanged.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            if (_rewards != null) _rewards.onChanged.RemoveListener(Refresh);
        }

        private void Refresh()
        {
            bool claimable = _rewards != null && _rewards.CanClaimToday;
            if (shortcut != null) shortcut.SetAttention(claimable);
        }

        private void OnClick()
        {
            _popups?.OpenDailyReward();
        }
    }
}
