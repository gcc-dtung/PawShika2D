using System.Linq;
using NUnit.Framework;

namespace PawBlock.Game.Tests
{
    /// <summary>EditMode: luật bot bám đuổi của cuộc thi tuần (WeeklyBots).</summary>
    public class WeeklyBotsTests
    {
        private static readonly WeeklyBots.Settings S = WeeklyBots.Settings.Default;
        private const int Rivals = 10;

        [Test]
        public void ActivePlayer_IsTop_AndRunnerUpStaysClose()
        {
            int player = 60;
            int[] bots = WeeklyBots.Scores(7, Rivals, 120, player, 0, S);
            Assert.That(bots.Max(), Is.LessThan(player), "đang chơi thì không bot nào vượt");
            Assert.That(player - bots.Max(), Is.LessThanOrEqualTo(S.maxLead), "top 2 không cách quá maxLead");
        }

        [Test]
        public void IdlePlayer_IsPushedDown_ButNotFar()
        {
            int player = 40;
            int above = WeeklyBots.AboveCount(0, 24, S); // bỏ chơi 1 ngày
            int[] bots = WeeklyBots.Scores(7, Rivals, 120, player, above, S);
            Assert.That(above, Is.EqualTo(3));
            Assert.That(bots.Count(b => b > player), Is.EqualTo(3));
            Assert.That(bots.Max(), Is.LessThanOrEqualTo(player + S.maxAhead), "bot vượt không quá maxAhead");
        }

        [Test]
        public void BotsAbove_HaveDifferentScores()
        {
            int[] bots = WeeklyBots.Scores(7, Rivals, 120, 0, S.maxDrop, S);
            int[] above = bots.Where(b => b > 0).ToArray();
            Assert.That(above.Length, Is.EqualTo(S.maxDrop));
            Assert.That(above.Distinct().Count(), Is.EqualTo(above.Length), "bot vượt không dồn cùng một điểm");
        }

        [Test]
        public void LongIdle_DropsAtMost_MaxDrop()
        {
            Assert.That(WeeklyBots.AboveCount(0, 24 * 30, S), Is.EqualTo(S.maxDrop));
            Assert.That(WeeklyBots.AboveCount(S.maxDrop + 3, 0, S), Is.EqualTo(S.maxDrop));
        }

        [Test]
        public void ZeroScore_NoBotBelowZero()
        {
            int[] bots = WeeklyBots.Scores(3, Rivals, 2, 0, 0, S);
            Assert.That(bots.All(b => b == 0));
        }

        [Test]
        public void SameInput_SameScores()
        {
            CollectionAssert.AreEqual(WeeklyBots.Scores(11, Rivals, 50, 20, 2, S), WeeklyBots.Scores(11, Rivals, 50, 20, 2, S));
        }
    }
}
