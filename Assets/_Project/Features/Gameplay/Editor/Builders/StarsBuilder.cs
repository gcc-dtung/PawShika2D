using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Features.Economy;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hệ thống sao theo thời gian (Tools → PawBlock → Builders → Build Stars, chạy lại được):
    /// - LevelStars đặt cùng LevelTimer
    /// - Canvas_HUD/TopPanel/LevelFrame: thanh thời gian "TimeBar" có 3 mốc sao (TimeStarBar) ngay dưới khung,
    ///   (chỗ thanh % ô đã đặt cũ). Kích thước = TimeProgress gốc (549x60) x 1.166
    /// - Dựng lại màn thắng (WinScreenBuilder): tiêu đề theo số sao + 3 sao đập xuống
    /// </summary>
    public static class StarsBuilder
    {
        private const string UI = PopupBuilder.UI;
        private const string Bars = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Grid_Cells/";
        private const string FillSprite = Bars + "tin_trnh1.png";       // tiến_trình1 gốc (532x40, 2 đầu bo tròn bán kính 20)
        private const string BackSprite = Bars + "tin_trnh2.png";       // tiến_trình2 gốc (546x60)
        private const string GoldStar = UI + "Icons/star.png";
        private const string GrayStar = UI + "Icons/star2.png";

        private static readonly Vector2 BarSize = new Vector2(640, 70);
        private static readonly Vector2 FillAreaSize = new Vector2(618, 48); // vùng trong viền nền (gốc 530x41)
        private const float FillAreaY = 2f;
        private const float Gap = 12f;                                       // khoảng cách dưới mép khung LEVEL
        private static readonly Vector2 StarSize = new Vector2(98, 93);      // gốc 84x80
        private const float StarY = 10f;                                     // sao nhô lên trên tâm thanh 1 chút như gốc

        [MenuItem("Tools/PawBlock/Builders/Build Stars", priority = 103)]
        public static void Build()
        {
            LevelTimer timer = Object.FindFirstObjectByType<LevelTimer>(FindObjectsInactive.Include);
            if (timer == null)
            {
                Debug.LogError("[StarsBuilder] Không thấy LevelTimer trong scene.");
                return;
            }
            if (timer.GetComponent<LevelStars>() == null) Undo.AddComponent<LevelStars>(timer.gameObject);

            GameObject canvasGo = PopupBuilder.FindRoot("Canvas_HUD");
            Transform levelFrame = canvasGo != null ? PopupBuilder.HudRoot(canvasGo).Find("TopPanel/LevelFrame") : null;
            if (levelFrame == null)
            {
                Debug.LogError("[StarsBuilder] Không thấy Canvas_HUD/TopPanel/LevelFrame.");
                return;
            }
            BuildTimeBar(levelFrame);
            EditorSceneManager.MarkSceneDirty(levelFrame.gameObject.scene);

            WinScreenBuilder.Build();
            Debug.Log("[StarsBuilder] Đã dựng LevelStars, thanh thời gian 3 mốc sao, màn thắng có sao.");
        }

        private static void BuildTimeBar(Transform levelFrame)
        {
            PopupBuilder.DestroyChild(levelFrame, "TimeBar");

            RectTransform bar = PopupBuilder.NewUI("TimeBar", levelFrame);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0, -Gap);
            bar.sizeDelta = BarSize;
            Image back = bar.gameObject.AddComponent<Image>();
            back.sprite = PopupBuilder.LoadSprite(BackSprite);
            back.raycastTarget = false;

            RectTransform area = PopupBuilder.NewUI("FillArea", bar);
            area.anchoredPosition = new Vector2(0, FillAreaY);
            area.sizeDelta = FillAreaSize;

            // Phần còn lại: Sliced, co chiều rộng từ mép trái (2 đầu bo tròn giữ nguyên)
            PopupBuilder.EnsureSliced(FillSprite, new Vector4(20, 0, 20, 0));
            RectTransform fillRt = PopupBuilder.NewUI("Fill", area);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(FillAreaSize.x, 0f);
            Image fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = PopupBuilder.LoadSprite(FillSprite);
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 40f / FillAreaSize.y; // đầu bo theo chiều cao thật (sprite cao 40)
            fill.raycastTarget = false;

            // 3 mốc: sao 1 ở 0, sao 2 ở twoStarRatio, sao 3 ở threeStarRatio (TimeStarBar đặt x lúc chạy theo config)
            LivesEnergyConfig cfg = AssetDatabase.LoadAssetAtPath<LivesEnergyConfig>("Assets/_Project/Configs/LivesEnergyConfig.asset");
            RectTransform[] markers = new RectTransform[StarRating.Max];
            Image[] golds = new Image[StarRating.Max];
            for (int i = 0; i < markers.Length; i++)
            {
                RectTransform m = PopupBuilder.NewUI($"Star{i + 1}", area);
                m.anchorMin = m.anchorMax = new Vector2(0f, 0.5f);
                float ratio = StarRating.MarkerRatio(i + 1, cfg.twoStarRatio, cfg.threeStarRatio);
                m.anchoredPosition = new Vector2(ratio * FillAreaSize.x, StarY);
                m.sizeDelta = StarSize;
                Star(m, "Gray", GrayStar);
                golds[i] = Star(m, "Gold", GoldStar);
                markers[i] = m;
            }

            TimeStarBar ui = bar.gameObject.AddComponent<TimeStarBar>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("fillArea").objectReferenceValue = area;
            so.FindProperty("fill").objectReferenceValue = fill;
            SerializedProperty mp = so.FindProperty("markers");
            SerializedProperty gp = so.FindProperty("goldStars");
            mp.arraySize = markers.Length;
            gp.arraySize = golds.Length;
            for (int i = 0; i < markers.Length; i++)
            {
                mp.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
                gp.GetArrayElementAtIndex(i).objectReferenceValue = golds[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image Star(RectTransform marker, string name, string sprite)
        {
            RectTransform rt = PopupBuilder.Stretch(PopupBuilder.NewUI(name, marker));
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = PopupBuilder.LoadSprite(sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }
    }
}
