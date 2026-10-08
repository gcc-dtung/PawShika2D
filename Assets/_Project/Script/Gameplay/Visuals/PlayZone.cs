using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vùng chơi: RectTransform trong Canvas_HUD (giữa HUD trên và thanh booster dưới), tự giãn theo màn hình.
/// Camera được phóng / dời để lưới (tâm ở gốc world) luôn nằm trọn và ở giữa vùng này, trên mọi tỉ lệ màn hình.
/// Vùng bị cắt thêm theo Screen.safeArea (tai thỏ, thanh home của iPhone X...).
/// Màn hình, safe area hoặc kích thước vùng đổi (xoay máy, đổi máy trong Simulator) thì tự căn lại.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PlayZone : MonoBehaviour
{
    public const float DefaultMinOrthoSize = 5f;

    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool respectSafeArea = true;
    [Tooltip("Lưới nhỏ không bị phóng quá to: nửa chiều cao camera tối thiểu (world)")]
    [SerializeField] private float minOrthoSize = DefaultMinOrthoSize;
    [Tooltip("Tô màu vùng chơi để căn chỉnh (chỉ để debug)")]
    [SerializeField] private bool showZone;
    [SerializeField] private Color zoneColor = new Color(0f, 1f, 0.4f, 0.2f);

    private RectTransform _rect;
    private Canvas _canvas;
    private Vector2 _content;   // kích thước world của lưới cần chứa
    private bool _hasContent;
    private Rect _lastZone;
    private Vector2Int _lastScreen;
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>Vùng chơi tính bằng pixel màn hình (đã cắt theo safe area) ở lần căn gần nhất.</summary>
    public Rect ScreenRect => _lastZone;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (targetCamera == null) targetCamera = Camera.main;
        ApplyZoneColor();
    }

    private void OnEnable()
    {
        _lastScreen = Vector2Int.zero; // bật lại (vào màn chơi) thì căn lại
    }

    private void OnValidate()
    {
        ApplyZoneColor();
    }

    /// <summary>LevelManager gọi khi nạp level: kích thước world của lưới. Căn ngay nếu vùng đang hiện.</summary>
    public void SetContent(Vector2 worldSize)
    {
        _content = worldSize;
        _hasContent = true;
        if (isActiveAndEnabled) Fit();
    }

    private void LateUpdate()
    {
        if (!_hasContent) return;
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (screen != _lastScreen || ComputeZone() != _lastZone) Fit();
    }

    private void Fit()
    {
        if (!_hasContent || targetCamera == null || !targetCamera.orthographic) return;

        Rect zone = ComputeZone();
        _lastZone = zone;
        _lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (zone.width <= 1f || zone.height <= 1f) return; // canvas chưa kịp tính layout, frame sau căn lại

        Rect view = targetCamera.pixelRect;
        // ortho = nửa chiều cao camera (world); 1 world = view.height / (2 * ortho) pixel
        float byHeight = _content.y * view.height / (2f * zone.height);
        float byWidth = _content.x * view.height / (2f * zone.width);
        float ortho = Mathf.Max(minOrthoSize, byHeight, byWidth);
        targetCamera.orthographicSize = ortho;

        // Dời camera để gốc world (tâm lưới) trùng tâm vùng chơi
        float pixelsPerUnit = view.height / (2f * ortho);
        Vector2 offset = (zone.center - view.center) / pixelsPerUnit;
        Vector3 pos = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(-offset.x, -offset.y, pos.z);
    }

    /// <summary>Hình chữ nhật của vùng trên màn hình (pixel), cắt theo safe area.</summary>
    private Rect ComputeZone()
    {
        if (_rect == null) return Rect.zero;
        Vector3[] corners = _corners;
        _rect.GetWorldCorners(corners);
        Camera uiCam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);
        Rect zone = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        if (respectSafeArea)
        {
            Rect safe = Screen.safeArea;
            zone = Rect.MinMaxRect(
                Mathf.Max(zone.xMin, safe.xMin), Mathf.Max(zone.yMin, safe.yMin),
                Mathf.Min(zone.xMax, safe.xMax), Mathf.Min(zone.yMax, safe.yMax));
        }
        return zone;
    }

    private void ApplyZoneColor()
    {
        Image img = GetComponent<Image>();
        if (img == null) return;
        img.raycastTarget = false;
        img.color = zoneColor;
        img.enabled = showZone; // tắt hẳn khi không debug để không tốn lượt vẽ
    }
}
