using System;
using NUnit.Framework;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.DailyChallenge.Tests
{
    /// <summary>EditMode: luật Daily Challenge (DailyChallengeModel): trạng thái ngày, ngày lỡ, quà mốc theo tháng.</summary>
    public class DailyChallengeModelTests
    {
        private static long D(int y, int m, int d) => DailyChallengeModel.DayNumber(new DateTime(y, m, d));

        private static DailyChallengeModel Started(long start)
        {
            DailyChallengeModel model = new DailyChallengeModel(new DailyChallengeSave());
            model.EnsureStart(start);
            return model;
        }

        [Test]
        public void DoiQuaLaiNgay()
        {
            long day = D(2026, 10, 11);
            Assert.AreEqual(new DateTime(2026, 10, 11), DailyChallengeModel.DateOf(day));
        }

        [Test]
        public void TrangThaiNgay()
        {
            long today = D(2026, 10, 11);
            DailyChallengeModel m = Started(D(2026, 10, 5));
            m.MarkDone(D(2026, 10, 6));

            Assert.AreEqual(DailyDayState.Unavailable, m.StateOf(D(2026, 10, 4), today)); // trước ngày mở khóa
            Assert.AreEqual(DailyDayState.Missed, m.StateOf(D(2026, 10, 5), today));
            Assert.AreEqual(DailyDayState.Done, m.StateOf(D(2026, 10, 6), today));
            Assert.AreEqual(DailyDayState.Today, m.StateOf(today, today));
            Assert.AreEqual(DailyDayState.Unavailable, m.StateOf(D(2026, 10, 12), today)); // chưa tới
            m.MarkDone(today);
            Assert.AreEqual(DailyDayState.Done, m.StateOf(today, today));
        }

        [Test]
        public void NgayBatDauChiDatMotLan()
        {
            DailyChallengeModel m = Started(D(2026, 10, 5));
            Assert.IsFalse(m.EnsureStart(D(2026, 10, 9)));
            Assert.AreEqual(D(2026, 10, 5), m.StartDay);
        }

        [Test]
        public void NgayLoGanNhatKhongTinhHomNayVaKhongTruocNgayMo()
        {
            long today = D(2026, 10, 11);
            DailyChallengeModel m = Started(D(2026, 10, 8));
            m.MarkDone(D(2026, 10, 10));
            Assert.IsTrue(m.TryGetNearestMissed(today, out long day));
            Assert.AreEqual(D(2026, 10, 9), day);

            m.MarkDone(D(2026, 10, 9));
            m.MarkDone(D(2026, 10, 8));
            Assert.IsFalse(m.TryGetNearestMissed(today, out _)); // 7/10 trước ngày mở khóa: không tính lỡ

            DailyChallengeModel fresh = new DailyChallengeModel(new DailyChallengeSave());
            Assert.IsFalse(fresh.TryGetNearestMissed(today, out _)); // chưa mở khóa
        }

        [Test]
        public void DemNgayThangTheoThangDuongLich()
        {
            DailyChallengeModel m = Started(D(2026, 9, 1));
            m.MarkDone(D(2026, 9, 30));
            m.MarkDone(D(2026, 10, 1));
            m.MarkDone(D(2026, 10, 2));
            Assert.IsFalse(m.MarkDone(D(2026, 10, 2))); // không đếm trùng
            Assert.AreEqual(1, m.DoneInMonth(2026, 9));
            Assert.AreEqual(2, m.DoneInMonth(2026, 10));
            Assert.AreEqual(2f / 21f, m.Progress(2026, 10), 1e-5f);
        }

        [Test]
        public void QuaMocTheoThangNhanMotLan()
        {
            DailyChallengeModel m = Started(D(2026, 10, 1));
            for (int d = 1; d <= 6; d++) m.MarkDone(D(2026, 10, d));
            Assert.AreEqual(DailyGiftState.Locked, m.GiftState(2026, 10, 7));
            Assert.IsFalse(m.MarkGiftClaimed(2026, 10, 7));

            m.MarkDone(D(2026, 10, 7));
            Assert.AreEqual(DailyGiftState.Ready, m.GiftState(2026, 10, 7));
            Assert.AreEqual(DailyGiftState.Locked, m.GiftState(2026, 10, 14));
            Assert.IsTrue(m.MarkGiftClaimed(2026, 10, 7));
            Assert.AreEqual(DailyGiftState.Claimed, m.GiftState(2026, 10, 7));
            Assert.IsFalse(m.MarkGiftClaimed(2026, 10, 7));

            Assert.AreEqual(DailyGiftState.Locked, m.GiftState(2026, 11, 7)); // tháng sau đếm lại từ đầu
        }

        [Test]
        public void ThanhTienTrinhDayOMocCuoi()
        {
            DailyChallengeModel m = Started(D(2026, 10, 1));
            for (int d = 1; d <= 25; d++) m.MarkDone(D(2026, 10, d));
            Assert.AreEqual(1f, m.Progress(2026, 10));
        }
    }
}
