using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dựng Resources/GameplayVisuals.asset từ asset PawBlock:
/// - Đặt 9-slice cho sprite khối màu.
/// - Cắt atlas Spine ingame_cat_face_03 thành các khung hình mặt mèo (không cần Spine runtime).
/// - Gán sprite tai, ổ khóa, mũi tên, bong bóng, chữ khen.
/// </summary>
public static class GameplayVisualsSetup
{
    private const string Root = "Assets/_Project/GameAssets";
    private const string BlocksFolder = Root + "/03_Gameplay_Blocks_Grid/Blocks";
    private const string EarsFolder = Root + "/03_Gameplay_Blocks_Grid/Cat_Ears";
    private const string FaceFolder = Root + "/02_Cat_Characters_Spine/Spine_Cat_Face";
    private const string FaceAtlas = FaceFolder + "/ingame_cat_face_03.atlas.txt";
    private const string PraiseFolder = Root + "/02_Cat_Characters_Spine/Praise_Badges";
    private const string LockBody = Root + "/04_UI_Graphics/Buttons/btn_lock_1.png";
    private const string LockShackle = Root + "/04_UI_Graphics/Buttons/btn_lock_2.png";
    private const string Arrow = "Assets/_Project/ShikaCat_GameAssets/04_UI_Graphics/Dev_Debug_UI/arrow-right@2x.png";
    private const string Bubble = Root + "/03_Gameplay_Blocks_Grid/Grid_Cells/Circle.png";
    private const string OutputPath = "Assets/_Project/Resources/GameplayVisuals.asset";

    private const int BlockBorder = 48;      // px, sprite khối 160x160
    private const float FacePixelsPerUnit = 100f;

    private class AtlasRegion
    {
        public string page;
        public string name;   // ví dụ chilly_01/chilly_000
        public RectInt bounds;
        public bool rotated;
    }

    [MenuItem("Tools/PawBlock/Setup Gameplay Visuals")]
    public static void Setup()
    {
        GameplayVisuals visuals = AssetDatabase.LoadAssetAtPath<GameplayVisuals>(OutputPath);
        if (visuals == null)
        {
            EnsureFolder(Path.GetDirectoryName(OutputPath).Replace('\\', '/'));
            visuals = ScriptableObject.CreateInstance<GameplayVisuals>();
            AssetDatabase.CreateAsset(visuals, OutputPath);
        }

        string[] colorNames = Enum.GetNames(typeof(ColorType));
        visuals.blockSprites = new Sprite[colorNames.Length];
        visuals.earSprites = new Sprite[colorNames.Length];
        for (int i = 0; i < colorNames.Length; i++)
        {
            visuals.blockSprites[i] = LoadSprite($"{BlocksFolder}/{colorNames[i]}_Block.png", new Vector4(BlockBorder, BlockBorder, BlockBorder, BlockBorder));
            visuals.earSprites[i] = LoadSprite($"{EarsFolder}/{colorNames[i]}_Ear.png");
        }

        visuals.lockBodySprite = LoadSprite(LockBody);
        visuals.lockShackleSprite = LoadSprite(LockShackle);
        visuals.arrowSprite = LoadSprite(Arrow);
        visuals.bubbleSprite = LoadSprite(Bubble);
        visuals.praiseSprites = Directory.GetFiles(PraiseFolder, "*.png")
            .Select(p => LoadSprite(p.Replace('\\', '/')))
            .Where(s => s != null)
            .ToArray();

        visuals.faces = BuildFaces();

        EditorUtility.SetDirty(visuals);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VisualsSetup] Xong: {visuals.faces.Length} bộ mặt mèo, {visuals.praiseSprites.Length} chữ khen -> {OutputPath}");
    }

    private static Sprite LoadSprite(string path, Vector4? border = null)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[VisualsSetup] Không tìm thấy {path}");
            return null;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (border.HasValue && importer.spriteBorder != border.Value)
        {
            importer.spriteBorder = border.Value;
            changed = true;
        }
        if (border.HasValue)
        {
            // Sprite 9-slice vẽ bằng SpriteRenderer.Sliced cần mesh Full Rect
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                changed = true;
            }
        }
        if (changed) importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static CatFaceAnimation[] BuildFaces()
    {
        List<AtlasRegion> regions = ParseAtlas(FaceAtlas, out Dictionary<string, Vector2Int> pageSizes);
        if (regions.Count == 0) return new CatFaceAnimation[0];

        // 1. Cắt từng trang atlas thành sprite
        foreach (var group in regions.GroupBy(r => r.page))
        {
            string pagePath = $"{FaceFolder}/{group.Key}";
            TextureImporter importer = AssetImporter.GetAtPath(pagePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[VisualsSetup] Không tìm thấy trang atlas {pagePath}");
                continue;
            }

            int pageH = pageSizes[group.Key].y;
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            foreach (AtlasRegion r in group)
            {
                int w = r.rotated ? r.bounds.height : r.bounds.width;
                int h = r.rotated ? r.bounds.width : r.bounds.height;
                metas.Add(new SpriteMetaData
                {
                    name = SpriteName(r.name),
                    rect = new Rect(r.bounds.x, pageH - r.bounds.y - h, w, h),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = FacePixelsPerUnit;
            importer.mipmapEnabled = false;
#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();
        }

        // 2. Gom sprite theo bộ mặt (thư mục trong tên region), sắp theo số khung
        Dictionary<string, Sprite> spriteByName = new Dictionary<string, Sprite>();
        foreach (string page in pageSizes.Keys)
        {
            foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath($"{FaceFolder}/{page}"))
            {
                if (o is Sprite s) spriteByName[s.name] = s;
            }
        }

        List<CatFaceAnimation> faces = new List<CatFaceAnimation>();
        foreach (var group in regions.GroupBy(r => Path.GetDirectoryName(r.name).Replace('\\', '/')).OrderBy(g => g.Key))
        {
            // Bộ mặt có khung bị xoay khi đóng gói (boltie_01) có kích thước khác các bộ còn lại -> bỏ
            if (group.Any(r => r.rotated)) continue;

            CatFaceAnimation anim = new CatFaceAnimation { name = Path.GetFileName(group.Key) };
            foreach (AtlasRegion r in group.OrderBy(r => r.name, StringComparer.Ordinal))
            {
                if (spriteByName.TryGetValue(SpriteName(r.name), out Sprite s)) anim.frames.Add(s);
            }
            if (anim.frames.Count > 0) faces.Add(anim);
        }
        return faces.ToArray();
    }

    private static string SpriteName(string regionName) => regionName.Replace('/', '_');

    /// <summary>Đọc atlas Spine 4.x: dòng tên trang, các dòng "key:value" của trang, rồi từng region.</summary>
    private static List<AtlasRegion> ParseAtlas(string path, out Dictionary<string, Vector2Int> pageSizes)
    {
        List<AtlasRegion> regions = new List<AtlasRegion>();
        pageSizes = new Dictionary<string, Vector2Int>();

        TextAsset atlas = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (atlas == null)
        {
            Debug.LogWarning($"[VisualsSetup] Không tìm thấy atlas {path}");
            return regions;
        }

        string page = null;
        bool expectPage = true;
        AtlasRegion current = null;

        foreach (string raw in atlas.text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0)
            {
                expectPage = true;
                current = null;
                continue;
            }

            if (expectPage)
            {
                page = line;
                expectPage = false;
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon > 0)
            {
                string key = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();

                if (current == null)
                {
                    if (key == "size")
                    {
                        int[] v = ParseInts(value);
                        pageSizes[page] = new Vector2Int(v[0], v[1]);
                    }
                    continue;
                }

                if (key == "bounds")
                {
                    int[] v = ParseInts(value);
                    current.bounds = new RectInt(v[0], v[1], v[2], v[3]);
                }
                else if (key == "rotate")
                {
                    current.rotated = value == "90" || value == "true";
                }
                continue;
            }

            current = new AtlasRegion { page = page, name = line.StartsWith("images/") ? line.Substring(7) : line };
            regions.Add(current);
        }

        return regions;
    }

    private static int[] ParseInts(string value)
    {
        return value.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
