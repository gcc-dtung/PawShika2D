using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Features.Gameplay;
using PawBlock.Game;
using PawBlock.Shared;

namespace PawBlock.Features.DailyReward
{
    /// <summary>
    /// Điểm danh hằng ngày (Tools → PawBlock → Builders → Build Daily Reward), chạy lại được:
    /// - DailyRewardDatabase.asset (chưa có thì tạo với quà mặc định 7 ngày)
    /// - DailyRewardManager trên Manager
    /// - DailyRewardPopup trong Canvas_Popup/Popups (thay bản cũ, không dựng lại popup khác), nối GameFlowUI
    /// - Nút Daily trên Home (dựng lại qua MainMenuBuilder.BuildDaily), gắn DailyRewardButton
    /// Build Popups / Build Main Menu cũng dựng popup / nút này.
    /// </summary>
    public static class DailyRewardBuilder
    {
        private const string DatabasePath = GameConfigSetup.Folder + "DailyRewardDatabase.asset";
        private const string UI = PopupBuilder.UI;
        private const string SlotFrame = UI + "Frames_Borders/khung_popup_4.png";

        private static readonly Vector2 SlotSize = new Vector2(252, 240);
        private const float SlotGap = 12f;
        private static readonly Color Day7Tint = new Color(1f, 0.9f, 0.68f);
        private static readonly Color HighlightColor = new Color(1f, 0.8f, 0.15f);
        private const int ItemsPerSlot = 5; // ngày 7 mặc định 5 món

        [MenuItem("Tools/PawBlock/Builders/Build Daily Reward", priority = 102)]
        public static void Build()
        {
            GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
            Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
            GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
            if (flow == null)
            {
                Debug.LogError("[DailyRewardBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
                return;
            }

            DailyRewardDatabase db = EnsureDatabase();

            LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (lm != null)
            {
                DailyRewardManager manager = lm.GetComponent<DailyRewardManager>();
                if (manager == null) manager = Undo.AddComponent<DailyRewardManager>(lm.gameObject);
                PopupBuilder.Assign(manager, "database", db);
            }

            Transform old = popups.Find("DailyRewardPopup");
            int index = old != null ? old.GetSiblingIndex() : popups.childCount;
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            DailyRewardPopup popup = BuildPopup(popups);
            popup.transform.SetSiblingIndex(index);
            popup.gameObject.SetActive(false);
            PopupBuilder.Assign(flow, "dailyRewardPopup", popup);

            MainMenuBuilder.RebuildDaily(flow);

            EditorSceneManager.MarkSceneDirty(popupCanvas.scene);
            Debug.Log("[DailyRewardBuilder] Đã dựng điểm danh hằng ngày.");
        }

        // ---------- Dữ liệu ----------

        /// <summary>Quà mặc định: coin xen booster, ngày 7 quà lớn (coin + mỗi booster + 30 phút năng lượng vô hạn).</summary>
        internal static DailyRewardDatabase EnsureDatabase()
        {
            DailyRewardDatabase db = AssetDatabase.LoadAssetAtPath<DailyRewardDatabase>(DatabasePath);
            if (db != null) return db;

            db = ScriptableObject.CreateInstance<DailyRewardDatabase>();
            db.days.Add(Day(Coin(100)));
            db.days.Add(Day(Booster(BoosterType.Hint, 1)));
            db.days.Add(Day(Coin(200)));
            db.days.Add(Day(Booster(BoosterType.Freeze, 1)));
            db.days.Add(Day(Coin(300)));
            db.days.Add(Day(Booster(BoosterType.Reveal, 1)));
            db.days.Add(Day(Coin(500), Booster(BoosterType.Hint, 1), Booster(BoosterType.Freeze, 1), Booster(BoosterType.Reveal, 1),
                new DailyRewardItem { kind = DailyRewardKind.InfiniteEnergy, amount = 30 }));
            db.coinIcon = PopupBuilder.LoadSprite(UI + "Icons/coin.png");
            db.infiniteEnergyIcon = PopupBuilder.LoadSprite(PopupBuilder.EnergyInfiniteSprite);
            AssetDatabase.CreateAsset(db, DatabasePath);
            AssetDatabase.SaveAssets();
            return db;
        }

        private static DailyRewardDay Day(params DailyRewardItem[] items)
        {
            DailyRewardDay d = new DailyRewardDay();
            d.items.AddRange(items);
            return d;
        }

        private static DailyRewardItem Coin(int amount) => new DailyRewardItem { kind = DailyRewardKind.Coin, amount = amount };

        private static DailyRewardItem Booster(BoosterType type, int amount) =>
            new DailyRewardItem { kind = DailyRewardKind.Booster, booster = type, amount = amount };

        // ---------- Popup ----------

        /// <summary>
        /// Khung chung (như Cài đặt / Nạp năng lượng), tiêu đề "DAILY REWARD". 7 ô như bản gốc "Đăng nhập 7 ngày":
        /// hàng 1 ngày 1–3, hàng 2 ngày 4–6, hàng 3 ô ngày 7 rộng hết 3 cột (quà lớn, icon to); dưới cùng nút CLAIM / đồng hồ.
        /// </summary>
        internal static DailyRewardPopup BuildPopup(Transform root)
        {
            RectTransform panel;
            Button close;
            DailyRewardPopup popup = PopupBuilder.FramedPopup<DailyRewardPopup>(root, "DailyRewardPopup", "DAILY REWARD",
                new Vector2(920, 1160), 60, out panel, out close);
            PopupBuilder.EnsureSliced(SlotFrame, new Vector4(45, 45, 45, 45));

            // 3 cột; hàng cách nhau SlotSize.y + SlotGap, hàng 1 ngay dưới tab tiêu đề
            float stepX = SlotSize.x + SlotGap;
            float stepY = SlotSize.y + SlotGap;
            const float row1 = 290f;
            DailyRewardSlot[] slots = new DailyRewardSlot[DailyRewardDatabase.CycleDays];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3, row = i / 3;
                slots[i] = BuildSlot(panel, i, new Vector2((col - 1) * stepX, row1 - row * stepY), SlotSize);
            }
            slots[6] = BuildSlot(panel, 6, new Vector2(0f, row1 - 2 * stepY), new Vector2(SlotSize.x * 3 + SlotGap * 2, SlotSize.y));

            RectTransform claimFace;
            Button claim = PopupBuilder.FramedButton(panel, "ClaimButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
                new Vector2(0, -445), new Vector2(360, 150), out claimFace);
            TMP_Text claimLabel = PopupBuilder.GameText(claimFace, "Label", "CLAIM", Vector2.zero, new Vector2(300, 100), 66, Color.white);
            claimLabel.fontSharedMaterial = GameFontSetup.OutlineGreen();

            RectTransform timer = PopupBuilder.NewUI("Timer", panel);
            timer.anchoredPosition = new Vector2(0, -445);
            timer.sizeDelta = new Vector2(760, 90);
            TMP_Text timerText = PopupBuilder.GameText(timer, "Text", "Next reward in 00:00:00", Vector2.zero, new Vector2(760, 90), 46, Color.white);
            timerText.fontSharedMaterial = GameFontSetup.OutlinePurple();

            SerializedObject so = new SerializedObject(popup);
            SerializedProperty arr = so.FindProperty("slots");
            arr.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.FindProperty("claimButton").objectReferenceValue = claim;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("timerGroup").objectReferenceValue = timer.gameObject;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        /// <summary>
        /// Ô quà: viền vàng (hôm nay) | nền tím nhạt (ngày 7 vàng nhạt) | Content (CanvasGroup: "Day n", hàng quà) | dấu tick | icon video (bù).
        /// Hàng quà dựng sẵn ItemsPerSlot món (icon + số), lúc chạy hiện đúng số món của ngày, hàng tự căn giữa.
        /// </summary>
        private static DailyRewardSlot BuildSlot(Transform panel, int day, Vector2 pos, Vector2 size)
        {
            bool big = day == DailyRewardDatabase.CycleDays - 1;
            RectTransform root = PopupBuilder.NewUI("Day" + (day + 1), panel);
            root.anchoredPosition = pos;
            root.sizeDelta = size;

            RectTransform highlight = PopupBuilder.NewUI("Highlight", root);
            highlight.sizeDelta = size + new Vector2(30, 30);
            Image hl = highlight.gameObject.AddComponent<Image>();
            hl.sprite = PopupBuilder.LoadSprite(SlotFrame);
            hl.type = Image.Type.Sliced;
            hl.color = HighlightColor;
            hl.raycastTarget = false;

            RectTransform bgRt = PopupBuilder.NewUI("Background", root);
            bgRt.sizeDelta = size;
            Image bg = bgRt.gameObject.AddComponent<Image>();
            bg.sprite = PopupBuilder.LoadSprite(SlotFrame);
            bg.type = Image.Type.Sliced;
            bg.color = big ? Day7Tint : Color.white;

            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bg;

            RectTransform content = PopupBuilder.NewUI("Content", root);
            content.sizeDelta = size;
            CanvasGroup group = content.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            TMP_Text dayText = PopupBuilder.GameText(content, "Day", "Day " + (day + 1), new Vector2(0, size.y * 0.5f - 34), new Vector2(size.x, 56), 40, PopupBuilder.SettingLabel);

            RectTransform row = PopupBuilder.Row(content, "Items", new Vector2(0, -14), 160);
            row.GetComponent<HorizontalLayoutGroup>().spacing = big ? 34 : 8;
            float iconSize = big ? 100 : 104;
            GameObject[] roots = new GameObject[ItemsPerSlot];
            Image[] icons = new Image[ItemsPerSlot];
            TMP_Text[] texts = new TMP_Text[ItemsPerSlot];
            for (int i = 0; i < ItemsPerSlot; i++)
            {
                RectTransform item = PopupBuilder.NewUI("Item" + (i + 1), row);
                item.sizeDelta = new Vector2(iconSize + 8, 160);
                LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = iconSize + 8;
                le.preferredHeight = 160;
                Image icon = PopupBuilder.Icon(item, "Icon", UI + "Icons/coin.png", iconSize);
                icon.rectTransform.anchoredPosition = new Vector2(0, 18);
                TMP_Text amount = PopupBuilder.GameText(item, "Amount", "100", new Vector2(0, -iconSize * 0.5f - 6), new Vector2(iconSize + 40, 50),
                    44, Color.white);
                amount.fontSharedMaterial = GameFontSetup.OutlinePurple();
                PopupBuilder.Fit(amount);
                roots[i] = item.gameObject;
                icons[i] = icon;
                texts[i] = amount;
            }

            Image tick = PopupBuilder.Icon(root, "Tick", UI + "Icons/icon_tick.png", 110);
            tick.rectTransform.sizeDelta = new Vector2(110, 78);
            tick.rectTransform.anchoredPosition = new Vector2(0, -10);
            tick.gameObject.SetActive(false);

            Image recover = PopupBuilder.Icon(root, "Recover", UI + "Buttons/btn_video2.png", 64);
            recover.rectTransform.anchoredPosition = new Vector2(size.x * 0.5f - 30, -size.y * 0.5f + 32); // góc dưới phải, không đè chữ "Day n"
            recover.gameObject.SetActive(false);

            DailyRewardSlot slot = root.gameObject.AddComponent<DailyRewardSlot>();
            SerializedObject so = new SerializedObject(slot);
            so.FindProperty("day").intValue = day;
            so.FindProperty("dayText").objectReferenceValue = dayText;
            SetArray(so.FindProperty("itemRoots"), roots);
            SetArray(so.FindProperty("itemIcons"), icons);
            SetArray(so.FindProperty("itemTexts"), texts);
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("highlight").objectReferenceValue = highlight;
            so.FindProperty("tick").objectReferenceValue = tick.rectTransform;
            so.FindProperty("recover").objectReferenceValue = recover.gameObject;
            so.FindProperty("content").objectReferenceValue = group;
            so.FindProperty("button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            highlight.gameObject.SetActive(false);
            return slot;
        }

        private static void SetArray(SerializedProperty p, Object[] values)
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
