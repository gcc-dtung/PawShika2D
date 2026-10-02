using UnityEngine;

/// <summary>
/// Khung màu phủ lên vùng đang kéo. Khi vùng đổi, tâm và kích thước trượt mượt tới vùng mới
/// (các mép giãn / co ra) thay vì nhảy ngay. Khi đang hiện mèo xem trước thì khung mờ đi.
/// Bình thường khung ôm sát các ô (mỗi mép thụt vào `inset`). Ngón tay đẩy vào khối đã đặt / ô Void /
/// ra ngoài lưới thì mép phía đó trượt sang lấp khe rồi dừng ngay trước vật cản (như bị kẹt).
/// </summary>
public class SelectionBoxView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.5f;

    [Header("Chuyển động")]
    [SerializeField] private float resizeSmoothTime = 0.05f; // thời gian mép bắt kịp ô mới
    [SerializeField] private float fadeSpeed = 12f;          // tốc độ mờ / hiện khi bật tắt mèo xem trước

    [Header("Ôm ô / bị chặn")]
    [SerializeField] private float inset = 0.1f;          // bình thường mỗi mép thụt vào so với ranh giới ô
    [SerializeField] private float blockedExtend = 0.1f;  // bị chặn: mép phía đó trượt ra thêm bao nhiêu (từ vị trí ôm ô)

    private ColorType _colorType;
    private bool _isSelecting;

    private Vector3 _rectCenter;   // vùng ô (chưa thụt / chưa trượt)
    private Vector2 _rectSize;
    private Vector2Int _blockDir;
    private Vector3 _targetCenter;
    private Vector2 _targetSize;
    private Vector3 _center;
    private Vector2 _size;
    private Vector3 _centerVelocity;
    private Vector2 _sizeVelocity;

    private float _alpha;        // 0..1, nhân với opacity
    private float _targetAlpha = 1f;

    /// <summary>Màu của lượt kéo hiện tại; khối mèo tạo từ lượt kéo này dùng đúng màu này.</summary>
    public ColorType ColorType => _colorType;

    private void Awake()
    {
        EnsureSpriteRenderer();

        // Luôn đè lên ô và khối mèo đã đặt (thân 20, mặt 21, số 22); dưới bong bóng đếm ô (40)
        spriteRenderer.sortingOrder = 30;

        // Object để tắt sẵn trong scene thì Awake chạy lần đầu ngay trong Show() -> không được ẩn lại
        if (!_isSelecting) Hide();
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        // Tạo 1 sprite vuông trắng mặc định nếu chưa gán sprite nào
        if (spriteRenderer.sprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f
            );
        }
    }

    /// <summary>Đặt vùng đích (world). Lượt kéo mới thì nhảy ngay tới vùng, các lần sau trượt mượt.</summary>
    public void Show(Vector3 center, Vector2 size)
    {
        EnsureSpriteRenderer();
        center.z = -0.1f; // Nổi lên trên các ô
        _rectCenter = center;
        _rectSize = size;

        if (!_isSelecting || !gameObject.activeSelf)
        {
            // Mỗi lượt kéo chọn mới random 1 màu theo ColorType
            _isSelecting = true;
            _colorType = ColorTypeExtensions.GetRandomColorType();
            _blockDir = Vector2Int.zero;
            UpdateTarget();
            _center = _targetCenter;
            _size = _targetSize;
            _centerVelocity = Vector3.zero;
            _sizeVelocity = Vector2.zero;
            _alpha = _targetAlpha = 1f;
            gameObject.SetActive(true);
            Apply();
        }
        else
        {
            UpdateTarget();
        }
    }

    /// <summary>Hướng ngón tay đang đẩy vào vật cản (-1/0/1 mỗi trục); (0, 0) khi không bị chặn.</summary>
    public void SetBlocked(Vector2Int dir)
    {
        if (dir == _blockDir) return;
        _blockDir = dir;
        UpdateTarget();
    }

    /// <summary>Vùng ô thụt vào inset mỗi mép; mép phía bị chặn trượt ra thêm blockedExtend.</summary>
    private void UpdateTarget()
    {
        float left = -_rectSize.x / 2f + inset;
        float right = _rectSize.x / 2f - inset;
        float bottom = -_rectSize.y / 2f + inset;
        float top = _rectSize.y / 2f - inset;

        if (_blockDir.x < 0) left -= blockedExtend;
        if (_blockDir.x > 0) right += blockedExtend;
        if (_blockDir.y < 0) bottom -= blockedExtend;
        if (_blockDir.y > 0) top += blockedExtend;

        _targetCenter = _rectCenter + new Vector3((left + right) / 2f, (bottom + top) / 2f, 0f);
        _targetSize = new Vector2(right - left, top - bottom);
    }

    /// <summary>Mờ khung đi khi mèo xem trước đang hiện, hiện lại khi vùng không còn đủ điều kiện.</summary>
    public void SetFilled(bool filled)
    {
        _targetAlpha = filled ? 1f : 0f;
    }

    public void Hide()
    {
        _isSelecting = false;
        _blockDir = Vector2Int.zero;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isSelecting) return;

        _center = Vector3.SmoothDamp(_center, _targetCenter, ref _centerVelocity, resizeSmoothTime);
        _size = Vector2.SmoothDamp(_size, _targetSize, ref _sizeVelocity, resizeSmoothTime);
        _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        Apply();
    }

    private void Apply()
    {
        transform.position = _center;

        if (spriteRenderer.drawMode == SpriteDrawMode.Sliced || spriteRenderer.drawMode == SpriteDrawMode.Tiled)
        {
            spriteRenderer.size = _size;
        }
        else
        {
            transform.localScale = new Vector3(_size.x, _size.y, 1f);
        }

        spriteRenderer.color = _colorType.ToColor(opacity * _alpha);
    }
}
