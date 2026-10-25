using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Lo ghi save trong Play mode (đặt sẵn trong scene cùng GameBootstrap, Bootstrap gắn vào JsonFileSaveService):
    /// gom các thay đổi liên tiếp, ghi sau FlushDelay giây kể từ thay đổi đầu tiên; luôn ghi khi app vào nền / mất focus / thoát
    /// (trên điện thoại app bị kill từ nền sẽ không có OnApplicationQuit).
    /// </summary>
    public class SaveRunner : MonoBehaviour
    {
        private const float FlushDelay = 0.5f;

        private readonly List<ISaveService> _services = new List<ISaveService>();
        private float _dirtySince = -1f;

        /// <summary>Hẹn ghi <paramref name="service"/> (JsonFileSaveService.MarkDirty gọi trong Play mode).</summary>
        public void Schedule(ISaveService service)
        {
            if (!_services.Contains(service))
            {
                _services.Add(service);
            }
            if (_dirtySince < 0f)
            {
                _dirtySince = Time.unscaledTime;
                StartCoroutine(FlushLater()); // chỉ chạy khi có thay đổi, không cần Update mỗi frame
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
