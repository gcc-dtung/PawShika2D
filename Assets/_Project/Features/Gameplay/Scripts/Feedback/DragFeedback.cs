using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Phản hồi trên lưới khi kéo / thả sai, đặt ngay tại ô số bị vi phạm (không đổi màu, chỉ chuyển động):
    /// - Trong lúc kéo: ô số nằm trong vùng kéo phóng to. Vùng vừa chuyển sang "chắc chắn sai" thì lắc nhẹ 1 nhịp:
    ///   ≥ 2 ô số -> các số lắc ngang; ô số còn khóa -> ổ khóa lắc; sai hướng -> số + ô hướng lắc theo trục sai; thừa ô -> số lắc;
    ///   sai góc / cạnh -> số + dấu lắc theo trục vùng kéo lố sang phía bên kia ô số.
    ///   Ngón tay đẩy vào khối đã đặt -> khối đó lùi theo hướng đẩy rồi bật về; đẩy qua tường -> tường cong ra rồi bật về.
    /// - Thả tay bị từ chối (GridManager.onInvalidMove): lắc lại mạnh hơn + nảy số; thiếu ô chỉ nảy nhẹ.
    ///   Chạm 1 ô số để xem không tính là sai (GridManager bỏ qua).
    /// Bong bóng đếm ô / khung chọn / âm thanh / rung do DragCountBubble, SelectionBoxView, GameAudioEvents, GameHapticEvents lo.
    /// </summary>
    public class DragFeedback : MonoBehaviour
    {
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid, GridSpawner spawner)
        {
            _grid = grid;
            _spawner = spawner;
        }

        [SerializeField] private bool clueHighlight = true;
        [Tooltip("Lắc ô số khi vùng kéo vừa chuyển sang sai")]
        [SerializeField] private bool dragShake = true;
        [Tooltip("Ngón tay đẩy vào khối đã đặt / tường: khối (tường) lùi rồi bật về")]
        [SerializeField] private bool blockPush = true;

        [Header("Thả tay bị từ chối")]
        [SerializeField] private bool releaseFeedback = true;
        [Tooltip("Độ mạnh lắc khi thả (1 = như lúc kéo)")]
        [SerializeField, Min(0f)] private float releaseShake = 1.4f;
        [Tooltip("Ô số còn khóa: lắc mạnh hơn nữa")]
        [SerializeField, Min(0f)] private float lockedShake = 1.7f;
        [SerializeField, Min(0f)] private float releasePunch = 0.22f;
        [Tooltip("Thiếu ô: chỉ nảy nhẹ (thường do nhấc tay sớm)")]
        [SerializeField, Min(0f)] private float tooSmallPunch = 0.12f;

        private GridSpawner _spawner;
        private List<CellView> _lit = new List<CellView>();
        private List<CellView> _next = new List<CellView>();
        private readonly List<CellView> _clues = new List<CellView>();
        private PlacementResult _invalidResult; // lỗi đang báo trong lượt kéo (Ready = chưa sai) -> chỉ lắc khi đổi
        private CellModel _invalidClue;
        private object _pushed;                 // khối / tường ngón tay đang đẩy vào (đẩy sang cái khác mới lùi tiếp)

        private void Start()
        {
            if (_spawner != null) _spawner.gridCellEvent.AddListener(OnGridSpawned);
            if (_grid != null)
            {
                _grid.onSelectionChanged.AddListener(OnSelectionChanged);
                _grid.onDragCellChanged.AddListener(OnDragCellChanged);
                _grid.onInvalidMove.AddListener(OnInvalidMove);
            }
        }

        private void OnDestroy()
        {
            if (_spawner != null) _spawner.gridCellEvent.RemoveListener(OnGridSpawned);
            if (_grid != null)
            {
                _grid.onSelectionChanged.RemoveListener(OnSelectionChanged);
                _grid.onDragCellChanged.RemoveListener(OnDragCellChanged);
                _grid.onInvalidMove.RemoveListener(OnInvalidMove);
            }
        }

        private void OnDisable() => ClearAll();

        private void OnGridSpawned(List<CellView> cells)
        {
            _lit.Clear(); // ô cũ đã bị xóa / Unbind (tự trả số về cỡ gốc)
            ResetInvalid();
        }

        // ---------- Trong lúc kéo ----------

        private void OnSelectionChanged(int count, SelectionHint hint)
        {
            GridManager grid = _grid;
            if (count == 0 || grid == null || !grid.IsSelecting || _spawner == null || _spawner.GetGrid == null)
            {
                ClearAll();
                return;
            }

            PlacementCheck check = grid.SelectionCheck;
            CollectClues(check.Rect, _next);
            if (clueHighlight)
            {
                foreach (CellView cell in _lit)
                {
                    if (cell != null && !_next.Contains(cell)) cell.SetHighlighted(false);
                }
                foreach (CellView cell in _next) cell.SetHighlighted(true);
            }
            (_lit, _next) = (_next, _lit);

            // Chỉ lắc khi vừa sai, hoặc sai sang luật khác / ô số khác (kéo qua lại trong cùng 1 lỗi thì thôi)
            PlacementResult result = hint == SelectionHint.Invalid ? check.Result : PlacementResult.Ready;
            if (result == _invalidResult && check.ClueCell == _invalidClue) return;
            _invalidResult = result;
            _invalidClue = check.ClueCell;
            if (dragShake && result != PlacementResult.Ready) PlayInvalid(check, _lit, 1f, 0f);
        }

        // Ngón tay ra ngoài vùng kéo (vùng bị chặn, không giãn theo) = đang đẩy vào vật cản sát mép vùng theo hướng đó.
        // Vật cản là tường / khối ngay ngoài mép vùng (không phải thứ dưới ngón tay): ngón tay đi xa hơn hay quay lại
        // vẫn là cùng vật cản nên chỉ lùi 1 lần; khối ở xa mà ngón tay lướt qua không bị tính.
        // GridManager gọi sau khi vùng kéo đã cập nhật theo ô mới.
        private void OnDragCellChanged(Vector2Int cell)
        {
            GridManager grid = _grid;
            if (!blockPush || grid == null || !grid.IsSelecting || grid.Board == null) return;

            RectInt rect = grid.SelectionRect;
            Vector2Int dir = new Vector2Int(
                cell.x < rect.xMin ? -1 : (cell.x >= rect.xMax ? 1 : 0),
                cell.y < rect.yMin ? -1 : (cell.y >= rect.yMax ? 1 : 0));

            // Ra ngoài chéo: thử từng trục, trục ngang trước
            object pushed = null;
            Vector2Int pushDir = Vector2Int.zero;
            if (dir.x != 0) pushed = ObstacleAhead(grid, cell, rect, pushDir = new Vector2Int(dir.x, 0));
            if (pushed == null && dir.y != 0) pushed = ObstacleAhead(grid, cell, rect, pushDir = new Vector2Int(0, dir.y));

            if (pushed == _pushed) return;
            _pushed = pushed;

            if (pushed is WallView wall) wall.PlayPushed(pushDir);
            else if (pushed is BlockModel block && grid.TryGetView(block, out PlacedBlockView view)) view.PlayPushed(pushDir);
        }

        // Ô mép vùng (cùng hàng / cột với ngón tay) và ô ngay ngoài mép theo dir: tường giữa 2 ô, hoặc khối đang nằm ở ô ngoài
        private object ObstacleAhead(GridManager grid, Vector2Int cell, RectInt rect, Vector2Int dir)
        {
            Vector2Int edge = new Vector2Int(
                Mathf.Clamp(cell.x, rect.xMin, rect.xMax - 1),
                Mathf.Clamp(cell.y, rect.yMin, rect.yMax - 1));
            Vector2Int next = edge + dir;
            WallView wall = _spawner.GetWallBetween(edge, next);
            if (wall != null) return wall;
            return grid.Board.GetBlockAt(next);
        }

        // ---------- Thả tay ----------

        private void OnInvalidMove(PlacementResult result, Vector3 center, Vector2 size)
        {
            if (!releaseFeedback || _grid == null || _spawner == null || _spawner.GetGrid == null) return;
            PlacementCheck check = _grid.LastRejected;
            CollectClues(check.Rect, _clues);
            PlayInvalid(check, _clues, releaseShake, releasePunch);
        }

        /// <summary>Phản hồi 1 vùng sai tại ô số bị vi phạm. punch = 0: chỉ lắc (lúc kéo).</summary>
        private void PlayInvalid(PlacementCheck check, List<CellView> clues, float shake, float punch)
        {
            CellView clue = FindView(check.ClueCell);
            switch (check.Result)
            {
                case PlacementResult.ClueCount: // 0 ô số thì không có gì để chỉ vào
                    if (clues.Count > 1)
                    {
                        foreach (CellView cell in clues) cell.PlayShake(Vector2.right, shake);
                    }
                    break;

                case PlacementResult.Locked:
                    if (clue == null) break;
                    clue.PlayShake(Vector2.right, punch > 0f ? lockedShake : shake);
                    clue.PlayPunch(punch);
                    break;

                case PlacementResult.WrongShape:
                    if (clue == null) break;
                    clue.PlayShake(WrongAxis(check.ClueCell.Clue.Constraint, check.Rect), shake);
                    clue.PlayPunch(punch);
                    break;

                case PlacementResult.WrongCorner:
                    if (clue == null) break;
                    clue.PlayShake(CornerAxis(check.ClueCell.Clue.Corner, check.Rect, check.ClueCell.Position), shake);
                    clue.PlayPunch(punch);
                    break;

                case PlacementResult.WrongSize:
                    if (clue == null) break;
                    if (check.Cells != null && check.Cells.Count < check.ClueCell.Clue.Value)
                    {
                        clue.PlayPunch(punch > 0f ? tooSmallPunch : 0f); // thiếu ô: lúc kéo là bình thường, thả thì nảy nhẹ
                        break;
                    }
                    clue.PlayShake(Vector2.right, shake);
                    clue.PlayPunch(punch);
                    break;
            }
        }

        // Trục người chơi kéo sai: ô ngang mà kéo dọc -> lắc dọc; ô dọc mà kéo ngang -> lắc ngang; ô vuông -> theo cạnh dài
        private static Vector2 WrongAxis(ClueConstraint constraint, RectInt rect)
        {
            switch (constraint)
            {
                case ClueConstraint.Horizontal: return Vector2.up;
                case ClueConstraint.Vertical: return Vector2.right;
                default: return rect.height > rect.width ? Vector2.up : Vector2.right;
            }
        }

        // Trục vùng kéo lố khỏi góc / cạnh: ô số phải ở mép trái / phải mà vùng lố theo chiều ngang -> lắc ngang, còn lại lắc dọc
        private static Vector2 CornerAxis(ClueCorner corner, RectInt rect, Vector2Int clue)
        {
            if (corner == ClueCorner.Top || corner == ClueCorner.Bottom) return Vector2.up;
            if (corner == ClueCorner.Left || corner == ClueCorner.Right) return Vector2.right;
            bool left = corner == ClueCorner.TopLeft || corner == ClueCorner.BottomLeft;
            bool xWrong = left ? clue.x != rect.xMin : clue.x != rect.xMax - 1;
            return xWrong ? Vector2.right : Vector2.up;
        }

        private void CollectClues(RectInt rect, List<CellView> result)
        {
            result.Clear();
            foreach (CellView cell in _spawner.GetGrid)
            {
                if (cell != null && cell.Model != null && cell.Model.HasClue && rect.Contains(cell.Position)) result.Add(cell);
            }
        }

        private CellView FindView(CellModel model)
        {
            if (model == null) return null;
            foreach (CellView cell in _spawner.GetGrid)
            {
                if (cell != null && cell.Model == model) return cell;
            }
            return null;
        }

        private void ResetInvalid()
        {
            _invalidResult = PlacementResult.Ready;
            _invalidClue = null;
            _pushed = null;
        }

        private void ClearAll()
        {
            foreach (CellView cell in _lit)
            {
                if (cell != null) cell.SetHighlighted(false);
            }
            _lit.Clear();
            ResetInvalid();
        }
    }
}
