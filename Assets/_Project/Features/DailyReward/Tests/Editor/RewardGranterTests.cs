using NUnit.Framework;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.DailyReward.Tests
{
    /// <summary>EditMode: trao quà qua IRewardGranter (DailyRewardManager), save trong bộ nhớ.</summary>
    public class RewardGranterTests
    {
        private SaveData _save;
        private GameObject _go;
        private IRewardGranter _sut;

        [SetUp]
        public void SetUp()
        {
            _save = new SaveData { coins = 50 };
            SaveSystem.UseInMemoryForTests(_save);
            // EditMode: AddComponent không chạy Awake nên không đăng ký Singleton
            _go = new GameObject("DailyRewardManager (test)");
            _sut = _go.AddComponent<DailyRewardManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            SaveSystem.UseInMemoryForTests(null);
        }

        [Test]
        public void Grant_Coin_AddsToWallet()
        {
            _sut.Grant(new DailyRewardItem { kind = DailyRewardKind.Coin, amount = 25 });

            Assert.AreEqual(75, _save.coins);
        }

        [Test]
        public void Grant_CoinZero_Ignored()
        {
            _sut.Grant(new DailyRewardItem { kind = DailyRewardKind.Coin, amount = 0 });

            Assert.AreEqual(50, _save.coins);
        }
    }
}
