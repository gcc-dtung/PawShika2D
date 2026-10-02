using UnityEngine;

public class SelectionBox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float cellSize = 1.2f;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.5f;

    private Color _currentColor = Color.white;
    private ColorType _currentColorType;
    private bool _isSelecting = false;

    public Color CurrentColor => _currentColor;
    public ColorType CurrentColorType => _currentColorType;
    public float Opacity => opacity;

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

    public void SetCellSize(float size)
    {
        cellSize = size;
    }

    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
        if (spriteRenderer != null)
        {
            Color c = _currentColor;
            c.a = opacity;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// Random màu mới từ ColorType với opacity đã cài đặt.
    /// </summary>
    public void PickRandomColor()
    {
        _currentColorType = ColorTypeExtensions.GetRandomColorType();
        _currentColor = _currentColorType.ToColor(opacity);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = _currentColor;
        }
    }

    public void Show(Cell startCell, Cell targetCell, bool isFinalSelect = false)
    {
        if (startCell == null || targetCell == null) return;

        EnsureSpriteRenderer();

        // Khi bắt đầu 1 lượt kéo chọn mới, random màu theo ColorType
        if (!gameObject.activeSelf || !_isSelecting)
        {
            _isSelecting = true;
            PickRandomColor();
        }

        gameObject.SetActive(true);

        Vector3 startPos = startCell.transform.position;
        Vector3 targetPos = targetCell.transform.position;

        // 1. Đặt tâm khung chọn ở giữa 2 ô
        Vector3 centerPos = (startPos + targetPos) / 2f;
        centerPos.z = -0.1f; // Nổi lên trên trục Z
        transform.position = centerPos;

        // 2. Tính kích thước bao phủ số ô đã kéo
        float width = Mathf.Abs(targetPos.x - startPos.x) + cellSize;
        float height = Mathf.Abs(targetPos.y - startPos.y) + cellSize;

        if (spriteRenderer.drawMode == SpriteDrawMode.Sliced || spriteRenderer.drawMode == SpriteDrawMode.Tiled)
        {
            spriteRenderer.size = new Vector2(width, height);
        }
        else
        {
            transform.localScale = new Vector3(width, height, 1f);
        }

        // 3. Áp dụng màu random với opacity đã giảm
        Color c = _currentColor;
        c.a = opacity;
        spriteRenderer.color = c;
    }

    public void Hide()
    {
        _isSelecting = false;
        gameObject.SetActive(false);
    }
}
