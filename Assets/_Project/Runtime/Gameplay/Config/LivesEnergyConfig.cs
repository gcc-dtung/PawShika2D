using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Đồng hồ trong màn (LevelTimer), sao, Freeze và năng lượng ngoài màn. Lấy qua GameConfig.livesEnergy;
    /// năng lượng (Economy) không biết class này: GameBootstrap truyền maxEnergy / refillSeconds cho EnergyManager.
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Config/Lives Energy Config", fileName = "LivesEnergyConfig")]
    public class LivesEnergyConfig : ScriptableObject
    {
        [Header("Đồng hồ trong màn (số của GameplayController gốc)")]
        [Tooltip("Thời gian nền mỗi level (giây)")]
        [Min(0)] public float baseSeconds = 8f;
        [Tooltip("Cộng thêm cho mỗi ô của lưới")]
        [Min(0)] public float secondsPerCell = 0.25f;
        [Tooltip("Cộng thêm cho mỗi khối (ô số), random trong khoảng min – max")]
        [Min(0)] public float secondsPerBlockMin = 5.5f;
        [Min(0)] public float secondsPerBlockMax = 6.5f;
        [Tooltip("Tổng thời gian tối thiểu")]
        [Min(1)] public float minSeconds = 45f;
        [Tooltip("Còn dưới số giây này thì đồng hồ đỏ + đập")]
        [Min(0)] public float warningSeconds = 10f;

        [Header("Sao theo thời gian còn lại (thanh TimeProgress gốc: mốc 60 / 20 / 0 %)")]
        [Tooltip("Còn trên tỉ lệ này của tổng thời gian thì giữ 3 sao")]
        [Range(0f, 1f)] public float threeStarRatio = 0.6f;
        [Tooltip("Còn trên tỉ lệ này thì giữ 2 sao; còn giây nào là 1 sao, hết giờ 0 sao")]
        [Range(0f, 1f)] public float twoStarRatio = 0.2f;

        [Header("Booster Đóng băng")]
        [Tooltip("Số giây đồng hồ đứng yên (Freeze Time gốc: 20 s)")]
        [Min(1)] public float freezeSeconds = 20f;

        [Header("Năng lượng")]
        [Min(1)] public int maxEnergy = 5;
        [Tooltip("Hồi 1 năng lượng sau bao nhiêu giây (gamePlay_TimeRefillHeart = 1800 của bản gốc)")]
        [Min(1)] public int refillSeconds = 1800;
    }
}
