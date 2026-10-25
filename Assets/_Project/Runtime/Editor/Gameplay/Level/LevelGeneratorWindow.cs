using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Tools → PawBlock → Level Generator: sinh hàng loạt level Shikaku bằng LevelGenerator (cùng code Daily Challenge dùng lúc chạy).
    /// - Chọn bộ thông số (Easy / Normal / Hard / Daily) hoặc chỉnh tay, seed đầu + số level -> Sinh.
    /// - Mỗi level: kích thước, số ô số, đúng 1 lời giải, độ khó theo LevelDifficultyAnalyzer (điểm, giây dự kiến), thời gian sinh.
    /// - Xem trước lưới (bật / tắt lời giải); sắp theo độ khó; lưu level chọn / tất cả vào Levels/Generated,
    ///   tùy chọn thêm vào cuối LevelDatabase; mở trong Level Editor.
    /// - Gắn cơ chế ngay khi sinh (tùy chọn): chọn loại, dùng chung MechanicInjector.Apply (số lượng theo MechanicInjectorSettings).
    ///   Level cho database chính thì nên gắn bằng Mechanic Injector (theo lịch giới thiệu).
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        public const string OutputFolder = "Assets/_Project/Data/Levels/Generated";
        private const string DatabasePath = "Assets/_Project/Data/Levels/LevelDatabase.asset";
        private const string DifficultySettingsPath = "Assets/_Project/Data/Levels/LevelDifficultySettings.asset";
        private const string InjectorSettingsPath = "Assets/_Project/Data/Levels/MechanicInjectorSettings.asset";

        private class Entry
        {
            public int seed;
            public LevelDefinition level;  // trong bộ nhớ cho tới khi lưu
            public bool saved;
            public bool unique;
            public int tries;
            public long ms;
            public float score, seconds;
            public List<string> mechanics = new List<string>();
        }

        private LevelGeneratorPreset _preset = LevelGeneratorPreset.Normal;
        private LevelGeneratorSettings _settings = LevelGeneratorSettings.Preset(LevelGeneratorPreset.Normal);
        private int _seed = 1;
        private int _count = 10;
        private bool _appendToDatabase;
        private bool _showSolution = true;

        private readonly List<Entry> _entries = new List<Entry>();
        private int _selected = -1;
        private Vector2 _listScroll;
        private LevelDifficultySettings _difficulty;
        private MechanicInjectorSettings _injector;
        private readonly HashSet<MechanicKind> _mechanics = new HashSet<MechanicKind>();

        [MenuItem("Tools/PawBlock/Level Generator", priority = 5)]
        public static void Open()
        {
            LevelGeneratorWindow w = GetWindow<LevelGeneratorWindow>("Level Generator");
            w.minSize = new Vector2(640, 560);
        }

        private void OnEnable()
        {
            _difficulty = AssetDatabase.LoadAssetAtPath<LevelDifficultySettings>(DifficultySettingsPath);
            _injector = AssetDatabase.LoadAssetAtPath<MechanicInjectorSettings>(InjectorSettingsPath);
        }

        private void OnDisable() => ClearUnsaved();

        private void OnGUI()
        {
            DrawSettings();
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(330))) DrawList();
                using (new EditorGUILayout.VerticalScope()) DrawPreview();
            }
        }

        // ---------- Thông số ----------

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Thông số", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _preset = (LevelGeneratorPreset)EditorGUILayout.EnumPopup("Bộ có sẵn", _preset);
            if (EditorGUI.EndChangeCheck()) _settings = LevelGeneratorSettings.Preset(_preset);

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.width = Mathf.Clamp(EditorGUILayout.IntField("Rộng x Cao", _settings.width), 2, 16);
                _settings.height = Mathf.Clamp(EditorGUILayout.IntField(_settings.height, GUILayout.Width(60)), 2, 16);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.minArea = Mathf.Max(1, EditorGUILayout.IntField("Số ô / khối (min - max)", _settings.minArea));
                _settings.maxArea = Mathf.Max(_settings.minArea, EditorGUILayout.IntField(_settings.maxArea, GUILayout.Width(60)));
            }
            _settings.maxSide = Mathf.Max(1, EditorGUILayout.IntField("Cạnh dài nhất", _settings.maxSide));
            _settings.maxSingles = Mathf.Max(0, EditorGUILayout.IntField("Khối 1 ô tối đa", _settings.maxSingles));
            _settings.requireUnique = EditorGUILayout.Toggle("Đúng 1 lời giải", _settings.requireUnique);

            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed đầu", _seed);
                if (GUILayout.Button("Ngẫu nhiên", GUILayout.Width(90))) _seed = Random.Range(1, int.MaxValue / 2);
            }
            _count = Mathf.Clamp(EditorGUILayout.IntField("Số level", _count), 1, 500);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Gắn cơ chế", "Gắn ngay khi sinh (MechanicInjector.Apply), số lượng theo MechanicInjectorSettings"), GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                GUI.enabled = _injector != null;
                foreach (MechanicKind kind in (MechanicKind[])System.Enum.GetValues(typeof(MechanicKind)))
                {
                    bool on = EditorGUILayout.ToggleLeft(kind.ToString(), _mechanics.Contains(kind), GUILayout.Width(84));
                    if (on) _mechanics.Add(kind); else _mechanics.Remove(kind);
                }
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sinh", GUILayout.Height(26))) Generate();
                GUI.enabled = _entries.Count > 1;
                if (GUILayout.Button("Sắp theo độ khó", GUILayout.Height(26))) SortByDifficulty();
                GUI.enabled = true;
            }
        }

        private void Generate()
        {
            ClearUnsaved();
            _entries.Clear();
            _selected = -1;
            Stopwatch sw = new Stopwatch();
            try
            {
                for (int i = 0; i < _count; i++)
                {
                    int seed = _seed + i;
                    if (EditorUtility.DisplayCancelableProgressBar("Level Generator", $"Seed {seed} ({i + 1}/{_count})", (float)i / _count)) break;
                    sw.Restart();
                    LevelGenerator.Result r = LevelGenerator.Generate(_settings, seed);
                    sw.Stop();
                    if (r.level == null)
                    {
                        UnityEngine.Debug.LogWarning($"[LevelGenerator] Seed {seed}: không sinh được sau {r.partitions} lần chia lưới.");
                        continue;
                    }
                    Entry e = new Entry { seed = seed, level = r.level, unique = r.unique, tries = r.clueTries, ms = sw.ElapsedMilliseconds };
                    if (_mechanics.Count > 0 && _injector != null)
                        e.mechanics = MechanicInjector.Apply(r.level, new List<MechanicKind>(_mechanics), _injector, seed);
                    if (_difficulty != null)
                    {
                        LevelDifficultyAnalyzer.Report rep = LevelDifficultyAnalyzer.Analyze(r.level, _difficulty);
                        e.score = rep.score;
                        e.seconds = rep.seconds;
                    }
                    _entries.Add(e);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (_entries.Count > 0) _selected = 0;
        }

        private void SortByDifficulty()
        {
            Entry current = _selected >= 0 ? _entries[_selected] : null;
            _entries.Sort((a, b) => a.score.CompareTo(b.score));
            _selected = current != null ? _entries.IndexOf(current) : -1;
        }

        // ---------- Danh sách ----------

        private void DrawList()
        {
            EditorGUILayout.LabelField($"Kết quả ({_entries.Count})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Seed   Lưới   Số   1 LG   Điểm   Giây   ms", EditorStyles.miniLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                string text = $"{e.seed,-6} {e.level.size}x{e.level.Height,-3} {e.level.clues.Length,3}   {(e.unique ? "✓" : "?")}   " +
                              $"{e.score,5:0.0}  {e.seconds,4:0}  {e.ms,3}{(e.saved ? "  (đã lưu)" : "")}";
                GUIStyle style = i == _selected ? EditorStyles.helpBox : EditorStyles.label;
                if (e.mechanics.Count > 0) text += "  +" + e.mechanics.Count;
                if (GUILayout.Button(new GUIContent(text, string.Join("\n", e.mechanics)), style)) _selected = i;
            }
            EditorGUILayout.EndScrollView();

            _appendToDatabase = EditorGUILayout.ToggleLeft("Thêm vào cuối LevelDatabase khi lưu", _appendToDatabase);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _selected >= 0 && !_entries[_selected].saved;
                if (GUILayout.Button("Lưu level chọn")) Save(new[] { _entries[_selected] });
                GUI.enabled = _entries.Exists(e => !e.saved);
                if (GUILayout.Button("Lưu tất cả")) Save(_entries.FindAll(e => !e.saved));
                GUI.enabled = true;
            }
            GUI.enabled = _selected >= 0 && _entries[_selected].saved;
            if (GUILayout.Button("Mở trong Level Editor")) LevelEditorWindow.Open(_entries[_selected].level);
            GUI.enabled = true;
        }

        private void Save(IList<Entry> entries)
        {
            EnsureFolder(OutputFolder);
            LevelDatabase database = _appendToDatabase ? AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath) : null;
            foreach (Entry e in entries)
            {
                string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{e.level.id}.asset");
                AssetDatabase.CreateAsset(e.level, path);
                e.saved = true;
                if (database != null && !database.levels.Contains(e.level)) database.levels.Add(e.level);
            }
            if (database != null) EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[LevelGenerator] Đã lưu {entries.Count} level vào {OutputFolder}" +
                                  (database != null ? $", thêm vào cuối LevelDatabase ({database.levels.Count} level)." : "."));
        }

        // ---------- Xem trước ----------

        private void DrawPreview()
        {
            _showSolution = EditorGUILayout.ToggleLeft("Hiện lời giải", _showSolution);
            if (_selected < 0 || _selected >= _entries.Count) return;
            LevelDefinition level = _entries[_selected].level;

            Rect area = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float cell = Mathf.Floor(Mathf.Min(area.width / level.size, area.height / level.Height));
            Vector2 origin = new Vector2(area.x + (area.width - cell * level.size) * 0.5f, area.y);

            // Ô (hàng y = 0 ở dưới như trong game)
            for (int x = 0; x < level.size; x++)
                for (int y = 0; y < level.Height; y++)
                    EditorGUI.DrawRect(Inset(CellRect(origin, cell, level, x, y), 1), new Color(0.22f, 0.2f, 0.32f));

            if (_showSolution && level.solution != null)
            {
                foreach (SolutionRectData s in level.solution)
                {
                    ClueData clue = System.Array.Find(level.clues, c => c.id == s.clueId);
                    Color color = ((ColorType)clue.color).ToColor(0.55f);
                    Rect min = CellRect(origin, cell, level, s.x, s.y + s.height - 1);
                    Rect r = new Rect(min.x, min.y, cell * s.width, cell * s.height);
                    EditorGUI.DrawRect(Inset(r, 3), color);
                }
            }

            GUIStyle number = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp((int)(cell * 0.45f), 8, 28),
                normal = { textColor = Color.white }
            };
            foreach (ClueData c in level.clues)
            {
                Rect r = CellRect(origin, cell, level, c.x, c.y);
                string text = c.value.ToString();
                if (c.mechanic.hidden) text = "?";
                else if (c.mechanic.ambiguity != ClueAmbiguity.None) text = AmbiguityRules.Describe(c.mechanic, c.value);
                if (c.mechanic.corner != ClueCorner.None) text += "\n" + CornerGlyph(c.mechanic.corner);
                GUI.Label(r, text, number);
            }

            // Tường: vạch gỗ trên đường kẻ giữa 2 ô
            if (level.walls != null)
            {
                float t = Mathf.Max(3f, cell * 0.12f);
                foreach (WallData w in level.walls)
                {
                    Rect a = CellRect(origin, cell, level, w.x, w.y);
                    Rect line = w.side == WallSide.Right ? new Rect(a.xMax - t / 2f, a.y, t, a.height) : new Rect(a.x, a.y - t / 2f, a.width, t);
                    EditorGUI.DrawRect(line, new Color(0.91f, 0.67f, 0.41f));
                }
            }
        }

        private static string CornerGlyph(ClueCorner c)
        {
            switch (c)
            {
                case ClueCorner.TopLeft: return "┌";
                case ClueCorner.TopRight: return "┐";
                case ClueCorner.BottomLeft: return "└";
                case ClueCorner.BottomRight: return "┘";
                case ClueCorner.Top: return "▔";
                case ClueCorner.Bottom: return "▁";
                case ClueCorner.Left: return "▏";
                default: return "▕";
            }
        }

        private static Rect CellRect(Vector2 origin, float cell, LevelDefinition level, int x, int y) =>
            new Rect(origin.x + x * cell, origin.y + (level.Height - 1 - y) * cell, cell, cell);

        private static Rect Inset(Rect r, float d) => new Rect(r.x + d, r.y + d, r.width - 2 * d, r.height - 2 * d);

        // ---------- Tiện ích ----------

        /// <summary>Level chưa lưu là ScriptableObject trong bộ nhớ: hủy khi sinh lại / đóng cửa sổ.</summary>
        private void ClearUnsaved()
        {
            foreach (Entry e in _entries)
                if (!e.saved && e.level != null) DestroyImmediate(e.level);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
