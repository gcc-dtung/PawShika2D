using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đặt trên Canvas: mọi Button bên trong (kể cả đang tắt) phát âm Click khi bấm.
/// Nút không muốn có âm: thêm component NoClickSound.
/// </summary>
public class UIButtonSounds : MonoBehaviour
{
    [SerializeField] private SoundId sound = SoundId.Click;

    private void Awake()
    {
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            if (b.GetComponent<NoClickSound>() != null) continue;
            b.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick() => AudioManager.Play(sound);
}
