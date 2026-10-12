using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Những gì 1 booster được phép dùng từ BoosterManager (booster không gọi thẳng .Instance của manager khác).
/// </summary>
public interface IBoosterHost
{
    GridManager Grid { get; }
    LevelTimer Timer { get; }

    /// <summary>
    /// Booster đã thực sự có tác dụng: trừ 1 lượt (hoặc dùng lượt miễn phí của tutorial), phát onBoosterUsed.
    /// <paramref name="target"/>: điểm world hiệu ứng nhắm tới (icon bay từ giữa màn hình tới đó); null = mặc định của booster.
    /// </summary>
    void Consume(BoosterType type, Vector3? target = null);

    /// <summary>Đặt lần lượt các khối của lời giải (sau khi icon booster bay tới), lưới bị khóa trong lúc đặt.</summary>
    void PlaceInSequence(List<RectInt> rects);

    /// <summary>Hint: hiện mèo mờ + bàn tay kéo ở vùng đúng (sau khi icon booster bay tới).</summary>
    void ShowHint(RectInt rect);

    /// <summary>Gợi ý Hint đang hiện (chưa được đặt).</summary>
    bool IsHintShowing { get; }

    /// <summary>Chạy <paramref name="action"/> khi hiệu ứng dùng booster tới đoạn "có tác dụng" (vd Freeze: lúc sóng băng nổ).</summary>
    void AfterEffect(BoosterType type, System.Action action);

    /// <summary>Booster bật / tắt chế độ chờ (vd Reveal chờ chạm 1 ô số): nút và banner vẽ lại.</summary>
    void NotifyModeChanged(BoosterType type, bool on);
}

/// <summary>
/// Luật của 1 booster (Strategy). BoosterManager lo phần chung (khóa / mở, đếm lượt, mua, tutorial),
/// class con chỉ lo tác dụng riêng. Thêm booster mới: thêm BoosterType + class con + 1 dòng trong BoosterEffectFactory.
/// </summary>
public abstract class BoosterEffect
{
    protected IBoosterHost Host { get; private set; }

    public abstract BoosterType Type { get; }

    /// <summary>Đang bật (nút có quầng sáng): đồng hồ đang đóng băng, Reveal đang chờ chọn số...</summary>
    public virtual bool IsActive => false;

    public void Bind(IBoosterHost host) => Host = host;

    /// <summary>Bấm nút khi đã còn lượt (hoặc lượt miễn phí). Chỉ gọi Host.Consume khi thực sự có tác dụng.</summary>
    public abstract void Activate();

    /// <summary>Bấm lại nút khi đang IsActive. Mặc định bỏ qua (vd đang đóng băng thì không bật lần nữa).</summary>
    public virtual void PressWhileActive() { }

    /// <summary>Thoát chế độ chờ (nạp level, ván dừng, dùng booster khác).</summary>
    public virtual void Cancel() { }

    /// <summary>Chạm 1 ô trên lưới. Trả về true nếu booster đã xử lý chạm (lưới bỏ qua).</summary>
    public virtual bool HandleCellTap(Vector2Int pos) => false;
}
