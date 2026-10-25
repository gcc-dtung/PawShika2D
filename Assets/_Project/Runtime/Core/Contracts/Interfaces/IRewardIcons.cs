using UnityEngine;

namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Icon của 1 món quà (coin / booster / năng lượng vô hạn). DailyReward cài đặt (DailyRewardManager);
    /// ô quà Daily Reward, quà mốc Daily Challenge, ô trúng Vòng quay dùng chung.
    /// </summary>
    public interface IRewardIcons
    {
        Sprite IconOf(DailyRewardItem item);
    }
}
