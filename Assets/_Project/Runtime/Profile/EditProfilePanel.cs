using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Bảng "Edit Profile" (nằm trong ProfilePopup): 4 tab Avatar / Frame / Name / Badge, lưới ô để chọn, xem trước ở đầu bảng.
    /// Chọn chỉ đổi bản nháp; bấm Save mới ghi vào PlayerProfileService, bấm X thì bỏ nháp.
    /// Món chưa mở (level người chơi &lt; unlockLevel) bấm vào chỉ hiện dòng gợi ý.
    /// </summary>
    public class EditProfilePanel : MonoBehaviour
    {
        public enum Tab { Avatar = 0, Frame = 1, Name = 2, Badge = 3 }

        [SerializeField] private AvatarDatabase looks;
        [SerializeField] private RectTransform box;

        [Header("Xem trước")]
        [SerializeField] private ProfileAvatarView preview;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button editNameButton;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text idText;

        [Header("Tab (thứ tự theo Tab)")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabSelected;  // nền tab đang mở

        [Header("Lưới")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private ProfileItemCell cellPrefab;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private Vector2 itemCellSize = new Vector2(240f, 240f);  // avatar / khung / huy hiệu: 3 cột
        [SerializeField] private Vector2 nameCellSize = new Vector2(360f, 137f);  // kiểu tên: 2 cột
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private string nameSample = "Name";

        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;

        private readonly List<ProfileItemCell> _cells = new List<ProfileItemCell>();
        private PlayerProfileService _service;
        private Tab _tab;
        private int _level;
        private int _avatar, _frame, _nameStyle, _badge;
        private string _name;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Đã đóng (lưu hoặc bỏ).</summary>
        public event Action Hidden;

        private void Awake()
        {
            for (int i = 0; tabButtons != null && i < tabButtons.Length; i++)
            {
                Tab tab = (Tab)i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }
            if (saveButton != null) saveButton.onClick.AddListener(OnSave);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (editNameButton != null) editNameButton.onClick.AddListener(BeginEditName);
            if (nameInput != null)
            {
                nameInput.characterLimit = PlayerProfileService.NameMaxLength;
                nameInput.onEndEdit.AddListener(EndEditName);
            }
        }

        /// <summary>Mở bảng với bản nháp = hồ sơ hiện tại của người chơi máy này.</summary>
        public void Show(PlayerProfileService service, Tab tab = Tab.Avatar)
        {
            _service = service;
            if (_service == null) return;
            PlayerProfile me = _service.Local();
            _level = me.Level;
            _avatar = me.AvatarId;
            _frame = me.FrameId;
            _nameStyle = me.NameStyleId;
            _badge = me.BadgeId;
            _name = me.Name;
            if (idText != null) idText.SetText("PLAYER ID: " + me.PlayerId);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetEditing(false);
            SelectTab(tab);
            RefreshPreview();
            if (box != null)
            {
                box.localScale = Vector3.one * 0.8f;
                box.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
        }

        public void Hide()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            Hidden?.Invoke();
        }

        private void OnSave()
        {
            if (nameInput != null && nameInput.gameObject.activeSelf) EndEditName(nameInput.text);
            if (_name != _service.Local().Name && !_service.Rename(_name))
            {
                ShowHint($"Name needs {PlayerProfileService.NameMinLength}-{PlayerProfileService.NameMaxLength} characters");
                return;
            }
            _service.SetLook(_avatar, _frame, _nameStyle, _badge);
            Hide();
        }

        // ---------- Xem trước ----------

        private void RefreshPreview()
        {
            if (preview != null) preview.Set(looks, _avatar, _frame);
            if (nameText != null)
            {
                nameText.SetText(_name);
                if (looks != null) looks.Style(_nameStyle).ApplyTo(nameText);
            }
        }

        // ---------- Sửa tên (nháp, Save mới ghi) ----------

        private void BeginEditName()
        {
            if (nameInput == null) return;
            SetEditing(true);
            nameInput.text = _name;
            nameInput.Select();
            nameInput.ActivateInputField();
        }

        private void EndEditName(string text)
        {
            if (nameInput == null || !nameInput.gameObject.activeSelf) return;
            SetEditing(false);
            string clean = PlayerProfileService.Clean(text);
            if (clean.Length >= PlayerProfileService.NameMinLength) _name = clean;
            else ShowHint($"Name needs {PlayerProfileService.NameMinLength}-{PlayerProfileService.NameMaxLength} characters");
            RefreshPreview();
        }

        private void SetEditing(bool editing)
        {
            if (nameInput != null) nameInput.gameObject.SetActive(editing);
            if (nameText != null) nameText.gameObject.SetActive(!editing);
            if (editNameButton != null) editNameButton.gameObject.SetActive(!editing);
        }

        // ---------- Tab + lưới ----------

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            for (int i = 0; tabSelected != null && i < tabSelected.Length; i++)
            {
                if (tabSelected[i] != null) tabSelected[i].SetActive(i == (int)tab);
            }
            ShowHint(null);
            BuildGrid();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private int ItemCount()
        {
            if (looks == null) return 0;
            switch (_tab)
            {
                case Tab.Avatar: return looks.avatars.Length;
                case Tab.Frame: return looks.frames.Length;
                case Tab.Name: return looks.nameStyles.Length;
                default: return looks.badges.Length;
            }
        }

        private int UnlockLevel(int i)
        {
            switch (_tab)
            {
                case Tab.Avatar: return looks.avatars[i].unlockLevel;
                case Tab.Frame: return looks.frames[i].unlockLevel;
                case Tab.Name: return looks.nameStyles[i].unlockLevel;
                default: return looks.badges[i].unlockLevel;
            }
        }

        private int SelectedIndex()
        {
            switch (_tab)
            {
                case Tab.Avatar: return _avatar;
                case Tab.Frame: return _frame;
                case Tab.Name: return _nameStyle;
                default: return _badge;
            }
        }

        private void BuildGrid()
        {
            int count = ItemCount();
            if (grid != null)
            {
                bool names = _tab == Tab.Name;
                grid.cellSize = names ? nameCellSize : itemCellSize;
                grid.constraintCount = names ? 2 : 3;
            }
            while (_cells.Count < count)
            {
                ProfileItemCell cell = Instantiate(cellPrefab, content);
                cell.Clicked += OnCellClicked;
                _cells.Add(cell);
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                bool used = i < count;
                _cells[i].gameObject.SetActive(used);
                if (!used) continue;
                switch (_tab)
                {
                    case Tab.Avatar: _cells[i].SetSprite(i, looks.avatars[i].sprite); break;
                    case Tab.Frame: _cells[i].SetSprite(i, looks.frames[i].sprite); break;
                    case Tab.Name: _cells[i].SetName(i, nameSample, looks.nameStyles[i]); break;
                    default: _cells[i].SetSprite(i, looks.badges[i].sprite); break;
                }
            }
            RefreshCells();
        }

        private void RefreshCells()
        {
            int selected = SelectedIndex();
            int count = ItemCount();
            for (int i = 0; i < count; i++) _cells[i].SetState(_level < UnlockLevel(i), i == selected);
        }

        private void OnCellClicked(ProfileItemCell cell)
        {
            int i = cell.Index;
            int need = UnlockLevel(i);
            if (_level < need)
            {
                ShowHint($"Unlocks at level {need}");
                cell.transform.DOKill(true);
                cell.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20).SetUpdate(true).SetLink(cell.gameObject);
                return;
            }
            ShowHint(null);
            switch (_tab)
            {
                case Tab.Avatar: _avatar = i; break;
                case Tab.Frame: _frame = i; break;
                case Tab.Name: _nameStyle = i; break;
                default: _badge = _badge == i ? -1 : i; break; // bấm lại huy hiệu đang đeo = tháo
            }
            RefreshCells();
            RefreshPreview();
        }

        private void ShowHint(string text)
        {
            if (hintText == null) return;
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) hintText.SetText(text);
        }
    }
}
