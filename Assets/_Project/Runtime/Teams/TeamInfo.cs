using System;

namespace PawBlock.Features.Teams
{
    /// <summary>1 đội (giả lập hoặc do người chơi tạo). Đội người chơi tạo được lưu nguyên trong TeamSave.</summary>
    [Serializable]
    public class TeamInfo
    {
        public string id;
        public string name;
        public string description;
        public int logo;
        public int requiredLevel;
        public int members;
        public int score;    // điểm đội, xếp hạng danh sách (đội mạnh lên trên)
        public int activity; // TeamActivity

        public bool IsFull(int capacity) => members >= capacity;
    }

    public enum TeamActivity { Low, Medium, High }

    /// <summary>1 thành viên hiển thị trong màn thông tin đội.</summary>
    public readonly struct TeamMember
    {
        public readonly string Name;
        public readonly int Avatar;
        public readonly int Level;
        public readonly bool IsLeader;
        public readonly bool IsLocal;
        /// <summary>Số ngày từ lần chơi cuối (0 = hôm nay).</summary>
        public readonly int LastSeenDays;

        public TeamMember(string name, int avatar, int level, bool isLeader, bool isLocal, int lastSeenDays = 0)
        {
            Name = name;
            Avatar = avatar;
            Level = level;
            IsLeader = isLeader;
            IsLocal = isLocal;
            LastSeenDays = lastSeenDays;
        }
    }

    public enum TeamFeedType { Joined, Chat, LifeRequest }

    /// <summary>1 dòng bảng tin "My Team": vào đội, tin nhắn, xin mạng (lưu trong TeamSave).</summary>
    [Serializable]
    public class TeamFeedItem
    {
        public int type;     // TeamFeedType
        public string name;
        public int avatar;
        public string text;
        public long time;    // UTC giây
        public bool mine;
        public int helped;   // xin mạng: số mạng đã nhận

        public TeamFeedType Type => (TeamFeedType)type;
    }

    public enum TeamJoinResult { Ok, AlreadyInTeam, Full, LevelTooLow, NotFound }

    public enum TeamCreateResult { Ok, AlreadyInTeam, InvalidName, NotEnoughCoins }
}
