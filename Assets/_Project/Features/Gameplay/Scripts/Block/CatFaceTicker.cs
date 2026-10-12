using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Object ẩn (tự tạo khi mặt mèo đầu tiên chạy, sống qua đổi scene): 1 Update duy nhất chạy khung hình cho mọi CatFaceAnimator,
    /// thay vì mỗi khối mèo một Update.
    /// </summary>
    public class CatFaceTicker : MonoBehaviour
    {
        private static CatFaceTicker _instance;

        /// <summary>Không đặt sẵn trong scene: chưa có thì tạo (Awake giữ instance + DontDestroyOnLoad).</summary>
        public static void Ensure()
        {
            if (_instance != null) return;
            // Không DontSave: Editor không hủy object DontSave khi thoát Play (rò sang Edit mode)
            new GameObject("[CatFaceTicker]") { hideFlags = HideFlags.HideInHierarchy }.AddComponent<CatFaceTicker>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update() => CatFaceAnimator.TickAll(Time.time);
    }
}
