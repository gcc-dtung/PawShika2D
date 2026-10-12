using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Nút Đóng băng trong lúc đồng hồ đang đóng băng (theo video tham khảo): đĩa tối che icon, số giây còn lại màu xanh lá
    /// và vòng tiến trình quanh nút chạy ngược. Hết đóng băng thì thu nhỏ biến mất.
    /// Gắn lên GameObject nút Freeze (BoosterButton); phần hiển thị tự tạo trong Awake, không chắn bấm.
    /// </summary>
    public class FreezeButtonCountdown : MonoBehaviour
    {
        [SerializeField] private float discSize = 150f;
        [SerializeField] private float ringSize = 176f;
        [SerializeField] private Color discColor = new Color(0.08f, 0.07f, 0.16f, 0.88f);
        [SerializeField] private Color ringBackColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color ringColor = new Color(0.45f, 0.95f, 0.35f, 1f);
        [SerializeField] private Color numberColor = new Color(0.55f, 1f, 0.3f, 1f);
        [SerializeField] private float numberSize = 64f;

        private RectTransform _root;
        private Image _fill;
        private TMP_Text _number;
        private LevelTimer _timer;
        private Tween _pop;
        private int _shown = -1;
        private bool _on;

        private void Awake()
        {
            _root = NewRect("FreezeCountdown", transform);
            _root.sizeDelta = Vector2.one * ringSize;

            NewImage("Disc", ProceduralSprites.Disc, discColor, discSize);
            NewImage("RingBack", ProceduralSprites.Ring, ringBackColor, ringSize);
            _fill = NewImage("Ring", ProceduralSprites.Ring, ringColor, ringSize);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = false; // vòng hụt dần theo chiều kim đồng hồ ngược như kim đếm lùi

            RectTransform textRt = NewRect("Seconds", _root);
            textRt.sizeDelta = new Vector2(discSize, discSize);
            TextMeshProUGUI text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_Text sample = GetComponentInChildren<TMP_Text>(true);
            if (sample != null && sample != text)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
            }
            text.fontSize = numberSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = numberColor;
            text.raycastTarget = false;
            _number = text;

            _root.gameObject.SetActive(false);
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private Image NewImage(string name, Sprite sprite, Color color, float size)
        {
            RectTransform rt = NewRect(name, _root);
            rt.sizeDelta = Vector2.one * size;
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void OnEnable()
        {
            _timer = LevelTimer.Instance;
            if (_timer != null) _timer.onFreezeChanged.AddListener(OnFreezeChanged);
            SetOn(_timer != null && _timer.IsFrozen, true);
        }

        private void OnDisable()
        {
            if (_timer != null) _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            _pop?.Kill();
            _pop = null;
            _on = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void OnFreezeChanged(bool frozen) => SetOn(frozen, false);

        private void SetOn(bool on, bool instant)
        {
            if (on == _on && !instant) return;
            _on = on;
            _pop?.Kill();
            _root.SetAsLastSibling(); // trên icon, số lượt
            if (on)
            {
                _root.gameObject.SetActive(true);
                _shown = -1;
                Refresh();
                _root.localScale = instant ? Vector3.one : Vector3.zero;
                if (!instant) _pop = _root.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
            else if (_root.gameObject.activeSelf)
            {
                if (instant)
                {
                    _root.gameObject.SetActive(false);
                    return;
                }
                _pop = _root.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() => _root.gameObject.SetActive(false));
            }
        }

        private void Update()
        {
            if (_on) Refresh();
        }

        private void Refresh()
        {
            if (_timer == null) return;
            float total = Mathf.Max(0.01f, GameConfig.LivesEnergy.freezeSeconds);
            float left = _timer.FreezeLeft;
            _fill.fillAmount = Mathf.Clamp01(left / total);
            int seconds = Mathf.CeilToInt(left);
            if (seconds == _shown) return;
            _shown = seconds;
            _number.SetText("{0}", seconds);
        }
    }
}
