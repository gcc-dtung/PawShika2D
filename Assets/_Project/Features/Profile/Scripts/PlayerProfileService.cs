using System;
using System.Text;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Hồ sơ người chơi máy này (section "profile"): tạo lần đầu (ID 8 số ngẫu nhiên, tên mặc định, ngày bắt đầu),
    /// đổi tên / avatar, dựng PlayerProfile để hiển thị. Chỉ số do ProfileStatsRecorder ghi.
    /// GameBootstrap tạo 1 instance; level hiện tại (của Gameplay) do Game truyền vào qua <c>currentLevel</c>.
    /// </summary>
    public sealed class PlayerProfileService
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 16;

        private readonly ISaveService _save;
        private readonly ProfileSave _data;
        private readonly Func<int> _currentLevel;
        private readonly IClock _clock;

        /// <summary>Tên / avatar / chỉ số đổi: nút avatar, popup hồ sơ vẽ lại.</summary>
        public event Action Changed;

        /// <param name="currentLevel">Chỉ số level thường hiện tại (bắt đầu từ 0).</param>
        public PlayerProfileService(ISaveService save, IClock clock, Func<int> currentLevel)
        {
            _save = save;
            _clock = clock;
            _data = save.Get<ProfileSave>(ProfileSave.Key);
            _currentLevel = currentLevel ?? (() => 0);
            EnsureCreated(_data);
        }

        public ProfileSave Save => _data;

        public PlayerProfile Local()
        {
            ProfileSave p = _data;
            int currentLevel = _currentLevel();
            int levelsCleared = Math.Max(p.levelsCleared, currentLevel); // save cũ chưa có số này: level hiện tại - 1
            ProfileStat[] stats =
            {
                new ProfileStat(ProfileStatType.FirstTryWins, p.firstTryWins),
                new ProfileStat(ProfileStatType.CoinsEarned, p.coinsEarned),
                new ProfileStat(ProfileStatType.LevelsCleared, levelsCleared),
                new ProfileStat(ProfileStatType.HardWins, p.hardWins),
                new ProfileStat(ProfileStatType.BoostersUsed, p.boostersUsed),
                new ProfileStat(ProfileStatType.BestWinStreak, p.bestWinStreak)
            };
            return new PlayerProfile(p.playerId, p.name, p.avatar, p.createdAt, currentLevel + 1, stats, true);
        }

        /// <summary>Đổi tên (cắt khoảng trắng, bỏ ký tự điều khiển, 2..16 ký tự; ô tên tắt rich text). Trả về false nếu tên không hợp lệ.</summary>
        public bool Rename(string name)
        {
            string clean = Clean(name);
            if (clean.Length < NameMinLength) return false;
            ProfileSave p = _data;
            if (p.name == clean) return true;
            p.name = clean;
            _save.MarkDirty();
            Changed?.Invoke();
            return true;
        }

        public void SetAvatar(int avatarId)
        {
            ProfileSave p = _data;
            if (avatarId < 0 || p.avatar == avatarId) return;
            p.avatar = avatarId;
            _save.MarkDirty();
            Changed?.Invoke();
        }

        /// <summary>ProfileStatsRecorder gọi sau khi cập nhật chỉ số: ghi save + báo UI.</summary>
        public void NotifyStatsChanged()
        {
            _save.MarkDirty();
            Changed?.Invoke();
        }

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

        private void EnsureCreated(ProfileSave p)
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
                p.createdAt = _clock != null ? _clock.UtcNowSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                changed = true;
            }
            if (changed) _save.MarkDirty();
        }
    }
}
