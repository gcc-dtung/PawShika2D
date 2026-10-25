using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>
    /// EditMode: tường giữa 2 ô kề nhau, khối không được vắt qua (BoardModel kéo / đặt, ShikakuSolver).
    /// Lưới 1 hàng: [2][ ][2][ ], tường giữa ô 1 và ô 2.
    /// </summary>
    public class WallTests
    {
        private static readonly WallData Wall = new WallData { x = 1, y = 0, side = WallSide.Right };

        private static LevelDefinition Level()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.size = 4;
            level.height = 1;
            level.clues = new[]
            {
                new ClueData { id = 0, x = 0, y = 0, value = 2 },
                new ClueData { id = 1, x = 2, y = 0, value = 2 }
            };
            level.walls = new[] { Wall };
            return level;
        }

        private static BoardModel Board(LevelDefinition level)
        {
            List<CellModel> cells = new List<CellModel>
            {
                new CellModel(new Vector2Int(0, 0), level.clues[0]),
                new CellModel(new Vector2Int(1, 0)),
                new CellModel(new Vector2Int(2, 0), level.clues[1]),
                new CellModel(new Vector2Int(3, 0))
            };
            return new BoardModel(level, new GridGeometry(4, 1, 1f), cells);
        }

        [Test]
        public void VungVatQuaTuong_BiChan()
        {
            LevelDefinition level = Level();
            BoardModel board = Board(level);
            RectInt across = new RectInt(1, 0, 2, 1);
            Assert.IsFalse(board.IsAreaFree(across));
            Assert.AreEqual(PlacementResult.BlockedCell, board.Check(across).Result);
            Assert.IsTrue(board.Check(new RectInt(0, 0, 2, 1)).IsReady);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void TuongChiChan1Canh_HaiChieuNhuNhau()
        {
            Assert.IsTrue(WallRules.TryFind(new[] { Wall }, new Vector2Int(2, 0), new Vector2Int(1, 0), out _));
            Assert.IsFalse(WallRules.TryFind(new[] { Wall }, new Vector2Int(0, 0), new Vector2Int(1, 0), out _));
            Assert.IsFalse(WallRules.Crosses(new[] { Wall }, new RectInt(2, 0, 2, 1)));
        }

        [Test]
        public void Solver_KhongVatQuaTuong()
        {
            LevelDefinition level = Level();
            Assert.IsTrue(ShikakuSolver.TrySolve(level, new List<RectInt>(), out List<RectInt> solution));
            foreach (RectInt r in solution) Assert.IsFalse(WallRules.Crosses(level.walls, r));
            Object.DestroyImmediate(level);
        }

        [Test]
        public void TuongNgauNhien_NamTrenRanhGioiLoiGiai()
        {
            List<RectInt> solution = new List<RectInt> { new RectInt(0, 0, 2, 2), new RectInt(2, 0, 1, 2), new RectInt(0, 2, 3, 1) };
            WallData[] walls = WallRules.RandomOnSolution(solution, 100, new System.Random(1));
            Assert.AreEqual(5, walls.Length); // 2 cạnh dọc giữa khối 1-2, 2 cạnh ngang trên khối 1, 1 cạnh ngang trên khối 2
            foreach (RectInt r in solution) Assert.IsFalse(WallRules.Crosses(walls, r));
        }
    }
}
