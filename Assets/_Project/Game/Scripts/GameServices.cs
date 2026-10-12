using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Profile;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// Các service dùng chung GameBootstrap tạo 1 lần lúc mở game (C# thuần, không MonoBehaviour).
    /// Feature không giữ class này: Bootstrap chỉ truyền phần mỗi feature cần qua Init. Cheat / Debug Panel đọc ở đây.
    /// </summary>
    public sealed class GameServices
    {
        public JsonFileSaveService Save { get; }
        public CoinWallet Coins { get; }
        public StarWallet Stars { get; }
        public BoosterInventory Boosters { get; }
        public PlayerProfileService Profile { get; }
        public ShopService Shop { get; set; } // cần năng lượng + kho booster (component trong scene): Bootstrap gán sau

        public GameServices(JsonFileSaveService save)
        {
            Save = save;
            GameplaySave gameplay = save.Get<GameplaySave>(GameplaySave.Key);
            Coins = new CoinWallet(save);
            Stars = new StarWallet(save);
            Boosters = new BoosterInventory(save);
            Profile = new PlayerProfileService(save, () => gameplay.currentLevel);
        }
    }
}
