using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hint: tự đặt 1 khối đúng (khối nhỏ nhất đang đặt được cho dễ thấy), lấy từ lời giải khớp các khối đã đặt;
/// có khối đặt sai làm lưới hết lời giải thì lấy lời giải từ đầu và gỡ khối sai đang đè lên chỗ đặt.
/// </summary>
public class HintBooster : BoosterEffect
{
    public override BoosterType Type => BoosterType.Hint;

    public override void Activate()
    {
        GridManager gm = Host.Grid;
        if (gm == null || gm.Board == null || !gm.Board.TrySolveHint(out List<RectInt> remaining)) return;

        RectInt? pick = null;
        foreach (RectInt rect in remaining)
        {
            if (IsLocked(gm.Board, rect)) continue; // ô số còn khóa
            if (pick == null || rect.width * rect.height < pick.Value.width * pick.Value.height) pick = rect;
        }
        if (pick == null || !gm.PlaceSolved(pick.Value)) return;

        Host.Consume(Type);
    }

    /// <summary>Ô số của vùng còn khóa (vùng có thể đang bị khối sai đè nên không dùng Check).</summary>
    internal static bool IsLocked(BoardModel board, RectInt rect)
    {
        foreach (CellModel cell in board.GetCellsIn(rect))
        {
            if (cell.HasClue) return cell.Clue.IsLocked;
        }
        return true;
    }
}
