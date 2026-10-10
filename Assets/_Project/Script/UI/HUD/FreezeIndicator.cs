using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng Đóng băng trên đồng hồ (Image ice_effect_2 phủ lên đồng hồ, không chắn bấm):
/// - Bật: viền băng toàn màn (frostOverlay, ice_effect) lóe lên rồi tan; lớp băng đóng lại (phóng 0.6 -> 1, OutBack),
///   rồi thở nhẹ; bông tuyết lấp lánh quanh lớp băng (xoay, hiện / tắt lần lượt), thỉnh thoảng 1 vệt sáng lướt qua.
/// - Hết thời gian đóng băng: lớp băng rung, phồng to rồi vỡ (mờ đi), mảnh băng (bông tuyết) văng ra 4 phía, xoay và tan.
/// - Tắt (đổi level...): mờ đi.
/// Bông tuyết / mảnh vỡ là pool Image con tạo 1 lần trong Awake, không tạo object lúc chơi.
/// </summary>
[RequireComponent(typeof(Image))]
public class FreezeIndicator : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float alpha = 0.85f;
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private float breakDuration = 0.35f;

    [Header("Viền băng toàn màn khi bật (tùy chọn)")]
    [SerializeField] private Image frostOverlay;
    [SerializeField, Range(0f, 1f)] private float frostAlpha = 0.55f;
    [SerializeField] private float frostDuration = 0.7f;

    [Header("Bông tuyết lấp lánh / mảnh vỡ")]
    [SerializeField] private Sprite sparkSprite;
    [SerializeField] private int sparkCount = 6;
    [SerializeField] private float sparkSize = 34f;
    [SerializeField] private float twinkleDuration = 0.9f;
    [SerializeField] private int shardCount = 10;
    [SerializeField] private float shardDistance = 170f;
    [SerializeField] private float shardDuration = 0.55f;

    [Header("Vệt sáng lướt qua")]
    [SerializeField] private float glintInterval = 2.2f;
    [SerializeField] private float glintDuration = 0.35f;

    private Image _image;
    private RectTransform _rect;
    private Color _baseColor;
    private Vector2 _basePos;
    private readonly List<Image> _sparks = new List<Image>();
    private readonly List<Tween> _tweens = new List<Tween>(); // tween phụ (bông tuyết, viền màn, vệt sáng)
    private Tween _frost;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = (RectTransform)transform;
        _image.raycastTarget = false;
        _baseColor = _image.color;
        _basePos = _rect.anchoredPosition;
        SetAlpha(_image, 0f);
        if (frostOverlay != null)
        {
            frostOverlay.raycastTarget = false;
            SetAlpha(frostOverlay, 0f);
        }
        BuildSparks();
    }

    private void OnEnable()
    {
        LevelTimer timer = LevelTimer.Instance;
        if (timer == null) return;
        timer.onFreezeChanged.AddListener(OnFreezeChanged);
        timer.onFreezeEnded.AddListener(OnBroken);
        if (timer.IsFrozen) Appear(false);
    }

    private void OnDisable()
    {
        LevelTimer timer = LevelTimer.Instance;
        if (timer != null)
        {
            timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            timer.onFreezeEnded.RemoveListener(OnBroken);
        }
        KillAll();
        SetAlpha(_image, 0f);
        if (frostOverlay != null) SetAlpha(frostOverlay, 0f);
    }

    private void OnFreezeChanged(bool on)
    {
        // Tắt: mờ đi. Hết giờ đóng băng: LevelTimer báo tắt trước rồi onFreezeEnded -> hiệu ứng vỡ đè lên
        if (on) Appear(true);
        else Hide();
    }

    // ---------- Bật ----------

    private void Appear(bool withFrost)
    {
        KillAll();
        transform.localScale = Vector3.one * 0.6f;
        SetAlpha(_image, 0f);
        if (withFrost) FlashFrost();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(_image.DOFade(alpha, appearDuration));
        seq.Join(transform.DOScale(1f, appearDuration).SetEase(Ease.OutBack));
        // Thở nhẹ + lấp lánh khi đang bật (tween lặp vô hạn không lồng được trong Sequence nên chạy riêng)
        seq.OnComplete(() =>
        {
            Track(transform.DOScale(1.05f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject));
            StartTwinkle();
            StartGlint();
        });
        Track(seq);
    }

    private void FlashFrost()
    {
        if (frostOverlay == null) return;
        _frost?.Kill();
        frostOverlay.gameObject.SetActive(true);
        SetAlpha(frostOverlay, 0f);
        frostOverlay.rectTransform.localScale = Vector3.one * 1.15f;
        Sequence s = DOTween.Sequence().SetLink(frostOverlay.gameObject);
        s.Append(frostOverlay.DOFade(frostAlpha, frostDuration * 0.25f));
        s.Join(frostOverlay.rectTransform.DOScale(1f, frostDuration * 0.25f).SetEase(Ease.OutQuad));
        s.Append(frostOverlay.DOFade(0f, frostDuration * 0.75f).SetEase(Ease.InQuad));
        s.OnKill(() => { if (frostOverlay != null) SetAlpha(frostOverlay, 0f); });
        _frost = s;
    }

    /// <summary>Mỗi bông tuyết nằm ngẫu nhiên trên lớp băng, hiện ra - xoay - tắt, lặp với độ trễ khác nhau.</summary>
    private void StartTwinkle()
    {
        Vector2 half = _rect.rect.size * 0.5f;
        for (int i = 0; i < _sparks.Count; i++)
        {
            Image spark = _sparks[i];
            RectTransform rt = spark.rectTransform;
            spark.gameObject.SetActive(true);
            SetAlpha(spark, 0f);
            rt.localScale = Vector3.zero;

            Sequence s = DOTween.Sequence().SetLink(spark.gameObject).SetDelay(i * twinkleDuration / _sparks.Count);
            s.AppendCallback(() =>
            {
                rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x) * 0.9f, Random.Range(-half.y, half.y) * 0.8f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            });
            s.Append(rt.DOScale(Random.Range(0.6f, 1f), twinkleDuration * 0.4f).SetEase(Ease.OutBack));
            s.Join(spark.DOFade(1f, twinkleDuration * 0.4f));
            s.Join(rt.DOLocalRotate(new Vector3(0f, 0f, 90f), twinkleDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
            s.Insert(twinkleDuration * 0.6f, spark.DOFade(0f, twinkleDuration * 0.4f));
            s.Insert(twinkleDuration * 0.6f, rt.DOScale(0f, twinkleDuration * 0.4f).SetEase(Ease.InBack));
            s.AppendInterval(Random.Range(0.2f, 0.8f));
            s.SetLoops(-1);
            Track(s);
        }
    }

    /// <summary>Lớp băng sáng lên rồi về màu cũ theo nhịp (như ánh sáng lướt trên mặt băng).</summary>
    private void StartGlint()
    {
        Color bright = Color.Lerp(_baseColor, Color.white, 0.6f);
        bright.a = alpha;
        Color normal = _baseColor;
        normal.a = alpha;
        Sequence s = DOTween.Sequence().SetLink(gameObject);
        s.AppendInterval(glintInterval);
        s.Append(_image.DOColor(bright, glintDuration * 0.4f));
        s.Append(_image.DOColor(normal, glintDuration * 0.6f));
        s.SetLoops(-1);
        Track(s);
    }

    // ---------- Vỡ ----------

    private void OnBroken()
    {
        KillAll();
        SetAlpha(_image, alpha);
        Burst();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(_rect.DOShakeAnchorPos(breakDuration * 0.4f, new Vector2(14f, 4f), 30, 90f, false, true));
        seq.Append(transform.DOScale(1.4f, breakDuration).SetEase(Ease.OutQuad));
        seq.Join(_image.DOFade(0f, breakDuration));
        Track(seq);
    }

    /// <summary>Mảnh băng văng ra từ tâm lớp băng: bay xa dần, xoay, rơi nhẹ và tan.</summary>
    private void Burst()
    {
        int count = Mathf.Min(shardCount, _sparks.Count);
        for (int i = 0; i < count; i++)
        {
            Image shard = _sparks[i];
            RectTransform rt = shard.rectTransform;
            shard.gameObject.SetActive(true);
            SetAlpha(shard, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one * Random.Range(0.5f, 1.1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float angle = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f);
            Vector2 end = dir * shardDistance * Random.Range(0.7f, 1.1f) + Vector2.down * 40f;

            Sequence s = DOTween.Sequence().SetLink(shard.gameObject);
            s.Append(rt.DOAnchorPos(end, shardDuration).SetEase(Ease.OutCubic));
            s.Join(rt.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-360f, 360f)), shardDuration, RotateMode.LocalAxisAdd));
            s.Join(rt.DOScale(0.2f, shardDuration).SetEase(Ease.InQuad));
            s.Insert(shardDuration * 0.4f, shard.DOFade(0f, shardDuration * 0.6f));
            s.OnComplete(() => shard.gameObject.SetActive(false));
            Track(s);
        }
    }

    // ---------- Tắt ----------

    private void Hide()
    {
        KillAll();
        Track(_image.DOFade(0f, 0.15f).SetLink(gameObject));
    }

    // ---------- Chung ----------

    /// <summary>Pool bông tuyết (đủ cho cả lấp lánh và mảnh vỡ), con của lớp băng, tắt sẵn.</summary>
    private void BuildSparks()
    {
        if (sparkSprite == null) return;
        int n = Mathf.Max(sparkCount, shardCount);
        for (int i = 0; i < n; i++)
        {
            GameObject go = new GameObject("Spark_" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = Vector2.one * sparkSize;
            go.SetActive(false);
            _sparks.Add(img);
        }
    }

    private void Track(Tween tween)
    {
        if (tween == null) return;
        tween.OnKill(() => _tweens.Remove(tween));
        _tweens.Add(tween);
    }

    private void KillAll()
    {
        for (int i = _tweens.Count - 1; i >= 0; i--) _tweens[i]?.Kill();
        _tweens.Clear();
        transform.localScale = Vector3.one;
        _rect.anchoredPosition = _basePos; // rung bị hủy giữa chừng: về đúng chỗ
        Color c = _baseColor;
        c.a = _image.color.a;
        _image.color = c;
        foreach (Image s in _sparks)
        {
            if (s != null) s.gameObject.SetActive(false);
        }
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }
}
