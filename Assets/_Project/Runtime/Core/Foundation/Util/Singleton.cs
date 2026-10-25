using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Singleton cho **hạ tầng** (guide 3, hiện chỉ AudioManager): 1 instance đặt sẵn trong scene, đăng ký ở Awake,
    /// xóa ở OnDestroy. Không tự tìm / tự tạo; thiếu thì Instance báo lỗi rõ. Hỏi mà không muốn báo lỗi: HasInstance.
    /// Game logic không dùng: nhận dependency qua Init từ GameBootstrap.
    /// Bản trùng chỉ hủy chính component đó (không hủy cả GameObject, tránh xóa nhầm component khác đặt chung).
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError($"[Singleton] {typeof(T).Name} chưa có trong scene (hoặc chưa Awake)");
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null || _instance == this)
            {
                _instance = this as T;
                return;
            }
            Debug.LogWarning($"[Singleton] Có 2 {typeof(T).Name} ({_instance.name}, {name}): hủy component trùng trên {name}");
            Destroy(this);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
