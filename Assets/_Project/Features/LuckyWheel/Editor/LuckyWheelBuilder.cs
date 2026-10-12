using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Gameplay;
using PawBlock.Game;
using PawBlock.Shared;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>
    /// Vòng quay may mắn (Tools → PawBlock → Builders → Build Lucky Wheel, chạy lại được):
    /// - Configs/LuckyWheelDatabase.asset: 10 sao / lần quay, 8 ô mặc định (đã có ô thì giữ nguyên, chỉnh trong Inspector).
    /// - Canvas_Popup/Popups/LuckyWheelPopup: khung chung (FramedPopup), số sao đang có, vòng quay vẽ bằng Circle.png
    ///   (mỗi ô 1 Image Filled Radial360 xoay tới đúng cung, icon + số hướng ra ngoài), viền vàng / tím, kim ở đỉnh, nút SPIN.
    /// - Nút "Lucky" trên Home (MainMenuBuilder.BuildLucky, đối xứng nút Daily).
    /// Không có asset vòng quay trong game gốc nên vòng được dựng từ hình tròn + màu.
    /// </summary>
    public static class LuckyWheelBuilder
    {
        private const string UI = PopupBuilder.UI;
        private const string Circle = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/Circle.png";
        private const string DatabasePath = GameConfigSetup.Folder + "LuckyWheelDatabase.asset";
        private const string RewardDatabasePath = GameConfigSetup.Folder + "DailyRewardDatabase.asset";

        private const float WheelSize = 700f;
        private const float WheelY = 0f;
        private const float ItemRadius = 225f;

        [MenuItem("Tools/PawBlock/Builders/Build Lucky Wheel", priority = 116)]
        public static void Build()
        {
            LuckyWheelDatabase db = EnsureDatabase();
            GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
            Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
            if (popups == null)
            {
                Debug.LogError("[LuckyWheelBuilder] Không thấy Canvas_Popup/Popups. Chạy Build Popups trước.");
                return;
            }
            Transform old = popups.Find("LuckyWheelPopup");
            int index = old != null ? old.GetSiblingIndex() : popups.childCount;
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            LuckyWheelPopup popup = BuildPopup(popups, db);
            popup.transform.SetSiblingIndex(index);
            popup.gameObject.SetActive(false);

            WireButton(MainMenuBuilder.RebuildLucky());
            EditorSceneManager.MarkSceneDirty(popupCanvas.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LuckyWheelBuilder] Đã dựng Vòng quay may mắn ({db.segments.Count} ô, {db.spinCost} sao / lần).");
        }

        /// <summary>Nối nút Lucky với popup + database (MainMenuBuilder gọi khi dựng lại Menu).</summary>
        internal static void WireButton(LuckyWheelButton button)
        {
            if (button == null) return;
            PopupBuilder.Assign(button, "database", AssetDatabase.LoadAssetAtPath<LuckyWheelDatabase>(DatabasePath));
            PopupBuilder.Assign(button, "popup", Object.FindFirstObjectByType<LuckyWheelPopup>(FindObjectsInactive.Include));
        }

        // ---------- Database ----------

        private static LuckyWheelDatabase EnsureDatabase()
        {
            LuckyWheelDatabase db = AssetDatabase.LoadAssetAtPath<LuckyWheelDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LuckyWheelDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            if (db.segments.Count == 0)
            {
                db.segments = DefaultSegments();
                EditorUtility.SetDirty(db);
            }
            return db;
        }

        /// <summary>
        /// 8 ô theo chiều kim đồng hồ từ đỉnh, tổng trọng số 95: coin nhỏ hay ra, booster vừa, coin 500 hiếm (~2 %).
        /// 10 sao ~ 4 level thắng (2–3 sao / level).
        /// </summary>
        private static List<LuckyWheelSegment> DefaultSegments() => new List<LuckyWheelSegment>
        {
            Seg(DailyRewardKind.Coin, BoosterType.Hint, 50, 25, new Color32(0xFF, 0xC9, 0x3C, 0xFF)),
            Seg(DailyRewardKind.Booster, BoosterType.Hint, 1, 15, new Color32(0xFF, 0x7E, 0xB6, 0xFF)),
            Seg(DailyRewardKind.Coin, BoosterType.Hint, 100, 15, new Color32(0x5C, 0xD6, 0xF5, 0xFF)),
            Seg(DailyRewardKind.Booster, BoosterType.Freeze, 1, 12, new Color32(0x7C, 0xDB, 0x5A, 0xFF)),
            Seg(DailyRewardKind.Coin, BoosterType.Hint, 200, 8, new Color32(0xFF, 0x9A, 0x3D, 0xFF)),
            Seg(DailyRewardKind.Booster, BoosterType.Reveal, 1, 8, new Color32(0xA9, 0x7C, 0xFF, 0xFF)),
            Seg(DailyRewardKind.InfiniteEnergy, BoosterType.Hint, 15, 10, new Color32(0x4F, 0x8B, 0xFF, 0xFF)),
            Seg(DailyRewardKind.Coin, BoosterType.Hint, 500, 2, new Color32(0xFF, 0x52, 0x52, 0xFF)),
        };

        private static LuckyWheelSegment Seg(DailyRewardKind kind, BoosterType booster, int amount, int weight, Color color) =>
            new LuckyWheelSegment { item = new DailyRewardItem { kind = kind, booster = booster, amount = amount }, weight = weight, color = color };

        // ---------- Popup ----------

        private static LuckyWheelPopup BuildPopup(Transform root, LuckyWheelDatabase db)
        {
            LuckyWheelPopup popup = PopupBuilder.FramedPopup<LuckyWheelPopup>(root, "LuckyWheelPopup", "LUCKY SPIN",
                new Vector2(960f, 1340f), 0f, out RectTransform panel, out Button close);
            DailyRewardDatabase rewards = AssetDatabase.LoadAssetAtPath<DailyRewardDatabase>(RewardDatabasePath);

            // Số sao đang có
            RectTransform balance = PopupBuilder.Row(panel, "Balance", new Vector2(0f, 500f), 80f);
            PopupBuilder.Icon(balance, "Star", UI + "Icons/star.png", 76f);
            TMP_Text balanceText = PopupBuilder.GameText(balance, "Value", "0", Vector2.zero, new Vector2(160f, 80f), 64f, Color.white);
            balanceText.fontSharedMaterial = GameFontSetup.OutlinePurple();
            PopupBuilder.Fit(balanceText);

            // Viền: vàng ngoài, tím trong (ảnh tròn trắng tô màu)
            PopupBuilder.EnsureSprite(Circle);
            Disc(panel, "RimGold", WheelSize + 70f, new Color(1f, 0.78f, 0.22f));
            Disc(panel, "RimPurple", WheelSize + 34f, new Color(0.29f, 0.19f, 0.62f));

            // Vòng quay: các ô + icon / số
            RectTransform wheel = PopupBuilder.NewUI("Wheel", panel);
            wheel.anchoredPosition = new Vector2(0f, WheelY);
            wheel.sizeDelta = new Vector2(WheelSize, WheelSize);
            int n = db.segments.Count;
            float step = 360f / Mathf.Max(1, n);
            Image[] segments = new Image[n];
            for (int i = 0; i < n; i++)
            {
                LuckyWheelSegment seg = db.segments[i];
                Image img = PopupBuilder.Icon(wheel, $"Segment{i}", Circle, WheelSize);
                img.preserveAspect = false;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount = 1f / n;
                img.color = seg.color;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -i * step); // cung [i, i + 1] x step theo chiều kim đồng hồ
                segments[i] = img;
            }
            for (int i = 0; i < n; i++)
            {
                // Đường kẻ giữa 2 ô
                RectTransform line = PopupBuilder.NewUI($"Divider{i}", wheel);
                line.pivot = new Vector2(0.5f, 0f);
                line.sizeDelta = new Vector2(6f, WheelSize * 0.5f);
                line.localEulerAngles = new Vector3(0f, 0f, -i * step);
                Image li = line.gameObject.AddComponent<Image>();
                li.color = new Color(1f, 1f, 1f, 0.85f);
                li.raycastTarget = false;
            }
            for (int i = 0; i < n; i++) Item(wheel, db.segments[i].item, LuckyWheelModel.CenterAngle(i, n), rewards, i);

            // Trục giữa
            Disc(panel, "HubRim", 170f, new Color(1f, 0.78f, 0.22f)).rectTransform.anchoredPosition = new Vector2(0f, WheelY);
            Disc(panel, "Hub", 140f, new Color(0.29f, 0.19f, 0.62f)).rectTransform.anchoredPosition = new Vector2(0f, WheelY);
            Image hubStar = PopupBuilder.Icon(panel, "HubStar", UI + "Icons/star_2.png", 100f);
            hubStar.rectTransform.anchoredPosition = new Vector2(0f, WheelY + 4f);

            // Kim ở đỉnh (mũi nhọn chỉ xuống, xoay quanh đế)
            Image pointer = PopupBuilder.Icon(panel, "Pointer", UI + "Popups_Dialogs/ArrowDialog 1.png", 100f);
            RectTransform prt = pointer.rectTransform;
            prt.sizeDelta = new Vector2(110f, 100f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, WheelY + WheelSize * 0.5f + 70f);
            prt.localScale = new Vector3(1f, -1f, 1f); // ảnh mũi chỉ lên -> lật xuống

            // Dòng kết quả
            RectTransform result = PopupBuilder.Row(panel, "Result", new Vector2(0f, -428f), 90f);
            Image resultIcon = PopupBuilder.Icon(result, "Icon", null, 90f);
            TMP_Text resultText = PopupBuilder.GameText(result, "Text", "You got x1!", Vector2.zero, new Vector2(600f, 80f), 56f, Color.white);
            resultText.fontSharedMaterial = GameFontSetup.OutlinePurple();
            PopupBuilder.Fit(resultText);
            result.gameObject.SetActive(false);

            // Nút SPIN: "SPIN" + sao + giá
            Button spin = PopupBuilder.FramedButton(panel, "SpinButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
                new Vector2(0f, -565f), new Vector2(460f, 170f), out RectTransform face);
            TMP_Text spinLabel = PopupBuilder.GameText(face, "Label", "SPIN", new Vector2(0f, 28f), new Vector2(400f, 70f), 62f, Color.white);
            spinLabel.fontSharedMaterial = GameFontSetup.OutlineGreen();
            RectTransform costRow = PopupBuilder.Row(face, "Cost", new Vector2(0f, -30f), 60f);
            PopupBuilder.Icon(costRow, "Star", UI + "Icons/star.png", 54f);
            TMP_Text cost = PopupBuilder.GameText(costRow, "Value", "10", Vector2.zero, new Vector2(100f, 60f), 50f, Color.white);
            cost.fontSharedMaterial = GameFontSetup.OutlineGreen();
            PopupBuilder.Fit(cost);
            PopupBuilder.GrayWhenDisabled(spin);

            SerializedObject so = new SerializedObject(popup);
            so.FindProperty("database").objectReferenceValue = db;
            so.FindProperty("rewardIcons").objectReferenceValue = rewards;
            so.FindProperty("wheel").objectReferenceValue = wheel;
            so.FindProperty("pointer").objectReferenceValue = prt;
            SerializedProperty sp = so.FindProperty("segmentImages");
            sp.arraySize = segments.Length;
            for (int i = 0; i < segments.Length; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
            so.FindProperty("spinButton").objectReferenceValue = spin;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.FindProperty("balanceText").objectReferenceValue = balanceText;
            so.FindProperty("resultRow").objectReferenceValue = result;
            so.FindProperty("resultIcon").objectReferenceValue = resultIcon;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static Image Disc(RectTransform parent, string name, float size, Color color)
        {
            Image img = PopupBuilder.Icon(parent, name, Circle, size);
            img.color = color;
            img.rectTransform.anchoredPosition = new Vector2(0f, WheelY);
            return img;
        }

        /// <summary>Icon + số của 1 ô, đặt ở tâm cung (bán kính ItemRadius), xoay theo hướng ra ngoài.</summary>
        private static void Item(RectTransform wheel, DailyRewardItem item, float angle, DailyRewardDatabase rewards, int index)
        {
            RectTransform rt = PopupBuilder.NewUI($"Item{index}", wheel);
            float rad = angle * Mathf.Deg2Rad;
            rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * ItemRadius;
            rt.localEulerAngles = new Vector3(0f, 0f, -angle);
            rt.sizeDelta = new Vector2(150f, 170f);

            Image icon = PopupBuilder.Icon(rt, "Icon", null, 100f);
            icon.sprite = IconOf(item, rewards);
            icon.rectTransform.anchoredPosition = new Vector2(0f, 26f);
            TMP_Text amount = PopupBuilder.GameText(rt, "Amount", DailyGiftBubble.AmountOf(item), new Vector2(0f, -50f), new Vector2(150f, 56f), 44f, Color.white);
            amount.fontSharedMaterial = GameFontSetup.OutlinePurple();
            PopupBuilder.Fit(amount);
        }

        private static Sprite IconOf(DailyRewardItem item, DailyRewardDatabase rewards)
        {
            switch (item.kind)
            {
                case DailyRewardKind.Coin: return rewards != null ? rewards.coinIcon : null;
                case DailyRewardKind.InfiniteEnergy: return rewards != null ? rewards.infiniteEnergyIcon : null;
                default:
                    BoosterDatabase boosters = AssetDatabase.LoadAssetAtPath<BoosterDatabase>(GameConfigSetup.Folder + "BoosterDatabase.asset");
                    BoosterData data = boosters != null ? boosters.Get(item.booster) : null;
                    return data != null ? data.ItemIcon : null;
            }
        }
    }
}
