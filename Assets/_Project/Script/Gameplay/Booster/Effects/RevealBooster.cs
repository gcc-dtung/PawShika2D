using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reveal: bấm nút -> chế độ chọn số; chạm 1 ô số -> giải mọi khối cùng số (ô còn khóa thì bỏ qua).
/// Lượt chỉ bị trừ lúc chạm trúng ô số có khối để đặt; bấm lại nút / nút X trên banner = hủy.
/// </summary>
public class RevealBooster : BoosterEffect
{
    private bool _on;

    public override BoosterType Type => BoosterType.Reveal;

    public override bool IsActive => _on;

    public override void Activate() => SetMode(true);

    public override void PressWhileActive() => SetMode(false);

    public override void Cancel() => SetMode(false);

    public override bool HandleCellTap(Vector2Int pos)
    {
        if (!_on) return false;

        GridManager gm = Host.Grid;
        CellModel cell = gm != null && gm.Board != null ? gm.Board.GetCell(pos) : null;
        // Chạm ô không phải số / đã có khối / còn khóa: giữ chế độ chọn, chờ chạm lại
        // [MECHANIC] Reveal: ô số nào không được chọn (cơ chế che số / chặn chọn)
        if (cell == null || !cell.HasClue || cell.IsOccupied || cell.Clue.IsLocked) return true;
        if (!gm.Board.TrySolveRemaining(out List<RectInt> remaining)) return true;

        int value = cell.Clue.Value;
        List<RectInt> targets = new List<RectInt>();
        foreach (RectInt rect in remaining)
        {
            PlacementCheck check = gm.Board.Check(rect);
            if (check.IsReady && check.ClueCell.Clue.Value == value) targets.Add(rect);
        }
        if (targets.Count == 0) return true;

        SetMode(false);
        Host.Consume(Type);
        Host.PlaceInSequence(targets);
        return true;
    }

    private void SetMode(bool on)
    {
        if (_on == on) return;
        _on = on;
        Host.NotifyModeChanged(Type, on);
    }
}
