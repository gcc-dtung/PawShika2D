using System;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy
{
    /// <summary>Số coin của người chơi, lưu trong SaveData. Shop / IAP (Giai đoạn 6) sẽ cộng vào đây.</summary>
    public static class CoinWallet
    {
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
        }
    }
}
