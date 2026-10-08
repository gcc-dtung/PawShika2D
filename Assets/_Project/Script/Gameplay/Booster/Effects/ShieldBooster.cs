/// <summary>Khiên (thay Freeze Time): chặn lần sai kế tiếp trong level này (LivesManager bỏ qua 1 lần trừ mạng).</summary>
public class ShieldBooster : BoosterEffect
{
    public override BoosterType Type => BoosterType.Shield;

    public override bool IsActive => Host.Lives != null && Host.Lives.HasShield;

    public override void Activate()
    {
        if (Host.Lives == null || !Host.Lives.ActivateShield()) return;
        Host.Consume(Type);
    }
}
