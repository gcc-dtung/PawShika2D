using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng các popup vào Canvas_Popup (canvas riêng, luôn bật, vẽ trên Canvas_HUD và Canvas_Menu) để dùng được
/// ở cả màn chơi lẫn Main Menu: Win, Hồi mạng, Thua, Xác nhận chơi lại, Mua booster, Nạp năng lượng;
/// gắn GameFlowUI, RewardedAds và nối các nút cần GameFlowUI (LevelHUD, EnergyHUD, ScreenFlow).
/// Chạy lại được: xóa "Popups" cũ (cả bản cũ nằm trong Canvas_HUD) rồi dựng mới.
/// </summary>
public static class PopupBuilder
{
    internal const string UI = "Assets/_Project/GameAssets/04_UI_Graphics/";
    private const float PanelScale = 1.6f; // popup_1 552x442 -> 883x707

    internal static readonly Color Yellow = new Color(1f, 0.84f, 0.29f);
    // Icon năng lượng: tim như bản gốc (trước đây là cá ShikaCat tô cam)
    internal const string EnergySprite = UI + "Icons/icon_hear_2.png";
    internal const string EnergyInfiniteSprite = UI + "Icons/icon_hear_infinity2.png";
    private static readonly Color Red = new Color(1f, 0.45f, 0.45f);

    [MenuItem("Tools/PawBlock/Build Popups")]
    public static void Build()
    {
        GameObject hudGo = FindRoot("Canvas_HUD");
        if (hudGo != null)
        {
            DestroyChild(hudGo.transform, "LowLivesWarning");
            DestroyChild(hudGo.transform, "Popups"); // bản cũ nằm trong HUD
        }

        GameObject canvasGo = EnsurePopupCanvas();
        Transform canvas = canvasGo.transform;
        DestroyChild(canvas, "Popups");

        RectTransform root = Stretch(NewUI("Popups", canvas));
        GameFlowUI flow = root.gameObject.AddComponent<GameFlowUI>();

        WinPopup win = BuildWin(root);
        ReviveLivesPopup revive = BuildRevive(root);
        LosePopup lose = BuildLose(root);
        ConfirmReplayPopup confirm = BuildConfirm(root);
        BuyBoosterPopup buy = BuildBuyBooster(root);
        RefillEnergyPopup refill = BuildRefillEnergy(root);

        Assign(flow, "winPopup", win);
        Assign(flow, "revivePopup", revive);
        Assign(flow, "losePopup", lose);
        Assign(flow, "confirmReplayPopup", confirm);
        Assign(flow, "buyBoosterPopup", buy);
        Assign(flow, "refillEnergyPopup", refill);

        EnergyHUD energyHud = Object.FindFirstObjectByType<EnergyHUD>(FindObjectsInactive.Include);
        if (energyHud != null) Assign(energyHud, "gameFlow", flow);

        LevelHUD hud = Object.FindFirstObjectByType<LevelHUD>(FindObjectsInactive.Include);
        if (hud != null) Assign(hud, "gameFlow", flow);

        foreach (EnergyHUD e in Object.FindObjectsByType<EnergyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Assign(e, "gameFlow", flow);
        ScreenFlow screenFlow = Object.FindFirstObjectByType<ScreenFlow>(FindObjectsInactive.Include);
        if (screenFlow != null) Assign(screenFlow, "gameFlow", flow);

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null && lm.GetComponent<RewardedAds>() == null) lm.gameObject.AddComponent<RewardedAds>();

        foreach (PopupView p in new PopupView[] { win, revive, lose, confirm, buy, refill }) p.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[PopupBuilder] Đã dựng popup trong Canvas_Popup/Popups.");
    }

    /// <summary>Canvas Overlay riêng cho popup: 1080x1920 như HUD, sortingOrder 20 (trên Menu 10, HUD 0).</summary>
    private static GameObject EnsurePopupCanvas()
    {
        GameObject go = FindRoot("Canvas_Popup");
        if (go == null)
        {
            go = new GameObject("Canvas_Popup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            Undo.RegisterCreatedObjectUndo(go, "Build Popups");
        }
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 20;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;
        if (!go.activeSelf) go.SetActive(true);
        return go;
    }

    /// <summary>Tìm object gốc trong scene đang mở, kể cả đang tắt.</summary>
    internal static GameObject FindRoot(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }

    /// <summary>
    /// Gốc chứa UI của Canvas_HUD: Canvas_HUD/UIZone (khung safe area, dựng bằng Build UI Zone) nếu có, không thì chính canvas.
    /// Các builder tìm TopPanel / DownPanel / RevealBanner / PlayZone theo gốc này.
    /// </summary>
    internal static Transform HudRoot(GameObject hudCanvas)
    {
        if (hudCanvas == null) return null;
        Transform zone = hudCanvas.transform.Find(UIZoneBuilder.ZoneName);
        return zone != null ? zone : hudCanvas.transform;
    }

    // ---------- Từng popup ----------

    private static WinPopup BuildWin(Transform root)
    {
        RectTransform panel;
        WinPopup popup = NewPopup<WinPopup>(root, "WinPopup", out panel);
        TMP_Text title = Title(panel, "LEVEL 1");
        Text(panel, "Completed", "COMPLETED!", new Vector2(0, -50), new Vector2(760, 110), 76, Yellow);

        RectTransform reward = Row(panel, "Reward", new Vector2(0, -190), 150);
        Icon(reward, "Coin", UI + "Icons/coin.png", 90);
        TMP_Text rewardText = Text(reward, "Amount", "+10", Vector2.zero, new Vector2(200, 100), 72, Color.white);
        Fit(rewardText);

        Button doubleBtn = NewButton(panel, "DoubleButton", UI + "Buttons/btn_yellow_1.png", new Vector2(-200, -470), "x2", UI + "Buttons/btn_video.png", null);
        Button next = NewButton(panel, "NextButton", UI + "Buttons/btn_green_1.png", new Vector2(200, -470), "NEXT", null, null);

        Assign(popup, "titleText", title);
        Assign(popup, "rewardText", rewardText);
        Assign(popup, "nextButton", next);
        Assign(popup, "doubleButton", doubleBtn);
        return popup;
    }

    private static ReviveLivesPopup BuildRevive(Transform root)
    {
        RectTransform panel;
        ReviveLivesPopup popup = NewPopup<ReviveLivesPopup>(root, "ReviveLivesPopup", out panel);
        Title(panel, "OUT OF LIVES");

        Button close = NewButton(panel, "CloseButton", UI + "Buttons/btn_X_1.png", new Vector2(405, 255), null, null, null);
        ((RectTransform)close.transform).sizeDelta = new Vector2(120, 120);

        RectTransform heartRow = Row(panel, "Heart", new Vector2(0, -40), 140);
        Icon(heartRow, "BrokenHeart", UI + "Icons/heart_2.png", 130);
        Fit(Text(heartRow, "Message", "Keep playing?", Vector2.zero, new Vector2(460, 100), 60, Color.white));

        RectTransform balanceRow = Row(panel, "Balance", new Vector2(0, -185), 90);
        Icon(balanceRow, "Coin", UI + "Icons/coin.png", 70);
        TMP_Text balance = Text(balanceRow, "Amount", "0", Vector2.zero, new Vector2(200, 80), 54, Yellow);
        Fit(balance);

        Button ads = NewButton(panel, "AdsButton", UI + "Buttons/btn_green_1.png", new Vector2(-200, -470), "+1", UI + "Buttons/btn_video.png", UI + "Icons/heart_1.png");
        Button coin = NewButton(panel, "CoinButton", UI + "Buttons/btn_yellow_1.png", new Vector2(200, -470), "+3", null, UI + "Icons/heart_1.png");

        // Giá coin ghi dưới nút
        RectTransform costRow = Row(panel, "Cost", new Vector2(200, -590), 80);
        Icon(costRow, "Coin", UI + "Icons/coin.png", 60);
        TMP_Text cost = Text(costRow, "Amount", "900", Vector2.zero, new Vector2(160, 70), 48, Color.white);
        Fit(cost);
        Fit(Text(panel, "AdsCaption", "FREE", new Vector2(-200, -590), new Vector2(300, 70), 48, Color.white));

        Assign(popup, "adsButton", ads);
        Assign(popup, "coinButton", coin);
        Assign(popup, "closeButton", close);
        Assign(popup, "coinCostText", cost);
        Assign(popup, "balanceText", balance);
        return popup;
    }

    private static LosePopup BuildLose(Transform root)
    {
        RectTransform panel;
        LosePopup popup = NewPopup<LosePopup>(root, "LosePopup", out panel);
        TMP_Text title = Title(panel, "LEVEL 1");
        Text(panel, "Failed", "FAILED", new Vector2(0, -60), new Vector2(760, 120), 84, Red);
        // Thua tốn 1 năng lượng (tim)
        RectTransform cost = Row(panel, "EnergyCost", new Vector2(0, -190), 100);
        Fit(Text(cost, "Amount", "-1", Vector2.zero, new Vector2(120, 90), 60, Color.white));
        Icon(cost, "Heart", EnergySprite, 100);
        Button home = NewButton(panel, "HomeButton", UI + "Buttons/btn_red_1.png", new Vector2(-200, -470), "HOME", null, null);
        Button retry = NewButton(panel, "RetryButton", UI + "Buttons/btn_green_1.png", new Vector2(200, -470), "RETRY", null, null);

        Assign(popup, "titleText", title);
        Assign(popup, "retryButton", retry);
        Assign(popup, "homeButton", home);
        return popup;
    }

    private static ConfirmReplayPopup BuildConfirm(Transform root)
    {
        RectTransform panel;
        ConfirmReplayPopup popup = NewPopup<ConfirmReplayPopup>(root, "ConfirmReplayPopup", out panel);
        Title(panel, "REPLAY?");
        Text(panel, "Message", "Placed blocks will be lost.\nStart this level again?", new Vector2(0, -110), new Vector2(760, 220), 52, Color.white);
        Button no = NewButton(panel, "NoButton", UI + "Buttons/btn_red_1.png", new Vector2(-200, -470), "NO", null, null);
        Button yes = NewButton(panel, "YesButton", UI + "Buttons/btn_green_1.png", new Vector2(200, -470), "YES", null, null);

        Assign(popup, "yesButton", yes);
        Assign(popup, "noButton", no);
        return popup;
    }

    private static BuyBoosterPopup BuildBuyBooster(Transform root)
    {
        RectTransform panel;
        BuyBoosterPopup popup = NewPopup<BuyBoosterPopup>(root, "BuyBoosterPopup", out panel);
        TMP_Text title = Title(panel, "HINT");

        Button close = NewButton(panel, "CloseButton", UI + "Buttons/btn_X_1.png", new Vector2(405, 255), null, null, null);
        ((RectTransform)close.transform).sizeDelta = new Vector2(120, 120);

        RectTransform iconRow = Row(panel, "Item", new Vector2(0, -20), 170);
        Image icon = Icon(iconRow, "Icon", "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/btn_hint.png", 160);
        TMP_Text amount = Text(iconRow, "Amount", "x3", Vector2.zero, new Vector2(150, 100), 72, Color.white);
        Fit(amount);

        TMP_Text detail = Text(panel, "Detail", "Detail", new Vector2(0, -190), new Vector2(720, 130), 40, Color.white);
        detail.fontStyle = FontStyles.Normal;

        Button buy = NewButton(panel, "BuyButton", UI + "Buttons/btn_green_1.png", new Vector2(0, -470), "1200", UI + "Icons/coin.png", null);
        TMP_Text price = buy.transform.Find("Content/Label").GetComponent<TMP_Text>();

        RectTransform balanceRow = Row(panel, "Balance", new Vector2(0, -590), 80);
        Icon(balanceRow, "Coin", UI + "Icons/coin.png", 60);
        TMP_Text balance = Text(balanceRow, "Amount", "0", Vector2.zero, new Vector2(160, 70), 48, Yellow);
        Fit(balance);

        Assign(popup, "titleText", title);
        Assign(popup, "detailText", detail);
        Assign(popup, "icon", icon);
        Assign(popup, "amountText", amount);
        Assign(popup, "priceText", price);
        Assign(popup, "balanceText", balance);
        Assign(popup, "buyButton", buy);
        Assign(popup, "closeButton", close);
        return popup;
    }

    private static RefillEnergyPopup BuildRefillEnergy(Transform root)
    {
        RectTransform panel;
        RefillEnergyPopup popup = NewPopup<RefillEnergyPopup>(root, "RefillEnergyPopup", out panel);
        Title(panel, "MORE LIVES");

        Button close = NewButton(panel, "CloseButton", UI + "Buttons/btn_X_1.png", new Vector2(405, 255), null, null, null);
        ((RectTransform)close.transform).sizeDelta = new Vector2(120, 120);

        RectTransform heartRow = Row(panel, "Energy", new Vector2(0, -40), 150);
        Icon(heartRow, "Heart", EnergySprite, 150);
        TMP_Text count = Text(heartRow, "Count", "0/5", Vector2.zero, new Vector2(200, 100), 72, Color.white);
        Fit(count);

        TMP_Text timer = Text(panel, "Timer", "Next in 30:00", new Vector2(0, -170), new Vector2(720, 70), 48, Yellow);

        Button ads = NewButton(panel, "AdsButton", UI + "Buttons/btn_green_1.png", new Vector2(-200, -470), "+1", UI + "Buttons/btn_video.png", EnergySprite);
        Button coin = NewButton(panel, "CoinButton", UI + "Buttons/btn_yellow_1.png", new Vector2(200, -470), "FULL", null, EnergySprite);

        RectTransform costRow = Row(panel, "Cost", new Vector2(200, -590), 80);
        Icon(costRow, "Coin", UI + "Icons/coin.png", 60);
        TMP_Text cost = Text(costRow, "Amount", "900", Vector2.zero, new Vector2(160, 70), 48, Color.white);
        Fit(cost);
        Fit(Text(panel, "AdsCaption", "FREE", new Vector2(-200, -590), new Vector2(300, 70), 48, Color.white));

        RectTransform balanceRow = Row(panel, "Balance", new Vector2(0, -680), 70);
        Icon(balanceRow, "Coin", UI + "Icons/coin.png", 50);
        TMP_Text balance = Text(balanceRow, "Amount", "0", Vector2.zero, new Vector2(160, 60), 40, Yellow);
        Fit(balance);

        Assign(popup, "countText", count);
        Assign(popup, "timerText", timer);
        Assign(popup, "coinCostText", cost);
        Assign(popup, "balanceText", balance);
        Assign(popup, "adsButton", ads);
        Assign(popup, "coinButton", coin);
        Assign(popup, "closeButton", close);
        return popup;
    }

    // ---------- Khung chung ----------

    private static T NewPopup<T>(Transform root, string name, out RectTransform panel) where T : PopupView
    {
        RectTransform rt = Stretch(NewUI(name, root));
        rt.gameObject.AddComponent<CanvasGroup>();
        T popup = rt.gameObject.AddComponent<T>();

        RectTransform dim = Stretch(NewUI("Dim", rt));
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.75f);

        panel = NewUI("Panel", rt);
        Image bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = LoadSprite(UI + "Popups_Dialogs/popup_1.png");
        bg.raycastTarget = true;
        panel.sizeDelta = new Vector2(552, 442) * PanelScale;
        panel.anchoredPosition = new Vector2(0, 120);

        Assign(popup, "panel", panel);
        return popup;
    }

    /// <summary>Chữ trong tab trên cùng của popup_1 (tab ở 2–40 % chiều cao tính từ trên).</summary>
    private static TMP_Text Title(RectTransform panel, string text)
    {
        float y = panel.sizeDelta.y * (0.5f - 0.215f);
        TMP_Text t = Text(panel, "Title", text, new Vector2(0, y), new Vector2(470, 120), 64, Color.white);
        t.enableAutoSizing = true;
        t.fontSizeMin = 36;
        t.fontSizeMax = 64;
        return t;
    }

    private static Button NewButton(Transform parent, string name, string spritePath, Vector2 pos, string label, string leftIcon, string rightIcon)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = LoadSprite(spritePath);
        if (img.sprite != null) rt.sizeDelta = img.sprite.rect.size;
        rt.anchoredPosition = pos;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;

        if (label == null && leftIcon == null && rightIcon == null) return b;

        RectTransform content = Row(rt, "Content", new Vector2(0, 6), rt.sizeDelta.y * 0.7f);
        if (leftIcon != null) Icon(content, "LeftIcon", leftIcon, 72);
        if (label != null) Fit(Text(content, "Label", label, Vector2.zero, new Vector2(200, 90), 64, Color.white));
        if (rightIcon != null) Icon(content, "RightIcon", rightIcon, 64);
        return b;
    }

    /// <summary>Hàng ngang tự căn giữa theo nội dung (icon + chữ).</summary>
    internal static RectTransform Row(Transform parent, string name, Vector2 pos, float height)
    {
        RectTransform rt = NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(0, height);
        HorizontalLayoutGroup h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 14;
        h.childControlWidth = true; // icon theo LayoutElement, chữ theo độ rộng nội dung
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        ContentSizeFitter f = rt.gameObject.AddComponent<ContentSizeFitter>();
        f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    internal static Image Icon(Transform parent, string name, string spritePath, float size)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = spritePath != null ? LoadSprite(spritePath) : null;
        img.preserveAspect = true;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(size, size);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
        return img;
    }

    internal static TMP_Text Text(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        RectTransform rt = NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        return t;
    }

    /// <summary>Chữ trong Row (hoặc đứng riêng): một dòng, Row lấy độ rộng theo nội dung.</summary>
    internal static void Fit(TMP_Text t)
    {
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
    }

    // ---------- Tiện ích ----------

    internal static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Build Popups");
        return (RectTransform)go.transform;
    }

    internal static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    internal static void DestroyChild(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
    }

    internal static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[PopupBuilder] Không tìm thấy sprite: {path}");
        return s;
    }

    internal static void Assign(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[PopupBuilder] {target.GetType().Name} không có field {field}");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
