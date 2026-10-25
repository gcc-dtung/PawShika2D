using System;

namespace PawBlock.Features.Teams
{
    /// <summary>Section "teams" của save v2: đội đang ở và đội người chơi tự tạo.</summary>
    [Serializable]
    public class TeamSave
    {
        public const string Key = "teams";

        public string joinedId = "";   // trống = chưa vào đội
        public TeamInfo created = new TeamInfo(); // đội do người chơi tạo (id trống = chưa tạo; JsonUtility không lưu null)

        public bool HasCreated => created != null && !string.IsNullOrEmpty(created.id);
    }
}
