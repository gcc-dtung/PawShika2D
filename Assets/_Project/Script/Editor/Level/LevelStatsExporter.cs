using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Xuất số liệu chơi thật (SaveData.levelStats, do LevelStatsRecorder ghi) ra CSV để chỉnh đường độ khó:
/// Tools → PawBlock → Level Difficulty → "Nạp dữ liệu thực tế" chọn file này để so với độ khó lý thuyết (tương quan Spearman).
/// - Từ save máy này, hoặc gộp nhiều file save.json của người chơi thử (đặt chung 1 thư mục).
/// - Cột khóa "name" (tên asset level) nên vẫn khớp khi đã xếp lại thứ tự level.
/// </summary>
public static class LevelStatsExporter
{
    private const string Menu = "Tools/PawBlock/Level Stats/";
    private const string DatabasePath = "Assets/_Project/Levels/LevelDatabase.asset";

    [MenuItem(Menu + "Xuất CSV (save máy này)", priority = 4)]
    private static void ExportLocal()
    {
        SaveData data = SaveSystem.Data;
        Export(data.levelStats, "LevelStats_Real.csv", 1);
    }

    [MenuItem(Menu + "Xuất CSV gộp nhiều save.json...", priority = 5)]
    private static void ExportFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Thư mục chứa các file save .json của người chơi thử", "", "");
        if (string.IsNullOrEmpty(folder)) return;

        Dictionary<string, LevelStatSave> merged = new Dictionary<string, LevelStatSave>();
        int files = 0;
        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LevelStats] Bỏ qua {Path.GetFileName(path)}: {e.Message}");
                continue;
            }
            if (data == null || data.levelStats == null) continue;
            files++;
            foreach (LevelStatSave s in data.levelStats) Merge(merged, s);
        }
        Export(new List<LevelStatSave>(merged.Values), "LevelStats_Real_Merged.csv", files);
    }

    [MenuItem(Menu + "Log tóm tắt", priority = 6)]
    private static void LogSummary()
    {
        List<LevelStatSave> list = SaveSystem.Data.levelStats;
        int attempts = 0, wins = 0;
        foreach (LevelStatSave s in list)
        {
            attempts += s.attempts;
            wins += s.wins;
        }
        Debug.Log($"[LevelStats] {list.Count} level có số liệu, {attempts} lượt, {wins} thắng (save: {SaveSystem.FilePath})");
    }

    [MenuItem(Menu + "Xóa số liệu", priority = 7)]
    private static void Clear()
    {
        if (!EditorUtility.DisplayDialog("Xóa số liệu level", "Xóa toàn bộ SaveData.levelStats trên máy này?", "Xóa", "Thôi")) return;
        SaveSystem.Data.levelStats.Clear();
        SaveSystem.MarkDirty();
        SaveSystem.Save(true);
    }

    private static void Merge(Dictionary<string, LevelStatSave> merged, LevelStatSave s)
    {
        if (string.IsNullOrEmpty(s.id)) return;
        if (!merged.TryGetValue(s.id, out LevelStatSave m))
        {
            merged[s.id] = m = new LevelStatSave { id = s.id, level = s.level };
        }
        m.attempts += s.attempts;
        m.wins += s.wins;
        m.losses += s.losses;
        m.quits += s.quits;
        m.outOfLives += s.outOfLives;
        m.mistakes += s.mistakes;
        m.boosters += s.boosters;
        m.playSeconds += s.playSeconds;
        if (s.bestWinSeconds > 0f && (m.bestWinSeconds <= 0f || s.bestWinSeconds < m.bestWinSeconds)) m.bestWinSeconds = s.bestWinSeconds;
    }

    private static void Export(List<LevelStatSave> stats, string fileName, int players)
    {
        if (stats == null || stats.Count == 0)
        {
            EditorUtility.DisplayDialog("Level Stats", "Chưa có số liệu (chơi vài level trong Play mode / trên máy thật trước).", "OK");
            return;
        }
        string path = EditorUtility.SaveFilePanel("Xuất số liệu chơi thật", "Assets/Docs", fileName, "csv");
        if (string.IsNullOrEmpty(path)) return;

        Dictionary<string, int> order = LevelOrder();
        stats.Sort((a, b) => Order(order, a).CompareTo(Order(order, b)));

        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("name,order,attempts,wins,losses,quits,win_rate,out_of_lives_per_attempt,mistakes_per_attempt,boosters_per_attempt,avg_seconds,best_win_seconds");
        foreach (LevelStatSave s in stats)
        {
            float n = Mathf.Max(1, s.attempts);
            sb.AppendLine(string.Join(",", new[]
            {
                s.id,
                Order(order, s).ToString(inv),
                s.attempts.ToString(inv), s.wins.ToString(inv), s.losses.ToString(inv), s.quits.ToString(inv),
                (s.wins / n).ToString("0.000", inv),
                (s.outOfLives / n).ToString("0.000", inv),
                (s.mistakes / n).ToString("0.00", inv),
                (s.boosters / n).ToString("0.00", inv),
                (s.playSeconds / n).ToString("0.0", inv),
                s.bestWinSeconds.ToString("0.0", inv)
            }));
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        Debug.Log($"[LevelStats] Xuất {stats.Count} level ({players} người chơi) ra {path}. Nạp vào Tools → PawBlock → Level Difficulty → Nạp dữ liệu thực tế.");
        EditorUtility.RevealInFinder(path);
    }

    /// <summary>Tên asset level → số thứ tự hiện tại trong LevelDatabase (1, 2...).</summary>
    private static Dictionary<string, int> LevelOrder()
    {
        Dictionary<string, int> order = new Dictionary<string, int>();
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
        if (db == null) return order;
        for (int i = 0; i < db.Count; i++)
        {
            LevelDefinition l = db.Get(i);
            if (l != null && !order.ContainsKey(l.name)) order[l.name] = i + 1;
        }
        return order;
    }

    private static int Order(Dictionary<string, int> order, LevelStatSave s) => order.TryGetValue(s.id, out int n) ? n : s.level;
}
