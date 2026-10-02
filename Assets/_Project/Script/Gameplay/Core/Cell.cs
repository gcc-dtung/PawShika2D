using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hiển thị ô số (object con có sẵn trong Cell.prefab)")]
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private TextMeshPro lockText;
    [SerializeField] private SpriteRenderer orientationBadge; // ô nền sau con số: cao = dọc, vuông = vuông, dẹt = ngang

    // Kích thước ô nền (đơn vị world). Sprite 9-slice có viền lớn nên vẽ to rồi thu nhỏ transform.
    private static readonly Vector2 BadgeTall = new Vector2(0.5f, 0.74f);
    private static readonly Vector2 BadgeSquare = new Vector2(0.64f, 0.64f);
    private static readonly Vector2 BadgeWide = new Vector2(0.74f, 0.5f);
    private const float BadgeScale = 0.25f;

    private Vector2Int _cellPos;

    public PlacedBlock CurrentPlacedBlock { get; private set; }
    public bool IsOccupied => CurrentPlacedBlock != null;
    public ClueCell Clue { get; private set; }
    public bool HasClue => Clue != null;
    public TextMeshPro ValueText => valueText;

    public void SetPositionInCell(int x, int y)
    {
        _cellPos = new Vector2Int(x, y);
    }

    public Vector2Int GetPositionInCell() => _cellPos;

    public bool IsCellInRange(int xMin, int xMax, int yMin, int yMax)
    {
        return (_cellPos.x >= xMin && _cellPos.x <= xMax && _cellPos.y >= yMin && _cellPos.y <= yMax);
    }

    public void SetPlacedBlock(PlacedBlock block)
    {
        CurrentPlacedBlock = block;
    }

    public void ClearPlacedBlock()
    {
        CurrentPlacedBlock = null;
    }

    public void SetClue(ClueData clueData)
    {
        Clue = new ClueCell(clueData);
        RefreshClueView();
    }

    public void ClearClue()
    {
        Clue = null;
        RefreshClueView();
    }

    /// <summary>Giảm 1 bước khóa. Trả về true nếu ô vừa được mở khóa.</summary>
    public bool StepLock()
    {
        if (Clue == null || !Clue.IsLocked) return false;
        bool unlocked = Clue.StepLock();
        RefreshClueView();
        return unlocked;
    }

    public void RevealClue()
    {
        if (Clue != null && Clue.Reveal()) RefreshClueView();
    }

    private void RefreshClueView()
    {
        bool hasClue = Clue != null;
        bool locked = hasClue && Clue.IsLocked;

        if (valueText != null)
        {
            bool showValue = hasClue && !locked;
            valueText.gameObject.SetActive(showValue);
            if (showValue)
            {
                if (Clue.IsHidden) valueText.SetText("?");
                else valueText.SetText("{0}", Clue.Value);
            }
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(locked);
            if (locked && lockText != null) lockText.SetText("{0}", Clue.LockSteps);
        }

        if (orientationBadge != null)
        {
            // Ô còn khóa: chưa biết gì về clue, kể cả hướng
            bool showBadge = hasClue && !locked && Clue.Constraint != ClueConstraint.None;
            orientationBadge.gameObject.SetActive(showBadge);
            if (showBadge)
            {
                Vector2 size = Clue.Constraint switch
                {
                    ClueConstraint.Vertical => BadgeTall,
                    ClueConstraint.Horizontal => BadgeWide,
                    _ => BadgeSquare
                };
                orientationBadge.transform.localScale = new Vector3(BadgeScale, BadgeScale, 1f);
                orientationBadge.size = size / BadgeScale;
            }
        }
    }
}
