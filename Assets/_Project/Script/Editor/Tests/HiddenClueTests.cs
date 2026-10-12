using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode: ô "?" không bao giờ lộ số khi đặt khối (đặt đúng hay sai số ô đều vẫn ẩn),
/// nhưng số thật vẫn dùng để chấm khi lưới kín.
/// </summary>
public class HiddenClueTests
{
    /// <summary>Lưới 4 × 1, ô (0,0) là "?" số thật 3.</summary>
    private static BoardModel Board(out CellModel hidden)
    {
        ClueData data = new ClueData { x = 0, y = 0, value = 3, mechanic = new ClueMechanic { hidden = true } };
        hidden = new CellModel(Vector2Int.zero, data);
        List<CellModel> cells = new List<CellModel> { hidden };
        for (int x = 1; x < 4; x++) cells.Add(new CellModel(new Vector2Int(x, 0)));
        return new BoardModel(null, new GridGeometry(4, 1, 1f), cells);
    }

    [Test]
    public void DatSaiSoO_VanAn_VaBiChamSai()
    {
        BoardModel board = Board(out CellModel hidden);
        bool changed = false;
        hidden.Changed += () => changed = true;

        PlacementCheck check = board.Check(new RectInt(0, 0, 4, 1)); // 4 ô, số thật 3
        Assert.IsTrue(check.IsReady, "Ô \"?\" không xét số ô nên vẫn đặt được");
        board.Place(check, default, null);

        Assert.IsTrue(hidden.Clue.IsHidden, "Đặt khối sai số ô không được lộ số thật");
        Assert.IsFalse(changed, "Ô không được đổi hiển thị");
        List<BlockModel> wrong = new List<BlockModel>();
        Assert.IsFalse(board.IsSolved(wrong), "Số thật vẫn dùng để chấm");
        Assert.AreEqual(1, wrong.Count);
    }

    [Test]
    public void DatDungSoO_VanAn_VaThang()
    {
        BoardModel board = Board(out CellModel hidden);
        board.Place(board.Check(new RectInt(0, 0, 3, 1)), default, null);
        Assert.IsTrue(hidden.Clue.IsHidden, "Đặt đúng cũng không lộ số");

        // Ô còn lại không có số -> lưới chưa kín; lưới 3 ô thì kín và đúng
        BoardModel full = new BoardModel(null, new GridGeometry(3, 1, 1f), new List<CellModel>
        {
            new CellModel(Vector2Int.zero, new ClueData { value = 3, mechanic = new ClueMechanic { hidden = true } }),
            new CellModel(new Vector2Int(1, 0)),
            new CellModel(new Vector2Int(2, 0))
        });
        full.Place(full.Check(new RectInt(0, 0, 3, 1)), default, null);
        Assert.IsTrue(full.IsSolved(null), "Đúng số thật thì thắng dù vẫn hiện \"?\"");
    }

    [Test]
    public void GoKhoi_VanAn()
    {
        BoardModel board = Board(out CellModel hidden);
        BlockModel block = board.Place(board.Check(new RectInt(0, 0, 2, 1)), default, null);
        board.Remove(block);
        Assert.IsTrue(hidden.Clue.IsHidden);
    }
}
