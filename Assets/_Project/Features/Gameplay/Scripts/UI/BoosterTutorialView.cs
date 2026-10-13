using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Giao diện tutorial mở khóa booster (Canvas_HUD/BoosterTutorial, Canvas riêng sortingOrder 5):
    /// nền tối chặn mọi thao tác, khung lời nhắc trên cùng, bàn tay nhấn liên tục vào nút booster.
    /// Nút booster được "nhấc" lên trên nền tối bằng Canvas override sorting (Canvas có sẵn trên nút) và bật quầng sáng.
    /// Thứ tự: nền tối (order nền) < nút (nền + 1) < bàn tay (Canvas riêng, nền + 2).
    /// Có spotlightSprite (CircleMask, như TutorialBoosterUI/CircleMask(F) gốc): nền tối thành đèn rọi, khoét 1 vòng tròn
    /// mờ viền quanh nút (ảnh CircleMask đặt tại nút + 4 dải tối cùng màu phủ phần còn lại, Editor dựng sẵn, cùng chặn chạm).
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

        [Header("Object con (Editor dựng sẵn)")]
        [SerializeField] private RectTransform dim;
        [SerializeField] private Image dimImage;
        [Tooltip("Ảnh đèn rọi + 4 dải tối (trái, phải, trên, dưới), con của Dim, tắt tới lần đầu dùng")]
        [SerializeField] private Image spot;
        [SerializeField] private Image[] sides = new Image[4];
        [SerializeField] private Canvas handCanvas;

        private float _bubbleY = float.NaN; // vị trí dựng sẵn (đè khung LEVEL)
        private bool _spotReady;

        private BoosterButton _target;
        private Canvas _targetCanvas;
        private bool _targetHadOverride;
        private int _targetOrder;
        private Tween _handTween;

        /// <summary>
        /// Chỉ khung lời nhắc, không nền tối / bàn tay / nút: lưới vẫn kéo được (bước 2 tutorial Đóng băng: FreezeTutorialDemo).
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
            if (dim != null) dim.gameObject.SetActive(on);
        }

        /// <summary>
        /// Đèn rọi: ảnh CircleMask cạnh = nút x spotlightScale đặt tại nút, 4 dải tối phủ trên / dưới / trái / phải.
        /// Tất cả cùng màu Dim (ảnh đen x alpha Dim = dải tối) nên không thấy đường nối; Image của Dim tắt để không tối 2 lớp.
        /// </summary>
        private void PlaceSpotlight(RectTransform target)
        {
            if (dim == null || dimImage == null || spotlightSprite == null || spot == null || sides == null || sides.Length < 4) return;
            if (!_spotReady) ReadySpotlight();

            Canvas.ForceUpdateCanvases();
            Vector2 center = dim.InverseTransformPoint(target.TransformPoint(target.rect.center));
            Vector2 size = target.rect.size * (target.lossyScale.x / dim.lossyScale.x);
            float half = Mathf.Max(size.x, size.y) * spotlightScale * 0.5f;

            Rect r = dim.rect;
            // Các mảnh chung mép đúng bằng nhau (không chồng): chồng dù nửa pixel cũng thành vệt tối 2 lớp
            float left = center.x - half, right = center.x + half, bottom = center.y - half, top = center.y + half;
            SetSide(spot.rectTransform, left, bottom, right, top);
            SetSide(sides[0].rectTransform, r.xMin, r.yMin, left, r.yMax);    // trái (cả chiều cao)
            SetSide(sides[1].rectTransform, right, r.yMin, r.xMax, r.yMax);   // phải
            SetSide(sides[2].rectTransform, left, top, right, r.yMax);        // trên (giữa trái / phải)
            SetSide(sides[3].rectTransform, left, r.yMin, right, bottom);     // dưới
        }

        /// <summary>Lần đầu dùng đèn rọi: các mảnh lấy màu Dim, bật lên; Image của Dim tắt để không tối 2 lớp.</summary>
        private void ReadySpotlight()
        {
            _spotReady = true;
            Color color = dimImage.color;
            dimImage.enabled = false;
            ReadyPiece(spot, color).sprite = spotlightSprite;
            foreach (Image side in sides) ReadyPiece(side, color);
        }

        private static Image ReadyPiece(Image piece, Color color)
        {
            piece.color = color; // raycastTarget bật: chặn chạm như Dim cũ
            piece.gameObject.SetActive(true);
            return piece;
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
                _targetCanvas = _target.OverlayCanvas;
                if (_targetCanvas == null) return;
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
            if (handCanvas != null)
            {
                handCanvas.overrideSorting = true;
                handCanvas.sortingOrder = (canvas != null ? canvas.sortingOrder : 5) + 2;
            }
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

#if UNITY_EDITOR
        /// <summary>Editor: nền tối, đèn rọi, Canvas của bàn tay. Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            Transform d = transform.Find("Dim");
            if (d == null) return;
            dim = (RectTransform)d;
            dimImage = dim.GetComponent<Image>();
            spot = BuildPiece("Spot");
            string[] names = { "Left", "Right", "Top", "Bottom" };
            sides = new Image[names.Length];
            for (int i = 0; i < names.Length; i++) sides[i] = BuildPiece(names[i]);
            if (hand != null) handCanvas = EditorChildren.On<Canvas>(hand.gameObject);
        }

        private Image BuildPiece(string pieceName)
        {
            Image img = EditorChildren.Child<Image>(dim, pieceName, false);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            return img;
        }
#endif
    }
}
