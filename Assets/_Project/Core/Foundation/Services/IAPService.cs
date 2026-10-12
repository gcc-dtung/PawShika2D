using System;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Mua bằng tiền thật (VND, In-App Purchase). Chưa tích hợp SDK (Google Play Billing / App Store / Unity IAP):
    /// mọi lần mua đều thất bại như bản gốc hiện tại -> phát PurchaseFailed (GameFlowUI hiện popup NOTICE "Purchase Failed!").
    /// Khi tích hợp: thay thân Purchase, nơi gọi giữ nguyên.
    /// </summary>
    public static class IAPService
    {
        /// <summary>(mã sản phẩm, lý do) – UI nghe để báo người chơi.</summary>
        public static event Action<string, string> PurchaseFailed;

        /// <summary>(mã sản phẩm) – mua thành công, nơi gọi tự trao quà qua onDone.</summary>
        public static event Action<string> PurchaseSucceeded;

        public static bool IsInitialized => false;

        /// <summary>Mua sản phẩm <paramref name="productId"/>; onDone(true) khi đã thanh toán xong.</summary>
        public static void Purchase(string productId, Action<bool> onDone = null)
        {
            Debug.Log($"[IAPService] Mua '{productId}': chưa có SDK thanh toán -> thất bại");
            Fail(productId, "Purchase Failed!", onDone);
        }

        private static void Fail(string productId, string reason, Action<bool> onDone)
        {
            PurchaseFailed?.Invoke(productId, reason);
            onDone?.Invoke(false);
        }

        // Dùng khi có SDK
        private static void Succeed(string productId, Action<bool> onDone)
        {
            PurchaseSucceeded?.Invoke(productId);
            onDone?.Invoke(true);
        }
    }
}
