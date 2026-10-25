using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Kiểm tra level trước khi đưa vào game:
    /// - Data: clue nằm trong lưới, không trùng / không nằm trên Void, tổng số = số ô chơi được,
    ///   hướng hợp lệ với số (vuông cần số chính phương, số 1 không gắn hướng), không có khóa + "?",
    ///   tường nằm giữa 2 ô chơi được (không trùng), dấu góc / cạnh trên số 1 thì cảnh báo.
    /// - Có lời giải.
    /// - Level có ô "?" / ô khóa / số mơ hồ: giải được KHÔNG CẦN ĐOÁN. Mô phỏng người chơi chỉ đặt khối chắc chắn đúng
    ///   (mọi cách đặt khác của clue đó đều làm lưới hết lời giải), trong khi chưa biết số của ô "?"
    ///   và ô còn khóa. Kẹt giữa chừng = người chơi buộc phải đoán (dễ đặt nhầm, mất thời gian gỡ).
    /// </summary>
    public static class LevelValidator
    {
        private const string DatabasePath = "Assets/_Project/Data/Levels/LevelDatabase.asset";

        public class Report
        {
            public readonly List<string> errors = new List<string>();
            public readonly List<string> warnings = new List<string>();
            public bool IsValid => errors.Count == 0;
        }

        [MenuItem("Tools/PawBlock/Validate Levels", priority = 4)]
        public static void ValidateDatabase()
        {
            LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[LevelValidator] Không tìm thấy {DatabasePath}");
                return;
            }

            int errorLevels = 0;
            int warningLevels = 0;
            try
            {
                for (int i = 0; i < database.levels.Count; i++)
                {
                    LevelDefinition level = database.levels[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Validate Levels", $"Level {i + 1}/{database.levels.Count}", (float)i / database.levels.Count))
                    {
                        Debug.LogWarning("[LevelValidator] Đã hủy.");
                        break;
                    }

                    if (level == null)
                    {
                        Debug.LogError($"[LevelValidator] Level {i + 1}: ô trống trong database");
                        errorLevels++;
                        continue;
                    }

                    Report report = Validate(level);
                    if (!report.IsValid) errorLevels++;
                    else if (report.warnings.Count > 0) warningLevels++;
                    Log(level, i + 1, report, logPassed: false);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[LevelValidator] Xong {database.levels.Count} level: {errorLevels} level lỗi, {warningLevels} level cảnh báo.");
        }

        public static void Log(LevelDefinition level, int number, Report report, bool logPassed)
        {
            string head = $"[LevelValidator] Level {number} ({level.name})";
            if (!report.IsValid) Debug.LogError($"{head}\n{Join(report.errors)}{Join(report.warnings)}", level);
            else if (report.warnings.Count > 0) Debug.LogWarning($"{head}\n{Join(report.warnings)}", level);
            else if (logPassed) Debug.Log($"{head}: hợp lệ", level);
        }

        /// <param name="alwaysCheckGuessing">Kiểm tra "không cần đoán" cả khi level không có ô "?" / ô khóa
        /// (Mechanic Injector dùng để biết level gốc có gắn được khóa / "?" không).</param>
        public static Report Validate(LevelDefinition level, bool alwaysCheckGuessing = false)
        {
            Report report = new Report();
            if (!CheckData(level, report)) return report;

            if (!ShikakuSolver.TrySolve(level, null, null, null, out _, out bool aborted))
            {
                report.errors.Add(aborted ? "Không xác định được lời giải (quá giới hạn tìm kiếm)" : "Không có lời giải");
                return report;
            }

            bool hasHiddenInfo = false;
            foreach (ClueData c in level.clues)
            {
                // [MECHANIC] Cơ chế giấu thông tin -> bật kiểm tra "không cần đoán"
                if (c.mechanic.hidden || c.mechanic.lockSteps > 0 || c.mechanic.ambiguity != ClueAmbiguity.None) hasHiddenInfo = true;
            }
            if (hasHiddenInfo || alwaysCheckGuessing) CheckNoGuessing(level, report);

            return report;
        }

        private static bool CheckData(LevelDefinition level, Report report)
        {
            int width = level.size;
            int height = level.Height;
            if (width <= 0 || height <= 0 || level.clues == null || level.clues.Length == 0)
            {
                report.errors.Add("Lưới hoặc danh sách clue rỗng");
                return false;
            }

            HashSet<Vector2Int> voids = new HashSet<Vector2Int>();
            if (level.cellOverrides != null)
            {
                foreach (CellOverrideData o in level.cellOverrides)
                {
                    if (o.type == CellType.Void) voids.Add(new Vector2Int(o.x, o.y));
                }
            }

            HashSet<Vector2Int> cluePositions = new HashSet<Vector2Int>();
            int sum = 0;
            for (int i = 0; i < level.clues.Length; i++)
            {
                ClueData c = level.clues[i];
                Vector2Int p = new Vector2Int(c.x, c.y);
                string name = $"Clue #{i} ({c.x},{c.y}) = {c.value}";
                sum += c.value;

                if (c.x < 0 || c.y < 0 || c.x >= width || c.y >= height) report.errors.Add($"{name}: nằm ngoài lưới");
                if (voids.Contains(p)) report.errors.Add($"{name}: nằm trên ô Void");
                if (!cluePositions.Add(p)) report.errors.Add($"{name}: trùng vị trí với clue khác");
                if (c.value <= 0) report.errors.Add($"{name}: số phải > 0");

                // [MECHANIC] Luật dữ liệu của cơ chế (kết hợp cấm, giá trị hợp lệ)
                ClueMechanic m = c.mechanic;
                if (m.hidden && m.lockSteps > 0) report.errors.Add($"{name}: vừa khóa vừa \"?\" (mở khóa xong vẫn không biết số)");
                if (m.lockSteps >= level.clues.Length) report.errors.Add($"{name}: khóa {m.lockSteps} bước nhưng chỉ có {level.clues.Length - 1} khối khác");

                if (m.ambiguity != ClueAmbiguity.None)
                {
                    if (m.hidden) report.errors.Add($"{name}: vừa \"?\" vừa số mơ hồ");
                    if (!AmbiguityRules.Accepts(m, c.value, c.value))
                        report.errors.Add($"{name}: số mơ hồ ({AmbiguityRules.Describe(m, c.value)}) không chứa số thật {c.value}");
                    if (m.ambiguity == ClueAmbiguity.Either && (m.ambiguityValue < 1 || m.ambiguityValue == c.value))
                        report.errors.Add($"{name}: Either cần giá trị thứ hai ≥ 1 và khác số thật");
                    if (m.ambiguity == ClueAmbiguity.AtLeast && m.ambiguityValue <= 1)
                        report.warnings.Add($"{name}: \"≥ {m.ambiguityValue}\" không cho thông tin gì");
                }

                if (m.corner != ClueCorner.None && c.value == 1) report.warnings.Add($"{name}: dấu {m.corner} trên số 1 không cho thêm thông tin");

                switch (m.constraint)
                {
                    case ClueConstraint.Horizontal:
                    case ClueConstraint.Vertical:
                        if (c.value < 2) report.errors.Add($"{name}: số 1 không thể {m.constraint}");
                        break;
                    case ClueConstraint.Square:
                        int root = Mathf.RoundToInt(Mathf.Sqrt(c.value));
                        if (root * root != c.value) report.errors.Add($"{name}: Square nhưng {c.value} không phải số chính phương");
                        else if (c.value == 1) report.warnings.Add($"{name}: Square trên số 1 không cho thêm thông tin");
                        break;
                }
            }

            if (level.walls != null)
            {
                HashSet<WallData> seen = new HashSet<WallData>();
                foreach (WallData w in level.walls)
                {
                    Vector2Int a = WallRules.CellA(w), b = WallRules.CellB(w);
                    string name = $"Tường ({w.x},{w.y}) {w.side}";
                    bool inside = a.x >= 0 && a.y >= 0 && b.x < width && b.y < height;
                    if (!inside) report.errors.Add($"{name}: nằm ngoài lưới / trên viền lưới");
                    else if (voids.Contains(a) || voids.Contains(b)) report.errors.Add($"{name}: sát ô Void (không chặn gì)");
                    if (!seen.Add(w)) report.errors.Add($"{name}: trùng");
                }
            }

            // Dấu góc / cạnh phải khớp vị trí ô số trong khối của lời giải đã lưu (nếu có)
            if (level.solution != null)
            {
                foreach (ClueData c in level.clues)
                {
                    if (c.mechanic.corner == ClueCorner.None) continue;
                    foreach (SolutionRectData s in level.solution)
                    {
                        RectInt rect = new RectInt(s.x, s.y, s.width, s.height);
                        Vector2Int p = new Vector2Int(c.x, c.y);
                        if (!rect.Contains(p)) continue;
                        if (!ShikakuSolver.MatchesCorner(c.mechanic.corner, rect, p))
                            report.errors.Add($"Clue ({c.x},{c.y}): dấu {c.mechanic.corner} không khớp vị trí số trong khối lời giải");
                        break;
                    }
                }
            }

            int playable = width * height - voids.Count;
            if (sum != playable) report.errors.Add($"Tổng các số = {sum}, số ô chơi được = {playable}");

            return report.errors.Count == 0;
        }

        /// <summary>
        /// Mô phỏng người chơi chỉ đặt khối chắc chắn đúng, chưa biết số của ô "?" và ô còn khóa.
        /// </summary>
        private static void CheckNoGuessing(LevelDefinition level, Report report)
        {
            int count = level.clues.Length;
            int[] lockSteps = new int[count];
            HashSet<int> hidden = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                lockSteps[i] = level.clues[i].mechanic.lockSteps;
                if (level.clues[i].mechanic.hidden) hidden.Add(i);
            }

            List<RectInt> placed = new List<RectInt>();
            bool[] done = new bool[count];
            bool anyAborted = false;

            while (placed.Count < count)
            {
                // [MECHANIC] Mô phỏng người chơi: cơ chế che số / hướng thì thêm vào unknownValue / unknownConstraint (giống LevelDifficultyAnalyzer)
                // Người chơi chưa biết: số của ô "?" và ô còn khóa; hướng của ô còn khóa
                HashSet<int> unknownValue = new HashSet<int>(hidden);
                HashSet<int> unknownConstraint = new HashSet<int>();
                for (int i = 0; i < count; i++)
                {
                    if (lockSteps[i] > 0)
                    {
                        unknownValue.Add(i);
                        unknownConstraint.Add(i);
                    }
                }

                int forcedClue = -1;
                RectInt forcedRect = default;
                for (int i = 0; i < count && forcedClue < 0; i++)
                {
                    if (done[i] || lockSteps[i] > 0) continue;

                    int solvable = 0;
                    RectInt last = default;
                    foreach (RectInt r in ShikakuSolver.GetCandidates(level, placed, i, unknownValue, unknownConstraint))
                    {
                        placed.Add(r);
                        bool ok = ShikakuSolver.TrySolve(level, placed, unknownValue, unknownConstraint, out _, out bool aborted);
                        placed.RemoveAt(placed.Count - 1);

                        if (aborted)
                        {
                            anyAborted = true;
                            ok = true; // không chắc -> coi như còn khả năng, clue này không chắc chắn
                        }
                        if (!ok) continue;

                        solvable++;
                        last = r;
                        if (solvable > 1) break;
                    }

                    if (solvable == 1)
                    {
                        forcedClue = i;
                        forcedRect = last;
                    }
                }

                if (forcedClue < 0)
                {
                    List<string> remaining = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        if (done[i]) continue;
                        ClueData c = level.clues[i];
                        string tag = lockSteps[i] > 0 ? $" (khóa {lockSteps[i]})" : hidden.Contains(i) ? " (?)" : "";
                        remaining.Add($"#{i} ({c.x},{c.y}){tag}");
                    }
                    string reason = anyAborted ? " (có lần quá giới hạn tìm kiếm, kết quả có thể chưa chính xác)" : "";
                    report.errors.Add($"Phải đoán: sau {placed.Count}/{count} khối không còn nước nào chắc chắn đúng{reason}. Còn lại: {string.Join(", ", remaining)}");
                    return;
                }

                placed.Add(forcedRect);
                done[forcedClue] = true;
                for (int i = 0; i < count; i++)
                {
                    if (lockSteps[i] > 0) lockSteps[i]--;
                }
            }

            if (anyAborted) report.warnings.Add("Có lần quá giới hạn tìm kiếm khi kiểm tra \"không cần đoán\"");
        }

        private static string Join(List<string> lines)
        {
            if (lines.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            foreach (string line in lines) sb.Append("- ").AppendLine(line);
            return sb.ToString();
        }
    }
}
