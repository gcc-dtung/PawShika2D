using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Số coin của người chơi, lưu trong SaveData. Shop / IAP (Giai đoạn 6) sẽ cộng vào đây.
    /// Feature khác dùng qua ICurrencyWallet (new CoinWallet(): mọi instance cùng đọc / ghi SaveData);
    /// API static giữ tới P3 (Refactor) thì chuyển hẳn sang instance.
    /// </summary>
    public sealed class CoinWallet : ICurrencyWallet
    {
        int ICurrencyWallet.Balance => Balance;
        bool ICurrencyWallet.TrySpend(int amount) => TrySpend(amount);
        void ICurrencyWallet.Add(int amount) => Add(amount);

        /// <summary>(số dư mới, thay đổi)</summary>
        public static event Action<int, int> Changed;

        public static int Balance => SaveSystem.Data.coins;

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Set(Balance + amount, amount);
        }

        public static bool CanAfford(int amount) => Balance >= amount;

        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;
            Set(Balance - amount, -amount);
            return true;
        }

        private static void Set(int value, int delta)
        {
            SaveSystem.Data.coins = Mathf.Max(0, value);
            SaveSystem.MarkDirty();
            Changed?.Invoke(Balance, delta);
            EventBus<CoinsChangedEvent>.Raise(new CoinsChangedEvent(Balance, delta));
        }
    }
}
