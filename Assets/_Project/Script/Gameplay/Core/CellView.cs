using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị 1 ô và là đích raycast khi kéo. Dữ liệu nằm ở CellModel (Model).
/// PlayIntro: hiệu ứng xuất hiện khi vào level (ô nảy từ chấm nhỏ lên, sau đó số / khóa / ô hướng hiện mờ dần).
/// </summary>
public class CellView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hiển thị ô số (object con có sẵn trong Cell.prefab)")]
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private TextMeshPro lockText;
    [SerializeField] private SpriteRenderer orientationBadge; // ô nền sau con số: cao = dọc, vuông = vuông, dẹt = ngang

    // Kích thước ô nền (đơn vị world). Sprite 9-slice có viền lớn nên vẽ to rồi thu nhỏ transform.
    private static readonly Vector2 BadgeTall = new Vector2(0.5f, 0.74f);
    private static readonly Vector2 BadgeSquare = new Vector2(0.64f, 0.64f);
    private static readonly Vector2 BadgeWide = new Vector2(0.74f, 0.5f);
    private const float BadgeScale = 0.25f;

    public CellModel Model { get; private set; }
    public Vector2Int Position => Model.Position;

    private Sequence _intro;
    private Color _bodyColor = Color.white;
    private readonly List<TMP_Text> _contentTexts = new List<TMP_Text>();
    private readonly List<SpriteRenderer> _contentSprites = new List<SpriteRenderer>();
    private readonly List<float> _contentAlpha = new List<float>(); // alpha gốc: chữ trước, sprite sau

    private void Awake()
    {
        if (spriteRenderer != null) _bodyColor = spriteRenderer.color;
        // Nội dung hiện sau ô nền: số, khóa (chữ + hình), ô hướng
        if (valueText != null) _contentTexts.Add(valueText);
        if (lockText != null) _contentTexts.Add(lockText);
        if (lockRoot != null) _contentSprites.AddRange(lockRoot.GetComponentsInChildren<SpriteRenderer>(true));
        if (orientationBadge != null) _contentSprites.Add(orientationBadge);
        foreach (TMP_Text t in _contentTexts) _contentAlpha.Add(t.alpha);
        foreach (SpriteRenderer sr in _contentSprites) _contentAlpha.Add(sr.color.a);
    }

    private void OnDisable()
    {
        StopIntro();
    }

    /// <summary>
    /// Ô nền: chờ <paramref name="cellDelay"/>, phóng 0 -> 1 (OutBack) và hiện dần trong <paramref name="cellDuration"/>.
    /// Nội dung: chờ <paramref name="contentDelay"/> (tính từ lúc gọi), hiện mờ dần trong <paramref name="contentDuration"/>.
    /// </summary>
    public void PlayIntro(float cellDelay, float cellDuration, float contentDelay, float contentDuration)
    {
        StopIntro();
        transform.localScale = Vector3.zero;
        SetBodyAlpha(0f);
        SetContentAlpha(0f);

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Insert(cellDelay, transform.DOScale(1f, cellDuration).SetEase(Ease.OutBack, 1.6f));
        seq.Insert(cellDelay, DOVirtual.Float(0f, 1f, cellDuration * 0.6f, SetBodyAlpha));
        seq.Insert(contentDelay, DOVirtual.Float(0f, 1f, contentDuration, SetContentAlpha).SetEase(Ease.OutQuad));
        seq.OnKill(() => { if (_intro == seq) _intro = null; });
        _intro = seq;
    }

    /// <summary>Dừng hiệu ứng, ô về trạng thái hiển thị bình thường.</summary>
    public void StopIntro()
    {
        if (_intro == null) return;
        _intro.Kill();
        _intro = null;
        transform.localScale = Vector3.one;
        SetBodyAlpha(1f);
        SetContentAlpha(1f);
    }

    private void SetBodyAlpha(float a)
    {
        if (spriteRenderer == null) return;
        Color c = _bodyColor;
        c.a *= a;
        spriteRenderer.color = c;
    }

    private void SetContentAlpha(float a)
    {
        int i = 0;
        foreach (TMP_Text t in _contentTexts) t.alpha = _contentAlpha[i++] * a;
        foreach (SpriteRenderer sr in _contentSprites)
        {
            Color c = sr.color;
            c.a = _contentAlpha[i++] * a;
            sr.color = c;
        }
    }

    public void Bind(CellModel model)
    {
        Unbind();
        Model = model;
        Model.Changed += Refresh;
        Refresh();
    }

    public void Unbind()
    {
        StopIntro();
        if (Model != null) Model.Changed -= Refresh;
        Model = null;
    }

    // [MECHANIC] Hiển thị cơ chế trên ô: object con trong Cell.prefab, bật / tắt theo ClueCell; nhớ thêm vào _contentSprites (Awake) để hiện dần
    private void Refresh()
    {
        ClueCell clue = Model != null ? Model.Clue : null;
        bool hasClue = clue != null;
        bool locked = hasClue && clue.IsLocked;

        if (valueText != null)
        {
            bool showValue = hasClue && !locked;
            valueText.gameObject.SetActive(showValue);
            if (showValue)
            {
                if (clue.IsHidden) valueText.SetText("?");
                else valueText.SetText("{0}", clue.Value);
            }
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(locked);
            if (locked && lockText != null) lockText.SetText("{0}", clue.LockSteps);
        }

        if (orientationBadge != null)
        {
            // Ô còn khóa: chưa biết gì về clue, kể cả hướng
            bool showBadge = hasClue && !locked && clue.Constraint != ClueConstraint.None;
            orientationBadge.gameObject.SetActive(showBadge);
            if (showBadge)
            {
                Vector2 size = clue.Constraint switch
                {
                    ClueConstraint.Vertical => BadgeTall,
                    ClueConstraint.Horizontal => BadgeWide,
                    _ => BadgeSquare
                };
                orientationBadge.transform.localScale = new Vector3(BadgeScale, BadgeScale, 1f);
                orientationBadge.size = size / BadgeScale;
            }
        }
    }
}
