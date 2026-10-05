using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện tutorial mở khóa booster (Canvas_HUD/BoosterTutorial, Canvas riêng sortingOrder 5):
/// nền tối chặn mọi thao tác, khung lời nhắc trên cùng, bàn tay nhấn liên tục vào nút booster.
/// Nút booster được "nhấc" lên trên nền tối bằng Canvas override sorting (thêm lúc chạy nếu chưa có) và bật quầng sáng.
/// Thứ tự: nền tối (order nền) < nút (nền + 1) < bàn tay (Canvas riêng, nền + 2).
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

    private float _bubbleY = float.NaN; // vị trí dựng sẵn (đè khung LEVEL)

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
