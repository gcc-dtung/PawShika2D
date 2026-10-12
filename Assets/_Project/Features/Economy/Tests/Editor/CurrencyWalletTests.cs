using System.Collections.Generic;
using NUnit.Framework;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy.Tests
{
    /// <summary>EditMode: CoinWallet / StarWallet dùng qua ICurrencyWallet (save chỉ trong bộ nhớ, không đụng save.json).</summary>
    public class CurrencyWalletTests
    {
        private JsonFileSaveService _save;
        private EconomySave _economy;

        [SetUp]
        public void SetUp()
        {
            _save = new JsonFileSaveService(null);
            _economy = _save.Get<EconomySave>(EconomySave.Key);
            _economy.coins = 100;
            _economy.stars = 20;
        }

        private static IEnumerable<TestCaseData> Wallets()
        {
            yield return new TestCaseData("coin", 100).SetName("Coin");
            yield return new TestCaseData("star", 20).SetName("Star");
        }

        private ICurrencyWallet Create(string kind) => kind == "coin" ? new CoinWallet(_save) : (ICurrencyWallet)new StarWallet(_save);

        [TestCaseSource(nameof(Wallets))]
        public void Balance_ReadsSaveData(string kind, int expected)
        {
            ICurrencyWallet sut = Create(kind);
            Assert.AreEqual(expected, sut.Balance);
        }

        [TestCaseSource(nameof(Wallets))]
        public void Add_Positive_IncreasesBalance(string kind, int start)
        {
            ICurrencyWallet sut = Create(kind);
            sut.Add(15);

            Assert.AreEqual(start + 15, sut.Balance);
        }

        [TestCaseSource(nameof(Wallets))]
        public void Add_ZeroOrNegative_Ignored(string kind, int start)
        {
            ICurrencyWallet sut = Create(kind);
            sut.Add(0);
            sut.Add(-5);

            Assert.AreEqual(start, sut.Balance);
        }

        [TestCaseSource(nameof(Wallets))]
        public void TrySpend_Enough_SpendsAndReturnsTrue(string kind, int start)
        {
            ICurrencyWallet sut = Create(kind);
            bool ok = sut.TrySpend(start);

            Assert.IsTrue(ok);
            Assert.AreEqual(0, sut.Balance);
        }

        [TestCaseSource(nameof(Wallets))]
        public void TrySpend_NotEnough_ReturnsFalseAndKeepsBalance(string kind, int start)
        {
            ICurrencyWallet sut = Create(kind);
            bool ok = sut.TrySpend(start + 1);

            Assert.IsFalse(ok);
            Assert.AreEqual(start, sut.Balance);
        }

        [TestCaseSource(nameof(Wallets))]
        public void TrySpend_Negative_ReturnsFalse(string kind, int start)
        {
            ICurrencyWallet sut = Create(kind);
            Assert.IsFalse(sut.TrySpend(-1));
            Assert.AreEqual(start, sut.Balance);
        }

        [Test]
        public void CoinAdd_RaisesCoinsChangedEvent()
        {
            ICurrencyWallet sut = new CoinWallet(_save);
            var events = new List<CoinsChangedEvent>();
            System.Action<CoinsChangedEvent> handler = events.Add;
            EventBus<CoinsChangedEvent>.Subscribe(handler);
            try
            {
                sut.Add(5);
                sut.TrySpend(30);
            }
            finally
            {
                EventBus<CoinsChangedEvent>.Unsubscribe(handler);
            }

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(105, events[0].Balance);
            Assert.AreEqual(5, events[0].Delta);
            Assert.AreEqual(75, events[1].Balance);
            Assert.AreEqual(-30, events[1].Delta);
        }

        [Test]
        public void StarSpend_RaisesStarsBalanceChangedEvent()
        {
            ICurrencyWallet sut = new StarWallet(_save);
            var events = new List<StarsBalanceChangedEvent>();
            System.Action<StarsBalanceChangedEvent> handler = events.Add;
            EventBus<StarsBalanceChangedEvent>.Subscribe(handler);
            try
            {
                sut.TrySpend(10);
                sut.TrySpend(100); // không đủ: không phát
            }
            finally
            {
                EventBus<StarsBalanceChangedEvent>.Unsubscribe(handler);
            }

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(10, events[0].Balance);
            Assert.AreEqual(-10, events[0].Delta);
        }

        [Test]
        public void TwoInstances_ShareSameSaveData()
        {
            ICurrencyWallet a = new CoinWallet(_save);
            ICurrencyWallet b = new CoinWallet(_save);

            a.Add(1);

            Assert.AreEqual(101, b.Balance);
            Assert.AreEqual(101, _economy.coins);
        }
    }
}
