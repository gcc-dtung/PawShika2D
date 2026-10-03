using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Nút PLAY trên Home: "Level n" theo level đang chơi, thở nhẹ để mời bấm; bấm -> ScreenFlow.Play.</summary>
[RequireComponent(typeof(Button))]
public class PlayButtonView : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private float breathScale = 1.04f;
    [SerializeField] private float breathDuration = 0.9f;

    private Tween _breath;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.AddListener(SetLevel);
        Refresh();

        transform.localScale = Vector3.one;
        _breath = transform.DOScale(breathScale, breathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(SetLevel);
        _breath?.Kill();
        _breath = null;
        transform.localScale = Vector3.one;
    }

    private void OnClick()
    {
        if (ScreenFlow.Instance != null) ScreenFlow.Instance.Play();
    }

    private void Refresh()
    {
        if (LevelManager.Instance != null) SetLevel(LevelManager.Instance.CurrentLevelNumber);
    }

    private void SetLevel(int number)
    {
        if (levelText != null) levelText.SetText("Level {0}", number);
    }
}
