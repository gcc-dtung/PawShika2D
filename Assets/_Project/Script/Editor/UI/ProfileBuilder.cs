using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng hồ sơ người chơi trong scene đang mở (chạy lại được):
/// - Configs/AvatarDatabase.asset: 12 mèo ShikaCat (Cats_InGame), thứ tự = AvatarId (chỉ thêm vào cuối).
/// - Canvas_Popup/Popups/ProfilePopup: khung như popup cài đặt; thẻ trắng (avatar, tên + EDIT, ID, ngày bắt đầu, huy hiệu LVL),
///   ruy băng STATISTICS, lưới 3 x 2 ô thống kê; lớp chọn avatar 4 x 3. Gán vào GameFlowUI.profilePopup.
/// - Canvas_Menu .../Avatar: khung + mèo đang chọn + AvatarButton (bấm mở hồ sơ).
/// - ProfileStatsRecorder trên Manager.
/// </summary>
public static class ProfileBuilder
{
    private const string UI = PopupBuilder.UI;
    private const string Frames = UI + "Frames_Borders/";
    private const string Icons = UI + "Icons/";
    private const string Cats = "Assets/_Project/ShikaCat_GameAssets/02_Cats_And_Decoration/Cats_InGame/";
    private const string AvatarDbPath = "Assets/_Project/Configs/AvatarDatabase.asset";

    private static readonly string[] CatNames =
    {
        "Orange", "Blue", "Pink", "Green", "Purple", "Yellow", "Red", "Teal", "BabyBlue", "Lime", "ElectricBlue", "WarmPink"
    };

    // Icon theo thứ tự ProfileStatType
    private static readonly string[] StatIcons =
    {
        Icons + "icon_cup.png",                                                         // FirstTryWins
        Icons + "coin.png",                                                             // CoinsEarned
        Icons + "star.png",                                                             // LevelsCleared
        Frames + "khung_hard_2.png",                                                    // HardWins
        "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/btn_hint.png", // BoostersUsed
        Icons + "heart_1.png"                                                           // BestWinStreak
    };

    private static readonly Color Purple = PopupBuilder.SettingLabel;
    private static readonly Color Muted = new Color(0.5f, 0.45f, 0.7f);

    [MenuItem("Tools/PawBlock/Builders/Build Profile", priority = 112)]
    public static void Build()
    {
        GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
        Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
        GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
        if (flow == null)
        {
            Debug.LogError("[ProfileBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
            return;
        }

        AvatarDatabase db = EnsureAvatarDatabase();

        PopupBuilder.DestroyChild(popups, "ProfilePopup");
        ProfilePopup popup = BuildPopup(popups, db);
        popup.gameObject.SetActive(false);
        PopupBuilder.Assign(flow, "profilePopup", popup);

        GameObject menu = PopupBuilder.FindRoot("Canvas_Menu");
        if (menu != null)
        {
            // Lấy danh sách nút trước: SetupMenuAvatar xóa / tạo con nên không duyệt Transform trực tiếp
            foreach (Button b in menu.GetComponentsInChildren<Button>(true))
            {
                if (b.name == "Avatar") SetupMenuAvatar((RectTransform)b.transform, db, flow);
            }
        }

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null && lm.GetComponent<ProfileStatsRecorder>() == null) Undo.AddComponent<ProfileStatsRecorder>(lm.gameObject);

        EditorSceneManager.MarkSceneDirty(popupCanvas.scene);
        Debug.Log("[ProfileBuilder] Đã dựng ProfilePopup, avatar Main Menu, AvatarDatabase.");
    }

    // =====================================================================
    // Avatar database

    internal static AvatarDatabase EnsureAvatarDatabase()
    {
        AvatarDatabase db = AssetDatabase.LoadAssetAtPath<AvatarDatabase>(AvatarDbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AvatarDatabase>();
            AssetDatabase.CreateAsset(db, AvatarDbPath);
        }
        if (db.Count == 0)
        {
            db.avatars = new Sprite[CatNames.Length];
            for (int i = 0; i < CatNames.Length; i++) db.avatars[i] = PopupBuilder.LoadSprite(Cats + "Image_Cat_" + CatNames[i] + ".png");
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }
        return db;
    }

    // =====================================================================
    // Avatar trên Main Menu

    /// <summary>Khung tím bo góc + mèo đang chọn; gắn AvatarButton. MainMenuBuilder gọi lại khi dựng Menu.</summary>
    internal static void SetupMenuAvatar(RectTransform root, AvatarDatabase db, GameFlowUI flow)
    {
        Image frame = root.GetComponent<Image>();
        frame.sprite = PopupBuilder.LoadSprite(Frames + "khung3.png");
        frame.preserveAspect = true;
        PopupBuilder.DestroyChild(root, "Cat");
        Image cat = PopupBuilder.Icon(root, "Cat", null, root.sizeDelta.x * 0.74f);
        cat.sprite = db != null ? db.Get(0) : null;
        cat.rectTransform.anchoredPosition = new Vector2(0f, -2f);

        AvatarButton button = root.GetComponent<AvatarButton>();
        if (button == null) button = Undo.AddComponent<AvatarButton>(root.gameObject);
        PopupBuilder.Assign(button, "avatars", db);
        PopupBuilder.Assign(button, "avatarImage", cat);
        if (flow != null) PopupBuilder.Assign(button, "gameFlow", flow);
    }

    // =====================================================================
    // Popup

    private static ProfilePopup BuildPopup(Transform root, AvatarDatabase db)
    {
        ProfilePopup popup = PopupBuilder.FramedPopup<ProfilePopup>(root, "ProfilePopup", "PROFILE", new Vector2(980, 1500), 40,
            out RectTransform panel, out Button close);

        // ---------- Thẻ người chơi ----------
        RectTransform card = PopupBuilder.WhiteBox(panel, "Card", new Vector2(0, 430), new Vector2(880, 380));

        RectTransform avatarRoot = PopupBuilder.NewUI("Avatar", card);
        avatarRoot.anchoredPosition = new Vector2(-280, 20);
        avatarRoot.sizeDelta = new Vector2(250, 250);
        Image avatarFrame = avatarRoot.gameObject.AddComponent<Image>();
        avatarFrame.sprite = PopupBuilder.LoadSprite(Frames + "khung3.png");
        Button avatarButton = avatarRoot.gameObject.AddComponent<Button>();
        avatarButton.targetGraphic = avatarFrame;
        ColorBlock colors = avatarButton.colors;
        colors.disabledColor = Color.white; // hồ sơ người khác: không bấm được nhưng không tối đi
        avatarButton.colors = colors;
        Image avatarImage = PopupBuilder.Icon(avatarRoot, "Cat", null, 190);
        avatarImage.sprite = db != null ? db.Get(0) : null;
        GameObject avatarBadge = SmallLabelPill(avatarRoot, "ChangeBadge", "CHANGE", new Vector2(0, -122), new Vector2(170, 56), 30).gameObject;

        TMP_Text name = PopupBuilder.GameText(card, "Name", "Player", new Vector2(45, 105), new Vector2(330, 90), 60, Purple);
        name.alignment = TextAlignmentOptions.Left;
        name.enableAutoSizing = true;
        name.fontSizeMin = 36;
        name.fontSizeMax = 60;
        name.enableWordWrapping = false;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.richText = false; // tên người chơi hiện nguyên văn (kể cả "<b>")

        TMP_InputField input = BuildNameInput(card, new Vector2(45, 105), new Vector2(340, 90));

        Button editName = PopupBuilder.FramedButton(card, "EditName", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
            new Vector2(326, 105), new Vector2(204, 106), out RectTransform editFace);
        TMP_Text editText = PopupBuilder.GameText(editFace, "Label", "EDIT", new Vector2(0, 2), new Vector2(190, 90), 56, Color.white);
        editText.fontSharedMaterial = GameFontSetup.OutlineGreenBold();
        editText.enableWordWrapping = false;

        TMP_Text id = PopupBuilder.GameText(card, "Id", "ID: 00000000", new Vector2(70, 22), new Vector2(380, 60), 36, Muted);
        id.alignment = TextAlignmentOptions.Left;

        Image calendar = PopupBuilder.Icon(card, "Calendar", Icons + "icon_daily.png", 72);
        calendar.rectTransform.anchoredPosition = new Vector2(-84, -62);
        TMP_Text joined = PopupBuilder.GameText(card, "Joined", "10/2026", new Vector2(110, -62), new Vector2(260, 60), 44, Purple);
        joined.alignment = TextAlignmentOptions.Left;

        // Huy hiệu LVL
        Image badge = PopupBuilder.Icon(card, "LevelBadge", Icons + "daily_back.png", 190);
        badge.rectTransform.anchoredPosition = new Vector2(330, -75);
        TMP_Text lvlLabel = PopupBuilder.GameText(badge.transform, "Label", "LVL", new Vector2(0, 46), new Vector2(160, 50), 36, Color.white);
        lvlLabel.fontSharedMaterial = GameFontSetup.OutlinePurple();
        TMP_Text lvl = PopupBuilder.GameText(badge.transform, "Value", "1", new Vector2(0, -8), new Vector2(170, 90), 72, Color.white);
        lvl.fontSharedMaterial = GameFontSetup.OutlinePurple();
        lvl.enableAutoSizing = true;
        lvl.fontSizeMin = 40;
        lvl.fontSizeMax = 72;

        // ---------- Thống kê ----------
        Image ribbon = PopupBuilder.Icon(panel, "StatsRibbon", Icons + "daily_ribbon.png", 560);
        ribbon.rectTransform.anchoredPosition = new Vector2(0, 175);
        ribbon.rectTransform.sizeDelta = new Vector2(560, 170);
        ribbon.preserveAspect = false;
        // Dải ruy băng nằm ở phần dưới ảnh (tâm dải ~27 % chiều cao dưới tâm ảnh)
        TMP_Text ribbonText = PopupBuilder.GameText(ribbon.transform, "Title", "STATISTICS", new Vector2(0, -44), new Vector2(400, 70), 50, Color.white);
        ribbonText.fontSharedMaterial = GameFontSetup.OutlinePurple();

        ProfileStatTile[] tiles = new ProfileStatTile[6];
        for (int i = 0; i < tiles.Length; i++)
        {
            Vector2 pos = new Vector2(-292 + (i % 3) * 292, -110 - (i / 3) * 330);
            tiles[i] = BuildTile(panel, "Stat_" + i, pos, StatIcons[i]);
        }
        Sprite[] icons = new Sprite[StatIcons.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = PopupBuilder.LoadSprite(StatIcons[i]);

        // ---------- Chọn avatar ----------
        GameObject picker = BuildPicker(panel, db, out Button[] pickerButtons, out GameObject[] selected, out Button pickerClose);

        Assign(popup, "avatars", db);
        Assign(popup, "avatarImage", avatarImage);
        Assign(popup, "avatarButton", avatarButton);
        Assign(popup, "avatarEditBadge", avatarBadge);
        Assign(popup, "nameText", name);
        Assign(popup, "editNameButton", editName);
        Assign(popup, "nameInput", input);
        Assign(popup, "idText", id);
        Assign(popup, "joinedText", joined);
        Assign(popup, "levelText", lvl);
        Assign(popup, "avatarPicker", picker);
        Assign(popup, "pickerCloseButton", pickerClose);
        Assign(popup, "closeButton", close);

        SerializedObject so = new SerializedObject(popup);
        SetArray(so.FindProperty("tiles"), tiles);
        SetArray(so.FindProperty("statIcons"), icons);
        SetArray(so.FindProperty("pickerButtons"), pickerButtons);
        SetArray(so.FindProperty("pickerSelected"), selected);
        so.ApplyModifiedPropertiesWithoutUndo();
        return popup;
    }

    /// <summary>Ô thống kê: nền tím nhạt bo góc, icon, nhãn 2 dòng, số trong ô kem.</summary>
    private static ProfileStatTile BuildTile(Transform parent, string name, Vector2 pos, string iconPath)
    {
        PopupBuilder.EnsureSliced(Frames + "khung_popup_4.png", new Vector4(50, 50, 50, 50));
        RectTransform rt = PopupBuilder.NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(270, 310);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(Frames + "khung_popup_4.png");
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        Image icon = PopupBuilder.Icon(rt, "Icon", iconPath, 110);
        icon.rectTransform.anchoredPosition = new Vector2(0, 82);

        TMP_Text label = PopupBuilder.GameText(rt, "Label", "LABEL", new Vector2(0, -10), new Vector2(240, 76), 30, Purple);
        label.enableAutoSizing = true;
        label.fontSizeMin = 22;
        label.fontSizeMax = 30;
        label.lineSpacing = -12;

        RectTransform pill = SmallLabelPill(rt, "ValuePill", null, new Vector2(0, -100), new Vector2(220, 74), 0);
        TMP_Text value = PopupBuilder.GameText(pill, "Value", "0", Vector2.zero, new Vector2(200, 70), 50, Purple);
        value.enableAutoSizing = true;
        value.fontSizeMin = 28;
        value.fontSizeMax = 50;
        value.enableWordWrapping = false;

        ProfileStatTile tile = rt.gameObject.AddComponent<ProfileStatTile>();
        Assign(tile, "icon", icon);
        Assign(tile, "label", label);
        Assign(tile, "value", value);
        return tile;
    }

    /// <summary>Ô kem bo tròn (khung_pill_kem); có <paramref name="text"/> thì thêm chữ tím.</summary>
    private static RectTransform SmallLabelPill(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
    {
        RectTransform rt = PopupBuilder.NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = PopupBuilder.LoadSprite(Frames + "khung_pill_kem.png");
        img.raycastTarget = false;
        if (text != null)
        {
            TMP_Text t = PopupBuilder.GameText(rt, "Label", text, Vector2.zero, size, fontSize, Purple);
            t.enableWordWrapping = false;
        }
        return rt;
    }

    /// <summary>Ô nhập tên (TMP_InputField) đè đúng chỗ chữ tên, tắt sẵn; nền kem.</summary>
    private static TMP_InputField BuildNameInput(Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform rt = PopupBuilder.NewUI("NameInput", parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(Frames + "khung_pill_kem.png");

        RectTransform area = PopupBuilder.NewUI("Text Area", rt);
        PopupBuilder.Stretch(area);
        area.offsetMin = new Vector2(24, 6);
        area.offsetMax = new Vector2(-24, -6);
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text text = PopupBuilder.GameText(area, "Text", "", Vector2.zero, Vector2.zero, 50, Purple);
        PopupBuilder.Stretch((RectTransform)text.transform);
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;

        TMP_InputField input = rt.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = text;
        input.targetGraphic = bg;
        input.characterLimit = PlayerProfileService.NameMaxLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.richText = false;
        text.richText = false;
        input.fontAsset = GameFontSetup.Font;
        input.pointSize = 50;
        input.caretColor = Purple;
        rt.gameObject.SetActive(false);
        return input;
    }

    /// <summary>Lớp chọn avatar phủ thân popup: nền tối, khung trắng, lưới 4 x 3 mèo, viền vàng ở mèo đang chọn, nút X.</summary>
    private static GameObject BuildPicker(Transform panel, AvatarDatabase db, out Button[] buttons, out GameObject[] selected, out Button close)
    {
        RectTransform rt = PopupBuilder.Stretch(PopupBuilder.NewUI("AvatarPicker", panel));
        Image dim = rt.gameObject.AddComponent<Image>();
        dim.color = new Color(0.12f, 0.08f, 0.25f, 0.85f); // chặn bấm xuống thẻ bên dưới

        RectTransform box = PopupBuilder.WhiteBox(rt, "Box", new Vector2(0, 40), new Vector2(880, 1000));
        TMP_Text title = PopupBuilder.GameText(box, "Title", "CHOOSE AVATAR", new Vector2(0, 410), new Vector2(700, 100), 60, Purple);
        title.enableWordWrapping = false;

        int n = db != null ? db.Count : 0;
        buttons = new Button[n];
        selected = new GameObject[n];
        PopupBuilder.EnsureSliced(Frames + "rounded_white.png", new Vector4(40, 40, 40, 40));
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = new Vector2(-300 + (i % 4) * 200, 230 - (i / 4) * 220);

            RectTransform cell = PopupBuilder.NewUI("Avatar_" + i, box);
            cell.anchoredPosition = pos;
            cell.sizeDelta = new Vector2(180, 180);

            RectTransform ring = PopupBuilder.NewUI("Selected", cell);
            ring.sizeDelta = new Vector2(204, 204);
            Image ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = PopupBuilder.LoadSprite(Frames + "rounded_white.png");
            ringImg.type = Image.Type.Sliced;
            ringImg.color = PopupBuilder.Yellow;
            ringImg.raycastTarget = false;
            ring.gameObject.SetActive(false);
            selected[i] = ring.gameObject;

            Image frame = cell.gameObject.AddComponent<Image>();
            frame.sprite = PopupBuilder.LoadSprite(Frames + "khung3.png");
            Button b = cell.gameObject.AddComponent<Button>();
            b.targetGraphic = frame;
            buttons[i] = b;
            ring.SetAsFirstSibling();

            Image cat = PopupBuilder.Icon(cell, "Cat", null, 140);
            cat.sprite = db.avatars[i];
        }

        close = PopupBuilder.FramedButton(box, "Done", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
            new Vector2(0, -400), new Vector2(330, 130), out RectTransform face);
        TMP_Text label = PopupBuilder.GameText(face, "Label", "DONE", Vector2.zero, new Vector2(300, 100), 56, Color.white);
        label.fontSharedMaterial = GameFontSetup.OutlineGreen();

        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    // =====================================================================

    private static void Assign(Object target, string field, Object value) => PopupBuilder.Assign(target, field, value);

    private static void SetArray<T>(SerializedProperty prop, T[] values) where T : Object
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
