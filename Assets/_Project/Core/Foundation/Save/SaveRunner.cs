using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Object ẩn (tự tạo khi Play, sống qua đổi scene) lo ghi save: gom các thay đổi liên tiếp,
    /// ghi sau flushDelay giây kể từ thay đổi đầu tiên; luôn ghi khi app vào nền / mất focus / thoát
    /// (trên điện thoại app bị kill từ nền sẽ không có OnApplicationQuit).
    /// </summary>
    public class SaveRunner : MonoBehaviour
    {
        private const float FlushDelay = 0.5f;

        private static SaveRunner _instance;
        private readonly List<ISaveService> _services = new List<ISaveService>();
        private float _dirtySince = -1f;

        /// <summary>Hẹn ghi <paramref name="service"/> (JsonFileSaveService.MarkDirty gọi trong Play mode).</summary>
        public static void Ensure(ISaveService service)
        {
            Ensure();
            if (!_instance._services.Contains(service))
            {
                _instance._services.Add(service);
            }
        }

        public static void Ensure()
        {
            if (_instance == null)
            {
                // Không DontSave: Editor không hủy object DontSave khi thoát Play (rò sang Edit mode)
                GameObject go = new GameObject("[SaveRunner]") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SaveRunner>();
            }
            if (_instance._dirtySince < 0f)
            {
                _instance._dirtySince = Time.unscaledTime;
                _instance.StartCoroutine(_instance.FlushLater()); // chỉ chạy khi có thay đổi, không cần Update mỗi frame
            }
        }

        private IEnumerator FlushLater()
        {
            yield return new WaitForSecondsRealtime(FlushDelay);
            if (_dirtySince >= 0f) Flush();
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
            StopAllCoroutines(); // đã ghi sớm (vào nền / thoát): bỏ lượt chờ cũ
            _dirtySince = -1f;
            foreach (ISaveService service in _services)
            {
                service.SaveNow();
            }
        }
    }
}
