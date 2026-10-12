using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>Object ẩn chạy coroutine đồng bộ của TrustedClockService; quay lại app thì đồng bộ lại.</summary>
    public class TrustedClockRunner : MonoBehaviour
    {
        private TrustedClockService _clock;

        public void Init(TrustedClockService clock) => _clock = clock;

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                _clock?.RequestSync();
            }
        }
    }
}
