using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Số coin của người chơi (section "economy"). GameBootstrap tạo 1 instance, truyền vào nơi cần qua Init
    /// (feature khác nhận ICurrencyWallet). Mỗi lần đổi: event Changed (trong Economy) + CoinsChangedEvent (feature khác).
    /// </summary>
    public sealed class CoinWallet : ICurrencyWallet
    {
        private readonly ISaveService _save;
        private readonly EconomySave _data;

        /// <summary>(số dư mới, thay đổi)</summary>
        public event Action<int, int> Changed;

        public CoinWallet(ISaveService save)
        {
            _save = save;
            _data = save.Get<EconomySave>(EconomySave.Key);
        }

        public int Balance => _data.coins;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            Set(Balance + amount, amount);
        }

        public bool CanAfford(int amount) => Balance >= amount;

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount)
            {
                return false;
            }
            Set(Balance - amount, -amount);
            return true;
        }

        private void Set(int value, int delta)
        {
            _data.coins = Mathf.Max(0, value);
            _save.MarkDirty();
            Changed?.Invoke(Balance, delta);
            EventBus<CoinsChangedEvent>.Raise(new CoinsChangedEvent(Balance, delta));
        }
    }
}
