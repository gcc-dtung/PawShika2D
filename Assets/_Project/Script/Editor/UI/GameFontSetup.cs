using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Font chung của game: Fredoka Bold (nét tròn, đậm, hợp phong cách casual mèo của bản gốc).
/// - Đặt làm font mặc định của TMP Settings (chữ dựng bằng PopupBuilder.Text dùng font này)
/// - LiberationSans SDF làm fallback cho ký tự Fredoka không có
/// - Tạo các material preset viền chữ (Outline Purple / Green / Brown) dùng cho Menu
/// - Đổi font mọi TMP_Text đang dùng LiberationSans trong scene đang mở và prefab trong Assets/_Project
/// Chạy lại được.
/// </summary>
public static class GameFontSetup
{
    public const string FontPath = "Assets/_Project/GameAssets/06_Fonts/Fredoka-Bold SDF.asset";
    private const string FallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string PresetFolder = "Assets/_Project/GameAssets/06_Fonts/";
    // Gán shader theo đường dẫn, không dùng Shader.Find: 09_Shaders_Materials có bản rip "TextMeshPro/Distance Field"
    // trùng tên từ game gốc, không vẽ được (chữ thành ô đen) — đó là lý do Fredoka từng "hỏng".
    private const string SdfShaderPath = "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader";

    public static readonly Color PurpleOutline = new Color32(0x3B, 0x1F, 0x78, 0xFF);
    public static readonly Color GreenOutline = new Color32(0x1C, 0x62, 0x10, 0xFF);
    public static readonly Color BrownOutline = new Color32(0x7A, 0x2E, 0x12, 0xFF);
    public static readonly Color RedOutline = new Color32(0x8E, 0x0E, 0x14, 0xFF);
    public static readonly Color GrayOutline = new Color32(0x4A, 0x4A, 0x4A, 0xFF);

    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    [MenuItem("Tools/PawBlock/Builders/Apply Game Font", priority = 100)]
    public static void Apply()
    {
        TMP_FontAsset font = Font;
        if (font == null)
        {
            Debug.LogError("[GameFontSetup] Không tìm thấy " + FontPath);
            return;
        }

        Shader sdf = AssetDatabase.LoadAssetAtPath<Shader>(SdfShaderPath);
        if (sdf != null && font.material.shader != sdf)
        {
            font.material.shader = sdf;
            EditorUtility.SetDirty(font.material);
        }

        // Fallback
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        if (fallback != null)
        {
            if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback))
            {
                font.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(font);
            }
        }

        // Font mặc định của TMP
        SerializedObject settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(TMP_Settings.instance);

        // Preset viền
        OutlinePurple();
        OutlineGreen();
        OutlineBrown();
        OutlineRed();
        OutlineGray();

        // Scene đang mở
        int count = 0;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (Replace(t, font)) count++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Prefab
        int prefabCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Replace(t, font)) { changed = true; prefabCount++; }
            }
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GameFontSetup] Font Fredoka Bold: đổi {count} chữ trong scene, {prefabCount} chữ trong prefab.");
    }

    public static Material OutlinePurple() => Preset("Outline Purple", PurpleOutline, 0.3f, 1.1f);
    public static Material OutlineGreen() => Preset("Outline Green", GreenOutline, 0.3f, 1.1f);
    public static Material OutlineRed() => Preset("Outline Red", RedOutline, 0.28f, 0.8f);
    public static Material OutlineBrown() => Preset("Outline Brown", BrownOutline, 0.28f, 0.8f);
    public static Material OutlineGray() => Preset("Outline Gray", GrayOutline, 0.3f, 1.1f);

    /// <summary>
    /// Material preset từ material gốc của font: viền đậm + bóng đổ cùng màu phía dưới (như chữ bản gốc).
    /// Tên bắt đầu bằng tên font để hiện trong ô Material Preset của TMP.
    /// </summary>
    private static Material Preset(string suffix, Color color, float width, float shadowOffset)
    {
        ShaderUtilities.GetShaderPropertyIDs();
        TMP_FontAsset font = Font;
        string path = PresetFolder + font.name + " - " + suffix + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(font.material);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = AssetDatabase.LoadAssetAtPath<Shader>(SdfShaderPath);
        mat.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
        mat.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
        mat.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
        mat.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);

        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);

        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, color);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -shadowOffset);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Đổi chữ đang dùng LiberationSans (hoặc chưa có font) sang Fredoka; giữ nguyên chữ đã dùng font khác.</summary>
    private static bool Replace(TMP_Text t, TMP_FontAsset font)
    {
        if (t.font == font) return false;
        if (t.font != null && !t.font.name.StartsWith("LiberationSans")) return false;
        Undo.RecordObject(t, "Apply Game Font");
        t.font = font;
        t.fontSharedMaterial = font.material;
        EditorUtility.SetDirty(t);
        return true;
    }
}
