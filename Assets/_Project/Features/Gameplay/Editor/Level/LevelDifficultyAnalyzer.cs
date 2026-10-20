using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Độ khó LÝ THUYẾT của 1 level: mô phỏng người chơi giải từng khối, mỗi bước dùng kỹ thuật đơn giản nhất còn tác dụng.
    /// Người chơi chưa biết số của ô "?" và ô còn khóa, chưa biết hướng của ô còn khóa; ô khóa chưa đặt được.
    /// - T1 (đơn trực tiếp): có số chỉ còn đúng 1 cách đặt.
    /// - T2 (loại trừ 1 bước): bỏ các cách đặt làm 1 số khác hết chỗ, hoặc làm 1 ô trống không còn số nào phủ được -> còn 1 cách.
    /// - T3 (thử sâu): chỉ 1 cách đặt còn giải tiếp được tới hết lưới (ShikakuSolver) – người chơi phải nhẩm nhiều bước / phản chứng.
    /// - T4 (không có nước bắt buộc): mọi số còn ≥ 2 cách đặt giải tiếp được (level nhiều lời giải). Game chấm bằng solver nên
    ///   chọn cách nào còn giải được cũng đúng; mô phỏng đặt theo 1 lời giải rồi đi tiếp.
    /// Công thức và ý nghĩa: Assets/Docs/DifficultyFormula.md.
    /// </summary>
    public static class LevelDifficultyAnalyzer
    {
        public class Report
        {
            public int clues, cells, voids, constraints, locks, lockSteps, hidden, walls, corners;
            /// <summary>Trung bình log2(số cách đặt) của mỗi số lúc chưa đặt gì.</summary>
            public float ambiguity;
            public int t1, t2, t3, t4;
            /// <summary>Tỉ lệ bước chỉ có đúng 1 nước chắc chắn ở kỹ thuật thấp nhất.</summary>
            public float narrow;
            public int maxTechnique;
            /// <summary>Kỹ thuật từng bước theo thứ tự giải, vd "1121131".</summary>
            public string trace = "";
            public bool aborted;
            public string error;

            public float score, seconds, risk;

            public int Steps => t1 + t2 + t3 + t4;
        }

        public static Report Analyze(LevelDefinition level, LevelDifficultySettings w)
        {
            Report rep = new Report();
            if (level == null || level.clues == null || level.clues.Length == 0)
            {
                rep.error = "Level rỗng";
                return rep;
            }

            int width = level.size, height = level.Height;
            bool[,] voids = new bool[width, height];
            if (level.cellOverrides != null)
            {
                foreach (CellOverrideData o in level.cellOverrides)
                {
                    if (o.type != CellType.Void || o.x < 0 || o.y < 0 || o.x >= width || o.y >= height || voids[o.x, o.y]) continue;
                    voids[o.x, o.y] = true;
                    rep.voids++;
                }
            }

            int count = level.clues.Length;
            rep.clues = count;
            rep.cells = width * height - rep.voids;
            int[] lockSteps = new int[count];
            HashSet<int> hidden = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                // [MECHANIC] Đếm cơ chế cho báo cáo độ khó (thêm field vào Report + trọng số LevelDifficultySettings nếu cần)
                ClueMechanic m = level.clues[i].mechanic;
                lockSteps[i] = m.lockSteps;
                if (m.lockSteps > 0) { rep.locks++; rep.lockSteps += m.lockSteps; }
                if (m.hidden) { rep.hidden++; hidden.Add(i); }
                if (m.constraint != ClueConstraint.None) rep.constraints++;
                if (m.corner != ClueCorner.None) rep.corners++;
            }
            rep.walls = level.walls != null ? level.walls.Length : 0;

            List<RectInt> placed = new List<RectInt>();
            bool[] done = new bool[count];
            int narrowSteps = 0;
            StringBuilder trace = new StringBuilder();

            while (placed.Count < count)
            {
                HashSet<int> unknownValue = new HashSet<int>(hidden);
                HashSet<int> unknownConstraint = new HashSet<int>();
                for (int i = 0; i < count; i++)
                {
                    if (lockSteps[i] <= 0) continue;
                    unknownValue.Add(i);
                    unknownConstraint.Add(i);
                }

                List<RectInt>[] cand = new List<RectInt>[count];
                for (int i = 0; i < count; i++)
                    cand[i] = done[i] ? null : ShikakuSolver.GetCandidates(level, placed, i, unknownValue, unknownConstraint);

                if (placed.Count == 0)
                {
                    float sum = 0f;
                    for (int i = 0; i < count; i++) sum += Mathf.Log(Mathf.Max(1, cand[i].Count), 2f);
                    rep.ambiguity = sum / count;
                }

                int technique = 0, clue = -1;
                RectInt rect = default;
                int available = 0;

                // T1: số chỉ còn 1 cách đặt
                for (int i = 0; i < count; i++)
                {
                    if (done[i] || lockSteps[i] > 0 || cand[i].Count != 1) continue;
                    if (available++ == 0) { clue = i; rect = cand[i][0]; }
                }
                if (available > 0) technique = 1;

                // T2: loại trừ 1 bước
                if (technique == 0)
                {
                    bool[,] occupied = Occupied(voids, placed, width, height);
                    for (int i = 0; i < count; i++)
                    {
                        if (done[i] || lockSteps[i] > 0) continue;
                        List<RectInt> ok = cand[i].FindAll(r => Consistent(i, r, cand, done, occupied, width, height));
                        if (ok.Count != 1) continue;
                        if (available++ == 0) { clue = i; rect = ok[0]; }
                    }
                    if (available > 0) technique = 2;
                }

                // T3: chỉ 1 cách đặt còn giải tới hết lưới
                if (technique == 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (done[i] || lockSteps[i] > 0) continue;
                        int solvable = 0;
                        RectInt last = default;
                        foreach (RectInt r in cand[i])
                        {
                            placed.Add(r);
                            bool ok = ShikakuSolver.TrySolve(level, placed, unknownValue, unknownConstraint, out _, out bool aborted);
                            placed.RemoveAt(placed.Count - 1);
                            if (aborted) { rep.aborted = true; ok = true; }
                            if (!ok) continue;
                            last = r;
                            if (++solvable > 1) break;
                        }
                        if (solvable != 1) continue;
                        if (available++ == 0) { clue = i; rect = last; }
                    }
                    if (available > 0) technique = 3;
                }

                // T4: không có nước bắt buộc -> đặt theo 1 lời giải để mô phỏng tiếp
                if (technique == 0)
                {
                    technique = 4;
                    available = 0;
                    if (!ShikakuSolver.TrySolve(level, placed, unknownValue, unknownConstraint, out List<RectInt> solution, out bool aborted) || solution == null)
                    {
                        rep.error = aborted ? "Quá giới hạn tìm kiếm" : "Không có lời giải";
                        break;
                    }
                    // Số chưa khóa có ít cách đặt nhất: chỗ người chơi dễ đoán nhất
                    int best = int.MaxValue;
                    for (int i = 0; i < count; i++)
                    {
                        if (done[i] || lockSteps[i] > 0) continue;
                        Vector2Int p = new Vector2Int(level.clues[i].x, level.clues[i].y);
                        int idx = solution.FindIndex(r => r.Contains(p));
                        if (idx < 0 || cand[i].Count >= best) continue;
                        best = cand[i].Count;
                        clue = i;
                        rect = solution[idx];
                    }
                    if (clue < 0)
                    {
                        rep.error = "Kẹt: mọi số còn lại đều đang khóa";
                        break;
                    }
                }

                switch (technique)
                {
                    case 1: rep.t1++; break;
                    case 2: rep.t2++; break;
                    case 3: rep.t3++; break;
                    default: rep.t4++; break;
                }
                if (available <= 1) narrowSteps++;
                rep.maxTechnique = Mathf.Max(rep.maxTechnique, technique);
                trace.Append(technique);

                placed.Add(rect);
                done[clue] = true;
                for (int i = 0; i < count; i++)
                    if (lockSteps[i] > 0) lockSteps[i]--;
            }

            rep.trace = trace.ToString();
            rep.narrow = rep.Steps > 0 ? (float)narrowSteps / rep.Steps : 0f;
            Score(rep, w);
            return rep;
        }

        /// <summary>Tính D, thời gian, rủi ro từ các chỉ số (gọi lại được khi đổi trọng số, không cần phân tích lại).</summary>
        public static void Score(Report r, LevelDifficultySettings w)
        {
            float n = Mathf.Max(1, r.Steps);
            r.score = w.wSize * Mathf.Log(Mathf.Max(1, r.clues), 2f)
                      + w.wAmbiguity * r.ambiguity
                      + w.wT2 * r.t2 / n + w.wT3 * r.t3 / n + w.wT4 * r.t4 / n
                      + w.wNarrow * r.narrow
                      + w.wHidden * r.hidden + w.wLock * r.locks
                      + w.wWall * r.walls + w.wCorner * r.corners;
            r.seconds = w.timeBase + w.timePerCell * r.cells
                        + w.timeT1 * r.t1 + w.timeT2 * r.t2 + w.timeT3 * r.t3 + w.timeT4 * r.t4;
            r.risk = w.riskT2 * r.t2 + w.riskT3 * r.t3 + w.riskT4 * r.t4;
        }

        /// <summary>Cách đặt r của số i có để lại chỗ cho mọi số khác và phủ được mọi ô trống không (nhìn trước 1 bước).</summary>
        private static bool Consistent(int i, RectInt r, List<RectInt>[] cand, bool[] done, bool[,] occupied, int width, int height)
        {
            bool[,] coverable = new bool[width, height];
            Mark(coverable, r);
            for (int j = 0; j < cand.Length; j++)
            {
                if (j == i || done[j]) continue;
                bool any = false;
                foreach (RectInt o in cand[j])
                {
                    if (o.Overlaps(r)) continue;
                    any = true;
                    Mark(coverable, o);
                }
                if (!any) return false;
            }

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (!occupied[x, y] && !coverable[x, y]) return false;
            return true;
        }

        private static bool[,] Occupied(bool[,] voids, List<RectInt> placed, int width, int height)
        {
            bool[,] occ = (bool[,])voids.Clone();
            foreach (RectInt r in placed) Mark(occ, r);
            return occ;
        }

        private static void Mark(bool[,] grid, RectInt r)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    grid[x, y] = true;
        }

        // =====================================================================
        // Thống kê

        /// <summary>Hệ số tương quan Spearman (Pearson trên hạng, hạng trùng lấy trung bình). NaN nếu dưới 3 cặp.</summary>
        public static double Spearman(IList<double> a, IList<double> b)
        {
            if (a.Count != b.Count || a.Count < 3) return double.NaN;
            return Pearson(Ranks(a), Ranks(b));
        }

        public static double Pearson(IList<double> a, IList<double> b)
        {
            int n = a.Count;
            if (n < 3) return double.NaN;
            double ma = a.Average(), mb = b.Average(), sab = 0, saa = 0, sbb = 0;
            for (int i = 0; i < n; i++)
            {
                sab += (a[i] - ma) * (b[i] - mb);
                saa += (a[i] - ma) * (a[i] - ma);
                sbb += (b[i] - mb) * (b[i] - mb);
            }
            return saa <= 0 || sbb <= 0 ? double.NaN : sab / System.Math.Sqrt(saa * sbb);
        }

        private static double[] Ranks(IList<double> v)
        {
            int[] order = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToArray();
            double[] ranks = new double[v.Count];
            for (int k = 0; k < order.Length;)
            {
                int end = k;
                while (end + 1 < order.Length && v[order[end + 1]] == v[order[k]]) end++;
                double rank = (k + end) / 2.0 + 1;
                for (int m = k; m <= end; m++) ranks[order[m]] = rank;
                k = end + 1;
            }
            return ranks;
        }
    }
}
