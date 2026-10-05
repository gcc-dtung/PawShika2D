using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dựng UI Zone (SafeAreaZone) để UI tránh tai thỏ / thanh home:
/// - Canvas_HUD/UIZone (4 cạnh): chuyển TopPanel, DownPanel, RevealBanner, PlayZone vào trong (giữ thứ tự vẽ)
/// - Canvas_Menu/Panels/HomePanel/UIZone (cạnh trên): Build Main Menu dựng TopBar, Starter, NoAds vào đây
/// Chạy lại được. Popup (Canvas_Popup) nằm giữa màn hình nên không cần.
/// </summary>
public static class UIZoneBuilder
{
    public const string ZoneName = "UIZone";
    private static readonly string[] HudChildren = { "PlayZone", "TopPanel", "DownPanel", "RevealBanner" };

    [MenuItem("Tools/PawBlock/Builders/Build UI Zone", priority = 109)]
    public static void Build()
    {
        GameObject hud = PopupBuilder.FindRoot("Canvas_HUD");
        if (hud == null)
        {
            Debug.LogError("[UIZoneBuilder] Không tìm thấy Canvas_HUD trong scene.");
            return;
        }

        RectTransform zone = Ensure(hud.transform, true, true, true, true);
        foreach (string name in HudChildren)
        {
            Transform child = hud.transform.Find(name);
            if (child == null) continue;
            // Giữ neo / vị trí cục bộ. Không giữ vị trí world: Canvas_HUD tắt sẵn (scale 0) sẽ làm scale con thành 0
            Undo.SetTransformParent(child, zone, false, "Build UI Zone");
        }
        foreach (Transform child in zone)
        {
            if (child.localScale == Vector3.one) continue;
            Undo.RecordObject(child, "Build UI Zone");
            child.localScale = Vector3.one; // sửa bản dựng cũ đã bị scale 0
        }
        // Giữ thứ tự vẽ như cũ: PlayZone (debug) dưới cùng, RevealBanner trên cùng
        for (int i = 0; i < HudChildren.Length; i++)
        {
            Transform child = zone.Find(HudChildren[i]);
            if (child != null) child.SetSiblingIndex(Mathf.Min(i, zone.childCount - 1));
        }
        zone.SetSiblingIndex(0);

        MainMenuBuilder.Build(); // HomePanel/UIZone + TopBar, Starter, NoAds

        EditorSceneManager.MarkSceneDirty(hud.scene);
        Debug.Log("[UIZoneBuilder] Đã dựng UIZone cho Canvas_HUD và HomePanel.");
    }

    /// <summary>Lấy / tạo con UIZone giãn hết <paramref name="parent"/>, gắn SafeAreaZone với các cạnh chỉ định.</summary>
    internal static RectTransform Ensure(Transform parent, bool top, bool bottom, bool left, bool right)
    {
        Transform t = parent.Find(ZoneName);
        RectTransform zone = t != null ? (RectTransform)t : PopupBuilder.NewUI(ZoneName, parent);
        Undo.RecordObject(zone, "Build UI Zone");
        PopupBuilder.Stretch(zone);

        SafeAreaZone safe = zone.GetComponent<SafeAreaZone>();
        if (safe == null) safe = Undo.AddComponent<SafeAreaZone>(zone.gameObject);
        SerializedObject so = new SerializedObject(safe);
        so.FindProperty("top").boolValue = top;
        so.FindProperty("bottom").boolValue = bottom;
        so.FindProperty("left").boolValue = left;
        so.FindProperty("right").boolValue = right;
        so.ApplyModifiedPropertiesWithoutUndo();
        return zone;
    }
}
