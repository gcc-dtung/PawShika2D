using System;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Số liệu chơi thật của 1 level (LevelStatsRecorder), khóa theo tên asset level.</summary>
    [Serializable]
    public class LevelStatSave
    {
        public string id;            // tên asset LevelDefinition
        public int level;            // số thứ tự lúc chơi gần nhất
        public int attempts;         // số lượt bắt đầu
        public int wins;
        public int losses;           // thua hẳn (từ chối thêm giờ)
        public int quits;            // bỏ dở (chơi lại / đổi level / về Menu rồi chơi lại)
        public int outOfTime;        // số lần hết giờ (kể cả lần thêm giờ chơi tiếp)
        public int mistakes;         // nước đi sai (khối đặt rồi phải gỡ)
        public int boosters;         // booster đã dùng
        public float playSeconds;    // tổng thời gian ở trạng thái Playing
        public float bestWinSeconds; // lượt thắng nhanh nhất (0 = chưa thắng)
        public int bestStars;        // số sao cao nhất khi thắng (0..3, xem cùng wins > 0)
    }
}
