using PawBlock.Core.Contracts;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Đóng băng (Freeze Time bản gốc): đồng hồ đứng yên LivesEnergyConfig.freezeSeconds giây (mặc định 20 s).</summary>
    public class FreezeBooster : BoosterEffect
    {
        public override BoosterType Type => BoosterType.Freeze;

        public override bool IsActive => Host.Timer != null && Host.Timer.IsFrozen;

        /// <summary>
        /// Trừ lượt ngay (hiệu ứng đồng hồ reo bắt đầu), đồng hồ thật sự đóng băng lúc bàn tay Spine bấm nút (sự kiện "Press" – FreezeOverlayFX).
        /// Trong lúc hiệu ứng, thao tác bị khóa nên đồng hồ đứng yên sẵn.
        /// </summary>
        public override void Activate()
        {
            LevelTimer timer = Host.Timer;
            if (timer == null || timer.IsFrozen || timer.IsTimeUp) return;
            Host.Consume(Type);
            Host.AfterEffect(Type, () =>
            {
                if (Host.Timer != null) Host.Timer.Freeze(Host.FreezeSeconds);
            });
        }
    }
}
