using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.DailyReward
{
    [Serializable]
    public class DailyRewardDay
    {
        public List<DailyRewardItem> items = new List<DailyRewardItem>();
    }

    /// <summary>Quà điểm danh 7 ngày (ngày 7 là quà lớn) và icon hiển thị. Sửa trong Inspector, UI tự đọc lúc chạy.</summary>
    [CreateAssetMenu(menuName = "PawBlock/Daily Reward Database", fileName = "DailyRewardDatabase")]
    public class DailyRewardDatabase : ScriptableObject
    {
        public const int CycleDays = 7;

        public List<DailyRewardDay> days = new List<DailyRewardDay>();

        [Header("Icon")]
        public Sprite coinIcon;
        public Sprite infiniteEnergyIcon;

        [Header("Bù ngày lỡ")]
        [Tooltip("Ngày đã lỡ trong chu kỳ: xem quảng cáo để nhận bù")]
        public bool recoverMissedWithAd = true;

        public DailyRewardDay Get(int day) => day >= 0 && day < days.Count ? days[day] : null;
    }
}
