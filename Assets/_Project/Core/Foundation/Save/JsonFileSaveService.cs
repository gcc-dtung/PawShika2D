using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Save v2: 1 file JSON, cấp ngoài là "version", "savedAt" và các section theo key:
    /// { "version": 2, "savedAt": ..., "economy": { ... }, "gameplay": { ... } }.
    /// - Đọc: save.json -> .tmp -> .bak (file hỏng / app tắt giữa lúc ghi vẫn còn bản dùng được).
    /// - Ghi an toàn: ghi .tmp, giữ bản cũ thành .bak, rồi đổi tên .tmp thành save.json.
    /// - Section chưa ai Get vẫn được ghi lại nguyên văn (không mất dữ liệu của feature chưa nạp / đã bỏ).
    /// - path = null: chỉ trong bộ nhớ, không đọc / ghi file (test).
    /// File đời cũ (version &lt; 2) không tự đọc thành section: <see cref="FileVersion"/>, <see cref="LoadedText"/>
    /// để migrator (Game) chuyển, rồi <see cref="Set{T}"/> từng section.
    /// </summary>
    public class JsonFileSaveService : ISaveService
    {
        public const int CurrentVersion = 2;
        private const string VersionKey = "version";
        private const string SavedAtKey = "savedAt";

        private readonly string _path;
        private readonly Dictionary<string, string> _raw = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _live = new Dictionary<string, object>();
        private readonly List<string> _order = new List<string>();
        private bool _dirty;
        private bool _migrated; // file đời cũ đã được migrator chuyển (Set): mới cho ghi

        /// <summary>Đường dẫn save của game: persistentDataPath/save.json.</summary>
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

        /// <summary>Version của file đã đọc (0 = không có file / file không có version).</summary>
        public int FileVersion { get; private set; }

        /// <summary>Nguyên văn file đã đọc (null nếu không có), cho migrator đọc định dạng cũ.</summary>
        public string LoadedText { get; private set; }

        /// <summary>Đọc được file nào (null = không có / chỉ bộ nhớ).</summary>
        public string LoadedFrom { get; private set; }

        /// <summary>File đời cũ chưa migrate: không được ghi (ghi bây giờ sẽ mất các section chưa chuyển).</summary>
        public bool NeedsMigration => LoadedText != null && FileVersion < CurrentVersion && !_migrated;

        public bool IsDirty => _dirty;
        public string FilePath => _path;
        private string TempPath => _path + ".tmp";
        private string BackupPath => _path + ".bak";

        public JsonFileSaveService(string path)
        {
            _path = path;
            if (_path != null)
            {
                Load();
            }
        }

        // ---------- ISaveService ----------

        public T Get<T>(string key) where T : class, new()
        {
            if (_live.TryGetValue(key, out object obj))
            {
                if (obj is T typed)
                {
                    return typed;
                }
                throw new InvalidOperationException($"[Save] Section \"{key}\" đã nạp kiểu {obj.GetType().Name}, không phải {typeof(T).Name}");
            }

            var data = new T();
            if (_raw.TryGetValue(key, out string json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, data); // field thiếu trong file giữ giá trị mặc định
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Save] Section \"{key}\" lỗi, dùng mặc định: {e.Message}");
                }
            }
            Track(key, data);
            return data;
        }

        public void MarkDirty()
        {
            if (_path == null)
            {
                return;
            }
            _dirty = true;
            if (Application.isPlaying)
            {
                SaveRunner.Ensure(this);
            }
            else
            {
                SaveNow();
            }
        }

        public void SaveNow()
        {
            if (_path == null || !_dirty)
            {
                return;
            }
            if (NeedsMigration)
            {
                Debug.LogError($"[Save] {Path.GetFileName(_path)} là save v{FileVersion}, chưa chuyển sang v{CurrentVersion}: không ghi để khỏi mất dữ liệu (chạy game 1 lần để migrate)");
                return;
            }
            if (Write(BuildText()))
            {
                _dirty = false;
            }
        }

        // ---------- Cho migrator / công cụ ----------

        /// <summary>Thay cả section bằng <paramref name="data"/> (migrator). Không tự MarkDirty.</summary>
        public void Set<T>(string key, T data) where T : class
        {
            _raw.Remove(key);
            Track(key, data);
            _migrated = true;
        }

        public bool HasSection(string key) => _live.ContainsKey(key) || _raw.ContainsKey(key);

        /// <summary>Chép file đang có sang <paramref name="backupPath"/> (vd. giữ save v1 trước khi ghi đè bằng v2).</summary>
        public bool CopyFileTo(string backupPath)
        {
            if (_path == null || !File.Exists(_path))
            {
                return false;
            }
            try
            {
                File.Copy(_path, backupPath, true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Không chép được {Path.GetFileName(_path)} sang {Path.GetFileName(backupPath)}: {e.Message}");
                return false;
            }
        }

        /// <summary>Văn bản sẽ ghi ra file (version, savedAt, các section).</summary>
        public string BuildText()
        {
            var parts = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(VersionKey, CurrentVersion.ToString()),
                new KeyValuePair<string, string>(SavedAtKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            };
            foreach (string key in _order)
            {
                string value = _live.TryGetValue(key, out object obj)
                    ? JsonSections.IndentValue(JsonUtility.ToJson(obj, true))
                    : _raw[key];
                parts.Add(new KeyValuePair<string, string>(key, value));
            }
            return JsonSections.Build(parts);
        }

        /// <summary>Xóa save (nút reset / Editor): bộ nhớ về rỗng, lần ghi sau tạo file mới.</summary>
        public void Delete()
        {
            if (_path != null)
            {
                foreach (string p in new[] { _path, TempPath, BackupPath })
                {
                    if (File.Exists(p))
                    {
                        File.Delete(p);
                    }
                }
            }
            _raw.Clear();
            _live.Clear();
            _order.Clear();
            _dirty = false;
            _migrated = false;
            FileVersion = 0;
            LoadedText = null;
            LoadedFrom = null;
        }

        // ---------- Đọc ----------

        private void Track(string key, object data)
        {
            _live[key] = data;
            if (!_order.Contains(key))
            {
                _order.Add(key);
            }
        }

        private void Load()
        {
            foreach (string path in new[] { _path, TempPath, BackupPath })
            {
                if (!TryRead(path, out string text, out Dictionary<string, string> top))
                {
                    continue;
                }
                if (path != _path)
                {
                    Debug.LogWarning($"[Save] {Path.GetFileName(_path)} lỗi / thiếu, đọc bản dự phòng {Path.GetFileName(path)}");
                    _dirty = true; // ghi lại thành file chính
                }
                LoadedText = text;
                LoadedFrom = path;
                FileVersion = top.TryGetValue(VersionKey, out string v) && int.TryParse(v, out int n) ? n : 0;
                if (FileVersion < CurrentVersion)
                {
                    return; // định dạng cũ: để migrator đọc LoadedText
                }
                foreach (KeyValuePair<string, string> s in top)
                {
                    if (s.Key == VersionKey || s.Key == SavedAtKey)
                    {
                        continue;
                    }
                    _raw[s.Key] = s.Value;
                    _order.Add(s.Key);
                }
                return;
            }
        }

        private static bool TryRead(string path, out string text, out Dictionary<string, string> top)
        {
            text = null;
            top = null;
            if (!File.Exists(path))
            {
                return false;
            }
            try
            {
                text = File.ReadAllText(path);
                return !string.IsNullOrWhiteSpace(text) && JsonSections.TryParse(text, out top);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Không đọc được {Path.GetFileName(path)}: {e.Message}");
                return false;
            }
        }

        // ---------- Ghi ----------

        private bool Write(string text)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(TempPath, text);
                if (File.Exists(_path))
                {
                    File.Copy(_path, BackupPath, true);
                    File.Delete(_path);
                }
                File.Move(TempPath, _path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Ghi save lỗi: {e.Message}");
                return false;
            }
        }
    }
}
