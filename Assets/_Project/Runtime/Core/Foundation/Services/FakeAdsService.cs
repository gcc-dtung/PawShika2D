using System;
using DG.Tweening;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Quảng cáo giả lập (chưa có SDK): chờ <c>duration</c> giây thời gian thực rồi coi như xem xong, luôn được thưởng.
    /// Dispose (GameBootstrap tắt) thì hủy lượt đang chờ, không gọi onDone.
    /// </summary>
    public sealed class FakeAdsService : IAdsService, IDisposable
    {
        private readonly float _duration;
        private Tween _simulated;

        public FakeAdsService(float duration) => _duration = duration;

        public bool IsShowing => _simulated != null;

        public void Show(string placement, Action<bool> onDone)
        {
            if (IsShowing)
            {
                return;
            }
            Debug.Log($"[Ads] Giả lập quảng cáo: {placement}");
            _simulated = DOVirtual.DelayedCall(_duration, () =>
            {
                _simulated = null;
                onDone?.Invoke(true);
            }, ignoreTimeScale: true);
        }

        public void Dispose()
        {
            _simulated?.Kill();
            _simulated = null;
        }
    }
}
