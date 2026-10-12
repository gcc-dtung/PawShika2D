using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PawBlock.Game;

namespace PawBlock.Shared
{
    /// <summary>
    /// Hoa văn chân mèo phủ nền màn chơi như bản gốc (GameplayUI/BG_Pattern: ảnh BG_Pattern lát kín màn hình trên nền bg).
    /// Tools → PawBlock → Builders → Build Background Pattern (chạy lại được):
    /// SpriteRenderer con "Pattern" của nền "bg" (BackgroundFitter), vẽ kiểu Tiled phủ đúng kích thước sprite nền,
    /// nên đi theo nền khi camera zoom / dời theo lưới. Thứ tự vẽ: bg -2 < Pattern -1 < ô lưới (0+).
    /// Ô lát rộng ~1.55 đơn vị = 198 px trên màn 1920 px chiều cao như bản gốc (camera ortho 7.5 -> 128 px / đơn vị).
    /// </summary>
    public static class BackgroundPatternBuilder
    {
        private const string PatternPath = "Assets/_Project/GameAssets/04_UI_Graphics/Backgrounds/BG_Pattern.png";
        private const float TileWorldWidth = 198f / 128f;

        [MenuItem("Tools/PawBlock/Builders/Build Background Pattern", priority = 114)]
        public static void Build()
        {
            BackgroundFitter fitter = Object.FindFirstObjectByType<BackgroundFitter>(FindObjectsInactive.Include);
            SpriteRenderer bg = fitter != null ? fitter.GetComponent<SpriteRenderer>() : null;
            if (bg == null || bg.sprite == null)
            {
                Debug.LogError("[BackgroundPatternBuilder] Không thấy nền (BackgroundFitter + SpriteRenderer) trong scene.");
                return;
            }

            Sprite pattern = EnsureTileable(PatternPath);
            if (pattern == null) return;

            PopupBuilder.DestroyChild(bg.transform, "Pattern");
            GameObject go = new GameObject("Pattern");
            Undo.RegisterCreatedObjectUndo(go, "Build Background Pattern");
            go.transform.SetParent(bg.transform, false);

            // Ô lát theo kích thước thế giới mong muốn: bù scale của nền (con kế thừa scale cha)
            float tileLocal = pattern.bounds.size.x;
            float parentScale = bg.transform.lossyScale.x;
            float scale = TileWorldWidth / (tileLocal * parentScale);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pattern;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = bg.sprite.bounds.size / scale; // phủ đúng sprite nền (BackgroundFitter đã lo phủ kín camera)
            sr.sortingLayerID = bg.sortingLayerID;

            Undo.RecordObject(bg, "Build Background Pattern");
            bg.sortingOrder = -2;
            sr.sortingOrder = -1;

            EditorSceneManager.MarkSceneDirty(bg.gameObject.scene);
            Debug.Log($"[BackgroundPatternBuilder] Đã phủ hoa văn lên nền {bg.name} (ô lát {TileWorldWidth:0.00} đơn vị).");
        }

        /// <summary>Sprite vẽ Tiled cần Mesh Type = Full Rect và Wrap = Repeat.</summary>
        private static Sprite EnsureTileable(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[BackgroundPatternBuilder] Không thấy {path}");
                return null;
            }
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType != TextureImporterType.Sprite || settings.spriteMeshType != SpriteMeshType.FullRect
                || importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
