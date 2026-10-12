using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hint: chỉ chỗ 1 vùng đúng (mèo mờ nhấp nháy + bàn tay diễn động tác kéo), người chơi tự kéo vào; KHÔNG tự đặt khối.
/// Vùng lấy từ lời giải khớp các khối đã đặt (có khối sai làm lưới hết lời giải thì lấy lời giải từ đầu).
/// Ưu tiên vùng còn trống, rồi vùng nhỏ nhất. Gợi ý còn hiện thì bấm lại nút không tốn thêm lượt.
/// </summary>
public class HintBooster : BoosterEffect
{
    public override BoosterType Type => BoosterType.Hint;

    public override bool IsActive => Host.IsHintShowing;

    public override void Activate()
    {
        GridManager gm = Host.Grid;
        if (gm == null || gm.Board == null || !gm.Board.TrySolveHint(out List<RectInt> remaining)) return;
        if (!PickRegion(gm.Board, remaining, out RectInt pick)) return;

        Host.Consume(Type, gm.Board.Geometry.RectCenter(pick));
        Host.ShowHint(pick);
    }

    /// <summary>
    /// Vùng để gợi ý: bỏ vùng có ô số còn khóa; ưu tiên vùng chưa bị khối nào đè (người chơi kéo vào được ngay),
    /// trong cùng nhóm thì vùng nhỏ nhất (dễ thấy, dễ kéo).
    /// </summary>
    public static bool PickRegion(BoardModel board, List<RectInt> candidates, out RectInt pick)
    {
        pick = default;
        bool found = false, foundFree = false;
        int bestArea = int.MaxValue;
        foreach (RectInt rect in candidates)
        {
            if (IsLocked(board, rect)) continue;
            bool free = board.GetBlocksOverlapping(rect).Count == 0;
            int area = rect.width * rect.height;
            bool better = !found || (free && !foundFree) || (free == foundFree && area < bestArea);
            if (!better) continue;
            pick = rect;
            found = true;
            foundFree = free;
            bestArea = area;
        }
        return found;
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
