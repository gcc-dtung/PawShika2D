/// <summary>Đóng băng (Freeze Time bản gốc): đồng hồ đứng yên LivesEnergyConfig.freezeSeconds giây (mặc định 20 s).</summary>
public class FreezeBooster : BoosterEffect
{
    public override BoosterType Type => BoosterType.Freeze;

    public override bool IsActive => Host.Timer != null && Host.Timer.IsFrozen;

    public override void Activate()
    {
        if (Host.Timer == null || !Host.Timer.Freeze(GameConfig.LivesEnergy.freezeSeconds)) return;
        Host.Consume(Type);
    }
}
