using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EnergyManager trên Manager (Giai đoạn 5). Năng lượng chỉ hiện ở Main Menu (Build Main Menu) và popup nạp (Build Popups);
/// trong ván không còn tim năng lượng (chỗ đó là nút cài đặt) -> xóa bản cũ trên HUD nếu còn. Chạy lại được.
/// </summary>
public static class EnergyBuilder
{
    [MenuItem("Tools/PawBlock/Build Energy")]
    public static void Build()
    {
        GameObject canvasGo = PopupBuilder.FindRoot("Canvas_HUD"); // tìm được cả khi HUD đang tắt (đang ở Menu)
        Transform top = canvasGo != null ? PopupBuilder.HudRoot(canvasGo).Find("TopPanel") : null;
        if (top != null) PopupBuilder.DestroyChild(top, "Energy");

        LevelManager lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm == null)
        {
            Debug.LogError("[EnergyBuilder] Không tìm thấy LevelManager.");
            return;
        }
        if (lm.GetComponent<EnergyManager>() == null) Undo.AddComponent<EnergyManager>(lm.gameObject);

        EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
        Debug.Log("[EnergyBuilder] Đã gắn EnergyManager.");
    }
}
