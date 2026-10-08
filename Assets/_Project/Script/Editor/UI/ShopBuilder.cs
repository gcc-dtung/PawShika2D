using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab Shop như bản gốc (Tools → PawBlock → Builders → Build Shop; Build Main Menu cũng gọi BuildPanel):
/// mái hiên + ô năng lượng / coin trên cùng (đè lên nội dung cuộn); danh sách cuộn dọc (NestedScrollRect: vuốt ngang vẫn chuyển tab):
/// Starter Pack (tem 90% Off) → "Remove Ads" (dòng No more disruptive ads, thẻ No Ads Bundle) → "Special Bundle"
/// (Fail / Mini / Super / Mega / Deluxe Offer) → "Coin" (lưới 3 x 2). Dữ liệu ShopDatabase.asset (chép số từ *PackShop.asset gốc,
/// giá VND theo video); thẻ đọc dữ liệu lúc chạy (ShopCardView). Bấm giá -> ShopService.Buy (chưa SDK: "Purchase Failed!").
/// Tọa độ trong thẻ tính theo pixel khung gốc (frame_pack / frame_starter 1008x521, frame_coin 323x421, gốc trên trái).
/// </summary>
public static class ShopBuilder
{
    private const string UI = PopupBuilder.UI;
    private const string Frames = UI + "Frames_Borders/";
    private const string Icons = UI + "Icons/";
    private const string Packs = UI + "Shop_Packs/";
    private const string DatabasePath = GameConfigSetup.Folder + "ShopDatabase.asset";
    private const string BoosterDbPath = GameConfigSetup.Folder + "BoosterDatabase.asset";
    private const string Prefix = "com.phoenix.pawblock.puzzle.";

    private static readonly Vector2 CardSize = new Vector2(1008, 521);
    private static readonly Vector2 CoinSize = new Vector2(323, 421);
    private static readonly Color CoinYellow = new Color32(0xFF, 0xD8, 0x3A, 0xFF);

    [MenuItem("Tools/PawBlock/Builders/Build Shop", priority = 108)]
    public static void Build()
    {
        MainMenuNavigator nav = Object.FindFirstObjectByType<MainMenuNavigator>(FindObjectsInactive.Include);
        Transform panels = nav != null ? nav.transform.Find("Panels") : null;
        if (panels == null)
        {
            Debug.LogError("[ShopBuilder] Không tìm thấy Canvas_Menu/Panels (Build Main Menu trước).");
            return;
        }
        Transform old = panels.Find("ShopPanel");
        Vector2 pos = old != null ? ((RectTransform)old).anchoredPosition : Vector2.zero;
        bool active = old == null || old.gameObject.activeSelf;
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        RectTransform shop = BuildPanel(panels);
        shop.SetSiblingIndex(0);
        shop.anchoredPosition = pos;
        shop.gameObject.SetActive(active);
        if (shop.GetComponent<RectMask2D>() == null) shop.gameObject.AddComponent<RectMask2D>();

        SerializedObject so = new SerializedObject(nav);
        so.FindProperty("panels").GetArrayElementAtIndex(0).objectReferenceValue = shop;
        so.ApplyModifiedPropertiesWithoutUndo();
        Wire(shop, nav);
        EditorSceneManager.MarkSceneDirty(nav.gameObject.scene);
        Debug.Log("[ShopBuilder] Đã dựng tab Shop.");
    }

    /// <summary>Nối ô coin (bấm → Shop) và ô năng lượng (bấm → nạp) của Shop.</summary>
    internal static void Wire(Transform shop, MainMenuNavigator nav)
    {
        GameFlowUI flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
        foreach (CoinHUD c in shop.GetComponentsInChildren<CoinHUD>(true)) PopupBuilder.Assign(c, "navigator", nav);
        if (flow == null) return;
        foreach (EnergyHUD e in shop.GetComponentsInChildren<EnergyHUD>(true)) PopupBuilder.Assign(e, "gameFlow", flow);
    }

    internal static RectTransform BuildPanel(Transform parent)
    {
        ShopDatabase db = EnsureDatabase();
        BoosterDatabase boosters = AssetDatabase.LoadAssetAtPath<BoosterDatabase>(BoosterDbPath);

        RectTransform panel = PopupBuilder.Stretch(PopupBuilder.NewUI("ShopPanel", parent));
        panel.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.19f, 0.42f);

        // ---------- Danh sách cuộn ----------
        RectTransform scrollRt = PopupBuilder.Stretch(PopupBuilder.NewUI("Scroll", panel));
        Image hit = scrollRt.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f); // nhận kéo ở khoảng trống
        NestedScrollRect scroll = scrollRt.gameObject.AddComponent<NestedScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 40f;

        RectTransform content = PopupBuilder.NewUI("Content", scrollRt);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(1080f, 0f);
        VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(0, 0, 400, 330);
        v.spacing = 26f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = v.childControlHeight = false;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = scrollRt;
        scroll.content = content;

        foreach (ShopProduct p in db.products)
        {
            if (p.section == ShopSection.Starter) Card(content, db, boosters, p, Frames + "frame_starter.png", true);
        }
        Header(content, "Remove Ads", Packs + "title_ads.png", new Color32(0x5B, 0x2A, 0xB0, 0xFF));
        foreach (ShopProduct p in db.products)
        {
            if (p.section == ShopSection.RemoveAds) RemoveAdsRow(content, db, p);
        }
        foreach (ShopProduct p in db.products)
        {
            if (p.section == ShopSection.NoAdsBundle) Card(content, db, boosters, p, Frames + "frame_pack_ads1.png", false);
        }
        Header(content, "Special Bundle", Packs + "title_bundle.png", new Color32(0x30, 0x33, 0xA8, 0xFF));
        foreach (ShopProduct p in db.products)
        {
            if (p.section == ShopSection.Bundle) Card(content, db, boosters, p, Frames + "frame_pack.png", false);
        }
        Header(content, "Coin", Packs + "title_coin.png", new Color32(0xA8, 0x5A, 0x08, 0xFF));
        CoinGrid(content, db);

        // ---------- Mái hiên + ô năng lượng / coin (đè lên nội dung cuộn) ----------
        for (int i = 0; i < 4; i++)
        {
            RectTransform awning = PopupBuilder.NewUI("Awning_" + i, panel);
            awning.anchorMin = awning.anchorMax = new Vector2(0f, 1f);
            awning.pivot = new Vector2(0f, 0f);
            awning.sizeDelta = new Vector2(270f, 353f);
            awning.anchoredPosition = new Vector2(i * 270f, -300f);
            Image img = awning.gameObject.AddComponent<Image>();
            img.sprite = PopupBuilder.LoadSprite(UI + "Backgrounds/bg_shop_1.png");
            img.raycastTarget = false;
        }
        RectTransform zone = UIZoneBuilder.Ensure(panel, true, false, false, false);
        zone.SetAsLastSibling();
        RectTransform topBar = PopupBuilder.NewUI("TopBar", zone);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 260f);
        // Như bản gốc: 2 ô thấp hơn Home một chút (nằm trên mái hiên)
        RectTransform energy = (RectTransform)MainMenuBuilder.BuildEnergyPill(topBar).transform;
        RectTransform coinPill = (RectTransform)MainMenuBuilder.BuildCoinPill(topBar).transform;
        energy.anchoredPosition = new Vector2(energy.anchoredPosition.x, -170f);
        coinPill.anchoredPosition = new Vector2(coinPill.anchoredPosition.x, -170f);
        return panel;
    }

    // ---------- Thẻ gói (Starter / No Ads Bundle / Special Bundle) ----------

    private static Vector2 P(float x, float y) => new Vector2(x - CardSize.x * 0.5f, CardSize.y * 0.5f - y);

    private static void Card(Transform content, ShopDatabase db, BoosterDatabase boosters, ShopProduct product, string frame, bool starter)
    {
        RectTransform root = Fixed(PopupBuilder.NewUI(product.title.Replace("\n", " "), content), CardSize);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(frame);

        Image icon = PopupBuilder.Icon(root, "Icon", null, 330);
        icon.rectTransform.anchoredPosition = starter ? P(215, 165) : P(240, 182);
        icon.rectTransform.sizeDelta = starter ? new Vector2(360, 290) : new Vector2(350, 260);

        // Ô coin (x 454–632, y 56–298)
        Image coin = PopupBuilder.Icon(root, "Coin", Icons + "icon_coin_1.png", 140);
        coin.rectTransform.anchoredPosition = P(543, 140);
        TMP_Text coinText = PopupBuilder.GameText(root, "CoinAmount", "0", P(543, 252), new Vector2(190, 70), 56, CoinYellow);
        coinText.fontSharedMaterial = GameFontSetup.OutlineBrown();
        PopupBuilder.Fit(coinText);

        // Ô vật phẩm 2x2 (x 664–943, y 56–298)
        GameObject hint, shield, reveal, inf;
        TMP_Text hintT, shieldT, revealT, infT;
        Item(root, "Hint", BoosterIcon(boosters, BoosterType.Hint), P(737, 115), false, out hint, out hintT);
        Item(root, "Shield", BoosterIcon(boosters, BoosterType.Shield), P(873, 115), false, out shield, out shieldT);
        Item(root, "Reveal", BoosterIcon(boosters, BoosterType.Reveal), P(737, 235), false, out reveal, out revealT);
        Item(root, "Infinite", PopupBuilder.LoadSprite(PopupBuilder.EnergyInfiniteSprite), P(873, 235), true, out inf, out infT);

        // Tên + nút giá (thanh dưới, y 312–480)
        TMP_Text title = PopupBuilder.GameText(root, "Title", product.title, P(330, 400), new Vector2(560, 100), 62, Color.white);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.fontSharedMaterial = starter ? GameFontSetup.OutlineBrown() : GameFontSetup.OutlinePurple();
        title.enableWordWrapping = false;
        TMP_Text price;
        Button buy = PriceButton(root, P(800, 402), new Vector2(300, 112), out price);

        TMP_Text sale = null;
        if (starter)
        {
            Image tag = PopupBuilder.Icon(root, "SaleTag", Packs + "sale_pop2.png", 168);
            tag.rectTransform.sizeDelta = new Vector2(131, 168);
            tag.rectTransform.anchoredPosition = P(80, 62);
            tag.rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);
            sale = PopupBuilder.GameText(tag.transform, "Text", "90%\nOff", new Vector2(4, -22), new Vector2(120, 110), 34, Color.white);
            sale.fontSharedMaterial = GameFontSetup.OutlineRed();
            sale.lineSpacing = -18;
        }

        ShopCardView view = root.gameObject.AddComponent<ShopCardView>();
        SerializedObject so = new SerializedObject(view);
        Set(so, "database", db);
        so.FindProperty("productId").stringValue = product.productId;
        Set(so, "titleText", title);
        Set(so, "priceText", price);
        Set(so, "buyButton", buy);
        Set(so, "icon", icon);
        Set(so, "coinText", coinText);
        Set(so, "hintItem", hint); Set(so, "hintText", hintT);
        Set(so, "shieldItem", shield); Set(so, "shieldText", shieldT);
        Set(so, "revealItem", reveal); Set(so, "revealText", revealT);
        Set(so, "infiniteItem", inf); Set(so, "infiniteText", infT);
        Set(so, "saleText", sale);
        so.FindProperty("hideWhenNoAds").boolValue = product.removeAds && product.section == ShopSection.RemoveAds;
        so.ApplyModifiedPropertiesWithoutUndo();
        icon.sprite = product.icon;
    }

    private static void Item(Transform card, string name, Sprite sprite, Vector2 pos, bool infinite, out GameObject item, out TMP_Text text)
    {
        RectTransform rt = PopupBuilder.NewUI(name, card);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120, 110);
        Image img = PopupBuilder.Icon(rt, "Icon", null, 96);
        img.sprite = sprite;
        text = PopupBuilder.GameText(rt, "Amount", "x1", new Vector2(30, -38), new Vector2(110, 54), 40, Color.white);
        text.fontSharedMaterial = infinite ? GameFontSetup.OutlineRed() : GameFontSetup.OutlinePurple();
        PopupBuilder.Fit(text);
        item = rt.gameObject;
    }

    private static Sprite BoosterIcon(BoosterDatabase db, BoosterType type)
    {
        BoosterData data = db != null ? db.Get(type) : null;
        return data != null ? data.icon : null;
    }

    private static Button PriceButton(Transform parent, Vector2 pos, Vector2 size, out TMP_Text label)
    {
        RectTransform face;
        Button b = PopupBuilder.FramedButton(parent, "BuyButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png", pos, size, out face);
        label = PopupBuilder.GameText(face, "Price", "0 VND", Vector2.zero, new Vector2(size.x - 30, size.y - 30), 44, Color.white);
        label.fontSharedMaterial = GameFontSetup.OutlineGreen();
        label.enableAutoSizing = true;
        label.fontSizeMin = 28;
        label.fontSizeMax = 44;
        label.enableWordWrapping = false;
        return b;
    }

    // ---------- Dòng Remove Ads ----------

    private static void RemoveAdsRow(Transform content, ShopDatabase db, ShopProduct product)
    {
        string frame = Frames + "frame_pack_ads.png";
        PopupBuilder.EnsureSliced(frame, new Vector4(60, 60, 60, 60));
        Vector2 size = new Vector2(1008, 200);
        RectTransform root = Fixed(PopupBuilder.NewUI("RemoveAds", content), size);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(frame);
        bg.type = Image.Type.Sliced;

        Image icon = PopupBuilder.Icon(root, "Icon", null, 140);
        icon.sprite = product.icon;
        icon.rectTransform.anchoredPosition = new Vector2(-504 + 125, 2);
        TMP_Text title = PopupBuilder.GameText(root, "Title", product.title, new Vector2(-504 + 210 + 210, 2), new Vector2(420, 140), 50, Color.white);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.fontSharedMaterial = GameFontSetup.OutlinePurple();
        title.lineSpacing = -14;
        TMP_Text price;
        Button buy = PriceButton(root, new Vector2(296, 2), new Vector2(300, 112), out price);

        ShopCardView view = root.gameObject.AddComponent<ShopCardView>();
        SerializedObject so = new SerializedObject(view);
        Set(so, "database", db);
        so.FindProperty("productId").stringValue = product.productId;
        Set(so, "titleText", title);
        Set(so, "priceText", price);
        Set(so, "buyButton", buy);
        Set(so, "icon", icon);
        so.FindProperty("hideWhenNoAds").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Tiêu đề mục ----------

    private static void Header(Transform content, string text, string sprite, Color color)
    {
        RectTransform root = Fixed(PopupBuilder.NewUI("Header_" + text.Replace(" ", ""), content), new Vector2(855, 72));
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(sprite);
        bg.raycastTarget = false;
        TMP_Text t = PopupBuilder.GameText(root, "Text", text, new Vector2(0, 2), new Vector2(800, 70), 46, color);
        t.enableWordWrapping = false;
    }

    // ---------- Lưới coin ----------

    private static void CoinGrid(Transform content, ShopDatabase db)
    {
        int count = db.products.FindAll(p => p.section == ShopSection.Coin).Count;
        int rows = Mathf.Max(1, (count + 2) / 3);
        Vector2 spacing = new Vector2(22, 24);
        Vector2 size = new Vector2(CoinSize.x * 3 + spacing.x * 2, CoinSize.y * rows + spacing.y * (rows - 1));
        RectTransform grid = Fixed(PopupBuilder.NewUI("CoinGrid", content), size);
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = CoinSize;
        g.spacing = spacing;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;
        g.childAlignment = TextAnchor.UpperCenter;

        foreach (ShopProduct p in db.products)
        {
            if (p.section != ShopSection.Coin) continue;
            RectTransform root = PopupBuilder.NewUI("Coin_" + p.coin, grid);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.sprite = PopupBuilder.LoadSprite(Frames + "frame_coin.png");
            Button buy = root.gameObject.AddComponent<Button>();
            buy.targetGraphic = bg;

            // gốc trên trái của frame_coin 323x421
            System.Func<float, float, Vector2> c = (x, y) => new Vector2(x - CoinSize.x * 0.5f, CoinSize.y * 0.5f - y);
            Image icon = PopupBuilder.Icon(root, "Icon", null, 210);
            icon.sprite = p.icon;
            icon.rectTransform.anchoredPosition = c(161.5f, 128);
            TMP_Text amount = PopupBuilder.GameText(root, "CoinAmount", "0", c(161.5f, 250), new Vector2(290, 66), 52, CoinYellow);
            amount.fontSharedMaterial = GameFontSetup.OutlineBrown();
            TMP_Text price = PopupBuilder.GameText(root, "Price", "0 VND", c(161.5f, 350), new Vector2(290, 70), 40, Color.white);
            price.fontSharedMaterial = GameFontSetup.OutlineGreen();
            price.enableAutoSizing = true;
            price.fontSizeMin = 26;
            price.fontSizeMax = 40;
            price.enableWordWrapping = false;

            ShopCardView view = root.gameObject.AddComponent<ShopCardView>();
            SerializedObject so = new SerializedObject(view);
            Set(so, "database", db);
            so.FindProperty("productId").stringValue = p.productId;
            Set(so, "priceText", price);
            Set(so, "buyButton", buy);
            Set(so, "icon", icon);
            Set(so, "coinText", amount);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------- Dữ liệu ----------

    /// <summary>ShopDatabase.asset: số từ *PackShop.asset của bản gốc (freeze = Shield), giá VND theo video bản gốc.</summary>
    internal static ShopDatabase EnsureDatabase()
    {
        ShopDatabase db = AssetDatabase.LoadAssetAtPath<ShopDatabase>(DatabasePath);
        if (db != null) return db;

        db = ScriptableObject.CreateInstance<ShopDatabase>();
        db.products.Add(Pack("starterpack", "Starter Pack", ShopSection.Starter, 66000, "icon_bundle_1", 3000, 1, 1, 1, 0, false, "90%\nOff"));
        db.products.Add(Pack("noad", "No more\ndisruptive ads", ShopSection.RemoveAds, 210000, null, 0, 0, 0, 0, 0, true));
        db.products[1].icon = PopupBuilder.LoadSprite(Icons + "no_ads_2.png");
        db.products.Add(Pack("noadbundle", "No Ads Bundle", ShopSection.NoAdsBundle, 263000, "icon_noadsbundle", 2500, 1, 1, 1, 60, true));
        db.products.Add(Pack("failoffer", "Fail Offer", ShopSection.Bundle, 158000, "icon_bundle_3", 1000, 1, 1, 1, 30, false));
        db.products.Add(Pack("minibundle", "Mini Offer", ShopSection.Bundle, 158000, "icon_bundle_2", 2000, 1, 1, 0, 60, false));
        db.products.Add(Pack("superbundle", "Super Offer", ShopSection.Bundle, 289000, "icon_bundle_4", 6000, 3, 3, 1, 60, false));
        db.products.Add(Pack("megabundle", "Mega Offer", ShopSection.Bundle, 553000, "icon_bundle_5", 12000, 6, 6, 2, 180, false));
        db.products.Add(Pack("deluxebundle", "Deluxe Offer", ShopSection.Bundle, 816000, "icon_bundle_6", 18000, 9, 9, 3, 360, false));
        string[] coinIcons = { "icon_coin_1", "icon_coin_2_0", "icon_coin_3", "icon_coin_4", "icon_coin_5", "icon_coin_6" };
        int[] coins = { 1000, 5000, 10000, 25000, 50000, 100000 };
        int[] prices = { 52000, 237000, 421000, 816000, 1600000, 2650000 };
        for (int i = 0; i < 6; i++) db.products.Add(Pack("coin" + (i + 1), coins[i] + " Coin", ShopSection.Coin, prices[i], coinIcons[i], coins[i], 0, 0, 0, 0, false));
        AssetDatabase.CreateAsset(db, DatabasePath);
        AssetDatabase.SaveAssets();
        return db;
    }

    private static ShopProduct Pack(string id, string title, ShopSection section, int price, string icon,
        int coin, int hint, int shield, int reveal, int infMinutes, bool removeAds, string sale = null)
    {
        return new ShopProduct
        {
            productId = Prefix + id, title = title, section = section, priceVnd = price,
            icon = icon != null ? PopupBuilder.LoadSprite(Icons + icon + ".png") : null,
            coin = coin, hint = hint, shield = shield, reveal = reveal, infiniteMinutes = infMinutes,
            removeAds = removeAds, saleTag = sale
        };
    }

    // ---------- Tiện ích ----------

    /// <summary>Kích thước cố định trong VerticalLayoutGroup (LayoutElement để Image không lấy cỡ sprite gốc).</summary>
    private static RectTransform Fixed(RectTransform rt, Vector2 size)
    {
        rt.sizeDelta = size;
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = size.x;
        le.preferredHeight = le.minHeight = size.y;
        return rt;
    }

    private static void Set(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
}
