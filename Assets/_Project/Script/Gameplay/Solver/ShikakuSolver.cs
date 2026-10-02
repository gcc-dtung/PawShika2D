using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Giải Shikaku trên LevelDefinition. Dùng để:
/// - Kiểm tra 1 nước đi có SAI không (đặt xong thì lưới không còn lời giải nào) -> trừ mạng.
/// - Tìm lời giải khớp với các khối đã đặt (Hint / Reveal ở giai đoạn sau).
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
    }

    public static bool IsSolvable(LevelDefinition level, IList<RectInt> placed)
    {
        return TrySolve(level, placed, out _);
    }

    public static bool TrySolve(LevelDefinition level, IList<RectInt> placed, out List<RectInt> solution)
    {
        solution = null;
        if (level == null || level.clues == null) return false;

        int width = level.size;
        int height = level.Height;
        bool[,] blocked = new bool[width, height];   // Void hoặc đã bị khối khác chiếm
        int[,] clueAt = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                clueAt[x, y] = -1;

        if (level.cellOverrides != null)
        {
            foreach (CellOverrideData o in level.cellOverrides)
            {
                if (o.type == CellType.Void && InBounds(o.x, o.y, width, height))
                    blocked[o.x, o.y] = true;
            }
        }

        List<Clue> clues = new List<Clue>();
        for (int i = 0; i < level.clues.Length; i++)
        {
            ClueData c = level.clues[i];
            if (!InBounds(c.x, c.y, width, height)) continue;
            clues.Add(new Clue { index = i, x = c.x, y = c.y, value = c.value, constraint = c.mechanic.constraint });
            clueAt[c.x, c.y] = i;
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
                        if (!InBounds(x, y, width, height) || blocked[x, y]) return false;
                        blocked[x, y] = true;
                        if (clueAt[x, y] >= 0) solvedClues.Add(clueAt[x, y]);
                    }
                }
            }
        }

        List<Clue> open = clues.FindAll(c => !solvedClues.Contains(c.index));
        List<List<RectInt>> candidates = new List<List<RectInt>>(open.Count);
        foreach (Clue c in open)
        {
            List<RectInt> list = BuildCandidates(c, width, height, blocked, clueAt);
            if (list.Count == 0) return false;
            candidates.Add(list);
        }

        List<RectInt> result = new List<RectInt>(open.Count);
        bool[] done = new bool[open.Count];
        int nodes = 0;
        bool aborted = false;

        bool found = Backtrack(candidates, done, blocked, result, ref nodes, ref aborted);
        if (aborted)
        {
            Debug.LogWarning("[ShikakuSolver] Quá giới hạn tìm kiếm, coi như còn lời giải.");
            return true;
        }

        if (found) solution = result;
        return found;
    }

    private static bool Backtrack(List<List<RectInt>> candidates, bool[] done, bool[,] occupied,
        List<RectInt> result, ref int nodes, ref bool aborted)
    {
        if (++nodes > MaxNodes)
        {
            aborted = true;
            return false;
        }

        // Chọn clue có ít lựa chọn hợp lệ nhất (MRV)
        int best = -1;
        List<RectInt> bestOptions = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (done[i]) continue;

            List<RectInt> options = new List<RectInt>();
            foreach (RectInt r in candidates[i])
            {
                if (IsFree(r, occupied)) options.Add(r);
            }

            if (options.Count == 0) return false;
            if (bestOptions == null || options.Count < bestOptions.Count)
            {
                best = i;
                bestOptions = options;
                if (options.Count == 1) break;
            }
        }

        if (best < 0) return true; // Mọi clue đã có khối

        done[best] = true;
        foreach (RectInt r in bestOptions)
        {
            SetCells(r, occupied, true);
            result.Add(r);

            if (Backtrack(candidates, done, occupied, result, ref nodes, ref aborted)) return true;

            result.RemoveAt(result.Count - 1);
            SetCells(r, occupied, false);
            if (aborted) break;
        }
        done[best] = false;
        return false;
    }

    private static List<RectInt> BuildCandidates(Clue c, int width, int height, bool[,] blocked, int[,] clueAt)
    {
        List<RectInt> list = new List<RectInt>();
        for (int h = 1; h <= c.value; h++)
        {
            if (c.value % h != 0) continue;
            int w = c.value / h;
            if (!MatchesConstraint(c.constraint, w, h)) continue;

            for (int x0 = c.x - w + 1; x0 <= c.x; x0++)
            {
                for (int y0 = c.y - h + 1; y0 <= c.y; y0++)
                {
                    if (x0 < 0 || y0 < 0 || x0 + w > width || y0 + h > height) continue;

                    bool ok = true;
                    for (int x = x0; x < x0 + w && ok; x++)
                    {
                        for (int y = y0; y < y0 + h; y++)
                        {
                            if (blocked[x, y] || (clueAt[x, y] >= 0 && clueAt[x, y] != c.index))
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
        return list;
    }

    public static bool MatchesConstraint(ClueConstraint constraint, int w, int h)
    {
        switch (constraint)
        {
            case ClueConstraint.Horizontal: return w > h;
            case ClueConstraint.Vertical: return h > w;
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
