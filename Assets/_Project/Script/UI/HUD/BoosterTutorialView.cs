using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện tutorial mở khóa booster (Canvas_HUD/BoosterTutorial, Canvas riêng sortingOrder 5):
/// nền tối chặn mọi thao tác, khung lời nhắc trên cùng, bàn tay nhấn liên tục vào nút booster.
/// Nút booster được "nhấc" lên trên nền tối bằng Canvas override sorting (thêm lúc chạy nếu chưa có) và bật quầng sáng.
/// Thứ tự: nền tối (order nền) < nút (nền + 1) < bàn tay (Canvas riêng, nền + 2).
/// Có spotlightSprite (CircleMask, như TutorialBoosterUI/CircleMask(F) gốc): nền tối thành đèn rọi, khoét 1 vòng tròn
/// mờ viền quanh nút (ảnh CircleMask đặt tại nút + 4 dải tối cùng màu phủ phần còn lại, tự tạo lúc chạy, cùng chặn chạm).
/// </summary>
public class BoosterTutorialView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text message;
    [SerializeField] private RectTransform bubble;
    [SerializeField] private RectTransform hand;
    [Tooltip("Đầu ngón tay lệch khỏi tâm nút (đơn vị canvas)")]
    [SerializeField] private Vector2 handOffset = new Vector2(20f, -20f);
    [Tooltip("ShowMessage (không nền tối): khung xuống dưới hàng tim để thấy lớp băng khiên")]
    [SerializeField] private float messageOnlyY = -390f;

    [Header("Đèn rọi quanh nút (CircleMask)")]
    [Tooltip("Ảnh đen, giữa trong suốt mờ viền. Trống = nền tối phủ đều như cũ")]
    [SerializeField] private Sprite spotlightSprite;
    [Tooltip("Cạnh ảnh đèn rọi = cạnh lớn của nút x hệ số này")]
    [SerializeField] private float spotlightScale = 2f;

    private float _bubbleY = float.NaN; // vị trí dựng sẵn (đè khung LEVEL)
    private Image _dimImage;
    private RectTransform _spot;
    private readonly RectTransform[] _sides = new RectTransform[4];

    private BoosterButton _target;
    private Canvas _targetCanvas;
    private bool _targetHadOverride;
    private int _targetOrder;
    private Tween _handTween;

    /// <summary>
    /// Chỉ khung lời nhắc, không nền tối / bàn tay / nút: lưới vẫn kéo được (bước 2 tutorial Khiên: "kéo sai thử").
    /// Khung và chữ không nhận chạm nên không chặn thao tác.
    /// </summary>
    public void ShowMessage(string text)
    {
        _handTween?.Kill();
        _handTween = null;
        if (_target != null)
        {
            _target.SetHighlight(false);
            RaiseTarget(false);
            _target = null;
        }
        gameObject.SetActive(true);
        SetDim(false);
        SetBubbleY(messageOnlyY);
        if (hand != null) hand.gameObject.SetActive(false);
        if (message != null) message.text = text;
        if (group != null)
        {
            group.DOKill();
            group.alpha = 1f;
        }
        PopBubble();
    }

    public void Show(BoosterButton target, string text)
    {
        gameObject.SetActive(true);
        SetDim(true);
        SetBubbleY(float.NaN);
        _target = target;
        PlaceSpotlight((RectTransform)target.transform);
        if (message != null) message.text = text;

        RaiseTarget(true);
        target.SetHighlight(true);

        if (group != null)
        {
            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, 0.25f).SetLink(gameObject);
        }
        PopBubble();
        PlayHand(target);
    }

    private void PopBubble()
    {
        if (bubble == null) return;
        bubble.DOKill();
        bubble.localScale = Vector3.one * 0.9f;
        bubble.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(bubble.gameObject);
    }

    /// <summary>NaN = về vị trí dựng sẵn.</summary>
    private void SetBubbleY(float y)
    {
        if (bubble == null) return;
        if (float.IsNaN(_bubbleY)) _bubbleY = bubble.anchoredPosition.y;
        bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, float.IsNaN(y) ? _bubbleY : y);
    }

    /// <summary>Nền tối (con "Dim") bật = chặn mọi thao tác ngoài nút được chỉ.</summary>
    private void SetDim(bool on)
    {
        Transform dim = transform.Find("Dim");
        if (dim != null) dim.gameObject.SetActive(on);
    }

    /// <summary>
    /// Đèn rọi: ảnh CircleMask cạnh = nút x spotlightScale đặt tại nút, 4 dải tối phủ trên / dưới / trái / phải.
    /// Tất cả cùng màu Dim (ảnh đen x alpha Dim = dải tối) nên không thấy đường nối; Image của Dim tắt để không tối 2 lớp.
    /// </summary>
    private void PlaceSpotlight(RectTransform target)
    {
        RectTransform dim = transform.Find("Dim") as RectTransform;
        if (dim == null || spotlightSprite == null) return;
        if (_dimImage == null) _dimImage = dim.GetComponent<Image>();
        if (_dimImage == null) return;
        if (_spot == null) BuildSpotlight(dim);

        Canvas.ForceUpdateCanvases();
        Vector2 center = dim.InverseTransformPoint(target.TransformPoint(target.rect.center));
        Vector2 size = target.rect.size * (target.lossyScale.x / dim.lossyScale.x);
        float half = Mathf.Max(size.x, size.y) * spotlightScale * 0.5f;

        Rect r = dim.rect;
        // Các mảnh chung mép đúng bằng nhau (không chồng): chồng dù nửa pixel cũng thành vệt tối 2 lớp
        float left = center.x - half, right = center.x + half, bottom = center.y - half, top = center.y + half;
        SetSide(_spot, left, bottom, right, top);
        SetSide(_sides[0], r.xMin, r.yMin, left, r.yMax);    // trái (cả chiều cao)
        SetSide(_sides[1], right, r.yMin, r.xMax, r.yMax);   // phải
        SetSide(_sides[2], left, top, right, r.yMax);        // trên (giữa trái / phải)
        SetSide(_sides[3], left, r.yMin, right, bottom);     // dưới
    }

    private void BuildSpotlight(RectTransform dim)
    {
        Color color = _dimImage.color;
        _dimImage.enabled = false;
        _spot = NewPiece(dim, "Spot", color);
        _spot.GetComponent<Image>().sprite = spotlightSprite;
        string[] names = { "Left", "Right", "Top", "Bottom" };
        for (int i = 0; i < _sides.Length; i++) _sides[i] = NewPiece(dim, names[i], color);
    }

    private static RectTransform NewPiece(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        go.GetComponent<Image>().color = color; // raycastTarget mặc định bật: chặn chạm như Dim cũ
        return rt;
    }

    /// <summary>
    /// Mảnh theo 2 góc (tọa độ local của Dim, gốc ở tâm), gán thẳng offset để góc đúng từng số với mảnh kề bên;
    /// rộng / cao âm (nút sát mép) thì thu về 0.
    /// </summary>
    private static void SetSide(RectTransform side, float xMin, float yMin, float xMax, float yMax)
    {
        side.offsetMin = new Vector2(xMin, yMin);
        side.offsetMax = new Vector2(Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
    }

    public void Hide()
    {
        _handTween?.Kill();
        _handTween = null;
        if (_target != null)
        {
            _target.SetHighlight(false);
            RaiseTarget(false);
            _target = null;
        }
        if (group != null) group.DOKill();
        gameObject.SetActive(false);
    }

    /// <summary>Vẽ nút trên nền tối và nhận chạm: Canvas con override sorting (cần GraphicRaycaster riêng).</summary>
    private void RaiseTarget(bool raise)
    {
        if (_target == null) return;
        if (raise)
        {
            _targetCanvas = _target.GetComponent<Canvas>();
            if (_targetCanvas == null) _targetCanvas = _target.gameObject.AddComponent<Canvas>();
            if (_target.GetComponent<GraphicRaycaster>() == null) _target.gameObject.AddComponent<GraphicRaycaster>();
            _targetHadOverride = _targetCanvas.overrideSorting;
            _targetOrder = _targetCanvas.sortingOrder;
            _targetCanvas.overrideSorting = true;
            _targetCanvas.sortingOrder = (canvas != null ? canvas.sortingOrder : 5) + 1;
        }
        else if (_targetCanvas != null)
        {
            _targetCanvas.overrideSorting = _targetHadOverride;
            _targetCanvas.sortingOrder = _targetOrder;
            _targetCanvas = null;
        }
    }

    /// <summary>Bàn tay đặt trên nút, nhấn xuống - nhấc lên lặp lại.</summary>
    private void PlayHand(BoosterButton target)
    {
        if (hand == null) return;
        _handTween?.Kill();
        hand.gameObject.SetActive(true);
        // Bàn tay vẽ trên nút booster (nút đã nhấc lên order nền + 1): Canvas riêng order nền + 2
        Canvas handCanvas = hand.GetComponent<Canvas>();
        if (handCanvas == null) handCanvas = hand.gameObject.AddComponent<Canvas>();
        handCanvas.overrideSorting = true;
        handCanvas.sortingOrder = (canvas != null ? canvas.sortingOrder : 5) + 2;
        hand.position = ((RectTransform)target.transform).position;
        hand.anchoredPosition += handOffset;
        hand.localScale = Vector3.one;
        Vector2 basePos = hand.anchoredPosition;
        _handTween = DOTween.Sequence()
            .Append(hand.DOScale(0.85f, 0.25f).SetEase(Ease.OutQuad))
            .Join(hand.DOAnchorPos(basePos + new Vector2(-8f, 8f), 0.25f))
            .Append(hand.DOScale(1f, 0.25f).SetEase(Ease.OutQuad))
            .Join(hand.DOAnchorPos(basePos, 0.25f))
            .AppendInterval(0.35f)
            .SetLoops(-1)
            .SetLink(hand.gameObject);
    }
}
