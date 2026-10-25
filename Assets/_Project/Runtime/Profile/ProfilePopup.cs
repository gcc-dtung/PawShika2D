using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Popup hồ sơ: avatar, tên, ID, tháng bắt đầu chơi, LVL, lưới ô thống kê.
    /// Show(profile) nhận bất kỳ PlayerProfile nào: hồ sơ của mình (IsLocal) thì có nút sửa tên + bấm avatar để đổi;
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
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button avatarButton;
        [SerializeField] private GameObject avatarEditBadge;  // bút nhỏ trên avatar (chỉ hồ sơ của mình)
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button editNameButton;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private TMP_Text joinedText;
        [SerializeField] private TMP_Text levelText;

        [Header("Thống kê (icon theo thứ tự ProfileStatType)")]
        [SerializeField] private ProfileStatTile[] tiles;
        [SerializeField] private Sprite[] statIcons;

        [Header("Chọn avatar")]
        [SerializeField] private GameObject avatarPicker;
        [SerializeField] private Button[] pickerButtons;     // nút i = avatar i
        [SerializeField] private GameObject[] pickerSelected; // viền chọn của nút i
        [SerializeField] private Button pickerCloseButton;

        [SerializeField] private Button closeButton;

        private PlayerProfile _profile;
        private bool _pausedGame;

        public PlayerProfile Current => _profile;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (editNameButton != null) editNameButton.onClick.AddListener(BeginEditName);
            if (avatarButton != null) avatarButton.onClick.AddListener(OpenPicker);
            if (pickerCloseButton != null) pickerCloseButton.onClick.AddListener(ClosePicker);
            if (nameInput != null)
            {
                nameInput.characterLimit = PlayerProfileService.NameMaxLength;
                nameInput.onEndEdit.AddListener(EndEditName);
            }
            for (int i = 0; pickerButtons != null && i < pickerButtons.Length; i++)
            {
                int id = i;
                if (pickerButtons[i] != null) pickerButtons[i].onClick.AddListener(() => PickAvatar(id));
            }
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
            if (avatarPicker != null) avatarPicker.SetActive(false);
            SetEditing(false);
            IGamePause lm = _pause;
            _pausedGame = lm != null && lm.IsPlaying && (_screens == null || !_screens.IsMenu);
            if (_pausedGame) lm.Pause();
        }

        private void OnCloseClicked()
        {
            if (!IsOpen) return;
            if (nameInput != null && nameInput.gameObject.activeSelf) EndEditName(nameInput.text); // đang sửa tên: lưu luôn
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

            if (avatarImage != null && avatars != null) avatarImage.sprite = avatars.Get(_profile.AvatarId);
            if (avatarButton != null) avatarButton.interactable = mine;
            if (avatarEditBadge != null) avatarEditBadge.SetActive(mine);
            if (editNameButton != null) editNameButton.gameObject.SetActive(mine);
            if (nameText != null) nameText.SetText(_profile.Name);
            if (idText != null) idText.SetText("ID: " + _profile.PlayerId);
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
            RefreshPicker();
        }

        // Hồ sơ của mình đang mở mà tên / avatar / chỉ số đổi: lấy lại bản mới
        private void OnLocalChanged()
        {
            if (!IsOpen || _profile == null || !_profile.IsLocal) return;
            _profile = _service.Local();
            Refresh();
        }

        // ---------- Sửa tên ----------

        private void BeginEditName()
        {
            if (_profile == null || !_profile.IsLocal || nameInput == null) return;
            SetEditing(true);
            nameInput.text = _profile.Name;
            nameInput.Select();
            nameInput.ActivateInputField();
        }

        private void EndEditName(string text)
        {
            if (nameInput == null || !nameInput.gameObject.activeSelf) return;
            SetEditing(false);
            if (!_service.Rename(text)) Refresh(); // tên không hợp lệ: giữ tên cũ
        }

        private void SetEditing(bool editing)
        {
            if (nameInput != null) nameInput.gameObject.SetActive(editing);
            if (nameText != null) nameText.gameObject.SetActive(!editing);
            if (editNameButton != null && _profile != null) editNameButton.gameObject.SetActive(!editing && _profile.IsLocal);
        }

        // ---------- Chọn avatar ----------

        private void OpenPicker()
        {
            if (_profile == null || !_profile.IsLocal || avatarPicker == null) return;
            RefreshPicker();
            avatarPicker.SetActive(true);
        }

        private void ClosePicker()
        {
            if (avatarPicker != null) avatarPicker.SetActive(false);
        }

        private void PickAvatar(int id)
        {
            _service.SetAvatar(id);
            ClosePicker();
        }

        private void RefreshPicker()
        {
            int current = _profile != null ? _profile.AvatarId : -1;
            for (int i = 0; pickerSelected != null && i < pickerSelected.Length; i++)
            {
                if (pickerSelected[i] != null) pickerSelected[i].SetActive(i == current);
            }
        }
    }
}
