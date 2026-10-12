using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Shared;

namespace PawBlock.Features.LuckyWheel
{
    /// <summary>
    /// Vòng quay may mắn (mở từ nút "Lucky" trên Home): tiêu LuckyWheelDatabase.spinCost sao (StarWallet) mỗi lần quay.
    /// Bấm SPIN: trừ sao, chọn ô theo trọng số (LuckyWheelModel.Pick) và trao quà NGAY (tắt app giữa lúc quay vẫn không mất),
    /// rồi vòng quay theo chiều kim đồng hồ spinTurns vòng, chậm dần (OutQuart), mỗi lần kim đi qua 1 ô thì "tick" + kim nảy;
    /// dừng thì ô trúng nhấp sáng, dòng kết quả hiện icon + số, âm nhận thưởng. Thiếu sao: nút xám, dòng gợi ý thắng level để lấy sao.
    /// Các ô (màu, icon, số) dựng bởi Tools/PawBlock/Builders/Build Lucky Wheel theo database.
    /// </summary>
    public class LuckyWheelPopup : PopupView
    {
        [SerializeField] private LuckyWheelDatabase database;
        [Tooltip("Icon coin / năng lượng vô hạn cho dòng kết quả (dùng chung với điểm danh)")]
        [SerializeField] private DailyRewardDatabase rewardIcons;
        [SerializeField] private RectTransform wheel;      // quay quanh tâm
        [SerializeField] private RectTransform pointer;    // kim ở đỉnh
        [SerializeField] private Image[] segmentImages;    // cùng thứ tự database.segments (để nhấp sáng ô trúng)
        [SerializeField] private Button spinButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private RectTransform resultRow;
        [SerializeField] private Image resultIcon;
        [SerializeField] private TMP_Text resultText;

        [Header("Quay")]
        [SerializeField] private int spinTurns = 5;
        [SerializeField] private float spinDuration = 4f;
        [Tooltip("Dừng lệch ngẫu nhiên quanh tâm ô (tỉ lệ nửa ô) cho tự nhiên")]
        [SerializeField, Range(0f, 0.9f)] private float stopJitter = 0.6f;

        private bool _spinning;
        private Tween _spin;
        private Tween _flash;
        private int _lastTick;

        public bool IsSpinning => _spinning;

        protected override void Awake()
        {
            base.Awake();
            if (spinButton != null) spinButton.onClick.AddListener(Spin);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StarWallet.Changed += OnStarsChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StarWallet.Changed -= OnStarsChanged;
            // Đóng giữa lúc quay (đổi màn...): quà đã trao lúc bấm, chỉ dừng hình
            _spin?.Kill();
            _flash?.Kill();
            _spinning = false;
        }

        protected override void OnOpen()
        {
            if (resultRow != null) resultRow.gameObject.SetActive(false);
            if (costText != null && database != null) costText.SetText("{0}", database.spinCost);
            if (resultText != null && resultRow == null) resultText.SetText("");
            Refresh();
            ShowHint();
        }

        private void OnStarsChanged(int balance, int delta) => Refresh();

        private void Refresh()
        {
            if (balanceText != null) balanceText.SetText("{0}", StarWallet.Balance);
            bool afford = database != null && StarWallet.CanAfford(database.spinCost);
            if (spinButton != null) spinButton.interactable = afford && !_spinning;
            if (closeButton != null) closeButton.interactable = !_spinning;
        }

        /// <summary>Chưa đủ sao: gợi ý cách lấy sao ở dòng kết quả.</summary>
        private void ShowHint()
        {
            if (database == null || StarWallet.CanAfford(database.spinCost) || resultText == null) return;
            if (resultRow != null) resultRow.gameObject.SetActive(true);
            if (resultIcon != null) resultIcon.gameObject.SetActive(false);
            resultText.SetText("Win levels to earn stars!");
        }

        private void OnClose()
        {
            if (!_spinning) Close();
        }

        private void Spin()
        {
            if (_spinning || database == null || database.segments.Count == 0 || wheel == null) return;
            int[] weights = new int[database.segments.Count];
            for (int i = 0; i < weights.Length; i++) weights[i] = database.segments[i].weight;
            int index = LuckyWheelModel.Pick(weights, Random.value);
            if (index < 0 || !StarWallet.TrySpend(database.spinCost)) return;

            DailyRewardItem item = database.segments[index].item;
            DailyRewardManager.GrantItem(item); // trao ngay, vòng quay chỉ là hình

            _spinning = true;
            Refresh();
            if (resultRow != null) resultRow.gameObject.SetActive(false);
            _flash?.Kill();
            ResetSegmentColors();

            int count = database.segments.Count;
            float half = 180f / count;
            float current = wheel.localEulerAngles.z;
            float target = LuckyWheelModel.TargetRotation(current, index, count, spinTurns, Random.Range(-stopJitter, stopJitter) * half);
            _lastTick = Mathf.FloorToInt(-current / (360f / count));
            float angle = current;
            _spin = DOTween.To(() => angle, a =>
                {
                    angle = a;
                    wheel.localRotation = Quaternion.Euler(0f, 0f, a);
                    Tick(a, count);
                }, target, spinDuration)
                .SetEase(Ease.OutQuart).SetUpdate(true).SetLink(gameObject)
                .OnComplete(() => Finish(index, item));
        }

        // Kim đi qua ranh giới 1 ô: tick + kim nghiêng nảy
        private void Tick(float angle, int count)
        {
            int tick = Mathf.FloorToInt(-angle / (360f / count));
            if (tick == _lastTick) return;
            _lastTick = tick;
            AudioManager.Play(SoundId.DragTick);
            if (pointer == null) return;
            pointer.DOKill(true);
            pointer.localRotation = Quaternion.Euler(0f, 0f, 18f);
            pointer.DOLocalRotate(Vector3.zero, 0.12f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(pointer.gameObject);
        }

        private void Finish(int index, DailyRewardItem item)
        {
            _spinning = false;
            Refresh();
            AudioManager.Play(SoundId.Reward);
            AudioManager.Vibrate(HapticType.Success);

            // Ô trúng nhấp sáng 3 lần
            if (segmentImages != null && index < segmentImages.Length && segmentImages[index] != null)
            {
                Image seg = segmentImages[index];
                Color baseColor = database.segments[index].color;
                _flash = seg.DOColor(Color.Lerp(baseColor, Color.white, 0.65f), 0.15f).SetLoops(6, LoopType.Yoyo)
                    .SetUpdate(true).SetLink(seg.gameObject).OnKill(() => { if (seg != null) seg.color = baseColor; });
            }

            if (resultRow == null) return;
            resultRow.gameObject.SetActive(true);
            if (resultIcon != null)
            {
                resultIcon.gameObject.SetActive(true);
                resultIcon.sprite = DailyGiftBubble.IconOf(item, rewardIcons);
            }
            if (resultText != null) resultText.SetText($"You got {DailyGiftBubble.AmountOf(item)}!");
            resultRow.DOKill(true);
            resultRow.localScale = Vector3.one * 0.6f;
            resultRow.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(resultRow.gameObject);
        }

        private void ResetSegmentColors()
        {
            if (segmentImages == null) return;
            for (int i = 0; i < segmentImages.Length && i < database.segments.Count; i++)
                if (segmentImages[i] != null) segmentImages[i].color = database.segments[i].color;
        }
    }
}
