using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng phần cài đặt trong scene đang mở:
/// - Canvas_HUD/TopPanel: bỏ nút chơi lại và tim năng lượng, thêm nút bánh răng góc trên phải (SettingsButton)
/// - Canvas_Popup/Popups: thay popup cài đặt (và xóa ConfirmReplayPopup cũ) mà không dựng lại các popup khác
/// - Canvas_Popup/Popups: popup QUIT LEVEL (đang chơi bấm Home -> hỏi lại, bỏ dở mất 1 năng lượng)
/// - Nút Settings trên thanh trên Main Menu: gắn SettingsButton
/// Chạy lại được. Build Popups (dựng lại toàn bộ) cũng có sẵn popup cài đặt.
/// </summary>
public static class SettingsBuilder
{
    private const string Frame = PopupBuilder.UI + "Frames_Borders/khung_setting_3.png";
    private const string Gear = PopupBuilder.UI + "Buttons/btn_setting.png";

    [MenuItem("Tools/PawBlock/Builders/Build Settings", priority = 107)]
    public static void Build()
    {
        GameObject hudGo = PopupBuilder.FindRoot("Canvas_HUD");
        Transform top = hudGo != null ? PopupBuilder.HudRoot(hudGo).Find("TopPanel") : null;
        if (top == null)
        {
            Debug.LogError("[SettingsBuilder] Không tìm thấy Canvas_HUD/TopPanel.");
            return;
        }

        GameObject popupCanvas = PopupBuilder.FindRoot("Canvas_Popup");
        Transform popups = popupCanvas != null ? popupCanvas.transform.Find("Popups") : null;
        GameFlowUI flow = popups != null ? popups.GetComponent<GameFlowUI>() : null;
        if (flow == null)
        {
            Debug.LogError("[SettingsBuilder] Không tìm thấy Canvas_Popup/Popups (GameFlowUI). Chạy Build Popups trước.");
            return;
        }

        // ---------- HUD ----------
        PopupBuilder.DestroyChild(top, "ReplayButton");
        PopupBuilder.DestroyChild(top, "Energy");
        PopupBuilder.DestroyChild(top, "SettingsButton");
        BuildHudButton(top);

        // ---------- Popup ----------
        PopupBuilder.DestroyChild(popups, "ConfirmReplayPopup");
        PopupBuilder.DestroyChild(popups, "SettingsPopup");
        PopupBuilder.DestroyChild(popups, "QuitLevelPopup");
        SettingsPopup popup = PopupBuilder.BuildSettings(popups);
        popup.gameObject.SetActive(false);
        PopupBuilder.Assign(flow, "settingsPopup", popup);
        // Đang chơi mà bấm Home: hỏi lại, bỏ dở mất 1 năng lượng
        QuitLevelPopup quit = PopupBuilder.BuildQuitLevel(popups);
        quit.gameObject.SetActive(false);
        PopupBuilder.Assign(popup, "quitPopup", quit);
        PopupBuilder.Assign(flow, "quitLevelPopup", quit);

        // ---------- Nút Settings trên Main Menu ----------
        GameObject menu = PopupBuilder.FindRoot("Canvas_Menu");
        if (menu != null)
        {
            foreach (Button b in menu.GetComponentsInChildren<Button>(true))
            {
                if (b.name == "Settings" && b.GetComponent<SettingsButton>() == null) Undo.AddComponent<SettingsButton>(b.gameObject);
            }
        }

        foreach (SettingsButton b in Object.FindObjectsByType<SettingsButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PopupBuilder.Assign(b, "gameFlow", flow);
        }

        EditorSceneManager.MarkSceneDirty(hudGo.scene);
        Debug.Log("[SettingsBuilder] Đã dựng nút cài đặt trên HUD và popup cài đặt.");
    }

    /// <summary>Góc trên phải TopPanel (chỗ tim năng lượng cũ): ô tím nhạt + bánh răng, như nút trên Main Menu.</summary>
    private static void BuildHudButton(Transform top)
    {
        RectTransform root = PopupBuilder.NewUI("SettingsButton", top);
        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-50, -20);
        root.sizeDelta = new Vector2(150, 150);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = PopupBuilder.LoadSprite(Frame);
        Button b = root.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;

        Image gear = PopupBuilder.Icon(root, "Gear", Gear, 112);
        gear.rectTransform.anchoredPosition = new Vector2(0f, 4f);

        root.gameObject.AddComponent<SettingsButton>();
    }
}
