using UnityEngine;

namespace PawBlock.Core.Contracts
{
    /// <summary>Thông tin hiển thị của booster (icon to cho ô quà). Gameplay cài đặt (BoosterManager).</summary>
    public interface IBoosterCatalog
    {
        /// <summary>Icon to không nền của booster (null nếu không có trong database).</summary>
        Sprite ItemIcon(BoosterType type);
    }
}
