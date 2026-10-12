using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Game;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Thanh thời gian dưới khung LEVEL có 3 mốc sao (SafeArea/TimeAndStar/TimeProgress của bản gốc):
    /// nền tin_trnh2, phần thời gian còn lại tin_trnh1 (Sliced, co theo chiều rộng nên 2 đầu luôn bo tròn),
    /// 3 mốc đặt ở 0 / twoStarRatio / threeStarRatio (LivesEnergyConfig), mỗi mốc sao xám (star2) dưới + sao vàng (star) trên.
    /// Thời gian tụt qua mốc (StarsChangedEvent Lost) -> sao vàng phồng lên rồi tan, còn lại sao xám.
    /// Lưới mới -> 3 sao vàng bật lại lần lượt. Đóng băng -> phần còn lại ngả xanh băng.
    /// Dựng bởi Tools/PawBlock/Builders/Build Stars (StarsBuilder).
    /// </summary>
    public class TimeStarBar : MonoBehaviour
    {
        private LivesEnergyConfig _lives;
        private LevelStars _levelStars;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelTimer timer, LevelStars levelStars, LivesEnergyConfig lives)
        {
            _timer = timer;
            _levelStars = levelStars;
            _lives = lives;
        }

        [Tooltip("Vùng chạy của phần còn lại (bên trong viền nền), mốc sao đặt theo chiều rộng vùng này")]
        [SerializeField] private RectTransform fillArea;
        [SerializeField] private Image fill;
        [Tooltip("Mốc của sao 1, 2, 3 (con của fillArea, neo mép trái)")]
        [SerializeField] private RectTransform[] markers = new RectTransform[StarRating.Max];
        [Tooltip("Sao vàng của từng mốc (cùng thứ tự markers)")]
        [SerializeField] private Image[] goldStars = new Image[StarRating.Max];

        [SerializeField] private Color frozenTint = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private float poofScale = 1.8f;
        [SerializeField] private float poofDuration = 0.35f;

        private LevelTimer _timer;
        private float _ratio = 1f;
        private int _shownStars = StarRating.Max;

        private void OnEnable()
        {
            PlaceMarkers();
            EventBus<StarsChangedEvent>.Subscribe(OnStarsChanged);
            if (_timer != null)
            {
                _timer.onTimeChanged.AddListener(OnTimeChanged);
                _timer.onFreezeChanged.AddListener(OnFreezeChanged);
                OnTimeChanged(_timer.Remaining, _timer.Total);
                OnFreezeChanged(_timer.IsFrozen);
            }
            LevelStars stars = _levelStars;
            ShowStars(stars != null ? stars.Stars : StarRating.Max);
        }

        private void OnDisable()
        {
            EventBus<StarsChangedEvent>.Unsubscribe(OnStarsChanged);
            if (_timer != null)
            {
                _timer.onTimeChanged.RemoveListener(OnTimeChanged);
                _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            }
            foreach (Image star in goldStars) if (star != null) star.rectTransform.DOKill(true);
        }

        // Kích thước vùng chạy đổi (màn hình xoay / Canvas co giãn): đặt lại mốc + phần còn lại
        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            PlaceMarkers();
            ApplyFill();
        }

        /// <summary>Mốc sao i (1..3) ở đúng tỉ lệ thời gian của nó trên vùng chạy.</summary>
        private void PlaceMarkers()
        {
            if (fillArea == null) return;
            LivesEnergyConfig cfg = _lives;
            float width = fillArea.rect.width;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null) continue;
                float ratio = StarRating.MarkerRatio(i + 1, cfg.twoStarRatio, cfg.threeStarRatio);
                markers[i].anchoredPosition = new Vector2(ratio * width, markers[i].anchoredPosition.y);
            }
        }

        private void OnTimeChanged(float remaining, float total)
        {
            _ratio = total > 0f ? Mathf.Clamp01(remaining / total) : 1f;
            ApplyFill();
        }

        private void ApplyFill()
        {
            if (fill == null || fillArea == null) return;
            RectTransform rt = fill.rectTransform;
            rt.sizeDelta = new Vector2(_ratio * fillArea.rect.width, rt.sizeDelta.y);
            fill.enabled = _ratio > 0f;
        }

        private void OnFreezeChanged(bool frozen)
        {
            if (fill == null) return;
            fill.DOKill();
            fill.DOColor(frozen ? frozenTint : Color.white, 0.25f).SetLink(fill.gameObject);
        }

        private void OnStarsChanged(StarsChangedEvent e)
        {
            if (e.Lost)
            {
                for (int i = e.Stars; i < _shownStars && i < goldStars.Length; i++) Poof(goldStars[i]);
                _shownStars = e.Stars;
            }
            else
            {
                ShowStars(e.Stars, animate: true);
            }
        }

        /// <summary>Sao 1..stars vàng, còn lại xám. animate: sao vàng bật lần lượt từ 0.</summary>
        private void ShowStars(int stars, bool animate = false)
        {
            _shownStars = stars;
            for (int i = 0; i < goldStars.Length; i++)
            {
                Image star = goldStars[i];
                if (star == null) continue;
                RectTransform rt = star.rectTransform;
                rt.DOKill();
                star.DOKill();
                bool on = i < stars;
                star.gameObject.SetActive(on);
                star.color = Color.white;
                rt.localScale = on && animate ? Vector3.zero : Vector3.one;
                if (on && animate) rt.DOScale(1f, 0.3f).SetDelay(0.08f * i).SetEase(Ease.OutBack).SetLink(star.gameObject);
            }
        }

        /// <summary>Sao vàng phồng lên + mờ dần rồi tắt, lộ sao xám bên dưới.</summary>
        private void Poof(Image star)
        {
            if (star == null || !star.gameObject.activeSelf) return;
            RectTransform rt = star.rectTransform;
            rt.DOKill(true);
            star.DOKill();
            DOTween.Sequence()
                .Append(rt.DOScale(poofScale, poofDuration).SetEase(Ease.OutQuad))
                .Join(star.DOFade(0f, poofDuration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    star.gameObject.SetActive(false);
                    rt.localScale = Vector3.one;
                    star.color = Color.white;
                })
                .SetTarget(rt) // rt.DOKill (lưới mới giữa lúc đang tan) hủy được cả chuỗi
                .SetLink(star.gameObject);
        }
    }
}
