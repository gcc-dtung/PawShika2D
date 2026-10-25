using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Shared;

namespace PawBlock.Features.DailyReward
{
    /// <summary>
    /// Popup điểm danh 7 ngày (DailyRewardManager): 7 ô quà, nút CLAIM nhận quà hôm nay; nhận rồi thì thay bằng
    /// đồng hồ đếm tới ngày mai. Bấm ô hôm nay cũng nhận; bấm ô đã lỡ (có icon video) thì xem quảng cáo để bù.
    /// </summary>
    public class DailyRewardPopup : PopupView
    {
        private DailyRewardManager _rewards;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(DailyRewardManager rewards)
        {
            _rewards = rewards;
        }

        [SerializeField] private DailyRewardSlot[] slots;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject timerGroup;
        [SerializeField] private TMP_Text timerText;

        private bool _setup;
        private float _tick;

        protected override void Awake()
        {
            base.Awake();
            if (claimButton != null) claimButton.onClick.AddListener(OnClaimToday);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            foreach (DailyRewardSlot slot in slots)
            {
                DailyRewardSlot s = slot;
                if (s != null && s.Button != null) s.Button.onClick.AddListener(() => ClaimSlot(s));
            }
        }

        protected override void OnOpened()
        {
            DailyRewardManager m = _rewards;
            if (m == null) return;
            if (!_setup)
            {
                foreach (DailyRewardSlot s in slots) s.Setup(m.Database != null ? m.Database.Get(s.Day) : null, m);
                _setup = true;
            }
            m.onChanged.RemoveListener(Refresh); // Open lại khi đang mở: không đăng ký trùng
            m.onChanged.AddListener(Refresh);
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_rewards != null) _rewards.onChanged.RemoveListener(Refresh);
        }

        private void Update()
        {
            if (!IsOpen || timerGroup == null || !timerGroup.activeSelf) return;
            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f) return;
            _tick = 0.25f;
            UpdateTimer();
        }

        private void Refresh()
        {
            DailyRewardManager m = _rewards;
            if (m == null) return;
            foreach (DailyRewardSlot s in slots) s.SetState(m.StateOf(s.Day), m.CanRecover(s.Day));

            bool canClaim = m.CanClaimToday;
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(canClaim);
                claimButton.interactable = !m.IsBusy;
            }
            if (timerGroup != null) timerGroup.SetActive(!canClaim);
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (timerText == null || _rewards == null) return;
            System.TimeSpan t = _rewards.TimeToTomorrow;
            timerText.SetText("Next reward in {0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
        }

        private void OnClaimToday()
        {
            DailyRewardManager m = _rewards;
            if (m == null) return;
            foreach (DailyRewardSlot s in slots)
            {
                if (s.Day == m.TodayIndex) { ClaimSlot(s); return; }
            }
        }

        private void ClaimSlot(DailyRewardSlot slot)
        {
            DailyRewardManager m = _rewards;
            if (m == null || m.IsBusy) return;
            m.Claim(slot.Day, ok => { if (ok && slot != null) slot.PlayClaimed(); });
        }
    }
}
