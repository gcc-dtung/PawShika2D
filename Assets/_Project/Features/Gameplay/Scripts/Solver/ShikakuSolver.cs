using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Giải Shikaku trên LevelDefinition. Dùng để:
    /// - Kiểm tra 1 nước đi có SAI không (đặt xong thì lưới không còn lời giải nào) -> trừ mạng.
    /// - Tìm lời giải khớp với các khối đã đặt (Hint / Reveal ở giai đoạn sau).
    /// - Kiểm tra level (Editor): giải khi người chơi CHƯA biết số của ô "?" / ô khóa.
    /// Không so với solution lưu sẵn vì 17/205 level ShikaCat có nhiều hơn 1 lời giải.
    /// </summary>
    public static class ShikakuSolver
    {
        // Chặn trường hợp xấu: quá số node thì coi như "còn lời giải" để không phạt oan người chơi
        private const int MaxNodes = 500000;

        private class Clue
        {
            public int index;
            public int x;
            public int y;
            public int value;
            public ClueConstraint constraint;
            // [MECHANIC] Thông tin solver cần về clue (luật hình dạng / người chơi chưa biết gì)
            public bool unknownValue; // chưa biết số: khối có thể rộng bất kỳ
        }

        private class State
        {
            public int width;
            public int height;
            public bool[,] blocked;   // Void hoặc đã bị khối khác chiếm
            public int[,] clueAt;
            public List<Clue> open = new List<Clue>();
        }

        public static bool IsSolvable(LevelDefinition level, IList<RectInt> placed)
        {
            return TrySolve(level, placed, out _);
        }

        public static bool TrySolve(LevelDefinition level, IList<RectInt> placed, out List<RectInt> solution)
        {
            bool found = TrySolve(level, placed, null, null, out solution, out bool aborted);
            if (aborted)
            {
                Debug.LogWarning("[ShikakuSolver] Quá giới hạn tìm kiếm, coi như còn lời giải.");
                return true;
            }
            return found;
        }

        /// <summary>
        /// Giải với các clue người chơi chưa biết: unknownValue = chưa biết số (ô "?", ô khóa),
        /// unknownConstraint = chưa biết hướng (ô khóa che mất ô nền điều hướng).
        /// Khi có clue chưa biết số, lời giải phải phủ kín mọi ô còn trống.
        /// </summary>
        public static bool TrySolve(LevelDefinition level, IList<RectInt> placed,
            ICollection<int> unknownValue, ICollection<int> unknownConstraint,
            out List<RectInt> solution, out bool aborted)
        {
            solution = null;
            aborted = false;
            if (!BuildState(level, placed, unknownValue, unknownConstraint, out State state)) return false;

            List<List<RectInt>> candidates = new List<List<RectInt>>(state.open.Count);
            foreach (Clue c in state.open)
            {
                List<RectInt> list = BuildCandidates(c, state);
                if (list.Count == 0) return false;
                candidates.Add(list);
            }

            // Có clue chưa biết số (kể cả đã đặt): tổng diện tích không còn tự khớp -> phải kiểm tra phủ kín
            bool needCoverage = unknownValue != null && unknownValue.Count > 0;
            List<RectInt> result = new List<RectInt>(state.open.Count);
            bool[] done = new bool[state.open.Count];
            int nodes = 0;

            int count = 0;
            bool found = Backtrack(candidates, done, state.blocked, needCoverage, 1, ref count, result, ref nodes, ref aborted);
            if (aborted) return false;

            if (found) solution = result;
            return found;
        }

        /// <summary>
        /// Đếm số lời giải của level chưa đặt khối nào, dừng khi đủ <paramref name="limit"/> (vd. 2 để biết có đúng 1 lời giải).
        /// aborted = quá giới hạn tìm kiếm (kết quả không chắc).
        /// </summary>
        public static int CountSolutions(LevelDefinition level, int limit, out bool aborted)
        {
            aborted = false;
            if (limit <= 0 || !BuildState(level, null, null, null, out State state)) return 0;

            List<List<RectInt>> candidates = new List<List<RectInt>>(state.open.Count);
            foreach (Clue c in state.open)
            {
                List<RectInt> list = BuildCandidates(c, state);
                if (list.Count == 0) return 0;
                candidates.Add(list);
            }

            int count = 0, nodes = 0;
            Backtrack(candidates, new bool[state.open.Count], state.blocked, false, limit, ref count,
                new List<RectInt>(state.open.Count), ref nodes, ref aborted);
            return count;
        }

        /// <summary>Các vùng có thể đặt cho clue (chưa xét clue khác có còn chỗ hay không).</summary>
        public static List<RectInt> GetCandidates(LevelDefinition level, IList<RectInt> placed, int clueIndex,
            ICollection<int> unknownValue, ICollection<int> unknownConstraint)
        {
            if (!BuildState(level, placed, unknownValue, unknownConstraint, out State state)) return new List<RectInt>();
            Clue clue = state.open.Find(c => c.index == clueIndex);
            return clue != null ? BuildCandidates(clue, state) : new List<RectInt>();
        }

        private static bool BuildState(LevelDefinition level, IList<RectInt> placed,
            ICollection<int> unknownValue, ICollection<int> unknownConstraint, out State state)
        {
            state = null;
            if (level == null || level.clues == null) return false;

            State s = new State { width = level.size, height = level.Height };
            s.blocked = new bool[s.width, s.height];
            s.clueAt = new int[s.width, s.height];
            for (int x = 0; x < s.width; x++)
                for (int y = 0; y < s.height; y++)
                    s.clueAt[x, y] = -1;

            if (level.cellOverrides != null)
            {
                foreach (CellOverrideData o in level.cellOverrides)
                {
                    if (o.type == CellType.Void && InBounds(o.x, o.y, s.width, s.height))
                        s.blocked[o.x, o.y] = true;
                }
            }

            List<Clue> clues = new List<Clue>();
            for (int i = 0; i < level.clues.Length; i++)
            {
                ClueData c = level.clues[i];
                if (!InBounds(c.x, c.y, s.width, s.height)) continue;
                bool hideConstraint = unknownConstraint != null && unknownConstraint.Contains(i);
                clues.Add(new Clue
                {
                    index = i,
                    x = c.x,
                    y = c.y,
                    value = c.value,
                    constraint = hideConstraint ? ClueConstraint.None : c.mechanic.constraint,
                    unknownValue = unknownValue != null && unknownValue.Contains(i)
                });
                s.clueAt[c.x, c.y] = i;
            }

            // Các khối đã đặt: đánh dấu chiếm chỗ và loại clue tương ứng khỏi danh sách cần giải
            HashSet<int> solvedClues = new HashSet<int>();
            if (placed != null)
            {
                foreach (RectInt r in placed)
                {
                    for (int x = r.xMin; x < r.xMax; x++)
                    {
                        for (int y = r.yMin; y < r.yMax; y++)
                        {
                            if (!InBounds(x, y, s.width, s.height) || s.blocked[x, y]) return false;
                            s.blocked[x, y] = true;
                            if (s.clueAt[x, y] < 0) continue;

                            // Khối phải đúng số ô của clue (ô "?" người chơi đặt không qua kiểm tra số ô ở BoardModel.Check)
                            int ci = s.clueAt[x, y];
                            bool known = unknownValue == null || !unknownValue.Contains(ci);
                            if (known && r.width * r.height != level.clues[ci].value) return false;
                            solvedClues.Add(ci);
                        }
                    }
                }
            }

            s.open = clues.FindAll(c => !solvedClues.Contains(c.index));
            state = s;
            return true;
        }

        /// <summary>Tìm lời giải; dừng (trả true) khi đã thấy đủ <paramref name="limit"/> lời giải, result giữ lời giải cuối.</summary>
        private static bool Backtrack(List<List<RectInt>> candidates, bool[] done, bool[,] occupied, bool needCoverage,
            int limit, ref int found, List<RectInt> result, ref int nodes, ref bool aborted)
        {
            if (++nodes > MaxNodes)
            {
                aborted = true;
                return false;
            }

            int width = occupied.GetLength(0);
            int height = occupied.GetLength(1);
            bool[,] coverable = needCoverage ? new bool[width, height] : null;

            // Chọn clue có ít lựa chọn hợp lệ nhất (MRV)
            int best = -1;
            List<RectInt> bestOptions = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (done[i]) continue;

                List<RectInt> options = new List<RectInt>();
                foreach (RectInt r in candidates[i])
                {
                    if (IsFree(r, occupied))
                    {
                        options.Add(r);
                        if (needCoverage) SetCells(r, coverable, true);
                    }
                }

                if (options.Count == 0) return false;
                if (bestOptions == null || options.Count < bestOptions.Count)
                {
                    best = i;
                    bestOptions = options;
                    if (options.Count == 1 && !needCoverage) break;
                }
            }

            // Có clue chưa biết số: mọi ô trống phải còn ít nhất 1 khối có thể phủ
            if (needCoverage)
            {
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        if (!occupied[x, y] && !coverable[x, y]) return false;
            }

            if (best < 0) return ++found >= limit; // Mọi clue đã có khối

            done[best] = true;
            foreach (RectInt r in bestOptions)
            {
                SetCells(r, occupied, true);
                result.Add(r);

                if (Backtrack(candidates, done, occupied, needCoverage, limit, ref found, result, ref nodes, ref aborted)) return true;

                result.RemoveAt(result.Count - 1);
                SetCells(r, occupied, false);
                if (aborted) break;
            }
            done[best] = false;
            return false;
        }

        private static List<RectInt> BuildCandidates(Clue c, State s)
        {
            List<RectInt> list = new List<RectInt>();
            for (int h = 1; h <= s.height; h++)
            {
                for (int w = 1; w <= s.width; w++)
                {
                    if (!c.unknownValue && w * h != c.value) continue;
                    // [MECHANIC] Luật hình dạng mới: lọc ứng viên ở đây (phải khớp đúng luật trong BoardModel.Check)
                    if (!MatchesConstraint(c.constraint, w, h)) continue;

                    for (int x0 = c.x - w + 1; x0 <= c.x; x0++)
                    {
                        for (int y0 = c.y - h + 1; y0 <= c.y; y0++)
                        {
                            if (x0 < 0 || y0 < 0 || x0 + w > s.width || y0 + h > s.height) continue;

                            bool ok = true;
                            for (int x = x0; x < x0 + w && ok; x++)
                            {
                                for (int y = y0; y < y0 + h; y++)
                                {
                                    if (s.blocked[x, y] || (s.clueAt[x, y] >= 0 && s.clueAt[x, y] != c.index))
                                    {
                                        ok = false;
                                        break;
                                    }
                                }
                            }

                            if (ok) list.Add(new RectInt(x0, y0, w, h));
                        }
                    }
                }
            }
            return list;
        }

        public static bool MatchesConstraint(ClueConstraint constraint, int w, int h)
        {
            switch (constraint)
            {
                case ClueConstraint.Horizontal: return w > h;
                case ClueConstraint.Vertical: return h > w;
                case ClueConstraint.Square: return w == h;
                default: return true;
            }
        }

        private static bool IsFree(RectInt r, bool[,] occupied)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    if (occupied[x, y]) return false;
            return true;
        }

        private static void SetCells(RectInt r, bool[,] occupied, bool value)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    occupied[x, y] = value;
        }

        private static bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
}
