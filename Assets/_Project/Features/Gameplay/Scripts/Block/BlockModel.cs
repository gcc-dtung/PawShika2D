using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Khối đã đặt đúng (class C# thuần): vùng, các ô chiếm, ô số và màu.
    /// Màu là màu của khung chọn ở lượt kéo tạo ra khối. PlacedBlockView chỉ đọc dữ liệu này để vẽ.
    /// </summary>
    public class BlockModel
    {
        private readonly List<CellModel> _cells;

        public RectInt Rect { get; }
        public CellModel ClueCell { get; }
        public ColorType Color { get; }
        public IReadOnlyList<CellModel> Cells => _cells;
        public int Area => Rect.width * Rect.height;

        public BlockModel(RectInt rect, List<CellModel> cells, CellModel clueCell, ColorType color)
        {
            Rect = rect;
            _cells = new List<CellModel>(cells);
            ClueCell = clueCell;
            Color = color;
        }

        public void Occupy()
        {
            foreach (CellModel cell in _cells) cell.SetBlock(this);
        }

        public void Release()
        {
            foreach (CellModel cell in _cells)
            {
                if (cell.Block == this) cell.SetBlock(null);
            }
        }
    }
}
