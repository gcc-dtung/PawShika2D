using System.Collections.Generic;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>
    /// Luật Vòng quay may mắn (C# thuần): chọn ô theo trọng số, góc quay để ô đó dừng dưới kim (đỉnh vòng).
    /// Ô i chiếm cung [i, i + 1] x (360 / số ô) độ, tính theo chiều kim đồng hồ từ đỉnh.
    /// </summary>
    public static class LuckyWheelModel
    {
        /// <summary>Ô được chọn với <paramref name="roll"/> trong [0, 1): ô i chiếm đoạn dài trọng số i / tổng. -1 nếu tổng 0.</summary>
        public static int Pick(IList<int> weights, double roll)
        {
            int total = 0;
            foreach (int w in weights) total += System.Math.Max(0, w);
            if (total <= 0) return -1;
            double target = roll * total;
            int acc = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                acc += System.Math.Max(0, weights[i]);
                if (target < acc) return i;
            }
            return weights.Count - 1;
        }

        /// <summary>Góc (độ) của tâm ô i, theo chiều kim đồng hồ từ đỉnh.</summary>
        public static float CenterAngle(int index, int count) => (index + 0.5f) * 360f / count;

        /// <summary>
        /// Góc quay z cuối cùng (Unity: dương = ngược chiều kim đồng hồ) khi quay theo chiều kim đồng hồ từ <paramref name="current"/>
        /// ít nhất <paramref name="turns"/> vòng, để điểm lệch <paramref name="offset"/> độ quanh tâm ô <paramref name="index"/> dừng dưới kim.
        /// </summary>
        public static float TargetRotation(float current, int index, int count, int turns, float offset = 0f)
        {
            float target = CenterAngle(index, count) + offset; // ô quay ngược lại đúng góc tâm của nó thì nằm ở đỉnh
            float delta = ((current - target) % 360f + 360f) % 360f;
            return current - 360f * turns - delta;
        }
    }
}
