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

        private RectTransform _root;
        private Image _dim, _rays, _icon, _flash;
        private TMP_Text _label;
        private Sequence _seq;
        private BoosterManager _boosters;

        private void Awake()
        {
            _root = NewRect("BoosterFx", transform);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            _dim = NewImage("Dim", null, Color.black);
            RectTransform dimRt = _dim.rectTransform;
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;

            _rays = NewImage("Rays", ProceduralSprites.Rays, raysColor);
            _rays.rectTransform.sizeDelta = Vector2.one * raysSize;
            _icon = NewImage("Icon", null, Color.white);
            _icon.rectTransform.sizeDelta = Vector2.one * iconSize;
            _icon.preserveAspect = true;
            _flash = NewImage("Flash", ProceduralSprites.Dot, flashColor);
            _flash.rectTransform.sizeDelta = Vector2.one * iconSize;

            _label = CreateLabel();
            _root.gameObject.SetActive(false);
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private Image NewImage(string name, Sprite sprite, Color color)
        {
            RectTransform rt = NewRect(name, _root);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Chữ tên booster: lấy font của chữ có sẵn trên HUD (Fredoka) cho đồng bộ.</summary>
        private TMP_Text CreateLabel()
        {
            RectTransform rt = NewRect("Label", _root);
            rt.sizeDelta = new Vector2(900f, 160f);
            TextMeshProUGUI label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_Text sample = null;
            foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == label || t.font == null) continue;
                sample = t;
                if (t.font.name.Contains("Fredoka")) break;
            }
            if (sample != null)
            {
                label.font = sample.font;
                label.fontSharedMaterial = sample.fontSharedMaterial;
            }
            label.fontSize = labelFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.color = labelColor;
            label.raycastTarget = false;
            return label;
        }

        private void Start()
        {
            _boosters = BoosterManager.Instance;
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
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Play(BoosterType type)
        {
            if (type == BoosterType.Freeze) return; // Freeze có hiệu ứng riêng như bản gốc (FreezeOverlayFX: Spine + hạt)
            BoosterData data = _boosters != null ? _boosters.Get(type) : null;
            if (data == null) return;

            _seq?.Kill();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            InputGate.Lock(this);

            Vector2 start = ToLocal(ButtonScreenPoint(type), Vector2.zero);
            Vector2 target = ToLocal(TargetScreenPoint(type), Vector2.zero);

            RectTransform icon = _icon.rectTransform, rays = _rays.rectTransform, label = _label.rectTransform, flash = _flash.rectTransform;
            _icon.sprite = data.icon;
            _icon.color = Color.white;
            icon.anchoredPosition = start;
            icon.localScale = Vector3.one * 0.6f;
            rays.anchoredPosition = Vector2.zero;
            rays.localScale = Vector3.one * 0.5f;
            rays.localRotation = Quaternion.identity;
            _rays.color = WithAlpha(raysColor, 0f);
            label.anchoredPosition = new Vector2(0f, -iconSize * centerScale * 0.62f);
            label.localScale = Vector3.zero;
            _label.SetText((string.IsNullOrEmpty(data.name) ? type.ToString() : data.name).ToUpperInvariant() + "!");
            _label.alpha = 1f;
            _dim.color = WithAlpha(Color.black, 0f);
            _flash.color = WithAlpha(flashColor, 0f);

            float t1 = popDuration + holdDuration;
            float t2 = t1 + flyDuration;
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            // Bật ra giữa màn hình
            seq.Insert(0f, icon.DOAnchorPos(Vector2.zero, popDuration).SetEase(Ease.OutCubic));
            seq.Insert(0f, icon.DOScale(centerScale, popDuration).SetEase(Ease.OutBack));
            seq.Insert(0f, _dim.DOFade(dimAlpha, popDuration * 0.6f));
            seq.Insert(popDuration * 0.3f, _rays.DOFade(raysColor.a, popDuration * 0.7f));
            seq.Insert(popDuration * 0.3f, rays.DOScale(1f, popDuration * 0.9f).SetEase(Ease.OutBack));
            seq.Insert(popDuration * 0.45f, label.DOScale(1f, popDuration * 0.8f).SetEase(Ease.OutBack));

            // Lao tới mục tiêu
            seq.Insert(t1, icon.DOAnchorPos(target, flyDuration).SetEase(Ease.InCubic));
            seq.Insert(t1, icon.DOScale(0.7f, flyDuration).SetEase(Ease.InCubic));
            seq.Insert(t1, _rays.DOFade(0f, flyDuration * 0.7f));
            seq.Insert(t1, label.DOScale(0f, flyDuration * 0.7f).SetEase(Ease.InBack));
            seq.Insert(t1, _dim.DOFade(0f, flyDuration + 0.1f));

            // Tới nơi: icon tắt, lóe sáng tròn tại chỗ
            seq.InsertCallback(t2, () =>
            {
                InputGate.Unlock(this);
                _icon.color = WithAlpha(Color.white, 0f);
                flash.anchoredPosition = target;
                flash.localScale = Vector3.one * 0.3f;
                _flash.color = WithAlpha(flashColor, 0.9f);
                AudioManager.Vibrate(HapticType.Light);
            });
            seq.Insert(t2, flash.DOScale(2.4f, 0.28f).SetEase(Ease.OutQuad));
            seq.Insert(t2, _flash.DOFade(0f, 0.28f).SetEase(Ease.InQuad));

            seq.OnUpdate(() => rays.Rotate(0f, 0f, -raysSpin * Time.unscaledDeltaTime));
            seq.OnComplete(() => _root.gameObject.SetActive(false));
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
            BoosterButton button = BoosterButton.Find(type);
            return button != null ? UiScreenPoint(button.transform) : (Vector2?)null;
        }

        /// <summary>Lần dùng có mục tiêu world (Hint: tâm vùng, Reveal: ô vừa chạm) thì bay tới đó; không thì Freeze -> đồng hồ.</summary>
        private Vector2? TargetScreenPoint(BoosterType type)
        {
            Vector3? world = _boosters != null ? _boosters.LastTarget : null;
            Camera cam = Camera.main;
            if (world.HasValue && cam != null) return cam.WorldToScreenPoint(world.Value);
            TimerUI timer = FindFirstObjectByType<TimerUI>();
            if (type == BoosterType.Freeze && timer != null) return UiScreenPoint(timer.transform);
            return null;
        }

        private static Vector2 UiScreenPoint(Transform t)
        {
            Canvas canvas = t.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(uiCam, t.position);
        }

        /// <summary>Điểm màn hình -> toạ độ trong _root (null = giữa màn hình).</summary>
        private Vector2 ToLocal(Vector2? screen, Vector2 fallback)
        {
            if (!screen.HasValue) return fallback;
            Canvas canvas = _root.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen.Value, uiCam, out Vector2 local) ? local : fallback;
        }
    }
}
