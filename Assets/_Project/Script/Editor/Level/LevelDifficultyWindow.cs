using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → PawBlock → Level Difficulty: độ khó lý thuyết của mọi level trong LevelDatabase (LevelDifficultyAnalyzer).
/// - Biểu đồ điểm D theo level (màu = đánh dấu Hard / Very Hard, chấm trắng = gợi ý), bảng chỉ số từng level.
/// - Tick Hard / Very Hard để level hiện giao diện khó trong game (LevelDefinition.difficulty). Level trong Levels/Mechanic
///   ghi cả bản gốc ShikaCat để Mechanic Injector chạy lại vẫn giữ.
/// - Xuất CSV chỉ số lý thuyết; nạp CSV dữ liệu thực tế (cột level / id / name + cột số bất kỳ) để xem tương quan Spearman.
/// Công thức: Assets/Docs/DifficultyFormula.md.
/// </summary>
public class LevelDifficultyWindow : EditorWindow
{
    private const string DatabasePath = "Assets/_Project/Levels/LevelDatabase.asset";
    private const string SettingsPath = "Assets/_Project/Levels/LevelDifficultySettings.asset";
    private const string ShikaCatFolder = "Assets/_Project/Levels/ShikaCat";
    private const string TestFolder = "Assets/_Project/Levels/Test";

    private static readonly Color NormalColor = new Color(0.45f, 0.55f, 0.75f);
    private static readonly Color HardColor = new Color(0.95f, 0.6f, 0.2f);
    private static readonly Color VeryHardColor = new Color(0.9f, 0.25f, 0.3f);
    private static readonly Color RealColor = new Color(0.4f, 0.9f, 0.5f);

    private class Row
    {
        public int number;
        public LevelDefinition level;
        public LevelDifficultyAnalyzer.Report report;
        public LevelDifficulty suggested;
        public bool eligible; // được xét gợi ý Hard / Very Hard
    }

    private enum SortMode { Level, ScoreDesc }

    private LevelDatabase _database;
    private LevelDifficultySettings _settings;
    private SerializedObject _settingsSO;
    private readonly List<Row> _rows = new List<Row>();

    private bool _showSettings;
    private bool _onlyMarked;
    private SortMode _sort = SortMode.Level;
    private Vector2 _scroll, _settingsScroll;
    private string _summary = "";

    // Dữ liệu thực tế (CSV)
    private string _realPath;
    private string[] _realMetrics = new string[0];
    private readonly Dictionary<int, Dictionary<string, double>> _real = new Dictionary<int, Dictionary<string, double>>();
    private int _realShown;
    private readonly List<string> _correlations = new List<string>();

    [MenuItem("Tools/PawBlock/Level Difficulty", priority = 3)]
    public static void Open()
    {
        LevelDifficultyWindow w = GetWindow<LevelDifficultyWindow>("Level Difficulty");
        w.minSize = new Vector2(640, 520);
    }

    private void OnEnable()
    {
        _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        _settings = AssetDatabase.LoadAssetAtPath<LevelDifficultySettings>(SettingsPath);
        if (_settings == null)
        {
            _settings = CreateInstance<LevelDifficultySettings>();
            AssetDatabase.CreateAsset(_settings, SettingsPath);
            AssetDatabase.SaveAssets();
        }
        _settingsSO = new SerializedObject(_settings);
        if (_database != null) Analyze();
    }

    // =====================================================================
    // Phân tích

    private void Analyze()
    {
        _rows.Clear();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < _database.Count; i++)
        {
            LevelDefinition level = _database.levels[i];
            if (level == null) continue;
            string path = AssetDatabase.GetAssetPath(level);
            _rows.Add(new Row
            {
                number = i + 1,
                level = level,
                report = LevelDifficultyAnalyzer.Analyze(level, _settings),
                eligible = i + 1 >= _settings.minLevelForHard && !path.StartsWith(TestFolder + "/")
            });
        }
        Suggest();
        int errors = _rows.Count(r => r.report.error != null);
        _summary = $"Phân tích {_rows.Count} level trong {watch.ElapsedMilliseconds} ms" + (errors > 0 ? $", {errors} level lỗi (xem cột Ghi chú)" : "") + ".";
        ComputeCorrelations();
    }

    /// <summary>Đổi trọng số: tính lại điểm từ chỉ số đã có, không cần mô phỏng lại.</summary>
    private void Rescore()
    {
        foreach (Row r in _rows) LevelDifficultyAnalyzer.Score(r.report, _settings);
        Suggest();
        ComputeCorrelations();
    }

    /// <summary>Gợi ý theo phân vị: veryHardShare level điểm cao nhất -> Very Hard, hardShare level kế tiếp -> Hard.</summary>
    private void Suggest()
    {
        List<Row> eligible = _rows.Where(r => r.eligible && r.report.error == null).OrderByDescending(r => r.report.score).ToList();
        int veryHard = Mathf.RoundToInt(eligible.Count * _settings.veryHardShare);
        int hard = Mathf.RoundToInt(eligible.Count * _settings.hardShare);
        foreach (Row r in _rows) r.suggested = LevelDifficulty.Normal;
        for (int i = 0; i < eligible.Count && i < veryHard + hard; i++)
            eligible[i].suggested = i < veryHard ? LevelDifficulty.VeryHard : LevelDifficulty.Hard;
    }

    // =====================================================================
    // GUI

    private void OnGUI()
    {
        if (_database == null)
        {
            EditorGUILayout.HelpBox($"Không tìm thấy {DatabasePath}.", MessageType.Error);
            if (GUILayout.Button("Tải lại")) OnEnable();
            return;
        }

        DrawToolbar();
        DrawSettings();
        if (!string.IsNullOrEmpty(_summary)) EditorGUILayout.HelpBox(_summary, MessageType.None);
        DrawChart();
        DrawRealData();
        DrawTable();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Phân tích lại", "Mô phỏng lại mọi level (sau khi sửa level / chạy Mechanic Injector)"))) Analyze();
        if (GUILayout.Button(new GUIContent("Áp dụng gợi ý", "Đặt Hard / Very Hard đúng theo cột Gợi ý cho mọi level"))) ApplySuggestions();
        if (GUILayout.Button(new GUIContent("Bỏ hết đánh dấu", "Đưa mọi level về Normal"))) ClearMarks();
        if (GUILayout.Button(new GUIContent("Xuất CSV", "Chỉ số lý thuyết từng level, để ghép với dữ liệu thực tế"))) ExportCsv();
        if (GUILayout.Button(new GUIContent("Nạp dữ liệu thực tế", "CSV có cột level (số thứ tự) / id / name và các cột số"))) ImportRealCsv();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _onlyMarked = GUILayout.Toggle(_onlyMarked, "Chỉ hiện Hard / Very Hard", GUILayout.Width(170));
        GUILayout.Label("Sắp xếp", GUILayout.Width(50));
        _sort = (SortMode)EditorGUILayout.Popup((int)_sort, new[] { "Theo level", "Điểm D giảm dần" }, GUILayout.Width(130));
        GUILayout.FlexibleSpace();
        int hard = _rows.Count(r => r.level.difficulty == LevelDifficulty.Hard);
        int veryHard = _rows.Count(r => r.level.difficulty == LevelDifficulty.VeryHard);
        GUILayout.Label($"Đang đánh dấu: Hard {hard} · Very Hard {veryHard}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettings()
    {
        _showSettings = EditorGUILayout.Foldout(_showSettings, "Trọng số công thức (LevelDifficultySettings.asset) – xem Assets/Docs/DifficultyFormula.md", true);
        if (!_showSettings) return;

        _settingsSO.Update();
        EditorGUI.BeginChangeCheck();
        _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll, GUILayout.MaxHeight(220));
        SerializedProperty p = _settingsSO.GetIterator();
        p.NextVisible(true); // bỏ m_Script
        while (p.NextVisible(false)) EditorGUILayout.PropertyField(p, true);
        EditorGUILayout.EndScrollView();
        if (EditorGUI.EndChangeCheck())
        {
            _settingsSO.ApplyModifiedProperties();
            Rescore();
        }
    }

    /// <summary>Cột = điểm D từng level theo thứ tự chơi; màu = đánh dấu; chấm trắng = gợi ý khác đánh dấu; chấm xanh = dữ liệu thực tế đang chọn.</summary>
    private void DrawChart()
    {
        Rect rect = GUILayoutUtility.GetRect(10, 110, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.15f));
        if (_rows.Count == 0) return;

        float max = Mathf.Max(1f, _rows.Max(r => r.report.score));
        float barW = rect.width / _rows.Count;
        string metric = _realShown < _realMetrics.Length ? _realMetrics[_realShown] : null;
        double realMin = 0, realMax = 1;
        if (metric != null)
        {
            List<double> values = _rows.Select(r => RealValue(r, metric)).Where(v => v.HasValue).Select(v => v.Value).ToList();
            if (values.Count > 0) { realMin = values.Min(); realMax = Mathf.Max((float)(values.Max() - realMin), 1e-6f) + realMin; }
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            Row r = _rows[i];
            float h = (rect.height - 6) * r.report.score / max;
            Rect bar = new Rect(rect.x + i * barW, rect.yMax - h, Mathf.Max(1f, barW - (barW > 3 ? 1 : 0)), h);
            EditorGUI.DrawRect(bar, ColorOf(r.level.difficulty));
            if (r.suggested != r.level.difficulty && r.suggested != LevelDifficulty.Normal)
                EditorGUI.DrawRect(new Rect(bar.x, bar.y - 4, bar.width, 3), Color.white);

            double? v = metric != null ? RealValue(r, metric) : null;
            if (v.HasValue)
            {
                float y = rect.yMax - 3 - (float)((v.Value - realMin) / (realMax - realMin)) * (rect.height - 6);
                EditorGUI.DrawRect(new Rect(bar.x, y - 1, Mathf.Max(2f, bar.width), 3), RealColor);
            }
        }
        GUI.Label(new Rect(rect.x + 4, rect.y + 2, 400, 16), $"D tối đa {max:0.0}" + (metric != null ? $" · chấm xanh: {metric} (thực tế)" : ""), EditorStyles.miniLabel);
    }

    private void DrawRealData()
    {
        if (_realMetrics.Length == 0) return;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Thực tế: {Path.GetFileName(_realPath)} ({_real.Count} level)", GUILayout.Width(260));
        GUILayout.Label("Hiện", GUILayout.Width(30));
        _realShown = EditorGUILayout.Popup(_realShown, _realMetrics, GUILayout.Width(160));
        if (GUILayout.Button("Bỏ", GUILayout.Width(40)))
        {
            _realMetrics = new string[0];
            _real.Clear();
            _correlations.Clear();
        }
        EditorGUILayout.EndHorizontal();
        foreach (string c in _correlations) EditorGUILayout.LabelField(c, EditorStyles.miniLabel);
    }

    private void DrawTable()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", GUILayout.Width(32));
        GUILayout.Label("Level", GUILayout.Width(110));
        GUILayout.Label(new GUIContent("D", "Điểm độ khó lý thuyết"), GUILayout.Width(90));
        GUILayout.Label(new GUIContent("Giây", "Thời gian giải ước tính"), GUILayout.Width(40));
        GUILayout.Label(new GUIContent("Rủi ro", "Số lần đặt sai kỳ vọng"), GUILayout.Width(42));
        GUILayout.Label(new GUIContent("T1/T2/T3/T4", "Số bước theo kỹ thuật"), GUILayout.Width(80));
        GUILayout.Label(new GUIContent("Gợi ý", "Theo phân vị điểm D"), GUILayout.Width(62));
        GUILayout.Label("Hard", GUILayout.Width(36));
        GUILayout.Label("V.Hard", GUILayout.Width(44));
        if (_realMetrics.Length > 0) GUILayout.Label(_realMetrics[_realShown], GUILayout.Width(70));
        GUILayout.Label("Bước giải / ghi chú");
        EditorGUILayout.EndHorizontal();

        IEnumerable<Row> rows = _rows;
        if (_onlyMarked) rows = rows.Where(r => r.level.difficulty != LevelDifficulty.Normal);
        if (_sort == SortMode.ScoreDesc) rows = rows.OrderByDescending(r => r.report.score);
        float maxScore = _rows.Count > 0 ? Mathf.Max(1f, _rows.Max(r => r.report.score)) : 1f;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (Row r in rows.ToList())
        {
            LevelDifficultyAnalyzer.Report rep = r.report;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(r.number.ToString(), EditorStyles.boldLabel, GUILayout.Width(32));
            if (GUILayout.Button(r.level.name, EditorStyles.linkLabel, GUILayout.Width(110))) LevelEditorWindow.Open(r.level);

            Rect barRect = GUILayoutUtility.GetRect(90, 16, GUILayout.Width(90));
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y + 3, barRect.width * rep.score / maxScore, barRect.height - 6), ColorOf(r.level.difficulty) * 0.8f);
            GUI.Label(barRect, rep.score.ToString("0.0"), EditorStyles.miniBoldLabel);

            GUILayout.Label(rep.seconds.ToString("0"), GUILayout.Width(40));
            GUILayout.Label(rep.risk.ToString("0.00"), GUILayout.Width(42));
            GUILayout.Label($"{rep.t1}/{rep.t2}/{rep.t3}/{rep.t4}", GUILayout.Width(80));
            GUILayout.Label(r.suggested == LevelDifficulty.Normal ? "" : r.suggested.ToString(), GUILayout.Width(62));

            bool hard = r.level.difficulty == LevelDifficulty.Hard;
            bool veryHard = r.level.difficulty == LevelDifficulty.VeryHard;
            bool newHard = GUILayout.Toggle(hard, "", GUILayout.Width(36));
            bool newVeryHard = GUILayout.Toggle(veryHard, "", GUILayout.Width(44));
            if (newHard != hard) SetDifficulty(r.level, newHard ? LevelDifficulty.Hard : LevelDifficulty.Normal);
            else if (newVeryHard != veryHard) SetDifficulty(r.level, newVeryHard ? LevelDifficulty.VeryHard : LevelDifficulty.Normal);

            if (_realMetrics.Length > 0)
            {
                double? v = RealValue(r, _realMetrics[_realShown]);
                GUILayout.Label(v.HasValue ? v.Value.ToString("0.###", CultureInfo.InvariantCulture) : "—", GUILayout.Width(70));
            }

            string note = rep.trace;
            if (rep.hidden + rep.locks + rep.constraints + rep.voids > 0)
                note += $"  ·  ?{rep.hidden} khóa{rep.locks} hướng{rep.constraints} void{rep.voids}";
            if (rep.t4 > 0) note += "  ·  nhiều lời giải";
            if (rep.aborted) note += "  ·  quá giới hạn tìm kiếm";
            if (rep.error != null) note += "  ·  LỖI: " + rep.error;
            GUILayout.Label(note, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private static Color ColorOf(LevelDifficulty d)
    {
        switch (d)
        {
            case LevelDifficulty.Hard: return HardColor;
            case LevelDifficulty.VeryHard: return VeryHardColor;
            default: return NormalColor;
        }
    }

    // =====================================================================
    // Đánh dấu Hard / Very Hard

    /// <summary>Ghi difficulty vào level trong database và bản gốc ShikaCat (nếu level do Mechanic Injector sinh).</summary>
    private static void SetDifficulty(LevelDefinition level, LevelDifficulty difficulty)
    {
        List<LevelDefinition> targets = new List<LevelDefinition> { level };
        if (AssetDatabase.GetAssetPath(level).StartsWith(MechanicInjectorWindow.OutputFolder + "/"))
        {
            LevelDefinition source = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{ShikaCatFolder}/{level.name}.asset");
            if (source != null) targets.Add(source);
        }

        Undo.RecordObjects(targets.ToArray(), "Level Difficulty");
        foreach (LevelDefinition t in targets)
        {
            if (t.difficulty == difficulty) continue;
            t.difficulty = difficulty;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssetIfDirty(t);
        }
    }

    private void ApplySuggestions()
    {
        int hard = _rows.Count(r => r.suggested == LevelDifficulty.Hard);
        int veryHard = _rows.Count(r => r.suggested == LevelDifficulty.VeryHard);
        if (!EditorUtility.DisplayDialog("Level Difficulty",
                $"Đặt {hard} level Hard, {veryHard} level Very Hard theo gợi ý; mọi level khác về Normal?", "Áp dụng", "Hủy"))
            return;
        foreach (Row r in _rows) SetDifficulty(r.level, r.suggested);
    }

    private void ClearMarks()
    {
        if (!EditorUtility.DisplayDialog("Level Difficulty", "Đưa mọi level về Normal?", "Bỏ hết", "Hủy")) return;
        foreach (Row r in _rows) SetDifficulty(r.level, LevelDifficulty.Normal);
    }

    // =====================================================================
    // CSV

    private void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("Xuất độ khó lý thuyết", "Assets/Docs", "LevelDifficulty_Theory.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("level,name,id,marked,suggested,score,est_seconds,mistake_risk,clues,cells,voids,constraints,locks,lock_steps,hidden,ambiguity,t1,t2,t3,t4,narrow,max_technique,trace");
        foreach (Row r in _rows)
        {
            LevelDifficultyAnalyzer.Report p = r.report;
            sb.AppendLine(string.Join(",", new[]
            {
                r.number.ToString(inv), r.level.name, r.level.id, r.level.difficulty.ToString(), r.suggested.ToString(),
                p.score.ToString("0.00", inv), p.seconds.ToString("0.0", inv), p.risk.ToString("0.000", inv),
                p.clues.ToString(inv), p.cells.ToString(inv), p.voids.ToString(inv), p.constraints.ToString(inv),
                p.locks.ToString(inv), p.lockSteps.ToString(inv), p.hidden.ToString(inv), p.ambiguity.ToString("0.000", inv),
                p.t1.ToString(inv), p.t2.ToString(inv), p.t3.ToString(inv), p.t4.ToString(inv),
                p.narrow.ToString("0.000", inv), p.maxTechnique.ToString(inv), "'" + p.trace
            }));
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        _summary = $"Đã xuất {_rows.Count} level ra {path}";
        EditorUtility.RevealInFinder(path);
    }

    /// <summary>
    /// CSV dữ liệu thực tế: dòng đầu là tên cột, có 1 cột khóa "level" (số thứ tự) hoặc "id" / "name" (khớp LevelDefinition),
    /// các cột số còn lại là chỉ số thực tế (vd win_rate, avg_seconds, avg_mistakes, attempts). Ngăn cách bằng , hoặc ;
    /// </summary>
    private void ImportRealCsv()
    {
        string path = EditorUtility.OpenFilePanel("Nạp dữ liệu thực tế", "Assets/Docs", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length < 2)
        {
            _summary = "CSV rỗng.";
            return;
        }
        char sep = lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';
        string[] header = lines[0].Split(sep).Select(h => h.Trim().Trim('"')).ToArray();
        int keyCol = System.Array.FindIndex(header, h => h.ToLowerInvariant() == "level");
        bool byNumber = keyCol >= 0;
        if (!byNumber) keyCol = System.Array.FindIndex(header, h => h.ToLowerInvariant() == "id" || h.ToLowerInvariant() == "name");
        if (keyCol < 0)
        {
            _summary = "CSV cần cột 'level' (số thứ tự), 'id' hoặc 'name'.";
            return;
        }

        _real.Clear();
        HashSet<int> numeric = new HashSet<int>();
        for (int li = 1; li < lines.Length; li++)
        {
            string[] cells = lines[li].Split(sep).Select(c => c.Trim().Trim('"')).ToArray();
            if (cells.Length <= keyCol) continue;
            Row row = byNumber && int.TryParse(cells[keyCol], out int n)
                ? _rows.Find(r => r.number == n)
                : _rows.Find(r => r.level.id == cells[keyCol] || r.level.name == cells[keyCol]);
            if (row == null) continue;

            Dictionary<string, double> values = new Dictionary<string, double>();
            for (int c = 0; c < header.Length && c < cells.Length; c++)
            {
                if (c == keyCol) continue;
                if (!double.TryParse(cells[c].Replace("%", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) continue;
                values[header[c]] = v;
                numeric.Add(c);
            }
            _real[row.number] = values;
        }

        _realMetrics = numeric.OrderBy(c => c).Select(c => header[c]).ToArray();
        _realShown = 0;
        _realPath = path;
        ComputeCorrelations();
        _summary = $"Nạp {_real.Count} level, {_realMetrics.Length} chỉ số thực tế từ {Path.GetFileName(path)}.";
    }

    private double? RealValue(Row r, string metric)
    {
        return _real.TryGetValue(r.number, out Dictionary<string, double> values) && values.TryGetValue(metric, out double v) ? v : (double?)null;
    }

    /// <summary>Spearman giữa D / thời gian / rủi ro lý thuyết và từng chỉ số thực tế.</summary>
    private void ComputeCorrelations()
    {
        _correlations.Clear();
        foreach (string metric in _realMetrics)
        {
            List<Row> pairs = _rows.Where(r => RealValue(r, metric).HasValue && r.report.error == null).ToList();
            List<double> real = pairs.Select(r => RealValue(r, metric).Value).ToList();
            double cScore = LevelDifficultyAnalyzer.Spearman(pairs.Select(r => (double)r.report.score).ToList(), real);
            double cTime = LevelDifficultyAnalyzer.Spearman(pairs.Select(r => (double)r.report.seconds).ToList(), real);
            double cRisk = LevelDifficultyAnalyzer.Spearman(pairs.Select(r => (double)r.report.risk).ToList(), real);
            _correlations.Add($"{metric} (n={pairs.Count}):  ρ(D) = {Fmt(cScore)}   ρ(giây) = {Fmt(cTime)}   ρ(rủi ro) = {Fmt(cRisk)}");
        }
    }

    private static string Fmt(double v) => double.IsNaN(v) ? "—" : v.ToString("+0.00;-0.00", CultureInfo.InvariantCulture);
}
