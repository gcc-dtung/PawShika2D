using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Luật tường: khối không được nằm vắt qua tường (chứa cả 2 ô ở 2 bên). Dùng chung cho BoardModel (kéo / đặt) và ShikakuSolver.
    /// </summary>
    public static class WallRules
    {
        public static Vector2Int CellA(WallData w) => new Vector2Int(w.x, w.y);

        public static Vector2Int CellB(WallData w) => w.side == WallSide.Right ? new Vector2Int(w.x + 1, w.y) : new Vector2Int(w.x, w.y + 1);

        /// <summary>Vùng chứa cả 2 ô ở 2 bên 1 tường nào đó.</summary>
        public static bool Crosses(WallData[] walls, RectInt rect)
        {
            if (walls == null) return false;
            foreach (WallData w in walls)
            {
                if (rect.Contains(CellA(w)) && rect.Contains(CellB(w))) return true;
            }
            return false;
        }

        public static bool Crosses(LevelDefinition level, RectInt rect) => level != null && Crosses(level.walls, rect);

        /// <summary>Có tường giữa 2 ô kề nhau (thứ tự 2 ô không quan trọng).</summary>
        public static bool TryFind(WallData[] walls, Vector2Int a, Vector2Int b, out WallData wall)
        {
            wall = default;
            if (walls == null) return false;
            foreach (WallData w in walls)
            {
                Vector2Int wa = CellA(w), wb = CellB(w);
                if ((wa == a && wb == b) || (wa == b && wb == a))
                {
                    wall = w;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Đặt tối đa <paramref name="count"/> tường ngẫu nhiên trên ranh giới giữa 2 khối khác nhau của lời giải
        /// (lời giải luôn còn đúng). Bản thử: chưa kiểm tra tường có giúp suy luận hay không.
        /// </summary>
        public static WallData[] RandomOnSolution(IList<RectInt> solution, int count, System.Random rng)
        {
            List<WallData> edges = new List<WallData>();
            if (solution != null)
            {
                foreach (RectInt r in solution)
                {
                    // Cạnh phải và cạnh trên của khối: ô bên kia thuộc khối khác (nếu có)
                    for (int y = r.yMin; y < r.yMax; y++) TryAdd(solution, edges, new WallData { x = r.xMax - 1, y = y, side = WallSide.Right });
                    for (int x = r.xMin; x < r.xMax; x++) TryAdd(solution, edges, new WallData { x = x, y = r.yMax - 1, side = WallSide.Top });
                }
            }

            for (int i = edges.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (edges[i], edges[j]) = (edges[j], edges[i]);
            }
            if (edges.Count > count) edges.RemoveRange(count, edges.Count - count);
            return edges.ToArray();
        }

        private static void TryAdd(IList<RectInt> solution, List<WallData> edges, WallData w)
        {
            Vector2Int b = CellB(w);
            foreach (RectInt r in solution)
            {
                if (r.Contains(b))
                {
                    edges.Add(w);
                    return;
                }
            }
        }
    }
}
