using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode: Hint chọn vùng nào để gợi ý (HintBooster.PickRegion).</summary>
public class HintBoosterTests
{
    /// <summary>Lưới 6 × 1: ô số (0,0) = 2, (2,0) = 3 (khóa tùy chọn), (5,0) = 1.</summary>
    private static BoardModel Board(bool lockThree = false)
    {
        List<CellModel> cells = new List<CellModel>();
        for (int x = 0; x < 6; x++)
        {
            Vector2Int p = new Vector2Int(x, 0);
            if (x == 0) cells.Add(new CellModel(p, new ClueData { x = 0, value = 2 }));
            else if (x == 2) cells.Add(new CellModel(p, new ClueData { x = 2, value = 3, mechanic = new ClueMechanic { lockSteps = lockThree ? 2 : 0 } }));
            else if (x == 5) cells.Add(new CellModel(p, new ClueData { x = 5, value = 1 }));
            else cells.Add(new CellModel(p));
        }
        return new BoardModel(null, new GridGeometry(6, 1, 1f), cells);
    }

    private static readonly RectInt Two = new RectInt(0, 0, 2, 1);
    private static readonly RectInt Three = new RectInt(2, 0, 3, 1);
    private static readonly RectInt One = new RectInt(5, 0, 1, 1);

    [Test]
    public void ChonVungNhoNhat()
    {
        Assert.IsTrue(HintBooster.PickRegion(Board(), new List<RectInt> { Three, Two, One }, out RectInt pick));
        Assert.AreEqual(One, pick);
    }

    [Test]
    public void BoQuaVungCoOSoConKhoa()
    {
        Assert.IsTrue(HintBooster.PickRegion(Board(lockThree: true), new List<RectInt> { Three, Two }, out RectInt pick));
        Assert.AreEqual(Two, pick);
    }

    [Test]
    public void UuTienVungChuaBiKhoiDe_DuLonHon()
    {
        BoardModel board = Board();
        // Vùng "1" đã có khối đè (như vùng lời giải bị khối sai chiếm), vùng "3" còn trống: chọn vùng trống dù lớn hơn
        board.Place(board.Check(One), default, null);
        Assert.IsTrue(HintBooster.PickRegion(board, new List<RectInt> { One, Three }, out RectInt pick));
        Assert.AreEqual(Three, pick);
    }

    [Test]
    public void KhongCoVungHopLe_TraVeFalse()
    {
        Assert.IsFalse(HintBooster.PickRegion(Board(lockThree: true), new List<RectInt> { Three }, out _));
        Assert.IsFalse(HintBooster.PickRegion(Board(), new List<RectInt>(), out _));
    }
}
