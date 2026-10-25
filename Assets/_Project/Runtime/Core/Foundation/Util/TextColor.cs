using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Project dùng Linear color space nhưng shader TextMeshPro Mobile không đổi màu đỉnh từ gamma sang linear,
    /// nên màu gán cho chữ hiện nhạt hơn màu chọn. Gán qua hàm này để chữ ra đúng màu thấy trong Inspector / mẫu.
    /// </summary>
    public static class TextColor
    {
        public static Color Display(Color color)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return color;
            Color linear = color.linear;
            linear.a = color.a;
            return linear;
        }
    }
}
