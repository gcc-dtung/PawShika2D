using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>Quà 1 mốc Daily Challenge (thắng đủ milestone ngày trong tháng).</summary>
    [Serializable]
    public class DailyChallengeGift
    {
        public int milestone;
        [Tooltip("Hộp quà hiện trên thanh tiến trình (hop_qua_1_1 / 2_1 / 3_1)")]
        public Sprite box;
        public List<DailyRewardItem> items = new List<DailyRewardItem>();
    }

    /// <summary>
    /// Cấu hình Daily Challenge: mở từ level nào, quà 3 mốc (lấy từ DailyChallengeData.txt của bản gốc,
    /// ThirdParty/PawBlock/10_Data_Configs). Thông số sinh level mỗi ngày ở DailyLevelLoader (Gameplay).
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Daily Challenge Database", fileName = "DailyChallengeDatabase")]
    public class DailyChallengeDatabase : ScriptableObject
    {
        [Tooltip("Mở khi tới level này (đã thắng level trước nó). Gốc: SaveManager.DailyUnlockLevel = 21")]
        [Min(1)] public int unlockLevel = 21;
        [Tooltip("Ngày đã lỡ chơi bù được bằng quảng cáo (nút Play vàng)")]
        public bool replayMissedWithAd = true;
        public List<DailyChallengeGift> gifts = new List<DailyChallengeGift>();

        public DailyChallengeGift GetGift(int milestone) => gifts.Find(g => g.milestone == milestone);
    }
}
