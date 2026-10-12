using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đồng hồ đếm ngược trên HUD (dưới chữ LEVEL): icon đồng hồ + "m:ss".
/// Còn dưới LivesEnergyConfig.warningSeconds: chữ đỏ, icon đập mỗi giây. Đóng băng: chữ xanh băng.
/// Cộng giờ: chữ "+30s" bật lên giữa màn hình rồi bay vào đồng hồ, chạm tới thì icon nảy.
/// Đồng hồ đang chờ chạm đầu tiên (level mới): chữ giờ + icon nhấp nháy nhẹ.
/// Icon / chữ dựng bởi Tools/PawBlock/Builders/Build Boosters (BoosterBuilder.BuildTimer).
/// </summary>
public class TimerUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color frozenColor = new Color(0.6f, 0.9f, 1f);
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("Cộng giờ: chữ \"+Ns\" bay vào")]
    [SerializeField] private Color addedColor = new Color(0.55f, 1f, 0.55f);
    [SerializeField] private float addedPopDuration = 0.35f;
    [SerializeField] private float addedHold = 0.25f;
    [SerializeField] private float addedFlyDuration = 0.45f;
    [SerializeField] private float addedFontScale = 1.4f;

    [Header("Chờ chạm để bắt đầu: nhấp nháy")]
    [SerializeField, Range(0f, 1f)] private float waitingMinAlpha = 0.4f;
    [SerializeField] private float waitingPeriod = 1.1f;

    private int _shownSeconds = -1;
    private Tween _punch;
    private Sequence _added;
    private Tween _waiting;
    private TMP_Text _addedText;

    private void OnEnable()
    {
        LevelTimer timer = LevelTimer.Instance;
        if (timer == null) return;
        timer.onTimeChanged.AddListener(OnTimeChanged);
        timer.onTimeAdded.AddListener(OnTimeAdded);
        timer.onFreezeChanged.AddListener(OnFreezeChanged);
        timer.onWaitingForTouchChanged.AddListener(OnWaitingForTouchChanged);
        _shownSeconds = -1;
        OnTimeChanged(timer.Remaining, timer.Total);
        OnWaitingForTouchChanged(timer.IsWaitingForTouch);
    }

    private void OnDisable()
    {
        KillPunch();
        KillAdded();
        OnWaitingForTouchChanged(false);
        LevelTimer timer = LevelTimer.Instance;
        if (timer == null) return;
        timer.onTimeChanged.RemoveListener(OnTimeChanged);
        timer.onTimeAdded.RemoveListener(OnTimeAdded);
        timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
        timer.onWaitingForTouchChanged.RemoveListener(OnWaitingForTouchChanged);
    }

    /// <summary>Chờ chạm: chữ giờ + icon mờ / rõ theo nhịp (thời gian thực); bắt đầu chạy thì rõ hẳn lại.</summary>
    private void OnWaitingForTouchChanged(bool waiting)
    {
        _waiting?.Kill();
        _waiting = null;
        if (!waiting)
        {
            SetWaitingAlpha(1f);
            return;
        }
        _waiting = DOVirtual.Float(0f, 1f, waitingPeriod, p => SetWaitingAlpha(Mathf.Lerp(1f, waitingMinAlpha, (1f - Mathf.Cos(p * Mathf.PI * 2f)) * 0.5f)))
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnKill(() => SetWaitingAlpha(1f));
    }

    private void SetWaitingAlpha(float a)
    {
        if (timeText != null) timeText.alpha = a;
        if (icon != null)
        {
            Color c = icon.color;
            c.a = a;
            icon.color = c;
        }
    }

    private void OnTimeChanged(float remaining, float total)
    {
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == _shownSeconds) return;
        bool tick = _shownSeconds > seconds; // giây vừa trôi (không phải đổi level / cộng giờ)
        _shownSeconds = seconds;

        if (timeText != null) timeText.SetText("{0}:{1:00}", seconds / 60, seconds % 60);
        RefreshColor();
        if (tick && LevelTimer.Instance != null && LevelTimer.Instance.IsLow && seconds > 0) Punch();
    }

    private void OnTimeAdded(float seconds)
    {
        _shownSeconds = -1; // vẽ lại ngay ở onTimeChanged kế tiếp
        if (!FlyAddedText(seconds)) Punch();
    }

    /// <summary>
    /// "+Ns" phóng to bật lên giữa màn hình, giữ 1 nhịp, rồi bay vào chữ giờ (nhỏ dần); chạm tới thì icon nảy.
    /// Chạy theo thời gian thực (popup vừa đóng / slow-motion không ảnh hưởng).
    /// </summary>
    private bool FlyAddedText(float seconds)
    {
        if (timeText == null || timeText.canvas == null) return false;
        Canvas root = timeText.canvas.rootCanvas;
        KillAdded();

        if (_addedText == null)
        {
            GameObject go = Instantiate(timeText.gameObject, root.transform);
            go.name = "TimeAdded";
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c is UnityEngine.UI.ILayoutElement && !(c is TMP_Text)) Destroy(c); // bỏ LayoutElement của bản gốc
            }
            _addedText = go.GetComponent<TMP_Text>();
            _addedText.raycastTarget = false;
            RectTransform r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(400f, 160f);
            _addedText.alignment = TextAlignmentOptions.Center;
            _addedText.enableWordWrapping = false;
        }

        GameObject textGo = _addedText.gameObject;
        textGo.SetActive(true);
        textGo.transform.SetAsLastSibling();
        _addedText.SetText("+{0}s", Mathf.RoundToInt(seconds));
        _addedText.color = addedColor;
        Transform t = textGo.transform;
        Vector3 start = root.transform.position; // giữa màn hình (canvas overlay)
        t.position = start;
        t.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(textGo);
        seq.Append(t.DOScale(addedFontScale, addedPopDuration).SetEase(Ease.OutBack));
        seq.AppendInterval(addedHold);
        seq.Append(DOVirtual.Float(0f, 1f, addedFlyDuration, p =>
            {
                Vector3 target = timeText.transform.position; // đọc mỗi frame: đồng hồ có thể đang nảy
                float e = p * p; // nhanh dần như bị hút vào
                t.position = Vector3.LerpUnclamped(start, target, e) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 60f * root.scaleFactor);
                t.localScale = Vector3.one * Mathf.Lerp(addedFontScale, 0.5f, e);
            }).SetEase(Ease.Linear));
        seq.OnComplete(() =>
        {
            textGo.SetActive(false);
            Punch();
        });
        seq.OnKill(() => { if (textGo != null) textGo.SetActive(false); });
        _added = seq;
        return true;
    }

    private void KillAdded()
    {
        _added?.Kill();
        _added = null;
    }

    private void OnFreezeChanged(bool _) => RefreshColor();

    private void RefreshColor()
    {
        if (timeText == null) return;
        LevelTimer timer = LevelTimer.Instance;
        if (timer != null && timer.IsFrozen) timeText.color = frozenColor;
        else if (timer != null && timer.IsLow) timeText.color = warningColor;
        else timeText.color = normalColor;
    }

    /// <summary>Icon phồng to rồi về, lắc nhẹ.</summary>
    private void Punch()
    {
        if (icon == null) return;
        KillPunch();
        Transform t = icon.transform;
        Tween tween = null;
        tween = DOVirtual.Float(0f, 1f, punchDuration, p =>
            {
                t.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, Mathf.Sin(p * Mathf.PI));
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 12f * (1f - p));
            })
            .SetEase(Ease.Linear)
            .SetLink(icon.gameObject)
            .OnKill(() =>
            {
                if (t != null)
                {
                    t.localScale = Vector3.one;
                    t.localRotation = Quaternion.identity;
                }
                if (_punch == tween) _punch = null;
            });
        _punch = tween;
    }

    private void KillPunch()
    {
        _punch?.Kill();
        _punch = null;
    }
}
