using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Settings;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Dựng các popup vào Canvas_Popup (canvas riêng, luôn bật, vẽ trên Canvas_HUD và Canvas_Menu) để dùng được
    /// ở cả màn chơi lẫn Main Menu: Win, Hồi mạng, Thua, Cài đặt, Mua booster, Nạp năng lượng, Điểm danh;
    /// gắn GameFlowUI, RewardedAds và nối các nút cần GameFlowUI (SettingsButton, EnergyHUD, ScreenFlow).
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
        // Linear color space: alpha 0.75 chỉ làm nền tối đi ~1/2, 0.88 mới tối như ref
        internal const float DimAlpha = 0.88f;
        internal static readonly Color SettingLabel = new Color(0.36f, 0.24f, 0.72f);
        private const string SettingsBox = UI + "Frames_Borders/khung_popup_1.png";
        // popup_1.png bỏ tab (tách bằng script ngoài): thân 9-slice dùng cho cả thân lẫn tab tiêu đề của popup cài đặt
        private const string SettingsFrame = UI + "Popups_Dialogs/popup_1_body.png";
        private const float FrameBorderScale = 1.4f; // viền 9-slice hiện nhỏ đi 1.4 lần (viền vàng ~14 px như ref)
        private const string TimerPill = UI + "Frames_Borders/khung_popup_4.png";

        [MenuItem("Tools/PawBlock/Builders/Build Popups", priority = 106)]
        public static void Build()
        {
            GameObject hudGo = FindRoot("Canvas_HUD");
            if (hudGo != null)
            {
                DestroyChild(hudGo.transform, "Popups"); // bản cũ nằm trong HUD
            }

            GameObject canvasGo = EnsurePopupCanvas();
            Transform canvas = canvasGo.transform;
            DestroyChild(canvas, "Popups");

            RectTransform root = Stretch(NewUI("Popups", canvas));
            GameFlowUI flow = root.gameObject.AddComponent<GameFlowUI>();

            WinPopup win = BuildWin(root);
            MoreTimePopup moreTime = BuildMoreTime(root);
            LosePopup lose = BuildLose(root);
            SettingsPopup settings = BuildSettings(root);
            QuitLevelPopup quit = BuildQuitLevel(root);
            NoticePopup notice = BuildNotice(root);
            Assign(settings, "quitPopup", quit);
            BuyBoosterPopup buy = BuildBuyBooster(root);
            RefillEnergyPopup refill = BuildRefillEnergy(root);
            DailyRewardPopup daily = DailyRewardBuilder.BuildPopup(root);

            Assign(flow, "winPopup", win);
            Assign(flow, "moreTimePopup", moreTime);
            Assign(flow, "losePopup", lose);
            Assign(flow, "settingsPopup", settings);
            Assign(flow, "quitLevelPopup", quit);
            Assign(flow, "noticePopup", notice);
            Assign(flow, "buyBoosterPopup", buy);
            Assign(flow, "refillEnergyPopup", refill);
            Assign(flow, "dailyRewardPopup", daily);

            EnergyHUD energyHud = Object.FindFirstObjectByType<EnergyHUD>(FindObjectsInactive.Include);
            if (energyHud != null) Assign(energyHud, "gameFlow", flow);

            foreach (SettingsButton b in Object.FindObjectsByType<SettingsButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Assign(b, "gameFlow", flow);

            foreach (EnergyHUD e in Object.FindObjectsByType<EnergyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Assign(e, "gameFlow", flow);
            ScreenFlow screenFlow = Object.FindFirstObjectByType<ScreenFlow>(FindObjectsInactive.Include);
            if (screenFlow != null) Assign(screenFlow, "gameFlow", flow);

            LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (lm != null && lm.GetComponent<RewardedAds>() == null) lm.gameObject.AddComponent<RewardedAds>();

            foreach (PopupView p in new PopupView[] { win, moreTime, lose, settings, quit, buy, refill, daily, notice }) p.gameObject.SetActive(false);

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

        /// <summary>Màn thắng như bản gốc: dựng ở WinScreenBuilder.</summary>
        private static WinPopup BuildWin(Transform root) => WinScreenBuilder.BuildPopup(root);

        /// <summary>Hết giờ (MoreTimePopup gốc): đồng hồ + "Need more time?", nút ads +30s / coin +60s, X = thua.</summary>
        private static MoreTimePopup BuildMoreTime(Transform root)
        {
            RectTransform panel;
            MoreTimePopup popup = NewPopup<MoreTimePopup>(root, "MoreTimePopup", out panel);
            Title(panel, "TIME'S UP!");

            Button close = NewButton(panel, "CloseButton", UI + "Buttons/btn_X_1.png", new Vector2(405, 255), null, null, null);
            ((RectTransform)close.transform).sizeDelta = new Vector2(120, 120);

            RectTransform clockRow = Row(panel, "Clock", new Vector2(0, -40), 140);
            Icon(clockRow, "Clock", UI + "Buttons/btn_clock.png", 130);
            Fit(Text(clockRow, "Message", "Need more time?", Vector2.zero, new Vector2(460, 100), 60, Color.white));

            RectTransform balanceRow = Row(panel, "Balance", new Vector2(0, -185), 90);
            Icon(balanceRow, "Coin", UI + "Icons/coin.png", 70);
            TMP_Text balance = Text(balanceRow, "Amount", "0", Vector2.zero, new Vector2(200, 80), 54, Yellow);
            Fit(balance);

            Button ads = NewButton(panel, "AdsButton", UI + "Buttons/btn_green_1.png", new Vector2(-200, -470), "+30s", UI + "Buttons/btn_video.png", null);
            Button coin = NewButton(panel, "CoinButton", UI + "Buttons/btn_yellow_1.png", new Vector2(200, -470), "+60s", null, UI + "Buttons/btn_clock.png");

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
            Assign(popup, "adsLabel", ads.transform.Find("Content/Label").GetComponent<TMP_Text>());
            Assign(popup, "coinLabel", coin.transform.Find("Content/Label").GetComponent<TMP_Text>());
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

        /// <summary>
        /// Cài đặt như ref: thân cao (popup_1_body 9-slice) + tab "SETTING" rộng (cùng sprite, nhỏ hơn) đè lên mép trên,
        /// nút X góc trên phải, khung trắng chứa 3 nút tròn Music / Sound / Vibrate (xanh = bật, xám + gạch đỏ = tắt),
        /// nút Home trong thân (ẩn khi mở ở Main Menu), chữ phiên bản dưới popup.
        /// Kích thước đo từ ảnh ref (màn 900 px rộng = canvas 1080, x1.2).
        /// </summary>
        internal static SettingsPopup BuildSettings(Transform root)
        {
            RectTransform panel;
            Button close;
            SettingsPopup popup = FramedPopup<SettingsPopup>(root, "SettingsPopup", "SETTING", new Vector2(920, 800), 150, out panel, out close);
            RectTransform box = WhiteBox(panel, "Options", new Vector2(0, 38), new Vector2(780, 372));

            SettingToggle(box, SettingType.Music, "Music", UI + "Icons/music.png", new Vector2(-246, 40));
            SettingToggle(box, SettingType.Sound, "Sound", UI + "Icons/sound.png", new Vector2(0, 40));
            SettingToggle(box, SettingType.Vibrate, "Vibrate", UI + "Icons/vibrate.png", new Vector2(246, 40));

            Button home = NewButton(panel, "HomeButton", UI + "Buttons/btn_yellow_5_0.png", new Vector2(0, -262), "Home", UI + "Icons/icon_home2.png", null);
            ((RectTransform)home.transform).sizeDelta = new Vector2(358, 150);
            TMP_Text homeLabel = home.transform.Find("Content/Label").GetComponent<TMP_Text>();
            homeLabel.fontStyle = FontStyles.Normal;
            homeLabel.font = GameFontSetup.Font;
            homeLabel.fontSharedMaterial = GameFontSetup.OutlineBrown();

            TMP_Text version = Text(panel, "Version", "Ver 0.0.0", new Vector2(0, -462), new Vector2(600, 70), 48, Color.white);
            version.fontStyle = FontStyles.Normal;
            version.font = GameFontSetup.Font;

            // Neo mép trên (chữ phiên bản neo mép dưới): ở Menu popup thu ngắn đáy mà nội dung không xê dịch
            foreach (RectTransform child in panel)
            {
                if (child == version.rectTransform) Anchor(child, 0f);
                else Anchor(child, 1f);
            }

            Assign(popup, "closeButton", close);
            Assign(popup, "homeButton", home);
            Assign(popup, "versionText", version);
            return popup;
        }

        /// <summary>Đổi neo dọc của con (đang neo giữa cha) sang mép trên (y = 1) / dưới (y = 0), giữ nguyên vị trí.</summary>
        private static void Anchor(RectTransform rt, float y)
        {
            float parentHeight = ((RectTransform)rt.parent).rect.height;
            Vector2 pos = rt.anchoredPosition;
            pos.y -= (y - 0.5f) * parentHeight;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.anchoredPosition = pos;
        }

        /// <summary>"NOTICE" (như bản gốc): khung chung, khung trắng chứa nội dung, nút xanh viền kem "Continue".</summary>
        internal static NoticePopup BuildNotice(Transform root)
        {
            RectTransform panel;
            Button close;
            NoticePopup popup = FramedPopup<NoticePopup>(root, "NoticePopup", "NOTICE", new Vector2(920, 800), 150, out panel, out close);
            RectTransform box = WhiteBox(panel, "Content", new Vector2(0, 38), new Vector2(780, 372));
            TMP_Text message = GameText(box, "Message", "Purchase Failed!", Vector2.zero, new Vector2(700, 320), 58, SettingLabel);
            message.enableAutoSizing = true;
            message.fontSizeMin = 36;
            message.fontSizeMax = 58;

            RectTransform face;
            Button cont = FramedButton(panel, "ContinueButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
                new Vector2(0, -262), new Vector2(360, 150), out face);
            TMP_Text label = GameText(face, "Label", "Continue", Vector2.zero, new Vector2(300, 100), 62, Color.white);
            label.fontSharedMaterial = GameFontSetup.OutlineGreen();

            Assign(popup, "titleText", panel.Find("TitleTab/Title").GetComponent<TMP_Text>());
            Assign(popup, "messageText", message);
            Assign(popup, "continueButton", cont);
            Assign(popup, "closeButton", close);
            return popup;
        }

        /// <summary>Thay riêng popup NOTICE trong scene (không dựng lại popup khác).</summary>
        [MenuItem("Tools/PawBlock/Builders/Build Notice Popup", priority = 105)]
        public static void BuildNoticeOnly()
        {
            GameObject canvas = FindRoot("Canvas_Popup");
            Transform popups = canvas != null ? canvas.transform.Find("Popups") : null;
            GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
            if (flow == null)
            {
                Debug.LogError("[PopupBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
                return;
            }
            DestroyChild(popups, "NoticePopup");
            NoticePopup notice = BuildNotice(popups);
            notice.gameObject.SetActive(false);
            Assign(flow, "noticePopup", notice);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
        }

        /// <summary>
        /// "QUIT LEVEL" (như bản gốc): khung chung, khung trắng "You will lose" + tim vỡ "-1", nút đỏ viền kem "Quit".
        /// Đang chơi mà bấm Home trong Cài đặt thì mở popup này.
        /// </summary>
        internal static QuitLevelPopup BuildQuitLevel(Transform root)
        {
            RectTransform panel;
            Button close;
            QuitLevelPopup popup = FramedPopup<QuitLevelPopup>(root, "QuitLevelPopup", "QUIT LEVEL", new Vector2(920, 800), 150, out panel, out close);
            RectTransform box = WhiteBox(panel, "Content", new Vector2(0, 38), new Vector2(780, 372));
            GameText(box, "Message", "You will lose", new Vector2(0, 120), new Vector2(700, 90), 62, SettingLabel);
            Image heart = Icon(box, "Heart", UI + "Icons/heart_2.png", 230);
            heart.rectTransform.anchoredPosition = new Vector2(0, -42);
            TMP_Text minus = GameText(heart.transform, "Amount", "-1", new Vector2(78, -58), new Vector2(220, 140), 118, Color.white);
            minus.fontSharedMaterial = GameFontSetup.OutlineRed();
            Fit(minus);

            RectTransform quitFace;
            Button quit = FramedButton(panel, "QuitButton", UI + "Buttons/btn_red_2_0.png", UI + "Buttons/btn_red_1_0.png",
                new Vector2(0, -262), new Vector2(360, 150), out quitFace);
            TMP_Text label = GameText(quitFace, "Label", "Quit", Vector2.zero, new Vector2(300, 100), 68, Color.white);
            label.fontSharedMaterial = GameFontSetup.OutlineRed();

            Assign(popup, "quitButton", quit);
            Assign(popup, "closeButton", close);
            return popup;
        }

        /// <summary>1 nút tròn + icon + gạch đỏ (khi tắt) + chữ bên dưới.</summary>
        private static void SettingToggle(Transform parent, SettingType type, string label, string iconPath, Vector2 pos)
        {
            RectTransform rt = NewUI(label + "Toggle", parent);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(192, 192);
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = LoadSprite(UI + "Buttons/btn_setting2.png");
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;

            Icon(rt, "Icon", iconPath, 110).rectTransform.anchoredPosition = new Vector2(0, 4);
            Image off = Icon(rt, "OffMark", UI + "Icons/off.png", 136);
            off.gameObject.SetActive(false);
            TMP_Text t = Text(rt, "Label", label, new Vector2(0, -154), new Vector2(260, 76), 56, SettingLabel);
            t.fontStyle = FontStyles.Normal;
            t.font = GameFontSetup.Font;
            Fit(t);

            SettingToggleView view = rt.gameObject.AddComponent<SettingToggleView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("onSprite").objectReferenceValue = LoadSprite(UI + "Buttons/btn_setting2.png");
            so.FindProperty("offSprite").objectReferenceValue = LoadSprite(UI + "Buttons/btn_setting1.png");
            so.FindProperty("offMark").objectReferenceValue = off.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Hết booster như ref "REVEAL": khung như popup cài đặt (tab tiêu đề = tên booster); trong khung trắng: icon booster to
        /// + "x1" đè góc dưới phải, mô tả tím 2 dòng; dưới: nút xanh viền kem coin + giá. Dùng chung cho cả 3 booster.
        /// Số đo từ ảnh ref (màn 858 px rộng = canvas 1080, x1.259).
        /// </summary>
        internal static BuyBoosterPopup BuildBuyBooster(Transform root)
        {
            RectTransform panel;
            Button close;
            BuyBoosterPopup popup = FramedPopup<BuyBoosterPopup>(root, "BuyBoosterPopup", "HINT", new Vector2(920, 812), 150, out panel, out close);
            RectTransform box = WhiteBox(panel, "Content", new Vector2(0, 38), new Vector2(778, 426));

            Image icon = Icon(box, "Icon", "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/btn_hint.png", 220);
            icon.rectTransform.anchoredPosition = new Vector2(10, 58);
            TMP_Text amount = GameText(icon.transform, "Amount", "x1", new Vector2(52, -78), new Vector2(160, 100), 84, Color.white);
            amount.fontSharedMaterial = GameFontSetup.OutlinePurple();
            Fit(amount);

            TMP_Text detail = GameText(box, "Detail", "Detail", new Vector2(0, -128), new Vector2(680, 120), 42, SettingLabel);
            detail.lineSpacing = -8;
            detail.enableAutoSizing = true;
            detail.fontSizeMin = 30;
            detail.fontSizeMax = 42;

            // Nút xanh: coin + giá
            RectTransform face;
            Button buy = FramedButton(panel, "BuyButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
                new Vector2(0, -276), new Vector2(395, 160), out face);
            RectTransform priceRow = Row(face, "Price", new Vector2(0, 0), 90);
            priceRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;
            Icon(priceRow, "Coin", UI + "Icons/coin.png", 70);
            TMP_Text price = GameText(priceRow, "Amount", "1000", Vector2.zero, new Vector2(200, 90), 74, Color.white);
            price.fontSharedMaterial = GameFontSetup.OutlineGreen();
            Fit(price);
            GrayWhenDisabled(buy);

            Assign(popup, "titleText", panel.Find("TitleTab/Title").GetComponent<TMP_Text>());
            Assign(popup, "detailText", detail);
            Assign(popup, "icon", icon);
            Assign(popup, "amountText", amount);
            Assign(popup, "priceText", price);
            Assign(popup, "buyButton", buy);
            Assign(popup, "closeButton", close);
            return popup;
        }

        /// <summary>Thay riêng popup mua booster trong scene (không dựng lại popup khác).</summary>
        [MenuItem("Tools/PawBlock/Builders/Build Buy Booster Popup", priority = 105)]
        public static void BuildBuyBoosterOnly()
        {
            GameObject canvas = FindRoot("Canvas_Popup");
            Transform popups = canvas != null ? canvas.transform.Find("Popups") : null;
            GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
            if (flow == null)
            {
                Debug.LogError("[PopupBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
                return;
            }
            DestroyChild(popups, "BuyBoosterPopup");
            BuyBoosterPopup buy = BuildBuyBooster(popups);
            buy.gameObject.SetActive(false);
            Assign(flow, "buyBoosterPopup", buy);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
        }

        /// <summary>
        /// Nạp năng lượng như ref "REFILL LIVES?": khung như popup cài đặt; trong khung trắng: tim to có số năng lượng,
        /// chữ mời, ô đồng hồ đếm tới lần hồi kế; dưới: nút xanh "Refill" + giá coin, nút vàng "Free live" + video.
        /// Số đo từ ảnh ref (màn 858 px rộng = canvas 1080, x1.259).
        /// </summary>
        private static RefillEnergyPopup BuildRefillEnergy(Transform root)
        {
            RectTransform panel;
            Button close;
            RefillEnergyPopup popup = FramedPopup<RefillEnergyPopup>(root, "RefillEnergyPopup", "REFILL LIVES?", new Vector2(920, 820), 166, out panel, out close);
            RectTransform box = WhiteBox(panel, "Content", new Vector2(0, 33), new Vector2(778, 424));

            Image heart = Icon(box, "Heart", UI + "Icons/heart_1.png", 250);
            heart.rectTransform.anchoredPosition = new Vector2(-201, 0);
            TMP_Text count = GameText(heart.transform, "Count", "5", new Vector2(0, 8), new Vector2(200, 150), 120, Color.white);
            count.fontSharedMaterial = GameFontSetup.OutlineRed();

            TMP_Text message = GameText(box, "Message", "Get more lives to\ncontinue playing", new Vector2(156, 60), new Vector2(420, 140), 46, SettingLabel);
            message.lineSpacing = -10;

            // Ô đồng hồ: khung tím nhạt kéo dài, đồng hồ tai mèo đè mép trái
            EnsureSliced(TimerPill, new Vector4(45, 45, 45, 45));
            RectTransform pill = NewUI("Timer", box);
            pill.anchoredPosition = new Vector2(157, -75);
            pill.sizeDelta = new Vector2(287, 83);
            Image pillImg = pill.gameObject.AddComponent<Image>();
            pillImg.sprite = LoadSprite(TimerPill);
            pillImg.type = Image.Type.Sliced;
            pillImg.raycastTarget = false;
            Icon(pill, "Clock", UI + "Buttons/btn_time_2.png", 104).rectTransform.anchoredPosition = new Vector2(-115, 4);
            TMP_Text timer = GameText(pill, "Time", "29:59", new Vector2(26, 2), new Vector2(200, 70), 50, Color.white);
            timer.fontSharedMaterial = GameFontSetup.OutlinePurple();

            // Nút xanh: "Refill" trên, coin + giá dưới
            RectTransform coinFace;
            Button coin = FramedButton(panel, "CoinButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png", new Vector2(-213, -277), new Vector2(345, 145), out coinFace);
            TMP_Text refillLabel = GameText(coinFace, "Label", "Refill", new Vector2(0, 30), new Vector2(260, 50), 40, Color.white);
            refillLabel.fontSharedMaterial = GameFontSetup.OutlineGreen();
            RectTransform priceRow = Row(coinFace, "Price", new Vector2(0, -16), 70);
            priceRow.GetComponent<HorizontalLayoutGroup>().spacing = 8;
            Icon(priceRow, "Coin", UI + "Icons/coin.png", 62);
            TMP_Text cost = GameText(priceRow, "Amount", "900", Vector2.zero, new Vector2(160, 70), 66, Color.white);
            cost.fontSharedMaterial = GameFontSetup.OutlineGreen();
            Fit(cost);

            // Nút vàng: "Free live" + video
            RectTransform adsFace;
            Button ads = FramedButton(panel, "AdsButton", UI + "Buttons/btn_yellow_6_0.png", UI + "Buttons/btn_yellow_5_0.png", new Vector2(206, -277), new Vector2(345, 145), out adsFace);
            RectTransform adsRow = Row(adsFace, "Content", new Vector2(0, 0), 90);
            adsRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;
            TMP_Text free = GameText(adsRow, "Label", "Free live", Vector2.zero, new Vector2(200, 80), 54, Color.white);
            free.fontSharedMaterial = GameFontSetup.OutlineBrown();
            Fit(free);
            Icon(adsRow, "Video", UI + "Buttons/btn_video2.png", 84);

            Assign(popup, "countText", count);
            Assign(popup, "timerText", timer);
            Assign(popup, "coinCostText", cost);
            Assign(popup, "adsButton", ads);
            Assign(popup, "coinButton", coin);
            Assign(popup, "closeButton", close);
            return popup;
        }

        // ---------- Khung chung ----------

        /// <summary>
        /// Khung kiểu ref (Cài đặt, Nạp năng lượng): thân popup_1_body 9-slice cỡ <paramref name="size"/> (tâm cao <paramref name="posY"/>),
        /// tab tiêu đề rộng 690 nhô 60 px trên mép thân (cùng sprite, nhỏ hơn), nút X góc trên phải.
        /// </summary>
        internal static T FramedPopup<T>(Transform root, string name, string titleText, Vector2 size, float posY,
            out RectTransform panel, out Button close) where T : PopupView
        {
            T popup = NewPopup<T>(root, name, out panel);
            EnsureSliced(SettingsFrame, new Vector4(130, 130, 130, 130));
            Image body = panel.GetComponent<Image>();
            body.sprite = LoadSprite(SettingsFrame);
            body.type = Image.Type.Sliced;
            body.pixelsPerUnitMultiplier = FrameBorderScale;
            panel.sizeDelta = size;
            panel.anchoredPosition = new Vector2(0, posY);

            RectTransform tab = NewUI("TitleTab", panel);
            tab.anchoredPosition = new Vector2(0, size.y * 0.5f + 60 - 100);
            tab.sizeDelta = new Vector2(690, 200);
            Image tabImg = tab.gameObject.AddComponent<Image>();
            tabImg.sprite = body.sprite;
            tabImg.type = Image.Type.Sliced;
            tabImg.pixelsPerUnitMultiplier = FrameBorderScale;
            tabImg.raycastTarget = false;
            TMP_Text title = GameText(tab, "Title", titleText, new Vector2(0, 4), new Vector2(620, 150), 100, Color.white);
            title.fontSharedMaterial = GameFontSetup.OutlinePurple();
            title.enableAutoSizing = true;
            title.fontSizeMin = 60;
            title.fontSizeMax = 100;
            title.enableWordWrapping = false;

            close = NewButton(panel, "CloseButton", UI + "Buttons/btn_X_1.png", new Vector2(size.x * 0.5f - 28, size.y * 0.5f - 52), null, null, null);
            ((RectTransform)close.transform).sizeDelta = new Vector2(124, 124);
            return popup;
        }

        /// <summary>Khung trắng bo tròn (khung_popup_1 9-slice) trong thân popup.</summary>
        internal static RectTransform WhiteBox(Transform panel, string name, Vector2 pos, Vector2 size)
        {
            EnsureSliced(SettingsBox, new Vector4(60, 60, 60, 60));
            RectTransform box = NewUI(name, panel);
            box.anchoredPosition = pos;
            box.sizeDelta = size;
            Image img = box.gameObject.AddComponent<Image>();
            img.sprite = LoadSprite(SettingsBox);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return box;
        }

        /// <summary>
        /// Nút 2 lớp như ref: nền viền kem (<paramref name="backer"/>) + mặt nút (<paramref name="face"/>) nhích lên 4 px (theo ảnh gốc).
        /// <paramref name="size"/> là cỡ nền; mặt co theo tỉ lệ 2 sprite gốc. Tắt (interactable = false) thì mặt xám.
        /// </summary>
        internal static Button FramedButton(Transform parent, string name, string backer, string face, Vector2 pos, Vector2 size, out RectTransform faceRect)
        {
            RectTransform rt = NewUI(name, parent);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image back = rt.gameObject.AddComponent<Image>();
            back.sprite = LoadSprite(backer);

            faceRect = NewUI("Face", rt);
            Image faceImg = faceRect.gameObject.AddComponent<Image>();
            faceImg.sprite = LoadSprite(face);
            faceImg.raycastTarget = false;
            if (back.sprite != null && faceImg.sprite != null)
            {
                Vector2 b = back.sprite.rect.size;
                Vector2 f = faceImg.sprite.rect.size;
                faceRect.sizeDelta = new Vector2(size.x * f.x / b.x, size.y * f.y / b.y);
                faceRect.anchoredPosition = new Vector2(0, size.y * 4f / b.y);
            }

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImg;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            button.colors = colors;
            return button;
        }

        private const string GrayMaterialPath = "Assets/_Project/GameAssets/09_Shaders_Materials/Materials/UI_Grayscale.mat";
        private const string GrayShaderPath = "Assets/_Project/GameAssets/09_Shaders_Materials/Shaders/UI_Grayscale.shader";

        /// <summary>Nút tắt thì xám (sprite + chữ viền xám) thay vì tối đi: gắn UIGrayscaleWhenDisabled, disabledColor = trắng.</summary>
        internal static void GrayWhenDisabled(Button button)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GrayMaterialPath);
            if (mat == null)
            {
                mat = new Material(AssetDatabase.LoadAssetAtPath<Shader>(GrayShaderPath)) { name = "UI_Grayscale" };
                AssetDatabase.CreateAsset(mat, GrayMaterialPath);
            }
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            UIGrayscaleWhenDisabled gray = button.gameObject.AddComponent<UIGrayscaleWhenDisabled>();
            Assign(gray, "grayMaterial", mat);
            Assign(gray, "grayTextMaterial", GameFontSetup.OutlineGray());
        }

        /// <summary>Chữ font game (Fredoka), không in đậm giả.</summary>
        internal static TMP_Text GameText(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            TMP_Text t = Text(parent, name, text, pos, size, fontSize, color);
            t.fontStyle = FontStyles.Normal;
            t.font = GameFontSetup.Font;
            return t;
        }

        internal static T NewPopup<T>(Transform root, string name, out RectTransform panel) where T : PopupView
        {
            RectTransform rt = Stretch(NewUI(name, root));
            rt.gameObject.AddComponent<CanvasGroup>();
            T popup = rt.gameObject.AddComponent<T>();

            RectTransform dim = Stretch(NewUI("Dim", rt));
            Image dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, DimAlpha);

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

        /// <summary>Ảnh mới thêm vào project: đặt kiểu Sprite nếu chưa phải.</summary>
        internal static void EnsureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        /// <summary>Đặt sprite + viền 9-slice nếu chưa có (để kéo giãn không méo góc bo).</summary>
        internal static void EnsureSliced(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder != Vector4.zero) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
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
}
