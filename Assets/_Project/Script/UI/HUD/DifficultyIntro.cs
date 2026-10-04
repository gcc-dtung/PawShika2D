using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng vào level Hard / Very Hard (như bản gốc "EXTREME LEVEL"): lưới hiện xong thì nền tối dần,
/// dải băng cảnh báo sọc và băng chữ (HARD LEVEL / EXTREME LEVEL) trượt chéo từ góc trên trái vào giữa, mèo đầu lâu bật ra
/// phía trên băng, giữ một lúc (đầu lâu nhịp nhẹ) rồi đầu lâu thu lại, 2 băng trượt ngược ra, nền sáng lại.
/// Đang chạy thì khóa thao tác trên lưới (Playing tĩnh, GridManager đọc). Chỉ chạy khi vào ván (không ở Main Menu).
/// Sprite theo độ khó lấy từ DifficultyTheme.
/// </summary>
public class DifficultyIntro : MonoBehaviour
{
    [SerializeField] private DifficultyTheme theme;
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image dim;
    [SerializeField] private RectTransform tape;
    [SerializeField] private Image banner;
    [SerializeField] private Image skull;

    [Header("Nhịp (giây)")]
    [SerializeField] private float dimAlpha = 0.65f;
    [SerializeField] private float slideIn = 0.42f;
    [SerializeField] private float hold = 1.3f;
    [SerializeField] private float slideOut = 0.36f;
    [Tooltip("Băng bắt đầu / kết thúc lệch khỏi chỗ đứng bao nhiêu (đơn vị canvas, hướng góc trên trái)")]
    [SerializeField] private Vector2 offscreen = new Vector2(-2200f, 900f);
    [Tooltip("Tỉ lệ hiện băng chữ so với kích thước ảnh (băng dài quá 2 mép màn, chữ giữ cỡ)")]
    [SerializeField] private float bannerScale = 0.8f;

    /// <summary>Đang chạy hiệu ứng: khóa thao tác trên lưới.</summary>
    public static bool Playing { get; private set; }

    private Vector2 _tapePos, _bannerPos, _skullPos;
    private Sequence _seq;
    private Coroutine _wait;

    private void Awake()
    {
        if (tape != null) _tapePos = tape.anchoredPosition;
        if (banner != null) _bannerPos = banner.rectTransform.anchoredPosition;
        if (skull != null) _skullPos = skull.rectTransform.anchoredPosition;
        Hide();
    }

    private void OnEnable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
        Stop();
    }

    private void OnLevelLoaded(int _)
    {
        Stop();
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return;
        LevelDefinition level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
        DifficultyStyle style = theme != null && level != null ? theme.Get(level.difficulty) : null;
        if (style == null || style.banner == null) return;

        Playing = true; // khóa luôn từ lúc lưới còn đang hiện
        _wait = StartCoroutine(PlayAfterGrid(style));
    }

    private IEnumerator PlayAfterGrid(DifficultyStyle style)
    {
        while (gridSpawner != null && gridSpawner.IsIntroPlaying) yield return null;
        _wait = null;
        Play(style);
    }

    /// <summary>Chạy hiệu ứng với kiểu <paramref name="style"/> (Debug / thử).</summary>
    public void Play(DifficultyStyle style)
    {
        _seq?.Kill();
        Playing = true;
        gameObject.SetActive(true);
        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
        banner.sprite = style.banner;
        banner.SetNativeSize();
        banner.rectTransform.sizeDelta *= bannerScale;
        if (skull != null)
        {
            skull.sprite = style.skull;
            skull.gameObject.SetActive(style.skull != null);
            skull.rectTransform.localScale = Vector3.zero;
            skull.rectTransform.anchoredPosition = _skullPos;
        }
        SetDim(0f);
        tape.anchoredPosition = _tapePos + offscreen;
        banner.rectTransform.anchoredPosition = _bannerPos + offscreen;
        AudioManager.Play(SoundId.HardLevel);

        Sequence s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        s.Insert(0f, DOVirtual.Float(0f, dimAlpha, 0.25f, SetDim));
        s.Insert(0f, tape.DOAnchorPos(_tapePos, slideIn).SetEase(Ease.OutCubic));
        s.Insert(0.06f, banner.rectTransform.DOAnchorPos(_bannerPos, slideIn).SetEase(Ease.OutBack, 1.2f));
        if (skull != null && style.skull != null)
        {
            RectTransform sk = skull.rectTransform;
            s.Insert(slideIn * 0.8f, sk.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 2.2f));
            // Đầu lâu nhịp nhẹ trong lúc giữ
            s.Insert(slideIn * 0.8f + 0.4f, sk.DOScale(1.08f, 0.3f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
        }

        float outAt = slideIn + hold;
        if (skull != null && style.skull != null) s.Insert(outAt, skull.rectTransform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        s.Insert(outAt + 0.1f, banner.rectTransform.DOAnchorPos(_bannerPos + offscreen, slideOut).SetEase(Ease.InCubic));
        s.Insert(outAt + 0.14f, tape.DOAnchorPos(_tapePos + offscreen, slideOut).SetEase(Ease.InCubic));
        s.Insert(outAt + 0.15f, DOVirtual.Float(dimAlpha, 0f, 0.3f, SetDim));
        s.OnComplete(Hide);
        s.OnKill(() => { if (_seq == s) _seq = null; });
        _seq = s;
    }

    private void Stop()
    {
        if (_wait != null)
        {
            StopCoroutine(_wait);
            _wait = null;
        }
        _seq?.Kill();
        Hide();
    }

    private void Hide()
    {
        Playing = false;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }

    private void SetDim(float a)
    {
        if (dim == null) return;
        Color c = dim.color;
        c.a = a;
        dim.color = c;
    }

}
