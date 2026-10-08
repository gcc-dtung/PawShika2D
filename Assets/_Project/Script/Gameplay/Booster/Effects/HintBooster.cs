using System.Collections.Generic;
using UnityEngine;

/// <summary>Hint: tự đặt 1 khối đúng (khối nhỏ nhất đang đặt được cho dễ thấy), lấy từ lời giải khớp các khối đã đặt.</summary>
public class HintBooster : BoosterEffect
{
    public override BoosterType Type => BoosterType.Hint;

    public override void Activate()
    {
        GridManager gm = Host.Grid;
        if (gm == null || gm.Board == null || !gm.Board.TrySolveRemaining(out List<RectInt> remaining)) return;

        RectInt? pick = null;
        foreach (RectInt rect in remaining)
        {
            if (!gm.Board.Check(rect).IsReady) continue; // ô số còn khóa
            if (pick == null || rect.width * rect.height < pick.Value.width * pick.Value.height) pick = rect;
        }
        if (pick == null || !gm.PlaceSolved(pick.Value)) return;

        Host.Consume(Type);
    }
}
