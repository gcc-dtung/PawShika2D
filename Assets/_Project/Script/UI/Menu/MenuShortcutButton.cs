using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Nút phụ trên Home (Starter, No Ads): nhún nhẹ mời bấm; bấm -> chuyển sang tab chỉ định (mặc định Shop).</summary>
[RequireComponent(typeof(Button))]
public class MenuShortcutButton : MonoBehaviour
{
    [SerializeField] private MainMenuNavigator navigator;
    [SerializeField] private int tab = 0;
    [SerializeField] private RectTransform bob;
    [SerializeField] private float bobHeight = 10f;
    [SerializeField] private float bobDuration = 1.2f;

    private Tween _bob;
    private Vector2 _bobStart;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        if (bob != null) _bobStart = bob.anchoredPosition;
    }

    private void OnEnable()
    {
        if (bob == null) return;
        bob.anchoredPosition = _bobStart;
        _bob = bob.DOAnchorPosY(_bobStart.y + bobHeight, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        _bob?.Kill();
        _bob = null;
        if (bob != null) bob.anchoredPosition = _bobStart;
    }

    private void OnClick()
    {
        if (navigator != null) navigator.Select(tab);
    }
}
