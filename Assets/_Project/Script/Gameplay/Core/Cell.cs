using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hiển thị ô số (object con có sẵn trong Cell.prefab)")]
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private TextMeshPro lockText;
    [SerializeField] private SpriteRenderer arrow;

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

        if (arrow != null)
        {
            bool showArrow = hasClue && !locked && Clue.Constraint != ClueConstraint.None;
            arrow.gameObject.SetActive(showArrow);
            // Sprite mũi tên gốc hướng sang phải: ngang giữ nguyên, dọc xoay 90 độ
            if (showArrow)
            {
                arrow.transform.localRotation = Quaternion.Euler(0f, 0f, Clue.Constraint == ClueConstraint.Vertical ? 90f : 0f);
            }
        }
    }
}
