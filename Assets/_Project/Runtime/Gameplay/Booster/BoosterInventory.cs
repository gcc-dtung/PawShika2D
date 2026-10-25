using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Số lượt booster (section "gameplay"). Lần đầu đọc 1 booster thì tặng valueStart lượt.
    /// GameBootstrap tạo 1 instance, BoosterManager / BoosterButton nhận qua Init.
    /// </summary>
    public sealed class BoosterInventory
    {
        private readonly ISaveService _save;
        private readonly GameplaySave _data;

        /// <summary>(loại, số lượt mới)</summary>
        public event Action<BoosterType, int> Changed;

        public BoosterInventory(ISaveService save)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
        }

        public int Get(BoosterData data)
        {
            int count = _data.GetBooster(data.boosterType);
            if (count < 0)
            {
                count = Mathf.Max(0, data.valueStart);
                _data.SetBooster(data.boosterType, count);
                _save.MarkDirty();
            }
            return count;
        }

        public void Add(BoosterData data, int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            Set(data.boosterType, Get(data) + amount);
        }

        public bool TryConsume(BoosterData data)
        {
            int count = Get(data);
            if (count <= 0)
            {
                return false;
            }
            Set(data.boosterType, count - 1);
            return true;
        }

        /// <summary>Đặt thẳng số lượt (Debug Panel).</summary>
        public void SetCount(BoosterData data, int value) => Set(data.boosterType, value);

        private void Set(BoosterType type, int value)
        {
            _data.SetBooster(type, Mathf.Max(0, value));
            _save.MarkDirty();
            Changed?.Invoke(type, value);
        }
    }
}
