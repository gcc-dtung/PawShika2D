using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sắp hết giờ (dưới LivesEnergyConfig.warningSeconds): viền đỏ quanh màn hình, đập theo nhịp mỗi giây
/// (cùng lúc icon đồng hồ đập / rung Tick). Đóng băng, được cộng giờ, hết ván thì tan đi.
/// Đặt trên Canvas HUD; Image viền tự tạo (sprite ProceduralSprites.Vignette), nằm dưới các nút HUD, không chắn bấm.
/// </summary>
public class LowTimeVignette : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.14f;  // lúc giữa 2 nhịp
    [SerializeField, Range(0f, 1f)] private float pulseAlpha = 0.4f;  // đỉnh mỗi nhịp
    [SerializeField] private float pulseDuration = 0.45f;
    [SerializeField] private float fadeDuration = 0.35f;

    private Image _image;
    private LevelTimer _timer;
    private Tween _tween;
    private bool _on;

    private void Awake()
    {
        GameObject go = new GameObject("LowTimeVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.SetAsFirstSibling(); // dưới các nút HUD
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _image = go.GetComponent<Image>();
        _image.sprite = ProceduralSprites.Vignette;
        _image.raycastTarget = false;
        SetAlpha(0f);
        go.SetActive(false);
    }

    private void OnEnable()
    {
        _timer = LevelTimer.Instance;
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
        if (_image != null)
        {
            SetAlpha(0f);
            _image.gameObject.SetActive(false);
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
        bool playing = LevelManager.Instance == null || LevelManager.Instance.State == GameState.Playing;
        return playing && _timer != null && _timer.IsLow && !_timer.IsFrozen && !_timer.IsTimeUp;
    }

    private void Refresh()
    {
        bool show = ShouldShow();
        if (show == _on) return;
        _on = show;
        if (show)
        {
            _image.gameObject.SetActive(true);
            Pulse();
        }
        else
        {
            _tween?.Kill();
            _tween = _image.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .SetLink(_image.gameObject)
                .OnComplete(() => _image.gameObject.SetActive(false));
        }
    }

    /// <summary>1 nhịp: sáng nhanh lên đỉnh rồi lắng về mức nền.</summary>
    private void Pulse()
    {
        _tween?.Kill();
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(_image.gameObject);
        seq.Append(_image.DOFade(pulseAlpha, pulseDuration * 0.25f).SetEase(Ease.OutQuad));
        seq.Append(_image.DOFade(baseAlpha, pulseDuration * 0.75f).SetEase(Ease.InOutSine));
        _tween = seq;
    }

    private void SetAlpha(float a)
    {
        Color c = color;
        c.a = a;
        _image.color = c;
    }
}
