using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Gốc của 1 tab trong Main Menu (Shop, Home, Daily...), đặt ở object gốc của prefab tab.
    /// Tab tự lo nội dung của nó (Init cho view con, vẽ lại khi hiện); điều hướng (MainMenuNavigator) chỉ xếp / trượt
    /// và báo tab nào đang được chọn qua SetSelected.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class MenuPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;

        public RectTransform Rect => rect;

        /// <summary>Đang là tab được chọn.</summary>
        public bool IsSelected { get; private set; }

        /// <summary>Điều hướng gọi khi tab này được chọn / bỏ chọn.</summary>
        public void SetSelected(bool selected)
        {
            if (selected == IsSelected) return;
            IsSelected = selected;
            if (selected) OnShown();
            else OnHidden();
        }

        /// <summary>Vừa thành tab đang chọn.</summary>
        protected virtual void OnShown() { }

        /// <summary>Vừa chuyển sang tab khác.</summary>
        protected virtual void OnHidden() { }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        protected virtual void OnValidate()
        {
            if (rect == null) rect = (RectTransform)transform;
        }
#endif
    }
}
