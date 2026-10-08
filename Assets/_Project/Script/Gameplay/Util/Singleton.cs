using UnityEngine;

/// <summary>
/// 1 instance trong scene, truy cập qua Instance (tự tìm nếu chưa Awake).
/// Bản trùng chỉ hủy chính component đó (không hủy cả GameObject, tránh xóa nhầm component khác đặt chung).
/// Code mới: ưu tiên [SerializeField] tham chiếu hoặc EventBus thay vì gọi .Instance từ khắp nơi.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _ins;

    public static T Instance
    {
        get
        {
            if (_ins == null) _ins = FindFirstObjectByType<T>();
            return _ins;
        }
    }

    protected virtual void Awake()
    {
        if (_ins == null || _ins == this)
        {
            _ins = this as T;
            return;
        }
        Debug.LogWarning($"[Singleton] Có 2 {typeof(T).Name} ({_ins.name}, {name}): hủy component trùng trên {name}");
        Destroy(this);
    }
}

/// <summary>Như Singleton nhưng sống qua các scene. Bản trùng (scene nạp lại) hủy cả GameObject của nó.</summary>
public class DontDestroyOnLoadSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _ins;

    public static T Instance
    {
        get
        {
            if (_ins == null) _ins = FindFirstObjectByType<T>();
            return _ins;
        }
    }

    protected virtual void Awake()
    {
        if (_ins == null || _ins == this)
        {
            _ins = this as T;
            DontDestroyOnLoad(gameObject);
            return;
        }
        Destroy(gameObject);
    }
}
