using TMPro;
using UnityEngine;

public class ClueCell : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    private Vector2Int _cellPosition;
    private int _lockStep;
    private bool _isHide;
    private int _value;
    private bool _hasData;
    private ClueData _clueData;

    private GameObject _lockRoot;
    private TextMeshPro _lockText;
    private SpriteRenderer _arrow;

    public int Value => _value;
    public bool HasData => _hasData;
    public ClueData ClueData => _clueData;
    public Vector2Int CellPosition => _cellPosition;
    public bool IsLocked => _hasData && _lockStep > 0;
    public bool IsHidden => _hasData && _isHide;
    public ClueConstraint Constraint => _hasData ? _clueData.mechanic.constraint : ClueConstraint.None;

    private void Awake()
    {
        EnsureTextComponent();
    }

    private void EnsureTextComponent()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshPro>(true);
            if (text == null)
            {
                text = GetComponent<TextMeshPro>();
            }
        }
    }

    public void SetClueData(ClueData data)
    {
        EnsureTextComponent();

        _clueData = data;
        _value = data.value;
        _cellPosition = new Vector2Int(data.x, data.y);
        _lockStep = data.mechanic.lockSteps;
        _isHide = data.mechanic.hidden;
        _hasData = true;

        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.sortingOrder = 6;
        }

        RefreshVisual();
    }

    /// <summary>Gọi mỗi khi người chơi đặt đúng 1 khối. Trả về true nếu ô vừa được mở khóa.</summary>
    public bool StepLock()
    {
        if (!IsLocked) return false;
        _lockStep--;
        RefreshVisual();
        return _lockStep == 0;
    }

    /// <summary>Khối chứa ô ẩn đã đặt đúng -> lộ số.</summary>
    public void Reveal()
    {
        if (!_isHide) return;
        _isHide = false;
        RefreshVisual();
    }

    public void ClearClue()
    {
        _hasData = false;
        _value = 0;
        _lockStep = 0;
        _isHide = false;
        _clueData = default;

        if (text != null)
        {
            text.SetText("");
            text.gameObject.SetActive(false);
        }
        if (_lockRoot != null) _lockRoot.SetActive(false);
        if (_arrow != null) _arrow.gameObject.SetActive(false);
    }

    private void RefreshVisual()
    {
        bool locked = IsLocked;

        if (text != null)
        {
            text.gameObject.SetActive(!locked);
            text.SetText(_isHide ? "?" : _value.ToString());
        }

        UpdateLock(locked);
        UpdateArrow(!locked && Constraint != ClueConstraint.None);
    }

    private void UpdateLock(bool show)
    {
        if (!show)
        {
            if (_lockRoot != null) _lockRoot.SetActive(false);
            return;
        }

        if (_lockRoot == null) BuildLock();
        _lockRoot.SetActive(true);
        _lockText.SetText(_lockStep.ToString());
    }

    private void BuildLock()
    {
        GameplayVisuals visuals = GameplayVisuals.Instance;

        _lockRoot = new GameObject("Lock");
        _lockRoot.transform.SetParent(transform, false);

        SpriteRenderer shackle = CreateSprite("Shackle", _lockRoot.transform, visuals != null ? visuals.lockShackleSprite : null, 6);
        shackle.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        FitSprite(shackle, 0.5f);

        SpriteRenderer body = CreateSprite("Body", _lockRoot.transform, visuals != null ? visuals.lockBodySprite : null, 7);
        body.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        FitSprite(body, 0.82f);

        GameObject textGo = new GameObject("LockText");
        textGo.transform.SetParent(_lockRoot.transform, false);
        textGo.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        _lockText = textGo.AddComponent<TextMeshPro>();
        if (text != null) _lockText.font = text.font;
        _lockText.fontSize = 4.5f;
        _lockText.fontStyle = FontStyles.Bold;
        _lockText.alignment = TextAlignmentOptions.Center;
        _lockText.color = new Color(0.36f, 0.32f, 0.7f);
        _lockText.sortingOrder = 8;
        _lockText.rectTransform.sizeDelta = new Vector2(1f, 1f);
    }

    private void UpdateArrow(bool show)
    {
        if (!show)
        {
            if (_arrow != null) _arrow.gameObject.SetActive(false);
            return;
        }

        if (_arrow == null)
        {
            GameplayVisuals visuals = GameplayVisuals.Instance;
            // Mũi tên mờ nằm dưới số, phủ gần kín ô
            _arrow = CreateSprite("Arrow", transform, visuals != null ? visuals.arrowSprite : null, 5);
            FitSprite(_arrow, 1.05f);
            _arrow.color = new Color(0.62f, 0.6f, 0.9f, 0.3f);
        }

        _arrow.gameObject.SetActive(true);
        // Sprite mũi tên gốc hướng sang phải: ngang giữ nguyên, dọc xoay 90 độ
        _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, Constraint == ClueConstraint.Vertical ? 90f : 0f);
    }

    private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static void FitSprite(SpriteRenderer sr, float maxSize)
    {
        if (sr.sprite == null) return;
        Vector3 size = sr.sprite.bounds.size;
        float k = maxSize / Mathf.Max(size.x, size.y);
        sr.transform.localScale = new Vector3(k, k, 1f);
    }
}
