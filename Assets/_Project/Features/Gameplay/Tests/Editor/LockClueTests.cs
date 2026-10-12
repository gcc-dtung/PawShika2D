using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>
    /// EditMode: ổ khóa = số bước gốc - số khối khác đang trên lưới. Đặt khối thì mở dần, gỡ khối thì khóa lại.
    /// Lưới 1 hàng: [1][1][2 khóa 2 bước][ ]
    /// </summary>
    public class LockClueTests
    {
        private static readonly RectInt A = new RectInt(0, 0, 1, 1);
        private static readonly RectInt B = new RectInt(1, 0, 1, 1);
        private static readonly RectInt Locked = new RectInt(2, 0, 2, 1);

        private static BoardModel Board(out CellModel lockCell)
        {
            lockCell = new CellModel(new Vector2Int(2, 0), new ClueData { x = 2, value = 2, mechanic = new ClueMechanic { lockSteps = 2 } });
            List<CellModel> cells = new List<CellModel>
            {
                new CellModel(new Vector2Int(0, 0), new ClueData { x = 0, value = 1 }),
                new CellModel(new Vector2Int(1, 0), new ClueData { x = 1, value = 1 }),
                lockCell,
                new CellModel(new Vector2Int(3, 0))
            };
            return new BoardModel(null, new GridGeometry(4, 1, 1f), cells);
        }

        private static BlockModel Place(BoardModel board, RectInt rect, List<CellModel> unlocked = null) =>
            board.Place(board.Check(rect), default, unlocked);

        [Test]
        public void DatDuKhoi_MoKhoa()
        {
            BoardModel board = Board(out CellModel lockCell);
            List<CellModel> unlocked = new List<CellModel>();
            Place(board, A, unlocked);
            Assert.AreEqual(1, lockCell.Clue.LockSteps);
            Assert.IsEmpty(unlocked);
            Place(board, B, unlocked);
            Assert.IsFalse(lockCell.Clue.IsLocked);
            CollectionAssert.AreEqual(new[] { lockCell }, unlocked);
        }

        [Test]
        public void GoKhoi_SauKhiMo_KhoaLai()
        {
            BoardModel board = Board(out CellModel lockCell);
            Place(board, A);
            BlockModel b = Place(board, B);
            Assert.IsFalse(lockCell.Clue.IsLocked);

            board.Remove(b);
            Assert.IsTrue(lockCell.Clue.IsLocked);
            Assert.AreEqual(1, lockCell.Clue.LockSteps);
            Assert.AreEqual(PlacementResult.Locked, board.Check(Locked).Result);

            Place(board, B);
            Assert.IsTrue(board.Check(Locked).IsReady);
        }

        [Test]
        public void KhoiTrenChinhOKhoa_KhongTinh()
        {
            BoardModel board = Board(out CellModel lockCell);
            BlockModel a = Place(board, A);
            Place(board, B);
            Place(board, Locked);
            Assert.IsFalse(lockCell.Clue.IsLocked);

            board.Remove(a); // còn 1 khối khác (B) -> khóa lại 1 bước, khối trên ô khóa không tính
            Assert.AreEqual(1, lockCell.Clue.LockSteps);
        }

        [Test]
        public void CanPlaceReplacing_GoKhoiLamKhoaLai_False_LuoiGiuNguyen()
        {
            // [2 khóa 1][ ][2][ ]: khối sai (1..2) mở khóa; booster muốn đặt (0..1) phải gỡ nó -> ô khóa đóng lại
            CellModel lockCell = new CellModel(new Vector2Int(0, 0), new ClueData { value = 2, mechanic = new ClueMechanic { lockSteps = 1 } });
            BoardModel board = new BoardModel(null, new GridGeometry(4, 1, 1f), new List<CellModel>
            {
                lockCell,
                new CellModel(new Vector2Int(1, 0)),
                new CellModel(new Vector2Int(2, 0), new ClueData { x = 2, value = 2 }),
                new CellModel(new Vector2Int(3, 0))
            });
            BlockModel wrong = Place(board, new RectInt(1, 0, 2, 1));
            Assert.IsFalse(lockCell.Clue.IsLocked);

            Assert.IsFalse(board.CanPlaceReplacing(new RectInt(0, 0, 2, 1), new List<BlockModel> { wrong }));
            Assert.IsFalse(lockCell.Clue.IsLocked);
            Assert.AreEqual(1, board.Blocks.Count);
            Assert.AreSame(wrong, board.GetBlockAt(new Vector2Int(1, 0)));
        }

        [Test]
        public void ClearBlocks_KhoaLaiNhuBanDau()
        {
            BoardModel board = Board(out CellModel lockCell);
            Place(board, A);
            Place(board, B);
            board.ClearBlocks();
            Assert.AreEqual(2, lockCell.Clue.LockSteps);
        }
    }
}
