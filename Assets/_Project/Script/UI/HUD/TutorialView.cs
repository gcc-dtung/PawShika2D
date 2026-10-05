using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện tutorial trên Canvas_HUD (như bản gốc): tiêu đề "Tutorial", nút Skip, vạch tiến độ (bước hiện tại sáng),
/// khung lời nhắc. Trong lúc tutorial ẩn các panel HUD thường (level, mạng, booster) rồi bật lại khi xong.
/// </summary>
public class TutorialView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text message;
    [SerializeField] private RectTransform bubble;
    [SerializeField] private Image[] dots;
    [SerializeField] private Button skipButton;
    [SerializeField] private Color dotActive = new Color(0.62f, 0.62f, 0.85f);
    [SerializeField] private Color dotInactive = new Color(0.16f, 0.13f, 0.38f);
    [Tooltip("Panel HUD thường, ẩn khi đang tutorial")]
    [SerializeField] private GameObject[] hideWhileActive;

    private bool[] _wasActive;

    public Button SkipButton => skipButton;

    public void Show()
    {
        gameObject.SetActive(true);
        if (hideWhileActive != null)
        {
            _wasActive = new bool[hideWhileActive.Length];
            for (int i = 0; i < hideWhileActive.Length; i++)
            {
                if (hideWhileActive[i] == null) continue;
                _wasActive[i] = hideWhileActive[i].activeSelf;
                hideWhileActive[i].SetActive(false);
            }
        }
        if (group != null)
        {
            group.alpha = 0f;
            group.DOFade(1f, 0.25f).SetLink(gameObject);
        }
    }

    public void Hide()
    {
        if (hideWhileActive != null && _wasActive != null)
        {
            for (int i = 0; i < hideWhileActive.Length; i++)
                if (hideWhileActive[i] != null) hideWhileActive[i].SetActive(_wasActive[i]);
        }
        _wasActive = null;
        gameObject.SetActive(false);
    }

    /// <summary>Đổi bước: lời nhắc mới (khung nảy nhẹ), vạch của bước hiện tại sáng lên.</summary>
    public void SetStep(int index, int count, string text)
    {
        if (message != null) message.text = text;
        if (dots != null)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                dots[i].gameObject.SetActive(i < count);
                dots[i].color = i == index ? dotActive : dotInactive;
            }
        }
        if (bubble != null)
        {
            bubble.DOKill();
            bubble.localScale = Vector3.one * 0.9f;
            bubble.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(bubble.gameObject);
        }
    }
}
