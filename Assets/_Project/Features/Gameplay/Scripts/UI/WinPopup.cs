using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Shared;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Màn thắng như bản gốc (WinUI, full màn hình trên nền tối, không có bảng):
    /// ô coin trên cùng, chữ tiêu đề theo số sao (Perfect 3 / Great 2 / Nice 1 / Well done 0 sao, StarRating.Title),
    /// 3 sao (sao xám làm bóng, sao vàng đập xuống), huy hiệu mèo (tia sáng xoay), confetti,
    /// hàng "đống coin + +N" dưới huy hiệu (coin nhận được: theo độ khó + EconomyConfig.coinsPerStar mỗi sao),
    /// 2 nút kiểu Snacky Dash: x2 (quảng cáo, chỉ icon + "x2" + coin) và Continue (chỉ chữ).
    /// Mở: tiêu đề bật cùng huy hiệu + confetti bắn 2 bên rồi rơi liên tục, hàng +N và 2 nút bật -> từng sao đập xuống (phóng to starSlamScale
    /// lần rồi thu về trong starSlamDuration giây, cách nhau starSlamInterval, số của WinUI gốc), mỗi sao cộng vào +N và số nảy -> nhận bấm.
    /// x2 (quảng cáo): xem xong thì nút x2 nảy to rồi nổ ra coin bay vào hàng +N (số nhân đôi, nảy) và biến mất,
    /// Continue trượt vào giữa. Continue: huy hiệu + hàng +N thu lại, coin (lật quanh trục Y) bung ra từ hàng +N rồi bay vào ô coin (số đếm lên),
    /// xong thì level thường tới menuAfterLevel sang level kế, từ level menuAfterLevel + 1 (và Daily) về Main Menu.
    /// 2 nút luôn tương tác được (có hiệu ứng nhấn, không xám); lúc chưa cho phép (intro, đang chờ quảng cáo, đang nhận) bấm chỉ bị bỏ qua.
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
        [Tooltip("Hàng \"đống coin + số\" dưới huy hiệu: coin nhận được (nảy khi cộng), coin bay đi từ đây")]
        [SerializeField] private RectTransform rewardRow;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private UIConfetti confetti;
        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private Sprite coinSprite;
        [Tooltip("Coin bay mẫu (con tắt sẵn của flyLayer, Editor dựng)")]
        [SerializeField] private Image coinTemplate;
        [Tooltip("Giây / 1 vòng lật của coin bay (xoay quanh trục Y, canvas Overlay vẽ thành đồng xu lật)")]
        [SerializeField] private float coinSpinPeriod = 0.45f;

        [Header("Thưởng")]
        [SerializeField] private int maxFlyingCoins = 12;
        [Tooltip("Thắng level thường tới số này: Continue sang level kế; lớn hơn: Continue về Main Menu")]
        [SerializeField] private int menuAfterLevel = 15;

        [Header("x2 nổ / Continue vào giữa")]
        [SerializeField] private float doublePopScale = 1.3f;
        [SerializeField] private float doublePopDuration = 0.16f;
        [SerializeField] private float nextSlideDuration = 0.35f;
        [Tooltip("Số coin bay từ nút x2 vào hàng \"+N\"")]
        [SerializeField] private int doubleBurstCoins = 6;

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
        private bool _doubled;
        private bool _ready; // cho phép bấm: intro xong, không chờ quảng cáo / không đang diễn x2 (nút không bao giờ tắt interactable)
        private Sequence _intro;
        private Sequence _claim;
        private Sequence _double;
        private Tween _raysSpin;
        private int _shownCoins;
        private int _shownReward; // số coin đang hiện ở hàng "+N" (tăng theo từng sao)
        private Vector2 _nextPos;
        private readonly List<Image> _coinPool = new List<Image>();

        protected override void Awake()
        {
            base.Awake();
            if (nextButton != null) _nextPos = ((RectTransform)nextButton.transform).anchoredPosition;
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (doubleButton != null) doubleButton.onClick.AddListener(OnDouble);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _intro?.Kill();
            _claim?.Kill();
            _double?.Kill();
            _raysSpin?.Kill();
            if (confetti != null) confetti.StopRain();
            foreach (Image c in _coinPool) if (c != null) c.gameObject.SetActive(false);
        }

        protected override void OnOpened()
        {
            _claimed = false;
            _doubled = false;
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
            _double?.Kill();
            int earned = _levelStars != null ? _levelStars.Stars : StarRating.Max;
            Image title = ShowTitle(StarRating.Title(earned));
            SetScale(title != null ? title.rectTransform : null, 0f);
            foreach (Image star in stars) if (star != null) star.gameObject.SetActive(false);
            SetScale(medallion, 0f);
            SetScale(glow, 0f);
            SetScale(rays, 0f);
            SetScale(rewardRow, 0f);
            SetScale(nextButton != null ? (RectTransform)nextButton.transform : null, 0f);
            SetScale(doubleButton != null ? (RectTransform)doubleButton.transform : null, 0f);
            if (nextButton != null) ((RectTransform)nextButton.transform).anchoredPosition = _nextPos; // lần trước đã trượt vào giữa
            if (doubleButton != null) doubleButton.gameObject.SetActive(true);
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

            // Hàng +N và 2 nút bật trước (thấy số coin cộng dần theo từng sao), rồi từng sao đập xuống; sao xong mới nhận bấm
            float buttonsAt = titleDelay + 0.2f;
            seq.Insert(buttonsAt, Pop(rewardRow, 0.3f, 2f));
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

        /// <summary>Số "+N" dưới huy hiệu; pop: hàng coin + số nảy (mỗi sao cộng thêm, x2 nhân đôi).</summary>
        private void SetRewardText(int amount, bool pop, float punch = 0.25f)
        {
            _shownReward = amount;
            if (rewardText != null) rewardText.SetText("+{0}", amount);
            if (!pop || rewardRow == null) return;
            rewardRow.DOKill(true);
            rewardRow.DOPunchScale(Vector3.one * punch, 0.3f, 6, 0.6f).SetUpdate(true).SetLink(rewardRow.gameObject);
        }

        // Thưởng nhận khi bấm Continue (đã xem x2 thì gấp đôi)
        private int ClaimReward => _doubled ? CoinReward * 2 : CoinReward;

        // Chưa cho phép thì bấm vẫn có hiệu ứng nhấn (ButtonPress) nhưng không làm gì
        private void OnNext()
        {
            if (_ready) Claim(ClaimReward);
        }

        private void OnDouble()
        {
            if (!_ready || _claimed || _doubled || _ads.IsShowing) return;
            _ready = false; // chờ quảng cáo: Continue cũng chưa nhận
            _ads.Show("win_x2", ok =>
            {
                if (this == null || !isActiveAndEnabled) return; // popup đã đóng trong lúc xem
                if (ok) PopDouble();
                else _ready = true;
            });
        }

        /// <summary>
        /// Xem xong quảng cáo: x2 nảy to rồi nổ ra coin bay vào hàng "+N" (chưa vào ví), mỗi đồng chạm thì số tăng dần tới gấp đôi, nảy + rung;
        /// x2 biến mất, Continue trượt vào giữa. Coin vào ví 1 lần khi bấm Continue.
        /// </summary>
        private void PopDouble()
        {
            _doubled = true;
            AudioManager.Play(SoundId.Reward);
            int doubled = CoinReward * 2;

            RectTransform rt = (RectTransform)doubleButton.transform;
            rt.DOKill(true);
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            seq.Insert(0f, rt.DOScale(doublePopScale, doublePopDuration).SetEase(Ease.OutQuad));
            seq.Insert(doublePopDuration, rt.DOScale(0f, 0.12f).SetEase(Ease.InBack));
            seq.InsertCallback(doublePopDuration, () => AudioManager.Vibrate(HapticType.Light));
            seq.InsertCallback(doublePopDuration + 0.12f, () => doubleButton.gameObject.SetActive(false));

            // Mỗi đồng chạm hàng +N: số tăng 1 phần (đồng cuối khớp gấp đôi), nảy nhẹ + tick rung như coin bay vào ô coin trên cùng
            Vector3 target = rewardRow != null ? rewardRow.position : rt.position;
            int from = _shownReward;
            int count = Mathf.Max(1, doubleBurstCoins);
            float last = InsertCoinFlight(seq, rt.position, target, count, doublePopDuration, i =>
            {
                bool final = i == count - 1;
                SetRewardText(from + Mathf.RoundToInt((doubled - from) * (i + 1f) / count), true, final ? 0.45f : 0.15f);
                AudioManager.Play(SoundId.Coin);
                AudioManager.Vibrate(HapticType.Coin); // Haptics tự chặn rung dồn (MinInterval)
            });
            seq.InsertCallback(last, () => SetRewardText(doubled, false)); // thiếu coin trong pool vẫn ra đúng số cuối
            float slideAt = doublePopDuration + 0.05f;
            if (nextButton != null)
            {
                RectTransform next = (RectTransform)nextButton.transform;
                seq.Insert(slideAt, next.DOAnchorPosX(0f, nextSlideDuration).SetEase(Ease.OutBack));
            }
            seq.InsertCallback(Mathf.Max(last, slideAt + nextSlideDuration), () => _ready = true); // số đã nhân đôi, Continue tới giữa
            _double = seq;
        }

        private void Claim(int coins)
        {
            if (_claimed) return;
            _claimed = true;
            _ready = false;
            _intro?.Complete();
            _double?.Complete();
            AudioManager.Play(SoundId.Reward);

            int from = _shownCoins;
            _coins.Add(coins);
            int to = _coins.Balance;

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (medallion != null) seq.Insert(0f, medallion.DOScale(0f, 0.3f).SetEase(Ease.InBack));
            if (glow != null) seq.Insert(0f, glow.DOScale(0f, 0.3f).SetEase(Ease.InQuad));
            if (rays != null) seq.Insert(0f, rays.DOScale(0f, 0.3f).SetEase(Ease.InQuad));
            if (rewardRow != null) seq.Insert(0.1f, rewardRow.DOScale(0f, 0.3f).SetEase(Ease.InBack));

            Vector3 start = rewardRow != null ? rewardRow.position : medallion != null ? medallion.position : transform.position;
            Vector3 target = coinIcon != null ? coinIcon.position : start;
            int count = Mathf.Clamp(coins, 1, maxFlyingCoins);
            float last = InsertCoinFlight(seq, start, target, count, 0f, i =>
            {
                SetCoinText(from + Mathf.RoundToInt((to - from) * (i + 1f) / count));
                AudioManager.Play(SoundId.Coin);
                AudioManager.Vibrate(HapticType.Coin); // mỗi đồng chạm ô coin 1 tick nhẹ
                if (coinPill != null)
                {
                    coinPill.DOKill(true);
                    coinPill.localScale = Vector3.one;
                    coinPill.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f).SetUpdate(true);
                }
            });
            seq.InsertCallback(last, () => SetCoinText(to));
            seq.AppendInterval(0.35f);
            seq.OnComplete(() =>
            {
                Close();
                LevelManager lm = _levels;
                if (lm == null) return;
                if (lm.IsDaily)
                {
                    if (_screens != null) _screens.ShowMenu(); // Daily: về tab Daily
                }
                else if (lm.CurrentLevelNumber > menuAfterLevel && _screens != null)
                {
                    _screens.ShowMenu(); // vào Menu trước: level kế nạp lúc đang ở Menu thì ScreenFlow tạm dừng nó, PLAY chơi tiếp
                    lm.NextLevel();
                }
                else lm.NextLevel();
            });
            _claim = seq;
        }

        /// <summary>Thêm vào seq: count đồng coin bung ra từ start rồi bay tới target, đồng thứ i chạm thì gọi onArrive(i). Trả về lúc đồng cuối chạm.</summary>
        private float InsertCoinFlight(Sequence seq, Vector3 start, Vector3 target, int count, float delay, System.Action<int> onArrive)
        {
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
                int index = i;
                float t0 = delay + 0.12f + i * 0.04f;

                seq.Insert(t0, rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
                seq.Insert(t0, rt.DOMove(start + (Vector3)(burst * transform.lossyScale.x), 0.28f).SetEase(Ease.OutCubic)); // đơn vị canvas -> world
                float t1 = t0 + 0.3f + i * 0.02f;
                seq.Insert(t1, rt.DOMove(target, 0.42f).SetEase(Ease.InCubic));
                seq.Insert(t1 + 0.25f, rt.DOScale(0.6f, 0.17f));
                seq.InsertCallback(t1 + 0.42f, () =>
                {
                    coin.gameObject.SetActive(false);
                    onArrive?.Invoke(index);
                });
                last = Mathf.Max(last, t1 + 0.42f);
            }
            return last;
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
