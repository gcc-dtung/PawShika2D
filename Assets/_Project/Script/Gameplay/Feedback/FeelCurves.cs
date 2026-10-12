using UnityEngine;

/// <summary>
/// Công thức hiệu ứng game feel (hàm thuần: không tween, không MonoBehaviour).
/// View chỉ gọi các hàm này trong tween, nên test được công thức mà không cần Play mode (xem Editor/Tests).
/// </summary>
public static class FeelCurves
{
    /// <summary>
    /// Squash & stretch khi khối "đáp" xuống, <paramref name="p"/> 0..1 -> (scale X, scale Y):
    /// dẹt (ngang to, dọc thấp) -> dài -> dẹt nhẹ -> đứng yên, biên độ tắt dần. p = 0 và p = 1 đều là (1, 1).
    /// </summary>
    public static Vector2 Squash(float p, float amount, float wobbles)
    {
        p = Mathf.Clamp01(p);
        float decay = (1f - p) * (1f - p);
        float d = amount * decay * Mathf.Sin(p * Mathf.PI * wobbles);
        return new Vector2(1f + d, 1f - d);
    }

    /// <summary>
    /// Dịch tâm theo Y để đáy của vật cao <paramref name="height"/> (pivot ở giữa) đứng yên khi scale Y = <paramref name="scaleY"/>.
    /// </summary>
    public static float BottomAnchorOffset(float scaleY, float height) => (scaleY - 1f) * height * 0.5f;

    /// <summary>Ô nảy, <paramref name="p"/> 0..1 -> scale đều: 1 -> 1 + amount (giữa) -> 1.</summary>
    public static float Bump(float p, float amount) => 1f + amount * Mathf.Sin(Mathf.Clamp01(p) * Mathf.PI);

    /// <summary>Ô cách vùng bao nhiêu vòng (tính cả chéo): 0 = trong vùng, 1 = sát cạnh / góc, 2 = vòng kế tiếp...</summary>
    public static int RingDistance(RectInt rect, Vector2Int cell)
    {
        int dx = Mathf.Max(rect.xMin - cell.x, cell.x - (rect.xMax - 1), 0);
        int dy = Mathf.Max(rect.yMin - cell.y, cell.y - (rect.yMax - 1), 0);
        return Mathf.Max(dx, dy);
    }

    /// <summary>Độ nảy của ô ở vòng <paramref name="ring"/>: vòng 1 = amount, giảm dần ra ngoài; vòng 0 / ngoài radius = 0.</summary>
    public static float RippleAmount(int ring, int radius, float amount)
    {
        if (ring < 1 || ring > radius || radius < 1) return 0f;
        return amount * (1f - (ring - 1f) / radius);
    }

    /// <summary>Thời điểm ô ở vòng <paramref name="ring"/> bắt đầu nảy: vòng 1 chờ startDelay, mỗi vòng ra ngoài trễ thêm stepDelay.</summary>
    public static float RippleDelay(int ring, float startDelay, float stepDelay) => startDelay + Mathf.Max(0, ring - 1) * stepDelay;

    /// <summary>
    /// Cao độ tick khi kéo vùng có <paramref name="count"/> ô: basePitch × 2^(nửa cung / 12),
    /// nửa cung lấy semitones[count - 1] (quá mảng thì giữ nốt cuối).
    /// </summary>
    public static float DragPitch(int count, float basePitch, int[] semitones)
    {
        if (semitones == null || semitones.Length == 0) return basePitch;
        int semis = semitones[Mathf.Clamp(count - 1, 0, semitones.Length - 1)];
        return basePitch * Mathf.Pow(2f, semis / 12f);
    }

    /// <summary>
    /// Ô dưới ngón tay có "dính": ngón tay còn cách tâm ô hiện tại không quá (0.5 + stick) ô theo mỗi trục thì giữ ô đó,
    /// tránh vùng chọn nhảy qua lại khi ngón tay rung ở ranh giới 2 ô. stick = 0 thì như WorldToCell.
    /// </summary>
    public static Vector2Int StickyCell(GridGeometry geometry, Vector3 world, Vector2Int current, float stick)
    {
        Vector2Int cell = geometry.WorldToCell(world);
        if (cell == current || stick <= 0f) return cell;
        if (current.x < 0 || current.y < 0 || current.x >= geometry.Width || current.y >= geometry.Height) return cell;

        Vector3 center = geometry.CellToWorld(current);
        float limit = (0.5f + stick) * geometry.Spacing;
        bool inside = Mathf.Abs(world.x - center.x) <= limit && Mathf.Abs(world.y - center.y) <= limit;
        return inside ? current : cell;
    }

    /// <summary>Cao độ âm đặt khối theo combo: combo 1 = 1, mỗi bậc cao thêm <paramref name="step"/>, tối đa <paramref name="max"/>.</summary>
    public static float ComboPitch(int combo, float step, float max) => Mathf.Min(1f + Mathf.Max(0, combo - 1) * step, max);

    /// <summary>Bậc lời khen theo combo: combo 1 → 0, 2 → 1, 3 → 2, từ 4 trở lên → 3 (kẹp theo số bậc có).</summary>
    public static int PraiseTier(int combo, int tierCount) => tierCount <= 0 ? 0 : Mathf.Clamp(combo - 1, 0, tierCount - 1);
}

/// <summary>Đếm combo: lần đặt sau cách lần trước không quá Window giây thì combo + 1, quá thì về 1.</summary>
public class ComboCounter
{
    public float Window;
    public int Count { get; private set; }
    private float _last = float.NegativeInfinity;

    public ComboCounter(float window) { Window = window; }

    /// <summary>Ghi 1 lần đặt lúc <paramref name="time"/> (giây), trả về combo hiện tại.</summary>
    public int Register(float time)
    {
        Count = time - _last <= Window ? Count + 1 : 1;
        _last = time;
        return Count;
    }

    public void Reset()
    {
        Count = 0;
        _last = float.NegativeInfinity;
    }
}
