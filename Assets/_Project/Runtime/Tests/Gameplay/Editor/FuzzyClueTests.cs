using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>
    /// EditMode: số mơ hồ (>n, <n, E / O, a/b). Luật AmbiguityRules.Accepts dùng chung BoardModel và ShikakuSolver.
    /// </summary>
    public class FuzzyClueTests
    {
        private static ClueMechanic Fuzzy(ClueAmbiguity kind, int param = 0) =>
            new ClueMechanic { ambiguity = kind, ambiguityValue = param };

        /// <summary>Lưới 4 x 1, ô số ở (0, 0).</summary>
        private static BoardModel Board(int value, ClueMechanic m)
        {
            ClueData clue = new ClueData { id = 0, x = 0, y = 0, value = value, mechanic = m };
            List<CellModel> cells = new List<CellModel>
            {
                new CellModel(new Vector2Int(0, 0), clue),
                new CellModel(new Vector2Int(1, 0)),
                new CellModel(new Vector2Int(2, 0)),
                new CellModel(new Vector2Int(3, 0))
            };
            return new BoardModel(null, new GridGeometry(4, 1, 1f), cells);
        }

        private static RectInt Row(int width) => new RectInt(0, 0, width, 1);

        private static string Visible(string label) =>
            System.Text.RegularExpressions.Regex.Replace(label, "<size=[0-9]+%>|</size>|<noparse>|</noparse>", "");

        private static int SizePercent(string label)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(label, "<size=([0-9]+)%>");
            return m.Success ? int.Parse(m.Groups[1].Value) : 100;
        }

        [Test]
        public void Accepts_TungLoai()
        {
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.AtLeast, 3), 4, 3));
            Assert.IsFalse(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.AtLeast, 3), 4, 2));
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.AtMost, 3), 2, 3));
            Assert.IsFalse(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.AtMost, 3), 2, 4));
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Even), 4, 6));
            Assert.IsFalse(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Even), 4, 5));
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Odd), 3, 5));
            Assert.IsFalse(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Odd), 3, 4));
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Either, 5), 3, 5));
            Assert.IsTrue(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Either, 5), 3, 3));
            Assert.IsFalse(AmbiguityRules.Accepts(Fuzzy(ClueAmbiguity.Either, 5), 3, 4));
            Assert.IsTrue(AmbiguityRules.Accepts(default, 3, 3));
            Assert.IsFalse(AmbiguityRules.Accepts(default, 3, 4));
        }

        [Test]
        public void ItNhat_DuNguongLaDatDuoc_ThieuThiChuaSai()
        {
            BoardModel board = Board(3, Fuzzy(ClueAmbiguity.AtLeast, 2)); // hiện ">1"
            PlacementCheck one = board.Check(Row(1));
            Assert.AreEqual(PlacementResult.WrongSize, one.Result);
            Assert.AreEqual(SelectionHint.Neutral, one.Hint); // kéo thêm còn đúng được
            Assert.IsTrue(board.Check(Row(2)).IsReady);
            Assert.IsTrue(board.Check(Row(4)).IsReady);
        }

        [Test]
        public void NhieuNhat_QuaNguongLaSaiChacChan()
        {
            BoardModel board = Board(2, Fuzzy(ClueAmbiguity.AtMost, 2)); // hiện "<3"
            Assert.IsTrue(board.Check(Row(1)).IsReady);
            Assert.IsTrue(board.Check(Row(2)).IsReady);
            PlacementCheck three = board.Check(Row(3));
            Assert.AreEqual(PlacementResult.WrongSize, three.Result);
            Assert.AreEqual(SelectionHint.Invalid, three.Hint);
        }

        [Test]
        public void HaiGiaTri_ChiHaiSoDoDatDuoc()
        {
            BoardModel board = Board(2, Fuzzy(ClueAmbiguity.Either, 4)); // hiện "2/4"
            Assert.AreEqual(PlacementResult.WrongSize, board.Check(Row(1)).Result);
            Assert.IsTrue(board.Check(Row(2)).IsReady);
            PlacementCheck three = board.Check(Row(3));
            Assert.AreEqual(PlacementResult.WrongSize, three.Result);
            Assert.AreEqual(SelectionHint.Neutral, three.Hint); // 4 vẫn còn đúng
            Assert.IsTrue(board.Check(Row(4)).IsReady);
        }

        [Test]
        public void ThangKhiLuoiKin_KhoiThoaDieuKien_DuKhacSoThat()
        {
            // Số thật 2 nhưng ">1": khối 4 ô vẫn thỏa -> lưới kín là thắng (không chấm theo số thật)
            BoardModel board = Board(2, Fuzzy(ClueAmbiguity.AtLeast, 2));
            PlacementCheck check = board.Check(Row(4));
            Assert.IsTrue(check.IsReady);
            board.Place(check, ColorType.Blue, null);
            List<BlockModel> wrong = new List<BlockModel>();
            Assert.IsTrue(board.IsSolved(wrong));
            Assert.AreEqual(0, wrong.Count);
        }

        [Test]
        public void Solver_PhaiPhuKin_KhongBoTrongO()
        {
            // [1][ ][ ][≥2]: số 1 chỉ đặt (0); ">1" có thể 2 hoặc 3 ô nhưng 2 ô bỏ trống ô (1) -> chỉ còn 3 ô
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.size = 4;
            level.height = 1;
            level.clues = new[]
            {
                new ClueData { id = 0, x = 0, y = 0, value = 1 },
                new ClueData { id = 1, x = 3, y = 0, value = 3, mechanic = Fuzzy(ClueAmbiguity.AtLeast, 2) }
            };
            Assert.IsTrue(ShikakuSolver.TrySolve(level, new List<RectInt>(), out List<RectInt> solution));
            Assert.Contains(new RectInt(1, 0, 3, 1), solution);
            Assert.AreEqual(1, ShikakuSolver.CountSolutions(level, 5, out bool aborted));
            Assert.IsFalse(aborted);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void Solver_DemDuNhieuLoiGiai()
        {
            // [>0][ ][ ][>0] trên 4 ô: 1+3, 2+2, 3+1 (1+1 bỏ trống giữa -> không tính)
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.size = 4;
            level.height = 1;
            level.clues = new[]
            {
                new ClueData { id = 0, x = 0, y = 0, value = 2, mechanic = Fuzzy(ClueAmbiguity.AtLeast, 1) },
                new ClueData { id = 1, x = 3, y = 0, value = 2, mechanic = Fuzzy(ClueAmbiguity.AtLeast, 1) }
            };
            Assert.AreEqual(3, ShikakuSolver.CountSolutions(level, 10, out _));
            Object.DestroyImmediate(level);
        }

        [Test]
        public void TryMake_LuonChuaSoThat()
        {
            System.Random rng = new System.Random(7);
            for (int value = 1; value <= 12; value++)
            {
                foreach (ClueAmbiguity kind in AmbiguityRules.Kinds)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        if (!AmbiguityRules.TryMake(default, value, kind, rng, out ClueMechanic m)) continue;
                        Assert.AreEqual(kind, m.ambiguity);
                        Assert.IsTrue(AmbiguityRules.Accepts(m, value, value), $"{kind} {m.ambiguityValue} không chứa {value}");
                    }
                }
            }
            Assert.IsFalse(AmbiguityRules.TryMake(default, 5, ClueAmbiguity.Even, rng, out _));
            Assert.IsFalse(AmbiguityRules.TryMake(default, 1, ClueAmbiguity.AtLeast, rng, out _));
        }

        [Test]
        public void Nhan_TrenO()
        {
            Assert.AreEqual("7", AmbiguityRules.Label(default, 7));
            // "<" / ">" bọc noparse để TMP không coi là thẻ: bỏ thẻ rồi so chữ người chơi thấy
            Assert.AreEqual(">5", Visible(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.AtLeast, 6), 8)));
            Assert.AreEqual("<5", Visible(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.AtMost, 4), 3)));
            Assert.AreEqual("E", AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Even), 4));
            Assert.AreEqual("O", AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Odd), 3));
            Assert.AreEqual("3/5", Visible(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Either, 3), 5)));
            Assert.AreEqual("9/11", Visible(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Either, 11), 9)));
            Assert.AreEqual(">12", Visible(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.AtLeast, 13), 15)));

            // Nhãn dài thu nhỏ theo bề rộng: dài hơn thì nhỏ hơn, không nhãn nào rộng quá số 2 chữ số
            int s1 = SizePercent(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.AtLeast, 6), 8));    // ">5"
            int s2 = SizePercent(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.AtLeast, 13), 15));  // ">12"
            int s3 = SizePercent(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Either, 11), 9));    // "9/11"
            int s4 = SizePercent(AmbiguityRules.Label(Fuzzy(ClueAmbiguity.Either, 14), 12));   // "12/14"
            Assert.IsTrue(s1 > s2 && s2 > s3 && s3 > s4, $"{s1} {s2} {s3} {s4}");
            Assert.LessOrEqual(s3, 60);
            Assert.AreEqual("?", new ClueCell(new ClueData { value = 4, mechanic = new ClueMechanic { hidden = true } }).Label);
        }
    }
}
