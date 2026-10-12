using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Thông số sinh level (LevelGenerator). Dùng chung cho công cụ Editor và Daily Challenge.</summary>
[Serializable]
public class LevelGeneratorSettings
{
    [Min(2)] public int width = 7;
    [Min(2)] public int height = 7;
    [Tooltip("Số ô nhỏ nhất của 1 khối (không tính khối 1 ô bất đắc dĩ)")]
    [Min(1)] public int minArea = 2;
    [Tooltip("Số ô lớn nhất của 1 khối")]
    [Min(1)] public int maxArea = 9;
    [Tooltip("Cạnh dài nhất của 1 khối (tránh thanh 1 x 8 quá dễ đoán)")]
    [Min(1)] public int maxSide = 5;
    [Tooltip("Số khối 1 ô tối đa (ô kẹt không vừa khối nào); vượt thì chia lại")]
    [Min(0)] public int maxSingles = 2;
    [Tooltip("Chỉ nhận level có đúng 1 lời giải (người chơi không phải đoán)")]
    public bool requireUnique = true;
    [Tooltip("Số lần đổi chỗ ô số trên cùng 1 cách chia trước khi chia lại")]
    [Min(1)] public int clueRetries = 12;
    [Tooltip("Số lần chia lưới tối đa")]
    [Min(1)] public int maxPartitions = 60;

    public LevelGeneratorSettings Clone() => (LevelGeneratorSettings)MemberwiseClone();

    /// <summary>Bộ thông số có sẵn theo độ khó (Daily = lưới 10 x 10 như GenerateDailyChallenge gốc).</summary>
    public static LevelGeneratorSettings Preset(LevelGeneratorPreset preset)
    {
        switch (preset)
        {
            case LevelGeneratorPreset.Easy: return new LevelGeneratorSettings { width = 6, height = 6, maxArea = 6, maxSide = 4 };
            case LevelGeneratorPreset.Hard: return new LevelGeneratorSettings { width = 9, height = 9, maxArea = 12, maxSide = 6 };
            case LevelGeneratorPreset.Daily: return new LevelGeneratorSettings { width = 10, height = 10, maxArea = 10, maxSide = 5, maxSingles = 3 };
            default: return new LevelGeneratorSettings();
        }
    }
}

public enum LevelGeneratorPreset
{
    Easy,
    Normal,
    Hard,
    Daily
}

/// <summary>
/// Sinh level Shikaku theo seed (cùng seed + thông số = cùng level, mọi máy):
/// 1. Chia lưới thành các khối chữ nhật (Partition): quét từng ô trống theo hàng, chọn ngẫu nhiên 1 khối vừa chỗ
///    có số ô trong [minArea, maxArea], cạnh không quá maxSide; ô kẹt không vừa khối nào thành khối 1 ô (tối đa maxSingles).
/// 2. Mỗi khối đặt ô số ở 1 ô ngẫu nhiên trong khối.
/// 3. requireUnique: đếm lời giải bằng ShikakuSolver.CountSolutions (dừng ở 2); chưa duy nhất thì đổi chỗ ô số, rồi chia lại.
/// Kết quả là LevelDefinition tạo trong bộ nhớ (ScriptableObject.CreateInstance) kèm lời giải; Editor lưu thành asset nếu cần.
/// Chưa gắn cơ chế (Void, hướng, khóa, "?"): level lưu ra chạy được qua Mechanic Injector.
/// </summary>
public static class LevelGenerator
{
    public class Result
    {
        public LevelDefinition level;  // null nếu không sinh được
        public int partitions;         // số lần chia lưới đã thử
        public int clueTries;          // tổng số lần đặt ô số đã thử
        public bool unique;            // đã kiểm tra đúng 1 lời giải
    }

    public static Result Generate(LevelGeneratorSettings s, int seed)
    {
        System.Random rng = new System.Random(seed);
        Result result = new Result();
        for (int p = 0; p < s.maxPartitions; p++)
        {
            result.partitions++;
            List<RectInt> blocks = Partition(s, rng);
            if (blocks == null) continue;

            int tries = s.requireUnique ? s.clueRetries : 1;
            for (int t = 0; t < tries; t++)
            {
                result.clueTries++;
                LevelDefinition level = Build(s, blocks, rng, seed);
                if (!s.requireUnique)
                {
                    result.level = level;
                    return result;
                }
                int count = ShikakuSolver.CountSolutions(level, 2, out bool aborted);
                if (count == 1 && !aborted)
                {
                    result.level = level;
                    result.unique = true;
                    return result;
                }
                UnityEngine.Object.DestroyImmediate(level);
            }
        }
        return result;
    }

    /// <summary>
    /// Chia lưới width x height thành các khối phủ kín, không chồng nhau. null nếu phải dùng quá maxSingles khối 1 ô.
    /// Quét hàng từ dưới lên, trái sang phải: mọi ô trước ô đang xét đã có khối, nên khối mới lấy ô đó làm góc dưới trái.
    /// </summary>
    public static List<RectInt> Partition(LevelGeneratorSettings s, System.Random rng)
    {
        int w = s.width, h = s.height;
        bool[,] taken = new bool[w, h];
        List<RectInt> blocks = new List<RectInt>();
        List<RectInt> options = new List<RectInt>();
        int singles = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (taken[x, y]) continue;

                options.Clear();
                for (int bh = 1; bh <= Mathf.Min(s.maxSide, h - y); bh++)
                {
                    for (int bw = 1; bw <= Mathf.Min(s.maxSide, w - x); bw++)
                    {
                        int area = bw * bh;
                        if (area < Mathf.Max(2, s.minArea) || area > s.maxArea) continue;
                        RectInt r = new RectInt(x, y, bw, bh);
                        if (IsFree(r, taken)) options.Add(r);
                    }
                }

                RectInt pick;
                if (options.Count > 0) pick = options[rng.Next(options.Count)];
                else
                {
                    if (++singles > s.maxSingles) return null;
                    pick = new RectInt(x, y, 1, 1);
                }
                Mark(pick, taken);
                blocks.Add(pick);
            }
        }
        return blocks;
    }

    /// <summary>LevelDefinition từ các khối: mỗi khối 1 ô số (vị trí ngẫu nhiên trong khối), lời giải = chính các khối.</summary>
    public static LevelDefinition Build(LevelGeneratorSettings s, IList<RectInt> blocks, System.Random rng, int seed)
    {
        LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
        level.id = $"Gen_{s.width}x{s.height}_{seed}";
        level.name = level.id;
        level.source = new LevelSource { game = "Generator", sourceIndex = seed };
        level.size = s.width;
        level.height = s.height;
        level.cellOverrides = new CellOverrideData[0];
        level.clues = new ClueData[blocks.Count];
        level.solution = new SolutionRectData[blocks.Count];
        for (int i = 0; i < blocks.Count; i++)
        {
            RectInt r = blocks[i];
            level.clues[i] = new ClueData
            {
                id = i,
                x = r.x + rng.Next(r.width),
                y = r.y + rng.Next(r.height),
                value = r.width * r.height,
                color = rng.Next(Enum.GetValues(typeof(ColorType)).Length)
            };
            level.solution[i] = new SolutionRectData { clueId = i, x = r.x, y = r.y, width = r.width, height = r.height };
        }
        return level;
    }

    /// <summary>Seed ổn định cho 1 ngày (Daily Challenge): mọi máy cùng ngày ra cùng level.</summary>
    public static int SeedForDate(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

    private static bool IsFree(RectInt r, bool[,] taken)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                if (taken[x, y]) return false;
        return true;
    }

    private static void Mark(RectInt r, bool[,] taken)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                taken[x, y] = true;
    }
}
