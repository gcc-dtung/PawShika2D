using System;

namespace PawBlock.Features.Economy
{
    /// <summary>Section "economy" của save v2: ví coin, ví sao, năng lượng.</summary>
    [Serializable]
    public class EconomySave
    {
        public const string Key = "economy";

        public int coins;
        public int stars; // ví sao: cộng số sao mỗi lần thắng (StarWallet), tiêu ở Vòng quay may mắn
        public EnergySave energy = new EnergySave();
    }
}
