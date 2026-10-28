using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Cảm giác nhấn nút (chỉ khi nút đang tương tác được), như bản gốc:
    /// - Nút có khung + mặt riêng (con tên "Face" / "Fill", vd PLAY, Refill): khung (ảnh của chính nút) đứng yên,
    ///   mọi thứ bên trong (mặt + chữ + icon) lún nhỏ lại quanh tâm nút và tối đi -> lộ viền tối quanh mặt như bị nhấn xuống.
    ///   Button đang tô khung (ColorTint) thì chuyển sang tô mặt để khung không tối.
    /// - Nút 1 ảnh: cả nút lún nhỏ và tối đi (tween scale khác trên nút, vd booster nảy, được tạm dừng rồi chạy tiếp).
    /// Thả tay: nảy nhẹ về (OutBack); kéo ra ngoài nút / đang kéo (vuốt): về luôn, không nảy.
    /// UIButtonPress trên Canvas: Editor gắn sẵn cho mọi Button. Nút không muốn có phản hồi: bỏ tick ButtonPress.
    /// Tham chiếu (Selectable, mặt, Graphic được tô, con đứng yên) do Editor thu sẵn (OnValidate / UIAutoSetup), lúc chạy không tìm.
    /// Chạy theo thời gian thực nên không phụ thuộc timeScale.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly string[] FaceNames = { "Face", "Fill" };

        [Tooltip("Mặt nút (trống: Editor tự tìm con tên Face / Fill). Có mặt thì khung đứng yên, phần bên trong lún xuống")]
        [SerializeField] private RectTransform face;

        [Header("Editor tự điền")]
        [SerializeField] private Selectable selectable;
        [SerializeField] private Graphic faceGraphic;
        [Tooltip("Graphic con được tô tối khi nhấn (trừ phần dưới ButtonPressIgnore)")]
        [SerializeField] private Graphic[] tintTargets;
        [Tooltip("Con trực tiếp có ButtonPressIgnore: khung đứng yên")]
        [SerializeField] private Transform[] fixedChildren;

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

        private bool _inset;
        private readonly List<RectTransform> _parts = new List<RectTransform>();   // phần lún (con trực tiếp, chế độ khung)
        private readonly List<Vector2> _partPos = new List<Vector2>();
        private readonly List<Vector3> _partScale = new List<Vector3>();
        private readonly List<Graphic> _tinted = new List<Graphic>();
        private Tween _tintTween;
        private float _tintT; // 0 = trắng, 1 = pressedTint
        private Sequence _tween;
        private Vector3 _baseScale;
        private List<Tween> _paused; // tween scale khác trên nút, tạm dừng trong lúc nhấn
        private bool _active;        // đang lún / về: kích thước gốc đã lưu
        private bool _pressed;

        /// <summary>Đang lún / về (script khác muốn tween scale nút thì nên bỏ qua lúc này).</summary>
        public bool IsPlaying => _active;

        private void Awake()
        {
            _inset = face != null;

            // Khung đứng yên: Button đang tô khung thì chuyển sang tô mặt (cả màu nhấn lẫn màu tắt)
            if (_inset && selectable != null && selectable.transition == Selectable.Transition.ColorTint
                && selectable.targetGraphic != null && selectable.targetGraphic.transform == transform)
            {
                if (faceGraphic != null) selectable.targetGraphic = faceGraphic;
            }
        }

        private void OnDisable()
        {
            _pressed = false;
            ClearTint();
            if (!_active) return;
            _tween?.Kill();
            RestoreInstant();
            End();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (selectable != null && !selectable.IsInteractable()) return;

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
                    if (fixedChildren != null && System.Array.IndexOf(fixedChildren, child) >= 0) continue; // khung đứng yên
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
        /// Tween chạy trên nút (không dùng Graphic.CrossFadeColor): CrossFade là coroutine trên chính hình con, hình con bị tắt / bật lại
        /// giữa lúc đang sáng dần (vd bấm booster: icon đổi sang đếm giây, ô số vẽ lại) thì dừng giữa chừng -> kẹt màu tối.
        /// </summary>
        private void Tint(bool on, bool instant = false)
        {
            if (!darken) return;
            if (on)
            {
                _tinted.Clear();
                Graphic selfTinted = selectable != null && selectable.transition == Selectable.Transition.ColorTint
                    ? selectable.targetGraphic : null;
                if (tintTargets != null)
                {
                    foreach (Graphic g in tintTargets)
                    {
                        if (g == null || !g.gameObject.activeInHierarchy || g == selfTinted) continue;
                        if (_inset && g.transform == transform) continue; // khung
                        _tinted.Add(g);
                    }
                }
            }

            _tintTween?.Kill();
            _tintTween = null;
            float to = on ? 1f : 0f;
            if (instant)
            {
                ApplyTint(to);
                if (!on) _tinted.Clear();
                return;
            }
            _tintTween = DOTween.To(() => _tintT, ApplyTint, to, pressDuration)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => { if (!on) _tinted.Clear(); });
        }

        private void ApplyTint(float t)
        {
            _tintT = t;
            Color c = Color.Lerp(Color.white, pressedTint, t);
            foreach (Graphic g in _tinted)
            {
                if (g == null) continue;
                c.a = g.canvasRenderer.GetAlpha(); // chỉ tô màu, giữ alpha
                g.canvasRenderer.SetColor(c);
            }
        }

        /// <summary>
        /// Trả màu ngay cho mọi hình có thể đã bị tô. Bấm nút làm ẩn màn (vd PLAY) thì nút tắt giữa lúc đang sáng lại dần
        /// (CrossFade dừng theo object) mà danh sách _tinted đã xoá lúc thả tay -> chữ kẹt màu tối khi quay lại.
        /// </summary>
        private void ClearTint()
        {
            _tintTween?.Kill();
            _tintTween = null;
            _tintT = 0f;
            _tinted.Clear();
            if (!darken || tintTargets == null) return;
            Graphic selfTinted = selectable != null && selectable.transition == Selectable.Transition.ColorTint
                ? selectable.targetGraphic : null;
            foreach (Graphic g in tintTargets)
            {
                if (g == null || g == selfTinted) continue;
                Color c = Color.white;
                c.a = g.canvasRenderer.GetAlpha(); // chỉ trả màu, giữ alpha (hình có thể đang mờ dần vì lý do khác)
                g.canvasRenderer.SetColor(c);
            }
        }

        /// <summary>Thả tay còn trên nút (giống điều kiện Button nhận click).</summary>
        private bool IsOver(PointerEventData eventData)
        {
            GameObject over = eventData.pointerCurrentRaycast.gameObject;
            return over != null && over.transform.IsChildOf(transform);
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent / Find)
        private void OnValidate() => EditorCollect();

        /// <summary>Thu lại tham chiếu theo cây con hiện tại; trả về true nếu có đổi.</summary>
        public bool EditorCollect()
        {
            Selectable sel = GetComponent<Selectable>();
            RectTransform f = face;
            if (f == null)
            {
                foreach (string n in FaceNames)
                {
                    if (transform.Find(n) is RectTransform found) { f = found; break; }
                }
            }
            Graphic fg = f != null ? f.GetComponent<Graphic>() : null;
            var tint = new List<Graphic>();
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                if (g.GetComponentInParent<ButtonPressIgnore>(true) != null) continue;
                tint.Add(g);
            }
            var fixedList = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.GetComponent<ButtonPressIgnore>() != null) fixedList.Add(child);
            }
            bool changed = sel != selectable || f != face || fg != faceGraphic
                || !Same(tintTargets, tint) || !Same(fixedChildren, fixedList);
            selectable = sel;
            face = f;
            faceGraphic = fg;
            tintTargets = tint.ToArray();
            fixedChildren = fixedList.ToArray();
            return changed;
        }

        private static bool Same<T>(T[] a, List<T> b) where T : Object
        {
            if (a == null || a.Length != b.Count) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
#endif
    }
}
