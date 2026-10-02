using UnityEditor;
using UnityEngine;

/// <summary>
/// Tạo level test gom mọi cơ chế hiện có: lưới không vuông, ô Void, hướng ngang/dọc/vuông,
/// ổ khóa, ô ẩn "?", khối 1 ô, và một nước đi "đúng luật nhưng sai" để thử trừ mạng.
/// Level được thêm vào CUỐI LevelDatabase (không làm lệch số level ShikaCat đã lưu).
/// </summary>
public static class TestLevelBuilder
{
    public const string TestFolder = "Assets/_Project/Levels/Test";
    private const string AssetPath = TestFolder + "/Level_Test_AllMechanics.asset";
    private const string DatabasePath = "Assets/_Project/Levels/LevelDatabase.asset";

    // Lời giải duy nhất (y = 6 ở trên cùng, # = Void):
    //   y6  A A B B B #      A 4 (□)  B 3 (→)
    //   y5  A A C D D D      C 3 (↑)  D 6 (?)
    //   y4  E E C D D D      E 4
    //   y3  E E C F G G      F 2 (↑)  G 4 (khóa 3)
    //   y2  H H H F G G      H 3
    //   y1  I J J J J K      I 2      J 4 (→)  K 2
    //   y0  I L L M N K      L 2      M 1      N 1
    //
    // Thử trừ mạng: kéo K thành (4,1)-(5,1) -> đủ 2 ô, 1 số, nhưng lưới hết lời giải.
    // Thử hủy thao tác: kéo qua ô Void, chọn 2 số, sai hướng mũi tên, sai số ô, chọn G khi còn khóa.

    [MenuItem("Tools/PawBlock/Create Test Level (All Mechanics)")]
    public static void Create()
    {
        LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetPath);
        bool isNew = level == null;
        if (isNew) level = ScriptableObject.CreateInstance<LevelDefinition>();

        Fill(level);

        if (isNew)
        {
            EnsureFolder(TestFolder);
            AssetDatabase.CreateAsset(level, AssetPath);
        }
        else
        {
            EditorUtility.SetDirty(level);
        }

        LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        if (database != null)
        {
            AppendTestLevels(database);
        }

        AssetDatabase.SaveAssets();

        int index = database != null ? database.levels.IndexOf(level) : -1;
        Debug.Log($"[TestLevel] {AssetPath}. Vị trí trong database: index {index} (level {index + 1}).");
        LevelValidator.Log(level, index + 1, LevelValidator.Validate(level), logPassed: true);
        Selection.activeObject = level;
    }

    /// <summary>Thêm mọi level trong thư mục Test vào cuối database (bỏ qua level đã có).</summary>
    public static void AppendTestLevels(LevelDatabase database)
    {
        if (!AssetDatabase.IsValidFolder(TestFolder)) return;

        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { TestFolder }))
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level != null && !database.levels.Contains(level)) database.levels.Add(level);
        }
        EditorUtility.SetDirty(database);
    }

    private static void Fill(LevelDefinition level)
    {
        level.id = "test_all_mechanics";
        level.source = new LevelSource { game = "Test", sourceIndex = -1 };
        level.size = 6;
        level.height = 7;
        level.rules = new LevelRules { mistakeLimit = 3 };
        level.cellOverrides = new[]
        {
            new CellOverrideData { x = 5, y = 6, type = CellType.Void }
        };

        level.clues = new[]
        {
            Clue(0, 0, 6, 4, ColorType.Blue, ClueConstraint.Square),
            Clue(1, 3, 6, 3, ColorType.Orange, ClueConstraint.Horizontal),
            Clue(2, 2, 4, 3, ColorType.Pink, ClueConstraint.Vertical),
            Clue(3, 4, 5, 6, ColorType.Green, hidden: true),
            Clue(4, 0, 4, 4, ColorType.Brown),
            Clue(5, 3, 2, 2, ColorType.Purple, ClueConstraint.Vertical),
            Clue(6, 5, 3, 4, ColorType.Red, lockSteps: 3),
            Clue(7, 1, 2, 3, ColorType.Cyan),
            Clue(8, 0, 0, 2, ColorType.Yellow),
            Clue(9, 2, 1, 4, ColorType.Gray, ClueConstraint.Horizontal),
            Clue(10, 5, 1, 2, ColorType.Blue),
            Clue(11, 1, 0, 2, ColorType.Green),
            Clue(12, 3, 0, 1, ColorType.Orange),
            Clue(13, 4, 0, 1, ColorType.Pink),
        };

        level.solution = new[]
        {
            Rect(0, 0, 5, 2, 2),
            Rect(1, 2, 6, 3, 1),
            Rect(2, 2, 3, 1, 3),
            Rect(3, 3, 4, 3, 2),
            Rect(4, 0, 3, 2, 2),
            Rect(5, 3, 2, 1, 2),
            Rect(6, 4, 2, 2, 2),
            Rect(7, 0, 2, 3, 1),
            Rect(8, 0, 0, 1, 2),
            Rect(9, 1, 1, 4, 1),
            Rect(10, 5, 0, 1, 2),
            Rect(11, 1, 0, 2, 1),
            Rect(12, 3, 0, 1, 1),
            Rect(13, 4, 0, 1, 1),
        };
    }

    private static ClueData Clue(int id, int x, int y, int value, ColorType color,
        ClueConstraint constraint = ClueConstraint.None, int lockSteps = 0, bool hidden = false)
    {
        return new ClueData
        {
            id = id,
            x = x,
            y = y,
            value = value,
            color = (int)color,
            mechanic = new ClueMechanic { constraint = constraint, lockSteps = lockSteps, hidden = hidden }
        };
    }

    private static SolutionRectData Rect(int clueId, int x, int y, int width, int height)
    {
        return new SolutionRectData { clueId = clueId, x = x, y = y, width = width, height = height };
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
}
