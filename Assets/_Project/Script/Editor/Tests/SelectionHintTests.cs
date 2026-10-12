using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode: trạng thái vùng kéo (PlacementCheck.Hint) đi qua đúng BoardModel.Check như lúc chơi.
/// Neutral = kéo tiếp còn có thể đúng; Ready = thả tay là đặt; Invalid = chắc chắn sai.
/// </summary>
public class SelectionHintTests
{
    private static ClueData Clue(int x, int y, int value,
        ClueConstraint constraint = ClueConstraint.None, int lockSteps = 0, bool hidden = false) =>
        new ClueData
        {
            x = x,
            y = y,
            value = value,
            mechanic = new ClueMechanic { constraint = constraint, lockSteps = lockSteps, hidden = hidden }
        };

    /// <summary>Lưới w × h; ô nằm trong <paramref name="voids"/> bị bỏ (ô Void).</summary>
    private static BoardModel Board(int w, int h, ClueData[] clues, params Vector2Int[] voids)
    {
        List<CellModel> cells = new List<CellModel>();
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (System.Array.IndexOf(voids, pos) >= 0) continue;
            int i = System.Array.FindIndex(clues, c => c.x == x && c.y == y);
            cells.Add(i >= 0 ? new CellModel(pos, clues[i]) : new CellModel(pos));
        }
        return new BoardModel(null, new GridGeometry(w, h, 1f), cells);
    }

    private static SelectionHint HintOf(BoardModel board, RectInt rect) => board.Check(rect).Hint;

    [Test]
    public void ThieuO_Neutral_DuO_Ready_ThuaO_Invalid()
    {
        BoardModel board = Board(6, 2, new[] { Clue(0, 0, 3) });
        Assert.AreEqual(SelectionHint.Neutral, HintOf(board, new RectInt(0, 0, 1, 1)), "Mới chạm ô số 3");
        Assert.AreEqual(SelectionHint.Neutral, HintOf(board, new RectInt(0, 0, 2, 1)));
        Assert.AreEqual(SelectionHint.Ready, HintOf(board, new RectInt(0, 0, 3, 1)));
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 4, 1)));
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 2, 2)), "4 ô dạng vuông cũng là thừa");
    }

    [Test]
    public void ONumber1_ChamVaoLaReady()
    {
        BoardModel board = Board(3, 1, new[] { Clue(1, 0, 1) });
        Assert.AreEqual(SelectionHint.Ready, HintOf(board, new RectInt(1, 0, 1, 1)));
    }

    [Test]
    public void ChuaCoOSo_Neutral()
    {
        BoardModel board = Board(6, 2, new[] { Clue(0, 0, 3) });
        Assert.AreEqual(SelectionHint.Neutral, HintOf(board, new RectInt(2, 1, 3, 1)));
    }

    [Test]
    public void HaiOSo_Invalid()
    {
        BoardModel board = Board(6, 1, new[] { Clue(0, 0, 3), Clue(2, 0, 2) });
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 3, 1)));
    }

    [Test]
    public void OSoConKhoa_Invalid()
    {
        BoardModel board = Board(4, 1, new[] { Clue(0, 0, 2, lockSteps: 1) });
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 2, 1)));
    }

    [Test]
    public void VungChuaOVoid_Invalid()
    {
        BoardModel board = Board(4, 1, new[] { Clue(0, 0, 3) }, new Vector2Int(1, 0));
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 3, 1)));
    }

    [Test]
    public void SaiHuong_ChuaDuO_Neutral_DaDuO_Invalid()
    {
        BoardModel board = Board(4, 4, new[] { Clue(0, 0, 3, ClueConstraint.Vertical) });
        Assert.AreEqual(SelectionHint.Neutral, HintOf(board, new RectInt(0, 0, 2, 1)), "Kéo ngang 2 ô: còn đổi hướng được");
        Assert.AreEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 3, 1)), "Ngang đủ 3 ô: chắc chắn sai hướng");
        Assert.AreEqual(SelectionHint.Ready, HintOf(board, new RectInt(0, 0, 1, 3)));
    }

    [Test]
    public void OAn_KhongBaoSaiTheoSoO()
    {
        // Ô "?" người chơi không biết số: Check không xét số ô -> không bao giờ đỏ vì thừa / thiếu
        BoardModel board = Board(6, 1, new[] { Clue(0, 0, 3, hidden: true) });
        Assert.AreNotEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 2, 1)));
        Assert.AreNotEqual(SelectionHint.Invalid, HintOf(board, new RectInt(0, 0, 5, 1)));
    }
}
