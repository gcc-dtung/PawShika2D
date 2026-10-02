using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Khối "mèo" sau khi đặt đúng: thân màu (9-slice), tai, mặt hoạt hình và số của clue.
/// </summary>
public class PlacedBlock : MonoBehaviour
{
    private const int BodyOrder = 20;
    private const float PopDuration = 0.18f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float cellSize = 1.2f;

    public List<Cell> OccupiedCells { get; private set; } = new List<Cell>();
    public Cell StartCell { get; private set; }
    public Cell TargetCell { get; private set; }
    public RectInt Rect { get; private set; }
    public ColorType ColorType { get; private set; }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
    }

    public void Init(RectInt rect, List<Cell> cells, Cell clueCell, ColorType color, float size)
    {
        Rect = rect;
        ColorType = color;
        OccupiedCells = new List<Cell>(cells);
        cellSize = size;

        // Ô góc dưới-trái và trên-phải của vùng
        foreach (Cell cell in OccupiedCells)
        {
            if (cell == null) continue;
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

        float width = Mathf.Abs(targetPos.x - startPos.x) + cellSize * 0.96f;
        float height = Mathf.Abs(targetPos.y - startPos.y) + cellSize * 0.96f;

        GameplayVisuals visuals = GameplayVisuals.Instance;
        SetupBody(visuals, color, width, height);
        SetupNumber(clueCell, color);
        Cell faceCell = PickFaceCell(rect, clueCell);
        SetupFace(visuals, faceCell, rect, clueCell);
        SetupEars(visuals, color, faceCell, height);

        StartCoroutine(PopIn());
    }

    private void SetupBody(GameplayVisuals visuals, ColorType color, float width, float height)
    {
        Sprite body = visuals != null ? visuals.GetBlockSprite(color) : null;
        if (body != null)
        {
            spriteRenderer.sprite = body;
            spriteRenderer.color = Color.white;
        }
        else
        {
            // Chưa cấu hình sprite: dùng ô vuông trắng tô màu
            Texture2D tex = Texture2D.whiteTexture;
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            spriteRenderer.color = color.ToColor();
        }

        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(width, height);
        spriteRenderer.sortingOrder = BodyOrder;
    }

    private void SetupNumber(Cell clueCell, ColorType color)
    {
        if (clueCell == null || clueCell.ClueCell == null) return;

        GameObject go = new GameObject("Number");
        go.transform.SetParent(transform, false);
        go.transform.position = clueCell.transform.position + new Vector3(0f, 0f, -0.06f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        TextMeshPro source = clueCell.GetComponentInChildren<TextMeshPro>(true);
        if (source != null)
        {
            tmp.font = source.font;
            tmp.fontStyle = source.fontStyle;
            tmp.fontSize = source.fontSize;
            tmp.enableAutoSizing = source.enableAutoSizing;
            tmp.fontSizeMin = source.fontSizeMin;
            tmp.fontSizeMax = source.fontSizeMax;
        }
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
        tmp.color = Color.Lerp(color.ToColor(), new Color(0.12f, 0.1f, 0.3f), 0.6f);
        tmp.sortingOrder = BodyOrder + 2;
        tmp.SetText(clueCell.ClueCell.Value.ToString());
    }

    /// <summary>Mặt ở ô trên-phải; nếu trùng ô số thì chuyển xuống ô dưới-trái.</summary>
    private Cell PickFaceCell(RectInt rect, Cell clueCell)
    {
        if (rect.width * rect.height == 1) return clueCell;
        return TargetCell != clueCell ? TargetCell : StartCell;
    }

    private void SetupFace(GameplayVisuals visuals, Cell faceCell, RectInt rect, Cell clueCell)
    {
        CatFaceAnimation face = visuals != null ? visuals.GetRandomFace() : null;
        if (face == null || face.frames.Count == 0 || faceCell == null) return;

        GameObject go = new GameObject("Face");
        go.transform.SetParent(transform, false);
        Vector3 pos = faceCell.transform.position;

        bool single = rect.width * rect.height == 1;
        float faceWidth = cellSize * (single ? 0.5f : 0.72f);
        if (single) pos += new Vector3(0f, -cellSize * 0.28f, 0f); // khối 1 ô: mặt nhỏ dưới số

        go.transform.position = new Vector3(pos.x, pos.y, transform.position.z - 0.06f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = BodyOrder + 1;

        CatFaceAnimator animator = go.AddComponent<CatFaceAnimator>();
        animator.Play(face, visuals.faceFps, faceWidth);
    }

    private void SetupEars(GameplayVisuals visuals, ColorType color, Cell faceCell, float blockHeight)
    {
        Sprite ear = visuals != null ? visuals.GetEarSprite(color) : null;
        if (ear == null || faceCell == null) return;

        GameObject go = new GameObject("Ears");
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ear;
        sr.sortingOrder = BodyOrder - 1;

        float k = cellSize * 0.62f / ear.bounds.size.x;
        go.transform.localScale = new Vector3(k, k, 1f);

        float earHeight = ear.bounds.size.y * k;
        float topY = transform.position.y + blockHeight / 2f;
        go.transform.position = new Vector3(faceCell.transform.position.x, topY + earHeight * 0.35f, transform.position.z + 0.01f);
    }

    private IEnumerator PopIn()
    {
        Vector3 baseScale = transform.localScale;
        float t = 0f;
        while (t < PopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / PopDuration);
            // Phóng to nhẹ rồi về kích thước chuẩn
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.08f;
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public void Remove()
    {
        // Giải phóng các Cell khỏi trạng thái bị chiếm
        foreach (Cell cell in OccupiedCells)
        {
            if (cell != null)
            {
                cell.ClearPlacedBlock();
                // cell.OnDeselect();
            }
        }
        OccupiedCells.Clear();

        Destroy(gameObject);
    }
}
