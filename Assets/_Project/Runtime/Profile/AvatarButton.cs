using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Profile
{
    /// <summary>Avatar góc trên trái Main Menu: hiện avatar + khung đang chọn, bấm mở popup hồ sơ của mình.</summary>
    [RequireComponent(typeof(Button))]
    public class AvatarButton : MonoBehaviour
    {
        private PlayerProfileService _service;

        /// <summary>GameBootstrap gọi trước OnEnable.</summary>
        public void Init(PlayerProfileService service, IMenuPopups popups)
        {
            _service = service;
            _popups = popups;
        }

        private IMenuPopups _popups;

        [SerializeField] private AvatarDatabase avatars;
        [SerializeField] private ProfileAvatarView avatarView;
        [SerializeField] private Button button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            if (_service != null) _service.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_service != null) _service.Changed -= Refresh;
        }

        private void OnClick()
        {
            _popups?.OpenProfile();
        }

        private void Refresh()
        {
            if (avatarView != null && _service != null) avatarView.Set(avatars, _service.Save.avatar, _service.Save.frame);
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
