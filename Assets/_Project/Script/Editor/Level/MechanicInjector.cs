using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gắn cơ chế (Void, hướng, khóa, "?") vào level có sẵn, dựa trên lời giải của level.
/// - MakePlans: level số N nhận cơ chế nào (giới thiệu / level nghỉ / thường / kết hợp), random theo seed + N.
/// - Inject: nhân bản level, gắn từng cơ chế; mỗi lần gắn chạy LevelValidator, sai thì hoàn tác và thử chỗ khác.
///   Void: bỏ hẳn 1 khối của lời giải (ô thành Void, xóa số) -> không thể sinh thêm lời giải; cắt viền lưới thừa.
///   Hướng: gắn đúng hình của khối trong lời giải, chỉ chọn số mà mũi tên thực sự loại bớt cách đặt.
///   Khóa / "?": chỉ giữ khi vẫn giải được không cần đoán; level gốc phải đoán (nhiều lời giải) thì không gắn.
/// Class C# thuần, không lưu asset: MechanicInjectorWindow lo lưu và cập nhật LevelDatabase.
/// </summary>
public static class MechanicInjector
{
    public class Plan
    {
        public int number;
        public readonly List<MechanicKind> kinds = new List<MechanicKind>();
        /// <summary>Cơ chế người chơi đã được giới thiệu tới level này (dùng để thay khi gắn hỏng).</summary>
        public readonly List<MechanicKind> unlocked = new List<MechanicKind>();
        public bool intro;
        public string reason;

        public override string ToString() => kinds.Count == 0 ? "—" : string.Join(" + ", kinds);
    }

    public class Result
    {
        public Plan plan;
        /// <summary>Bản đã gắn cơ chế (chưa lưu thành asset); null nếu không gắn được gì.</summary>
        public LevelDefinition level;
        public readonly List<MechanicKind> appliedKinds = new List<MechanicKind>();
        public readonly List<string> applied = new List<string>();
        public readonly List<string> notes = new List<string>();
    }

    private class Context
    {
        public LevelDefinition level;
        public MechanicInjectorSettings settings;
        public System.Random rng;
        public bool intro;
        public int originalPlayable;
    }

    // =====================================================================
    // Lập lịch

    /// <summary>
    /// Lịch cho cả database (sources[i] = level gốc của level số i + 1, null = bỏ qua).
    /// Giới thiệu cơ chế: introLength level PHÙ HỢP đầu tiên từ introLevel (level gốc phải đoán không nhận khóa / "?",
    /// level quá ít khối không nhận Void). Cơ chế chỉ xuất hiện ở level thường sau khi đã giới thiệu ít nhất 1 level.
    /// </summary>
    public static List<Plan> MakePlans(MechanicInjectorSettings s, IList<LevelDefinition> sources)
    {
        List<Plan> plans = new List<Plan>(sources.Count);
        Dictionary<MechanicInjectorSettings.MechanicRule, int> introDone = new Dictionary<MechanicInjectorSettings.MechanicRule, int>();
        Dictionary<MechanicInjectorSettings.MechanicRule, int> introEnd = new Dictionary<MechanicInjectorSettings.MechanicRule, int>();
        List<MechanicInjectorSettings.MechanicRule> rules = s.mechanics.FindAll(m => m.introLevel > 0);
        rules.Sort((a, b) => a.introLevel.CompareTo(b.introLevel));
        foreach (MechanicInjectorSettings.MechanicRule r in rules) introDone[r] = 0;

        for (int i = 0; i < sources.Count; i++)
        {
            LevelDefinition source = sources[i];
            if (source == null)
            {
                plans.Add(null);
                continue;
            }

            int number = i + 1;
            bool needsGuessing = !LevelValidator.Validate(source, alwaysCheckGuessing: true).IsValid;

            // Đợt giới thiệu đang chờ (cơ chế giới thiệu sớm hơn được ưu tiên)
            MechanicInjectorSettings.MechanicRule intro = rules.Find(r =>
                number >= r.introLevel && introDone[r] < Mathf.Max(1, r.introLength) && Suits(s, r.kind, source, needsGuessing));
            if (intro != null)
            {
                introDone[intro]++;
                if (introDone[intro] >= intro.introLength) introEnd[intro] = number;
                Plan p = new Plan { number = number, intro = true, reason = $"Giới thiệu {intro.kind} ({introDone[intro]}/{intro.introLength})" };
                p.kinds.Add(intro.kind);
                p.unlocked.AddRange(rules.FindAll(r => introDone[r] > 0).Select(r => r.kind));
                plans.Add(p);
                continue;
            }

            List<MechanicInjectorSettings.MechanicRule> pool = rules.FindAll(r =>
                introDone[r] > 0 && (!needsGuessing || (r.kind != MechanicKind.Lock && r.kind != MechanicKind.Hidden)));
            plans.Add(MakeNormalPlan(s, number, source.difficulty, pool, introEnd));
        }
        return plans;
    }

    /// <summary>Level gốc có hợp để giới thiệu cơ chế không (để level giới thiệu chắc chắn gắn được).</summary>
    private static bool Suits(MechanicInjectorSettings s, MechanicKind kind, LevelDefinition source, bool needsGuessing)
    {
        switch (kind)
        {
            case MechanicKind.Lock:
            case MechanicKind.Hidden: return !needsGuessing;
            case MechanicKind.Void: return source.clues != null && source.clues.Length - 1 >= s.minCluesAfterVoid;
            default: return true;
        }
    }

    private static Plan MakeNormalPlan(MechanicInjectorSettings s, int number, LevelDifficulty difficulty,
        List<MechanicInjectorSettings.MechanicRule> pool, Dictionary<MechanicInjectorSettings.MechanicRule, int> introEnd)
    {
        Plan plan = new Plan { number = number };
        plan.unlocked.AddRange(pool.Select(r => r.kind));
        System.Random rng = new System.Random(Hash(s.seed, number, 1));

        if (pool.Count == 0)
        {
            plan.reason = "Chưa mở cơ chế";
            return plan;
        }
        bool hard = s.comboOnHardLevels && difficulty != LevelDifficulty.Normal;
        if (!hard && s.reliefEvery > 0 && number % s.reliefEvery == 0)
        {
            plan.reason = "Level nghỉ";
            return plan;
        }

        int count;
        if (hard)
        {
            count = difficulty == LevelDifficulty.VeryHard ? 3 : 2;
            plan.reason = $"Level {difficulty}";
        }
        else if (rng.NextDouble() >= s.mechanicChance)
        {
            plan.reason = "Không cơ chế (random)";
            return plan;
        }
        else if (number >= s.comboStartLevel && rng.NextDouble() < s.comboChance)
        {
            count = 2;
            plan.reason = "Kết hợp";
        }
        else
        {
            count = 1;
            plan.reason = "Thường";
        }

        while (plan.kinds.Count < count && pool.Count > 0)
        {
            MechanicInjectorSettings.MechanicRule pick = PickWeighted(pool, r =>
                r.weight * (!introEnd.TryGetValue(r, out int end) || number <= end + s.newestBoostLevels ? s.newestBoost : 1f), rng);
            plan.kinds.Add(pick.kind);
            pool.Remove(pick);
        }
        plan.kinds.Sort(); // Void -> hướng -> khóa -> "?": cơ chế đổi lưới gắn trước
        return plan;
    }

    // =====================================================================
    // Gắn cơ chế

    public static Result Inject(LevelDefinition source, Plan plan, MechanicInjectorSettings s)
    {
        Result result = new Result { plan = plan };
        if (plan.kinds.Count == 0) return result;

        if (!LevelValidator.Validate(source).IsValid)
        {
            result.notes.Add("Level gốc không hợp lệ, bỏ qua");
            return result;
        }
        // Level gốc đã phải đoán (thường do nhiều lời giải) thì gắn khóa / "?" chắc chắn hỏng
        bool needsGuessing = !LevelValidator.Validate(source, alwaysCheckGuessing: true).IsValid;

        LevelDefinition level = UnityEngine.Object.Instantiate(source);
        level.name = source.name;
        if (!EnsureSolution(level))
        {
            result.notes.Add("Không lấy được lời giải");
            UnityEngine.Object.DestroyImmediate(level);
            return result;
        }

        Context ctx = new Context
        {
            level = level,
            settings = s,
            rng = new System.Random(Hash(s.seed, plan.number, 2)),
            intro = plan.intro,
            originalPlayable = Playable(level)
        };

        HashSet<MechanicKind> tried = new HashSet<MechanicKind>();
        foreach (MechanicKind kind in plan.kinds)
        {
            if (TryKind(ctx, kind, needsGuessing, tried, result)) continue;
            if (plan.intro) continue; // level giới thiệu không thay cơ chế khác

            // Thay bằng cơ chế đã mở khác (ưu tiên hướng: luôn giữ nguyên lời giải)
            MechanicKind[] fallbacks = { MechanicKind.Constraint, MechanicKind.Void, MechanicKind.Hidden, MechanicKind.Lock };
            foreach (MechanicKind f in fallbacks)
            {
                if (tried.Contains(f) || plan.kinds.Contains(f) || !plan.unlocked.Contains(f)) continue;
                if (TryKind(ctx, f, needsGuessing, tried, result))
                {
                    result.notes.Add($"Thay {kind} bằng {f}");
                    break;
                }
            }
        }

        if (result.applied.Count == 0)
        {
            UnityEngine.Object.DestroyImmediate(level);
            return result;
        }
        result.level = level;
        return result;
    }

    private static bool TryKind(Context ctx, MechanicKind kind, bool needsGuessing, HashSet<MechanicKind> tried, Result result)
    {
        tried.Add(kind);
        if (needsGuessing && (kind == MechanicKind.Lock || kind == MechanicKind.Hidden))
        {
            result.notes.Add($"{kind}: level gốc phải đoán (nhiều lời giải), không gắn được");
            return false;
        }

        MechanicInjectorSettings.MechanicRule rule = ctx.settings.Get(kind);
        Vector2Int range = rule != null ? rule.amount : Vector2Int.one;
        int min = Mathf.Max(1, range.x);
        int amount = ctx.intro ? min : ctx.rng.Next(min, Mathf.Max(min, range.y) + 1);

        List<string> parts = new List<string>();
        for (int k = 0; k < amount; k++)
        {
            string part;
            switch (kind)
            {
                case MechanicKind.Void: part = TryVoid(ctx); break;
                case MechanicKind.Constraint: part = TryConstraint(ctx); break;
                case MechanicKind.Lock: part = TryLock(ctx); break;
                default: part = TryHidden(ctx); break;
            }
            if (part == null) break;
            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            result.notes.Add($"{kind}: không tìm được chỗ gắn hợp lệ");
            return false;
        }
        if (parts.Count < amount) result.notes.Add($"{kind}: chỉ gắn được {parts.Count}/{amount}");
        result.appliedKinds.Add(kind);
        result.applied.Add($"{kind}: {string.Join("; ", parts)}");
        return true;
    }

    /// <summary>Bỏ 1 khối của lời giải: các ô thành Void, xóa số. Ưu tiên khối sát mép / sát Void để lưới có hình lạ thay vì thủng lỗ.</summary>
    private static string TryVoid(Context ctx)
    {
        LevelDefinition level = ctx.level;
        MechanicInjectorSettings s = ctx.settings;
        RectInt[] rects = SolutionRects(level);
        if (rects == null || level.clues.Length - 1 < s.minCluesAfterVoid) return null;

        List<int> candidates = Enumerable.Range(0, level.clues.Length).ToList();
        if (ctx.intro)
        {
            List<int> edge = candidates.FindAll(i => TouchesEdge(level, rects[i]));
            if (edge.Count > 0) candidates = edge;
        }
        int minPlayable = Mathf.CeilToInt(ctx.originalPlayable * (1f - s.maxVoidRatio));

        for (int attempt = 0; attempt < s.maxAttempts && candidates.Count > 0; attempt++)
        {
            // Khối to cho hình lưới rõ hơn, nhất là level giới thiệu (maxVoidRatio chặn bỏ quá nhiều)
            int i = PickWeighted(candidates, c =>
            {
                float area = rects[c].width * rects[c].height;
                return (TouchesEdge(level, rects[c]) ? 3f : 1f) * (ctx.intro ? area : Mathf.Sqrt(area));
            }, ctx.rng);
            candidates.Remove(i);

            RectInt r = rects[i];
            ClueData clue = level.clues[i];
            Snapshot snap = Snapshot.Of(level);
            RemoveBlock(level, i, r);

            bool ok = Playable(level) >= minPlayable && IsConnected(level);
            if (ok)
            {
                Trim(level);
                ok = LevelValidator.Validate(level).IsValid;
            }
            if (ok) return $"bỏ khối {clue.value} ({r.x},{r.y}) {r.width}x{r.height}";
            snap.Restore(level);
        }
        return null;
    }

    /// <summary>Gắn hướng đúng hình khối trong lời giải; chỉ chọn số có cách đặt khác hình (mũi tên loại bớt được).</summary>
    private static string TryConstraint(Context ctx)
    {
        LevelDefinition level = ctx.level;
        RectInt[] rects = SolutionRects(level);
        if (rects == null) return null;

        List<int> candidates = new List<int>();
        Dictionary<int, float> weights = new Dictionary<int, float>();
        for (int i = 0; i < level.clues.Length; i++)
        {
            ClueData c = level.clues[i];
            if (c.value < 2 || c.mechanic.constraint != ClueConstraint.None) continue;

            ClueConstraint shape = ShapeOf(rects[i]);
            List<RectInt> options = ShikakuSolver.GetCandidates(level, null, i, null, null);
            int eliminated = options.Count(o => !ShikakuSolver.MatchesConstraint(shape, o.width, o.height));
            if (eliminated == 0) continue;

            candidates.Add(i);
            weights[i] = eliminated;
        }

        for (int attempt = 0; attempt < ctx.settings.maxAttempts && candidates.Count > 0; attempt++)
        {
            int i = PickWeighted(candidates, c => weights[c], ctx.rng);
            candidates.Remove(i);

            Snapshot snap = Snapshot.Of(level);
            ClueConstraint shape = ShapeOf(rects[i]);
            level.clues[i].mechanic.constraint = shape;
            if (LevelValidator.Validate(level).IsValid)
                return $"{shape} số {level.clues[i].value} ({level.clues[i].x},{level.clues[i].y})";
            snap.Restore(level);
        }
        return null;
    }

    private static string TryLock(Context ctx)
    {
        LevelDefinition level = ctx.level;
        MechanicInjectorSettings s = ctx.settings;
        int min = Mathf.Max(1, s.lockSteps.x);
        int max = Mathf.Min(Mathf.Max(min, s.lockSteps.y), level.clues.Length - 1);
        if (max < min) return null;

        List<int> candidates = new List<int>();
        for (int i = 0; i < level.clues.Length; i++)
        {
            ClueMechanic m = level.clues[i].mechanic;
            if (m.lockSteps == 0 && !m.hidden) candidates.Add(i);
        }

        for (int attempt = 0; attempt < s.maxAttempts && candidates.Count > 0; attempt++)
        {
            int i = candidates[ctx.rng.Next(candidates.Count)];
            candidates.Remove(i);
            int steps = ctx.intro ? min : ctx.rng.Next(min, max + 1);

            Snapshot snap = Snapshot.Of(level);
            level.clues[i].mechanic.lockSteps = steps;
            if (LevelValidator.Validate(level).IsValid)
                return $"khóa số {level.clues[i].value} ({level.clues[i].x},{level.clues[i].y}) {steps} bước";
            snap.Restore(level);
        }
        return null;
    }

    private static string TryHidden(Context ctx)
    {
        LevelDefinition level = ctx.level;
        List<int> candidates = new List<int>();
        for (int i = 0; i < level.clues.Length; i++)
        {
            ClueData c = level.clues[i];
            if (c.value >= 2 && c.mechanic.lockSteps == 0 && !c.mechanic.hidden) candidates.Add(i);
        }

        for (int attempt = 0; attempt < ctx.settings.maxAttempts && candidates.Count > 0; attempt++)
        {
            int i = candidates[ctx.rng.Next(candidates.Count)];
            candidates.Remove(i);

            Snapshot snap = Snapshot.Of(level);
            level.clues[i].mechanic.hidden = true;
            if (LevelValidator.Validate(level).IsValid)
                return $"\"?\" số {level.clues[i].value} ({level.clues[i].x},{level.clues[i].y})";
            snap.Restore(level);
        }
        return null;
    }

    // =====================================================================
    // Lời giải / lưới

    /// <summary>Lời giải lưu sẵn sai / thiếu thì giải lại bằng ShikakuSolver và ghi vào level.solution.</summary>
    private static bool EnsureSolution(LevelDefinition level)
    {
        if (SolutionRects(level) != null) return true;
        if (!ShikakuSolver.TrySolve(level, null, null, null, out List<RectInt> rects, out _)) return false;

        List<SolutionRectData> list = new List<SolutionRectData>();
        foreach (RectInt r in rects)
        {
            int ci = Array.FindIndex(level.clues, c => r.Contains(new Vector2Int(c.x, c.y)));
            if (ci < 0) return false;
            list.Add(new SolutionRectData { clueId = level.clues[ci].id, x = r.x, y = r.y, width = r.width, height = r.height });
        }
        level.solution = list.ToArray();
        return SolutionRects(level) != null;
    }

    /// <summary>Khối của lời giải theo chỉ số clue; null nếu solution không khớp (thiếu khối, sai số ô, chồng nhau, đè Void).</summary>
    private static RectInt[] SolutionRects(LevelDefinition level)
    {
        if (level.solution == null || level.clues == null) return null;
        int width = level.size, height = level.Height;
        bool[,] used = VoidMask(level);
        RectInt[] rects = new RectInt[level.clues.Length];
        bool[] found = new bool[rects.Length];

        foreach (SolutionRectData sr in level.solution)
        {
            RectInt r = new RectInt(sr.x, sr.y, sr.width, sr.height);
            int ci = Array.FindIndex(level.clues, c => c.id == sr.clueId);
            if (ci < 0 || found[ci]) return null;
            ClueData clue = level.clues[ci];
            if (!r.Contains(new Vector2Int(clue.x, clue.y)) || r.width * r.height != clue.value) return null;
            if (r.xMin < 0 || r.yMin < 0 || r.xMax > width || r.yMax > height) return null;

            for (int x = r.xMin; x < r.xMax; x++)
            {
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    if (used[x, y]) return null;
                    used[x, y] = true;
                }
            }
            rects[ci] = r;
            found[ci] = true;
        }
        return found.All(f => f) ? rects : null;
    }

    private static void RemoveBlock(LevelDefinition level, int clueIndex, RectInt r)
    {
        int id = level.clues[clueIndex].id;
        List<CellOverrideData> overrides = new List<CellOverrideData>(level.cellOverrides ?? new CellOverrideData[0]);
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                overrides.Add(new CellOverrideData { x = x, y = y, type = CellType.Void });
        level.cellOverrides = overrides.ToArray();

        List<ClueData> clues = new List<ClueData>(level.clues);
        clues.RemoveAt(clueIndex);
        level.clues = clues.ToArray();
        level.solution = level.solution.Where(sr => sr.clueId != id).ToArray();
    }

    /// <summary>Cắt hàng / cột ngoài cùng chỉ toàn Void, dời mọi tọa độ về góc (0,0).</summary>
    private static void Trim(LevelDefinition level)
    {
        int width = level.size, height = level.Height;
        bool[,] voids = VoidMask(level);
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (voids[x, y]) continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }
        if (maxX < 0 || (minX == 0 && minY == 0 && maxX == width - 1 && maxY == height - 1)) return;

        int newWidth = maxX - minX + 1, newHeight = maxY - minY + 1;
        for (int i = 0; i < level.clues.Length; i++)
        {
            level.clues[i].x -= minX;
            level.clues[i].y -= minY;
        }
        for (int i = 0; i < level.solution.Length; i++)
        {
            level.solution[i].x -= minX;
            level.solution[i].y -= minY;
        }
        level.cellOverrides = level.cellOverrides
            .Select(o => new CellOverrideData { x = o.x - minX, y = o.y - minY, type = o.type })
            .Where(o => o.x >= 0 && o.y >= 0 && o.x < newWidth && o.y < newHeight)
            .ToArray();
        level.size = newWidth;
        level.height = newHeight;
    }

    private static bool[,] VoidMask(LevelDefinition level)
    {
        int width = level.size, height = level.Height;
        bool[,] mask = new bool[width, height];
        if (level.cellOverrides == null) return mask;
        foreach (CellOverrideData o in level.cellOverrides)
        {
            if (o.type == CellType.Void && o.x >= 0 && o.y >= 0 && o.x < width && o.y < height) mask[o.x, o.y] = true;
        }
        return mask;
    }

    private static int Playable(LevelDefinition level)
    {
        bool[,] voids = VoidMask(level);
        int count = 0;
        foreach (bool v in voids)
            if (!v) count++;
        return count;
    }

    /// <summary>Các ô chơi được liền một mảng (4 hướng).</summary>
    private static bool IsConnected(LevelDefinition level)
    {
        bool[,] blocked = VoidMask(level);
        int width = level.size, height = level.Height;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        int total = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (blocked[x, y]) continue;
                total++;
                if (queue.Count == 0 && total == 1)
                {
                    queue.Enqueue(new Vector2Int(x, y));
                    blocked[x, y] = true;
                }
            }
        }

        int reached = 0;
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            reached++;
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = p + d;
                if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= height || blocked[n.x, n.y]) continue;
                blocked[n.x, n.y] = true;
                queue.Enqueue(n);
            }
        }
        return reached == total;
    }

    /// <summary>Khối nằm sát mép lưới hoặc sát ô Void.</summary>
    private static bool TouchesEdge(LevelDefinition level, RectInt r)
    {
        if (r.xMin == 0 || r.yMin == 0 || r.xMax == level.size || r.yMax == level.Height) return true;
        bool[,] voids = VoidMask(level);
        for (int x = r.xMin; x < r.xMax; x++)
            if (voids[x, r.yMin - 1] || voids[x, r.yMax]) return true;
        for (int y = r.yMin; y < r.yMax; y++)
            if (voids[r.xMin - 1, y] || voids[r.xMax, y]) return true;
        return false;
    }

    private static ClueConstraint ShapeOf(RectInt r)
    {
        if (r.width > r.height) return ClueConstraint.Horizontal;
        if (r.height > r.width) return ClueConstraint.Vertical;
        return ClueConstraint.Square;
    }

    // =====================================================================
    // Tiện ích

    private struct Snapshot
    {
        private int _size, _height;
        private CellOverrideData[] _overrides;
        private ClueData[] _clues;
        private SolutionRectData[] _solution;

        public static Snapshot Of(LevelDefinition level) => new Snapshot
        {
            _size = level.size,
            _height = level.height,
            _overrides = Copy(level.cellOverrides),
            _clues = Copy(level.clues),
            _solution = Copy(level.solution)
        };

        public void Restore(LevelDefinition level)
        {
            level.size = _size;
            level.height = _height;
            level.cellOverrides = Copy(_overrides);
            level.clues = Copy(_clues);
            level.solution = Copy(_solution);
        }

        private static T[] Copy<T>(T[] array) => array == null ? new T[0] : (T[])array.Clone();
    }

    private static T PickWeighted<T>(List<T> items, Func<T, float> weight, System.Random rng)
    {
        float total = 0f;
        foreach (T item in items) total += Mathf.Max(0f, weight(item));
        if (total <= 0f) return items[rng.Next(items.Count)];

        double roll = rng.NextDouble() * total;
        foreach (T item in items)
        {
            roll -= Mathf.Max(0f, weight(item));
            if (roll < 0) return item;
        }
        return items[items.Count - 1];
    }

    /// <summary>Seed ổn định (FNV-1a) để chạy lại tool ra đúng kết quả cũ.</summary>
    private static int Hash(int a, int b, int c)
    {
        unchecked
        {
            uint h = 2166136261;
            h = (h ^ (uint)a) * 16777619;
            h = (h ^ (uint)b) * 16777619;
            h = (h ^ (uint)c) * 16777619;
            return (int)(h & 0x7fffffff);
        }
    }
}
