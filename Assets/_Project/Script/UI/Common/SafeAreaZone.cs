using UnityEngine;

/// <summary>
/// UI Zone: RectTransform co theo Screen.safeArea để UI bên trong không bị tai thỏ / camera đục lỗ / thanh home che.
/// Đặt làm con trực tiếp của một RectTransform phủ toàn màn hình (canvas, hoặc panel giãn hết canvas);
/// neo được đặt theo tỉ lệ safe area nên không phụ thuộc CanvasScaler.
/// Chọn cạnh áp dụng (vd. chỉ cạnh trên cho thanh trên của Home).
/// Chỉ chạy khi Play: trong Editor vùng phủ toàn màn hình như cũ, scene không bị đổi.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaZone : MonoBehaviour
{
    [SerializeField] private bool top = true;
    [SerializeField] private bool bottom = true;
    [SerializeField] private bool left = true;
    [SerializeField] private bool right = true;

    private RectTransform _rect;
    private Rect _lastSafe;
    private Vector2Int _lastScreen;

    private void Awake()
    {
        _rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (Screen.safeArea != _lastSafe || Screen.width != _lastScreen.x || Screen.height != _lastScreen.y) Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        _lastSafe = safe;
        _lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = new Vector2(left ? safe.xMin / Screen.width : 0f, bottom ? safe.yMin / Screen.height : 0f);
        Vector2 max = new Vector2(right ? safe.xMax / Screen.width : 1f, top ? safe.yMax / Screen.height : 1f);
        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
