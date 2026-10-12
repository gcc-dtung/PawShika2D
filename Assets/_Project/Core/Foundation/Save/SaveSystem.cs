using System;
using System.IO;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Đọc / ghi SaveData ra Application.persistentDataPath/save.json.
    /// - Data: nạp lười lần đầu dùng. Không có file: chuyển dữ liệu PlayerPrefs cũ (bản trước) sang, rồi xóa key cũ.
    /// - MarkDirty(): báo có thay đổi; SaveRunner gom lại ghi sau flushDelay giây và luôn ghi khi app vào nền / thoát.
    ///   Ngoài Play mode (Editor) thì ghi ngay.
    /// - Ghi an toàn: ghi save.json.tmp, giữ bản cũ thành save.json.bak, rồi đổi tên tmp thành save.json.
    ///   Đọc: save.json -> .tmp -> .bak (file hỏng / app tắt giữa lúc ghi vẫn còn bản dùng được).
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        // Key PlayerPrefs của bản trước (chỉ dùng để chuyển dữ liệu)
        private const string OldLevelKey = "PawBlock_CurrentLevel";
        private const string OldCoinsKey = "PawBlock_Coins";
        private const string OldBoosterPrefix = "PawBlock_Booster_";
        private const string OldEnergyKey = "PawBlock_Energy";
        private const string OldRefillKey = "PawBlock_EnergyRefillStart";
        private const string OldInfiniteKey = "PawBlock_EnergyInfiniteUntil";
        private const string OldMusicKey = "PawBlock_Music";
        private const string OldSoundKey = "PawBlock_Sound";
        private const string OldVibrateKey = "PawBlock_Vibrate";

        private static SaveData _data;
        private static bool _dirty;
        private static bool _memoryOnly; // test: không đọc / ghi file (UseInMemoryForTests)

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => FilePath + ".tmp";
        private static string BackupPath => FilePath + ".bak";

        public static bool IsDirty => _dirty;

        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        // Tắt Domain Reload khi vào Play mode: static không tự reset, phải xóa tay để đọc lại file
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _data = null;
            _dirty = false;
            _memoryOnly = false;
        }

        /// <summary>
        /// Chỉ cho test: dùng <paramref name="data"/> trong bộ nhớ, không đọc / ghi save.json của máy.
        /// Gọi lại với null khi xong để lần dùng sau đọc lại file như bình thường. Bỏ khi có ISaveService (P3 Refactor).
        /// </summary>
        public static void UseInMemoryForTests(SaveData data)
        {
            _data = data;
            _memoryOnly = data != null;
            _dirty = false;
        }

        /// <summary>Có thay đổi cần ghi.</summary>
        public static void MarkDirty()
        {
            if (_memoryOnly) return;
            _dirty = true;
            if (Application.isPlaying) SaveRunner.Ensure();
            else Save();
        }

        /// <summary>Ghi ngay (nếu có thay đổi, hoặc force).</summary>
        public static void Save(bool force = false)
        {
            if (_data == null || _memoryOnly || (!_dirty && !force)) return;
            _data.version = SaveData.CurrentVersion;
            _data.savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Write(JsonUtility.ToJson(_data, true))) _dirty = false;
        }

        /// <summary>Xóa save (Editor / nút reset): lần dùng sau bắt đầu lại từ đầu.</summary>
        public static void Delete()
        {
            foreach (string p in new[] { FilePath, TempPath, BackupPath })
            {
                if (File.Exists(p)) File.Delete(p);
            }
            _data = null;
            _dirty = false;
        }

        // ---------- Đọc ----------

        private static void Load()
        {
            foreach (string path in new[] { FilePath, TempPath, BackupPath })
            {
                if (TryRead(path, out SaveData data))
                {
                    if (path != FilePath) Debug.LogWarning($"[SaveSystem] save.json lỗi / thiếu, đọc bản dự phòng {Path.GetFileName(path)}");
                    _data = Upgrade(data);
                    _dirty = path != FilePath; // khôi phục từ bản dự phòng: ghi lại thành file chính
                    return;
                }
            }

            _data = new SaveData();
            if (MigrateFromPlayerPrefs(_data))
            {
                _dirty = true;
                Save();
                if (!_dirty) DeleteOldPlayerPrefs(); // chỉ xóa key cũ khi đã ghi file thành công
                Debug.Log("[SaveSystem] Đã chuyển dữ liệu PlayerPrefs cũ sang " + FilePath);
            }
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<SaveData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Không đọc được {Path.GetFileName(path)}: {e.Message}");
                return false;
            }
        }

        /// <summary>Chuyển save đời cũ lên CurrentVersion. Thêm case khi đổi cấu trúc.</summary>
        private static SaveData Upgrade(SaveData data)
        {
            // JsonUtility để null cho object lồng nhau thiếu trong file cũ
            if (data.boosters == null) data.boosters = new System.Collections.Generic.List<BoosterSave>();
            if (data.energy == null) data.energy = new EnergySave();
            if (data.settings == null) data.settings = new SettingsSave();
            if (data.daily == null) data.daily = new DailySave();
            if (data.clock == null) data.clock = new ClockSave();
            if (data.profile == null) data.profile = new ProfileSave();
            if (data.levelStats == null) data.levelStats = new System.Collections.Generic.List<LevelStatSave>();

            // if (data.version < 2) { ... chuyển đổi ...; data.version = 2; }
            data.version = SaveData.CurrentVersion;
            return data;
        }

        // ---------- Ghi ----------

        private static bool Write(string json)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(TempPath, json);
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, BackupPath, true);
                    File.Delete(FilePath);
                }
                File.Move(TempPath, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Ghi save lỗi: {e.Message}");
                return false;
            }
        }

        // ---------- Chuyển dữ liệu PlayerPrefs cũ ----------

        private static bool MigrateFromPlayerPrefs(SaveData data)
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

        private static void DeleteOldPlayerPrefs()
        {
            foreach (string key in new[] { OldLevelKey, OldCoinsKey, OldEnergyKey, OldRefillKey, OldInfiniteKey, OldMusicKey, OldSoundKey, OldVibrateKey })
                PlayerPrefs.DeleteKey(key);
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
                PlayerPrefs.DeleteKey(OldBoosterPrefix + type);
            PlayerPrefs.Save();
        }
    }
}
