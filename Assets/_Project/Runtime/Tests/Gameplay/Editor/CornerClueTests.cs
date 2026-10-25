using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>
    /// EditMode: ô số ở góc. Lưới 4 x 1: [ ][2 góc trái][ ][ ] -> chỉ khối (1..2) đúng, khối (0..1) sai góc.
    /// </summary>
    public class CornerClueTests
    {
        private static ClueData Clue(ClueCorner corner) =>
            new ClueData { id = 0, x = 1, y = 0, value = 2, mechanic = new ClueMechanic { corner = corner } };

        private static BoardModel Board(ClueCorner corner)
        {
            List<CellModel> cells = new List<CellModel>
            {
                new CellModel(new Vector2Int(0, 0)),
                new CellModel(new Vector2Int(1, 0), Clue(corner)),
                new CellModel(new Vector2Int(2, 0)),
                new CellModel(new Vector2Int(3, 0))
            };
            return new BoardModel(null, new GridGeometry(4, 1, 1f), cells);
        }

        [Test]
        public void SaiGoc_BiChan_VaBaoSaiNgay()
        {
            BoardModel board = Board(ClueCorner.TopLeft);
            PlacementCheck wrong = board.Check(new RectInt(0, 0, 2, 1));
            Assert.AreEqual(PlacementResult.WrongCorner, wrong.Result);
            Assert.AreEqual(SelectionHint.Invalid, wrong.Hint);
            Assert.IsTrue(board.Check(new RectInt(1, 0, 2, 1)).IsReady);
        }

        [Test]
        public void KhongDanhDau_GocNaoCungDuoc()
        {
            BoardModel board = Board(ClueCorner.None);
            Assert.IsTrue(board.Check(new RectInt(0, 0, 2, 1)).IsReady);
            Assert.IsTrue(board.Check(new RectInt(1, 0, 2, 1)).IsReady);
        }

        [Test]
        public void MatchesCorner_DungTheoHuongManHinh()
        {
            RectInt r = new RectInt(2, 3, 3, 2); // x 2..4, y 3..4
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.TopLeft, r, new Vector2Int(2, 4)));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.TopRight, r, new Vector2Int(4, 4)));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.BottomLeft, r, new Vector2Int(2, 3)));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.BottomRight, r, new Vector2Int(4, 3)));
            Assert.IsFalse(ShikakuSolver.MatchesCorner(ClueCorner.TopLeft, r, new Vector2Int(3, 4)));
        }

        [Test]
        public void Solver_TheoGoc()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.size = 3;
            level.height = 1;
            // [1][2][ ]: không gắn góc -> khối của 2 là (1..2). Gắn góc phải -> phải là (0..1), đè ô số 1 -> vô nghiệm.
            // Gắn góc trái -> (1..2)
            level.clues = new[]
            {
                new ClueData { id = 0, x = 0, y = 0, value = 1 },
                new ClueData { id = 1, x = 1, y = 0, value = 2 }
            };
            Assert.IsTrue(ShikakuSolver.TrySolve(level, new List<RectInt>(), out _));
            level.clues[1].mechanic.corner = ClueCorner.TopRight;
            Assert.IsFalse(ShikakuSolver.TrySolve(level, new List<RectInt>(), out _));
            level.clues[1].mechanic.corner = ClueCorner.TopLeft;
            Assert.IsTrue(ShikakuSolver.TrySolve(level, new List<RectInt>(), out List<RectInt> solution));
            Assert.Contains(new RectInt(1, 0, 2, 1), solution);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void Canh_ChiCanNamTrenHangCotNgoaiCung()
        {
            RectInt r = new RectInt(2, 3, 3, 2); // x 2..4, y 3..4
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.Top, r, new Vector2Int(3, 4)));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.Top, r, new Vector2Int(2, 4)));
            Assert.IsFalse(ShikakuSolver.MatchesCorner(ClueCorner.Top, r, new Vector2Int(3, 3)));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(ClueCorner.Left, r, new Vector2Int(2, 3)));
            Assert.IsFalse(ShikakuSolver.MatchesCorner(ClueCorner.Right, r, new Vector2Int(3, 3)));

            // Ô (1, 2) nằm giữa cạnh trên của khối 3 x 1 -> gắn cạnh được (góc thì không)
            ClueData[] clues = { new ClueData { x = 1, y = 2, value = 3 } };
            List<RectInt> solution = new List<RectInt> { new RectInt(0, 2, 3, 1) };
            Assert.AreEqual(1, CornerRules.AssignRandom(clues, solution, CornerRules.Edges, 1, new System.Random(1)));
            Assert.IsTrue(CornerRules.IsEdge(clues[0].mechanic.corner));
            Assert.IsTrue(ShikakuSolver.MatchesCorner(clues[0].mechanic.corner, solution[0], new Vector2Int(1, 2)));
        }

        [Test]
        public void GanGocNgauNhien_LoiGiaiVanDung()
        {
            ClueData[] clues =
            {
                new ClueData { x = 0, y = 0, value = 4 },
                new ClueData { x = 2, y = 1, value = 2 },
                new ClueData { x = 1, y = 2, value = 3 }
            };
            List<RectInt> solution = new List<RectInt> { new RectInt(0, 0, 2, 2), new RectInt(2, 0, 1, 2), new RectInt(0, 2, 3, 1) };
            int n = CornerRules.AssignRandom(clues, solution, CornerRules.Corners, 10, new System.Random(3));
            Assert.AreEqual(2, n); // ô (1, 2) nằm giữa khối 3 x 1 -> không phải góc
            Assert.AreEqual(ClueCorner.None, clues[2].mechanic.corner);
            for (int i = 0; i < 2; i++)
                Assert.IsTrue(ShikakuSolver.MatchesCorner(clues[i].mechanic.corner, solution[i], new Vector2Int(clues[i].x, clues[i].y)));
        }
    }
}
