namespace PawBlock.Core.Contracts
{
    /// <summary>Người chơi bấm đúng nút trong tutorial mở khóa booster (bước tiếp theo riêng của từng booster nghe ở đây).</summary>
    public readonly struct BoosterTutorialCompletedEvent : IGameEvent
    {
        public readonly BoosterType Type;

        public BoosterTutorialCompletedEvent(BoosterType type)
        {
            Type = type;
        }
    }
}
