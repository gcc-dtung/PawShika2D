using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Quảng cáo có thưởng. Hiện chỉ có FakeAdsService; tích hợp MAX / AdMob thì thêm 1 class cài interface này.</summary>
    public interface IAdsService
    {
        /// <summary>Đang chiếu 1 quảng cáo (Show lúc này bị bỏ qua, không gọi onDone).</summary>
        bool IsShowing { get; }

        /// <summary>onDone(true) khi xem hết và được thưởng.</summary>
        void Show(string placement, Action<bool> onDone);
    }
}
