using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → PawBlock → Mechanic Injector: gắn cơ chế (Void, hướng, khóa, "?") cho level ShikaCat theo lịch trong
/// MechanicInjectorSettings.asset.
/// - Xem trước: chỉ lập lịch (nhanh), chưa đụng asset.
/// - Áp dụng: sinh bản có cơ chế vào Levels/Mechanic/ (cùng tên level gốc, giữ GUID khi chạy lại) và thay vào LevelDatabase.
///   Level gốc trong Levels/ShikaCat không bị sửa. Level Custom / Test trong database được giữ nguyên.
/// - Khôi phục: trả các ô database về level gốc ShikaCat.
/// </summary>
public class MechanicInjectorWindow : EditorWindow
{
    private const string DatabasePath = "Assets/_Project/Levels/LevelDatabase.asset";
    private const string SettingsPath = "Assets/_Project/Levels/MechanicInjectorSettings.asset";
    private const string ShikaCatFolder = "Assets/_Project/Levels/ShikaCat";
    public const string OutputFolder = "Assets/_Project/Levels/Mechanic";

    private class Row
    {
        public int number;
        public LevelDefinition source;
        public LevelDefinition current;
        public MechanicInjector.Plan plan;
        public List<string> applied;
        public List<string> notes;
    }

    private LevelDatabase _database;
    private MechanicInjectorSettings _settings;
    private SerializedObject _settingsSO;
    private int _from = 1, _to = 9999;
    private bool _onlyWithMechanic;
    private bool _showSettings = true;
    private Vector2 _scroll, _settingsScroll;
    private readonly List<Row> _rows = new List<Row>();
    private string _summary = "";

    [MenuItem("Tools/PawBlock/Mechanic Injector", priority = 2)]
    public static void Open()
    {
        MechanicInjectorWindow w = GetWindow<MechanicInjectorWindow>("Mechanic Injector");
        w.minSize = new Vector2(520, 520);
    }

    private void OnEnable()
    {
        _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        _settings = LoadOrCreateSettings();
        _settingsSO = _settings != null ? new SerializedObject(_settings) : null;
        if (_database != null) _to = _database.Count;
    }

    private static MechanicInjectorSettings LoadOrCreateSettings()
    {
        MechanicInjectorSettings settings = AssetDatabase.LoadAssetAtPath<MechanicInjectorSettings>(SettingsPath);
        if (settings != null) return settings;
        settings = CreateInstance<MechanicInjectorSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private void OnGUI()
    {
        if (_database == null || _settings == null)
        {
            EditorGUILayout.HelpBox($"Không tìm thấy {DatabasePath}. Chạy Tools → PawBlock → Builders → Import ShikaCat Levels trước.", MessageType.Error);
            if (GUILayout.Button("Tải lại")) OnEnable();
            return;
        }

        DrawSettings();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level", GUILayout.Width(40));
        _from = Mathf.Clamp(EditorGUILayout.IntField(_from, GUILayout.Width(50)), 1, _database.Count);
        EditorGUILayout.LabelField("→", GUILayout.Width(16));
        _to = Mathf.Clamp(EditorGUILayout.IntField(_to, GUILayout.Width(50)), _from, _database.Count);
        if (GUILayout.Button("Tất cả", GUILayout.Width(60)))
        {
            _from = 1;
            _to = _database.Count;
            GUI.FocusControl(null);
        }
        GUILayout.FlexibleSpace();
        _onlyWithMechanic = GUILayout.Toggle(_onlyWithMechanic, "Chỉ level có cơ chế");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Xem trước", "Chỉ lập lịch, chưa sinh level"))) Preview();
        if (GUILayout.Button(new GUIContent("Áp dụng", "Sinh level có cơ chế vào Levels/Mechanic và thay vào LevelDatabase"))) Apply();
        if (GUILayout.Button(new GUIContent("Khôi phục bản gốc", "Trả các level trong khoảng về bản ShikaCat"))) Restore();
        if (GUILayout.Button(new GUIContent("Validate Levels", "Kiểm tra toàn bộ database (LevelValidator)"))) LevelValidator.ValidateDatabase();
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_summary)) EditorGUILayout.HelpBox(_summary, MessageType.Info);
        DrawRows();
    }

    private void DrawSettings()
    {
        _showSettings = EditorGUILayout.Foldout(_showSettings, "Cấu hình (MechanicInjectorSettings.asset)", true);
        if (!_showSettings) return;

        _settingsSO.Update();
        _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll, GUILayout.MaxHeight(260));
        SerializedProperty p = _settingsSO.GetIterator();
        p.NextVisible(true); // bỏ m_Script
        while (p.NextVisible(false)) EditorGUILayout.PropertyField(p, true);
        EditorGUILayout.EndScrollView();
        _settingsSO.ApplyModifiedProperties();
    }

    private void DrawRows()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (Row row in _rows)
        {
            bool has = row.applied != null ? row.applied.Count > 0 : row.plan != null && row.plan.kinds.Count > 0;
            if (_onlyWithMechanic && !has) continue;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(row.number.ToString(), EditorStyles.boldLabel, GUILayout.Width(34));
            if (GUILayout.Button(row.current != null ? row.current.name : "—", EditorStyles.linkLabel, GUILayout.Width(90)) && row.current != null)
                LevelEditorWindow.Open(row.current);

            EditorGUILayout.BeginVertical();
            if (row.plan == null)
            {
                EditorGUILayout.LabelField("Bỏ qua (không phải level ShikaCat)", EditorStyles.miniLabel);
            }
            else
            {
                string head = $"{row.plan} · {row.plan.reason}";
                EditorGUILayout.LabelField(head, row.plan.intro ? EditorStyles.boldLabel : EditorStyles.label);
                if (row.applied != null)
                    foreach (string a in row.applied) EditorGUILayout.LabelField("✓ " + a, EditorStyles.wordWrappedMiniLabel);
                if (row.notes != null)
                    foreach (string n in row.notes) EditorGUILayout.LabelField("! " + n, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // =====================================================================

    private void Preview()
    {
        _rows.Clear();
        List<MechanicInjector.Plan> plans = MakePlans();
        Dictionary<MechanicKind, int> counts = new Dictionary<MechanicKind, int>();
        int withMechanic = 0;
        for (int i = _from - 1; i < _to; i++)
        {
            Row row = MakeRow(i, plans);
            _rows.Add(row);
            if (row.plan == null || row.plan.kinds.Count == 0) continue;
            withMechanic++;
            foreach (MechanicKind k in row.plan.kinds) counts[k] = counts.TryGetValue(k, out int c) ? c + 1 : 1;
        }
        _summary = $"Lịch {_rows.Count} level: {withMechanic} level có cơ chế ({string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}"))}). Chưa sinh level.";
    }

    /// <summary>Lịch luôn tính từ level 1 (đợt giới thiệu phụ thuộc các level trước), dù chỉ xử lý 1 khoảng.</summary>
    private List<MechanicInjector.Plan> MakePlans()
    {
        List<LevelDefinition> sources = _database.levels.Select(SourceOf).ToList();
        return MechanicInjector.MakePlans(_settings, sources);
    }

    private Row MakeRow(int index, List<MechanicInjector.Plan> plans)
    {
        LevelDefinition entry = _database.levels[index];
        return new Row { number = index + 1, source = SourceOf(entry), current = entry, plan = plans[index] };
    }

    private void Apply()
    {
        if (!EditorUtility.DisplayDialog("Mechanic Injector",
                $"Sinh level có cơ chế cho level {_from}–{_to} và thay vào LevelDatabase?\n" +
                "Level gốc ShikaCat giữ nguyên; bản cũ trong Levels/Mechanic sẽ bị ghi đè.", "Áp dụng", "Hủy"))
            return;
        RunApply();
    }

    /// <summary>Áp dụng không hỏi xác nhận (gọi từ code / MCP): level from–to (tính từ 1) trong LevelDatabase.</summary>
    public static string ApplyRange(int from, int to)
    {
        MechanicInjectorWindow w = CreateInstance<MechanicInjectorWindow>();
        try
        {
            w.OnEnable();
            w._from = Mathf.Clamp(from, 1, w._database.Count);
            w._to = Mathf.Clamp(to, w._from, w._database.Count);
            w.RunApply();
            return w._summary;
        }
        finally
        {
            DestroyImmediate(w);
        }
    }

    private void RunApply()
    {
        EnsureFolder(OutputFolder);
        Undo.RecordObject(_database, "Mechanic Injector");
        _rows.Clear();
        List<MechanicInjector.Plan> plans = MakePlans();
        int done = 0, failed = 0;
        Dictionary<MechanicKind, int> counts = new Dictionary<MechanicKind, int>();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            for (int i = _from - 1; i < _to; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Mechanic Injector", $"Level {i + 1}/{_to}", (float)(i - _from + 1) / (_to - _from + 1)))
                {
                    Debug.LogWarning("[MechanicInjector] Đã hủy giữa chừng, các level đã xử lý vẫn được lưu.");
                    break;
                }

                Row row = MakeRow(i, plans);
                _rows.Add(row);
                if (row.plan == null) continue;

                MechanicInjector.Result result = MechanicInjector.Inject(row.source, row.plan, _settings);
                row.applied = result.applied;
                row.notes = result.notes;
                bool hasMechanic = result.level != null; // Save có thể hủy bản tạm khi ghi đè asset cũ
                row.current = Save(row.source, result.level);
                _database.levels[i] = row.current;

                if (hasMechanic)
                {
                    done++;
                    foreach (MechanicKind k in result.appliedKinds) counts[k] = counts.TryGetValue(k, out int c) ? c + 1 : 1;
                }
                if (row.plan.kinds.Any(k => !result.appliedKinds.Contains(k))) failed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
        }

        _summary = $"Xong {_rows.Count} level trong {watch.Elapsed.TotalSeconds:0.0}s: {done} level có cơ chế " +
                   $"({string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}"))}), {failed} level không gắn đủ theo lịch (xem dòng \"!\").";
        Debug.Log($"[MechanicInjector] {_summary}");
    }

    /// <summary>Lưu bản có cơ chế (ghi đè giữ GUID); không có cơ chế thì xóa bản cũ và trả về level gốc.</summary>
    private static LevelDefinition Save(LevelDefinition source, LevelDefinition generated)
    {
        string path = $"{OutputFolder}/{source.name}.asset";
        LevelDefinition existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);

        if (generated == null)
        {
            if (existing != null) AssetDatabase.DeleteAsset(path);
            return source;
        }

        generated.name = source.name;
        generated.id = source.id + "_mech";
        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, path);
            return generated;
        }

        EditorUtility.CopySerialized(generated, existing);
        EditorUtility.SetDirty(existing);
        DestroyImmediate(generated);
        return existing;
    }

    private void Restore()
    {
        if (!EditorUtility.DisplayDialog("Mechanic Injector",
                $"Trả level {_from}–{_to} trong LevelDatabase về bản ShikaCat gốc?\n(Asset trong Levels/Mechanic giữ nguyên, Áp dụng lại sẽ ghi đè.)",
                "Khôi phục", "Hủy"))
            return;

        Undo.RecordObject(_database, "Mechanic Injector Restore");
        int restored = 0;
        for (int i = _from - 1; i < _to; i++)
        {
            LevelDefinition source = SourceOf(_database.levels[i]);
            if (source == null || source == _database.levels[i]) continue;
            _database.levels[i] = source;
            restored++;
        }
        EditorUtility.SetDirty(_database);
        AssetDatabase.SaveAssets();
        _rows.Clear();
        _summary = $"Đã khôi phục {restored} level về bản gốc.";
    }

    /// <summary>Level ShikaCat gốc của 1 ô database; null với level Custom / Test (không đụng tới).</summary>
    private static LevelDefinition SourceOf(LevelDefinition entry)
    {
        if (entry == null) return null;
        string path = AssetDatabase.GetAssetPath(entry);
        if (path.StartsWith(ShikaCatFolder + "/")) return entry;
        if (path.StartsWith(OutputFolder + "/"))
            return AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{ShikaCatFolder}/{entry.name}.asset");
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
