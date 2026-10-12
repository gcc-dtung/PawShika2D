using System;
using System.Collections.Generic;
using System.Globalization;

namespace PawBlock.Features.Profile
{
    /// <summary>Các chỉ số trên hồ sơ. Thêm chỉ số: thêm giá trị cuối + nhãn trong ProfileStats.Label + icon trong ProfilePopup.</summary>
    public enum ProfileStatType
    {
        FirstTryWins = 0,  // thắng ngay lượt đầu tiên của level
        CoinsEarned = 1,   // tổng coin đã nhận
        LevelsCleared = 2, // số level đã qua
        HardWins = 3,      // thắng level Hard / Very Hard
        BoostersUsed = 4,
        BestWinStreak = 5  // chuỗi thắng liên tiếp dài nhất
    }

    public readonly struct ProfileStat
    {
        public readonly ProfileStatType Type;
        public readonly long Value;

        public ProfileStat(ProfileStatType type, long value)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>
    /// Hồ sơ 1 người chơi để hiển thị (dữ liệu thuần, chỉ đọc). Người chơi máy này: PlayerProfileService.Local().
    /// Sau này bảng xếp hạng tạo PlayerProfile từ dữ liệu server (IsLocal = false) rồi mở cùng ProfilePopup để xem người khác.
    /// </summary>
    public class PlayerProfile
    {
        public string PlayerId { get; }
        public string Name { get; }
        public int AvatarId { get; }
        public long JoinedUnix { get; }
        public int Level { get; }
        public IReadOnlyList<ProfileStat> Stats { get; }

        /// <summary>Hồ sơ của chính người chơi máy này: được đổi tên / avatar.</summary>
        public bool IsLocal { get; }

        public PlayerProfile(string playerId, string name, int avatarId, long joinedUnix, int level, IReadOnlyList<ProfileStat> stats, bool isLocal)
        {
            PlayerId = playerId;
            Name = name;
            AvatarId = avatarId;
            JoinedUnix = joinedUnix;
            Level = level;
            Stats = stats ?? Array.Empty<ProfileStat>();
            IsLocal = isLocal;
        }

        /// <summary>Tháng / năm bắt đầu chơi, vd "10/2026".</summary>
        public string JoinedText => JoinedUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(JoinedUnix).ToLocalTime().ToString("MM/yyyy", CultureInfo.InvariantCulture)
            : "--/----";
    }

    public static class ProfileStats
    {
        public static string Label(ProfileStatType type)
        {
            switch (type)
            {
                case ProfileStatType.FirstTryWins: return "FIRST TRY WINS";
                case ProfileStatType.CoinsEarned: return "COINS EARNED";
                case ProfileStatType.LevelsCleared: return "LEVELS CLEARED";
                case ProfileStatType.HardWins: return "HARD WINS";
                case ProfileStatType.BoostersUsed: return "BOOSTERS USED";
                case ProfileStatType.BestWinStreak: return "BEST STREAK";
                default: return type.ToString().ToUpperInvariant();
            }
        }

        /// <summary>1250 -> "1,250"; từ 1 triệu -> "1.2M".</summary>
        public static string Format(long value)
        {
            if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            return value.ToString("#,0", CultureInfo.InvariantCulture);
        }
    }
}
