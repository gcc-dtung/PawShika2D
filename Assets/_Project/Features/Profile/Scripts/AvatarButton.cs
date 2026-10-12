using UnityEngine;
using UnityEngine.UI;
using PawBlock.Game;

namespace PawBlock.Features.Profile
{
    /// <summary>Avatar góc trên trái Main Menu: hiện avatar đang chọn, bấm mở popup hồ sơ của mình.</summary>
    [RequireComponent(typeof(Button))]
    public class AvatarButton : MonoBehaviour
    {
        private PlayerProfileService _service;

        /// <summary>GameBootstrap gọi trước OnEnable.</summary>
        public void Init(PlayerProfileService service) => _service = service;

        [SerializeField] private AvatarDatabase avatars;
        [SerializeField] private Image avatarImage;
        [SerializeField] private GameFlowUI gameFlow;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
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
            if (gameFlow != null) gameFlow.OpenProfile();
        }

        private void Refresh()
        {
            if (avatarImage != null && avatars != null && _service != null) avatarImage.sprite = avatars.Get(_service.Save.avatar);
        }
    }
}
