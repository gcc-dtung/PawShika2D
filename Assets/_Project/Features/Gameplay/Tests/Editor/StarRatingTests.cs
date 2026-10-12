using NUnit.Framework;
using PawBlock.Features.Economy;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>EditMode: sao theo thời gian còn lại (StarRating) và số sao giữ trong ván (StarTracker), ngưỡng gốc 60 % / 20 %.</summary>
    public class StarRatingTests
    {
        private const float Two = 0.2f;
        private const float Three = 0.6f;

        [TestCase(1f, 3)]
        [TestCase(0.61f, 3)]
        [TestCase(0.6f, 2)]   // đúng mốc là đã mất sao (thanh tụt chạm mốc)
        [TestCase(0.21f, 2)]
        [TestCase(0.2f, 1)]
        [TestCase(0.01f, 1)]
        [TestCase(0f, 0)]
        public void SaoTheoTiLeConLai(float ratio, int stars)
        {
            Assert.AreEqual(stars, StarRating.FromRatio(ratio, Two, Three));
        }

        [Test]
        public void MocCuaTungSao()
        {
            Assert.AreEqual(0f, StarRating.MarkerRatio(1, Two, Three));
            Assert.AreEqual(Two, StarRating.MarkerRatio(2, Two, Three));
            Assert.AreEqual(Three, StarRating.MarkerRatio(3, Two, Three));
        }

        [TestCase(3, WinTitle.Perfect)]
        [TestCase(2, WinTitle.Great)]
        [TestCase(1, WinTitle.Nice)]
        [TestCase(0, WinTitle.WellDone)]
        public void TieuDeTheoSoSao(int stars, WinTitle title)
        {
            Assert.AreEqual(title, StarRating.Title(stars));
        }

        [Test]
        public void TrackerBatDau3SaoVaMatDanTheoDongHo()
        {
            StarTracker t = new StarTracker();
            Assert.AreEqual(3, t.Stars);
            Assert.IsFalse(t.Update(90f, 100f, Two, Three));
            Assert.IsTrue(t.Update(50f, 100f, Two, Three));
            Assert.AreEqual(2, t.Stars);
            Assert.IsFalse(t.Update(40f, 100f, Two, Three)); // vẫn 2 sao: không báo lại
            Assert.IsTrue(t.Update(0f, 100f, Two, Three));   // hết giờ: tụt thẳng 2 -> 0 trong 1 lần
            Assert.AreEqual(0, t.Stars);
        }

        [Test]
        public void ThemGioKhongLayLaiSao()
        {
            StarTracker t = new StarTracker();
            t.Update(0f, 60f, Two, Three);
            Assert.IsFalse(t.Update(30f, 60f, Two, Three)); // thêm 30 s sau khi hết giờ: tỉ lệ 50 % nhưng vẫn 0 sao
            Assert.AreEqual(0, t.Stars);
        }

        [Test]
        public void ResetVe3SaoVaBoQuaTongThoiGian0()
        {
            StarTracker t = new StarTracker();
            t.Update(10f, 100f, Two, Three);
            t.Reset();
            Assert.AreEqual(3, t.Stars);
            Assert.IsFalse(t.Update(0f, 0f, Two, Three)); // màn chưa có giờ (tổng 0): không tính
            Assert.AreEqual(3, t.Stars);
        }

        [TestCase(LevelDifficulty.Normal, 0, 10)]
        [TestCase(LevelDifficulty.Normal, 3, 25)]
        [TestCase(LevelDifficulty.Hard, 2, 30)]
        [TestCase(LevelDifficulty.VeryHard, 1, 35)]
        public void CoinThuongCongTheoSao(LevelDifficulty difficulty, int stars, int coins)
        {
            EconomyConfig cfg = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>(); // mặc định 10 / 20 / 30, 5 coin / sao
            try
            {
                Assert.AreEqual(coins, cfg.WinCoinReward(difficulty, stars));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cfg);
            }
        }
    }
}
