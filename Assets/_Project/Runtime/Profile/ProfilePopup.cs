using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Popup hồ sơ: avatar + khung, tên (kiểu tên), huy hiệu, ID, tháng bắt đầu chơi, LVL, lưới ô thống kê.
    /// Show(profile) nhận bất kỳ PlayerProfile nào: hồ sơ của mình (IsLocal) thì bút sửa / bấm avatar mở EditProfilePanel;
    /// hồ sơ người khác (sau này mở từ bảng xếp hạng) chỉ xem.
    /// Mở giữa ván thì ván tạm dừng.
    /// </summary>
    public class ProfilePopup : PopupView
    {
        private IGamePause _pause;
        private IScreenNavigator _screens;
        private PlayerProfileService _service;

        /// <summary>GameBootstrap gọi: hồ sơ người chơi máy này.</summary>
        public void Init(PlayerProfileService service, IScreenNavigator screens, IGamePause pause)
        {
            _service = service;
            _screens = screens;
            _pause = pause;
        }

        [Header("Thẻ người chơi")]
        [SerializeField] private AvatarDatabase avatars;
        [SerializeField] private ProfileAvatarView avatarView;
        [SerializeField] private Button avatarButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button editButton;  // bút cạnh tên (chỉ hồ sơ của mình)
        [SerializeField] private Image badgeImage;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Sprite noBadgeIcon;  // khiên trống khi chưa đeo huy hiệu
        [SerializeField] private string noBadgeText = "---";
        [SerializeField] private TMP_Text idText;
        [SerializeField] private TMP_Text joinedText;
        [SerializeField] private TMP_Text levelText;

        [Header("Thống kê (icon theo thứ tự ProfileStatType)")]
        [SerializeField] private ProfileStatTile[] tiles;
        [SerializeField] private Sprite[] statIcons;

        [SerializeField] private EditProfilePanel editPanel;

        [SerializeField] private Button closeButton;

        private PlayerProfile _profile;
        private bool _pausedGame;

        public PlayerProfile Current => _profile;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (editButton != null) editButton.onClick.AddListener(OpenEditor);
            if (avatarButton != null) avatarButton.onClick.AddListener(OpenEditor);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_service != null) _service.Changed += OnLocalChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_service != null) _service.Changed -= OnLocalChanged;
            ResumeGame();
        }

        /// <summary>Mở hồ sơ <paramref name="profile"/> (của mình hoặc người khác).</summary>
        public void Show(PlayerProfile profile)
        {
            if (profile == null) return;
            _profile = profile;
            Refresh();
            Open();
        }

        protected override void OnOpened()
        {
            if (editPanel != null) editPanel.gameObject.SetActive(false);
            IGamePause lm = _pause;
            _pausedGame = lm != null && lm.IsPlaying && (_screens == null || !_screens.IsMenu);
            if (_pausedGame) lm.Pause();
        }

        private void OnCloseClicked()
        {
            if (!IsOpen) return;
            Close();
            ResumeGame();
        }

        private void ResumeGame()
        {
            if (!_pausedGame) return;
            _pausedGame = false;
            if (_pause != null) _pause.Resume();
        }

        // ---------- Hiển thị ----------

        private void Refresh()
        {
            if (_profile == null) return;
            bool mine = _profile.IsLocal;

            if (avatarView != null) avatarView.Set(avatars, _profile.AvatarId, _profile.FrameId);
            if (avatarButton != null) avatarButton.interactable = mine;
            if (editButton != null) editButton.gameObject.SetActive(mine);
            if (nameText != null)
            {
                nameText.SetText(_profile.Name);
                if (avatars != null) avatars.Style(_profile.NameStyleId).ApplyTo(nameText);
            }
            Sprite badge = avatars != null ? avatars.Badge(_profile.BadgeId) : null;
            if (badgeImage != null)
            {
                badgeImage.sprite = badge != null ? badge : noBadgeIcon;
                badgeImage.enabled = badgeImage.sprite != null;
            }
            if (badgeText != null) badgeText.SetText(badge != null ? avatars.BadgeTitle(_profile.BadgeId) : noBadgeText);
            if (idText != null) idText.SetText("PLAYER ID: " + _profile.PlayerId);
            if (joinedText != null) joinedText.SetText(_profile.JoinedText);
            if (levelText != null) levelText.SetText("{0}", _profile.Level);

            for (int i = 0; tiles != null && i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                if (i >= _profile.Stats.Count)
                {
                    tiles[i].Hide();
                    continue;
                }
                ProfileStat stat = _profile.Stats[i];
                int iconIndex = (int)stat.Type;
                Sprite icon = statIcons != null && iconIndex < statIcons.Length ? statIcons[iconIndex] : null;
                tiles[i].Set(icon, ProfileStats.Label(stat.Type), ProfileStats.Format(stat.Value));
            }
        }

        // Hồ sơ của mình đang mở mà tên / avatar / chỉ số đổi: lấy lại bản mới
        private void OnLocalChanged()
        {
            if (!IsOpen || _profile == null || !_profile.IsLocal) return;
            _profile = _service.Local();
            Refresh();
        }

        // ---------- Sửa hồ sơ ----------

        private void OpenEditor()
        {
            if (_profile == null || !_profile.IsLocal || editPanel == null) return;
            editPanel.Show(_service);
        }
    }
}
