using Coffee.UIExtensions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Màn thắng như bản gốc (Tools → PawBlock → Builders → Build Win Screen, chạy lại được):
    /// - WinPopup trong Canvas_Popup/Popups (thay bản cũ, giữ thứ tự, nối GameFlowUI); Build Popups cũng gọi BuildPopup
    /// - Material lóe sáng BlockFlash.mat (GUI/Text Shader) gán cho PlacedBlock.prefab
    /// Vị trí (canvas 1080x1920, tính từ tâm, theo WinUI gốc): tiêu đề +633, 3 sao +405..+437 (2 sao bên nghiêng ~33°),
    /// huy hiệu +46, nút -509. Tiêu đề: 4 ảnh well_done_1 / nice_1 / great_1 / perfect_1 (cắt từ atlas EndGame), bật 1 theo số sao.
    /// </summary>
    public static class WinScreenBuilder
    {
        private const string UI = PopupBuilder.UI;
        private const string VFX = "Assets/_Project/GameAssets/05_VFX_Particles/";
        private const string FlashMaterialPath = "Assets/_Project/GameAssets/09_Shaders_Materials/Materials/BlockFlash.mat";
        private const string BlockPrefabPath = "Assets/_Project/Prefabs/Gameplay/PlacedBlock.prefab";
        private const string ConfettiSheet = VFX + "Confetti/confetti_sheet.png";
        private const string ConfettiMaterialPath = "Assets/_Project/GameAssets/09_Shaders_Materials/Materials/ConfettiUI.mat";
        private const float ParticleUnit = 100f; // UIParticle.scale: 1 đơn vị particle = 100 đơn vị canvas

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.85f, 0.2f), new Color(1f, 0.4f, 0.65f), new Color(0.3f, 0.85f, 1f),
            new Color(0.35f, 0.5f, 1f), new Color(0.45f, 0.95f, 0.4f), new Color(1f, 0.35f, 0.3f), Color.white
        };
        private static readonly Color PillText = new Color32(0x5A, 0x2A, 0x14, 0xFF);

        // Thứ tự theo WinTitle (Well done, Nice, Great, Perfect)
        private static readonly string[] TitleSprites =
        {
            UI + "Popups_Dialogs/well_done_1.png", UI + "Popups_Dialogs/nice_1.png",
            UI + "Popups_Dialogs/great_1.png", UI + "Popups_Dialogs/perfect_1.png"
        };

        // Vị trí / góc nghiêng 3 sao (Star1CG..Star3CG của WinUI gốc)
        private static readonly Vector2[] StarPositions = { new Vector2(-172, 407), new Vector2(-3, 437), new Vector2(164, 405) };
        private static readonly float[] StarAngles = { 32.5f, 0f, -35.3f };
        private const float StarSize = 132f;

        [MenuItem("Tools/PawBlock/Builders/Build Win Screen", priority = 110)]
        public static void Build()
        {
            GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
            Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
            GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
            if (flow == null)
            {
                Debug.LogError("[WinScreenBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
                return;
            }

            Transform old = popups.Find("WinPopup");
            int index = old != null ? old.GetSiblingIndex() : 0;
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            WinPopup popup = BuildPopup(popups);
            popup.transform.SetSiblingIndex(index);
            popup.gameObject.SetActive(false);
            PopupBuilder.Assign(flow, "winPopup", popup);

            AssignFlashMaterial();
            EditorSceneManager.MarkSceneDirty(popupCanvas.scene);
            Debug.Log("[WinScreenBuilder] Đã dựng màn thắng.");
        }

        internal static WinPopup BuildPopup(Transform root)
        {
            RectTransform panel;
            WinPopup popup = PopupBuilder.NewPopup<WinPopup>(root, "WinPopup", out panel);
            Object.DestroyImmediate(panel.gameObject); // không có bảng: các phần đặt thẳng trên nền tối
            RectTransform rt = (RectTransform)popup.transform;
            Image dim = rt.Find("Dim").GetComponent<Image>();
            dim.color = new Color(0.02f, 0.01f, 0.08f, 0.95f); // tối hơn popup thường: lưới chỉ còn mờ mờ như bản gốc

            // Ô coin trên cùng
            RectTransform pill = TopCenter(PopupBuilder.NewUI("CoinPill", rt), new Vector2(40, -110), new Vector2(280, 92));
            Image pillBg = pill.gameObject.AddComponent<Image>();
            pillBg.sprite = PopupBuilder.LoadSprite(UI + "Frames_Borders/khung_pill_kem.png");
            pillBg.type = Image.Type.Sliced;
            pillBg.raycastTarget = false;
            Image pillCoin = PopupBuilder.Icon(pill, "Coin", UI + "Icons/coin.png", 110);
            pillCoin.rectTransform.anchoredPosition = new Vector2(-140, 0);
            TMP_Text coinText = PopupBuilder.GameText(pill, "Amount", "0", new Vector2(24, 2), new Vector2(200, 80), 54, PillText);

            // Tiêu đề: 4 ảnh chồng nhau ở cùng chỗ, WinPopup bật 1 theo số sao
            RectTransform title = PopupBuilder.NewUI("Title", rt);
            title.anchoredPosition = new Vector2(0, 633);
            title.sizeDelta = new Vector2(805, 293);
            Image[] titles = new Image[TitleSprites.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                PopupBuilder.EnsureSprite(TitleSprites[i]);
                titles[i] = PopupBuilder.Icon(title, ((WinTitle)i).ToString(), TitleSprites[i], 100);
                titles[i].SetNativeSize();
            }

            // Tia sáng + quầng sáng sau huy hiệu
            Vector2 medalPos = new Vector2(0, 46);
            Image glow = PopupBuilder.Icon(rt, "Glow", VFX + "Light_Glow/light_win1.png", 820);
            glow.rectTransform.anchoredPosition = medalPos;
            glow.color = new Color(1f, 0.95f, 0.75f, 0.9f);
            Image rays = PopupBuilder.Icon(rt, "Rays", VFX + "Light_Glow/light_win2.png", 980);
            rays.rectTransform.anchoredPosition = medalPos;
            rays.color = new Color(1f, 0.95f, 0.8f, 0.55f);

            // Huy hiệu: vòng tím + mèo
            RectTransform medal = PopupBuilder.NewUI("Medallion", rt);
            medal.anchoredPosition = medalPos;
            medal.sizeDelta = new Vector2(430, 430);
            PopupBuilder.Icon(medal, "Circle", UI + "Frames_Borders/khung_win1.png", 430);
            Image cat = PopupBuilder.Icon(medal, "Cat", UI + "Icons/cat_1.png", 360);
            cat.rectTransform.anchoredPosition = new Vector2(0, 8);

            // 3 sao (vẽ đè tia sáng như gốc): bóng xám star_5 cố định, sao vàng star_2 đè lên (WinPopup cho đập xuống)
            Image[] stars = new Image[StarPositions.Length];
            for (int i = 0; i < stars.Length; i++)
            {
                Image shadow = PopupBuilder.Icon(rt, $"Star{i + 1}Shadow", UI + "Icons/star_5.png", StarSize);
                shadow.rectTransform.anchoredPosition = StarPositions[i];
                shadow.rectTransform.localEulerAngles = new Vector3(0, 0, StarAngles[i]);
            }
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = PopupBuilder.Icon(rt, $"Star{i + 1}", UI + "Icons/star_2.png", StarSize);
                stars[i].rectTransform.anchoredPosition = StarPositions[i];
                stars[i].rectTransform.localEulerAngles = new Vector3(0, 0, StarAngles[i]);
            }

            // Confetti: ParticleSystem vẽ qua UIParticle (1 mesh, 1 material)
            UIConfetti confetti = BuildConfetti(rt);

            // Nút
            RectTransform nextFace;
            Button next = PopupBuilder.FramedButton(rt, "NextButton", UI + "Buttons/btn_green_2.png", UI + "Buttons/btn_green_1.png",
                new Vector2(-215, -509), new Vector2(370, 150), out nextFace);
            TMP_Text nextLabel = PopupBuilder.GameText(nextFace, "Label", "Continue", new Vector2(0, 26), new Vector2(300, 50), 40, Color.white);
            nextLabel.fontSharedMaterial = GameFontSetup.OutlineGreen();
            TMP_Text nextAmount = CoinRow(nextFace, new Vector2(0, -20), GameFontSetup.OutlineGreen());

            RectTransform doubleFace;
            Button doubleBtn = PopupBuilder.FramedButton(rt, "DoubleButton", UI + "Buttons/btn_yellow_6_0.png", UI + "Buttons/btn_yellow_5_0.png",
                new Vector2(215, -509), new Vector2(370, 150), out doubleFace);
            Image video = PopupBuilder.Icon(doubleFace, "Video", UI + "Buttons/btn_video2.png", 78);
            video.rectTransform.anchoredPosition = new Vector2(-105, 0);
            TMP_Text doubleLabel = PopupBuilder.GameText(doubleFace, "Label", "x2 Coin", new Vector2(36, 26), new Vector2(220, 50), 40, Color.white);
            doubleLabel.fontSharedMaterial = GameFontSetup.OutlineBrown();
            TMP_Text doubleAmount = CoinRow(doubleFace, new Vector2(36, -20), GameFontSetup.OutlineBrown());

            // Bấm xong (nhận thưởng / chờ quảng cáo) thì 2 nút xám
            PopupBuilder.GrayWhenDisabled(next);
            PopupBuilder.GrayWhenDisabled(doubleBtn);

            // Lớp coin bay (trên cùng)
            RectTransform fly = PopupBuilder.Stretch(PopupBuilder.NewUI("FlyLayer", rt));

            SerializedObject so = new SerializedObject(popup);
            SerializedProperty titleProp = so.FindProperty("titleImages");
            titleProp.arraySize = titles.Length;
            for (int i = 0; i < titles.Length; i++) titleProp.GetArrayElementAtIndex(i).objectReferenceValue = titles[i];
            SerializedProperty starProp = so.FindProperty("stars");
            starProp.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++) starProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            so.FindProperty("medallion").objectReferenceValue = medal;
            so.FindProperty("rays").objectReferenceValue = rays.rectTransform;
            so.FindProperty("glow").objectReferenceValue = glow.rectTransform;
            so.FindProperty("coinPill").objectReferenceValue = pill;
            so.FindProperty("coinIcon").objectReferenceValue = pillCoin.rectTransform;
            so.FindProperty("coinText").objectReferenceValue = coinText;
            so.FindProperty("nextButton").objectReferenceValue = next;
            so.FindProperty("doubleButton").objectReferenceValue = doubleBtn;
            so.FindProperty("nextAmountText").objectReferenceValue = nextAmount;
            so.FindProperty("doubleAmountText").objectReferenceValue = doubleAmount;
            so.FindProperty("confetti").objectReferenceValue = confetti;
            so.FindProperty("flyLayer").objectReferenceValue = fly;
            so.FindProperty("coinSprite").objectReferenceValue = PopupBuilder.LoadSprite(UI + "Icons/coin.png");
            so.FindProperty("panel").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // ---------- Confetti ----------

        /// <summary>
        /// Lớp confetti phủ màn hình: UIParticle (scale ParticleUnit) + 3 ParticleSystem con dùng chung ConfettiUI.mat
        /// (UI/Default + atlas confetti_sheet 2x2): BurstLeft / BurstRight (60 mảnh, bắn chéo lên 55–85°), Rain (22 mảnh / giây từ mép trên).
        /// Chuyển động: trọng lực, lực cản 1.6 (rơi tới vận tốc giới hạn), nhiễu nhẹ (lắc ngang), xoay + lật (xoay trục X), mờ dần 20 % cuối đời.
        /// </summary>
        private static UIConfetti BuildConfetti(RectTransform parent)
        {
            RectTransform rt = PopupBuilder.Stretch(PopupBuilder.NewUI("Confetti", parent));
            UIParticle ui = rt.gameObject.AddComponent<UIParticle>();
            ui.scale = ParticleUnit;
            ui.raycastTarget = false;

            Material mat = EnsureConfettiMaterial();
            ParticleSystem left = BuildConfettiSystem(rt, "BurstLeft", mat, false, 70f);
            ParticleSystem right = BuildConfettiSystem(rt, "BurstRight", mat, false, 110f);
            ParticleSystem rain = BuildConfettiSystem(rt, "Rain", mat, true, 0f);
            ui.RefreshParticles();

            UIConfetti confetti = rt.gameObject.AddComponent<UIConfetti>();
            SerializedObject so = new SerializedObject(confetti);
            so.FindProperty("burstLeft").objectReferenceValue = left;
            so.FindProperty("burstRight").objectReferenceValue = right;
            so.FindProperty("rain").objectReferenceValue = rain;
            so.ApplyModifiedPropertiesWithoutUndo();
            return confetti;
        }

        /// <param name="angle">Hướng bắn (độ, 0 = sang phải, 90 = lên); bỏ qua với mưa.</param>
        private static ParticleSystem BuildConfettiSystem(Transform parent, string name, Material mat, bool isRain, float angle)
        {
            GameObject go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Build Win Screen");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = isRain ? 1f : 0.5f;
            main.loop = isRain;
            main.playOnAwake = false;
            main.startLifetime = isRain ? new ParticleSystem.MinMaxCurve(4f, 6f) : new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = isRain ? new ParticleSystem.MinMaxCurve(0f, 0.5f) : new ParticleSystem.MinMaxCurve(15f, 24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.38f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = RandomColors();
            main.gravityModifier = isRain ? 0.6f : 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = isRain ? 120 : 80;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = isRain ? 22f : 0f;
            emission.SetBursts(isRain ? new ParticleSystem.Burst[0] : new[] { new ParticleSystem.Burst(0f, 60) });

            ParticleSystem.ShapeModule shape = ps.shape;
            if (isRain)
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(10.8f, 0.1f, 0.1f);
                shape.position = new Vector3(0f, 10f, 0f);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                shape.radius = 0.4f;
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
                shape.rotation = Quaternion.FromToRotation(Vector3.forward, dir).eulerAngles;
            }

            // Lực cản: bay nhanh lúc đầu rồi chậm dần, rơi tới vận tốc giới hạn như giấy
            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.drag = 1.6f;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;

            // Lắc ngang
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            // Xoay + lật (trục X) như giấy thật (radian / giây)
            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-10f, 10f);
            rot.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;

            // 4 hình trong atlas 2x2, mỗi mảnh 1 hình ngẫu nhiên, không đổi hình theo thời gian
            ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 2;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.99f);
            sheet.cycleCount = 1;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 10f;
            return ps;
        }

        private static ParticleSystem.MinMaxGradient RandomColors()
        {
            // Gradient Fixed + chế độ RandomColor: mỗi mảnh lấy 1 trong các màu (không pha trộn)
            Gradient g = new Gradient { mode = GradientMode.Fixed };
            GradientColorKey[] keys = new GradientColorKey[ConfettiColors.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(ConfettiColors[i], (i + 1f) / keys.Length);
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        }

        /// <summary>ConfettiUI.mat: UI/Default (UIParticle chỉ hỗ trợ shader UI) + atlas confetti_sheet.</summary>
        private static Material EnsureConfettiMaterial()
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ConfettiSheet);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(ConfettiMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("UI/Default")) { name = "ConfettiUI" };
                AssetDatabase.CreateAsset(mat, ConfettiMaterialPath);
            }
            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        /// <summary>Hàng "coin + số" căn giữa trong mặt nút.</summary>
        private static TMP_Text CoinRow(Transform face, Vector2 pos, Material outline)
        {
            RectTransform row = PopupBuilder.Row(face, "Amount", pos, 64);
            row.GetComponent<HorizontalLayoutGroup>().spacing = 6;
            PopupBuilder.Icon(row, "Coin", UI + "Icons/coin.png", 54);
            TMP_Text amount = PopupBuilder.GameText(row, "Value", "10", Vector2.zero, new Vector2(120, 64), 54, Color.white);
            amount.fontSharedMaterial = outline;
            PopupBuilder.Fit(amount);
            return amount;
        }

        private static RectTransform TopCenter(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>BlockFlash.mat (GUI/Text Shader: vẽ sprite thành 1 màu đặc) gán vào PlacedBlock.prefab để có trong bản build.</summary>
        private static void AssignFlashMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("GUI/Text Shader")) { name = "BlockFlash" };
                AssetDatabase.CreateAsset(mat, FlashMaterialPath);
            }
            GameObject prefab = PrefabUtility.LoadPrefabContents(BlockPrefabPath);
            PlacedBlockView view = prefab.GetComponent<PlacedBlockView>();
            if (view != null)
            {
                SerializedObject so = new SerializedObject(view);
                so.FindProperty("flashMaterial").objectReferenceValue = mat;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, BlockPrefabPath);
            }
            PrefabUtility.UnloadPrefabContents(prefab);
            AssetDatabase.SaveAssets();
        }
    }
}
