using System;

namespace PawBlock.Features.Profile
{
    /// <summary>Hồ sơ người chơi máy này (PlayerProfileService) + chỉ số (ProfileStatsRecorder).</summary>
    [Serializable]
    public class ProfileSave
    {
        public const string Key = "profile";

        public string playerId;    // 8 số ngẫu nhiên, tạo lần đầu
        public string name;        // tên hiển thị (đổi được)
        public int avatar;         // chỉ số trong AvatarDatabase
        public long createdAt;     // unix giây, lần đầu mở game (từ lúc có hồ sơ)
        public int firstTryWins;
        public long coinsEarned;
        public int levelsCleared;  // level cao nhất đã thắng
        public int hardWins;
        public int boostersUsed;
        public int winStreak;      // chuỗi thắng hiện tại
        public int bestWinStreak;
    }
}
