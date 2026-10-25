using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Profile;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// Chuyển save đời cũ sang v2 (mỗi feature 1 section). Chạy 1 lần lúc mở game, trước khi feature đọc save.
    /// - v1 (SaveDataV1 phẳng, version 1): chia field về section, giữ file cũ thành save.v1.bak, ghi v2.
    /// - Không có file: chuyển PlayerPrefs của bản trước nữa (như SaveSystem cũ), ghi xong mới xóa key cũ.
    /// Nằm ở Game vì phải biết class save của mọi feature.
    /// </summary>
    public static class SaveMigrator
    {
        public const string V1BackupName = "save.v1.bak";

        // Key PlayerPrefs của bản trước v1 (chỉ dùng để chuyển dữ liệu)
        private const string OldLevelKey = "PawBlock_CurrentLevel";
        private const string OldCoinsKey = "PawBlock_Coins";
        private const string OldBoosterPrefix = "PawBlock_Booster_";
        private const string OldEnergyKey = "PawBlock_Energy";
        private const string OldRefillKey = "PawBlock_EnergyRefillStart";
        private const string OldInfiniteKey = "PawBlock_EnergyInfiniteUntil";
        private const string OldMusicKey = "PawBlock_Music";
        private const string OldSoundKey = "PawBlock_Sound";
        private const string OldVibrateKey = "PawBlock_Vibrate";

        /// <summary>
        /// Chuyển nếu cần. Trả về true nếu đã chuyển (save giờ là v2 trong bộ nhớ và đã ghi file).
        /// <paramref name="usePlayerPrefs"/> = false: không đọc / xóa PlayerPrefs (test).
        /// </summary>
        public static bool Migrate(JsonFileSaveService save, bool usePlayerPrefs = true)
        {
            if (save.FileVersion >= JsonFileSaveService.CurrentVersion)
            {
                return false;
            }

            SaveDataV1 v1;
            bool fromPrefs = false;
            if (save.LoadedText != null)
            {
                v1 = ReadV1(save.LoadedText);
                if (v1 == null)
                {
                    Debug.LogError("[SaveMigrator] Không đọc được save cũ, giữ nguyên file, bắt đầu với dữ liệu mặc định");
                    return false;
                }
                BackupV1(save);
            }
            else
            {
                v1 = new SaveDataV1();
                if (!usePlayerPrefs || !ReadPlayerPrefs(v1))
                {
                    return false; // người mới: section mặc định, ghi khi có thay đổi đầu tiên
                }
                fromPrefs = true;
            }

            ApplyV1(save, v1);
            save.MarkDirty();
            save.SaveNow(); // Play mode: MarkDirty chỉ hẹn ghi, ghi luôn để file là v2 ngay
            if (fromPrefs && !save.IsDirty)
            {
                DeletePlayerPrefs(); // chỉ xóa key cũ khi đã ghi file thành công
            }
            Debug.Log($"[SaveMigrator] Đã chuyển save {(fromPrefs ? "PlayerPrefs" : "v1")} sang v2");
            return true;
        }

        /// <summary>Chia dữ liệu v1 về section v2 (thứ tự gọi = thứ tự trong file).</summary>
        public static void ApplyV1(JsonFileSaveService save, SaveDataV1 v1)
        {
            save.Set(EconomySave.Key, new EconomySave { coins = v1.coins, stars = v1.stars, energy = v1.energy });
            save.Set(ShopSave.Key, new ShopSave { noAds = v1.noAds });
            save.Set(GameplaySave.Key, new GameplaySave
            {
                currentLevel = v1.currentLevel,
                boosters = v1.boosters,
                unlockedBoosters = v1.unlockedBoosters,
                seenMechanics = v1.seenMechanics,
                tutorialDone = v1.tutorialDone,
                allLevelsCleared = v1.allLevelsCleared,
                levelStats = v1.levelStats,
            });
            save.Set(DailyRewardSave.Key, v1.daily);
            save.Set(DailyChallengeSave.Key, v1.challenge);
            save.Set(ProfileSave.Key, v1.profile);
            save.Set(SettingsSave.Key, v1.settings);
            save.Set(ClockSave.Key, v1.clock);
        }

        /// <summary>Đọc JSON v1; object lồng nhau thiếu (file rất cũ) thì dùng mặc định.</summary>
        public static SaveDataV1 ReadV1(string json)
        {
            SaveDataV1 data;
            try
            {
                data = JsonUtility.FromJson<SaveDataV1>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveMigrator] JSON v1 lỗi: {e.Message}");
                return null;
            }
            if (data == null)
            {
                return null;
            }
            if (data.boosters == null) data.boosters = new List<BoosterSave>();
            if (data.energy == null) data.energy = new EnergySave();
            if (data.settings == null) data.settings = new SettingsSave();
            if (data.daily == null) data.daily = new DailyRewardSave();
            if (data.challenge == null) data.challenge = new DailyChallengeSave();
            if (data.clock == null) data.clock = new ClockSave();
            if (data.profile == null) data.profile = new ProfileSave();
            if (data.unlockedBoosters == null) data.unlockedBoosters = new List<string>();
            if (data.seenMechanics == null) data.seenMechanics = new List<string>();
            if (data.levelStats == null) data.levelStats = new List<LevelStatSave>();
            return data;
        }

        private static void BackupV1(JsonFileSaveService save)
        {
            if (save.FilePath == null || save.LoadedFrom == null)
            {
                return;
            }
            string backup = Path.Combine(Path.GetDirectoryName(save.FilePath), V1BackupName);
            try
            {
                File.Copy(save.LoadedFrom, backup, true); // bản đã đọc được (có thể là .bak nếu save.json hỏng)
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveMigrator] Không giữ được {V1BackupName}: {e.Message}");
            }
        }

        // ---------- PlayerPrefs của bản trước v1 ----------

        private static bool ReadPlayerPrefs(SaveDataV1 data)
        {
            bool any = false;
            if (PlayerPrefs.HasKey(OldLevelKey)) { data.currentLevel = PlayerPrefs.GetInt(OldLevelKey); any = true; }
            if (PlayerPrefs.HasKey(OldCoinsKey)) { data.coins = PlayerPrefs.GetInt(OldCoinsKey); any = true; }
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                string key = OldBoosterPrefix + type;
                if (!PlayerPrefs.HasKey(key)) continue;
                data.SetBooster(type, PlayerPrefs.GetInt(key));
                any = true;
            }
            if (PlayerPrefs.HasKey(OldEnergyKey))
            {
                data.energy.energy = PlayerPrefs.GetInt(OldEnergyKey);
                data.energy.refillStart = long.TryParse(PlayerPrefs.GetString(OldRefillKey, "0"), out long r) ? r : 0;
                data.energy.infiniteUntil = long.TryParse(PlayerPrefs.GetString(OldInfiniteKey, "0"), out long i) ? i : 0;
                any = true;
            }
            if (PlayerPrefs.HasKey(OldMusicKey)) { data.settings.music = PlayerPrefs.GetInt(OldMusicKey) == 1; any = true; }
            if (PlayerPrefs.HasKey(OldSoundKey)) { data.settings.sound = PlayerPrefs.GetInt(OldSoundKey) == 1; any = true; }
            if (PlayerPrefs.HasKey(OldVibrateKey)) { data.settings.vibrate = PlayerPrefs.GetInt(OldVibrateKey) == 1; any = true; }
            return any;
        }

        private static void DeletePlayerPrefs()
        {
            foreach (string key in new[] { OldLevelKey, OldCoinsKey, OldEnergyKey, OldRefillKey, OldInfiniteKey, OldMusicKey, OldSoundKey, OldVibrateKey })
            {
                PlayerPrefs.DeleteKey(key);
            }
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                PlayerPrefs.DeleteKey(OldBoosterPrefix + type);
            }
            PlayerPrefs.Save();
        }
    }
}
