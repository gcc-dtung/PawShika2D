using UnityEngine;

/// <summary>
/// Object ẩn (tự tạo khi mặt mèo đầu tiên chạy, sống qua đổi scene): 1 Update duy nhất chạy khung hình cho mọi CatFaceAnimator,
/// thay vì mỗi khối mèo một Update.
/// </summary>
public class CatFaceTicker : DontDestroyOnLoadSingleton<CatFaceTicker>
{
    /// <summary>Không đặt sẵn trong scene: chưa có thì tạo (Awake của lớp cha giữ Instance + DontDestroyOnLoad).</summary>
    public static void Ensure()
    {
        if (Instance != null) return;
        // Không DontSave: Editor không hủy object DontSave khi thoát Play (rò sang Edit mode)
        new GameObject("[CatFaceTicker]") { hideFlags = HideFlags.HideInHierarchy }.AddComponent<CatFaceTicker>();
    }

    private void Update() => CatFaceAnimator.TickAll(Time.time);
}
