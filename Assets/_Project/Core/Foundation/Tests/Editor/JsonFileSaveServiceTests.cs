using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace PawBlock.Core.Foundation.Tests
{
    /// <summary>EditMode: JsonSections (tách cấp ngoài) và JsonFileSaveService (section, ghi an toàn) trên thư mục tạm.</summary>
    public class JsonFileSaveServiceTests
    {
        [Serializable]
        private class SampleSave
        {
            public int coins;
            public string name = "Player";
            public List<int> days = new List<int>();
            public Nested nested = new Nested();
        }

        [Serializable]
        private class Nested
        {
            public long until = -1;
        }

        private string _dir;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PawBlockSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        // ---------- JsonSections ----------

        [Test]
        public void Parse_TopLevelObject_ReturnsRawValues()
        {
            const string json = "{ \"version\": 2, \"a\": { \"x\": [1, {\"y\": \"}\"}] }, \"b\": \"q\\\"u{o\", \"c\": true, \"d\": null }";

            bool ok = JsonSections.TryParse(json, out Dictionary<string, string> s);

            Assert.IsTrue(ok);
            Assert.AreEqual("2", s["version"]);
            Assert.AreEqual("{ \"x\": [1, {\"y\": \"}\"}] }", s["a"]);
            Assert.AreEqual("\"q\\\"u{o\"", s["b"]);
            Assert.AreEqual("true", s["c"]);
            Assert.AreEqual("null", s["d"]);
        }

        [TestCase("")]
        [TestCase("[1, 2]")]
        [TestCase("{ \"a\": 1 ")]
        [TestCase("{ \"a\" 1 }")]
        [TestCase("{ \"a\": { \"b\": 1 }")]
        [TestCase("{ \"a\": 1 } trailing")]
        public void Parse_Invalid_ReturnsFalse(string json)
        {
            Assert.IsFalse(JsonSections.TryParse(json, out _));
        }

        [Test]
        public void Parse_EmptyObject_ReturnsTrueAndEmpty()
        {
            Assert.IsTrue(JsonSections.TryParse(" { } ", out Dictionary<string, string> s));
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void Build_ThenParse_RoundTrips()
        {
            var parts = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("version", "2"),
                new KeyValuePair<string, string>("x", JsonSections.IndentValue("{\n    \"a\": 1\n}")),
            };

            string text = JsonSections.Build(parts);

            Assert.IsTrue(JsonSections.TryParse(text, out Dictionary<string, string> s));
            Assert.AreEqual("2", s["version"]);
            Assert.AreEqual(parts[1].Value, s["x"]);
        }

        // ---------- JsonFileSaveService ----------

        [Test]
        public void Get_NoFile_ReturnsDefaultsAndSameInstance()
        {
            var sut = new JsonFileSaveService(_path);

            SampleSave a = sut.Get<SampleSave>("sample");
            SampleSave b = sut.Get<SampleSave>("sample");

            Assert.AreEqual(0, sut.FileVersion);
            Assert.AreSame(a, b);
            Assert.AreEqual("Player", a.name);
            Assert.AreEqual(-1, a.nested.until);
        }

        [Test]
        public void MarkDirty_WritesV2File_ReadBackByNewService()
        {
            var sut = new JsonFileSaveService(_path);
            SampleSave data = sut.Get<SampleSave>("sample");
            data.coins = 42;
            data.days.Add(20737);
            data.nested.until = 123;

            sut.MarkDirty(); // ngoài Play mode: ghi ngay

            var reloaded = new JsonFileSaveService(_path);
            SampleSave back = reloaded.Get<SampleSave>("sample");
            Assert.AreEqual(JsonFileSaveService.CurrentVersion, reloaded.FileVersion);
            Assert.AreEqual(42, back.coins);
            CollectionAssert.AreEqual(new[] { 20737 }, back.days);
            Assert.AreEqual(123, back.nested.until);
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void MissingFieldsInFile_KeepDefaults()
        {
            File.WriteAllText(_path, "{ \"version\": 2, \"sample\": { \"coins\": 7 } }");

            SampleSave data = new JsonFileSaveService(_path).Get<SampleSave>("sample");

            Assert.AreEqual(7, data.coins);
            Assert.AreEqual("Player", data.name);
            Assert.IsNotNull(data.nested);
            Assert.AreEqual(-1, data.nested.until);
        }

        [Test]
        public void UntouchedSection_IsKeptVerbatim()
        {
            File.WriteAllText(_path, "{ \"version\": 2, \"other\": { \"keep\": [1, 2, 3] }, \"sample\": { \"coins\": 1 } }");
            var sut = new JsonFileSaveService(_path);
            sut.Get<SampleSave>("sample").coins = 2;

            sut.MarkDirty();
            sut.MarkDirty();

            Assert.IsTrue(JsonSections.TryParse(File.ReadAllText(_path), out Dictionary<string, string> s));
            Assert.AreEqual("{ \"keep\": [1, 2, 3] }", s["other"]);
            Assert.AreEqual(2, new JsonFileSaveService(_path).Get<SampleSave>("sample").coins);
        }

        [Test]
        public void SecondWrite_KeepsPreviousAsBak()
        {
            var sut = new JsonFileSaveService(_path);
            SampleSave data = sut.Get<SampleSave>("sample");
            data.coins = 1;
            sut.MarkDirty();
            data.coins = 2;
            sut.MarkDirty();

            Assert.AreEqual(1, new JsonFileSaveService(_path + ".bak").Get<SampleSave>("sample").coins);
            Assert.AreEqual(2, new JsonFileSaveService(_path).Get<SampleSave>("sample").coins);
        }

        [Test]
        public void CorruptMain_FallsBackToBak_AndMarksDirty()
        {
            File.WriteAllText(_path + ".bak", "{ \"version\": 2, \"sample\": { \"coins\": 9 } }");
            File.WriteAllText(_path, "{ broken");

            var sut = new JsonFileSaveService(_path);

            Assert.AreEqual(_path + ".bak", sut.LoadedFrom);
            Assert.IsTrue(sut.IsDirty);
            Assert.AreEqual(9, sut.Get<SampleSave>("sample").coins);
        }

        [Test]
        public void OldVersionFile_NotSplit_ExposesTextForMigrator()
        {
            const string v1 = "{ \"version\": 1, \"coins\": 5, \"energy\": { \"energy\": 3 } }";
            File.WriteAllText(_path, v1);

            var sut = new JsonFileSaveService(_path);

            Assert.AreEqual(1, sut.FileVersion);
            Assert.AreEqual(v1, sut.LoadedText);
            Assert.IsFalse(sut.HasSection("energy"));
        }

        [Test]
        public void Set_ReplacesSection()
        {
            var sut = new JsonFileSaveService(_path);
            var data = new SampleSave { coins = 77 };

            sut.Set("sample", data);

            Assert.AreSame(data, sut.Get<SampleSave>("sample"));
        }

        [Test]
        public void Get_WrongType_Throws()
        {
            var sut = new JsonFileSaveService(_path);
            sut.Get<SampleSave>("sample");

            Assert.Throws<InvalidOperationException>(() => sut.Get<Nested>("sample"));
        }

        [Test]
        public void MemoryOnly_NeverWrites()
        {
            var sut = new JsonFileSaveService(null);
            sut.Get<SampleSave>("sample").coins = 3;

            sut.MarkDirty();
            sut.SaveNow();

            Assert.IsFalse(sut.IsDirty);
            Assert.AreEqual(3, sut.Get<SampleSave>("sample").coins);
            Assert.AreEqual(0, Directory.GetFiles(_dir).Length);
        }

        [Test]
        public void Delete_RemovesFilesAndData()
        {
            var sut = new JsonFileSaveService(_path);
            sut.Get<SampleSave>("sample").coins = 1;
            sut.MarkDirty();

            sut.Delete();

            Assert.IsFalse(File.Exists(_path));
            Assert.AreEqual(0, sut.Get<SampleSave>("sample").coins);
        }
    }
}
