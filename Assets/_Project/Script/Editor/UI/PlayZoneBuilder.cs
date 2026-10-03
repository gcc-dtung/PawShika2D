using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng Canvas_HUD/PlayZone: vùng chơi giãn theo màn hình, nằm giữa HUD trên (LevelFrame) và thanh booster dưới;
/// gán cho LevelManager. Chạy lại được (giữ object cũ, đặt lại neo / lề).
/// </summary>
public static class PlayZoneBuilder
{
    // Lề tính theo canvas 1080 x (1920+) của Canvas_HUD
    private const float Top = 300f;    // dưới LevelFrame (cách đỉnh 60 + cao 220) + khoảng thở
    private const float Bottom = 310f; // trên hàng nút booster (đáy nút ~290)
    private const float Side = 30f;

    [MenuItem("Tools/PawBlock/Build Play Zone")]
    public static void Build()
    {
        GameObject hud = PopupBuilder.FindRoot("Canvas_HUD");
        if (hud == null)
        {
            Debug.LogError("[PlayZoneBuilder] Không tìm thấy Canvas_HUD trong scene.");
            return;
        }

        Transform root = PopupBuilder.HudRoot(hud); // trong UIZone (safe area) nếu đã có: tai thỏ đẩy HUD xuống thì vùng chơi xuống theo
        Transform t = root.Find("PlayZone");
        RectTransform zone = t != null ? (RectTransform)t : PopupBuilder.NewUI("PlayZone", root);
        Undo.RecordObject(zone, "Build Play Zone");
        zone.anchorMin = Vector2.zero;
        zone.anchorMax = Vector2.one;
        zone.pivot = new Vector2(0.5f, 0.5f);
        zone.offsetMin = new Vector2(Side, Bottom);
        zone.offsetMax = new Vector2(-Side, -Top);
        zone.SetSiblingIndex(0); // sau HUD, khi bật tô màu debug không đè nút

        Image img = zone.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(zone.gameObject);
        img.raycastTarget = false;

        PlayZone pz = zone.GetComponent<PlayZone>();
        if (pz == null) pz = Undo.AddComponent<PlayZone>(zone.gameObject);
        PopupBuilder.Assign(pz, "targetCamera", Camera.main);
        img.enabled = new SerializedObject(pz).FindProperty("showZone").boolValue;

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null) PopupBuilder.Assign(lm, "playZone", pz);

        EditorSceneManager.MarkSceneDirty(hud.scene);
        Debug.Log("[PlayZoneBuilder] Đã dựng Canvas_HUD/PlayZone và gán cho LevelManager.");
    }
}
