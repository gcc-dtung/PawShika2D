using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Màn thắng như bản gốc (full màn hình trên nền tối, không có bảng):
/// ô coin trên cùng, chữ "WELL DONE!" to ngay trên huy hiệu mèo (tia sáng xoay), confetti,
/// nút Continue (+coin theo độ khó, EconomyConfig.WinCoinReward) và x2 Coin (quảng cáo, gấp đôi). Game không có sao nên không hiện sao.
/// Mở: chữ đi vào từng ký tự (TMPLetterPopIn) cùng lúc huy hiệu bật + confetti bắn 2 bên rồi rơi liên tục -> chữ cuối đáp xuống -> 2 nút bật.
/// Nhận coin: huy hiệu thu lại, coin bung ra rồi bay vào ô coin (số đếm lên), xong thì sang level kế.
/// panel của PopupView để trống: không phóng cả màn, các phần tự diễn theo thứ tự.
/// </summary>
public class WinPopup : PopupView
{
    [Header("Thành phần")]
    [SerializeField] private TMPLetterPopIn banner;
    [SerializeField] private RectTransform medallion;
    [SerializeField] private RectTransform rays;
    [SerializeField] private RectTransform glow;
    [SerializeField] private RectTransform coinPill;
    [SerializeField] private RectTransform coinIcon;      // đích coin bay tới
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button doubleButton;
    [SerializeField] private TMP_Text nextAmountText;
    [SerializeField] private TMP_Text doubleAmountText;
    [SerializeField] private UIConfetti confetti;
    [SerializeField] private RectTransform flyLayer;
    [SerializeField] private Sprite coinSprite;

    [Header("Thưởng")]
    [SerializeField] private int maxFlyingCoins = 12;

    [Header("Nhịp")]
    [SerializeField] private float raysSpeed = 30f; // độ / giây
    [SerializeField] private float bannerDelay = 0.05f;

    private bool _claimed;
    private Sequence _intro;
    private Sequence _claim;
    private Tween _raysSpin;
    private int _shownCoins;
    private readonly List<Image> _coinPool = new List<Image>();

    protected override void Awake()
    {
        base.Awake();
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (doubleButton != null) doubleButton.onClick.AddListener(OnDouble);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _intro?.Kill();
        _claim?.Kill();
        _raysSpin?.Kill();
        if (confetti != null) confetti.StopRain();
        foreach (Image c in _coinPool) if (c != null) c.gameObject.SetActive(false);
    }

    protected override void OnOpen()
    {
        _claimed = false;
        _shownCoins = CoinWallet.Balance;
        SetCoinText(_shownCoins);
        if (nextAmountText != null) nextAmountText.SetText("{0}", CoinReward);
        if (doubleAmountText != null) doubleAmountText.SetText("{0}", CoinReward * 2);
        PlayIntro();
    }

    // ---------- Mở màn ----------

    private void PlayIntro()
    {
        _intro?.Kill();
        _claim?.Kill();
        if (banner != null) banner.Hide();
        SetScale(medallion, 0f);
        SetScale(glow, 0f);
        SetScale(rays, 0f);
        SetScale(nextButton != null ? (RectTransform)nextButton.transform : null, 0f);
        SetScale(doubleButton != null ? (RectTransform)doubleButton.transform : null, 0f);
        SetButtons(false);

        _raysSpin?.Kill();
        if (rays != null)
        {
            rays.localRotation = Quaternion.identity;
            _raysSpin = rays.DOLocalRotate(new Vector3(0f, 0f, -360f), 360f / raysSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(gameObject);
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.InsertCallback(bannerDelay, () => { if (banner != null) banner.Play(); });
        seq.Insert(0.15f, Pop(medallion, 0.45f, 1.6f));
        seq.Insert(0.15f, Pop(glow, 0.6f, 1f));
        seq.Insert(0.2f, Pop(rays, 0.5f, 1.2f));
        seq.InsertCallback(0.2f, () =>
        {
            if (confetti == null) return;
            confetti.Burst();
            confetti.StartRain();
        });

        // 2 nút bật sau khi chữ cuối đáp xuống
        float t = bannerDelay + (banner != null ? banner.Duration : 0.4f) + 0.05f;
        if (nextButton != null) seq.Insert(t, Pop((RectTransform)nextButton.transform, 0.3f, 2f));
        if (doubleButton != null) seq.Insert(t + 0.15f, Pop((RectTransform)doubleButton.transform, 0.3f, 2f));
        seq.InsertCallback(t + 0.2f, () => SetButtons(true));
        _intro = seq;
    }

    // ---------- Nhận thưởng ----------

    // Theo độ khó level vừa thắng: Normal 10 / Hard 20 / Very Hard 30 (EconomyConfig)
    private static int CoinReward
    {
        get
        {
            LevelDefinition level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
            return GameConfig.Economy.WinCoinReward(level != null ? level.difficulty : LevelDifficulty.Normal);
        }
    }

    private void OnNext() => Claim(CoinReward);

    private void OnDouble()
    {
        SetButtons(false);
        RewardedAds.Show("win_x2", ok =>
        {
            if (ok) Claim(CoinReward * 2);
            else SetButtons(true);
        });
    }

    private void Claim(int coins)
    {
        if (_claimed) return;
        _claimed = true;
        SetButtons(false);
        _intro?.Complete();
        if (banner != null) banner.Finish();

        int from = _shownCoins;
        CoinWallet.Add(coins);
        int to = CoinWallet.Balance;

        int count = Mathf.Clamp(coins, 1, maxFlyingCoins);
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (medallion != null) seq.Insert(0f, medallion.DOScale(0f, 0.3f).SetEase(Ease.InBack));
        if (glow != null) seq.Insert(0f, glow.DOScale(0f, 0.3f).SetEase(Ease.InQuad));
        if (rays != null) seq.Insert(0f, rays.DOScale(0f, 0.3f).SetEase(Ease.InQuad));

        Vector3 start = medallion != null ? medallion.position : transform.position;
        Vector3 target = coinIcon != null ? coinIcon.position : start;
        float last = 0f;
        for (int i = 0; i < count; i++)
        {
            Image coin = GetCoin();
            RectTransform rt = coin.rectTransform;
            rt.position = start;
            rt.localScale = Vector3.zero;
            Vector2 burst = Random.insideUnitCircle * 170f;
            int value = from + Mathf.RoundToInt((to - from) * (i + 1f) / count);
            float t0 = 0.12f + i * 0.04f;

            seq.Insert(t0, rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
            seq.Insert(t0, rt.DOMove(start + (Vector3)(burst * transform.lossyScale.x), 0.28f).SetEase(Ease.OutCubic)); // đơn vị canvas -> world
            float t1 = t0 + 0.3f + i * 0.02f;
            seq.Insert(t1, rt.DOMove(target, 0.42f).SetEase(Ease.InCubic));
            seq.Insert(t1 + 0.25f, rt.DOScale(0.6f, 0.17f));
            seq.InsertCallback(t1 + 0.42f, () =>
            {
                coin.gameObject.SetActive(false);
                SetCoinText(value);
                AudioManager.Play(SoundId.Coin);
                if (coinPill != null)
                {
                    coinPill.DOKill(true);
                    coinPill.localScale = Vector3.one;
                    coinPill.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f).SetUpdate(true);
                }
            });
            last = Mathf.Max(last, t1 + 0.42f);
        }
        seq.InsertCallback(last, () => SetCoinText(to));
        seq.AppendInterval(0.35f);
        seq.OnComplete(() =>
        {
            Close();
            if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
        });
        _claim = seq;
    }

    private Image GetCoin()
    {
        foreach (Image c in _coinPool)
        {
            if (!c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(true);
                return c;
            }
        }
        GameObject go = new GameObject("FlyCoin", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(flyLayer != null ? flyLayer : transform, false);
        rt.sizeDelta = new Vector2(86f, 86f);
        Image img = go.GetComponent<Image>();
        img.sprite = coinSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        _coinPool.Add(img);
        return img;
    }

    // ---------- Tiện ích ----------

    private void SetCoinText(int value)
    {
        _shownCoins = value;
        if (coinText != null) coinText.SetText("{0}", value);
    }

    private static Tween Pop(RectTransform rt, float duration, float overshoot)
    {
        if (rt == null) return DOVirtual.DelayedCall(0f, () => { });
        return rt.DOScale(1f, duration).SetEase(Ease.OutBack, overshoot);
    }

    private static void SetScale(RectTransform rt, float s)
    {
        if (rt != null) rt.localScale = Vector3.one * s;
    }

    private void SetButtons(bool interactable)
    {
        if (nextButton != null) nextButton.interactable = interactable;
        if (doubleButton != null) doubleButton.interactable = interactable;
    }
}
