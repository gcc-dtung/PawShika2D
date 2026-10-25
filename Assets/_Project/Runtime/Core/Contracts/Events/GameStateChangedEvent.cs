namespace PawBlock.Core.Contracts
{
    /// <summary>Trạng thái ván đổi (Playing / Paused / Won / OutOfTime / Lost).</summary>
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly GameState State;

        public GameStateChangedEvent(GameState state)
        {
            State = state;
        }
    }
}
