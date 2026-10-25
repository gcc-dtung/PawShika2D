using PawBlock.Core.Contracts;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Tạo luật booster theo loại (Factory). Chỗ duy nhất trong code gameplay phải sửa khi thêm 1 booster mới.
    /// </summary>
    public static class BoosterEffectFactory
    {
        public static BoosterEffect Create(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hint: return new HintBooster();
                case BoosterType.Freeze: return new FreezeBooster();
                case BoosterType.Reveal: return new RevealBooster();
                default: return null;
            }
        }
    }
}
