using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đồng hồ đếm ngược trên HUD (dưới chữ LEVEL): icon đồng hồ + "m:ss".
/// Còn dưới LivesEnergyConfig.warningSeconds: chữ đỏ, icon đập mỗi giây. Cộng giờ: icon nảy. Đóng băng: chữ xanh băng.
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

    private int _shownSeconds = -1;
    private Tween _punch;

    private void OnEnable()
    {
        LevelTimer timer = LevelTimer.Instance;
        if (timer == null) return;
        timer.onTimeChanged.AddListener(OnTimeChanged);
        timer.onTimeAdded.AddListener(OnTimeAdded);
        timer.onFreezeChanged.AddListener(OnFreezeChanged);
        _shownSeconds = -1;
        OnTimeChanged(timer.Remaining, timer.Total);
    }

    private void OnDisable()
    {
        KillPunch();
        LevelTimer timer = LevelTimer.Instance;
        if (timer == null) return;
        timer.onTimeChanged.RemoveListener(OnTimeChanged);
        timer.onTimeAdded.RemoveListener(OnTimeAdded);
        timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
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

    private void OnTimeAdded(float _)
    {
        _shownSeconds = -1; // vẽ lại ngay ở onTimeChanged kế tiếp
        Punch();
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
