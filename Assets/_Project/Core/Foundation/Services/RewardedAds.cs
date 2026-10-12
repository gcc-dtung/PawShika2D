using System;
using DG.Tweening;
using UnityEngine;
using PawBlock.Game;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Quảng cáo có thưởng. Chưa có SDK (Giai đoạn 6): giả lập xem xong sau EconomyConfig.adsSimulatedDuration giây, luôn thành công.
    /// Thay thân ShowAd bằng MAX / AdMob khi tích hợp; nơi gọi giữ nguyên.
    /// </summary>
    public class RewardedAds : Singleton<RewardedAds>
    {
        private Tween _simulated;

        public bool IsShowing { get; private set; }

        // Tắt service giữa chừng: hủy hẹn giờ, không gọi onDone (popup cũng đang bị tắt theo scene)
        private void OnDisable()
        {
            _simulated?.Kill();
            _simulated = null;
            IsShowing = false;
        }

        /// <summary>onDone(true) khi xem hết và được thưởng.</summary>
        public static void Show(string placement, Action<bool> onDone)
        {
            RewardedAds ads = Instance;
            if (ads == null)
            {
                Debug.Log($"[RewardedAds] (không có service) giả lập xem xong: {placement}");
                onDone?.Invoke(true);
                return;
            }
            if (ads.IsShowing) return;
            ads.ShowAd(placement, onDone);
        }

        private void ShowAd(string placement, Action<bool> onDone)
        {
            IsShowing = true;
            Debug.Log($"[RewardedAds] Giả lập quảng cáo: {placement}");
            _simulated = DOVirtual.DelayedCall(GameConfig.Economy.adsSimulatedDuration, () =>
                {
                    _simulated = null;
                    IsShowing = false;
                    onDone?.Invoke(true);
                }, ignoreTimeScale: true)
                .SetLink(gameObject);
        }
    }
}
