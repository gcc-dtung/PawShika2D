using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện tutorial mở khóa booster (Canvas_HUD/BoosterTutorial, Canvas riêng sortingOrder 5):
/// nền tối chặn mọi thao tác, khung lời nhắc trên cùng, bàn tay nhấn liên tục vào nút booster.
/// Nút booster được "nhấc" lên trên nền tối bằng Canvas override sorting (thêm lúc chạy nếu chưa có) và bật quầng sáng.
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

    private BoosterButton _target;
    private Canvas _targetCanvas;
    private bool _targetHadOverride;
    private int _targetOrder;
    private Tween _handTween;

    public void Show(BoosterButton target, string text)
    {
        gameObject.SetActive(true);
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
        if (bubble != null)
        {
            bubble.DOKill();
            bubble.localScale = Vector3.one * 0.9f;
            bubble.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(bubble.gameObject);
        }
        PlayHand(target);
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
