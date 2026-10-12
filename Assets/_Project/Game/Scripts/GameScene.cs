using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Các object điều khiển trong scene mà Cheat / Debug Panel cần (GameBootstrap điền từ tham chiếu của nó).
    /// Chỉ Game dùng; feature nhận dependency qua Init, không qua class này.
    /// </summary>
    public sealed class GameScene
    {
        public ScreenFlow Screens { get; set; }
        public LevelManager Levels { get; set; }
        public GridManager Grid { get; set; }
        public BoosterManager Boosters { get; set; }
    }
}
