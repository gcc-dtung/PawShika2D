using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>EditMode: số lượt booster qua IBoosterInventory (BoosterManager), save trong bộ nhớ.</summary>
    public class BoosterInventoryTests
    {
        private JsonFileSaveService _save;
        private GameplaySave _gameplay;
        private GameObject _go;
        private BoosterDatabase _db;
        private IBoosterInventory _sut;

        [SetUp]
        public void SetUp()
        {
            _save = new JsonFileSaveService(null);
            _gameplay = _save.Get<GameplaySave>(GameplaySave.Key);

            _db = ScriptableObject.CreateInstance<BoosterDatabase>();
            _db.boosterDatas = new List<BoosterData>
            {
                new BoosterData { boosterType = BoosterType.Hint, valueStart = 3 },
                new BoosterData { boosterType = BoosterType.Freeze, valueStart = 1 },
            };

            // EditMode: AddComponent không chạy Awake nên không đăng ký Singleton
            _go = new GameObject("BoosterManager (test)");
            BoosterManager manager = _go.AddComponent<BoosterManager>();
            manager.Init(_save, new BoosterInventory(_save), new Economy.CoinWallet(_save), null);
            var so = new SerializedObject(manager);
            so.FindProperty("database").objectReferenceValue = _db;
            so.ApplyModifiedPropertiesWithoutUndo();
            _sut = manager;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_db);
        }

        [Test]
        public void GetCount_FirstTime_ReturnsStartValueAndSaves()
        {
            int count = _sut.GetCount(BoosterType.Hint);

            Assert.AreEqual(3, count);
            Assert.AreEqual(3, _gameplay.GetBooster(BoosterType.Hint));
        }

        [Test]
        public void GetCount_NotInDatabase_ReturnsZero()
        {
            Assert.AreEqual(0, _sut.GetCount(BoosterType.Reveal));
        }

        [Test]
        public void Add_Positive_AddsToCurrent()
        {
            _sut.Add(BoosterType.Freeze, 4);

            Assert.AreEqual(5, _sut.GetCount(BoosterType.Freeze));
        }

        [Test]
        public void Add_ZeroOrNegative_Ignored()
        {
            _sut.Add(BoosterType.Hint, 0);
            _sut.Add(BoosterType.Hint, -2);

            Assert.AreEqual(3, _sut.GetCount(BoosterType.Hint));
        }

        [Test]
        public void Add_NotInDatabase_Ignored()
        {
            _sut.Add(BoosterType.Reveal, 5);

            Assert.AreEqual(0, _sut.GetCount(BoosterType.Reveal));
            Assert.AreEqual(-1, _gameplay.GetBooster(BoosterType.Reveal));
        }

        [Test]
        public void Add_KeepsExistingSavedCount()
        {
            _gameplay.SetBooster(BoosterType.Hint, 10);

            _sut.Add(BoosterType.Hint, 2);

            Assert.AreEqual(12, _sut.GetCount(BoosterType.Hint));
        }
    }
}
