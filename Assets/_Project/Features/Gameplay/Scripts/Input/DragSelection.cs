using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Vùng đang kéo (class C# thuần). Vùng luôn đi từ ô bắt đầu (Anchor) tới ô đích (Target) và
    /// KHÔNG BAO GIỜ chứa khối đã đặt hay ô Void: ngón tay đi tới đâu mà vùng bị chặn thì giữ vùng hợp lệ trước đó.
    /// Ngón tay nhảy nhiều ô trong 1 frame thì đi lần lượt qua từng ô ở giữa để không bị kẹt khi vuốt nhanh.
    /// </summary>
    public class DragSelection
    {
        public bool IsActive { get; private set; }
        public Vector2Int Anchor { get; private set; }
        public Vector2Int Target { get; private set; }
        public Vector2Int Finger { get; private set; }
        public RectInt Rect => BoardModel.RectFrom(Anchor, Target);
        public int CellCount => Rect.width * Rect.height;

        /// <summary>Hướng ngón tay đang đẩy vào vật cản (-1/0/1 mỗi trục); (0, 0) khi không bị chặn.</summary>
        public Vector2Int BlockDir
        {
            get
            {
                // Không bị chặn thì ô dưới ngón tay luôn nằm trong vùng; nằm ngoài mép nào thì mép đó đang bị chặn
                RectInt r = Rect;
                int x = Finger.x < r.xMin ? -1 : (Finger.x >= r.xMax ? 1 : 0);
                int y = Finger.y < r.yMin ? -1 : (Finger.y >= r.yMax ? 1 : 0);
                return new Vector2Int(x, y);
            }
        }

        public void Begin(Vector2Int anchor)
        {
            IsActive = true;
            Anchor = Target = Finger = anchor;
        }

        public void End()
        {
            IsActive = false;
        }

        /// <summary>Ngón tay sang ô mới. Trả về true nếu vùng chọn đổi.</summary>
        public bool MoveTo(Vector2Int finger, BoardModel board)
        {
            if (!IsActive || finger == Finger) return false;

            Vector2Int oldTarget = Target;

            // Đi từng ô trên đường thẳng từ vị trí cũ tới vị trí mới của ngón tay
            Vector2Int from = Finger;
            int steps = Mathf.Max(Mathf.Abs(finger.x - from.x), Mathf.Abs(finger.y - from.y));
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2Int step = new Vector2Int(
                    Mathf.RoundToInt(Mathf.Lerp(from.x, finger.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(from.y, finger.y, t)));
                if (board.IsAreaFree(BoardModel.RectFrom(Anchor, step))) Target = step;
            }
            Finger = finger;
            return Target != oldTarget;
        }
    }
}
