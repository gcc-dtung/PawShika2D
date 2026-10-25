using System;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Lưu tên loại (không lưu số enum) để thêm / đổi thứ tự BoosterType không làm lệch dữ liệu.</summary>
    [Serializable]
    public class BoosterSave
    {
        public string type;
        public int count;
    }
}
