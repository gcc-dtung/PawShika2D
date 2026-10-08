using TMPro;
using UnityEngine;

/// <summary>HUD trên: chữ "LEVEL n"; level Hard / Very Hard thì có khung có tab ghi mức khó bao ô LEVEL (DifficultyTheme).</summary>
public class LevelHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private DifficultyTheme theme;
    [SerializeField] private UnityEngine.UI.Image hardFrame;
    [SerializeField] private TMP_Text tabText;
    [Tooltip("Nền ô LEVEL: mức khó thì đổi sang hình bo góc để góc vuông không lòi ra ngoài khung")]
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private Sprite roundedBackground;

    private Sprite _normalBg;
    private UnityEngine.UI.Image.Type _normalBgType;
    private bool _cached;

    private void Start()
    {
        EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
        if (LevelManager.Instance != null) SetLevel(LevelManager.Instance.CurrentLevelNumber);
    }

    private void OnDestroy()
    {
        EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
    }

    private void OnLevelLoaded(LevelLoadedEvent e) => SetLevel(e.Number);

    private void SetLevel(int number)
    {
        if (levelText != null) levelText.SetText($"LEVEL {number}");

        LevelDefinition level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
        DifficultyStyle style = theme != null && level != null ? theme.Get(level.difficulty) : null;
        bool hard = style != null && style.hudFrame != null;
        if (background != null)
        {
            if (!_cached)
            {
                _cached = true;
                _normalBg = background.sprite;
                _normalBgType = background.type;
            }
            bool rounded = hard && roundedBackground != null;
            background.sprite = rounded ? roundedBackground : _normalBg;
            background.type = rounded ? UnityEngine.UI.Image.Type.Sliced : _normalBgType;
        }
        if (hardFrame == null) return;
        hardFrame.gameObject.SetActive(hard);
        if (!hard) return;
        hardFrame.sprite = style.hudFrame;
        if (tabText == null) return;
        tabText.SetText(style.label);
        if (style.hudTabOutline != null) tabText.fontSharedMaterial = style.hudTabOutline;
    }
}
