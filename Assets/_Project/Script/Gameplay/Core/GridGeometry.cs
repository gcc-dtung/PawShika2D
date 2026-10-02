using UnityEngine;

/// <summary>
/// Quy đổi tọa độ ô (x, y) sang tọa độ world. Lưới được đặt đối xứng quanh gốc (0, 0).
/// Dùng chung cho GridSpawner (đặt ô) và các view (khung chọn, khối mèo).
/// </summary>
public readonly struct GridGeometry
{
    public readonly int Width;
    public readonly int Height;
    public readonly float Spacing;

    public GridGeometry(int width, int height, float spacing)
    {
        Width = width;
        Height = height;
        Spacing = spacing;
    }

    public Vector3 CellToWorld(Vector2Int pos)
    {
        return new Vector3((pos.x - (Width - 1) / 2f) * Spacing, (pos.y - (Height - 1) / 2f) * Spacing, 0f);
    }

    /// <summary>Ô gần điểm world nhất, kẹp trong lưới (ngón tay ra ngoài lưới vẫn ra 1 ô ở mép).</summary>
    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector2Int p = WorldToCellUnclamped(world);
        return new Vector2Int(Mathf.Clamp(p.x, 0, Mathf.Max(0, Width - 1)), Mathf.Clamp(p.y, 0, Mathf.Max(0, Height - 1)));
    }

    /// <summary>Như WorldToCell nhưng không kẹp: ra ngoài lưới thì trả về ô ảo bên ngoài.</summary>
    public Vector2Int WorldToCellUnclamped(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / Spacing + (Width - 1) / 2f),
            Mathf.RoundToInt(world.y / Spacing + (Height - 1) / 2f));
    }

    /// <summary>Tâm của vùng, kể cả khi vùng chứa ô Void.</summary>
    public Vector3 RectCenter(RectInt rect)
    {
        float cx = (rect.x + (rect.width - 1) / 2f - (Width - 1) / 2f) * Spacing;
        float cy = (rect.y + (rect.height - 1) / 2f - (Height - 1) / 2f) * Spacing;
        return new Vector3(cx, cy, 0f);
    }

    /// <summary>Kích thước phủ trọn các ô của vùng (tính cả khe hở giữa các ô).</summary>
    public Vector2 RectSize(RectInt rect)
    {
        return new Vector2(rect.width * Spacing, rect.height * Spacing);
    }
}
