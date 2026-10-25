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
