using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Tools → PawBlock → Builders → Reorder Levels: xếp lại thứ tự level theo độ khó lý thuyết rồi gắn lại cơ chế.
    /// 1. Level 1 = Levels/Custom/Level_Custom_Level01_4x4 (giữ đầu). Level Test / Custom khác giữ ở cuối.
    /// 2. Level ShikaCat gốc (bỏ những level trong Excluded và level chỉ có 1 khối) chấm điểm D bằng LevelDifficultyAnalyzer
    ///    (bản gốc, chưa cơ chế), xếp D tăng dần.
    /// 3. Từ level 11, mỗi khối 10 level: vị trí ...9 (đỉnh) là level khó nhất trong khối này và khối kế tiếp,
    ///    dễ nhất ở ...0 (level nghỉ), còn lại tăng dần.
    /// 4. Đánh dấu: vị trí ...9 từ level 19 là Hard, ...29 / ...59 / ...89 (chia 30 dư 29) là Very Hard; còn lại Normal.
    ///    Ghi vào bản gốc ShikaCat để Mechanic Injector đọc (level Hard / Very Hard kết hợp 2 / 3 cơ chế).
    /// 5. Chạy Mechanic Injector cho cả database theo lịch trong MechanicInjectorSettings.asset.
    /// </summary>
    public static class LevelOrderTool
    {
        private const string DatabasePath = "Assets/_Project/Data/Levels/LevelDatabase.asset";
        private const string ShikaCatFolder = "Assets/_Project/Data/Levels/ShikaCat";
        private const string FirstLevelPath = "Assets/_Project/Data/Levels/Custom/Level_Custom_Level01_4x4.asset";
        private const string DifficultySettingsPath = "Assets/_Project/Data/Levels/LevelDifficultySettings.asset";

        /// <summary>Level ShikaCat không đưa vào game: 001 (trùng bước 1 tutorial, thay bằng Level 1 4x4), 004 (1 cột 3 ô, 1 số).</summary>
        private static readonly HashSet<string> Excluded = new HashSet<string> { "Level_001", "Level_004" };

        private const int RampEnd = 10;       // level 1–10 chỉ tăng dần, chưa có đỉnh / level nghỉ
        private const int FirstHard = 19;

        [MenuItem("Tools/PawBlock/Builders/Reorder Levels", priority = 112)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("Reorder Levels",
                    "Xếp lại thứ tự level theo độ khó, đánh dấu lại Hard / Very Hard và chạy lại Mechanic Injector cho cả database?\n" +
                    "Tiến độ người chơi (save) lưu theo số thứ tự nên sẽ lệch.", "Xếp lại", "Hủy"))
                return;
            Debug.Log("[LevelOrderTool] " + Apply());
        }

        /// <summary>Chạy không hỏi (gọi từ code / MCP). Trả về tóm tắt.</summary>
        public static string Apply()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            LevelDifficultySettings weights = AssetDatabase.LoadAssetAtPath<LevelDifficultySettings>(DifficultySettingsPath);
            if (db == null || weights == null) return "Thiếu LevelDatabase hoặc LevelDifficultySettings";

            LevelDefinition first = AssetDatabase.LoadAssetAtPath<LevelDefinition>(FirstLevelPath);
            // Level ngoài ShikaCat / Mechanic (Test, Custom tự làm) giữ ở cuối theo thứ tự cũ
            List<LevelDefinition> tail = db.levels
                .Where(l => l != null && l != first && !IsShikaCatOrMechanic(l))
                .ToList();

            // Bản gốc ShikaCat, bỏ đánh dấu cũ rồi chấm điểm
            List<(LevelDefinition level, float score)> scored = new List<(LevelDefinition, float)>();
            HashSet<LevelDefinition> multiSolution = new HashSet<LevelDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { ShikaCatFolder }))
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                // Bỏ level chỉ có 1 khối: tutorial đã dạy thao tác này, đặt sau Level 1 4x4 thì tụt độ khó
                if (level == null || Excluded.Contains(level.name) || level.clues == null || level.clues.Length <= 1) continue;
                SetDifficulty(level, LevelDifficulty.Normal);
                LevelDifficultyAnalyzer.Report r = LevelDifficultyAnalyzer.Analyze(level, weights);
                // Bước T4 (nhiều lời giải) làm D cao giả: chọn cách nào còn giải được cũng đúng -> bỏ khỏi điểm xếp thứ tự
                float score = r.score - weights.wT4 * r.t4 / Mathf.Max(1, r.Steps);
                if (r.t4 > 0) multiSolution.Add(level);
                scored.Add((level, score));
            }
            List<LevelDefinition> ordered = scored
                .OrderBy(s => s.score)
                .ThenBy(s => s.level.source.sourceIndex)
                .Select(s => s.level)
                .ToList();

            List<LevelDefinition> levels = new List<LevelDefinition>();
            if (first != null) levels.Add(first);
            int offset = levels.Count; // số thứ tự (từ 1) của ordered[i] = offset + i + 1
            ArrangeBlocks(ordered, offset, scored.ToDictionary(s => s.level, s => s.score), multiSolution);
            levels.AddRange(ordered);

            // Đánh dấu Hard / Very Hard theo vị trí
            int hard = 0, veryHard = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                int number = offset + i + 1;
                LevelDifficulty d = DifficultyAt(number);
                SetDifficulty(ordered[i], d);
                if (d == LevelDifficulty.Hard) hard++;
                else if (d == LevelDifficulty.VeryHard) veryHard++;
            }
            levels.AddRange(tail);

            Undo.RecordObject(db, "Reorder Levels");
            db.levels = levels;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            string injector = MechanicInjectorWindow.ApplyRange(1, db.Count);
            return $"{ordered.Count} level ShikaCat xếp theo D (bỏ {string.Join(", ", Excluded)}), level 1 = {(first != null ? first.name : "—")}, " +
                   $"{tail.Count} level giữ cuối; Hard {hard}, Very Hard {veryHard}. Mechanic Injector: {injector}";
        }

        /// <summary>
        /// Mỗi khối 10 level từ sau RampEnd: level đỉnh ở vị trí ...9 là level khó nhất trong khối này VÀ khối kế tiếp
        /// (mượn level khó hơn ở phía sau để đỉnh nổi hẳn lên), dễ nhất → ...0 (nghỉ), còn lại tăng dần.
        /// </summary>
        private static void ArrangeBlocks(List<LevelDefinition> ordered, int offset, Dictionary<LevelDefinition, float> rawScore,
            HashSet<LevelDefinition> multiSolution)
        {
            // Level nhiều lời giải không làm đỉnh (không gắn được "?" / khóa, đỉnh thành level dễ): coi điểm thấp nhất khi chọn đỉnh
            Dictionary<LevelDefinition, float> score = rawScore.ToDictionary(
                kv => kv.Key, kv => multiSolution.Contains(kv.Key) ? kv.Value - 1000f : kv.Value);
            for (int start = 0; start < ordered.Count; start++)
            {
                int number = offset + start + 1;
                if (number <= RampEnd || number % 10 != 1) continue; // khối bắt đầu ở level ...1
                int count = Mathf.Min(10, ordered.Count - start);
                if (count < 10) break; // khối cuối không đủ 10: giữ tăng dần

                // Đổi chỗ level khó nhất của khối kế tiếp vào khối này nếu nó khó hơn level khó nhất hiện có
                int windowEnd = Mathf.Min(ordered.Count, start + 20);
                int peak = start;
                for (int k = start; k < windowEnd; k++)
                    if (score[ordered[k]] > score[ordered[peak]]) peak = k;
                if (peak >= start + count)
                {
                    int blockMax = start;
                    for (int k = start; k < start + count; k++)
                        if (score[ordered[k]] > score[ordered[blockMax]]) blockMax = k;
                    (ordered[peak], ordered[blockMax]) = (ordered[blockMax], ordered[peak]);
                }

                // Đỉnh chọn theo score (bỏ level nhiều lời giải); dễ nhất / thứ tự giữa theo điểm thật
                List<LevelDefinition> block = ordered.GetRange(start, count);
                LevelDefinition hardest = block.OrderBy(l => score[l]).Last();
                block.Remove(hardest);
                block = block.OrderBy(l => rawScore[l]).ToList();
                LevelDefinition easiest = block[0];
                List<LevelDefinition> middle = block.GetRange(1, block.Count - 1);
                List<LevelDefinition> arranged = new List<LevelDefinition>(middle) { hardest, easiest }; // ...1–8, ...9 đỉnh, ...0 nghỉ
                for (int k = 0; k < count; k++) ordered[start + k] = arranged[k];
            }
        }

        public static LevelDifficulty DifficultyAt(int number)
        {
            if (number < FirstHard || number % 10 != 9) return LevelDifficulty.Normal;
            return number % 30 == 29 ? LevelDifficulty.VeryHard : LevelDifficulty.Hard;
        }

        private static bool IsShikaCatOrMechanic(LevelDefinition level)
        {
            string path = AssetDatabase.GetAssetPath(level);
            return path.StartsWith(ShikaCatFolder + "/") || path.StartsWith(MechanicInjectorWindow.OutputFolder + "/");
        }

        private static void SetDifficulty(LevelDefinition level, LevelDifficulty d)
        {
            if (level.difficulty == d) return;
            level.difficulty = d;
            EditorUtility.SetDirty(level);
        }
    }
}
