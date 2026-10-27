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
    /// Đổi level (LevelLoadedEvent) thì đóng mọi popup đang mở có CloseOnLevelLoad (PopupView không biết level).
    /// </summary>
    public class GameFlowUI : MonoBehaviour, IMenuPopups
    {
        private PopupView[] _popups;
        private EnergyManager _energy;
        private BoosterManager _boosterManager;
        private LevelManager _levels;
        private ScreenFlow _screens;
        private PlayerProfileService _profile;
        private IIAPService _iap;

        /// <summary>GameBootstrap gọi trước OnEnable: hồ sơ người chơi (popup hồ sơ), IAP (báo mua lỗi).</summary>
        public void Init(PlayerProfileService profile, IIAPService iap, ScreenFlow screens, LevelManager levels, BoosterManager boosterManager, EnergyManager energy, PopupView[] popups)
        {
            _profile = profile;
            _iap = iap;
            _screens = screens;
            _levels = levels;
            _boosterManager = boosterManager;
            _energy = energy;
            _popups = popups;
        }

        [SerializeField] private WinPopup winPopup;
        [SerializeField, FormerlySerializedAs("revivePopup")] private MoreTimePopup moreTimePopup;
        [SerializeField] private LosePopup losePopup;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private QuitLevelPopup quitLevelPopup;
        [SerializeField] private NoticePopup noticePopup;
        [SerializeField] private ConfirmPopup confirmPopup;
        [SerializeField] private DailyRewardPopup dailyRewardPopup;
        [SerializeField] private BuyBoosterPopup buyBoosterPopup;
        [SerializeField] private RefillEnergyPopup refillEnergyPopup;
        [SerializeField] private ProfilePopup profilePopup;

        [Header("Hết level")]
        [SerializeField] private string comingSoonTitle = "COMING SOON";
        [SerializeField, TextArea] private string comingSoonMessage = "You cleared all {0} levels!\nNew levels are on the way.\nPlay again from Level 1.";

        private void Awake()
        {
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            EventBus<AllLevelsCompletedEvent>.Subscribe(OnAllLevelsCompleted);
            if (_levels != null) _levels.AutoAdvance = false; // popup quyết định sang level kế / chơi lại
        }

        private void Start()
        {
            foreach (PopupView popup in _popups ?? new PopupView[0])
            {
                if (popup != null) popup.CloseImmediate();
            }
            if (losePopup != null) losePopup.RetryHandler = RetryAfterLose;
            if (_boosterManager != null) _boosterManager.onBuyRequested.AddListener(OnBuyBooster);
            if (_iap != null) _iap.PurchaseFailed += OnPurchaseFailed;
        }

        private void OnDestroy()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            EventBus<AllLevelsCompletedEvent>.Unsubscribe(OnAllLevelsCompleted);
            if (_levels != null) _levels.AutoAdvance = true;
            if (_boosterManager != null) _boosterManager.onBuyRequested.RemoveListener(OnBuyBooster);
            if (_iap != null) _iap.PurchaseFailed -= OnPurchaseFailed;
        }

        /// <summary>Nút cài đặt trên HUD / trên Menu. Trong ván chỉ mở khi đang chơi (không chồng lên popup khác).</summary>
        public void OpenSettings()
        {
            if (settingsPopup == null || settingsPopup.IsOpen) return;
            bool inMenu = _screens != null && _screens.IsMenu;
            if (!inMenu && _levels != null && !_levels.IsPlaying) return;
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

        /// <summary>Hỏi lại (popup Có / Không); chỉ nút Có gọi onYes. Thiếu popup thì làm luôn.</summary>
        public void ShowConfirm(string message, string title, Action onYes, string yes = "Yes", string no = "No")
        {
            if (confirmPopup != null) confirmPopup.Show(message, title, onYes, yes, no);
            else onYes?.Invoke();
        }

        private void OnLevelLoaded(LevelLoadedEvent _)
        {
            if (_popups == null) return;
            foreach (PopupView popup in _popups)
            {
                if (popup != null && popup.IsOpen && popup.CloseOnLevelLoad) popup.CloseImmediate();
            }
        }

        private void OnPurchaseFailed(string productId, string reason) => ShowNotice(reason);

        /// <summary>Nút Home trong popup Settings lúc đang chơi: hỏi thoát ván.</summary>
        public bool OpenQuitLevel()
        {
            if (quitLevelPopup == null) return false;
            quitLevelPopup.Open();
            return true;
        }

        /// <summary>Nút năng lượng trên Menu.</summary>
        public void OpenEnergyRefill()
        {
            if (refillEnergyPopup == null || refillEnergyPopup.IsOpen) return;
            bool inMenu = _screens != null && _screens.IsMenu;
            if (!inMenu && _levels != null && !_levels.IsPlaying) return; // đang có popup khác
            refillEnergyPopup.ShowOptional();
        }

        /// <summary>
        /// Nút PLAY trên Menu: còn năng lượng thì <paramref name="start"/> ngay; hết thì mở popup nạp (có X để thôi),
        /// có năng lượng thì tự vào chơi.
        /// </summary>
        public void RequestPlay(Action start)
        {
            if (_energy == null || _energy.CanPlay) start?.Invoke();
            else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(start, () => { });
            else start?.Invoke();
        }

        /// <summary>Thua rồi bấm Retry: còn năng lượng thì chơi lại, hết thì chờ / nạp rồi mới chơi lại.</summary>
        private void RetryAfterLose()
        {
            LevelManager lm = _levels;
            if (lm == null) return;
            if (_energy == null || _energy.CanPlay) lm.Retry();
            else if (refillEnergyPopup != null) refillEnergyPopup.ShowRequired(lm.Retry, () => Open(losePopup)); // X -> về lại popup Thua
            else lm.Retry();
        }

        /// <summary>Thắng level cuối: báo "Coming soon", đóng thì chơi lại từ level 1.</summary>
        private void OnAllLevelsCompleted(AllLevelsCompletedEvent e)
        {
            ShowNotice(string.Format(comingSoonMessage, e.LevelCount), comingSoonTitle, () =>
            {
                if (_levels != null) _levels.RestartFromFirst();
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
