using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Core.Contracts;
using PawBlock.Features.DailyReward;

namespace PawBlock.Game.Tests
{
    /// <summary>
    /// EditMode: SaveMigrator v1 -> v2 với 3 file save mẫu thật (Docs/Refactor/SaveSamples, P0).
    /// So từng field v1 với field tương ứng trong section v2 theo văn bản JSON (bỏ khoảng trắng), trên thư mục tạm.
    /// </summary>
    public class SaveMigratorTests
    {
        private static readonly string SamplesDir = Path.Combine(Application.dataPath, "Docs/Refactor/SaveSamples");

        /// <summary>Field v1 -> (section v2, field trong section; null = cả section).</summary>
        private static readonly Dictionary<string, (string section, string field)> Map = new Dictionary<string, (string, string)>
        {
            { "coins", ("economy", "coins") },
            { "stars", ("economy", "stars") },
            { "energy", ("economy", "energy") },
            { "noAds", ("shop", "noAds") },
            { "currentLevel", ("gameplay", "currentLevel") },
            { "boosters", ("gameplay", "boosters") },
            { "unlockedBoosters", ("gameplay", "unlockedBoosters") },
            { "seenMechanics", ("gameplay", "seenMechanics") },
            { "tutorialDone", ("gameplay", "tutorialDone") },
            { "allLevelsCleared", ("gameplay", "allLevelsCleared") },
            { "levelStats", ("gameplay", "levelStats") },
            { "daily", ("dailyReward", null) },
            { "challenge", ("dailyChallenge", null) },
            { "profile", ("profile", null) },
            { "settings", ("settings", null) },
            { "clock", ("clock", null) },
        };

        private static readonly HashSet<string> Dropped = new HashSet<string> { "version", "savedAt" };

        private string _dir;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PawBlockMigrateTests_" + Guid.NewGuid().ToString("N"));
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

        private static IEnumerable<string> Samples()
        {
            yield return "save_v1_new.json";
            yield return "save_v1_played.json";
            yield return "save_v1_infinite_energy.json";
        }

        [TestCaseSource(nameof(Samples))]
        public void Migrate_Sample_EveryV1FieldLandsInItsSection(string sample)
        {
            string v1Text = File.ReadAllText(Path.Combine(SamplesDir, sample));
            File.WriteAllText(_path, v1Text);

            bool migrated = SaveMigrator.Migrate(new JsonFileSaveService(_path), false);

            Assert.IsTrue(migrated);
            Assert.IsTrue(JsonSections.TryParse(v1Text, out Dictionary<string, string> v1));
            Assert.IsTrue(JsonSections.TryParse(File.ReadAllText(_path), out Dictionary<string, string> v2));
            Assert.AreEqual("2", v2["version"]);
            foreach (KeyValuePair<string, string> field in v1)
            {
                if (Dropped.Contains(field.Key))
                {
                    continue;
                }
                Assert.IsTrue(Map.TryGetValue(field.Key, out (string section, string field) to), $"Field v1 \"{field.Key}\" chưa có chỗ trong v2 (thêm vào SaveMigrator.ApplyV1 + Map)");
                Assert.IsTrue(v2.TryGetValue(to.section, out string sectionJson), $"Thiếu section {to.section}");
                string actual = sectionJson;
                if (to.field != null)
                {
                    Assert.IsTrue(JsonSections.TryParse(sectionJson, out Dictionary<string, string> inner));
                    Assert.IsTrue(inner.TryGetValue(to.field, out actual), $"Thiếu {to.section}.{to.field}");
                }
                Assert.AreEqual(Compact(field.Value), Compact(actual), $"{field.Key} -> {to.section}.{to.field}");
            }
        }

        [TestCaseSource(nameof(Samples))]
        public void Migrate_Sample_KeepsV1BackupByteForByte(string sample)
        {
            byte[] original = File.ReadAllBytes(Path.Combine(SamplesDir, sample));
            File.WriteAllBytes(_path, original);

            SaveMigrator.Migrate(new JsonFileSaveService(_path), false);

            CollectionAssert.AreEqual(original, File.ReadAllBytes(Path.Combine(_dir, SaveMigrator.V1BackupName)));
        }

        [Test]
        public void Migrate_Played_ReadBackThroughSections()
        {
            File.Copy(Path.Combine(SamplesDir, "save_v1_played.json"), _path);
            SaveMigrator.Migrate(new JsonFileSaveService(_path), false);

            var sut = new JsonFileSaveService(_path);
            EconomySave economy = sut.Get<EconomySave>(EconomySave.Key);
            GameplaySave gameplay = sut.Get<GameplaySave>(GameplaySave.Key);

            Assert.AreEqual(2, sut.FileVersion);
            Assert.AreEqual(2135, economy.coins);
            Assert.AreEqual(7, economy.stars);
            Assert.AreEqual(3, economy.energy.energy);
            Assert.AreEqual(22, gameplay.currentLevel);
            Assert.AreEqual(15, gameplay.GetBooster(Core.Contracts.BoosterType.Hint));
            Assert.AreEqual(16, gameplay.levelStats.Count);
            Assert.IsTrue(gameplay.tutorialDone);
        }

        [Test]
        public void Migrate_InfiniteEnergy_KeepsInfiniteUntil()
        {
            File.Copy(Path.Combine(SamplesDir, "save_v1_infinite_energy.json"), _path);
            SaveMigrator.Migrate(new JsonFileSaveService(_path), false);

            EnergySave energy = new JsonFileSaveService(_path).Get<EconomySave>(EconomySave.Key).energy;

            Assert.AreEqual(4102444800L, energy.infiniteUntil);
            Assert.AreEqual(0, energy.energy);
        }

        [Test]
        public void Migrate_AlreadyV2_DoesNothing()
        {
            File.Copy(Path.Combine(SamplesDir, "save_v1_played.json"), _path);
            SaveMigrator.Migrate(new JsonFileSaveService(_path), false);
            byte[] v2 = File.ReadAllBytes(_path);

            bool again = SaveMigrator.Migrate(new JsonFileSaveService(_path), false);

            Assert.IsFalse(again);
            CollectionAssert.AreEqual(v2, File.ReadAllBytes(_path));
        }

        [Test]
        public void Migrate_NoFile_NoPrefs_KeepsDefaultsAndWritesNothing()
        {
            var save = new JsonFileSaveService(_path);

            bool migrated = SaveMigrator.Migrate(save, false);

            Assert.IsFalse(migrated);
            Assert.IsFalse(File.Exists(_path));
            Assert.AreEqual(-1, save.Get<EconomySave>(EconomySave.Key).energy.energy);
        }

        [Test]
        public void Migrate_VeryOldFileWithoutNestedObjects_UsesDefaults()
        {
            File.WriteAllText(_path, "{ \"coins\": 12, \"currentLevel\": 3 }"); // không version, không energy / daily...

            var save = new JsonFileSaveService(_path);
            SaveMigrator.Migrate(save, false);

            Assert.AreEqual(12, save.Get<EconomySave>(EconomySave.Key).coins);
            Assert.AreEqual(-1, save.Get<EconomySave>(EconomySave.Key).energy.energy);
            Assert.AreEqual(3, save.Get<GameplaySave>(GameplaySave.Key).currentLevel);
            Assert.AreEqual(-1, save.Get<Features.DailyReward.DailyRewardSave>(Features.DailyReward.DailyRewardSave.Key).cycleStart);
        }

        /// <summary>
        /// Bỏ khoảng trắng ngoài chuỗi để so 2 JSON khác cách thụt lề. Số có phần thập phân so theo giá trị float32
        /// (field float của save; file mẫu ghi bằng Python khác Unity ở chữ số thứ 17); số nguyên giữ nguyên chữ.
        /// </summary>
        private static string Compact(string json)
        {
            var sb = new StringBuilder();
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (!inString && (c == '-' || char.IsDigit(c)))
                {
                    int start = i;
                    while (i + 1 < json.Length && "0123456789+-.eE".IndexOf(json[i + 1]) >= 0)
                    {
                        i++;
                    }
                    string number = json.Substring(start, i - start + 1);
                    bool isReal = number.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0;
                    sb.Append(isReal
                        ? ((float)double.Parse(number, System.Globalization.CultureInfo.InvariantCulture)).ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                        : number);
                    continue;
                }
                if (inString)
                {
                    sb.Append(c);
                    if (c == '\\' && i + 1 < json.Length)
                    {
                        sb.Append(json[++i]);
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else if (c == '"')
                {
                    inString = true;
                    sb.Append(c);
                }
                else if (!char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
