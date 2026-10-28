using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Profile
{
    /// <summary>1 ô trong lưới popup sửa hồ sơ: ảnh (avatar / khung / huy hiệu) hoặc chữ mẫu (kiểu tên), dấu khóa, dấu đang chọn.</summary>
    [RequireComponent(typeof(Button))]
    public class ProfileItemCell : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;     // bảng nền chỉ bật ở ô kiểu tên
        [SerializeField] private Sprite nameBackground;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject lockMark;
        [SerializeField] private GameObject selectedMark;  // viền sáng sau ô
        [SerializeField] private GameObject tickMark;      // dấu tick đè lên góc ô
        [SerializeField] private Vector2 nameLockOffset = new Vector2(150f, -10f);  // ô kiểu tên: khóa dời sang phải, không che chữ

        public int Index { get; private set; }

        public event Action<ProfileItemCell> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void SetSprite(int index, Sprite sprite)
        {
            Index = index;
            if (background != null) background.enabled = false;
            if (lockMark != null) lockMark.transform.localPosition = Vector3.zero;
            icon.gameObject.SetActive(true);
            icon.sprite = sprite;
            label.gameObject.SetActive(false);
        }

        public void SetName(int index, string text, NameStyle style)
        {
            Index = index;
            if (background != null)
            {
                background.enabled = true;
                background.sprite = nameBackground;
            }
            if (lockMark != null) lockMark.transform.localPosition = nameLockOffset;
            icon.gameObject.SetActive(false);
            label.gameObject.SetActive(true);
            label.SetText(text);
            style.ApplyTo(label);
        }

        public void SetState(bool locked, bool selected)
        {
            if (lockMark != null) lockMark.SetActive(locked);
            if (selectedMark != null) selectedMark.SetActive(selected);
            if (tickMark != null) tickMark.SetActive(selected);
            icon.color = locked ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
            label.alpha = locked ? 0.45f : 1f;
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
