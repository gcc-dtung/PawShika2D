using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>1 ô của Vòng quay may mắn: phần quà (dùng chung DailyRewardItem với điểm danh), tỉ lệ ra, màu ô.</summary>
    [Serializable]
    public class LuckyWheelSegment
    {
        public DailyRewardItem item = new DailyRewardItem();
        [Tooltip("Trọng số ra ô này (tỉ lệ = trọng số / tổng trọng số)")]
        [Min(0)] public int weight = 10;
        public Color color = Color.white;
    }

    /// <summary>
    /// Cấu hình Vòng quay may mắn: giá mỗi lần quay (sao), các ô theo chiều kim đồng hồ từ đỉnh.
    /// Sửa trong Inspector, popup tự đọc lúc chạy (đổi số ô thì sửa vòng trong prefab LuckyWheelPopup cho khớp).
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Lucky Wheel Database", fileName = "LuckyWheelDatabase")]
    public class LuckyWheelDatabase : ScriptableObject
    {
        [Tooltip("Số sao mỗi lần quay (ví sao: mỗi lần thắng cộng số sao của ván)")]
        [Min(1)] public int spinCost = 10;
        public List<LuckyWheelSegment> segments = new List<LuckyWheelSegment>();
    }
}
