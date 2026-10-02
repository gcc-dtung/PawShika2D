using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng popup Giai đoạn 3 vào Canvas_HUD của scene đang mở: Win, Hồi mạng, Thua, Xác nhận chơi lại;
/// gắn GameFlowUI, RewardedAds và nối nút chơi lại của LevelHUD.
/// Chạy lại được: xóa "Popups" cũ rồi dựng mới (kèm "LowLivesWarning" của bản trước nếu còn).
/// </summary>
public static class PopupBuilder
{
    private const string UI = "Assets/_Project/GameAssets/04_UI_Graphics/";
    private const float PanelScale = 1.6f; // popup_1 552x442 -> 883x707

    private static readonly Color Yellow = new Color(1f, 0.84f, 0.29f);
    private static readonly Color Red = new Color(1f, 0.45f, 0.45f);

    [MenuItem("Tools/PawBlock/Build Popups")]
    public static void Build()
    {
        GameObject canvasGo = GameObject.Find("Canvas_HUD");
        if (canvasGo == null)
        {
            Debug.LogError("[PopupBuilder] Không tìm thấy Canvas_HUD trong scene.");
            return;
        }
        Transform canvas = canvasGo.transform;

        DestroyChild(canvas, "LowLivesWarning");
        DestroyChild(canvas, "Popups");

        RectTransform root = Stretch(NewUI("Popups", canvas));
        GameFlowUI flow = root.gameObject.AddComponent<GameFlowUI>();

        WinPopup win = BuildWin(root);
        ReviveLivesPopup revive = BuildRevive(root);
        LosePopup lose = BuildLose(root);
        ConfirmReplayPopup confirm = BuildConfirm(root);

        Assign(flow, "winPopup", win);
        Assign(flow, "revivePopup", revive);
        Assign(flow, "losePopup", lose);
        Assign(flow, "confirmReplayPopup", confirm);

        LevelHUD hud = Object.FindFirstObjectByType<LevelHUD>(FindObjectsInactive.Include);
        if (hud != null) Assign(hud, "gameFlow", flow);

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null && lm.GetComponent<RewardedAds>() == null) lm.gameObject.AddComponent<RewardedAds>();

        foreach (PopupView p in new PopupView[] { win, revive, lose, confirm }) p.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[PopupBuilder] Đã dựng popup trong Canvas_HUD/Popups.");
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
        Text(panel, "Hint", "Try again!", new Vector2(0, -185), new Vector2(760, 80), 54, Color.white);
        Button retry = NewButton(panel, "RetryButton", UI + "Buttons/btn_green_1.png", new Vector2(0, -470), "RETRY", null, null);

        Assign(popup, "titleText", title);
        Assign(popup, "retryButton", retry);
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
    private static RectTransform Row(Transform parent, string name, Vector2 pos, float height)
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

    private static Image Icon(Transform parent, string name, string spritePath, float size)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = LoadSprite(spritePath);
        img.preserveAspect = true;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(size, size);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
        return img;
    }

    private static TMP_Text Text(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
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
    private static void Fit(TMP_Text t)
    {
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
    }

    // ---------- Tiện ích ----------

    private static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Build Popups");
        return (RectTransform)go.transform;
    }

    private static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[PopupBuilder] Không tìm thấy sprite: {path}");
        return s;
    }

    private static void Assign(Object target, string field, Object value)
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
