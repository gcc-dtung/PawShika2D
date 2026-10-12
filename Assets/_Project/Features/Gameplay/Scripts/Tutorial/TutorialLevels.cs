using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// 4 bước tutorial lần đầu mở game (như bản gốc), dựng LevelDefinition trong code, không cần asset.
    /// y = 0 ở hàng dưới cùng như trong game. Gợi ý = các lượt kéo bàn tay minh họa (từ ô -> tới ô), theo thứ tự.
    /// </summary>
    public static class TutorialLevels
    {
        public class Step
        {
            public string message;
            public LevelDefinition level;
            /// <summary>Bàn tay kéo mẫu cho từng số: (vị trí số, ô bắt đầu kéo, ô kết thúc). Rỗng = không có bàn tay.</summary>
            public readonly List<(Vector2Int clue, Vector2Int from, Vector2Int to)> hints = new List<(Vector2Int, Vector2Int, Vector2Int)>();
        }

        public static List<Step> Create()
        {
            List<Step> steps = new List<Step>();

            // 1. Một hàng 3 ô, số 3 ở giữa: kéo ngang
            Step s1 = NewStep("First, drag to create a rectangle with 3 cells.", "tutorial_1", 3, 1,
                Clue(0, 1, 0, 3));
            Solve(s1.level, (0, 0, 0, 3, 1));
            s1.hints.Add((new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(2, 0)));
            steps.Add(s1);

            // 2. Một cột 3 ô: kéo dọc
            Step s2 = NewStep("Try another 3-cell rectangle in a different direction.", "tutorial_2", 1, 3,
                Clue(0, 0, 1, 3));
            Solve(s2.level, (0, 0, 0, 1, 3));
            s2.hints.Add((new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 0)));
            steps.Add(s2);

            // 3. Lưới 3x3: 3 (cột trái) + 6 (2 cột phải)
            Step s3 = NewStep("Each number tells you how many cells its rectangle should cover.", "tutorial_3", 3, 3,
                Clue(0, 0, 2, 3), Clue(1, 1, 2, 6));
            Solve(s3.level, (0, 0, 0, 1, 3), (1, 1, 0, 2, 3));
            s3.hints.Add((new Vector2Int(0, 2), new Vector2Int(0, 2), new Vector2Int(0, 0)));
            s3.hints.Add((new Vector2Int(1, 2), new Vector2Int(1, 2), new Vector2Int(2, 0)));
            steps.Add(s3);

            // 4. Lưới 3x3 tự giải: 2 (dọc trái trên), 4 (vuông phải trên), 3 (hàng dưới)
            Step s4 = NewStep("Now, draw a rectangle around every number using the correct number of cells.", "tutorial_4", 3, 3,
                Clue(0, 0, 2, 2), Clue(1, 2, 1, 4), Clue(2, 1, 0, 3));
            Solve(s4.level, (0, 0, 1, 1, 2), (1, 1, 1, 2, 2), (2, 0, 0, 3, 1));
            steps.Add(s4);

            return steps;
        }

        private static Step NewStep(string message, string id, int width, int height, params ClueData[] clues)
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = id;
            level.id = id;
            level.source = new LevelSource { game = "Tutorial", sourceIndex = -1 };
            level.size = width;
            level.height = height;
            level.cellOverrides = new CellOverrideData[0];
            level.clues = clues;
            return new Step { message = message, level = level };
        }

        private static ClueData Clue(int id, int x, int y, int value)
        {
            return new ClueData { id = id, x = x, y = y, value = value, color = id };
        }

        private static void Solve(LevelDefinition level, params (int clueId, int x, int y, int w, int h)[] rects)
        {
            level.solution = new SolutionRectData[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                level.solution[i] = new SolutionRectData { clueId = r.clueId, x = r.x, y = r.y, width = r.w, height = r.h };
            }
        }
    }
}
