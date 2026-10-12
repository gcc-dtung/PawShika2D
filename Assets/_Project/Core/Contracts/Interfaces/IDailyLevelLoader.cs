namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Sinh level Daily Challenge của 1 ngày (cùng ngày luôn ra cùng level) và nạp vào màn chơi. Gameplay cài đặt
    /// (DailyLevelLoader); DailyChallenge gọi sau khi đã vào màn chơi. Ngày = số ngày từ 1/1/1970 (DayNumber).
    /// </summary>
    public interface IDailyLevelLoader
    {
        /// <summary>Trả về false nếu không sinh được level.</summary>
        bool LoadDaily(long day);
    }
}
