using TMPro;
using UnityEngine;

/// <summary>Bong bóng hiện số ô đang chọn, bám theo ngón tay khi kéo.</summary>
public class DragCountBubble : MonoBehaviour
{
    [SerializeField] private float diameter = 1.1f;
    [SerializeField] private Vector3 offset = new Vector3(-0.6f, 0.9f, 0f);
    [SerializeField] private Color bubbleColor = new Color(0.55f, 0.52f, 0.85f, 0.95f);
    [SerializeField] private int sortingOrder = 40;

    private SpriteRenderer _bg;
    private TextMeshPro _text;

    private void Awake()
    {
        Build();
        Hide();
    }

    private void Build()
    {
        GameplayVisuals visuals = GameplayVisuals.Instance;

        _bg = gameObject.AddComponent<SpriteRenderer>();
        _bg.sprite = visuals != null ? visuals.bubbleSprite : null;
        _bg.color = bubbleColor;
        _bg.sortingOrder = sortingOrder;
        if (_bg.sprite != null)
        {
            float k = diameter / Mathf.Max(_bg.sprite.bounds.size.x, _bg.sprite.bounds.size.y);
            transform.localScale = new Vector3(k, k, 1f);
        }

        GameObject textGo = new GameObject("Count");
        textGo.transform.SetParent(transform, false);
        _text = textGo.AddComponent<TextMeshPro>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontStyle = FontStyles.Bold;
        _text.color = Color.white;
        _text.sortingOrder = sortingOrder + 1;
        _text.rectTransform.sizeDelta = new Vector2(diameter, diameter) / Mathf.Max(0.0001f, transform.localScale.x);
        _text.fontSize = 6f / Mathf.Max(0.0001f, transform.localScale.x);
    }

    public void SetFont(TMP_FontAsset font)
    {
        if (font != null && _text != null) _text.font = font;
    }

    public void Show(Vector3 worldPos, int count)
    {
        gameObject.SetActive(true);
        transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f) + offset;
        _text.SetText(count.ToString());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
