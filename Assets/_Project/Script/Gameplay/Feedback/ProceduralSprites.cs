using UnityEngine;

/// <summary>
/// Sprite vẽ bằng code cho hiệu ứng game feel (không cần thêm ảnh vào project): chấm tròn mềm (hạt bụi),
/// viên thuốc 9-slice (thanh tiến độ), viền tối hình tròn (vignette), ô trắng, chùm tia sáng (booster). Tạo 1 lần, dùng chung.
/// </summary>
public static class ProceduralSprites
{
    private static Sprite _dot, _pill, _vignette, _white, _rays, _ring, _disc;

    /// <summary>Vòng tròn mảnh, mép mềm (sóng lan khi đóng băng; Image Filled Radial cho vòng đếm ngược).</summary>
    public static Sprite Ring => _ring != null ? _ring : (_ring = Circle("FeelRing", 0.86f));

    /// <summary>Đĩa tròn đặc, mép mịn (nền tối của nút booster khi đếm ngược).</summary>
    public static Sprite Disc => _disc != null ? _disc : (_disc = Circle("FeelDisc", 0f));

    /// <summary>Hình tròn trắng 256 px: <paramref name="inner"/> = 0 là đĩa đặc, &gt; 0 là vòng (bán kính trong / ngoài).</summary>
    private static Sprite Circle(string name, float inner)
    {
        const int size = 256;
        Texture2D tex = NewTexture(size, size, name);
        float r = size / 2f, aa = 1.5f;
        float rIn = r * inner;
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            float a = Mathf.Clamp01((r - 1f - d) / aa);                 // mép ngoài
            if (inner > 0f) a *= Mathf.Clamp01((d - rIn) / aa);         // mép trong
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        return sprite;
    }

    /// <summary>Chùm tia sáng (12 tia) tỏa từ tâm, mờ dần ra ngoài: nền sau icon booster khi bật ra giữa màn hình.</summary>
    public static Sprite Rays
    {
        get
        {
            if (_rays != null) return _rays;
            const int size = 256, rays = 12;
            Texture2D tex = NewTexture(size, size, "FeelRays");
            Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude / (size / 2f);
                float angle = Mathf.Atan2(d.y, d.x);
                float ray = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(angle * rays), 3f); // tia nhọn
                float fade = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f);        // mờ ra ngoài, rỗng ở tâm
                float glow = Mathf.Clamp01(1f - r * 1.8f) * 0.5f;                  // quầng sáng giữa
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ray * fade + glow)));
            }
            tex.Apply();
            _rays = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _rays.name = "FeelRays";
            return _rays;
        }
    }

    /// <summary>Chấm tròn trắng, mép mờ (hạt bụi / ngôi sao nhỏ). Kích thước 1 world unit ở pixelsPerUnit 64.</summary>
    public static Sprite Dot
    {
        get
        {
            if (_dot != null) return _dot;
            const int size = 64;
            Texture2D tex = NewTexture(size, size, "FeelDot");
            Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 3f)));
            }
            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _dot.name = "FeelDot";
            return _dot;
        }
    }

    /// <summary>Viên thuốc trắng (2 đầu tròn) có viền 9-slice: dùng Image.Type.Sliced để kéo dài không méo.</summary>
    public static Sprite Pill
    {
        get
        {
            if (_pill != null) return _pill;
            const int h = 32, w = 64;
            float r = h / 2f;
            Texture2D tex = NewTexture(w, h, "FeelPill");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = Mathf.Clamp(x + 0.5f, r, w - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(px, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            _pill = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, 0f, r, 0f));
            _pill.name = "FeelPill";
            return _pill;
        }
    }

    /// <summary>Giữa trong suốt, ra mép đậm dần (phủ toàn màn hình, tô màu bằng Image.color).</summary>
    public static Sprite Vignette
    {
        get
        {
            if (_vignette != null) return _vignette;
            const int size = 128;
            Texture2D tex = NewTexture(size, size, "FeelVignette");
            Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Khoảng cách tới tâm theo hình chữ nhật bo tròn (0 ở giữa, 1 ở mép)
                Vector2 p = new Vector2(Mathf.Abs(x - c.x) / c.x, Mathf.Abs(y - c.y) / c.y);
                float d = Mathf.Pow(Mathf.Pow(p.x, 4f) + Mathf.Pow(p.y, 4f), 0.25f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _vignette = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _vignette.name = "FeelVignette";
            return _vignette;
        }
    }

    /// <summary>Ô vuông trắng 1 × 1 world unit (phủ màu lên vùng lưới).</summary>
    public static Sprite White
    {
        get
        {
            if (_white != null) return _white;
            Texture2D tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            _white.name = "FeelWhite";
            return _white;
        }
    }

    private static Texture2D NewTexture(int w, int h, string name)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }
}
