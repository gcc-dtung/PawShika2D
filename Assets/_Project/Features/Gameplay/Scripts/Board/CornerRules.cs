using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Cơ chế "ô số ở góc / cạnh": luật nằm ở ShikakuSolver.MatchesCorner (dùng chung BoardModel.Check / solver).
    /// Ở đây là phần gắn cơ chế vào level có sẵn lời giải.
    /// </summary>
    public static class CornerRules
    {
        public static readonly ClueCorner[] Corners = { ClueCorner.TopLeft, ClueCorner.TopRight, ClueCorner.BottomLeft, ClueCorner.BottomRight };
        public static readonly ClueCorner[] Edges = { ClueCorner.Top, ClueCorner.Bottom, ClueCorner.Left, ClueCorner.Right };

        public static bool IsEdge(ClueCorner c) => c >= ClueCorner.Top;

        /// <summary>Các góc / cạnh trong <paramref name="kinds"/> của <paramref name="rect"/> mà ô <paramref name="clue"/> đang nằm.</summary>
        public static List<ClueCorner> MatchesOf(RectInt rect, Vector2Int clue, IEnumerable<ClueCorner> kinds)
        {
            List<ClueCorner> res = new List<ClueCorner>(4);
            foreach (ClueCorner c in kinds)
            {
                if (ShikakuSolver.MatchesCorner(c, rect, clue)) res.Add(c);
            }
            return res;
        }

        /// <summary>
        /// Gắn góc / cạnh (chọn trong <paramref name="kinds"/>) cho tối đa <paramref name="count"/> ô số ngẫu nhiên,
        /// đúng với vị trí ô số trong khối của lời giải (lời giải luôn còn đúng). Khối 1 ô bỏ qua (dấu vô nghĩa).
        /// Bản thử: chưa kiểm tra dấu có giúp suy luận hay không. Trả về số ô đã gắn.
        /// </summary>
        public static int AssignRandom(ClueData[] clues, IList<RectInt> solution, IEnumerable<ClueCorner> kinds, int count, System.Random rng)
        {
            if (clues == null || solution == null) return 0;
            List<int> order = new List<int>();
            for (int i = 0; i < clues.Length; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            int done = 0;
            foreach (int i in order)
            {
                if (done >= count) break;
                Vector2Int pos = new Vector2Int(clues[i].x, clues[i].y);
                foreach (RectInt r in solution)
                {
                    if (!r.Contains(pos)) continue;
                    if (r.width * r.height <= 1) break;
                    List<ClueCorner> corners = MatchesOf(r, pos, kinds);
                    if (corners.Count == 0) break;
                    clues[i].mechanic.corner = corners[rng.Next(corners.Count)];
                    done++;
                    break;
                }
            }
            return done;
        }
    }
}
