using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng phần năng lượng (Giai đoạn 5) trong scene đang mở: nút tim ở góc trên phải của TopPanel (EnergyHUD)
/// và EnergyManager trên Manager. Chạy lại được. Popup nạp năng lượng nằm ở Build Popups.
/// </summary>
public static class EnergyBuilder
{
    private const string Boosters = "Assets/_Project/GameAssets/03_Gameplay_Blocks_Grid/Boosters_Obstacles/";

    [MenuItem("Tools/PawBlock/Build Energy")]
    public static void Build()
    {
        GameObject canvasGo = PopupBuilder.FindRoot("Canvas_HUD"); // tìm được cả khi HUD đang tắt (đang ở Menu)
        Transform top = canvasGo != null ? PopupBuilder.HudRoot(canvasGo).Find("TopPanel") : null;
        if (top == null)
        {
            Debug.LogError("[EnergyBuilder] Không tìm thấy Canvas_HUD/TopPanel.");
            return;
        }

        PopupBuilder.DestroyChild(top, "Energy");

        // Góc trên phải, đối xứng với nút chơi lại bên trái
        RectTransform root = PopupBuilder.NewUI("Energy", top);
        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-40, -30);
        root.sizeDelta = new Vector2(240, 150);
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f); // vùng bấm trong suốt
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        RectTransform bg = PopupBuilder.NewUI("Bg", root);
        bg.anchoredPosition = new Vector2(20, 22);
        bg.sizeDelta = new Vector2(190, 70);
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = PopupBuilder.LoadSprite(Boosters + "khung_refill.png");
        bgImg.raycastTarget = false;

        Image heart = PopupBuilder.Icon(root, "Heart", PopupBuilder.EnergySprite, 100);
        heart.rectTransform.anchoredPosition = new Vector2(-62, 24);

        TMP_Text count = PopupBuilder.Text(root, "Count", "5", new Vector2(45, 24), new Vector2(110, 70), 56, new Color(0.25f, 0.2f, 0.55f));
        TMP_Text timer = PopupBuilder.Text(root, "Timer", "FULL", new Vector2(0, -42), new Vector2(240, 50), 38, Color.white);

        EnergyHUD hud = Undo.AddComponent<EnergyHUD>(root.gameObject);
        PopupBuilder.Assign(hud, "countText", count);
        PopupBuilder.Assign(hud, "timerText", timer);
        PopupBuilder.Assign(hud, "icon", heart.rectTransform); // số nằm ngoài tim nên không đổi sang tim ∞, số hiện "∞"
        GameFlowUI flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
        if (flow != null) PopupBuilder.Assign(hud, "gameFlow", flow);

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null && lm.GetComponent<EnergyManager>() == null) Undo.AddComponent<EnergyManager>(lm.gameObject);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[EnergyBuilder] Đã dựng nút năng lượng (tim) và EnergyManager.");
    }
}
