using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Đặt trên Canvas: mọi Button bên trong (kể cả đang tắt) có phản hồi khi nhấn (ButtonPress: tối đi khi giữ tay).
    /// ButtonPress được gắn sẵn trong Editor (UIAutoSetup chạy khi lưu scene / vào Play), lúc chạy không gắn gì (guide 8.3).
    /// Nút đã có ButtonPress (chỉnh riêng, bật punch, hoặc bỏ tick để tắt) thì giữ nguyên.
    /// </summary>
    public class UIButtonPress : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>Gắn ButtonPress cho Button con chưa có; trả về số nút vừa gắn.</summary>
        [ContextMenu("Gắn ButtonPress cho mọi Button con")]
        public int EditorAttach()
        {
            int added = 0;
            foreach (Button b in GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<ButtonPress>() != null) continue;
                UnityEditor.Undo.AddComponent<ButtonPress>(b.gameObject);
                added++;
            }
            return added;
        }
#endif
    }
}
