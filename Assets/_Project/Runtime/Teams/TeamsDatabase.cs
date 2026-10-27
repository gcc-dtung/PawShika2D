using UnityEngine;

namespace PawBlock.Features.Teams
{
    /// <summary>
    /// Cấu hình Đội (giả lập offline, chưa có server): logo, sức chứa, giá tạo, khoảng cấp độ yêu cầu,
    /// tên đội / thành viên giả để TeamService sinh danh sách cố định theo seed.
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Teams Database", fileName = "TeamsDatabase")]
    public class TeamsDatabase : ScriptableObject
    {
        public Sprite[] logos = new Sprite[0];
        public int capacity = 50;
        public int createCost = 3000;

        [Header("Cấp độ yêu cầu (ô chọn khi tạo đội)")]
        public int minRequiredLevel = 1;
        public int maxRequiredLevel = 200;
        public int requiredLevelStep = 5;
        public int defaultRequiredLevel = 1;

        [Header("Bảng tin My Team")]
        public int feedMax = 40;
        public int messageMaxLength = 80;
        [Tooltip("Mỗi lần xin được tối đa bao nhiêu mạng")]
        public int lifeRequestMax = 5;
        public int lifeRequestCooldownHours = 4;
        [Tooltip("Đồng đội giả giúp 1 mạng sau mỗi chừng này phút")]
        public int lifeHelpMinutes = 20;
        [Tooltip("Tin nhắn đồng đội giả gửi khi người chơi vào đội")]
        public string[] greetings = { "Hi", "Welcome!", "Hello :)" };

        [Header("Đội giả")]
        public int seed = 2026;
        public string[] teamNames = new string[0];
        [TextArea] public string[] teamDescriptions = new string[0];
        public string[] memberNames = new string[0];
        [Tooltip("Số avatar để random cho thành viên giả (lấy theo AvatarDatabase)")]
        public int avatarCount = 8;

        public Sprite Logo(int index) => logos != null && logos.Length > 0 ? logos[Mathf.Abs(index) % logos.Length] : null;
    }
}
