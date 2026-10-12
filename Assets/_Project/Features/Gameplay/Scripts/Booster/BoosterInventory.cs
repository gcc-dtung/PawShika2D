using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Số lượt mỗi booster, lưu trong SaveData. Lần đầu đọc thì tặng valueStart.</summary>
    public static class BoosterInventory
    {
        /// <summary>(loại, số lượt mới)</summary>
        public static event Action<BoosterType, int> Changed;

        public static int Get(BoosterData data)
        {
            int count = SaveSystem.Data.GetBooster(data.boosterType);
            if (count < 0)
            {
                count = Mathf.Max(0, data.valueStart);
                SaveSystem.Data.SetBooster(data.boosterType, count);
                SaveSystem.MarkDirty();
            }
            return count;
        }

        public static void Add(BoosterData data, int amount)
        {
            if (amount <= 0) return;
            Set(data.boosterType, Get(data) + amount);
        }

        public static bool TryConsume(BoosterData data)
        {
            int count = Get(data);
            if (count <= 0) return false;
            Set(data.boosterType, count - 1);
            return true;
        }

        /// <summary>Đặt thẳng số lượt (Debug Panel).</summary>
        public static void SetCount(BoosterData data, int value) => Set(data.boosterType, value);

        private static void Set(BoosterType type, int value)
        {
            SaveSystem.Data.SetBooster(type, Mathf.Max(0, value));
            SaveSystem.MarkDirty();
            Changed?.Invoke(type, value);
        }
    }
}
