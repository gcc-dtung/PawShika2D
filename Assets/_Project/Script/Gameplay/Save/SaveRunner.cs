using UnityEngine;

/// <summary>
/// Object ẩn (tự tạo khi Play, sống qua đổi scene) lo ghi save: gom các thay đổi liên tiếp,
/// ghi sau flushDelay giây kể từ thay đổi đầu tiên; luôn ghi khi app vào nền / mất focus / thoát
/// (trên điện thoại app bị kill từ nền sẽ không có OnApplicationQuit).
/// </summary>
public class SaveRunner : MonoBehaviour
{
    private const float FlushDelay = 0.5f;

    private static SaveRunner _instance;
    private float _dirtySince = -1f;

    public static void Ensure()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("[SaveRunner]") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SaveRunner>();
        }
        if (_instance._dirtySince < 0f) _instance._dirtySince = Time.unscaledTime;
    }

    private void Update()
    {
        if (_dirtySince < 0f || Time.unscaledTime - _dirtySince < FlushDelay) return;
        Flush();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Flush();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) Flush();
    }

    private void OnApplicationQuit() => Flush();

    private void OnDestroy()
    {
        Flush();
        if (_instance == this) _instance = null;
    }

    private void Flush()
    {
        _dirtySince = -1f;
        SaveSystem.Save();
    }
}
