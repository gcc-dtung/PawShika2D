using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút phụ trên Home (Starter, No Ads, Daily): định kỳ icon nhấc lên, to ra một chút, lắc nghiêng vài nhịp rồi hạ xuống để mời bấm
/// (như bản gốc; khung tròn / ruy băng / chữ đứng yên). Bấm -> chuyển sang tab chỉ định (mặc định Shop; tab &lt; 0 = không chuyển).
/// SetAttention(false): thôi lắc, ẩn chấm đỏ báo (badge, tùy chọn) – vd Daily đã nhận quà hôm nay.
/// Icon xoay quanh pivot của nó (đặt ở đáy icon để lắc như đặt trên mặt phẳng).
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuShortcutButton : MonoBehaviour
{
    [SerializeField] private MainMenuNavigator navigator;
    [SerializeField] private int tab = 0;
    [SerializeField] private RectTransform icon;
    [SerializeField] private GameObject badge;

    [Header("Lắc mời bấm")]
    [Tooltip("Thời gian nghỉ giữa 2 lần lắc")]
    [SerializeField] private float interval = 1.4f;
    [Tooltip("Chờ trước lần lắc đầu (lệch pha giữa các nút)")]
    [SerializeField] private float firstDelay = 0.6f;
    [SerializeField] private float lift = 12f;
    [SerializeField] private float scaleUp = 0.06f;
    [Tooltip("Góc lắc lớn nhất (độ), nhỏ dần qua từng nhịp")]
    [SerializeField] private float angle = 9f;
    [SerializeField] private int swings = 4;
    [SerializeField] private float swingDuration = 0.08f;
    [SerializeField] private float liftDuration = 0.12f;

    private Sequence _anim;
    private Vector2 _iconPos;
    private bool _hasIconPos;
    private bool _attention = true;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        if (icon != null)
        {
            _iconPos = icon.anchoredPosition;
            _hasIconPos = true;
        }
    }

    private void OnEnable()
    {
        StartWiggle();
    }

    private void StartWiggle()
    {
        _anim?.Kill();
        _anim = null;
        if (icon == null || !_attention) return;
        ResetIcon();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.AppendInterval(firstDelay);
        seq.Append(Wiggle());
        seq.OnComplete(Loop);
        _anim = seq;
    }

    private void OnDisable()
    {
        _anim?.Kill();
        _anim = null;
        ResetIcon();
    }

    private void Loop()
    {
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.AppendInterval(interval);
        seq.Append(Wiggle());
        seq.SetLoops(-1);
        _anim = seq;
    }

    /// <summary>Nhấc + to ra -> lắc trái / phải nhỏ dần -> hạ xuống.</summary>
    private Sequence Wiggle()
    {
        Sequence s = DOTween.Sequence();
        s.Append(icon.DOAnchorPosY(_iconPos.y + lift, liftDuration).SetEase(Ease.OutQuad));
        s.Join(icon.DOScale(1f + scaleUp, liftDuration).SetEase(Ease.OutQuad));
        for (int i = 0; i < swings; i++)
        {
            float a = angle * (1f - (float)i / swings) * (i % 2 == 0 ? 1f : -1f);
            s.Append(icon.DOLocalRotate(new Vector3(0f, 0f, a), swingDuration).SetEase(Ease.InOutSine));
        }
        s.Append(icon.DOLocalRotate(Vector3.zero, swingDuration).SetEase(Ease.InOutSine));
        s.Append(icon.DOAnchorPosY(_iconPos.y, liftDuration * 1.2f).SetEase(Ease.OutBounce));
        s.Join(icon.DOScale(1f, liftDuration * 1.2f).SetEase(Ease.OutQuad));
        return s;
    }

    private void ResetIcon()
    {
        if (icon == null || !_hasIconPos) return;
        icon.anchoredPosition = _iconPos;
        icon.localScale = Vector3.one;
        icon.localRotation = Quaternion.identity;
    }

    /// <summary>Mời bấm (lắc định kỳ + chấm đỏ) hay đứng yên.</summary>
    public void SetAttention(bool on)
    {
        if (badge != null) badge.SetActive(on);
        if (_attention == on) return;
        _attention = on;
        if (!isActiveAndEnabled) return;
        if (on) StartWiggle();
        else
        {
            _anim?.Kill();
            _anim = null;
            ResetIcon();
        }
    }

    private void OnClick()
    {
        if (navigator != null && tab >= 0) navigator.Select(tab);
    }
}
