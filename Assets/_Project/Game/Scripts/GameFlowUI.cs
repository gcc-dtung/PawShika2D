using System;
using UnityEngine;
using UnityEngine.Serialization;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Profile;
using PawBlock.Features.Settings;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Nối trạng thái ván (GameStateChangedEvent) với các popup.
    /// Có component này trong scene thì LevelManager không tự chuyển level / chơi lại nữa (AutoAdvance = false).
    /// Đổi level thì popup tự đóng (PopupView nghe LevelLoadedEvent), ở đây không cần liệt kê lại.
    /// </summary>
    public class GameFlowUI : MonoBehaviour
    {
        private PlayerProfileService _profile;
        private IIAPService _iap;

        /// <summary>GameBootstrap gọi trước OnEnable: hồ sơ người chơi (popup hồ sơ), IAP (báo mua lỗi).</summary>
        public void Init(PlayerProfileService profile, IIAPService iap)
        {
            _profile = profile;
            _iap = iap;
        }

        [SerializeField] private WinPopup winPopup;
        [SerializeField, FormerlySerializedAs("revivePopup")] private MoreTimePopup moreTimePopup;
        [SerializeField] private LosePopup losePopup;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private QuitLevelPopup quitLevelPopup;
        [SerializeField] private NoticePopup noticePopup;
        [SerializeField] private DailyRewardPopup dailyRewardPopup;
        [SerializeField] private BuyBoosterPopup buyBoosterPopup;
        [SerializeField] private RefillEnergyPopup refillEnergyPopup;
        [SerializeField] private ProfilePopup profilePopup;

        private PopupView[] AllPopups => new PopupView[]
        {
            winPopup, moreTimePopup, losePopup, settingsPopup, quitLevelPopup,
            noticePopup, dailyRewardPopup, buyBoosterPopup, refillEnergyPopup, profilePopup
        };

        [Header("Hết level")]
        [SerializeField] private string comingSoonTitle = "COMING SOON";
        [SerializeField, TextArea] private string comingSoonMessage = "You cleared all {0} levels!\nNew levels are on the way.\nPlay again from Level 1.";

        private void Awake()
        {
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<AllLevelsCompletedEvent>.Subscribe(OnAllLevelsCompleted);
            if (LevelManager.Instance != null) LevelManager.Instance.AutoAdvance = false; // popup quyết định sang level kế / chơi lại
        }

        private void Start()
        {
            foreach (PopupView popup in AllPopups)
            {
                if (popup != null) popup.CloseImmediate();
            }
            if (losePopup != null) losePopup.RetryHandler = RetryAfterLose;
            if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.AddListener(OnBuyBooster);
            if (_iap != null) _iap.PurchaseFailed += OnPurchaseFailed;
        }

        private void OnDestroy()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<AllLevelsCompletedEvent>.Unsubscribe(OnAllLevelsCompleted);
            if (LevelManager.Instance != null) LevelManager.Instance.AutoAdvance = true;
            if (BoosterManager.Instance != null) BoosterManager.Instance.onBuyRequested.RemoveListener(OnBuyBooster);
            if (_iap != null) _iap.PurchaseFailed -= OnPurchaseFailed;
        }

        /// <summary>Nút cài đặt trên HUD / trên Menu. Trong ván chỉ mở khi đang chơi (không chồng lên popup khác).</summary>
        public void OpenSettings()
        {
            if (settingsPopup == null || settingsPopup.IsOpen) return;
            bool inMenu = ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
            if (!inMenu && LevelManager.Instance != null && !LevelManager.Instance.IsPlaying) return;
            settingsPopup.Open();
        }

        /// <summary>Nút Daily trên Menu (và tự mở khi vào game lần đầu trong ngày).</summary>
        public void OpenDailyReward()
        {
            if (dailyRewardPopup == null || dailyRewardPopup.IsOpen) return;
            dailyRewardPopup.Open();
        }

        /// <summary>Nút avatar trên Menu: hồ sơ của mình.</summary>
        public void OpenProfile()
        {
            if (_profile != null) ShowProfile(_profile.Local());
        }

        /// <summary>Hồ sơ bất kỳ (của mình, hoặc người khác khi bấm vào 1 dòng bảng xếp hạng).</summary>
        public void ShowProfile(PlayerProfile profile)
        {
            if (profilePopup == null || profile == null) return;
            if (profilePopup.IsOpen && profilePopup.Current != null && profilePopup.Current.PlayerId == profile.PlayerId) return;
            profilePopup.Show(profile);
        }

        /// <summary>Thông báo chung (popup NOTICE).</summary>
        public void ShowNotice(string message, string title = "NOTICE", Action onClose = null)
        {
            if (noticePopup != null) noticePopup.Show(message, title, onClose);
            else Debug.Log($"[GameFlowUI] {title}: {message}");
        }

        private void OnPurchaseFailed(string productId, string reason) => ShowNotice(reason);

        /// <summary>Nút năng lượng trên Menu.</summary>
        public void OpenEnergyRefill()
        {
            if (refillEnergyPopup == null || refillEnergyPopup.IsOpen) return;
            bool inMenu = ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
            if (!inMenu && LevelManager.Instance != null && !LevelManager.Instance.IsPlaying) return; // đang có popup khác
            refillEnergyPopup.ShowOptional();
        }

        /// <summary>
        /// Nút PLAY trên Menu: còn năng lượng thì <paramref name="start"/> ngay; hết thì mở popup nạp (có X để thôi),
        /// có năng lượng thì tự vào chơi.
        /// </summary>
        public void RequestPlay(Action start)
        {
            if (EnergyManager.Instance == null || EnergyManager.Instance.CanPlay) start?.Invoke();
            else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(start, () => { });
            else start?.Invoke();
        }

        /// <summary>Thua rồi bấm Retry: còn năng lượng thì chơi lại, hết thì chờ / nạp rồi mới chơi lại.</summary>
        private void RetryAfterLose()
        {
            LevelManager lm = LevelManager.Instance;
            if (lm == null) return;
            if (EnergyManager.Instance == null || EnergyManager.Instance.CanPlay) lm.Retry();
            else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(lm.Retry, () => Open(losePopup)); // X -> về lại popup Thua
            else lm.Retry();
        }

        /// <summary>Thắng level cuối: báo "Coming soon", đóng thì chơi lại từ level 1.</summary>
        private void OnAllLevelsCompleted(AllLevelsCompletedEvent e)
        {
            ShowNotice(string.Format(comingSoonMessage, e.LevelCount), comingSoonTitle, () =>
            {
                if (LevelManager.Instance != null) LevelManager.Instance.RestartFromFirst();
            });
        }

        private void OnBuyBooster(BoosterType type)
        {
            if (buyBoosterPopup != null) buyBoosterPopup.Show(type);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            switch (e.State)
            {
                case GameState.Won: Open(winPopup); break;
                case GameState.OutOfTime: Open(moreTimePopup); break;
                case GameState.Lost: Open(losePopup); break;
            }
        }

        private static void Open(PopupView popup)
        {
            if (popup != null) popup.Open();
        }
    }
}
