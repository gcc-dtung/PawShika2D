using TMPro;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// HUD trên: chữ "LEVEL n" (Daily Challenge: ngày, vd "OCT 11"); level Hard / Very Hard thì có khung có tab ghi mức khó
    /// bao ô LEVEL (DifficultyTheme).
    /// </summary>
    public class LevelHUD : MonoBehaviour
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        [SerializeField] private TMP_Text levelText;
        [SerializeField] private DifficultyTheme theme;
        [SerializeField] private UnityEngine.UI.Image hardFrame;
        [SerializeField] private TMP_Text tabText;
        [Tooltip("Nền ô LEVEL: mức khó thì đổi sang hình bo góc để góc vuông không lòi ra ngoài khung")]
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private Sprite roundedBackground;
        [Tooltip("Màu tô lên nền bo góc (sprite trắng) cho giống nền thường khung1")]
        [SerializeField] private Color roundedBackgroundColor = new Color(0.204f, 0.204f, 0.365f, 1f);

        private Sprite _normalBg;
        private UnityEngine.UI.Image.Type _normalBgType;
        private Color _normalBgColor = Color.white;
        private bool _cached;

        private void Start()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            if (_levels != null) SetLevel(_levels.CurrentLevelNumber);
        }

        private void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
        }

        private void OnLevelLoaded(LevelLoadedEvent e) => SetLevel(e.Number);

        private void SetLevel(int number)
        {
            LevelManager lm = _levels;
            if (levelText != null)
            {
                if (lm != null && lm.IsDaily) levelText.SetText(DailyLabel(lm.DailyDay));
                else levelText.SetText($"LEVEL {number}");
            }

            LevelDefinition level = _levels != null ? _levels.CurrentLevel : null;
            DifficultyStyle style = theme != null && level != null ? theme.Get(level.difficulty) : null;
            bool hard = style != null && style.hudFrame != null;
            if (!_cached)
            {
                _cached = true;
                if (background != null)
                {
                    _normalBg = background.sprite;
                    _normalBgType = background.type;
                    _normalBgColor = background.color;
                }
            }
            bool rounded = hard && roundedBackground != null;
            if (background != null)
            {
                background.sprite = rounded ? roundedBackground : _normalBg;
                background.type = rounded ? UnityEngine.UI.Image.Type.Sliced : _normalBgType;
                // Sprite bo góc là hình trắng: tô màu nền thường, không thì ô LEVEL thành nền trắng (chữ trắng biến mất)
                background.color = rounded ? roundedBackgroundColor : _normalBgColor;
            }
            if (hardFrame == null) return;
            hardFrame.gameObject.SetActive(hard);
            if (!hard) return;
            hardFrame.sprite = style.hudFrame;
            if (tabText == null) return;
            tabText.SetText(style.label);
            if (style.hudTabOutline != null) tabText.fontSharedMaterial = style.hudTabOutline;
        }

        /// <summary>Nhãn level Daily: tháng (3 chữ) + ngày, vd "OCT 11".</summary>
        public static string DailyLabel(long day) =>
            DayNumber.ToDate(day).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
    }
}
