using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng phần HUD của booster (Giai đoạn 4) trong scene đang mở:
/// - 3 nút Canvas_HUD/DownPanel/Booster/Booster1..3: icon, số lượt, dấu "+", ổ khóa "LV n", quầng sáng khi bật
/// - Banner chọn số của Reveal, đồng hồ đếm ngược dưới chữ LEVEL + lớp băng khi đóng băng
/// - BoosterManager trên Manager, trỏ tới BoosterDatabase
/// Chạy lại được (xóa phần đã dựng rồi dựng mới). Popup mua nằm ở Build Popups.
/// </summary>
public static class BoosterBuilder
{
    private const string Boosters = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/";
    private const string ClockIcon = "Assets/_Project/GameAssets/04_UI_Graphics/Buttons/btn_clock.png";
    private const string Ice = "Assets/_Project/GameAssets/05_VFX_Particles/Ice_Effects/";
    private const string DatabasePath = GameConfigSetup.Folder + "BoosterDatabase.asset";
    private static readonly string[] ButtonParts = { "Glow", "Icon", "Count", "Add", "Lock" };

    [MenuItem("Tools/PawBlock/Builders/Build Boosters", priority = 101)]
    public static void Build()
    {
        GameObject canvasGo = PopupBuilder.FindRoot("Canvas_HUD"); // tìm được cả khi HUD đang tắt (đang ở Menu)
        if (canvasGo == null)
        {
            Debug.LogError("[BoosterBuilder] Không tìm thấy Canvas_HUD trong scene.");
            return;
        }
        Transform canvas = PopupBuilder.HudRoot(canvasGo); // trong UIZone nếu đã có

        BoosterDatabase db = AssetDatabase.LoadAssetAtPath<BoosterDatabase>(DatabasePath);
        if (db == null) Debug.LogError($"[BoosterBuilder] Không đọc được {DatabasePath}");

        BuildButton(canvas.Find("DownPanel/Booster/Booster1"), BoosterType.Hint, db);
        BuildButton(canvas.Find("DownPanel/Booster/Booster2"), BoosterType.Freeze, db);
        BuildButton(canvas.Find("DownPanel/Booster/Booster3"), BoosterType.Reveal, db);
        BuildRevealBanner(canvas);
        BuildTimer(canvas.Find("TopPanel/LevelFrame"));
        BuildFreezeIndicator(canvas.Find("TopPanel/LevelFrame"));

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null)
        {
            BoosterManager bm = lm.GetComponent<BoosterManager>();
            if (bm == null) bm = Undo.AddComponent<BoosterManager>(lm.gameObject);
            PopupBuilder.Assign(bm, "database", db);
        }

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[BoosterBuilder] Đã dựng thanh booster, banner Reveal, lớp băng Khiên.");
    }

    private static void BuildButton(Transform button, BoosterType type, BoosterDatabase db)
    {
        if (button == null)
        {
            Debug.LogError($"[BoosterBuilder] Thiếu nút cho {type}");
            return;
        }
        foreach (string part in ButtonParts) PopupBuilder.DestroyChild(button, part);
        if (button.GetComponent<Button>() == null) Undo.AddComponent<Button>(button.gameObject).targetGraphic = button.GetComponent<Image>();

        // Quầng sáng (dưới icon, trên nền nút)
        Image glow = PopupBuilder.Icon(button, "Glow", Boosters + "glow_booster.png", 250);
        glow.gameObject.SetActive(false);

        BoosterData data = db != null ? db.Get(type) : null;
        Image icon = PopupBuilder.Icon(button, "Icon", null, 130);
        if (data != null) icon.sprite = data.icon;
        icon.rectTransform.anchoredPosition = new Vector2(0, 6);

        // Số lượt: chấm xanh góc dưới-phải
        Image count = PopupBuilder.Icon(button, "Count", PopupBuilder.UI + "Buttons/btn_number.png", 76);
        count.rectTransform.anchoredPosition = new Vector2(68, -68);
        TMP_Text countText = PopupBuilder.Text(count.transform, "Text", "3", new Vector2(0, 2), new Vector2(76, 76), 46, Color.white);

        Image add = PopupBuilder.Icon(button, "Add", PopupBuilder.UI + "Buttons/btn_add_0.png", 76);
        add.rectTransform.anchoredPosition = new Vector2(68, -68);

        // Chưa mở: ổ khóa + "LV n"
        RectTransform lockGroup = PopupBuilder.NewUI("Lock", button);
        lockGroup.sizeDelta = new Vector2(208, 208);
        Image lockIcon = PopupBuilder.Icon(lockGroup, "LockIcon", PopupBuilder.UI + "Buttons/btn_lock.png", 90);
        lockIcon.rectTransform.anchoredPosition = new Vector2(0, 14);
        TMP_Text lockText = PopupBuilder.Text(lockGroup, "Level", "LV 5", new Vector2(0, -62), new Vector2(200, 60), 40, Color.white);

        BoosterButton bb = button.GetComponent<BoosterButton>();
        if (bb == null) bb = Undo.AddComponent<BoosterButton>(button.gameObject);
        SerializedObject so = new SerializedObject(bb);
        so.FindProperty("type").intValue = (int)type;
        so.ApplyModifiedPropertiesWithoutUndo();
        PopupBuilder.Assign(bb, "icon", icon);
        PopupBuilder.Assign(bb, "countBadge", count.gameObject);
        PopupBuilder.Assign(bb, "countText", countText);
        PopupBuilder.Assign(bb, "addBadge", add.gameObject);
        PopupBuilder.Assign(bb, "lockGroup", lockGroup.gameObject);
        PopupBuilder.Assign(bb, "lockText", lockText);
        PopupBuilder.Assign(bb, "glow", glow);
    }

    /// <summary>
    /// Banner khung_reveal2 "TAP A NUMBER" + nút X, đè lên thanh booster dưới (lưới lớn chiếm hết khoảng trống
    /// trên / dưới lưới nên không chen vào giữa được; lúc đang chọn số cũng không cần bấm booster khác).
    /// </summary>
    private static void BuildRevealBanner(Transform canvas)
    {
        PopupBuilder.DestroyChild(canvas, "RevealBanner");
        RectTransform root = PopupBuilder.Stretch(PopupBuilder.NewUI("RevealBanner", canvas));
        root.gameObject.AddComponent<CanvasGroup>();
        // Nằm dưới Popups để popup luôn che banner
        Transform popups = canvas.Find("Popups");
        root.SetSiblingIndex(popups != null ? popups.GetSiblingIndex() : canvas.childCount - 1);

        RectTransform content = PopupBuilder.NewUI("Content", root);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0f);
        content.anchoredPosition = new Vector2(0, 190);
        Image bg = content.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(Boosters + "khung_reveal2.png");
        content.sizeDelta = new Vector2(980, 220); // che kín 3 nút booster (208)

        TMP_Text text = PopupBuilder.Text(content, "Text", "TAP A NUMBER TO REVEAL", new Vector2(-50, 0), new Vector2(760, 120), 56, Color.white);
        text.enableAutoSizing = true;
        text.fontSizeMin = 30;
        text.fontSizeMax = 56;
        text.enableWordWrapping = false;

        RectTransform cancel = PopupBuilder.NewUI("CancelButton", content);
        cancel.anchoredPosition = new Vector2(415, 0);
        cancel.sizeDelta = new Vector2(90, 90);
        Image x = cancel.gameObject.AddComponent<Image>();
        x.sprite = PopupBuilder.LoadSprite(Boosters + "revel_X.png");
        x.preserveAspect = true;
        Button b = cancel.gameObject.AddComponent<Button>();
        b.targetGraphic = x;

        RevealBanner banner = root.gameObject.AddComponent<RevealBanner>();
        PopupBuilder.Assign(banner, "content", content);
        PopupBuilder.Assign(banner, "cancelButton", b);
    }

    /// <summary>Chỉ dựng lại đồng hồ + lớp băng (không đụng nút booster) trong scene đang mở.</summary>
    [MenuItem("Tools/PawBlock/Builders/Build Timer HUD", priority = 102)]
    public static void BuildTimerHud()
    {
        GameObject canvasGo = PopupBuilder.FindRoot("Canvas_HUD");
        if (canvasGo == null)
        {
            Debug.LogError("[BoosterBuilder] Không tìm thấy Canvas_HUD trong scene.");
            return;
        }
        Transform levelFrame = PopupBuilder.HudRoot(canvasGo).Find("TopPanel/LevelFrame");
        BuildTimer(levelFrame);
        BuildFreezeIndicator(levelFrame);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
    }

    /// <summary>
    /// Đồng hồ đếm ngược dưới chữ LEVEL (chỗ hàng tim cũ): icon đồng hồ + chữ "m:ss" (TimerUI).
    /// Object cũ "Hearts" (LivesUI, cùng script GUID) được dựng lại thành "Timer".
    /// </summary>
    private static void BuildTimer(Transform levelFrame)
    {
        if (levelFrame == null) return;
        PopupBuilder.DestroyChild(levelFrame, "Hearts");
        PopupBuilder.DestroyChild(levelFrame, "Timer");

        RectTransform rt = PopupBuilder.NewUI("Timer", levelFrame);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 25);
        rt.sizeDelta = new Vector2(320, 100);
        Transform levelText = levelFrame.Find("LevelText");
        if (levelText != null) rt.SetSiblingIndex(levelText.GetSiblingIndex() + 1);

        RectTransform row = PopupBuilder.Row(rt, "Row", Vector2.zero, 90);
        Image icon = PopupBuilder.Icon(row, "Icon", ClockIcon, 84);
        TMP_Text text = PopupBuilder.GameText(row, "Time", "0:00", Vector2.zero, new Vector2(200, 90), 72, Color.white);
        PopupBuilder.Fit(text);
        Material outline = GameFontSetup.OutlinePurple();
        if (outline != null) text.fontSharedMaterial = outline;

        TimerUI ui = rt.gameObject.AddComponent<TimerUI>();
        PopupBuilder.Assign(ui, "icon", icon);
        PopupBuilder.Assign(ui, "timeText", text);
    }

    /// <summary>
    /// Khung băng ice_effect_2 phủ đồng hồ khi đóng băng (sau Timer để vẽ đè lên).
    /// Đã có (kể cả object "Shield" cũ, cùng script GUID) thì giữ nguyên để không mất bông tuyết / viền băng gán tay, chỉ đổi tên + xếp lại.
    /// </summary>
    private static void BuildFreezeIndicator(Transform levelFrame)
    {
        if (levelFrame == null) return;
        RectTransform timer = levelFrame.Find("Timer") as RectTransform;
        Transform existing = levelFrame.Find("Freeze");
        if (existing == null) existing = levelFrame.Find("Shield");

        RectTransform rt;
        if (existing != null)
        {
            rt = (RectTransform)existing;
            Undo.RecordObject(rt.gameObject, "Build Timer HUD");
            rt.gameObject.name = "Freeze";
        }
        else
        {
            rt = PopupBuilder.NewUI("Freeze", levelFrame);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = PopupBuilder.LoadSprite(Ice + "ice_effect_2.png");
            img.preserveAspect = true;
            rt.gameObject.AddComponent<FreezeIndicator>();
        }
        if (timer != null)
        {
            rt.anchorMin = timer.anchorMin;
            rt.anchorMax = timer.anchorMax;
            rt.pivot = timer.pivot;
            rt.anchoredPosition = timer.anchoredPosition;
            rt.SetSiblingIndex(timer.GetSiblingIndex() + 1);
        }
        rt.sizeDelta = new Vector2(380, 112);
    }
}
