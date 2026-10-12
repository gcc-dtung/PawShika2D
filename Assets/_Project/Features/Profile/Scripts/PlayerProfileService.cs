using System;
using System.Text;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Hồ sơ người chơi máy này (lưu SaveData.profile): tạo lần đầu (ID 8 số ngẫu nhiên, tên mặc định, ngày bắt đầu),
    /// đổi tên / avatar, dựng PlayerProfile để hiển thị. Chỉ số do ProfileStatsRecorder ghi.
    /// </summary>
    public static class PlayerProfileService
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 16;

        /// <summary>Tên / avatar / chỉ số đổi: nút avatar, popup hồ sơ vẽ lại.</summary>
        public static event Action Changed;

        public static ProfileSave Save
        {
            get
            {
                SaveData data = SaveSystem.Data;
                if (data.profile == null) data.profile = new ProfileSave();
                EnsureCreated(data.profile);
                return data.profile;
            }
        }

        public static PlayerProfile Local()
        {
            ProfileSave p = Save;
            SaveData data = SaveSystem.Data;
            int levelsCleared = Math.Max(p.levelsCleared, data.currentLevel); // save cũ chưa có số này: level hiện tại - 1
            ProfileStat[] stats =
            {
                new ProfileStat(ProfileStatType.FirstTryWins, p.firstTryWins),
                new ProfileStat(ProfileStatType.CoinsEarned, p.coinsEarned),
                new ProfileStat(ProfileStatType.LevelsCleared, levelsCleared),
                new ProfileStat(ProfileStatType.HardWins, p.hardWins),
                new ProfileStat(ProfileStatType.BoostersUsed, p.boostersUsed),
                new ProfileStat(ProfileStatType.BestWinStreak, p.bestWinStreak)
            };
            return new PlayerProfile(p.playerId, p.name, p.avatar, p.createdAt, data.currentLevel + 1, stats, true);
        }

        /// <summary>Đổi tên (cắt khoảng trắng, bỏ ký tự điều khiển, 2..16 ký tự; ô tên tắt rich text). Trả về false nếu tên không hợp lệ.</summary>
        public static bool Rename(string name)
        {
            string clean = Clean(name);
            if (clean.Length < NameMinLength) return false;
            ProfileSave p = Save;
            if (p.name == clean) return true;
            p.name = clean;
            SaveSystem.MarkDirty();
            Changed?.Invoke();
            return true;
        }

        public static void SetAvatar(int avatarId)
        {
            ProfileSave p = Save;
            if (avatarId < 0 || p.avatar == avatarId) return;
            p.avatar = avatarId;
            SaveSystem.MarkDirty();
            Changed?.Invoke();
        }

        /// <summary>ProfileStatsRecorder gọi sau khi cập nhật chỉ số.</summary>
        public static void NotifyStatsChanged() => Changed?.Invoke();

        public static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            StringBuilder sb = new StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                if (char.IsControl(c)) continue;
                sb.Append(c);
            }
            string s = sb.ToString().Trim();
            return s.Length > NameMaxLength ? s.Substring(0, NameMaxLength).Trim() : s;
        }

        private static void EnsureCreated(ProfileSave p)
        {
            bool changed = false;
            if (string.IsNullOrEmpty(p.playerId))
            {
                p.playerId = new Random().Next(10_000_000, 100_000_000).ToString();
                changed = true;
            }
            if (string.IsNullOrEmpty(p.name))
            {
                p.name = "Player " + p.playerId.Substring(p.playerId.Length - 4);
                changed = true;
            }
            if (p.createdAt <= 0)
            {
                p.createdAt = TrustedClock.UtcNowSeconds;
                changed = true;
            }
            if (changed) SaveSystem.MarkDirty();
        }
    }
}
