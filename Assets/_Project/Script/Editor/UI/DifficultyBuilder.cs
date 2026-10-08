using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level Hard / Very Hard (Tools → PawBlock → Builders → Build Difficulty), chạy lại được:
/// - DifficultyTheme.asset (sprite / chữ / màu từng mức khó; chưa có thì tạo)
/// - Nút PLAY: con HardFrame (khung_home_hard: viền kem có tab ghi mức khó), đứng yên khi nhấn (ButtonPressIgnore)
/// - Canvas_HUD LevelFrame: con HardFrame (khung_hard1 / khung_expert1 có tab) bao ô LEVEL
/// - Canvas_HUD/DifficultyIntro: nền tối, dải băng sọc, băng chữ, mèo đầu lâu (hiệu ứng vào màn)
/// - Level nào Hard / Very Hard: đánh dấu trong Tools → PawBlock → Level Difficulty (hoặc Level Editor)
/// </summary>
public static class DifficultyBuilder
{
    private const string ThemePath = GameConfigSetup.Folder + "DifficultyTheme.asset";
    private const string UI = PopupBuilder.UI;
    private const string Frames = UI + "Frames_Borders/";
    private const string Buttons = UI + "Buttons/";

    // khung_home_hard 534x283: thân (không tính tab) cao ~229 -> thu theo thân nút PLAY 225
    private const float PlayFrameScale = 225f / 229f;
    private const float PlayTabHeight = 54f;

    [MenuItem("Tools/PawBlock/Builders/Build Difficulty", priority = 103)]
    public static void Build()
    {
        DifficultyTheme theme = EnsureTheme();

        PlayButtonView play = Object.FindFirstObjectByType<PlayButtonView>(FindObjectsInactive.Include);
        if (play != null) BuildPlayParts(play, theme);
        else Debug.LogWarning("[DifficultyBuilder] Không thấy nút PLAY (Build Main Menu trước).");

        LevelHUD hud = Object.FindFirstObjectByType<LevelHUD>(FindObjectsInactive.Include);
        Transform levelFrame = hud != null ? hud.transform.Find("LevelFrame") : null;
        if (levelFrame != null) BuildHudFrame(hud, (RectTransform)levelFrame, theme);
        else Debug.LogWarning("[DifficultyBuilder] Không thấy TopPanel/LevelFrame.");

        GameObject hudCanvas = PopupBuilder.FindRoot("Canvas_HUD");
        if (hudCanvas != null) BuildIntro(hudCanvas.transform, theme);

        if (play != null) EditorSceneManager.MarkSceneDirty(play.gameObject.scene);
        Debug.Log("[DifficultyBuilder] Đã dựng giao diện level Hard / Very Hard.");
    }

    // ---------- Theme ----------

    /// <summary>
    /// Băng chữ kéo dài (tạo bằng script ngoài từ khung_hard_3 / khung_expert_3): nối thêm 650 px băng trơn theo đúng độ nghiêng
    /// mỗi bên, chữ giữ nguyên cỡ -> băng phủ quá 2 mép màn như bản gốc.
    /// </summary>
    private static Sprite WideBanner(string name)
    {
        string path = Frames + name + "_wide.png";
        PopupBuilder.EnsureSprite(path);
        return PopupBuilder.LoadSprite(path);
    }

    internal static DifficultyTheme EnsureTheme()
    {
        DifficultyTheme theme = AssetDatabase.LoadAssetAtPath<DifficultyTheme>(ThemePath);
        if (theme != null)
        {
            UpgradeTheme(theme);
            return theme;
        }

        theme = ScriptableObject.CreateInstance<DifficultyTheme>();
        theme.playFrame = PopupBuilder.LoadSprite(Frames + "khung_home_hard.png");
        theme.hazardTape = PopupBuilder.LoadSprite(Frames + "khung_expert_4.png");
        theme.styles = new[]
        {
            new DifficultyStyle
            {
                difficulty = LevelDifficulty.Hard,
                label = "Hard",
                playFace = PopupBuilder.LoadSprite(Buttons + "btn_hard_1.png"),
                playBacker = PopupBuilder.LoadSprite(Buttons + "btn_hard_2.png"),
                playLevelTextColor = new Color32(0x7A, 0x1C, 0x0E, 0xFF),
                playTabTextColor = new Color32(0xB3, 0x2A, 0x1A, 0xFF),
                playTitleOutline = GameFontSetup.OutlineRed(),
                hudFrame = PopupBuilder.LoadSprite(Frames + "khung_hard1.png"),
                hudTabOutline = GameFontSetup.OutlineRed(),
                skull = PopupBuilder.LoadSprite(Frames + "khung_hard_2.png"),
                banner = WideBanner("khung_hard_3"),
            },
            new DifficultyStyle
            {
                difficulty = LevelDifficulty.VeryHard,
                label = "Very Hard",
                playFace = PopupBuilder.LoadSprite(Buttons + "btn_very_hard_1.png"),
                playBacker = PopupBuilder.LoadSprite(Buttons + "btn_very_hard_2.png"),
                playLevelTextColor = new Color32(0x3A, 0x1A, 0x7A, 0xFF),
                playTabTextColor = new Color32(0x4A, 0x22, 0x9A, 0xFF),
                playTitleOutline = GameFontSetup.OutlinePurple(),
                hudFrame = PopupBuilder.LoadSprite(Frames + "khung_expert1.png"),
                hudTabOutline = GameFontSetup.OutlinePurple(),
                skull = PopupBuilder.LoadSprite(Frames + "khung_expert_2.png"),
                banner = WideBanner("khung_expert_3"),
            },
        };
        AssetDatabase.CreateAsset(theme, ThemePath);
        AssetDatabase.SaveAssets();
        return theme;
    }

    /// <summary>Theme tạo từ bản trước: bổ sung nền tối nút PLAY và băng chữ dài.</summary>
    private static void UpgradeTheme(DifficultyTheme theme)
    {
        foreach (DifficultyStyle s in theme.styles)
        {
            bool hard = s.difficulty == LevelDifficulty.Hard;
            if (s.playBacker == null) s.playBacker = PopupBuilder.LoadSprite(Buttons + (hard ? "btn_hard_2.png" : "btn_very_hard_2.png"));
            string wide = hard ? "khung_hard_3" : "khung_expert_3";
            if (s.banner == null || !s.banner.name.EndsWith("_wide")) s.banner = WideBanner(wide);
        }
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
    }

    // ---------- Nút PLAY ----------

    /// <summary>HardFrame: khung kem có tab, thân trùng thân nút PLAY (tab nhô lên trên); chữ mức khó trên tab.</summary>
    internal static void BuildPlayParts(PlayButtonView play, DifficultyTheme theme)
    {
        RectTransform root = (RectTransform)play.transform;
        PopupBuilder.DestroyChild(root, "HardFrame");

        Sprite frameSprite = theme.playFrame;
        Vector2 size = frameSprite != null ? frameSprite.rect.size * PlayFrameScale : new Vector2(525, 278);
        RectTransform frame = PopupBuilder.NewUI("HardFrame", root);
        frame.SetAsFirstSibling(); // dưới mặt nút và chữ
        frame.sizeDelta = size;
        frame.anchoredPosition = new Vector2(0f, (size.y - 225f) * 0.5f);
        Image img = frame.gameObject.AddComponent<Image>();
        img.sprite = frameSprite;
        img.raycastTarget = false;
        frame.gameObject.AddComponent<ButtonPressIgnore>();

        TMP_Text tab = PopupBuilder.GameText(frame, "Tab", "Very Hard", new Vector2(0f, size.y * 0.5f - PlayTabHeight * PlayFrameScale * 0.5f - 2f),
            new Vector2(size.x * 0.6f, 50f), 40f, Color.white);
        frame.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(play);
        so.FindProperty("theme").objectReferenceValue = theme;
        so.FindProperty("frame").objectReferenceValue = root.GetComponent<Image>();
        so.FindProperty("fill").objectReferenceValue = root.Find("Fill").GetComponent<Image>();
        so.FindProperty("hardFrame").objectReferenceValue = frame.gameObject;
        so.FindProperty("tabText").objectReferenceValue = tab;
        Transform playLabel = root.Find("Play");
        so.FindProperty("playText").objectReferenceValue = playLabel != null ? playLabel.GetComponent<TMP_Text>() : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Theme cũ (tạo trước khi có trường này): bổ sung viền chữ PLAY
        foreach (DifficultyStyle s in theme.styles)
        {
            if (s.playTitleOutline != null) continue;
            s.playTitleOutline = s.difficulty == LevelDifficulty.Hard ? GameFontSetup.OutlineRed() : GameFontSetup.OutlinePurple();
            EditorUtility.SetDirty(theme);
        }
    }

    // ---------- HUD ----------

    /// <summary>khung_hard1 / khung_expert1 (298x173, tab ~14 % chiều cao) phủ quanh LevelFrame, tab nhô lên trên.</summary>
    private static void BuildHudFrame(LevelHUD hud, RectTransform levelFrame, DifficultyTheme theme)
    {
        PopupBuilder.DestroyChild(levelFrame, "HardFrame");
        Vector2 body = levelFrame.rect.size + new Vector2(24f, 16f);
        float tabRatio = 0.14f;
        Vector2 size = new Vector2(body.x, body.y / (1f - tabRatio));

        RectTransform frame = PopupBuilder.NewUI("HardFrame", levelFrame);
        frame.SetAsFirstSibling();
        frame.sizeDelta = size;
        frame.anchoredPosition = new Vector2(0f, (size.y - body.y) * 0.5f);
        Image img = frame.gameObject.AddComponent<Image>();
        img.sprite = theme.Get(LevelDifficulty.VeryHard)?.hudFrame;
        img.raycastTarget = false;
        TMP_Text tab = PopupBuilder.GameText(frame, "Tab", "Very hard", new Vector2(0f, size.y * 0.5f - size.y * tabRatio * 0.5f),
            new Vector2(size.x * 0.5f, size.y * tabRatio + 6f), 30f, Color.white);
        tab.fontSharedMaterial = GameFontSetup.OutlinePurple();
        frame.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("theme").objectReferenceValue = theme;
        so.FindProperty("hardFrame").objectReferenceValue = img;
        so.FindProperty("tabText").objectReferenceValue = tab;
        string rounded = Frames + "rounded_white.png";
        PopupBuilder.EnsureSliced(rounded, new Vector4(36, 36, 36, 36));
        so.FindProperty("background").objectReferenceValue = levelFrame.GetComponent<Image>();
        so.FindProperty("roundedBackground").objectReferenceValue = PopupBuilder.LoadSprite(rounded);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Hiệu ứng vào màn ----------

    /// <summary>Phủ cả Canvas_HUD (ngoài safe area để nền tối kín màn), vẽ trên cùng.</summary>
    private static void BuildIntro(Transform hudCanvas, DifficultyTheme theme)
    {
        PopupBuilder.DestroyChild(hudCanvas, "DifficultyIntro");
        RectTransform root = PopupBuilder.Stretch(PopupBuilder.NewUI("DifficultyIntro", hudCanvas));
        root.SetAsLastSibling();
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        RectTransform dimRt = PopupBuilder.Stretch(PopupBuilder.NewUI("Dim", root));
        Image dim = dimRt.gameObject.AddComponent<Image>();
        dim.color = new Color(0.03f, 0.01f, 0.1f, 0f);

        // Dải sọc chéo (xuống phải) sau băng chữ, băng chữ (lên phải) đè lên, đầu lâu trên băng
        Image tape = PopupBuilder.Icon(root, "Tape", Frames + "khung_expert_4.png", 1350);
        tape.rectTransform.sizeDelta = theme.hazardTape != null ? theme.hazardTape.rect.size * (1350f / theme.hazardTape.rect.width) : new Vector2(1350, 553);
        tape.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        Image banner = PopupBuilder.Icon(root, "Banner", Frames + "khung_expert_3.png", 1000);
        banner.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        Image skull = PopupBuilder.Icon(root, "Skull", Frames + "khung_expert_2.png", 300);
        skull.rectTransform.anchoredPosition = new Vector2(0f, 190f);

        DifficultyIntro intro = root.gameObject.AddComponent<DifficultyIntro>();
        SerializedObject so = new SerializedObject(intro);
        so.FindProperty("theme").objectReferenceValue = theme;
        so.FindProperty("gridSpawner").objectReferenceValue = Object.FindFirstObjectByType<GridSpawner>(FindObjectsInactive.Include);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("dim").objectReferenceValue = dim;
        so.FindProperty("tape").objectReferenceValue = tape.rectTransform;
        so.FindProperty("banner").objectReferenceValue = banner;
        so.FindProperty("skull").objectReferenceValue = skull;
        so.FindProperty("offscreen").vector2Value = new Vector2(-2200f, 900f); // băng dài ~2000: xuất phát hẳn ngoài màn
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
