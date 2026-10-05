using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng phần HUD của booster (Giai đoạn 4) trong scene đang mở:
/// - 3 nút Canvas_HUD/DownPanel/Booster/Booster1..3: icon, số lượt, dấu "+", ổ khóa "LV n", quầng sáng khi bật
/// - Banner chọn số của Reveal, lớp băng của Khiên trên hàng tim
/// - BoosterManager trên Manager, trỏ tới BoosterDatabase
/// Chạy lại được (xóa phần đã dựng rồi dựng mới). Popup mua nằm ở Build Popups.
/// </summary>
public static class BoosterBuilder
{
    private const string Boosters = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/";
    private const string Ice = "Assets/_Project/GameAssets/05_VFX_Particles/Ice_Effects/";
    private const string DatabasePath = "Assets/_Project/GameAssets/10_Data_Configs/BoosterDatabase.asset";
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
        BuildButton(canvas.Find("DownPanel/Booster/Booster2"), BoosterType.Shield, db);
        BuildButton(canvas.Find("DownPanel/Booster/Booster3"), BoosterType.Reveal, db);
        BuildRevealBanner(canvas);
        BuildShieldIndicator(canvas.Find("TopPanel/LevelFrame"));

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

    /// <summary>Khung băng ice_effect_2 phủ hàng tim (sau Hearts để vẽ đè lên tim).</summary>
    private static void BuildShieldIndicator(Transform levelFrame)
    {
        if (levelFrame == null) return;
        PopupBuilder.DestroyChild(levelFrame, "Shield");
        RectTransform hearts = levelFrame.Find("Hearts") as RectTransform;

        RectTransform rt = PopupBuilder.NewUI("Shield", levelFrame);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = PopupBuilder.LoadSprite(Ice + "ice_effect_2.png");
        img.preserveAspect = true;
        if (hearts != null)
        {
            rt.anchorMin = hearts.anchorMin;
            rt.anchorMax = hearts.anchorMax;
            rt.pivot = hearts.pivot;
            rt.anchoredPosition = hearts.anchoredPosition;
            rt.SetSiblingIndex(hearts.GetSiblingIndex() + 1);
        }
        rt.sizeDelta = new Vector2(380, 112);
        rt.gameObject.AddComponent<ShieldIndicator>();
    }
}
