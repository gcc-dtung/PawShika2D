using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → PawBlock → Builders → Build Tutorial (chạy lại được, dựng lại từ đầu):
/// - Canvas_HUD/UIZone/TutorialPanel (TutorialView): tiêu đề "Tutorial", nút Skip, 4 vạch tiến độ (tutorial_dot),
///   khung lời nhắc (popup_tutorial 9-slice). Tắt sẵn, TutorialController bật khi chạy.
/// - TutorialHand (gốc scene, world space): bàn tay tutorial_hand vẽ trên khối mèo, đầu ngón tay ở gốc object.
/// - TutorialController trên object có LevelManager.
/// - Canvas_HUD/BoosterTutorial (BoosterTutorialView): tutorial mở khóa booster; BoosterTutorial trên object có LevelManager.
/// - Canvas_Popup/Popups/MechanicIntroPopup: popup giới thiệu cơ chế; MechanicIntro trên object có LevelManager.
/// </summary>
public static class TutorialBuilder
{
    private const string HandSprite = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/tutorial_hand.png";
    private const string DotSprite = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/tutorial_dot.png";
    private const string BubbleSprite = "Assets/_Project/GameAssets/04_UI_Graphics/Popups_Dialogs/popup_tutorial.png";
    private const string RevealBannerSprite = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/khung_reveal2.png";
    private const string ConstraintPicture = "Assets/_Project/GameAssets/04_UI_Graphics/Popups_Dialogs/popup_tutorial_4.png";
    private const string LockPicture = "Assets/_Project/GameAssets/04_UI_Graphics/Popups_Dialogs/popup_tutorial_2.png";
    private const string HiddenPicture = "Assets/_Project/GameAssets/04_UI_Graphics/Popups_Dialogs/popup_tutorial_3.png";
    private const string PanelName = "TutorialPanel";
    private const string HandName = "TutorialHand";

    private static readonly Color BubbleText = new Color32(0x3B, 0x1F, 0x78, 0xFF);

    [MenuItem("Tools/PawBlock/Builders/Build Tutorial", priority = 111)]
    public static void Build()
    {
        GameObject hud = PopupBuilder.FindRoot("Canvas_HUD");
        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (hud == null || lm == null)
        {
            Debug.LogError("[TutorialBuilder] Không tìm thấy Canvas_HUD hoặc LevelManager trong scene.");
            return;
        }

        PopupBuilder.EnsureSprite(HandSprite);
        PopupBuilder.EnsureSprite(DotSprite);
        PopupBuilder.EnsureSliced(BubbleSprite, new Vector4(70, 70, 70, 70));

        Transform root = PopupBuilder.HudRoot(hud);
        TutorialView view = BuildPanel(root);
        TutorialHand hand = BuildHand();

        TutorialController controller = lm.GetComponent<TutorialController>();
        if (controller == null) controller = Undo.AddComponent<TutorialController>(lm.gameObject);
        PopupBuilder.Assign(controller, "view", view);
        PopupBuilder.Assign(controller, "hand", hand);

        BoosterTutorialView boosterView = BuildBoosterPanel(hud.transform);
        BoosterTutorial boosterTutorial = lm.GetComponent<BoosterTutorial>();
        if (boosterTutorial == null) boosterTutorial = Undo.AddComponent<BoosterTutorial>(lm.gameObject);
        PopupBuilder.Assign(boosterTutorial, "view", boosterView);

        MechanicIntro mechanicIntro = lm.GetComponent<MechanicIntro>();
        if (mechanicIntro == null) mechanicIntro = Undo.AddComponent<MechanicIntro>(lm.gameObject);
        GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
        Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
        if (popups != null) PopupBuilder.Assign(mechanicIntro, "popup", BuildMechanicPopup(popups));
        else Debug.LogError("[TutorialBuilder] Không thấy Canvas_Popup/Popups (chạy Build Popups trước): chưa dựng popup cơ chế.");
        foreach (string p in new[] { ConstraintPicture, LockPicture, HiddenPicture }) PopupBuilder.EnsureSprite(p);
        PopupBuilder.Assign(mechanicIntro, "constraintSprite", PopupBuilder.LoadSprite(ConstraintPicture));
        PopupBuilder.Assign(mechanicIntro, "lockSprite", PopupBuilder.LoadSprite(LockPicture));
        PopupBuilder.Assign(mechanicIntro, "hiddenSprite", PopupBuilder.LoadSprite(HiddenPicture));

        EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
        Selection.activeObject = view.gameObject;
        Debug.Log("[TutorialBuilder] Đã dựng TutorialPanel, TutorialHand, BoosterTutorial; gắn TutorialController, BoosterTutorial.");
    }

    /// <summary>
    /// Canvas_Popup/Popups/MechanicIntroPopup (tắt sẵn), như popup "Orientation" bản gốc: khung popup_tutorial 9-slice,
    /// tiêu đề tím đậm, 1 dòng giải thích, ảnh minh họa (popup_tutorial_2/3/4), nút xanh Ok.
    /// </summary>
    private static MechanicIntroPopup BuildMechanicPopup(Transform popups)
    {
        PopupBuilder.DestroyChild(popups, "MechanicIntroPopup");
        RectTransform panel;
        MechanicIntroPopup popup = PopupBuilder.NewPopup<MechanicIntroPopup>(popups, "MechanicIntroPopup", out panel);
        Image body = panel.GetComponent<Image>();
        body.sprite = PopupBuilder.LoadSprite(BubbleSprite);
        body.type = Image.Type.Sliced;
        panel.sizeDelta = new Vector2(940, 940);
        panel.anchoredPosition = new Vector2(0, 60);

        TMP_Text title = PopupBuilder.GameText(panel, "Title", "Orientation", new Vector2(0, 370), new Vector2(800, 110), 84, BubbleText);
        title.enableWordWrapping = false;
        title.enableAutoSizing = true;
        title.fontSizeMin = 50;
        title.fontSizeMax = 84;
        TMP_Text message = PopupBuilder.GameText(panel, "Message", "Blocks must be vertical, square, or horizontal.",
            new Vector2(0, 255), new Vector2(780, 120), 44, BubbleText);
        message.enableWordWrapping = true;
        message.enableAutoSizing = true;
        message.fontSizeMin = 30;
        message.fontSizeMax = 44;

        RectTransform pic = PopupBuilder.NewUI("Picture", panel);
        pic.anchoredPosition = new Vector2(0, -15);
        pic.sizeDelta = new Vector2(800, 400);
        Image picImg = pic.gameObject.AddComponent<Image>();
        picImg.sprite = PopupBuilder.LoadSprite(ConstraintPicture);
        picImg.preserveAspect = true;
        picImg.raycastTarget = false;

        RectTransform face;
        Button ok = PopupBuilder.FramedButton(panel, "OkButton", PopupBuilder.UI + "Buttons/btn_green_2.png", PopupBuilder.UI + "Buttons/btn_green_1.png",
            new Vector2(0, -335), new Vector2(340, 140), out face);
        TMP_Text label = PopupBuilder.GameText(face, "Label", "Ok", Vector2.zero, new Vector2(280, 100), 64, Color.white);
        label.fontSharedMaterial = GameFontSetup.OutlineGreen();

        PopupBuilder.Assign(popup, "titleText", title);
        PopupBuilder.Assign(popup, "messageText", message);
        PopupBuilder.Assign(popup, "picture", picImg);
        PopupBuilder.Assign(popup, "okButton", ok);
        popup.gameObject.SetActive(false);
        return popup;
    }

    /// <summary>
    /// Canvas_HUD/BoosterTutorial (tắt sẵn): Canvas riêng sortingOrder 5 phủ cả màn hình (ngoài safe area),
    /// nền tối chặn thao tác, khung lời nhắc khung_reveal2 đè lên khung LEVEL, bàn tay (đầu ngón ở pivot).
    /// </summary>
    private static BoosterTutorialView BuildBoosterPanel(Transform hudCanvas)
    {
        const string name = "BoosterTutorial";
        PopupBuilder.DestroyChild(hudCanvas, name);
        RectTransform root = PopupBuilder.Stretch(PopupBuilder.NewUI(name, hudCanvas));
        root.SetAsLastSibling();
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5;
        root.gameObject.AddComponent<GraphicRaycaster>();
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        RectTransform dim = PopupBuilder.Stretch(PopupBuilder.NewUI("Dim", root));
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.75f);
        dimImg.raycastTarget = true; // chặn lưới (world) và các nút HUD khác

        RectTransform bubble = PopupBuilder.NewUI("Bubble", root);
        AnchorTop(bubble, new Vector2(0, -180)); // đè lên khung LEVEL (đang tối) như bản gốc, không che lưới
        bubble.sizeDelta = new Vector2(940, 180);
        Image bubbleImg = bubble.gameObject.AddComponent<Image>();
        bubbleImg.sprite = PopupBuilder.LoadSprite(RevealBannerSprite);
        bubbleImg.raycastTarget = false;
        TMP_Text message = PopupBuilder.Text(bubble, "Message", "Hint will automatically complete one correct region!",
            Vector2.zero, Vector2.zero, 46, Color.white);
        RectTransform msgRect = PopupBuilder.Stretch(message.rectTransform);
        msgRect.offsetMin = new Vector2(60, 24);
        msgRect.offsetMax = new Vector2(-60, -24);
        message.enableWordWrapping = true;
        message.enableAutoSizing = true;
        message.fontSizeMin = 30;
        message.fontSizeMax = 46;

        RectTransform hand = PopupBuilder.NewUI("Hand", root);
        Image handImg = hand.gameObject.AddComponent<Image>();
        handImg.sprite = PopupBuilder.LoadSprite(HandSprite);
        handImg.raycastTarget = false;
        handImg.SetNativeSize();
        hand.pivot = new Vector2(0.11f, 0.93f); // đầu ngón trỏ

        BoosterTutorialView view = root.gameObject.AddComponent<BoosterTutorialView>();
        PopupBuilder.Assign(view, "group", group);
        PopupBuilder.Assign(view, "canvas", canvas);
        PopupBuilder.Assign(view, "message", message);
        PopupBuilder.Assign(view, "bubble", bubble);
        PopupBuilder.Assign(view, "hand", hand);
        root.gameObject.SetActive(false);
        return view;
    }

    private static TutorialView BuildPanel(Transform root)
    {
        PopupBuilder.DestroyChild(root, PanelName);
        RectTransform panel = PopupBuilder.Stretch(PopupBuilder.NewUI(PanelName, root));
        panel.SetAsLastSibling();
        CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.interactable = true;

        // Tiêu đề + Skip
        TMP_Text title = PopupBuilder.GameText(panel, "Title", "Tutorial", Vector2.zero, new Vector2(600, 90), 64, Color.white);
        AnchorTop(title.rectTransform, new Vector2(0, -110));

        RectTransform skipRect = PopupBuilder.NewUI("SkipButton", panel);
        AnchorTop(skipRect, new Vector2(410, -125));
        skipRect.sizeDelta = new Vector2(180, 90);
        Image skipHit = skipRect.gameObject.AddComponent<Image>();
        skipHit.color = new Color(1, 1, 1, 0); // vùng chạm trong suốt
        Button skip = skipRect.gameObject.AddComponent<Button>();
        skip.transition = Selectable.Transition.None;
        TMP_Text skipText = PopupBuilder.GameText(skipRect, "Label", "Skip", Vector2.zero, new Vector2(180, 90), 44, Color.white);
        PopupBuilder.Stretch(skipText.rectTransform);

        // Vạch tiến độ
        RectTransform dotsRoot = PopupBuilder.NewUI("Dots", panel);
        AnchorTop(dotsRoot, new Vector2(0, -205));
        dotsRoot.sizeDelta = new Vector2(400, 20);
        HorizontalLayoutGroup layout = dotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        Image[] dots = new Image[TutorialLevels.Create().Count];
        for (int i = 0; i < dots.Length; i++)
        {
            RectTransform d = PopupBuilder.NewUI($"Dot{i + 1}", dotsRoot);
            d.sizeDelta = new Vector2(64, 18);
            dots[i] = d.gameObject.AddComponent<Image>();
            dots[i].sprite = PopupBuilder.LoadSprite(DotSprite);
            dots[i].raycastTarget = false;
        }

        // Khung lời nhắc
        RectTransform bubble = PopupBuilder.NewUI("Bubble", panel);
        AnchorTop(bubble, new Vector2(0, -360));
        bubble.sizeDelta = new Vector2(940, 200);
        Image bubbleImg = bubble.gameObject.AddComponent<Image>();
        bubbleImg.sprite = PopupBuilder.LoadSprite(BubbleSprite);
        bubbleImg.type = Image.Type.Sliced;
        bubbleImg.raycastTarget = false;
        TMP_Text message = PopupBuilder.GameText(bubble, "Message", "First, drag to create a rectangle with 3 cells.",
            Vector2.zero, Vector2.zero, 44, BubbleText);
        RectTransform msgRect = PopupBuilder.Stretch(message.rectTransform);
        msgRect.offsetMin = new Vector2(60, 34);
        msgRect.offsetMax = new Vector2(-60, -26);
        message.enableWordWrapping = true;
        message.enableAutoSizing = true;
        message.fontSizeMin = 30;
        message.fontSizeMax = 44;

        TutorialView view = panel.gameObject.AddComponent<TutorialView>();
        PopupBuilder.Assign(view, "group", group);
        PopupBuilder.Assign(view, "message", message);
        PopupBuilder.Assign(view, "bubble", bubble);
        PopupBuilder.Assign(view, "skipButton", skip);

        SerializedObject so = new SerializedObject(view);
        SerializedProperty dotsProp = so.FindProperty("dots");
        dotsProp.arraySize = dots.Length;
        for (int i = 0; i < dots.Length; i++) dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];

        // Panel HUD thường ẩn trong lúc tutorial
        string[] hide = { "TopPanel", "DownPanel" };
        SerializedProperty hideProp = so.FindProperty("hideWhileActive");
        hideProp.arraySize = 0;
        foreach (string n in hide)
        {
            Transform t = root.Find(n);
            if (t == null) continue;
            hideProp.arraySize++;
            hideProp.GetArrayElementAtIndex(hideProp.arraySize - 1).objectReferenceValue = t.gameObject;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        return view;
    }

    private static TutorialHand BuildHand()
    {
        GameObject old = PopupBuilder.FindRoot(HandName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject go = new GameObject(HandName);
        Undo.RegisterCreatedObjectUndo(go, "Build Tutorial");
        go.transform.localScale = Vector3.one * 0.75f;

        // Đầu ngón trỏ nằm ở góc trên trái sprite (~11%, 7% tính từ trên trái) -> dời sprite để đầu ngón ở gốc
        GameObject spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(go.transform, false);
        SpriteRenderer sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandSprite);
        sr.sortingOrder = 80; // trên khối mèo (20–24), bong bóng số ô (40–41), chữ khen (60)
        if (sr.sprite != null)
        {
            Vector2 size = sr.sprite.bounds.size;
            spriteGo.transform.localPosition = new Vector3(size.x * (0.5f - 0.11f), -size.y * (0.5f - 0.07f), 0f);
        }

        TutorialHand hand = go.AddComponent<TutorialHand>();
        PopupBuilder.Assign(hand, "sprite", sr);
        go.SetActive(false);
        return hand;
    }

    private static void AnchorTop(RectTransform rt, Vector2 pos)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }
}
