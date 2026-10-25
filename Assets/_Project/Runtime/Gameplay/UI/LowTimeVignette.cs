using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Sắp hết giờ (dưới LivesEnergyConfig.warningSeconds): viền đỏ quanh màn hình, đập theo nhịp mỗi giây
    /// (cùng lúc icon đồng hồ đập / rung Tick). Đóng băng, được cộng giờ, hết ván thì tan đi.
    /// Đặt trên Canvas HUD; Image viền tự tạo (sprite ProceduralSprites.Vignette), nằm dưới các nút HUD, không chắn bấm.
    /// </summary>
    public class LowTimeVignette : MonoBehaviour
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels, LevelTimer timer)
        {
            _levels = levels;
            _timer = timer;
        }

        [SerializeField] private Color color = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.14f;  // lúc giữa 2 nhịp
        [SerializeField, Range(0f, 1f)] private float pulseAlpha = 0.4f;  // đỉnh mỗi nhịp
        [SerializeField] private float pulseDuration = 0.45f;
        [SerializeField] private float fadeDuration = 0.35f;
        [Tooltip("Ảnh viền (con đầu tiên, phủ cả màn, Editor dựng sẵn)")]
        [SerializeField] private Image vignette;

        private LevelTimer _timer;
        private Tween _tween;
        private bool _on;

        private void Awake()
        {
            if (vignette == null)
            {
                Debug.LogError($"[LowTimeVignette] Thiếu ảnh viền trên {name} (chuột phải component -> Dựng sẵn object con)", this);
                enabled = false;
                return;
            }
            vignette.sprite = ProceduralSprites.Vignette; // sprite sinh lúc chạy, không lưu được vào scene
            vignette.raycastTarget = false;
            SetAlpha(0f);
            vignette.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_timer != null)
            {
                _timer.onTimeWarning.AddListener(Refresh);
                _timer.onTimeChanged.AddListener(OnTimeChanged); // cheat / cộng giờ nhảy qua ngưỡng không phát onTimeWarning
                _timer.onLowTick.AddListener(OnLowTick);
                _timer.onTimeAdded.AddListener(OnTimeAdded);
                _timer.onFreezeChanged.AddListener(OnFreezeChanged);
            }
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            Refresh();
        }

        private void OnDisable()
        {
            if (_timer != null)
            {
                _timer.onTimeWarning.RemoveListener(Refresh);
                _timer.onTimeChanged.RemoveListener(OnTimeChanged);
                _timer.onLowTick.RemoveListener(OnLowTick);
                _timer.onTimeAdded.RemoveListener(OnTimeAdded);
                _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            }
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            _tween?.Kill();
            _tween = null;
            _on = false;
            if (vignette != null)
            {
                SetAlpha(0f);
                vignette.gameObject.SetActive(false);
            }
        }

        private void OnLowTick(int seconds)
        {
            if (_on) Pulse();
        }

        private void OnTimeAdded(float _) => Refresh();
        private void OnTimeChanged(float remaining, float total) => Refresh(); // Refresh chỉ làm gì khi trạng thái đổi
        private void OnFreezeChanged(bool _) => Refresh();
        private void OnStateChanged(GameStateChangedEvent _) => Refresh();
        private void OnLevelLoaded(LevelLoadedEvent _) => Refresh();

        private bool ShouldShow()
        {
            bool playing = _levels == null || _levels.State == GameState.Playing;
            return playing && _timer != null && _timer.IsLow && !_timer.IsFrozen && !_timer.IsTimeUp;
        }

        private void Refresh()
        {
            bool show = ShouldShow();
            if (show == _on) return;
            _on = show;
            if (show)
            {
                vignette.gameObject.SetActive(true);
                Pulse();
            }
            else
            {
                _tween?.Kill();
                _tween = vignette.DOFade(0f, fadeDuration)
                    .SetUpdate(true)
                    .SetLink(vignette.gameObject)
                    .OnComplete(() => vignette.gameObject.SetActive(false));
            }
        }

        /// <summary>1 nhịp: sáng nhanh lên đỉnh rồi lắng về mức nền.</summary>
        private void Pulse()
        {
            _tween?.Kill();
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(vignette.gameObject);
            seq.Append(vignette.DOFade(pulseAlpha, pulseDuration * 0.25f).SetEase(Ease.OutQuad));
            seq.Append(vignette.DOFade(baseAlpha, pulseDuration * 0.75f).SetEase(Ease.InOutSine));
            _tween = seq;
        }

        private void SetAlpha(float a)
        {
            Color c = color;
            c.a = a;
            vignette.color = c;
        }

#if UNITY_EDITOR
        /// <summary>Editor: Dựng sẵn object con (thay cho tạo lúc chạy). Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            vignette = EditorChildren.Child<Image>(transform, "LowTimeVignette", false);
            RectTransform rt = vignette.rectTransform;
            rt.SetAsFirstSibling(); // dưới các nút HUD
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            vignette.raycastTarget = false;
        }
#endif
    }
}
