using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Shared
{
    /// <summary>
    /// Đặt trên Canvas: mọi Button bên trong (kể cả đang tắt) phát âm Click và rung nhẹ (HapticType.Button) khi bấm.
    /// Nút không muốn có âm: thêm component NoClickSound; không muốn rung: NoClickVibrate (nút booster tự rung khi booster có tác dụng).
    /// Danh sách nút do Editor thu sẵn (UIAutoSetup khi lưu scene / vào Play), lúc chạy không tìm (guide 8.3).
    /// </summary>
    public class UIButtonSounds : MonoBehaviour
    {
        [SerializeField] private SoundId sound = SoundId.Click;
        [Tooltip("Rung nhẹ khi bấm nút (theo cài đặt Rung của người chơi)")]
        [SerializeField] private bool vibrate = true;
        [Tooltip("Editor tự điền: Button con phát âm Click")]
        [SerializeField] private Button[] clickButtons;
        [Tooltip("Editor tự điền: Button con rung khi bấm")]
        [SerializeField] private Button[] vibrateButtons;

        private void Awake()
        {
            if (clickButtons != null)
            {
                foreach (Button b in clickButtons)
                {
                    if (b != null) b.onClick.AddListener(PlayClick);
                }
            }
            if (vibrate && vibrateButtons != null)
            {
                foreach (Button b in vibrateButtons)
                {
                    if (b != null) b.onClick.AddListener(Vibrate);
                }
            }
        }

        private void PlayClick() => AudioManager.Play(sound);

        private static void Vibrate() => AudioManager.Vibrate(HapticType.Button);

#if UNITY_EDITOR
        /// <summary>Thu lại danh sách Button con; trả về true nếu có đổi.</summary>
        public bool EditorCollect()
        {
            var click = new List<Button>();
            var vib = new List<Button>();
            foreach (Button b in GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<NoClickSound>() == null) click.Add(b);
                if (b.GetComponent<NoClickVibrate>() == null) vib.Add(b);
            }
            bool changed = !Same(clickButtons, click) || !Same(vibrateButtons, vib);
            clickButtons = click.ToArray();
            vibrateButtons = vib.ToArray();
            return changed;
        }

        private static bool Same(Button[] a, List<Button> b)
        {
            if (a == null || a.Length != b.Count) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
#endif
    }
}
