using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Ví sao (section "economy"): mỗi lần thắng cộng số sao của ván (LevelStars), Vòng quay may mắn tiêu sao.
    /// Giống CoinWallet: event Changed(số dư mới, thay đổi) trong Economy, StarsBalanceChangedEvent cho feature khác.
    /// </summary>
    public sealed class StarWallet : ICurrencyWallet
    {
        private readonly ISaveService _save;
        private readonly EconomySave _data;

        public event Action<int, int> Changed;

        public StarWallet(ISaveService save)
        {
            _save = save;
            _data = save.Get<EconomySave>(EconomySave.Key);
        }

        public int Balance => _data.stars;

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
            _data.stars = Mathf.Max(0, value);
            _save.MarkDirty();
            Changed?.Invoke(Balance, delta);
            EventBus<StarsBalanceChangedEvent>.Raise(new StarsBalanceChangedEvent(Balance, delta));
        }
    }
}
