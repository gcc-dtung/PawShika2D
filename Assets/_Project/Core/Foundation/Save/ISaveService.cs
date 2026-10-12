namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Dữ liệu người chơi chia theo section, mỗi feature sở hữu 1 section (key + class [Serializable] của mình).
    /// Get trả về object sống: sửa trực tiếp rồi gọi MarkDirty, service tự gom và ghi.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>Section <paramref name="key"/> (cùng 1 instance mỗi lần gọi). Chưa có trong file: tạo mới với giá trị mặc định.</summary>
        T Get<T>(string key) where T : class, new();

        /// <summary>Có thay đổi cần ghi: Play mode gom lại ghi sau (SaveRunner trong scene), ngoài Play / không có runner thì ghi ngay.</summary>
        void MarkDirty();

        /// <summary>Ghi ngay nếu đang có thay đổi.</summary>
        void SaveNow();
    }
}
