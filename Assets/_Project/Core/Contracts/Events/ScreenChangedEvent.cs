namespace PawBlock.Core.Contracts
{
    /// <summary>ScreenFlow chuyển giữa Main Menu và màn chơi.</summary>
    public readonly struct ScreenChangedEvent : IGameEvent
    {
        public readonly bool IsMenu;

        public ScreenChangedEvent(bool isMenu)
        {
            IsMenu = isMenu;
        }
    }
}
