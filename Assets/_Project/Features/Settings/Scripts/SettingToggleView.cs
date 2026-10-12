using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Settings
{
    public enum SettingType
    {
        Music,
        Sound,
        Vibrate
    }

    /// <summary>
    /// 1 nút tròn trong popup cài đặt: bật = nền xanh, tắt = nền xám + gạch đỏ. Bấm -> đảo cài đặt trong AudioManager
    /// (lưu SaveData). Cài đặt đổi ở đâu cũng vẽ lại theo AudioManager.SettingsChanged.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SettingToggleView : MonoBehaviour
    {
        [SerializeField] private SettingType type;
        [SerializeField] private Image background;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private GameObject offMark;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            AudioManager.SettingsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            AudioManager.SettingsChanged -= Refresh;
        }

        private bool IsOn
        {
            get
            {
                switch (type)
                {
                    case SettingType.Music: return AudioManager.MusicOn;
                    case SettingType.Sound: return AudioManager.SoundOn;
                    default: return AudioManager.VibrateOn;
                }
            }
            set
            {
                switch (type)
                {
                    case SettingType.Music: AudioManager.MusicOn = value; break;
                    case SettingType.Sound: AudioManager.SoundOn = value; break;
                    default: AudioManager.VibrateOn = value; break;
                }
            }
        }

        private void Toggle()
        {
            IsOn = !IsOn;
            if (type == SettingType.Vibrate && IsOn) AudioManager.Vibrate(); // vừa bật rung: rung thử 1 cái
        }

        private void Refresh()
        {
            bool on = IsOn;
            if (background != null)
            {
                Sprite s = on ? onSprite : offSprite;
                if (s != null) background.sprite = s;
            }
            if (offMark != null) offMark.SetActive(!on);
        }
    }
}
