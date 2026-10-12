using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Gameplay;

namespace PawBlock.Shared
{
    /// <summary>
    /// Đặt trên Canvas: mọi Button bên trong (kể cả đang tắt) phát âm Click và rung nhẹ (HapticType.Button) khi bấm.
    /// Nút không muốn có âm: thêm component NoClickSound. Nút booster tự rung khi booster có tác dụng (GameHapticEvents) nên không rung ở đây.
    /// </summary>
    public class UIButtonSounds : MonoBehaviour
    {
        [SerializeField] private SoundId sound = SoundId.Click;
        [Tooltip("Rung nhẹ khi bấm nút (theo cài đặt Rung của người chơi)")]
        [SerializeField] private bool vibrate = true;

        private void Awake()
        {
            foreach (Button b in GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<NoClickSound>() == null) b.onClick.AddListener(PlayClick);
                if (vibrate && b.GetComponent<BoosterButton>() == null) b.onClick.AddListener(Vibrate);
            }
        }

        private void PlayClick() => AudioManager.Play(sound);

        private static void Vibrate() => AudioManager.Vibrate(HapticType.Button);
    }
}
