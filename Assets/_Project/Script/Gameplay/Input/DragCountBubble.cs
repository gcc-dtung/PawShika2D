using TMPro;
using UnityEngine;

/// <summary>
/// Bong bóng hiện số ô đang chọn, bám theo ngón tay khi kéo.
/// Hình nền và chữ là object có sẵn trong DragCountBubble.prefab.
/// </summary>
public class DragCountBubble : MonoBehaviour
{
    [SerializeField] private TextMeshPro countText;
    [SerializeField] private Vector3 offset = new Vector3(-0.6f, 0.9f, 0f);

    // Object để tắt sẵn trong scene; không ẩn trong Awake vì Awake chạy lần đầu ngay trong Show()
    private int _lastCount = -1;

    public void Show(Vector3 worldPos, int count)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f) + offset;

        if (count != _lastCount)
        {
            _lastCount = count;
            countText.SetText("{0}", count);
        }
    }

    public void Hide()
    {
        _lastCount = -1;
        gameObject.SetActive(false);
    }
}
