using System;
using System.Collections.Generic;

namespace PawBlock.Features.Teams
{
    /// <summary>Section "teams" của save v2: đội đang ở, đội người chơi tự tạo, bảng tin, lần xin mạng cuối.</summary>
    [Serializable]
    public class TeamSave
    {
        public const string Key = "teams";

        public string joinedId = "";   // trống = chưa vào đội
        public TeamInfo created = new TeamInfo(); // đội do người chơi tạo (id trống = chưa tạo; JsonUtility không lưu null)
        public List<TeamFeedItem> feed = new List<TeamFeedItem>();
        public long lastLifeRequest;   // UTC giây

        public bool HasCreated => created != null && !string.IsNullOrEmpty(created.id);
    }
}
