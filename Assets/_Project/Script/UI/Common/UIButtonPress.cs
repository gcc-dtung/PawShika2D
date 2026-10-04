using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đặt trên Canvas: mọi Button bên trong (kể cả đang tắt) có phản hồi khi nhấn (ButtonPress: tối đi khi giữ tay).
/// Nút đã có ButtonPress (chỉnh riêng, bật punch, hoặc bỏ tick để tắt) thì giữ nguyên.
/// </summary>
public class UIButtonPress : MonoBehaviour
{
    private void Awake()
    {
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            if (b.GetComponent<ButtonPress>() != null) continue;
            b.gameObject.AddComponent<ButtonPress>();
        }
    }
}
