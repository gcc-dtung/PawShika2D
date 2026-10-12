using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Game;
using PawBlock.Shared;
using PawBlock.Features.Economy;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Màn thắng như bản gốc (WinUI, full màn hình trên nền tối, không có bảng):
    /// ô coin trên cùng, chữ tiêu đề theo số sao (Perfect 3 / Great 2 / Nice 1 / Well done 0 sao, StarRating.Title),
    /// 3 sao (sao xám làm bóng, sao vàng đập xuống), huy hiệu mèo (tia sáng xoay), confetti,
    /// nút Continue (+coin theo độ khó + EconomyConfig.coinsPerStar mỗi sao) và x2 Coin (quảng cáo, gấp đôi).
    /// Số coin trên 2 nút bắt đầu ở mức theo độ khó, mỗi sao đập xuống thì cộng thêm và số nảy lên.
    /// Mở: tiêu đề bật cùng huy hiệu + confetti bắn 2 bên rồi rơi liên tục, 2 nút bật -> từng sao đập xuống (phóng to starSlamScale
    /// lần rồi thu về trong starSlamDuration giây, cách nhau starSlamInterval, số của WinUI gốc), mỗi sao cộng coin trên nút -> nhận bấm.
    /// Nhận coin: huy hiệu thu lại, coin (lật quanh trục Y) bung ra rồi bay vào ô coin (số đếm lên), xong thì sang level kế.
    /// panel của PopupView để trống: không phóng cả màn, các phần tự diễn theo thứ tự.
    /// </summary>
    public class WinPopup : PopupView
    {
        private EconomyConfig _economy;
        private LevelStars _levelStars;
        private LevelManager _levels;
        private IScreenNavigator _screens;
        private ICurrencyWallet _coins;
        private IAdsService _ads;

        /// <summary>GameBootstrap gọi: ví coin (Economy, qua contract), quảng cáo có thưởng.</summary>
        public void Init(ICurrencyWallet coins, IAdsService ads, IScreenNavigator screens, LevelManager levels, LevelStars levelStars, EconomyConfig economy)
        {
            _coins = coins;
            _ads = ads;
            _screens = screens;
            _levels = levels;
            _levelStars = levelStars;
            _economy = economy;
        }

        [Header("Thành phần")]
        [Tooltip("Ảnh tiêu đề, thứ tự theo WinTitle: Well done, Nice, Great, Perfect")]
        [SerializeField] private Image[] titleImages = new Image[4];
        [Tooltip("Sao vàng 1..3 (sao xám bóng bên dưới là ảnh tĩnh)")]
        [SerializeField] private Image[] stars = new Image[StarRating.Max];
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
        [Tooltip("Coin bay mẫu (con tắt sẵn của flyLayer, Editor dựng)")]
        [SerializeField] private Image coinTemplate;
        [Tooltip("Giây / 1 vòng lật của coin bay (xoay quanh trục Y, canvas Overlay vẽ thành đồng xu lật)")]
        [SerializeField] private float coinSpinPeriod = 0.45f;

        [Header("Thưởng")]
        [SerializeField] private int maxFlyingCoins = 12;

        [Header("Nhịp")]
        [SerializeField] private float raysSpeed = 30f; // độ / giây
        [SerializeField] private float titleDelay = 0.05f;

        [Header("Sao đập xuống (WinUI gốc)")]
        [SerializeField] private float starsDelay = 0.45f;
        [SerializeField] private float starSlamScale = 2.6f;
        [SerializeField] private float starSlamDuration = 0.18f;
        [SerializeField] private float starSlamPunch = 0.25f;
        [SerializeField] private float starSlamInterval = 0.18f;

        private bool _claimed;
        private bool _ready; // intro xong mới nhận bấm (không tắt interactable lúc intro để nút không bị xám)
        private Sequence _intro;
        private Sequence _claim;
        private Tween _raysSpin;
        private int _shownCoins;
        private int _shownReward; // số coin đang hiện trên nút Continue (tăng theo từng sao)
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
            _shownCoins = _coins.Balance;
            SetCoinText(_shownCoins);
            SetRewardText(BaseReward, false); // sao đập xuống sẽ cộng dần tới CoinReward
            PlayIntro();
        }

        // ---------- Mở màn ----------

        private void PlayIntro()
        {
            _intro?.Kill();
            _claim?.Kill();
            int earned = _levelStars != null ? _levelStars.Stars : StarRating.Max;
            Image title = ShowTitle(StarRating.Title(earned));
            SetScale(title != null ? title.rectTransform : null, 0f);
            foreach (Image star in stars) if (star != null) star.gameObject.SetActive(false);
            SetScale(medallion, 0f);
            SetScale(glow, 0f);
            SetScale(rays, 0f);
            SetScale(nextButton != null ? (RectTransform)nextButton.transform : null, 0f);
            SetScale(doubleButton != null ? (RectTransform)doubleButton.transform : null, 0f);
            SetButtons(true);
            _ready = false;

            _raysSpin?.Kill();
            if (rays != null)
            {
                rays.localRotation = Quaternion.identity;
                _raysSpin = rays.DOLocalRotate(new Vector3(0f, 0f, -360f), 360f / raysSpeed, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(gameObject);
            }

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (title != null) seq.Insert(titleDelay, Pop(title.rectTransform, 0.4f, 1.8f));
            seq.Insert(0.15f, Pop(medallion, 0.45f, 1.6f));
            seq.Insert(0.15f, Pop(glow, 0.6f, 1f));
            seq.Insert(0.2f, Pop(rays, 0.5f, 1.2f));
            seq.InsertCallback(0.2f, () =>
            {
                if (confetti == null) return;
                confetti.Burst();
                confetti.StartRain();
            });

            // 2 nút bật trước (thấy số coin cộng dần theo từng sao), rồi từng sao đập xuống; sao xong mới nhận bấm
            float buttonsAt = titleDelay + 0.2f;
            if (nextButton != null) seq.Insert(buttonsAt, Pop((RectTransform)nextButton.transform, 0.3f, 2f));
            if (doubleButton != null) seq.Insert(buttonsAt + 0.1f, Pop((RectTransform)doubleButton.transform, 0.3f, 2f));
            int count = Mathf.Min(earned, stars.Length);
            float slamFrom = Mathf.Max(starsDelay, buttonsAt + 0.45f);
            for (int i = 0; i < count; i++) InsertStarSlam(seq, i, slamFrom + i * starSlamInterval);
            float t = Mathf.Max(buttonsAt + 0.45f, slamFrom + count * starSlamInterval + starSlamDuration);
            seq.InsertCallback(t + 0.1f, () => _ready = true);
            _intro = seq;
        }

        /// <summary>Bật đúng 1 ảnh tiêu đề, trả về ảnh đó (null nếu chưa gán).</summary>
        private Image ShowTitle(WinTitle kind)
        {
            Image shown = null;
            for (int i = 0; i < titleImages.Length; i++)
            {
                if (titleImages[i] == null) continue;
                bool on = i == (int)kind;
                titleImages[i].gameObject.SetActive(on);
                if (on) shown = titleImages[i];
            }
            return shown;
        }

        /// <summary>Sao thứ i: hiện to gấp starSlamScale + trong suốt -> thu về 1 (nhanh dần) + rõ hẳn -> chạm thì nảy, kêu (cao dần), rung nhẹ.</summary>
        private void InsertStarSlam(Sequence seq, int i, float at)
        {
            Image star = stars[i];
            if (star == null) return;
            RectTransform rt = star.rectTransform;
            seq.InsertCallback(at, () =>
            {
                star.gameObject.SetActive(true);
                rt.localScale = Vector3.one * starSlamScale;
                star.color = new Color(1f, 1f, 1f, 0f);
            });
            seq.Insert(at, rt.DOScale(1f, starSlamDuration).SetEase(Ease.InQuad));
            seq.Insert(at, star.DOFade(1f, starSlamDuration * 0.6f));
            seq.InsertCallback(at + starSlamDuration, () =>
            {
                rt.DOPunchScale(Vector3.one * starSlamPunch, 0.25f, 6, 0.6f).SetUpdate(true).SetLink(star.gameObject);
                AudioManager.PlayPitched(SoundId.StarEarn, 1f + 0.12f * i);
                AudioManager.Vibrate(HapticType.Light);
                SetRewardText(_shownReward + _economy.coinsPerStar, true);
            });
        }

        // ---------- Nhận thưởng ----------

        // Theo độ khó level vừa thắng: Normal 10 / Hard 20 / Very Hard 30 (EconomyConfig), chưa tính sao
        private int BaseReward
        {
            get
            {
                LevelDefinition level = _levels != null ? _levels.CurrentLevel : null;
                return _economy.WinCoinReward(level != null ? level.difficulty : LevelDifficulty.Normal);
            }
        }

        // Thưởng thật: theo độ khó + coinsPerStar x số sao
        private int CoinReward
        {
            get
            {
                LevelDefinition level = _levels != null ? _levels.CurrentLevel : null;
                int stars = _levelStars != null ? _levelStars.Stars : 0;
                return _economy.WinCoinReward(level != null ? level.difficulty : LevelDifficulty.Normal, stars);
            }
        }

        /// <summary>Số coin trên Continue / x2; pop: số nảy (mỗi sao cộng thêm).</summary>
        private void SetRewardText(int amount, bool pop)
        {
            _shownReward = amount;
            if (nextAmountText != null) nextAmountText.SetText("{0}", amount);
            if (doubleAmountText != null) doubleAmountText.SetText("{0}", amount * 2);
            if (!pop) return;
            foreach (TMP_Text t in new[] { nextAmountText, doubleAmountText })
            {
                if (t == null) continue;
                Transform row = t.transform.parent; // hàng "coin + số"
                row.DOKill(true);
                row.DOPunchScale(Vector3.one * 0.25f, 0.25f, 6, 0.6f).SetUpdate(true).SetLink(row.gameObject);
            }
        }

        private void OnNext()
        {
            if (_ready) Claim(CoinReward);
        }

        // Bấm xong 2 nút chuyển xám (UIGrayscaleWhenDisabled) như nút Mua booster khi thiếu coin
        private void OnDouble()
        {
            if (!_ready || _claimed) return;
            SetButtons(false);
            _ads.Show("win_x2", ok =>
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
            AudioManager.Play(SoundId.Reward);

            int from = _shownCoins;
            _coins.Add(coins);
            int to = _coins.Balance;

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
                if (coin == null) break;
                RectTransform rt = coin.rectTransform;
                rt.position = start;
                rt.localScale = Vector3.zero;
                Spin(rt);
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
                    AudioManager.Vibrate(HapticType.Coin); // mỗi đồng chạm ô coin 1 tick nhẹ
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
                LevelManager lm = _levels;
                if (lm != null && lm.IsDaily && _screens != null) _screens.ShowMenu(); // Daily: về tab Daily
                else if (lm != null) lm.NextLevel();
            });
            _claim = seq;
        }

        /// <summary>Coin lật liên tục quanh trục Y, mỗi đồng lệch pha ngẫu nhiên; tắt coin (chạm ô coin) là dừng.</summary>
        private void Spin(RectTransform coin)
        {
            if (coinSpinPeriod <= 0f) return;
            coin.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            coin.DOLocalRotate(new Vector3(0f, 360f, 0f), coinSpinPeriod, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(coin.gameObject, LinkBehaviour.KillOnDisable);
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
            if (coinTemplate == null) return null;
            Image img = Instantiate(coinTemplate, flyLayer != null ? flyLayer : transform);
            img.name = "FlyCoin";
            GameObject go = img.gameObject;
            go.layer = gameObject.layer;
            go.SetActive(true);
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = new Vector2(86f, 86f);
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

#if UNITY_EDITOR
        /// <summary>Editor: Dựng sẵn object con (thay cho tạo lúc chạy). Chạy 1 lần (menu chuột phải component) rồi lưu scene / prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            coinTemplate = EditorChildren.Child<Image>(flyLayer != null ? flyLayer : transform, "FlyCoinTemplate", false);
            coinTemplate.raycastTarget = false;
        }
#endif
    }
}
