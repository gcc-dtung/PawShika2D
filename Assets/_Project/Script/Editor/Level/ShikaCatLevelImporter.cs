using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chuyển shikacat-levels.json (205 level của ShikaCat) thành LevelDefinition + LevelDatabase.
/// ShikaCat đếm hàng từ trên xuống; GridSpawner đếm y từ dưới lên -> lật trục y.
/// </summary>
public static class ShikaCatLevelImporter
{
    private const string JsonPath = "Assets/_Project/ShikaCat_GameAssets/03_Gameplay_Boxes_Grid/Level_Data/shikacat-levels.json";
    private const string OutputFolder = "Assets/_Project/Levels/ShikaCat";
    private const string DatabasePath = "Assets/_Project/Levels/LevelDatabase.asset";
    private const string TestFolder = "Assets/_Project/Levels/Test";

    [Serializable] private class Wrapper { public SkLevel[] items; }

    [Serializable]
    private class SkLevel
    {
        public int _rows;
        public int _cols;
        public int _gridSize;
        public SkCat[] _cats;
        public SkRect[] _solution;
    }

    [Serializable] private class SkCat { public int _row; public int _col; public int _size; public int _color; }
    [Serializable] private class SkRect { public int _row; public int _col; public int _width; public int _height; public int _catIndex; }

    [MenuItem("Tools/PawBlock/Builders/Import ShikaCat Levels", priority = 111)]
    public static void Import()
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
        if (json == null)
        {
            Debug.LogError($"[Importer] Không tìm thấy {JsonPath}");
            return;
        }

        Wrapper data = JsonUtility.FromJson<Wrapper>("{\"items\":" + json.text + "}");
        EnsureFolder(OutputFolder);

        LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        database.levels.Clear();

        int warnings = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < data.items.Length; i++)
            {
                SkLevel src = data.items[i];
                string path = $"{OutputFolder}/Level_{i + 1:000}.asset";

                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                bool isNew = level == null;
                if (isNew) level = ScriptableObject.CreateInstance<LevelDefinition>();

                if (!Fill(level, src, i)) warnings++;

                if (isNew) AssetDatabase.CreateAsset(level, path);
                else EditorUtility.SetDirty(level);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        for (int i = 0; i < data.items.Length; i++)
        {
            database.levels.Add(AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{OutputFolder}/Level_{i + 1:000}.asset"));
        }
        // Giữ level test (Levels/Test) ở cuối danh sách sau khi nhập lại
        AppendTestLevels(database);
        LevelEditorWindow.AppendCustomLevels(database); // level tự làm bằng Level Editor (thêm vào cuối)
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Importer] Đã nhập {data.items.Length} level vào {OutputFolder}, {warnings} cảnh báo. Database: {DatabasePath}");
    }

    private static bool Fill(LevelDefinition level, SkLevel src, int index)
    {
        int rows = src._rows > 0 ? src._rows : src._gridSize;
        int cols = src._cols > 0 ? src._cols : src._gridSize;

        level.id = $"shikacat_{index + 1:000}";
        level.source = new LevelSource { game = "ShikaCat", sourceIndex = index };
        level.size = cols;
        level.height = rows;
        level.cellOverrides = new CellOverrideData[0];
        level.rules = new LevelRules(); // timeLimit 0 = tự tính theo số ô / khối

        level.clues = new ClueData[src._cats.Length];
        for (int c = 0; c < src._cats.Length; c++)
        {
            SkCat cat = src._cats[c];
            level.clues[c] = new ClueData
            {
                id = c,
                x = cat._col,
                y = rows - 1 - cat._row,
                value = cat._size,
                color = cat._color,
                mechanic = new ClueMechanic()
            };
        }

        level.solution = new SolutionRectData[src._solution.Length];
        int area = 0;
        for (int s = 0; s < src._solution.Length; s++)
        {
            SkRect r = src._solution[s];
            level.solution[s] = new SolutionRectData
            {
                clueId = r._catIndex,
                x = r._col,
                y = rows - r._row - r._height,
                width = r._width,
                height = r._height
            };
            area += r._width * r._height;
        }

        if (area != rows * cols)
        {
            Debug.LogWarning($"[Importer] Level {index + 1}: tổng diện tích lời giải {area} != {rows * cols}");
            return false;
        }
        return true;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    /// <summary>Thêm mọi level trong Levels/Test vào cuối database (bỏ qua level đã có).</summary>
    private static void AppendTestLevels(LevelDatabase database)
    {
        if (!AssetDatabase.IsValidFolder(TestFolder)) return;

        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { TestFolder }))
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level != null && !database.levels.Contains(level)) database.levels.Add(level);
        }
        EditorUtility.SetDirty(database);
    }
}
