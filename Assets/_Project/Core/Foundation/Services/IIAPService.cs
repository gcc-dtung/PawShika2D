using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Mua bằng tiền thật (In-App Purchase). Hiện chỉ có FakeIAPService; tích hợp SDK thì thêm 1 class cài interface này.</summary>
    public interface IIAPService
    {
        /// <summary>(mã sản phẩm, lý do) – UI nghe để báo người chơi.</summary>
        event Action<string, string> PurchaseFailed;

        /// <summary>(mã sản phẩm) – mua thành công, nơi gọi tự trao quà qua onDone.</summary>
        event Action<string> PurchaseSucceeded;

        bool IsInitialized { get; }

        /// <summary>Mua sản phẩm <paramref name="productId"/>; onDone(true) khi đã thanh toán xong.</summary>
        void Purchase(string productId, Action<bool> onDone = null);
    }
}
