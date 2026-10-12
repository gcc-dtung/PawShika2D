using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Game;
using PawBlock.Features.Economy;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Nút Đóng băng trong lúc đồng hồ đang đóng băng (theo video tham khảo): đĩa tối che icon, số giây còn lại màu xanh lá
    /// và vòng tiến trình quanh nút chạy ngược. Hết đóng băng thì thu nhỏ biến mất.
    /// Gắn lên GameObject nút Freeze (BoosterButton); phần hiển thị tự tạo trong Awake, không chắn bấm.
    /// </summary>
    public class FreezeButtonCountdown : MonoBehaviour
    {
        private LivesEnergyConfig _lives;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelTimer timer, LivesEnergyConfig lives)
        {
            _timer = timer;
            _lives = lives;
        }

        [SerializeField] private float discSize = 150f;
        [SerializeField] private float ringSize = 176f;
        [SerializeField] private Color discColor = new Color(0.08f, 0.07f, 0.16f, 0.88f);
        [SerializeField] private Color ringBackColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color ringColor = new Color(0.45f, 0.95f, 0.35f, 1f);
        [SerializeField] private Color numberColor = new Color(0.55f, 1f, 0.3f, 1f);
        [SerializeField] private float numberSize = 64f;

        [Header("Object con (Editor dựng sẵn)")]
        [SerializeField] private RectTransform countdownRoot;
        [SerializeField] private Image disc;
        [SerializeField] private Image ringBack;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text number;
        private LevelTimer _timer;
        private Tween _pop;
        private int _shown = -1;
        private bool _on;

        private void Awake()
        {
            if (countdownRoot == null || disc == null || ringBack == null || fill == null || number == null)
            {
                Debug.LogError($"[FreezeButtonCountdown] Thiếu object con trên {name} (chuột phải component -> Dựng sẵn object con)", this);
                enabled = false;
                return;
            }
            // Sprite sinh lúc chạy, không lưu được vào scene; màu / cỡ theo Inspector
            countdownRoot.sizeDelta = Vector2.one * ringSize;
            SetImage(disc, ProceduralSprites.Disc, discColor, discSize);
            SetImage(ringBack, ProceduralSprites.Ring, ringBackColor, ringSize);
            SetImage(fill, ProceduralSprites.Ring, ringColor, ringSize);
            number.rectTransform.sizeDelta = new Vector2(discSize, discSize);
            number.fontSize = numberSize;
            number.color = numberColor;
            countdownRoot.gameObject.SetActive(false);
        }

        private static void SetImage(Image img, Sprite sprite, Color color, float size)
        {
            img.sprite = sprite;
            img.color = color;
            img.rectTransform.sizeDelta = Vector2.one * size;
        }

        private void OnEnable()
        {
            if (_timer != null) _timer.onFreezeChanged.AddListener(OnFreezeChanged);
            SetOn(_timer != null && _timer.IsFrozen, true);
        }

        private void OnDisable()
        {
            if (_timer != null) _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            _pop?.Kill();
            _pop = null;
            _on = false;
            if (countdownRoot != null) countdownRoot.gameObject.SetActive(false);
        }

        private void OnFreezeChanged(bool frozen) => SetOn(frozen, false);

        private void SetOn(bool on, bool instant)
        {
            if (on == _on && !instant) return;
            _on = on;
            _pop?.Kill();
            countdownRoot.SetAsLastSibling(); // trên icon, số lượt
            if (on)
            {
                countdownRoot.gameObject.SetActive(true);
                _shown = -1;
                Refresh();
                countdownRoot.localScale = instant ? Vector3.one : Vector3.zero;
                if (!instant) _pop = countdownRoot.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
            else if (countdownRoot.gameObject.activeSelf)
            {
                if (instant)
                {
                    countdownRoot.gameObject.SetActive(false);
                    return;
                }
                _pop = countdownRoot.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() => countdownRoot.gameObject.SetActive(false));
            }
        }

        private void Update()
        {
            if (_on) Refresh();
        }

        private void Refresh()
        {
            if (_timer == null) return;
            float total = Mathf.Max(0.01f, _lives.freezeSeconds);
            float left = _timer.FreezeLeft;
            fill.fillAmount = Mathf.Clamp01(left / total);
            int seconds = Mathf.CeilToInt(left);
            if (seconds == _shown) return;
            _shown = seconds;
            number.SetText("{0}", seconds);
        }

#if UNITY_EDITOR
        /// <summary>Editor: Dựng sẵn object con (thay cho tạo lúc chạy). Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            countdownRoot = EditorChildren.Rect(transform, "FreezeCountdown", false);
            disc = BuildImage("Disc");
            ringBack = BuildImage("RingBack");
            fill = BuildImage("Ring");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false; // vòng hụt dần theo chiều kim đồng hồ ngược như kim đếm lùi

            TextMeshProUGUI text = EditorChildren.Child<TextMeshProUGUI>(countdownRoot, "Seconds");
            TMP_Text sample = null;
            foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t != text) { sample = t; break; }
            }
            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
            }
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            number = text;
        }

        private Image BuildImage(string imageName)
        {
            Image img = EditorChildren.Child<Image>(countdownRoot, imageName);
            img.raycastTarget = false;
            return img;
        }
#endif
    }
}
