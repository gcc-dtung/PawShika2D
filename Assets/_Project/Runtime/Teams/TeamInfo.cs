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
        public int score; // xếp hạng danh sách (đội mạnh lên trên)

        public bool IsFull(int capacity) => members >= capacity;
    }

    /// <summary>1 thành viên hiển thị trong màn thông tin đội.</summary>
    public readonly struct TeamMember
    {
        public readonly string Name;
        public readonly int Avatar;
        public readonly int Level;
        public readonly bool IsLeader;
        public readonly bool IsLocal;

        public TeamMember(string name, int avatar, int level, bool isLeader, bool isLocal)
        {
            Name = name;
            Avatar = avatar;
            Level = level;
            IsLeader = isLeader;
            IsLocal = isLocal;
        }
    }

    public enum TeamJoinResult { Ok, AlreadyInTeam, Full, LevelTooLow, NotFound }

    public enum TeamCreateResult { Ok, AlreadyInTeam, InvalidName, NotEnoughCoins }
}
