using System;
using UnityEngine;

/// <summary>Số lượt mỗi booster, lưu PlayerPrefs "PawBlock_Booster_{type}". Lần đầu đọc thì tặng valueStart.</summary>
public static class BoosterInventory
{
    private const string KeyPrefix = "PawBlock_Booster_";

    /// <summary>(loại, số lượt mới)</summary>
    public static event Action<BoosterType, int> Changed;

    public static int Get(BoosterData data)
    {
        string key = Key(data.boosterType);
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, Mathf.Max(0, data.valueStart));
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetInt(key);
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

    private static void Set(BoosterType type, int value)
    {
        PlayerPrefs.SetInt(Key(type), Mathf.Max(0, value));
        PlayerPrefs.Save();
        Changed?.Invoke(type, value);
    }

    private static string Key(BoosterType type) => KeyPrefix + type;
}
