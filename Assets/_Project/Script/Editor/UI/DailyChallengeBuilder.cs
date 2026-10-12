using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daily Challenge (Tools → PawBlock → Builders → Build Daily Challenge, chạy lại được):
/// - Configs/DailyChallengeDatabase.asset: quà 3 mốc nhập từ 10_Data_Configs/DailyChallengeData.txt của bản gốc
///   (cột ILive(m) = phút năng lượng vô hạn, Hint, Ice = Freeze, Reveal), hộp quà hop_qua_1_1 / 2_1 / 3_1.
///   Đã có quà thì giữ nguyên (chỉnh tay trong Inspector).
/// - DailyChallengeManager cùng LevelManager.
/// - Canvas_Menu/Panels: "DailyPanel" thay "ProgressPanel" (tab thứ 3), MainMenuNavigator trỏ panel mới.
/// - Tab thứ 3 đổi thành "Daily" (icon_daily) + DailyTabBadge (ổ khóa btn_lock2 / chấm đỏ).
/// Vị trí theo HomeUI/.../DailyChallenge của bản gốc (canvas 1080 x 1920, tính từ mép trên): tên tháng +5, cúp -109,
/// nút tháng ±391 / -165, thanh tiến trình 706 x 72 ở -455, hộp quà 170 x 170 ở -394, hàng thứ -700, lịch 1050 x 554 ở -770,
/// nút Play cách đáy 452. Màu nền 6D5CFF, vùng đầu trang 543BCF.
/// </summary>
public static class DailyChallengeBuilder
{
    private const string UI = PopupBuilder.UI;
    private const string Grid = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/";
    private const string Glow = "Assets/_Project/GameAssets/05_VFX_Particles/Light_Glow/tia_sang.png";
    private const string CsvPath = "Assets/_Project/GameAssets/10_Data_Configs/DailyChallengeData.txt";
    private const string DatabasePath = GameConfigSetup.Folder + "DailyChallengeDatabase.asset";
    private const string RewardDatabasePath = GameConfigSetup.Folder + "DailyRewardDatabase.asset";
    public const string PanelName = "DailyPanel";

    private static readonly Color PanelColor = new Color(0.427f, 0.361f, 1f);
    private static readonly Color HeaderColor = new Color(0.329f, 0.231f, 0.812f);
    private static readonly string[] Weekdays = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
    private static readonly string[] GiftBoxes = { UI + "Shop_Packs/hop_qua_1_1.png", UI + "Shop_Packs/hop_qua_2_1.png", UI + "Shop_Packs/hop_qua_3_1.png" };

    private const float BarX = -2.3f, BarTop = -455f;
    private static readonly Vector2 BarSize = new Vector2(706f, 72f);
    private static readonly Vector2 FillInset = new Vector2(20f, 24f); // Track / vùng chạy nhỏ hơn khung
    private const float GiftTop = -394f, GiftSize = 170f;

    [MenuItem("Tools/PawBlock/Builders/Build Daily Challenge", priority = 115)]
    public static void Build()
    {
        DailyChallengeDatabase db = EnsureDatabase();

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null)
        {
            DailyChallengeManager manager = lm.GetComponent<DailyChallengeManager>();
            if (manager == null) manager = Undo.AddComponent<DailyChallengeManager>(lm.gameObject);
            PopupBuilder.Assign(manager, "database", db);
        }

        GameObject canvasGo = PopupBuilder.FindRoot("Canvas_Menu");
        Transform panels = canvasGo != null ? canvasGo.transform.Find("Panels") : null;
        MainMenuNavigator nav = canvasGo != null ? canvasGo.GetComponent<MainMenuNavigator>() : null;
        if (panels == null || nav == null)
        {
            Debug.LogError("[DailyChallengeBuilder] Không thấy Canvas_Menu/Panels + MainMenuNavigator. Chạy Build Main Menu trước.");
            return;
        }

        // Panel tab 3: thay đúng chỗ ProgressPanel / DailyPanel cũ
        Transform old = panels.Find(PanelName) ?? panels.Find("ProgressPanel");
        int index = old != null ? old.GetSiblingIndex() : panels.childCount;
        Vector2 pos = old != null ? ((RectTransform)old).anchoredPosition : Vector2.zero;
        PopupBuilder.DestroyChild(panels, "ProgressPanel");
        PopupBuilder.DestroyChild(panels, PanelName);
        RectTransform panel = BuildPanel(panels);
        panel.SetSiblingIndex(index);
        panel.anchoredPosition = pos;

        SerializedObject so = new SerializedObject(nav);
        SerializedProperty panelsProp = so.FindProperty("panels");
        if (panelsProp.arraySize > MainMenuNavigator.DailyTab)
            panelsProp.GetArrayElementAtIndex(MainMenuNavigator.DailyTab).objectReferenceValue = panel;
        SerializedProperty tabsProp = so.FindProperty("tabs");
        MenuTabButton tab = tabsProp.arraySize > MainMenuNavigator.DailyTab
            ? tabsProp.GetArrayElementAtIndex(MainMenuNavigator.DailyTab).objectReferenceValue as MenuTabButton : null;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (tab != null) SetupTab(tab.transform);

        Canvas.ForceUpdateCanvases();
        nav.LayoutInEditor();
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DailyChallengeBuilder] Đã dựng tab Daily Challenge ({db.gifts.Count} mốc quà, mở từ level {db.unlockLevel}).");
    }

    // ---------- Database ----------

    private static DailyChallengeDatabase EnsureDatabase()
    {
        DailyChallengeDatabase db = AssetDatabase.LoadAssetAtPath<DailyChallengeDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DailyChallengeDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        if (db.gifts.Count == 0)
        {
            db.gifts = ImportGifts();
            EditorUtility.SetDirty(db);
        }
        return db;
    }

    /// <summary>"#,Data,ILive(m),Hint,Ice,Reveal": dòng thứ i -> mốc 7 x i.</summary>
    private static List<DailyChallengeGift> ImportGifts()
    {
        List<DailyChallengeGift> gifts = new List<DailyChallengeGift>();
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
        if (csv == null)
        {
            Debug.LogWarning($"[DailyChallengeBuilder] Không thấy {CsvPath}, quà để trống.");
            return gifts;
        }
        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Trim().Split(',');
            if (cols.Length < 6 || !int.TryParse(cols[0], out int row)) continue;
            DailyChallengeGift gift = new DailyChallengeGift
            {
                milestone = 7 * row,
                box = PopupBuilder.LoadSprite(GiftBoxes[Mathf.Clamp(row - 1, 0, GiftBoxes.Length - 1)])
            };
            AddItem(gift, DailyRewardKind.InfiniteEnergy, BoosterType.Hint, cols[2]);
            AddItem(gift, DailyRewardKind.Booster, BoosterType.Hint, cols[3]);
            AddItem(gift, DailyRewardKind.Booster, BoosterType.Freeze, cols[4]);
            AddItem(gift, DailyRewardKind.Booster, BoosterType.Reveal, cols[5]);
            gifts.Add(gift);
        }
        return gifts;
    }

    private static void AddItem(DailyChallengeGift gift, DailyRewardKind kind, BoosterType booster, string value)
    {
        if (!int.TryParse(value.Trim(), out int amount) || amount <= 0) return;
        gift.items.Add(new DailyRewardItem { kind = kind, booster = booster, amount = amount });
    }

    // ---------- Panel ----------

    /// <summary>Panel tab Daily (MainMenuBuilder cũng gọi khi dựng lại cả Menu).</summary>
    internal static RectTransform BuildPanel(Transform parent)
    {
        RectTransform panel = PopupBuilder.Stretch(PopupBuilder.NewUI(PanelName, parent));
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        panel.gameObject.AddComponent<RectMask2D>(); // nội dung không đè panel bên cạnh khi trượt

        // Nền đầu trang tối hơn + dải vàng ngăn với lịch (nằm ngoài vùng an toàn để phủ cả tai thỏ)
        RectTransform header = Top(PopupBuilder.NewUI("WeekdayBG", panel), 0f, -217.8f, 855f);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.sizeDelta = new Vector2(0f, 855f);
        header.pivot = new Vector2(0.5f, 0.5f);
        Raycastless(header.gameObject.AddComponent<Image>()).color = HeaderColor;
        RectTransform line = PopupBuilder.NewUI("WeekdayLine", panel);
        line.anchorMin = new Vector2(0f, 1f);
        line.anchorMax = new Vector2(1f, 1f);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = new Vector2(0f, -643.1f);
        line.sizeDelta = new Vector2(0f, 33f);
        Image lineImg = Raycastless(line.gameObject.AddComponent<Image>());
        lineImg.sprite = PopupBuilder.LoadSprite(UI + "Backgrounds/daily_bg1.png");

        RectTransform zone = UIZoneBuilder.Ensure(panel, true, false, false, false);
        DailyChallengePanel ui = panel.gameObject.AddComponent<DailyChallengePanel>();
        SerializedObject so = new SerializedObject(ui);

        // Tháng + cúp + nút tháng
        TMP_Text month = PopupBuilder.GameText(zone, "MonthLabel", "October 2026", Vector2.zero, new Vector2(640f, 100f), 64f, Color.white);
        month.fontSharedMaterial = GameFontSetup.OutlinePurple();
        Top(month.rectTransform, 0f, -45f, 100f);
        Image cup = PopupBuilder.Icon(zone, "Cup", UI + "Icons/icon_cup.png", 300f);
        Top(cup.rectTransform, 0f, -256f, 295f).sizeDelta = new Vector2(372f, 295f);
        Button prev = Arrow(zone, "PrevMonthButton", -391f, true);
        Button next = Arrow(zone, "NextMonthButton", 391f, false);

        // Thanh tiến trình + mốc + hộp quà
        RectTransform bar = Top(PopupBuilder.NewUI("ProgressBar", zone), BarX, BarTop - BarSize.y * 0.5f, BarSize.y);
        bar.sizeDelta = BarSize;
        Image track = Raycastless(Inset(PopupBuilder.NewUI("Track", bar)).gameObject.AddComponent<Image>());
        track.sprite = PopupBuilder.LoadSprite(Grid + "daily_ttrinh_3.png");
        RectTransform area = Inset(PopupBuilder.NewUI("FillArea", bar));
        PopupBuilder.EnsureSliced(Grid + "daily_ttrinh_1.png", new Vector4(23, 0, 23, 0));
        RectTransform fillRt = PopupBuilder.NewUI("Fill", area);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.sizeDelta = Vector2.zero;
        Image fill = Raycastless(fillRt.gameObject.AddComponent<Image>());
        fill.sprite = PopupBuilder.LoadSprite(Grid + "daily_ttrinh_1.png");
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 46f / (BarSize.y - FillInset.y); // đầu bo theo chiều cao thật (sprite cao 46)
        Image frame = Raycastless(PopupBuilder.Stretch(PopupBuilder.NewUI("Frame", bar)).gameObject.AddComponent<Image>());
        frame.sprite = PopupBuilder.LoadSprite(Grid + "daily_ttrinh_2.png");

        float areaWidth = BarSize.x - FillInset.x;
        DailyGiftSlot[] gifts = new DailyGiftSlot[DailyChallengeModel.Milestones.Length];
        for (int i = 0; i < gifts.Length; i++)
        {
            int milestone = DailyChallengeModel.Milestones[i];
            float x = milestone / (float)DailyChallengeModel.MaxMilestone * areaWidth;
            Marker(area, milestone, x);
            gifts[i] = Gift(zone, milestone, BarX - areaWidth * 0.5f + x, GiftBoxes[i]);
        }

        TMP_Text plusOne = PopupBuilder.GameText(area, "PlusOne", "+1", Vector2.zero, new Vector2(120f, 80f), 64f, new Color(1f, 0.9f, 0.3f));
        plusOne.fontSharedMaterial = GameFontSetup.OutlineBrown();
        plusOne.rectTransform.anchorMin = plusOne.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        plusOne.gameObject.SetActive(false); // chỉ hiện lúc diễn "+1"

        // Hàng thứ + lịch
        RectTransform week = Top(PopupBuilder.NewUI("WeekdayRow", zone), 0f, -730f, 60f);
        week.sizeDelta = new Vector2(1050f, 60f);
        HorizontalLayoutGroup wl = week.gameObject.AddComponent<HorizontalLayoutGroup>();
        wl.childControlWidth = wl.childControlHeight = true;
        wl.childForceExpandWidth = wl.childForceExpandHeight = true;
        foreach (string d in Weekdays)
        {
            TMP_Text t = PopupBuilder.GameText(week, d, d, Vector2.zero, new Vector2(150f, 60f), 48f, Color.white);
            t.fontSharedMaterial = GameFontSetup.OutlinePurple();
        }

        RectTransform grid = PopupBuilder.NewUI("CalendarGrid", zone);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = new Vector2(0f, -770f);
        grid.sizeDelta = new Vector2(1050f, 554f);
        GridLayoutGroup gl = grid.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(150f, 92f);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 7;
        gl.childAlignment = TextAnchor.UpperCenter;
        DailyDayCell template = DayCell(grid);

        // Nút Play (xanh) / Play xem quảng cáo (vàng) cách đáy 452 như gốc
        Button play = PlayButton(zone, "PlayButton", UI + "Buttons/btn_green_4.png", UI + "Buttons/btn_green_3.png", false);
        Button playAds = PlayButton(zone, "PlayWithAdsButton", UI + "Buttons/btn_yellow_6.png", UI + "Buttons/btn_yellow_5.png", true);
        PopupBuilder.GrayWhenDisabled(play);
        PopupBuilder.GrayWhenDisabled(playAds);

        // Lớp khóa: phủ lịch + nút, chặn chạm
        RectTransform locked = PopupBuilder.NewUI("LockedOverlay", panel);
        locked.anchorMin = Vector2.zero;
        locked.anchorMax = Vector2.one;
        locked.offsetMin = new Vector2(0f, 200f); // trên thanh tab
        locked.offsetMax = new Vector2(0f, -660f);
        locked.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.35f, 0.75f);
        Image lockIcon = PopupBuilder.Icon(locked, "Lock", UI + "Buttons/btn_lock2.png", 180f);
        lockIcon.rectTransform.anchoredPosition = new Vector2(0f, 90f);
        TMP_Text lockText = PopupBuilder.GameText(locked, "Text", "Reach Level 21 to unlock", new Vector2(0f, -60f), new Vector2(900f, 90f), 60f, Color.white);
        lockText.fontSharedMaterial = GameFontSetup.OutlinePurple();

        DailyGiftBubble bubble = BuildBubble(panel);

        so.FindProperty("monthLabel").objectReferenceValue = month;
        so.FindProperty("prevMonthButton").objectReferenceValue = prev;
        so.FindProperty("nextMonthButton").objectReferenceValue = next;
        so.FindProperty("progressArea").objectReferenceValue = area;
        so.FindProperty("progressFill").objectReferenceValue = fill;
        so.FindProperty("plusOneText").objectReferenceValue = plusOne;
        SerializedProperty gp = so.FindProperty("gifts");
        gp.arraySize = gifts.Length;
        for (int i = 0; i < gifts.Length; i++) gp.GetArrayElementAtIndex(i).objectReferenceValue = gifts[i];
        so.FindProperty("giftBubble").objectReferenceValue = bubble;
        so.FindProperty("rewardIcons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DailyRewardDatabase>(RewardDatabasePath);
        so.FindProperty("grid").objectReferenceValue = grid;
        so.FindProperty("cellTemplate").objectReferenceValue = template;
        so.FindProperty("frameDone").objectReferenceValue = PopupBuilder.LoadSprite(UI + "Frames_Borders/daily_khung_1.png");
        so.FindProperty("frameMissed").objectReferenceValue = PopupBuilder.LoadSprite(UI + "Frames_Borders/daily_khung_2.png");
        so.FindProperty("frameToday").objectReferenceValue = PopupBuilder.LoadSprite(UI + "Frames_Borders/daily_khung_3.png");
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("playAdsButton").objectReferenceValue = playAds;
        so.FindProperty("lockedOverlay").objectReferenceValue = locked.gameObject;
        so.FindProperty("lockedText").objectReferenceValue = lockText;
        so.ApplyModifiedPropertiesWithoutUndo();

        bubble.transform.SetAsLastSibling();
        return panel;
    }

    private static Button Arrow(RectTransform zone, string name, float x, bool flip)
    {
        Image img = PopupBuilder.Icon(zone, name, UI + "Buttons/btn_next.png", 80f);
        img.raycastTarget = true;
        RectTransform rt = Top(img.rectTransform, x, -205f, 80f);
        rt.sizeDelta = new Vector2(49f, 80f);
        if (flip) rt.localScale = new Vector3(-1f, 1f, 1f);
        // Vùng bấm to hơn mũi tên (Image con trong suốt 160 x 160 như gốc)
        RectTransform hit = PopupBuilder.NewUI("Hit", rt);
        hit.sizeDelta = new Vector2(160f, 160f);
        hit.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Button b = img.gameObject.AddComponent<Button>();
        ColorBlock c = b.colors;
        c.disabledColor = new Color(1f, 1f, 1f, 0.3f);
        b.colors = c;
        return b;
    }

    private static void Marker(RectTransform area, int milestone, float x)
    {
        Image m = PopupBuilder.Icon(area, $"Marker{milestone}", Grid + "daily_ttrinh_4.png", 82f);
        RectTransform rt = m.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(82f, 57f);
        // Treo dưới thanh; hộp quà (đáy -52 so với tâm thanh) vẽ đè phần trên nên số đặt ở nửa dưới cho khỏi bị che
        rt.anchoredPosition = new Vector2(x, -66f);
        TMP_Text num = PopupBuilder.GameText(rt, "Num", milestone.ToString(), new Vector2(0f, -8f), new Vector2(90f, 50f), 40f, Color.white);
        num.fontSharedMaterial = GameFontSetup.OutlineBrown();
    }

    private static DailyGiftSlot Gift(RectTransform zone, int milestone, float x, string box)
    {
        RectTransform rt = Top(PopupBuilder.NewUI($"Gift{milestone}", zone), x, GiftTop - GiftSize * 0.5f, GiftSize);
        rt.sizeDelta = new Vector2(GiftSize, GiftSize);
        Image hit = rt.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        Image glow = PopupBuilder.Icon(rt, "Glow", Glow, 215f);
        glow.gameObject.SetActive(false);
        Image icon = PopupBuilder.Icon(rt, "Icon", box, 105f);
        icon.rectTransform.sizeDelta = new Vector2(96f, 105f);
        Image tick = PopupBuilder.Icon(rt, "Tick", UI + "Icons/icon_tick.png", 51f);
        tick.rectTransform.sizeDelta = new Vector2(51f, 36f);
        tick.rectTransform.anchoredPosition = new Vector2(34f, -34f);
        tick.gameObject.SetActive(false);

        DailyGiftSlot slot = rt.gameObject.AddComponent<DailyGiftSlot>();
        SerializedObject so = new SerializedObject(slot);
        so.FindProperty("milestone").intValue = milestone;
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("glow").objectReferenceValue = glow.rectTransform;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("tick").objectReferenceValue = tick.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    private static DailyDayCell DayCell(RectTransform grid)
    {
        RectTransform rt = PopupBuilder.NewUI("DayCellTemplate", grid);
        Image hit = rt.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        Image frame = PopupBuilder.Icon(rt, "Frame", UI + "Frames_Borders/daily_khung_3.png", 85f);
        TMP_Text label = PopupBuilder.GameText(rt, "Label", "0", Vector2.zero, new Vector2(120f, 85f), 52f, Color.white);
        label.fontSharedMaterial = GameFontSetup.OutlinePurple();

        DailyDayCell cell = rt.gameObject.AddComponent<DailyDayCell>();
        SerializedObject so = new SerializedObject(cell);
        so.FindProperty("frame").objectReferenceValue = frame;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("button").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();
        rt.gameObject.SetActive(false);
        return cell;
    }

    private static Button PlayButton(RectTransform zone, string name, string backer, string face, bool ads)
    {
        RectTransform faceRt;
        Button b = PopupBuilder.FramedButton(zone, name, backer, face, Vector2.zero, new Vector2(560f, 224f), out faceRt);
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 452f);
        Vector2 labelPos = ads ? new Vector2(56f, 4f) : new Vector2(0f, 4f);
        TMP_Text label = PopupBuilder.GameText(faceRt, "Label", "Play", labelPos, new Vector2(320f, 110f), 91f, Color.white);
        label.fontSharedMaterial = ads ? GameFontSetup.OutlineBrown() : GameFontSetup.OutlineGreen();
        if (ads)
        {
            Image video = PopupBuilder.Icon(faceRt, "Video", UI + "Buttons/btn_video2.png", 104f);
            video.rectTransform.anchoredPosition = new Vector2(-128f, 0f);
            b.gameObject.SetActive(false);
        }
        return b;
    }

    /// <summary>Bong thoại quà (GiftMess gốc): khung dialog.png (9-slice) + mũi ArrowDialog + hàng icon. Không có lớp chặn: chạm ngoài thì tự ẩn.</summary>
    private static DailyGiftBubble BuildBubble(RectTransform panel)
    {
        RectTransform root = PopupBuilder.Stretch(PopupBuilder.NewUI("GiftBubble", panel));

        PopupBuilder.EnsureSliced(UI + "Popups_Dialogs/dialog.png", new Vector4(50, 50, 50, 50));
        RectTransform bubble = PopupBuilder.NewUI("Bubble", root);
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.pivot = new Vector2(0.5f, 1f);
        Image bg = bubble.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(UI + "Popups_Dialogs/dialog.png");
        bg.type = Image.Type.Sliced;
        VerticalLayoutGroup vl = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(36, 36, 22, 26);
        vl.spacing = 4f;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = vl.childForceExpandHeight = false;
        ContentSizeFitter fit = bubble.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image arrow = PopupBuilder.Icon(bubble, "Arrow", UI + "Popups_Dialogs/ArrowDialog 1.png", 43f);
        arrow.rectTransform.sizeDelta = new Vector2(43f, 39f);
        arrow.GetComponent<LayoutElement>().ignoreLayout = true; // Icon đã gắn LayoutElement: thêm cái thứ 2 thì layout vẫn xếp mũi tên
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        arrow.rectTransform.anchoredPosition = new Vector2(0f, 14f);

        TMP_Text title = PopupBuilder.GameText(bubble, "Title", "Win 7 days", Vector2.zero, new Vector2(360f, 56f), 44f, new Color32(0x5A, 0x2A, 0x14, 0xFF));
        PopupBuilder.Fit(title);

        RectTransform row = PopupBuilder.NewUI("Items", bubble);
        HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 18f;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;

        Image item = PopupBuilder.Icon(row, "ItemTemplate", null, 100f);
        TMP_Text amount = PopupBuilder.GameText(item.rectTransform, "Amount", "x1", new Vector2(0f, -42f), new Vector2(120f, 50f), 40f, Color.white);
        amount.fontSharedMaterial = GameFontSetup.OutlineBrown();

        DailyGiftBubble ui = root.gameObject.AddComponent<DailyGiftBubble>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("bubble").objectReferenceValue = bubble;
        so.FindProperty("arrow").objectReferenceValue = arrow.rectTransform;
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("itemRow").objectReferenceValue = row;
        so.FindProperty("itemIconTemplate").objectReferenceValue = item;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false);
        return ui;
    }

    // ---------- Tab ----------

    /// <summary>Tab thứ 3: chữ "Daily", icon_daily, ổ khóa + chấm đỏ (DailyTabBadge).</summary>
    internal static void SetupTab(Transform tab)
    {
        Undo.RecordObject(tab.gameObject, "Build Daily Challenge");
        tab.gameObject.name = "Tab_Daily";
        Transform icon = tab.Find("Icon");
        if (icon != null)
        {
            Image img = icon.GetComponent<Image>();
            Undo.RecordObject(img, "Build Daily Challenge");
            img.sprite = PopupBuilder.LoadSprite(UI + "Icons/icon_daily.png");
        }
        TMP_Text label = tab.Find("Label") != null ? tab.Find("Label").GetComponent<TMP_Text>() : null;
        if (label != null)
        {
            Undo.RecordObject(label, "Build Daily Challenge");
            label.text = "Daily";
        }

        if (icon == null) return;
        PopupBuilder.DestroyChild(icon, "Lock");
        PopupBuilder.DestroyChild(icon, "Badge");
        Image lockImg = PopupBuilder.Icon(icon, "Lock", UI + "Buttons/btn_lock2.png", 70f);
        lockImg.rectTransform.anchoredPosition = new Vector2(42f, -30f);
        Image badge = PopupBuilder.Icon(icon, "Badge", UI + "Icons/badge_red.png", 44f);
        badge.rectTransform.anchoredPosition = new Vector2(50f, 50f);
        DailyTabBadge tb = tab.GetComponent<DailyTabBadge>();
        if (tb == null) tb = Undo.AddComponent<DailyTabBadge>(tab.gameObject);
        PopupBuilder.Assign(tb, "lockIcon", lockImg.gameObject);
        PopupBuilder.Assign(tb, "todayBadge", badge.gameObject);
    }

    // ---------- Tiện ích ----------

    /// <summary>Neo giữa mép trên, tâm ở (x, y) tính từ mép trên.</summary>
    private static RectTransform Top(RectTransform rt, float x, float y, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        if (rt.sizeDelta.y <= 0f) rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        return rt;
    }

    /// <summary>Vùng trong viền khung thanh tiến trình.</summary>
    private static RectTransform Inset(RectTransform rt)
    {
        PopupBuilder.Stretch(rt);
        rt.sizeDelta = -FillInset;
        return rt;
    }

    private static Image Raycastless(Image img)
    {
        img.raycastTarget = false;
        return img;
    }
}
