using UnityEngine;

/// <summary>Mạng trong màn (LivesManager) và năng lượng ngoài màn (EnergyManager). Lấy qua GameConfig.LivesEnergy.</summary>
[CreateAssetMenu(menuName = "PawBlock/Config/Lives Energy Config", fileName = "LivesEnergyConfig")]
public class LivesEnergyConfig : ScriptableObject
{
    [Header("Mạng trong màn")]
    [Tooltip("Số lần đi sai được phép mỗi level")]
    [Min(1)] public int maxLives = 3;

    [Header("Năng lượng")]
    [Min(1)] public int maxEnergy = 5;
    [Tooltip("Hồi 1 năng lượng sau bao nhiêu giây (gamePlay_TimeRefillHeart = 1800 của bản gốc)")]
    [Min(1)] public int refillSeconds = 1800;
}
