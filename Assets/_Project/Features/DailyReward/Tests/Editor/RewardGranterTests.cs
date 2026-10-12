using NUnit.Framework;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;

namespace PawBlock.Features.DailyReward.Tests
{
    /// <summary>EditMode: trao quà qua IRewardGranter (DailyRewardManager), save trong bộ nhớ.</summary>
    public class RewardGranterTests
    {
        private JsonFileSaveService _save;
        private Economy.EconomySave _economy;
        private GameObject _go;
        private IRewardGranter _sut;

        [SetUp]
        public void SetUp()
        {
            _save = new JsonFileSaveService(null);
            _economy = _save.Get<Economy.EconomySave>(Economy.EconomySave.Key);
            _economy.coins = 50;
            // EditMode: AddComponent không chạy Awake nên không đăng ký Singleton
            _go = new GameObject("DailyRewardManager (test)");
            DailyRewardManager manager = _go.AddComponent<DailyRewardManager>();
            manager.Init(_save, new Economy.CoinWallet(_save), null, null);
            _sut = manager;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Grant_Coin_AddsToWallet()
        {
            _sut.Grant(new DailyRewardItem { kind = DailyRewardKind.Coin, amount = 25 });

            Assert.AreEqual(75, _economy.coins);
        }

        [Test]
        public void Grant_CoinZero_Ignored()
        {
            _sut.Grant(new DailyRewardItem { kind = DailyRewardKind.Coin, amount = 0 });

            Assert.AreEqual(50, _economy.coins);
        }
    }
}
