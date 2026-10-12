using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Chạm / kéo trên lưới: kéo từ ô trống -> GridManager chọn vùng; chạm 1 khối đã đặt (thả tay trên chính khối đó) -> gỡ khối;
    /// đang Reveal thì chuyển chạm cho BoosterManager.
    /// </summary>
    public class DragAndDropController : MonoBehaviour
    {
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid)
        {
            _grid = grid;
        }

        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask cellLayer;
        [SerializeField] private DragCountBubble countBubble;

        private bool _isDragging = false;
        private bool _isTapping;           // chạm xuống 1 khối đã đặt: thả tay trên chính khối đó thì gỡ khối
        private Vector2Int _tapCell;

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            // Nếu cellLayer đang là Nothing (0), tự động lấy Layer "Cell" hoặc toàn bộ layers
            if (cellLayer.value == 0)
            {
                int mask = LayerMask.GetMask("Cell");
                cellLayer = mask != 0 ? mask : ~0;
            }

            if (countBubble == null)
            {
                Debug.LogError("[DragAndDropController] Chưa gán DragCountBubble");
            }
        }

        private CellView GetCellAtPosition(Vector2 screenPos)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

            Collider2D hit2D = Physics2D.OverlapPoint(worldPos, cellLayer);
            if (hit2D != null && hit2D.TryGetComponent(out CellView cell2D))
            {
                return cell2D;
            }
            return null;
        }

        public void StartDrag(Vector2 pos)
        {
            if (_isDragging || _isTapping) return;
            GridManager gm = _grid;
            if (!gm || gm.IsInputLocked) return;

            // Bắt đầu phải chạm trúng 1 ô
            CellView cell = GetCellAtPosition(pos);
            if (!cell) return;

            // Đang chọn số cho booster Reveal: chạm ô là chọn, không kéo
            BoosterManager booster = BoosterManager.Instance;
            if (booster && booster.HandleCellTap(cell.Position)) return;

            // Ô đã có khối: chờ thả tay để gỡ khối (kéo sang khối khác thì thôi)
            if (gm.Board != null && gm.Board.GetBlockAt(cell.Position) != null)
            {
                _isTapping = true;
                _tapCell = cell.Position;
                return;
            }

            if (!gm.BeginSelection(cell.Position)) return;

            _isDragging = true;
            UpdateBubble(pos);
        }

        public void Drag(Vector2 pos)
        {
            if (_isTapping)
            {
                GridManager g = _grid;
                CellView over = GetCellAtPosition(pos);
                BlockModel tapped = g != null && g.Board != null ? g.Board.GetBlockAt(_tapCell) : null;
                if (over == null || tapped == null || g.Board.GetBlockAt(over.Position) != tapped) _isTapping = false;
                return;
            }
            if (!_isDragging) return;
            GridManager gm = _grid;
            if (!gm || gm.IsInputLocked)
            {
                CancelDrag();
                return;
            }

            // Không cần trúng ô: ngón tay trên ô Void, trên khối hay ra ngoài lưới vẫn được quy về ô gần nhất
            gm.UpdateSelection(ScreenToWorld(pos));
            UpdateBubble(pos);
        }

        public void EndDrag()
        {
            if (_isTapping)
            {
                _isTapping = false;
                if (_grid != null) _grid.TryRemoveBlockAt(_tapCell);
                return;
            }
            if (!_isDragging) return;
            GridManager gm = _grid;
            if (gm == null || gm.IsInputLocked)
            {
                CancelDrag();
                return;
            }

            gm.EndSelection();
            ResetDrag();
        }

        private void CancelDrag()
        {
            if (_grid != null) _grid.CancelSelection();
            ResetDrag();
        }

        private void ResetDrag()
        {
            _isDragging = false;
            _isTapping = false;
            if (countBubble != null) countBubble.Hide();
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            if (cam == null) cam = Camera.main;
            return cam != null ? cam.ScreenToWorldPoint(screenPos) : Vector3.zero;
        }

        private void UpdateBubble(Vector2 screenPos)
        {
            if (!countBubble || !_grid) return;
            GridManager gm = _grid;
            countBubble.Show(ScreenToWorld(screenPos), gm.SelectionCellCount, gm.SelectionHint);
        }
    }
}
