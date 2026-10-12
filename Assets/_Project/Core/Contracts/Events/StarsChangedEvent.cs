namespace PawBlock.Core.Contracts
{
    /// <summary>Số sao của ván đổi (LevelStars): lưới mới về 3 sao (Lost = false) hoặc thời gian tụt qua 1 mốc (Lost = true).</summary>
    public readonly struct StarsChangedEvent : IGameEvent
    {
        public readonly int Stars;
        public readonly bool Lost;

        public StarsChangedEvent(int stars, bool lost)
        {
            Stars = stars;
            Lost = lost;
        }
    }
}
