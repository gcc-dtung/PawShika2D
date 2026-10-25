using System;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// IAP chưa tích hợp SDK (Google Play Billing / App Store / Unity IAP): mọi lần mua đều thất bại như bản gốc
    /// hiện tại -> PurchaseFailed (GameFlowUI hiện popup NOTICE "Purchase Failed!").
    /// </summary>
    public sealed class FakeIAPService : IIAPService
    {
        public event Action<string, string> PurchaseFailed;
#pragma warning disable 0067 // chỉ SDK thật mới phát
        public event Action<string> PurchaseSucceeded;
#pragma warning restore 0067

        public bool IsInitialized => false;

        public void Purchase(string productId, Action<bool> onDone = null)
        {
            Debug.Log($"[IAP] Mua '{productId}': chưa có SDK thanh toán -> thất bại");
            PurchaseFailed?.Invoke(productId, "Purchase Failed!");
            onDone?.Invoke(false);
        }
    }
}
