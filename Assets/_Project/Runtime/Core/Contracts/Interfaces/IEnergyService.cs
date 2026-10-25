namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Năng lượng chơi. Economy cài đặt (EnergyManager); Gameplay (bỏ dở level), Shop, DailyReward (thưởng vô hạn) dùng.
    /// </summary>
    public interface IEnergyService
    {
        /// <summary>Trừ 1 năng lượng (đang vô hạn thì không trừ). reason chỉ để log.</summary>
        void ConsumeOne(string reason);

        /// <summary>Năng lượng vô hạn thêm <paramref name="seconds"/> giây.</summary>
        void AddInfinite(int seconds);
    }
}
