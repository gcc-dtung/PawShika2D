using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>Chạy coroutine đồng bộ của TrustedClockService (đặt sẵn trong scene cùng GameBootstrap); quay lại app thì đồng bộ lại.</summary>
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
