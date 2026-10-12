using System;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.LuckyWheel;
using PawBlock.Features.Profile;
using PawBlock.Features.Shop;

namespace PawBlock.Game
{
    /// <summary>
    /// Composition root (guide 4.5): nơi duy nhất tạo service và nối feature với nhau, chạy trước mọi script khác.
    /// Thứ tự: save (+ migrate v1 -> v2) -> settings, đồng hồ -> ví / kho / hồ sơ -> manager -> Shop -> view -> cheat.
    /// Tham chiếu tới component trong scene do GameBootstrapCollector (Editor) tự điền khi lưu scene / vào Play,
    /// lúc chạy không tìm gì.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private BoosterManager boosterManager;
        [SerializeField] private BoosterTutorial boosterTutorial;
        [SerializeField] private LevelStatsRecorder levelStatsRecorder;
        [SerializeField] private MechanicIntro mechanicIntro;
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private LevelStars levelStars;
        [SerializeField] private BuyBoosterPopup buyBoosterPopup;
        [SerializeField] private MoreTimePopup moreTimePopup;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private BoosterButton[] boosterButtons;

        [Header("Economy")]
        [SerializeField] private EnergyManager energyManager;
        [SerializeField] private RefillEnergyPopup refillEnergyPopup;
        [SerializeField] private CoinHUD[] coinHuds;

        [Header("Meta")]
        [SerializeField] private ShopCardView[] shopCards;
        [SerializeField] private DailyRewardManager dailyRewardManager;
        [SerializeField] private DailyChallengeManager dailyChallengeManager;
        [SerializeField] private LuckyWheelButton luckyWheelButton;
        [SerializeField] private LuckyWheelPopup luckyWheelPopup;
        [SerializeField] private AvatarButton avatarButton;
        [SerializeField] private ProfilePopup profilePopup;
        [SerializeField] private ProfileStatsRecorder profileStatsRecorder;

        [Header("Game")]
        [SerializeField] private GameFlowUI gameFlowUI;

        public GameServices Services { get; private set; }

        private void Awake()
        {
            // Core / Foundation
            var save = new JsonFileSaveService(JsonFileSaveService.DefaultPath);
            SaveMigrator.Migrate(save);
            AudioManager.InitSettings(save);
            TrustedClock.Init(save);

            // Dữ liệu người chơi do feature sở hữu
            GameServices s = Services = new GameServices(save);
            GameplaySave gameplay = save.Get<GameplaySave>(GameplaySave.Key);
            Func<int> reachedLevel = () => gameplay.allLevelsCleared ? int.MaxValue : gameplay.currentLevel + 1;

            // Manager có dữ liệu riêng / lộ contract cho feature khác
            Require(energyManager)?.Init(save);
            Require(boosterManager)?.Init(save, s.Boosters, s.Coins);
            s.Shop = new ShopService(save, s.Coins, boosterManager, energyManager);

            // Gameplay
            Require(levelManager)?.Init(save);
            Require(boosterTutorial)?.Init(save);
            Require(levelStatsRecorder)?.Init(save);
            Require(mechanicIntro)?.Init(save);
            Require(tutorialController)?.Init(save);
            Require(levelStars)?.Init(s.Stars);
            Require(buyBoosterPopup)?.Init(s.Coins);
            Require(moreTimePopup)?.Init(s.Coins);
            Require(winPopup)?.Init(s.Coins);
            foreach (BoosterButton b in boosterButtons) Require(b)?.Init(s.Boosters);

            // Economy
            Require(refillEnergyPopup)?.Init(s.Coins);
            foreach (CoinHUD hud in coinHuds) Require(hud)?.Init(s.Coins);

            // Meta: chỉ nhận contract của feature khác
            foreach (ShopCardView card in shopCards) Require(card)?.Init(s.Shop);
            Require(dailyRewardManager)?.Init(save, s.Coins, boosterManager, energyManager);
            Require(dailyChallengeManager)?.Init(save, dailyRewardManager, reachedLevel);
            Require(luckyWheelButton)?.Init(s.Stars);
            Require(luckyWheelPopup)?.Init(s.Stars, dailyRewardManager);
            Require(avatarButton)?.Init(s.Profile);
            Require(profilePopup)?.Init(s.Profile);
            Require(profileStatsRecorder)?.Init(s.Profile);

            // Game
            Require(gameFlowUI)?.Init(s.Profile);
            DebugCheats.Init(s);
        }

        /// <summary>Thiếu thì báo lỗi và trả về null thật (object Unity đã hủy là "null giả", ?. không chặn được).</summary>
        private T Require<T>(T component) where T : Component
        {
            if (component == null)
            {
                Debug.LogError($"[GameBootstrap] Thiếu tham chiếu {typeof(T).Name}: lưu scene (Ctrl+S) để tự điền lại", this);
                return null;
            }
            return component;
        }
    }
}
