using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Trọng số công thức độ khó lý thuyết (Tools → PawBlock → Level Difficulty). Công thức: Assets/Docs/DifficultyFormula.md.
    /// Trọng số mặc định là ước lượng ban đầu; khi có dữ liệu thực tế thì hồi quy lại từ CSV xuất ra.
    /// </summary>
    public class LevelDifficultySettings : ScriptableObject
    {
        [Header("Điểm độ khó D")]
        [Tooltip("× log2(số khối)")]
        public float wSize = 5f;
        [Tooltip("× độ mơ hồ ban đầu: trung bình log2(số cách đặt) của mỗi số lúc chưa đặt gì")]
        public float wAmbiguity = 4f;
        [Tooltip("× tỉ lệ bước cần kỹ thuật 2 (loại trừ 1 bước)")]
        public float wT2 = 25f;
        [Tooltip("× tỉ lệ bước cần kỹ thuật 3 (thử sâu / phản chứng)")]
        public float wT3 = 60f;
        [Tooltip("× tỉ lệ bước không có nước bắt buộc (T4). Chỉ gặp ở level nhiều lời giải: chọn cách nào còn giải tiếp được cũng đúng, nhưng phải tránh cách sai")]
        public float wT4 = 40f;
        [Tooltip("× tỉ lệ bước chỉ có đúng 1 nước chắc chắn (đường hẹp, dễ kẹt)")]
        public float wNarrow = 10f;
        [Tooltip("+ mỗi ô \"?\" (phải nhớ, không biết số)")]
        public float wHidden = 3f;
        [Tooltip("+ mỗi ô khóa (phải giải chỗ khác trước)")]
        public float wLock = 2f;

        [Header("Thời gian ước tính (giây)")]
        public float timeBase = 5f;
        [Tooltip("Giây mỗi ô chơi được (nhìn lưới, kéo khối)")]
        public float timePerCell = 0.15f;
        public float timeT1 = 3f;
        public float timeT2 = 6f;
        public float timeT3 = 12f;
        public float timeT4 = 20f;

        [Header("Rủi ro đặt sai (số lần đặt nhầm kỳ vọng)")]
        [Tooltip("Xác suất đặt sai ở bước kỹ thuật 2 / 3 / không có nước bắt buộc")]
        public float riskT2 = 0.05f;
        public float riskT3 = 0.25f;
        public float riskT4 = 0.3f;

        [Header("Gợi ý Hard / Very Hard (theo phân vị điểm D trong database)")]
        [Range(0f, 0.5f), Tooltip("Tỉ lệ level Very Hard (điểm cao nhất)")]
        public float veryHardShare = 0.04f;
        [Range(0f, 0.5f), Tooltip("Tỉ lệ level Hard (ngay dưới Very Hard)")]
        public float hardShare = 0.10f;
        [Tooltip("Không gợi ý Hard / Very Hard trước level này (giai đoạn làm quen)")]
        public int minLevelForHard = 15;
    }
}
