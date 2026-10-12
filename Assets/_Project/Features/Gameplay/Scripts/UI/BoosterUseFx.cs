using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Phản hồi khi dùng booster (BoosterManager.onBoosterUsed), khoảng 0.6 s:
    /// icon bay từ nút ra giữa màn hình, phóng to nảy (OutBack) trên chùm tia sáng xoay + chữ tên booster ("HINT!"),
    /// màn hình tối nhẹ; rồi icon lao tới mục tiêu (vùng Hint, ô Reveal vừa chạm, đồng hồ cho Freeze) và lóe sáng ở đó.
    /// Thời gian khớp BoosterManager.EffectDelay: tới nơi thì tác dụng mới hiện (mèo mờ Hint, khối Reveal rơi xuống).
    /// Trong lúc icon bay: khóa thao tác trên lưới (InputGate) nên đồng hồ cũng dừng, không mất giờ vì hiệu ứng.
    /// Đặt trên Canvas HUD; mọi Image / chữ tự tạo trong Awake, không chắn bấm. Chạy theo thời gian thực.
    /// </summary>
    public class BoosterUseFx : MonoBehaviour
    {
        private BoosterButton[] _buttons;
        private TimerUI _timerUI;
        private Camera _cam;
        private BoosterManager _boosterManager;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(BoosterManager boosterManager, BoosterButton[] buttons, TimerUI timerUI, Camera cam)
        {
            _boosterManager = boosterManager;
            _buttons = buttons;
            _timerUI = timerUI;
            _cam = cam;
        }

        [Header("Thời gian (tổng nên bằng BoosterManager.effectDelay)")]
        [SerializeField] private float popDuration = 0.42f;
        [SerializeField] private float holdDuration = 0.45f;
        [SerializeField] private float flyDuration = 0.32f;

        [Header("Hình")]
        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.3f;
        [SerializeField] private float iconSize = 150f;
        [SerializeField] private float centerScale = 2.3f;
        [SerializeField] private float raysSize = 620f;
        [SerializeField] private float raysSpin = 90f; // độ / giây
        [SerializeField] private Color raysColor = new Color(1f, 0.9f, 0.5f, 0.9f);
        [SerializeField] private float labelFontSize = 96f;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.7f, 1f);

        [Header("Object con (Editor dựng sẵn)")]
        [SerializeField] private RectTransform fxRoot;
        [SerializeField] private Image fxDim;
        [SerializeField] private Image fxRays;
        [SerializeField] private Image fxIcon;
        [SerializeField] private Image fxFlash;
        [SerializeField] private TMP_Text fxLabel;
        [SerializeField] private Canvas canvas;
        private Sequence _seq;
        private BoosterManager _boosters;

        private void Awake()
        {
            if (fxRoot == null || fxDim == null || fxRays == null || fxIcon == null || fxFlash == null || fxLabel == null)
            {
                Debug.LogError($"[BoosterUseFx] Thiếu object con trên {name} (chuột phải component -> Dựng sẵn object con)", this);
                enabled = false;
                return;
            }
            // Sprite sinh lúc chạy, không lưu được vào scene; màu / cỡ theo Inspector
            fxRays.sprite = ProceduralSprites.Rays;
            fxRays.color = raysColor;
            fxRays.rectTransform.sizeDelta = Vector2.one * raysSize;
            fxIcon.rectTransform.sizeDelta = Vector2.one * iconSize;
            fxFlash.sprite = ProceduralSprites.Dot;
            fxFlash.color = flashColor;
            fxFlash.rectTransform.sizeDelta = Vector2.one * iconSize;
            fxLabel.fontSize = labelFontSize;
            fxLabel.color = labelColor;
            fxRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            _boosters = _boosterManager;
            if (_boosters != null) _boosters.onBoosterUsed.AddListener(Play);
        }

        private void OnDestroy()
        {
            if (_boosters != null) _boosters.onBoosterUsed.RemoveListener(Play);
        }

        private void OnDisable()
        {
            _seq?.Kill(); // OnKill mở khóa thao tác
            _seq = null;
            if (fxRoot != null) fxRoot.gameObject.SetActive(false);
        }

        private void Play(BoosterType type)
        {
            if (type == BoosterType.Freeze) return; // Freeze có hiệu ứng riêng như bản gốc (FreezeOverlayFX: Spine + hạt)
            BoosterData data = _boosters != null ? _boosters.Get(type) : null;
            if (data == null) return;

            _seq?.Kill();
            fxRoot.gameObject.SetActive(true);
            fxRoot.SetAsLastSibling();
            InputGate.Lock(this);

            Vector2 start = ToLocal(ButtonScreenPoint(type), Vector2.zero);
            Vector2 target = ToLocal(TargetScreenPoint(type), Vector2.zero);

            RectTransform icon = fxIcon.rectTransform, rays = fxRays.rectTransform, label = fxLabel.rectTransform, flash = fxFlash.rectTransform;
            fxIcon.sprite = data.icon;
            fxIcon.color = Color.white;
            icon.anchoredPosition = start;
            icon.localScale = Vector3.one * 0.6f;
            rays.anchoredPosition = Vector2.zero;
            rays.localScale = Vector3.one * 0.5f;
            rays.localRotation = Quaternion.identity;
            fxRays.color = WithAlpha(raysColor, 0f);
            label.anchoredPosition = new Vector2(0f, -iconSize * centerScale * 0.62f);
            label.localScale = Vector3.zero;
            fxLabel.SetText((string.IsNullOrEmpty(data.name) ? type.ToString() : data.name).ToUpperInvariant() + "!");
            fxLabel.alpha = 1f;
            fxDim.color = WithAlpha(Color.black, 0f);
            fxFlash.color = WithAlpha(flashColor, 0f);

            float t1 = popDuration + holdDuration;
            float t2 = t1 + flyDuration;
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            // Bật ra giữa màn hình
            seq.Insert(0f, icon.DOAnchorPos(Vector2.zero, popDuration).SetEase(Ease.OutCubic));
            seq.Insert(0f, icon.DOScale(centerScale, popDuration).SetEase(Ease.OutBack));
            seq.Insert(0f, fxDim.DOFade(dimAlpha, popDuration * 0.6f));
            seq.Insert(popDuration * 0.3f, fxRays.DOFade(raysColor.a, popDuration * 0.7f));
            seq.Insert(popDuration * 0.3f, rays.DOScale(1f, popDuration * 0.9f).SetEase(Ease.OutBack));
            seq.Insert(popDuration * 0.45f, label.DOScale(1f, popDuration * 0.8f).SetEase(Ease.OutBack));

            // Lao tới mục tiêu
            seq.Insert(t1, icon.DOAnchorPos(target, flyDuration).SetEase(Ease.InCubic));
            seq.Insert(t1, icon.DOScale(0.7f, flyDuration).SetEase(Ease.InCubic));
            seq.Insert(t1, fxRays.DOFade(0f, flyDuration * 0.7f));
            seq.Insert(t1, label.DOScale(0f, flyDuration * 0.7f).SetEase(Ease.InBack));
            seq.Insert(t1, fxDim.DOFade(0f, flyDuration + 0.1f));

            // Tới nơi: icon tắt, lóe sáng tròn tại chỗ
            seq.InsertCallback(t2, () =>
            {
                InputGate.Unlock(this);
                fxIcon.color = WithAlpha(Color.white, 0f);
                flash.anchoredPosition = target;
                flash.localScale = Vector3.one * 0.3f;
                fxFlash.color = WithAlpha(flashColor, 0.9f);
                AudioManager.Vibrate(HapticType.Light);
            });
            seq.Insert(t2, flash.DOScale(2.4f, 0.28f).SetEase(Ease.OutQuad));
            seq.Insert(t2, fxFlash.DOFade(0f, 0.28f).SetEase(Ease.InQuad));

            seq.OnUpdate(() => rays.Rotate(0f, 0f, -raysSpin * Time.unscaledDeltaTime));
            seq.OnComplete(() => fxRoot.gameObject.SetActive(false));
            seq.OnKill(() =>
            {
                InputGate.Unlock(this);
                if (_seq == seq) _seq = null;
            });
            _seq = seq;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        // ---------- Vị trí ----------

        private Vector2? ButtonScreenPoint(BoosterType type)
        {
            BoosterButton button = BoosterButton.Find(_buttons, type);
            return button != null ? UiScreenPoint(button.transform) : (Vector2?)null;
        }

        /// <summary>Lần dùng có mục tiêu world (Hint: tâm vùng, Reveal: ô vừa chạm) thì bay tới đó; không thì Freeze -> đồng hồ.</summary>
        private Vector2? TargetScreenPoint(BoosterType type)
        {
            Vector3? world = _boosters != null ? _boosters.LastTarget : null;
            Camera cam = _cam;
            if (world.HasValue && cam != null) return cam.WorldToScreenPoint(world.Value);
            TimerUI timer = _timerUI;
            if (type == BoosterType.Freeze && timer != null) return UiScreenPoint(timer.transform);
            return null;
        }

        /// <summary>Nút booster / đồng hồ cùng Canvas HUD với hiệu ứng: dùng camera của Canvas đó.</summary>
        private Vector2 UiScreenPoint(Transform t)
        {
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(uiCam, t.position);
        }

        /// <summary>Điểm màn hình -> toạ độ trong fxRoot (null = giữa màn hình).</summary>
        private Vector2 ToLocal(Vector2? screen, Vector2 fallback)
        {
            if (!screen.HasValue) return fallback;
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(fxRoot, screen.Value, uiCam, out Vector2 local) ? local : fallback;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
        }
#endif

#if UNITY_EDITOR
        /// <summary>Editor: Dựng sẵn object con (thay cho tạo lúc chạy). Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            fxRoot = EditorChildren.Rect(transform, "BoosterFx", false);
            fxRoot.anchorMin = Vector2.zero;
            fxRoot.anchorMax = Vector2.one;
            fxRoot.offsetMin = fxRoot.offsetMax = Vector2.zero;

            fxDim = BuildImage("Dim", Color.black);
            RectTransform dimRt = fxDim.rectTransform;
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            fxRays = BuildImage("Rays", raysColor);
            fxIcon = BuildImage("Icon", Color.white);
            fxIcon.preserveAspect = true;
            fxFlash = BuildImage("Flash", flashColor);

            // Chữ tên booster: font của chữ có sẵn trên HUD (Fredoka) cho đồng bộ
            TextMeshProUGUI text = EditorChildren.Child<TextMeshProUGUI>(fxRoot, "Label");
            text.rectTransform.sizeDelta = new Vector2(900f, 160f);
            TMP_Text sample = null;
            foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == text || t.font == null) continue;
                sample = t;
                if (t.font.name.Contains("Fredoka")) break;
            }
            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
            }
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            fxLabel = text;
            canvas = GetComponentInParent<Canvas>();
        }

        private Image BuildImage(string imageName, Color color)
        {
            Image img = EditorChildren.Child<Image>(fxRoot, imageName);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
#endif
    }
}
