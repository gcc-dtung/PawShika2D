using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode: sinh level (LevelGenerator) và đếm lời giải (ShikakuSolver.CountSolutions).</summary>
public class LevelGeneratorTests
{
    private readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in _created) if (o != null) Object.DestroyImmediate(o);
        _created.Clear();
    }

    private LevelDefinition Track(LevelDefinition level)
    {
        if (level != null) _created.Add(level);
        return level;
    }

    [TestCase(LevelGeneratorPreset.Easy)]
    [TestCase(LevelGeneratorPreset.Normal)]
    [TestCase(LevelGeneratorPreset.Hard)]
    [TestCase(LevelGeneratorPreset.Daily)]
    public void ChiaLuoiPhuKinKhongChongVaDungGioiHan(LevelGeneratorPreset preset)
    {
        LevelGeneratorSettings s = LevelGeneratorSettings.Preset(preset);
        System.Random rng = new System.Random(7);
        List<RectInt> blocks = null;
        for (int i = 0; i < 20 && blocks == null; i++) blocks = LevelGenerator.Partition(s, rng);
        Assert.IsNotNull(blocks);

        int[,] cover = new int[s.width, s.height];
        int singles = 0;
        foreach (RectInt r in blocks)
        {
            Assert.LessOrEqual(r.width * r.height, s.maxArea);
            Assert.LessOrEqual(Mathf.Max(r.width, r.height), s.maxSide);
            if (r.width * r.height == 1) singles++;
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    cover[x, y]++;
        }
        foreach (int c in cover) Assert.AreEqual(1, c);
        Assert.LessOrEqual(singles, s.maxSingles);
    }

    [Test]
    public void CungSeedRaCungLevel()
    {
        LevelGeneratorSettings s = LevelGeneratorSettings.Preset(LevelGeneratorPreset.Daily);
        LevelDefinition a = Track(LevelGenerator.Generate(s, 20261011).level);
        LevelDefinition b = Track(LevelGenerator.Generate(s, 20261011).level);
        Assert.IsNotNull(a);
        Assert.AreEqual(a.clues.Length, b.clues.Length);
        for (int i = 0; i < a.clues.Length; i++)
        {
            Assert.AreEqual(a.clues[i].x, b.clues[i].x);
            Assert.AreEqual(a.clues[i].y, b.clues[i].y);
            Assert.AreEqual(a.clues[i].value, b.clues[i].value);
        }
    }

    [Test]
    public void LevelSinhRaDungMotLoiGiaiVaKhopLoiGiaiLuuSan()
    {
        LevelGeneratorSettings s = LevelGeneratorSettings.Preset(LevelGeneratorPreset.Normal);
        for (int seed = 1; seed <= 10; seed++)
        {
            LevelGenerator.Result r = LevelGenerator.Generate(s, seed);
            LevelDefinition level = Track(r.level);
            Assert.IsNotNull(level, $"seed {seed}");
            Assert.IsTrue(r.unique);
            Assert.AreEqual(1, ShikakuSolver.CountSolutions(level, 2, out bool aborted));
            Assert.IsFalse(aborted);

            // Tổng số = số ô, mỗi khối lời giải chứa đúng ô số của nó và đúng số ô
            int sum = 0;
            foreach (ClueData c in level.clues) sum += c.value;
            Assert.AreEqual(s.width * s.height, sum);
            foreach (SolutionRectData b in level.solution)
            {
                ClueData c = System.Array.Find(level.clues, x => x.id == b.clueId);
                Assert.AreEqual(c.value, b.width * b.height);
                Assert.IsTrue(new RectInt(b.x, b.y, b.width, b.height).Contains(new Vector2Int(c.x, c.y)));
            }
        }
    }

    [Test]
    public void DemLoiGiaiNhanRaLevelHaiLoiGiai()
    {
        // Lưới 2 x 2, số 2 ở 2 góc chéo: chia ngang hay dọc đều đúng -> 2 lời giải
        LevelDefinition level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
        level.size = 2;
        level.height = 2;
        level.clues = new[]
        {
            new ClueData { id = 0, x = 0, y = 0, value = 2 },
            new ClueData { id = 1, x = 1, y = 1, value = 2 }
        };
        Assert.AreEqual(2, ShikakuSolver.CountSolutions(level, 5, out _));
        Assert.AreEqual(1, ShikakuSolver.CountSolutions(level, 1, out _)); // dừng ngay khi đủ giới hạn
    }

    [Test]
    public void SeedTheoNgay()
    {
        Assert.AreEqual(20261011, LevelGenerator.SeedForDate(new System.DateTime(2026, 10, 11)));
        Assert.AreNotEqual(LevelGenerator.SeedForDate(new System.DateTime(2026, 10, 11)),
            LevelGenerator.SeedForDate(new System.DateTime(2026, 10, 12)));
    }
}
