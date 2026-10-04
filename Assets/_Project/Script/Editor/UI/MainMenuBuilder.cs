using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng điều hướng Main Menu trong Canvas_Menu của scene đang mở:
/// - Panels: ShopPanel | HomePanel | ProgressPanel (HomePanel có sẵn thì giữ nguyên, chỉ chuyển vào Panels)
/// - AlwayOn/TabBar: thanh tab dưới (home_tab2 nền, home_tab1 tab đang chọn, home_tab3 vạch ngăn)
/// - MainMenuNavigator trên Canvas_Menu
/// - HomePanel/Background (BackgroundCover: phủ kín mọi tỉ lệ màn hình), HomePanel/PlayButton (PLAY / Level n)
/// - HomePanel/UIZone (SafeAreaZone cạnh trên, tránh tai thỏ): Starter + NoAds (bấm -> Shop), TopBar
/// - TopBar: avatar | năng lượng | coin | cài đặt (theo home_demo3 của bản gốc), chỉ hiện ở tab Home
/// - ScreenFlow trên Manager: chuyển Menu <-> màn chơi
/// Chữ dùng font Fredoka Bold + viền (GameFontSetup).
/// Chạy lại được: dựng lại TabBar, TopBar, PlayButton, Starter, NoAds, ShopPanel, ProgressPanel; không đụng phần khác của HomePanel.
/// </summary>
public static class MainMenuBuilder
{
    private const string BG = "Assets/_Project/GameAssets/04_UI_Graphics/Backgrounds/";
    private const string Icons = "Assets/_Project/GameAssets/04_UI_Graphics/Icons/";
    private const string Buttons = "Assets/_Project/GameAssets/04_UI_Graphics/Buttons/";
    private const string Frames = "Assets/_Project/GameAssets/04_UI_Graphics/Frames_Borders/";
    private const string Pill = Frames + "khung_pill_kem.png"; // ô kem bo tròn, 9-slice
    private static readonly Color PillText = new Color32(0x5A, 0x2A, 0x14, 0xFF); // nâu đậm như số trên ô kem bản gốc
    private static readonly Color LevelText = new Color32(0x2E, 0x6B, 0x14, 0xFF);
    private const float BarHeight = 200f;
    private const float SelectedWidth = 408f;  // home_tab1 408x292
    private const float SelectedHeight = 292f;

    private static readonly string[] TabNames = { "Shop", "Home", "Progress" };
    private static readonly string[] TabIcons = { Icons + "icon_shop.png", Icons + "icon_home.png", Icons + "icon_cup.png" };

    [MenuItem("Tools/PawBlock/Build Main Menu")]
    public static void Build()
    {
        GameObject canvasGo = FindInScene("Canvas_Menu");
        if (canvasGo == null)
        {
            Debug.LogError("[MainMenuBuilder] Không tìm thấy Canvas_Menu trong scene.");
            return;
        }
        Transform canvas = canvasGo.transform;

        // Cùng chuẩn với Canvas_HUD; vẽ trên HUD
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Build Main Menu");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;
        }
        Canvas c = canvasGo.GetComponent<Canvas>();
        if (c != null)
        {
            Undo.RecordObject(c, "Build Main Menu");
            c.sortingOrder = 10;
        }

        // ---------- Panels ----------
        Transform panelsT = canvas.Find("Panels");
        RectTransform panels = panelsT != null ? (RectTransform)panelsT : PopupBuilder.Stretch(PopupBuilder.NewUI("Panels", canvas));
        panels.SetSiblingIndex(0);

        Transform home = canvas.Find("HomePanel") ?? panels.Find("HomePanel");
        if (home == null) home = PopupBuilder.Stretch(PopupBuilder.NewUI("HomePanel", panels));
        if (home.parent != panels) Undo.SetTransformParent(home, panels, "Build Main Menu");

        PopupBuilder.DestroyChild(panels, "ShopPanel");
        PopupBuilder.DestroyChild(panels, "ProgressPanel");
        RectTransform shop = ShopBuilder.BuildPanel(panels);
        RectTransform progress = BuildProgressPlaceholder(panels);
        // Nội dung tràn ra ngoài panel (vd. home_bg 2048px) không được đè panel bên cạnh khi trượt
        foreach (Transform p in new[] { shop, home, progress })
        {
            if (p.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(p.gameObject);
        }
        shop.SetSiblingIndex(0);
        home.SetSiblingIndex(1);
        progress.SetSiblingIndex(2);

        // ---------- AlwayOn / TabBar ----------
        Transform alwaysOn = canvas.Find("AlwayOn");
        if (alwaysOn == null) alwaysOn = PopupBuilder.Stretch(PopupBuilder.NewUI("AlwayOn", canvas));
        alwaysOn.SetAsLastSibling();
        // Lớp phủ trong suốt full màn hình không được chặn bấm vào panel bên dưới
        Image alwaysImg = alwaysOn.GetComponent<Image>();
        if (alwaysImg != null)
        {
            Undo.RecordObject(alwaysImg, "Build Main Menu");
            alwaysImg.raycastTarget = false;
        }

        PopupBuilder.DestroyChild(alwaysOn, "TabBar");
        RectTransform bar = PopupBuilder.NewUI("TabBar", alwaysOn);
        // Neo bottom center, rộng 1080 (CanvasScaler khớp chiều ngang nên canvas luôn rộng 1080)
        BottomCenter(bar, 1080f, BarHeight);

        RectTransform barBg = PopupBuilder.Stretch(PopupBuilder.NewUI("Background", bar));
        Image barImg = barBg.gameObject.AddComponent<Image>();
        barImg.sprite = PopupBuilder.LoadSprite(BG + "home_tab2.png"); // dải 16px: kéo ngang giữ nguyên chuyển màu dọc

        RectTransform selected = BottomCenter(PopupBuilder.NewUI("SelectedTab", bar), SelectedWidth, SelectedHeight);
        Image selImg = selected.gameObject.AddComponent<Image>();
        selImg.sprite = PopupBuilder.LoadSprite(BG + "home_tab1.png");
        selImg.raycastTarget = false;

        MenuTabButton[] tabs = new MenuTabButton[TabNames.Length];
        for (int i = 0; i < TabNames.Length; i++) tabs[i] = BuildTab(bar, i);

        // ---------- Home: nền phủ kín, nút PLAY ----------
        Transform homeBg = home.Find("Background");
        if (homeBg != null && homeBg.GetComponent<BackgroundCover>() == null) Undo.AddComponent<BackgroundCover>(homeBg.gameObject);
        PopupBuilder.DestroyChild(home, "PlayButton");
        BuildPlayButton(home);

        // ---------- Home/UIZone (tránh tai thỏ, cạnh trên): Starter, No Ads, thanh trên ----------
        // Xóa cả bản cũ nằm thẳng trong HomePanel / AlwayOn
        RectTransform homeZone = UIZoneBuilder.Ensure(home, true, false, false, false);
        homeZone.SetSiblingIndex(home.Find("PlayButton").GetSiblingIndex()); // trên nền, cùng lớp với nút PLAY
        foreach (string old in new[] { "Starter", "NoAds", "Daily", "TopBar" })
        {
            PopupBuilder.DestroyChild(home, old);
            PopupBuilder.DestroyChild(homeZone, old);
        }
        PopupBuilder.DestroyChild(alwaysOn, "TopBar");
        MenuShortcutButton starter = BuildStarter(homeZone);
        MenuShortcutButton noAds = BuildNoAds(homeZone);
        MenuShortcutButton daily = BuildDaily(homeZone);

        // Thanh trên chỉ thuộc tab Home (trượt đi cùng panel)
        RectTransform topBar = PopupBuilder.NewUI("TopBar", homeZone);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 260f);
        topBar.anchoredPosition = Vector2.zero;
        BuildAvatar(topBar);
        EnergyHUD energy = BuildEnergyPill(topBar);
        CoinHUD coin = BuildCoinPill(topBar);
        SettingsButton settings = BuildSettings(topBar);

        // ---------- Navigator ----------
        MainMenuNavigator nav = canvasGo.GetComponent<MainMenuNavigator>();
        if (nav == null) nav = Undo.AddComponent<MainMenuNavigator>(canvasGo);
        SerializedObject so = new SerializedObject(nav);
        so.FindProperty("panelRoot").objectReferenceValue = panels;
        so.FindProperty("tabBar").objectReferenceValue = bar;
        so.FindProperty("selectedBackground").objectReferenceValue = selected;
        so.FindProperty("selectedWidth").floatValue = SelectedWidth;
        SetArray(so.FindProperty("panels"), shop, (RectTransform)home, progress);
        SetArray(so.FindProperty("tabs"), tabs);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Vuốt ngang trên panel để chuyển tab
        MenuSwipe swipe = panels.GetComponent<MenuSwipe>();
        if (swipe == null) swipe = Undo.AddComponent<MenuSwipe>(panels.gameObject);
        PopupBuilder.Assign(swipe, "navigator", nav);

        PopupBuilder.Assign(coin, "navigator", nav);
        ShopBuilder.Wire(shop, nav);
        PopupBuilder.Assign(starter, "navigator", nav);
        PopupBuilder.Assign(noAds, "navigator", nav);
        PopupBuilder.Assign(daily, "navigator", nav);
        GameFlowUI flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
        if (flow != null)
        {
            PopupBuilder.Assign(energy, "gameFlow", flow);
            PopupBuilder.Assign(settings, "gameFlow", flow);
            PopupBuilder.Assign(daily.GetComponent<DailyRewardButton>(), "gameFlow", flow);
        }

        // ---------- ScreenFlow ----------
        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null)
        {
            ScreenFlow screenFlow = lm.GetComponent<ScreenFlow>();
            if (screenFlow == null) screenFlow = Undo.AddComponent<ScreenFlow>(lm.gameObject);
            PopupBuilder.Assign(screenFlow, "menuCanvas", canvasGo);
            PopupBuilder.Assign(screenFlow, "hudCanvas", PopupBuilder.FindRoot("Canvas_HUD"));
            PopupBuilder.Assign(screenFlow, "navigator", nav);
            if (flow != null) PopupBuilder.Assign(screenFlow, "gameFlow", flow);
        }

        // Xếp sẵn trong Editor: tab Home được chọn, panel đặt cạnh nhau
        Canvas.ForceUpdateCanvases();
        nav.LayoutInEditor();

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[MainMenuBuilder] Đã dựng thanh tab Shop | Home | Progress trong Canvas_Menu.");
    }

    private static MenuTabButton BuildTab(Transform bar, int index)
    {
        RectTransform tab = BottomCenter(PopupBuilder.NewUI("Tab_" + TabNames[index], bar), 336f, BarHeight);
        Image hit = tab.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f); // vùng bấm trong suốt
        Button button = tab.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        // Vạch ngăn ở mép phải
        RectTransform divider = PopupBuilder.NewUI("Divider", tab);
        divider.anchorMin = divider.anchorMax = new Vector2(1f, 0f);
        divider.pivot = new Vector2(0.5f, 0f);
        divider.anchoredPosition = new Vector2(0f, 0f);
        divider.sizeDelta = new Vector2(14f, 190f);
        Image divImg = divider.gameObject.AddComponent<Image>();
        divImg.sprite = PopupBuilder.LoadSprite(BG + "home_tab3.png");
        divImg.raycastTarget = false;

        Image icon = PopupBuilder.Icon(tab, "Icon", TabIcons[index], 130f);
        RectTransform iconRt = icon.rectTransform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0f);
        iconRt.anchoredPosition = new Vector2(0f, 100f);

        TMP_Text label = PopupBuilder.Text(tab, "Label", TabNames[index], Vector2.zero, new Vector2(400f, 80f), 60f, Color.white);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 52f);
        label.enableWordWrapping = false;
        Outline(label, GameFontSetup.OutlinePurple());

        MenuTabButton mtb = Undo.AddComponent<MenuTabButton>(tab.gameObject);
        PopupBuilder.Assign(mtb, "icon", iconRt);
        PopupBuilder.Assign(mtb, "label", label);
        PopupBuilder.Assign(mtb, "divider", divider.gameObject);
        // Tab không có phản hồi nhấn (ButtonPress tắt): đổi tab đã có hiệu ứng icon + nền trượt riêng
        Undo.AddComponent<ButtonPress>(tab.gameObject).enabled = false;
        return mtb;
    }

    /// <summary>
    /// Nút PLAY phía trên thanh tab, ghép như bản gốc: viền trắng btn_green_4 (564x225) + lớp xanh btn_green_3 (542x203)
    /// bên trong; chữ "PLAY" + "Level n".
    /// </summary>
    private static void BuildPlayButton(Transform home)
    {
        RectTransform rt = BottomCenter(PopupBuilder.NewUI("PlayButton", home), 564f, 225f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 470f); // tâm nút cách đáy 470 như home_demo3
        Image frame = rt.gameObject.AddComponent<Image>();
        frame.sprite = PopupBuilder.LoadSprite(Buttons + "btn_green_4.png");

        RectTransform fill = PopupBuilder.NewUI("Fill", rt);
        fill.sizeDelta = new Vector2(542f, 203f);
        Image fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = PopupBuilder.LoadSprite(Buttons + "btn_green_3.png");
        fillImg.raycastTarget = false;

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = fillImg; // khung viền kem đứng yên khi nhấn, chỉ mặt nút tối đi (ButtonPress lún phần bên trong)

        TMP_Text play = PopupBuilder.Text(rt, "Play", "PLAY", new Vector2(0f, 26f), new Vector2(520f, 130f), 118f, Color.white);
        Outline(play, GameFontSetup.OutlineGreen());
        play.characterSpacing = 4f;
        TMP_Text level = PopupBuilder.Text(rt, "Level", "Level 1", new Vector2(0f, -52f), new Vector2(520f, 70f), 52f, LevelText);
        level.fontStyle = FontStyles.Normal;

        PlayButtonView view = Undo.AddComponent<PlayButtonView>(rt.gameObject);
        PopupBuilder.Assign(view, "levelText", level);
        DifficultyBuilder.BuildPlayParts(view, DifficultyBuilder.EnsureTheme()); // level Hard / Very Hard
    }

    /// <summary>Avatar góc trên trái (khung + mặt khối xanh có sẵn trong khung_avatar).</summary>
    private static void BuildAvatar(Transform topBar)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Avatar", topBar), new Vector2(-432f, -132f), new Vector2(150f, 152f));
        Image img = root.gameObject.AddComponent<Image>();
        img.sprite = PopupBuilder.LoadSprite(Frames + "khung_avatar.png");
        img.preserveAspect = true;
        Button b = root.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
    }

    /// <summary>Nút cài đặt góc trên phải: ô tím nhạt + bánh răng; bấm mở popup cài đặt.</summary>
    private static SettingsButton BuildSettings(Transform topBar)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Settings", topBar), new Vector2(445f, -122f), new Vector2(118f, 118f));
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(Frames + "khung_setting_3.png");
        Button b = root.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        Image gear = PopupBuilder.Icon(root, "Gear", Buttons + "btn_setting.png", 88f);
        gear.rectTransform.anchoredPosition = new Vector2(0f, 3f);
        return Undo.AddComponent<SettingsButton>(root.gameObject);
    }

    /// <summary>Ô năng lượng như ô tim bản gốc: tim (số đè lên) chồm lên đầu ô kem, đồng hồ trong ô, "+" dưới tim.</summary>
    internal static EnergyHUD BuildEnergyPill(Transform topBar)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Energy", topBar), new Vector2(-150f, -110f), new Vector2(300f, 120f));
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        PillBackground(root, 12f, 216f);
        Image heart = PopupBuilder.Icon(root, "Heart", PopupBuilder.EnergySprite, 112f);
        heart.rectTransform.anchoredPosition = new Vector2(-82f, 2f);
        TMP_Text count = PopupBuilder.Text(heart.transform, "Count", "5", new Vector2(0f, 4f), new Vector2(100f, 80f), 56f, Color.white);
        Outline(count, GameFontSetup.OutlineRed());
        TMP_Text timer = PopupBuilder.Text(root, "Timer", "FULL", new Vector2(40f, 0f), new Vector2(170f, 70f), 50f, PillText);
        PillTextStyle(timer);
        PlusBadge(root, new Vector2(-52f, -38f));

        EnergyHUD hud = Undo.AddComponent<EnergyHUD>(root.gameObject);
        PopupBuilder.Assign(hud, "countText", count);
        PopupBuilder.Assign(hud, "timerText", timer);
        PopupBuilder.Assign(hud, "icon", heart.rectTransform);
        // Số đè lên tim: vô hạn thì đổi sang tim có dấu ∞ và ẩn số, như "∞ 29:29" của bản gốc
        PopupBuilder.Assign(hud, "iconImage", heart);
        PopupBuilder.Assign(hud, "normalSprite", heart.sprite);
        PopupBuilder.Assign(hud, "infiniteSprite", PopupBuilder.LoadSprite(PopupBuilder.EnergyInfiniteSprite));
        return hud;
    }

    /// <summary>Ô coin; bấm -> tab Shop.</summary>
    internal static CoinHUD BuildCoinPill(Transform topBar)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Coin", topBar), new Vector2(170f, -110f), new Vector2(300f, 120f));
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        PillBackground(root, 12f, 216f);
        Image icon = PopupBuilder.Icon(root, "Icon", PopupBuilder.UI + "Icons/coin.png", 100f);
        icon.rectTransform.anchoredPosition = new Vector2(-82f, 0f);
        TMP_Text amount = PopupBuilder.Text(root, "Amount", "0", new Vector2(40f, 0f), new Vector2(170f, 70f), 50f, PillText);
        PillTextStyle(amount);
        PlusBadge(root, new Vector2(-52f, -38f));

        CoinHUD hud = Undo.AddComponent<CoinHUD>(root.gameObject);
        PopupBuilder.Assign(hud, "amountText", amount);
        PopupBuilder.Assign(hud, "icon", icon.rectTransform);
        PopupBuilder.Assign(hud, "button", button);
        return hud;
    }

    /// <summary>Rương "Starter" bên trái (btm_chest_stater đã có ruy băng xanh) -> Shop.</summary>
    private static MenuShortcutButton BuildStarter(Transform home)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Starter", home), new Vector2(-407f, -375f), new Vector2(210f, 210f));
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button b = root.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;

        RectTransform content = PopupBuilder.NewUI("Content", root);
        content.sizeDelta = root.sizeDelta;
        // btm_chest_stater.png tách sẵn 3 lớp (cùng khung 200x192) để chỉ rương lắc: vòng tròn | rương | ruy băng đè đáy rương
        PopupBuilder.Icon(content, "Back", StarterSprite("back"), 205f);
        Image chest = PopupBuilder.Icon(content, "Chest", StarterSprite("chest"), 205f);
        PopupBuilder.Icon(content, "Ribbon", StarterSprite("ribbon"), 205f);
        SetPivotKeepPlace(chest.rectTransform, new Vector2(0.5f, 0.245f)); // đáy rương (100, 147 trong ảnh)
        TMP_Text label = PopupBuilder.Text(content, "Label", "Starter", new Vector2(0f, -72f), new Vector2(200f, 50f), 40f, Color.white);
        Outline(label, GameFontSetup.OutlinePurple());
        label.enableWordWrapping = false;

        return Shortcut(root, chest.rectTransform, 1.4f, 0.6f, 12f, 0.05f, 9f);
    }

    /// <summary>TV "ADS" gạch chéo + ruy băng cam "No Ads" bên phải -> Shop.</summary>
    private static MenuShortcutButton BuildNoAds(Transform home)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("NoAds", home), new Vector2(400f, -375f), new Vector2(210f, 210f));
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button b = root.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;

        RectTransform content = PopupBuilder.NewUI("Content", root);
        content.sizeDelta = root.sizeDelta;
        Image tv = PopupBuilder.Icon(content, "Tv", PopupBuilder.UI + "Icons/no_ads_2.png", 175f);
        tv.rectTransform.anchoredPosition = new Vector2(0f, 18f);
        SetPivotKeepPlace(tv.rectTransform, new Vector2(0.5f, 0.06f)); // chân TV
        Image ribbon = PopupBuilder.Icon(content, "Ribbon", PopupBuilder.UI + "Icons/no_ads_1.png", 205f);
        ribbon.rectTransform.sizeDelta = new Vector2(205f, 68f);
        ribbon.rectTransform.anchoredPosition = new Vector2(0f, -70f);
        TMP_Text label = PopupBuilder.Text(content, "Label", "No Ads", new Vector2(0f, -70f), new Vector2(200f, 50f), 40f, Color.white);
        Outline(label, GameFontSetup.OutlineBrown());
        label.enableWordWrapping = false;

        return Shortcut(root, tv.rectTransform, 2.35f, 1.3f, 10f, 0.1f, 6f);
    }

    /// <summary>
    /// Điểm danh hằng ngày dưới Starter: quyển lịch trong khung tròn + ruy băng như Starter, tông hồng tím
    /// (daily_back / daily_ribbon đổi màu từ starter_back / starter_ribbon), chấm đỏ "!" báo chưa điểm danh.
    /// Bấm mở popup điểm danh (DailyRewardButton); còn quà hôm nay thì lắc + chấm đỏ.
    /// </summary>
    private static MenuShortcutButton BuildDaily(Transform home)
    {
        RectTransform root = TopCenter(PopupBuilder.NewUI("Daily", home), new Vector2(-407f, -615f), new Vector2(210f, 210f));
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        Button b = root.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;

        RectTransform content = PopupBuilder.NewUI("Content", root);
        content.sizeDelta = root.sizeDelta;
        PopupBuilder.Icon(content, "Back", IconSprite("daily_back"), 205f);
        Image calendar = PopupBuilder.Icon(content, "Calendar", PopupBuilder.UI + "Icons/icon_daily.png", 126f);
        calendar.rectTransform.anchoredPosition = new Vector2(0f, 14f);
        SetPivotKeepPlace(calendar.rectTransform, new Vector2(0.5f, 0.05f)); // đáy quyển lịch
        PopupBuilder.Icon(content, "Ribbon", IconSprite("daily_ribbon"), 205f);
        TMP_Text label = PopupBuilder.Text(content, "Label", "Daily", new Vector2(0f, -72f), new Vector2(200f, 50f), 40f, Color.white);
        Outline(label, GameFontSetup.OutlinePurple());
        label.enableWordWrapping = false;

        Image badge = PopupBuilder.Icon(content, "Badge", IconSprite("badge_red"), 52f);
        badge.rectTransform.anchoredPosition = new Vector2(66f, 68f);
        TMP_Text mark = PopupBuilder.Text(badge.transform, "Mark", "!", new Vector2(0f, 2f), new Vector2(52f, 52f), 40f, Color.white);
        mark.font = GameFontSetup.Font;

        MenuShortcutButton s = Shortcut(root, calendar.rectTransform, 1.9f, 1.0f, 10f, 0.06f, 8f);
        SerializedObject so = new SerializedObject(s);
        so.FindProperty("tab").intValue = -1; // không chuyển tab: DailyRewardButton mở popup
        so.FindProperty("badge").objectReferenceValue = badge.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        DailyRewardButton daily = Undo.AddComponent<DailyRewardButton>(root.gameObject);
        PopupBuilder.Assign(daily, "shortcut", s);
        return s;
    }

    /// <summary>Dựng lại riêng nút Daily trong scene đang mở (Build Daily Reward gọi), nối navigator + GameFlowUI.</summary>
    internal static void RebuildDaily(GameFlowUI flow)
    {
        MainMenuNavigator nav = Object.FindFirstObjectByType<MainMenuNavigator>(FindObjectsInactive.Include);
        Transform zone = nav != null ? nav.transform.Find("Panels/HomePanel/" + UIZoneBuilder.ZoneName) : null;
        if (zone == null)
        {
            Debug.LogError("[MainMenuBuilder] Không tìm thấy HomePanel/UIZone. Chạy Build Main Menu trước.");
            return;
        }
        Transform old = zone.Find("Daily");
        Transform starter = zone.Find("Starter");
        int index = old != null ? old.GetSiblingIndex() : (starter != null ? starter.GetSiblingIndex() + 1 : zone.childCount);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        MenuShortcutButton daily = BuildDaily(zone);
        daily.transform.SetSiblingIndex(index);
        PopupBuilder.Assign(daily, "navigator", nav);
        if (flow != null) PopupBuilder.Assign(daily.GetComponent<DailyRewardButton>(), "gameFlow", flow);
    }

    /// <summary>
    /// Nhịp lắc đo từ video bản gốc (30 fps): Starter ~2 s / lần, No Ads ~3 s / lần, lệch pha nhau; mỗi lần ~0.65 s.
    /// No Ads nhấc ít hơn nhưng to ra nhiều hơn, nghiêng nhẹ hơn.
    /// </summary>
    private static MenuShortcutButton Shortcut(RectTransform root, RectTransform icon, float interval, float firstDelay,
        float lift, float scaleUp, float angle)
    {
        MenuShortcutButton s = Undo.AddComponent<MenuShortcutButton>(root.gameObject);
        SerializedObject so = new SerializedObject(s);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("interval").floatValue = interval;
        so.FindProperty("firstDelay").floatValue = firstDelay;
        so.FindProperty("lift").floatValue = lift;
        so.FindProperty("scaleUp").floatValue = scaleUp;
        so.FindProperty("angle").floatValue = angle;
        so.ApplyModifiedPropertiesWithoutUndo();
        return s;
    }

    private static string StarterSprite(string layer) => IconSprite("starter_" + layer);

    /// <summary>Ảnh trong Icons/ tạo thêm bằng script (tách / đổi màu): đảm bảo kiểu Sprite.</summary>
    private static string IconSprite(string name)
    {
        string path = PopupBuilder.UI + "Icons/" + name + ".png";
        PopupBuilder.EnsureSprite(path);
        return path;
    }

    /// <summary>Đổi pivot mà hình vẫn nằm nguyên chỗ (để xoay / phóng quanh điểm khác tâm).</summary>
    private static void SetPivotKeepPlace(RectTransform rt, Vector2 pivot)
    {
        Vector2 delta = pivot - rt.pivot;
        rt.pivot = pivot;
        rt.anchoredPosition += new Vector2(delta.x * rt.rect.width, delta.y * rt.rect.height);
    }

    private static void PillBackground(RectTransform root, float x, float width)
    {
        EnsurePillSprite();
        RectTransform bg = PopupBuilder.NewUI("Bg", root);
        bg.anchoredPosition = new Vector2(x, 0f);
        bg.sizeDelta = new Vector2(width, 78f);
        Image img = bg.gameObject.AddComponent<Image>();
        img.sprite = PopupBuilder.LoadSprite(Pill);
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
    }

    private static void PlusBadge(RectTransform root, Vector2 pos)
    {
        Image plus = PopupBuilder.Icon(root, "Plus", Buttons + "btn_add.png", 46f);
        plus.rectTransform.anchoredPosition = pos;
    }

    /// <summary>Số trên ô kem: nét Fredoka thường (không làm đậm giả), không viền, co theo ô.</summary>
    private static void PillTextStyle(TMP_Text t)
    {
        t.fontStyle = FontStyles.Normal;
        t.enableWordWrapping = false;
        t.enableAutoSizing = true;
        t.fontSizeMin = 30f;
        t.fontSizeMax = t.fontSize;
    }

    private static void Outline(TMP_Text t, Material preset)
    {
        t.font = GameFontSetup.Font;
        t.fontSharedMaterial = preset;
    }

    /// <summary>khung_pill_kem.png: Sprite, viền 9-slice 44 px hai đầu để kéo dài mà giữ bo tròn.</summary>
    private static void EnsurePillSprite()
    {
        TextureImporter ti = AssetImporter.GetAtPath(Pill) as TextureImporter;
        if (ti == null) return;
        Vector4 border = new Vector4(44f, 0f, 44f, 0f);
        if (ti.textureType == TextureImporterType.Sprite && ti.spriteBorder == border && ti.spriteImportMode == SpriteImportMode.Single) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spriteBorder = border;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
    }

    private static RectTransform TopCenter(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }


    /// <summary>Khung tạm cho Progress: lên level nào nhận quà gì (thay Daily Challenge của bản gốc).</summary>
    private static RectTransform BuildProgressPlaceholder(Transform parent)
    {
        RectTransform panel = PopupBuilder.Stretch(PopupBuilder.NewUI("ProgressPanel", parent));
        panel.gameObject.AddComponent<Image>().color = new Color(0.36f, 0.31f, 0.9f);

        Image cup = PopupBuilder.Icon(panel, "Cup", Icons + "icon_cup.png", 300f);
        cup.rectTransform.anchorMin = cup.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        cup.rectTransform.anchoredPosition = new Vector2(0f, -330f);

        Placeholder(panel, "PROGRESS", "Reach levels to earn rewards\nComing soon");
        return panel;
    }

    private static void Placeholder(RectTransform panel, string title, string subtitle)
    {
        PopupBuilder.Text(panel, "Title", title, new Vector2(0f, 60f), new Vector2(900f, 140f), 110f, Color.white);
        TMP_Text sub = PopupBuilder.Text(panel, "Subtitle", subtitle, new Vector2(0f, -80f), new Vector2(900f, 160f), 50f, new Color(1f, 1f, 1f, 0.7f));
        sub.fontStyle = FontStyles.Normal;
    }

    // ---------- Tiện ích ----------

    private static RectTransform BottomCenter(RectTransform rt, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static void SetArray(SerializedProperty prop, params Object[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>Tìm cả object đang tắt trong scene đang mở.</summary>
    private static GameObject FindInScene(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }
}
