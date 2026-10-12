namespace PawBlock.Features.Gameplay
{
    /// <summary>Chữ tiêu đề màn thắng theo số sao (ảnh well_done_1 / nice_1 / great_1 / perfect_1 của bản gốc).</summary>
    public enum WinTitle
    {
        WellDone = 0, // 0 sao: hết giờ rồi thêm giờ chơi tiếp mới thắng
        Nice = 1,
        Great = 2,
        Perfect = 3
    }

    /// <summary>
    /// Sao của 1 ván theo phần thời gian còn lại, như thanh TimeProgress gốc có 3 mốc 60 / 20 / 0 %:
    /// còn trên 60 % = 3 sao, trên 20 % = 2 sao, còn giây nào = 1 sao, hết giờ = 0 sao.
    /// Hàm thuần (không đọc config) để test; ngưỡng thật lấy từ LivesEnergyConfig.
    /// </summary>
    public static class StarRating
    {
        public const int Max = 3;

        /// <param name="remainingRatio">Thời gian còn lại / tổng (0..1).</param>
        /// <param name="twoStarAbove">Còn trên tỉ lệ này thì giữ sao thứ 2 (gốc 0.2).</param>
        /// <param name="threeStarAbove">Còn trên tỉ lệ này thì giữ sao thứ 3 (gốc 0.6).</param>
        public static int FromRatio(float remainingRatio, float twoStarAbove, float threeStarAbove)
        {
            if (remainingRatio > threeStarAbove) return 3;
            if (remainingRatio > twoStarAbove) return 2;
            return remainingRatio > 0f ? 1 : 0;
        }

        /// <summary>Mốc trên thanh thời gian mà sao thứ <paramref name="star"/> (1..3) mất khi thời gian tụt qua.</summary>
        public static float MarkerRatio(int star, float twoStarAbove, float threeStarAbove)
        {
            if (star >= 3) return threeStarAbove;
            return star == 2 ? twoStarAbove : 0f;
        }

        public static WinTitle Title(int stars)
        {
            if (stars >= 3) return WinTitle.Perfect;
            if (stars == 2) return WinTitle.Great;
            return stars == 1 ? WinTitle.Nice : WinTitle.WellDone;
        }
    }

    /// <summary>
    /// Số sao đang giữ trong ván: chỉ giảm, không tăng lại (thêm giờ sau khi hết giờ / cheat cộng giờ vẫn giữ số sao đã mất).
    /// Lớp thuần, LevelStars bọc lại để nghe đồng hồ.
    /// </summary>
    public sealed class StarTracker
    {
        public int Stars { get; private set; } = StarRating.Max;

        public void Reset() => Stars = StarRating.Max;

        /// <summary>Cập nhật theo đồng hồ. Trả về true nếu vừa mất sao.</summary>
        public bool Update(float remaining, float total, float twoStarAbove, float threeStarAbove)
        {
            if (total <= 0f) return false;
            int stars = StarRating.FromRatio(remaining / total, twoStarAbove, threeStarAbove);
            if (stars >= Stars) return false;
            Stars = stars;
            return true;
        }
    }
}
