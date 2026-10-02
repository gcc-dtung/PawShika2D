using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị 1 ô và là đích raycast khi kéo. Dữ liệu nằm ở CellModel (Model).
/// </summary>
public class CellView : MonoBehaviour
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

    public CellModel Model { get; private set; }
    public Vector2Int Position => Model.Position;

    public void Bind(CellModel model)
    {
        Unbind();
        Model = model;
        Model.Changed += Refresh;
        Refresh();
    }

    public void Unbind()
    {
        if (Model != null) Model.Changed -= Refresh;
        Model = null;
    }

    private void Refresh()
    {
        ClueCell clue = Model != null ? Model.Clue : null;
        bool hasClue = clue != null;
        bool locked = hasClue && clue.IsLocked;

        if (valueText != null)
        {
            bool showValue = hasClue && !locked;
            valueText.gameObject.SetActive(showValue);
            if (showValue)
            {
                if (clue.IsHidden) valueText.SetText("?");
                else valueText.SetText("{0}", clue.Value);
            }
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(locked);
            if (locked && lockText != null) lockText.SetText("{0}", clue.LockSteps);
        }

        if (orientationBadge != null)
        {
            // Ô còn khóa: chưa biết gì về clue, kể cả hướng
            bool showBadge = hasClue && !locked && clue.Constraint != ClueConstraint.None;
            orientationBadge.gameObject.SetActive(showBadge);
            if (showBadge)
            {
                Vector2 size = clue.Constraint switch
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
