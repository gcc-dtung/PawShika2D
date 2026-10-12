using DG.Tweening;
using UnityEngine;

/// <summary>
/// Khởi tạo DOTween trước khi scene nạp, đặt sẵn sức chứa đủ cho lúc đông hiệu ứng nhất
/// (intro lưới lớn + hạt bụi, sóng ô, mèo kề bên, khối cuối...). Không đặt thì DOTween tự nới 200 → 500 giữa lúc chơi
/// (cấp phát lại bộ nhớ, có thể khựng 1 frame) và log "Max Tweens reached".
/// </summary>
public static class TweenSetup
{
    public const int Tweens = 500;
    public const int Sequences = 150;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Init() đọc DOTweenSettings (Resources). Không nối .SetCapacity(): project tắt domain reload nên từ lần vào Play thứ 2
        // DOTween đã "initialized" và Init() trả về null -> gọi hàm tĩnh riêng
        DOTween.Init();
        DOTween.SetTweensCapacity(Tweens, Sequences);
    }
}
