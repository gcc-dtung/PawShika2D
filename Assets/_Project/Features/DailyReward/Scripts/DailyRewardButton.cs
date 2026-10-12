using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Shared;

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
        [SerializeField] private Button button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
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

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }
#endif
    }
}
