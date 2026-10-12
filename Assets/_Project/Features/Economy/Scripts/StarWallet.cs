using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Ví sao (SaveData.stars): mỗi lần thắng cộng số sao của ván (LevelStars), Vòng quay may mắn tiêu sao.
    /// Giống CoinWallet: sự kiện Changed(số dư mới, thay đổi); feature khác dùng qua ICurrencyWallet (new StarWallet()).
    /// </summary>
    public sealed class StarWallet : ICurrencyWallet
    {
        int ICurrencyWallet.Balance => Balance;
        bool ICurrencyWallet.TrySpend(int amount) => TrySpend(amount);
        void ICurrencyWallet.Add(int amount) => Add(amount);

        public static event Action<int, int> Changed;

        public static int Balance => SaveSystem.Data.stars;

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
            SaveSystem.Data.stars = Mathf.Max(0, value);
            SaveSystem.MarkDirty();
            Changed?.Invoke(Balance, delta);
            EventBus<StarsBalanceChangedEvent>.Raise(new StarsBalanceChangedEvent(Balance, delta));
        }
    }
}
