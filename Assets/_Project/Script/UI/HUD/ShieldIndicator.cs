using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Khiên đang bật: lớp băng (ice_effect) phủ hàng tim, thở nhẹ. Khiên chặn 1 lần sai: băng phồng to rồi vỡ (mờ đi).
/// Gắn vào Image phủ lên hàng tim; không chắn bấm.
/// </summary>
[RequireComponent(typeof(Image))]
public class ShieldIndicator : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float alpha = 0.85f;
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private float breakDuration = 0.35f;

    private Image _image;
    private Tween _anim;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.raycastTarget = false;
        SetVisible(0f);
    }

    private void OnEnable()
    {
        LivesManager lives = LivesManager.Instance;
        if (lives == null) return;
        lives.onShieldChanged.AddListener(OnShieldChanged);
        lives.onShieldBlocked.AddListener(OnBlocked);
        if (lives.HasShield) Appear();
    }

    private void OnDisable()
    {
        LivesManager lives = LivesManager.Instance;
        if (lives != null)
        {
            lives.onShieldChanged.RemoveListener(OnShieldChanged);
            lives.onShieldBlocked.RemoveListener(OnBlocked);
        }
        Kill();
        SetVisible(0f);
    }

    private void OnShieldChanged(bool on)
    {
        // Tắt: mờ đi. Khi khiên chặn lỗi, LivesManager báo tắt trước rồi onShieldBlocked -> hiệu ứng vỡ đè lên
        if (on) Appear();
        else Hide();
    }

    private void Appear()
    {
        Kill();
        transform.localScale = Vector3.one * 0.6f;
        SetVisible(0f);

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(_image.DOFade(alpha, appearDuration));
        seq.Join(transform.DOScale(1f, appearDuration).SetEase(Ease.OutBack));
        // Thở nhẹ khi đang bật (tween lặp vô hạn không lồng được trong Sequence nên chạy riêng)
        seq.OnComplete(() => Track(transform.DOScale(1.05f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject)));
        Track(seq);
    }

    private void OnBlocked()
    {
        Kill();
        SetVisible(alpha);
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(transform.DOScale(1.4f, breakDuration).SetEase(Ease.OutQuad));
        seq.Join(_image.DOFade(0f, breakDuration));
        Track(seq);
    }

    private void Hide()
    {
        Kill();
        Track(_image.DOFade(0f, 0.15f).SetLink(gameObject));
    }

    private void Track(Tween tween)
    {
        tween.OnKill(() => { if (_anim == tween) _anim = null; });
        _anim = tween;
    }

    private void Kill()
    {
        _anim?.Kill();
        _anim = null;
        transform.localScale = Vector3.one;
    }

    private void SetVisible(float a)
    {
        Color c = _image.color;
        c.a = a;
        _image.color = c;
    }
}
