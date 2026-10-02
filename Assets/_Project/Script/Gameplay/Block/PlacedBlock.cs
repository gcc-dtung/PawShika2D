using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Khối "mèo" sau khi đặt đúng: thân màu (9-slice), tai, mặt hoạt hình và số của clue.
/// Mọi object con có sẵn trong PlacedBlock.prefab; khối được GridManager lấy ra / trả về pool.
/// </summary>
public class PlacedBlock : MonoBehaviour
{
    private const float PopDuration = 0.18f;
    private const float BodyFill = 0.96f; // thân khối chiếm 96% ô, phần còn lại là khe hở

    [Header("Object con trong prefab")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private TextMeshPro numberText;
    [SerializeField] private CatFaceAnimator face;
    [SerializeField] private SpriteRenderer ears;

    [Header("Tai mèo (tính theo tỉ lệ ô)")]
    [SerializeField] private float earWidth = 0.36f;   // bề rộng cặp tai
    [SerializeField] private float earOverlap = 0f;    // đỉnh tai đè lên khối bên trên thêm bao nhiêu

    private readonly List<Cell> _occupiedCells = new List<Cell>();
    private float _cellSize = GridSpawner.CellSpacing;
    private Coroutine _pop;

    public IReadOnlyList<Cell> OccupiedCells => _occupiedCells;
    public Cell StartCell { get; private set; }
    public Cell TargetCell { get; private set; }
    public RectInt Rect { get; private set; }
    public ColorType ColorType { get; private set; }

    public void Init(RectInt rect, List<Cell> cells, Cell clueCell, ColorType color, float size)
    {
        Rect = rect;
        ColorType = color;
        _cellSize = size;
        transform.localScale = Vector3.one;

        // Ô góc dưới-trái và trên-phải của vùng
        _occupiedCells.Clear();
        foreach (Cell cell in cells)
        {
            if (cell == null) continue;
            _occupiedCells.Add(cell);
            cell.SetPlacedBlock(this);
            Vector2Int p = cell.GetPositionInCell();
            if (p.x == rect.xMin && p.y == rect.yMin) StartCell = cell;
            if (p.x == rect.xMax - 1 && p.y == rect.yMax - 1) TargetCell = cell;
        }

        Vector3 startPos = StartCell.transform.position;
        Vector3 targetPos = TargetCell.transform.position;
        Vector3 centerPos = (startPos + targetPos) / 2f;
        centerPos.z = -0.05f;
        transform.position = centerPos;

        float width = Mathf.Abs(targetPos.x - startPos.x) + _cellSize * BodyFill;
        float height = Mathf.Abs(targetPos.y - startPos.y) + _cellSize * BodyFill;

        GameplayVisuals visuals = GameplayVisuals.Instance;
        SetupBody(visuals, color, width, height);
        SetupNumber(clueCell, color);
        Cell faceCell = PickFaceCell(rect, clueCell);
        SetupFace(visuals, faceCell, rect);
        SetupEars(visuals, color, faceCell, height);

        if (_pop != null) StopCoroutine(_pop);
        _pop = StartCoroutine(PopIn());
    }

    private void SetupBody(GameplayVisuals visuals, ColorType color, float width, float height)
    {
        Sprite sprite = visuals != null ? visuals.GetBlockSprite(color) : null;
        if (sprite != null)
        {
            body.sprite = sprite;
            body.color = Color.white;
        }
        else
        {
            // Chưa cấu hình sprite màu: giữ sprite trắng của prefab và tô màu
            body.color = color.ToColor();
        }
        body.size = new Vector2(width, height);
    }

    private void SetupNumber(Cell clueCell, ColorType color)
    {
        bool show = clueCell != null && clueCell.HasClue;
        numberText.gameObject.SetActive(show);
        if (!show) return;

        numberText.transform.position = clueCell.transform.position + new Vector3(0f, 0f, -0.06f);
        numberText.color = Color.Lerp(color.ToColor(), new Color(0.12f, 0.1f, 0.3f), 0.6f);
        numberText.SetText("{0}", clueCell.Clue.Value);
    }

    /// <summary>Mặt ở ô trên-phải; nếu trùng ô số thì chuyển xuống ô dưới-trái.</summary>
    private Cell PickFaceCell(RectInt rect, Cell clueCell)
    {
        if (rect.width * rect.height == 1) return clueCell;
        return TargetCell != clueCell ? TargetCell : StartCell;
    }

    private void SetupFace(GameplayVisuals visuals, Cell faceCell, RectInt rect)
    {
        CatFaceAnimation anim = visuals != null ? visuals.GetRandomFace() : null;
        bool show = anim != null && anim.frames.Count > 0 && faceCell != null;
        face.gameObject.SetActive(show);
        if (!show) return;

        bool single = rect.width * rect.height == 1;
        float faceWidth = _cellSize * (single ? 0.5f : 0.72f);
        Vector3 pos = faceCell.transform.position;
        if (single) pos += new Vector3(0f, -_cellSize * 0.28f, 0f); // khối 1 ô: mặt nhỏ dưới số

        face.transform.position = new Vector3(pos.x, pos.y, transform.position.z - 0.06f);
        face.Play(anim, visuals.faceFps, faceWidth);
    }

    private void SetupEars(GameplayVisuals visuals, ColorType color, Cell faceCell, float blockHeight)
    {
        Sprite sprite = visuals != null ? visuals.GetEarSprite(color) : null;
        bool show = sprite != null && faceCell != null;
        ears.gameObject.SetActive(show);
        if (!show) return;

        ears.sprite = sprite;
        float k = _cellSize * earWidth / sprite.bounds.size.x;
        ears.transform.localScale = new Vector3(k, k, 1f);

        // Đỉnh tai chạm đáy khối bên trên (qua khe hở), phần còn lại lún vào thân khối này
        float earHeight = sprite.bounds.size.y * k;
        float gap = _cellSize * (1f - BodyFill);
        float tipY = transform.position.y + blockHeight / 2f + gap + _cellSize * earOverlap;
        ears.transform.position = new Vector3(faceCell.transform.position.x, tipY - earHeight / 2f, transform.position.z - 0.01f);
    }

    private IEnumerator PopIn()
    {
        float t = 0f;
        while (t < PopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / PopDuration);
            // Phóng to nhẹ rồi về kích thước chuẩn
            transform.localScale = Vector3.one * (1f + Mathf.Sin(p * Mathf.PI) * 0.08f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _pop = null;
    }

    /// <summary>Giải phóng các ô đang chiếm. GridManager gọi trước khi trả khối về pool.</summary>
    public void ReleaseCells()
    {
        foreach (Cell cell in _occupiedCells)
        {
            if (cell != null) cell.ClearPlacedBlock();
        }
        _occupiedCells.Clear();
        StartCell = null;
        TargetCell = null;

        if (_pop != null)
        {
            StopCoroutine(_pop);
            _pop = null;
        }
        transform.localScale = Vector3.one;
    }
}
