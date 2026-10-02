using System;
using UnityEngine;

/// <summary>Số coin của người chơi, lưu PlayerPrefs. Shop / IAP (Giai đoạn 6) sẽ cộng vào đây.</summary>
public static class CoinWallet
{
    private const string Key = "PawBlock_Coins";

    /// <summary>(số dư mới, thay đổi)</summary>
    public static event Action<int, int> Changed;

    public static int Balance => PlayerPrefs.GetInt(Key, 0);

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
        PlayerPrefs.SetInt(Key, Mathf.Max(0, value));
        PlayerPrefs.Save();
        Changed?.Invoke(Balance, delta);
    }
}
