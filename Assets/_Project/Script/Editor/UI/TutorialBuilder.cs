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
/// </summary>
public static class TutorialBuilder
{
    private const string HandSprite = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/tutorial_hand.png";
    private const string DotSprite = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/tutorial_dot.png";
    private const string BubbleSprite = "Assets/_Project/GameAssets/04_UI_Graphics/Popups_Dialogs/popup_tutorial.png";
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

        EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
        Selection.activeObject = view.gameObject;
        Debug.Log("[TutorialBuilder] Đã dựng TutorialPanel, TutorialHand, gắn TutorialController.");
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
