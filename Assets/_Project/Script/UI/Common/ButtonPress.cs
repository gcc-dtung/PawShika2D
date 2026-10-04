using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cảm giác nhấn nút (chỉ khi nút đang tương tác được), như bản gốc:
/// - Nút có khung + mặt riêng (con tên "Face" / "Fill", vd PLAY, Refill): khung (ảnh của chính nút) đứng yên,
///   mọi thứ bên trong (mặt + chữ + icon) lún nhỏ lại quanh tâm nút và tối đi -> lộ viền tối quanh mặt như bị nhấn xuống.
///   Button đang tô khung (ColorTint) thì chuyển sang tô mặt để khung không tối.
/// - Nút 1 ảnh: cả nút lún nhỏ và tối đi (tween scale khác trên nút, vd booster nảy, được tạm dừng rồi chạy tiếp).
/// Thả tay: nảy nhẹ về (OutBack); kéo ra ngoài nút / đang kéo (vuốt): về luôn, không nảy.
/// UIButtonPress trên Canvas tự gắn cho mọi Button. Nút không muốn có phản hồi: gắn sẵn ButtonPress rồi bỏ tick.
/// Chạy theo thời gian thực nên không phụ thuộc timeScale.
/// </summary>
[DisallowMultipleComponent]
public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly string[] FaceNames = { "Face", "Fill" };

    [Tooltip("Mặt nút (trống: tự tìm con tên Face / Fill). Có mặt thì khung đứng yên, phần bên trong lún xuống")]
    [SerializeField] private RectTransform face;

    [Header("Nhấn xuống")]
    [SerializeField] private float pressScale = 0.93f;      // nút 1 ảnh
    [SerializeField] private float insetScale = 0.94f;      // phần bên trong khung
    [SerializeField] private float pressDuration = 0.07f;
    [SerializeField] private bool darken = true;
    [SerializeField] private Color pressedTint = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Thả tay")]
    [SerializeField] private float releaseDuration = 0.18f;
    [Tooltip("Độ nảy khi về (OutBack overshoot)")]
    [SerializeField] private float releaseOvershoot = 3f;

    private Selectable _selectable;
    private bool _inset;
    private readonly List<RectTransform> _parts = new List<RectTransform>();   // phần lún (con trực tiếp, chế độ khung)
    private readonly List<Vector2> _partPos = new List<Vector2>();
    private readonly List<Vector3> _partScale = new List<Vector3>();
    private readonly List<Graphic> _tinted = new List<Graphic>();
    private Sequence _tween;
    private Vector3 _baseScale;
    private List<Tween> _paused; // tween scale khác trên nút, tạm dừng trong lúc nhấn
    private bool _active;        // đang lún / về: kích thước gốc đã lưu
    private bool _pressed;

    /// <summary>Đang lún / về (script khác muốn tween scale nút thì nên bỏ qua lúc này).</summary>
    public bool IsPlaying => _active;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        if (face == null)
        {
            foreach (string n in FaceNames)
            {
                if (transform.Find(n) is RectTransform f) { face = f; break; }
            }
        }
        _inset = face != null;

        // Khung đứng yên: Button đang tô khung thì chuyển sang tô mặt (cả màu nhấn lẫn màu tắt)
        if (_inset && _selectable != null && _selectable.transition == Selectable.Transition.ColorTint
            && _selectable.targetGraphic != null && _selectable.targetGraphic.transform == transform)
        {
            Graphic faceGraphic = face.GetComponent<Graphic>();
            if (faceGraphic != null) _selectable.targetGraphic = faceGraphic;
        }
    }

    private void OnDisable()
    {
        _pressed = false;
        Tint(false, true);
        if (!_active) return;
        _tween?.Kill();
        RestoreInstant();
        End();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_selectable != null && !_selectable.IsInteractable()) return;

        _pressed = true;
        Begin();
        Press(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed) return;
        _pressed = false;
        // Kéo (vd vuốt chuyển tab) thì EventSystem đã hủy click: về luôn, không nảy
        Release(!eventData.dragging && IsOver(eventData));
    }

    // Giữ tay rồi kéo ra ngoài nút: nổi lên lại; kéo vào lại: lún xuống (giống Button)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pressed && _active) Release(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_pressed && !eventData.dragging)
        {
            Begin();
            Press(true);
        }
    }

    // ---------- Lún / về ----------

    private void Press(bool on)
    {
        Tint(on);
        Sequence seq = NewTween();
        if (_inset)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] == null) continue;
                seq.Join(_parts[i].DOScale(_partScale[i] * insetScale, pressDuration).SetEase(Ease.OutQuad));
                seq.Join(_parts[i].DOAnchorPos(_partPos[i] * insetScale, pressDuration).SetEase(Ease.OutQuad));
            }
        }
        else
        {
            seq.Join(transform.DOScale(_baseScale * pressScale, pressDuration).SetEase(Ease.OutQuad));
        }
    }

    private void Release(bool bounce)
    {
        Tint(false);
        Sequence seq = NewTween();
        float d = releaseDuration;
        Ease ease = bounce ? Ease.OutBack : Ease.OutQuad;
        if (_inset)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] == null) continue;
                seq.Join(_parts[i].DOScale(_partScale[i], d).SetEase(ease, releaseOvershoot));
                seq.Join(_parts[i].DOAnchorPos(_partPos[i], d).SetEase(ease, releaseOvershoot));
            }
        }
        else
        {
            seq.Join(transform.DOScale(_baseScale, d).SetEase(ease, releaseOvershoot));
        }
        seq.OnComplete(() => { if (!_pressed) End(); });
    }

    private Sequence NewTween()
    {
        _tween?.Kill();
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.OnKill(() => { if (_tween == seq) _tween = null; });
        _tween = seq;
        return seq;
    }

    /// <summary>Lưu trạng thái gốc khi bắt đầu nhấn (nhấn lại lúc đang về thì giữ gốc cũ).</summary>
    private void Begin()
    {
        _tween?.Kill();
        if (_active) return;
        _active = true;

        if (_inset)
        {
            _parts.Clear();
            _partPos.Clear();
            _partScale.Clear();
            foreach (Transform child in transform)
            {
                if (!(child is RectTransform rt) || !child.gameObject.activeSelf) continue;
                if (child.GetComponent<ButtonPressIgnore>() != null) continue; // khung đứng yên
                _parts.Add(rt);
                _partPos.Add(rt.anchoredPosition);
                _partScale.Add(rt.localScale);
            }
            return;
        }

        _baseScale = transform.localScale;
        _paused = DOTween.TweensByTarget(transform, true);
        if (_paused == null) return;
        foreach (Tween t in _paused) t.Pause();
    }

    private void RestoreInstant()
    {
        if (_inset)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] == null) continue;
                _parts[i].localScale = _partScale[i];
                _parts[i].anchoredPosition = _partPos[i];
            }
        }
        else
        {
            transform.localScale = _baseScale;
        }
    }

    private void End()
    {
        _active = false;
        if (_paused == null) return;
        foreach (Tween t in _paused)
        {
            if (t.IsActive()) t.Play();
        }
        _paused = null;
    }

    /// <summary>
    /// Tô / bỏ tô. Khung đứng yên: chỉ tô phần bên trong. Nút 1 ảnh: tô cả nút.
    /// Hình mà Button tự tô (targetGraphic, ColorTint) thì để Button lo.
    /// </summary>
    private void Tint(bool on, bool instant = false)
    {
        if (!darken) return;
        if (on)
        {
            _tinted.Clear();
            Graphic selfTinted = _selectable != null && _selectable.transition == Selectable.Transition.ColorTint
                ? _selectable.targetGraphic : null;
            foreach (Graphic g in GetComponentsInChildren<Graphic>())
            {
                if (g == selfTinted) continue;
                if (_inset && g.transform == transform) continue; // khung
                if (g.GetComponentInParent<ButtonPressIgnore>() != null) continue;
                _tinted.Add(g);
            }
        }

        Color c = on ? pressedTint : Color.white;
        float d = instant ? 0f : pressDuration;
        foreach (Graphic g in _tinted)
        {
            if (g != null) g.CrossFadeColor(c, d, true, true);
        }
        if (!on) _tinted.Clear();
    }

    /// <summary>Thả tay còn trên nút (giống điều kiện Button nhận click).</summary>
    private bool IsOver(PointerEventData eventData)
    {
        GameObject over = eventData.pointerCurrentRaycast.gameObject;
        return over != null && over.transform.IsChildOf(transform);
    }
}
