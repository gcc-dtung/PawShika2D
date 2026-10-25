using NUnit.Framework;

namespace PawBlock.Features.LuckyWheel.Tests
{
    /// <summary>EditMode: Vòng quay may mắn (LuckyWheelModel): chọn ô theo trọng số, góc dừng dưới kim.</summary>
    public class LuckyWheelTests
    {
        private static readonly int[] Weights = { 25, 15, 0, 60 }; // tổng 100: [0, .25) -> 0, [.25, .40) -> 1, ô 2 không bao giờ ra, [.40, 1) -> 3

        [TestCase(0.0, 0)]
        [TestCase(0.249, 0)]
        [TestCase(0.25, 1)]
        [TestCase(0.399, 1)]
        [TestCase(0.40, 3)]
        [TestCase(0.999, 3)]
        public void ChonOTheoTrongSo(double roll, int index)
        {
            Assert.AreEqual(index, LuckyWheelModel.Pick(Weights, roll));
        }

        [Test]
        public void TongTrongSo0KhongChonDuoc()
        {
            Assert.AreEqual(-1, LuckyWheelModel.Pick(new[] { 0, 0 }, 0.5));
        }

        [Test]
        public void GocTamO()
        {
            Assert.AreEqual(22.5f, LuckyWheelModel.CenterAngle(0, 8), 1e-4f);
            Assert.AreEqual(337.5f, LuckyWheelModel.CenterAngle(7, 8), 1e-4f);
        }

        [TestCase(0f, 0)]
        [TestCase(0f, 5)]
        [TestCase(123f, 3)]
        [TestCase(-1000f, 7)]
        public void QuayTheoChieuKimDongHoDuSoVongVaDungDungO(float current, int index)
        {
            const int count = 8, turns = 5;
            float target = LuckyWheelModel.TargetRotation(current, index, count, turns);
            Assert.LessOrEqual(target, current - 360f * turns + 1e-3f); // quay ít nhất 5 vòng theo chiều kim đồng hồ
            Assert.Greater(target, current - 360f * (turns + 1) - 1e-3f);
            // Quay z = target thì tâm ô index nằm ở đỉnh: (tâm ô - target) chia hết cho 360
            float rest = ((LuckyWheelModel.CenterAngle(index, count) - target) % 360f + 360f) % 360f;
            Assert.That(rest < 1e-2f || rest > 360f - 1e-2f, $"lệch {rest}");
        }
    }
}
