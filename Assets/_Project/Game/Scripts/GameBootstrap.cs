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
using PawBlock.Core.Contracts;
using PawBlock.Features.Settings;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Composition root (guide 4.5): nơi duy nhất tạo service và nối feature với nhau, chạy trước mọi script khác.
    /// Thứ tự: save (+ migrate v1 -> v2) -> settings, đồng hồ, quảng cáo, IAP -> ví / kho / hồ sơ -> manager -> Shop -> view -> cheat.
    /// Tham chiếu tới component trong scene do GameBootstrapCollector (Editor) tự điền khi lưu scene / vào Play,
    /// lúc chạy không tìm gì.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Config (asset)")]
        [SerializeField] private GameConfig config;
        [SerializeField] private GameplayVisuals visuals;

        [Header("Scene")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SaveRunner saveRunner;
        [SerializeField] private TrustedClockRunner clockRunner;

        [Header("Gameplay")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private DailyLevelLoader dailyLevelLoader;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private DragAndDropController dragAndDrop;
        [SerializeField] private HintMarker hintMarker;
        [SerializeField] private TutorialHand tutorialHand;
        [SerializeField] private BlockFeedback blockFeedback;
        [SerializeField] private DragFeedback dragFeedback;
        [SerializeField] private GameplayFeedback gameplayFeedback;
        [SerializeField] private GameHapticEvents gameHapticEvents;
        [SerializeField] private BoosterUseFx boosterUseFx;
        [SerializeField] private FreezeOverlayFX freezeOverlayFx;
        [SerializeField] private RevealBanner revealBanner;
        [SerializeField] private FreezeTutorialDemo freezeTutorialDemo;
        [SerializeField] private FreezeButtonCountdown freezeButtonCountdown;
        [SerializeField] private FreezeIndicator freezeIndicator;
        [SerializeField] private TimeStarBar timeStarBar;
        [SerializeField] private TimerUI timerUI;
        [SerializeField] private BoosterManager boosterManager;
        [SerializeField] private BoosterTutorial boosterTutorial;
        [SerializeField] private LevelStatsRecorder levelStatsRecorder;
        [SerializeField] private MechanicIntro mechanicIntro;
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private LevelStars levelStars;
        [SerializeField] private BuyBoosterPopup buyBoosterPopup;
        [SerializeField] private MoreTimePopup moreTimePopup;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private LosePopup losePopup;
        [SerializeField] private QuitLevelPopup quitLevelPopup;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private GridSpawner gridSpawner;
        [SerializeField] private LevelHUD levelHud;
        [SerializeField] private LevelIntroSequencer levelIntroSequencer;
        [SerializeField] private LowTimeVignette lowTimeVignette;
        [SerializeField] private MechanicIntroPopup mechanicIntroPopup;
        [SerializeField] private DifficultyIntro difficultyIntro;
        [SerializeField] private BoosterButton[] boosterButtons;

        [Header("Economy")]
        [SerializeField] private EnergyManager energyManager;
        [SerializeField] private RefillEnergyPopup refillEnergyPopup;

        [Header("Meta")]
        [SerializeField] private DailyRewardManager dailyRewardManager;
        [SerializeField] private DailyChallengeManager dailyChallengeManager;
        [SerializeField] private DailyChallengePanel dailyChallengePanel;
        [SerializeField] private DailyRewardPopup dailyRewardPopup;
        [SerializeField] private LuckyWheelPopup luckyWheelPopup;
        [SerializeField] private ProfilePopup profilePopup;
        [SerializeField] private ProfileStatsRecorder profileStatsRecorder;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private SettingsButton[] settingsButtons;
        [SerializeField] private NoticePopup noticePopup;
        [SerializeField] private PopupView[] popups;

        [Header("Game")]
        [SerializeField] private ScreenFlow screenFlow;
        [SerializeField] private GameFlowUI gameFlowUI;
        [SerializeField] private ShopPanel shopPanel;
        [SerializeField] private HomePanel homePanel;
        [SerializeField] private LeaderboardPanel leaderboardPanel;
        [SerializeField] private MenuTabBar menuTabBar;
        [SerializeField] private GameAudioEvents gameAudioEvents;

        public GameServices Services { get; private set; }

        private void Awake()
        {
            // Core / Foundation
            var save = new JsonFileSaveService(JsonFileSaveService.DefaultPath);
            save.AttachRunner(Require(saveRunner));
            SaveMigrator.Migrate(save);
            AudioManager.InitSettings(save);
            var clock = new TrustedClockService(save, Require(clockRunner));
            clock.RequestSync(true);
            EconomyConfig economy = ConfigOrDefault(config != null ? config.economy : null);
            LivesEnergyConfig lives = ConfigOrDefault(config != null ? config.livesEnergy : null);
            if (visuals == null) Debug.LogError("[GameBootstrap] Thiếu GameplayVisuals (Configs/GameplayVisuals)", this);
            IAdsService ads = new FakeAdsService(economy.adsSimulatedDuration); // chưa có SDK quảng cáo
            IIAPService iap = new FakeIAPService(); // chưa có SDK thanh toán

            // Dữ liệu người chơi do feature sở hữu
            GameServices s = Services = new GameServices(save, clock, ads, iap);
            GameplaySave gameplay = save.Get<GameplaySave>(GameplaySave.Key);
            Func<int> reachedLevel = () => gameplay.allLevelsCleared ? int.MaxValue : gameplay.currentLevel + 1;
            IScreenNavigator screens = Require(screenFlow);

            // Manager có dữ liệu riêng / lộ contract cho feature khác
            Require(energyManager)?.Init(save, clock, lives.maxEnergy, lives.refillSeconds);
            Require(boosterManager)?.Init(save, s.Boosters, s.Coins, gridManager, levelTimer, lives, hintMarker);
            s.Shop = new ShopService(save, s.Coins, boosterManager, energyManager, iap);

            // Gameplay
            Require(levelManager)?.Init(save, gridManager, levelTimer, tutorialController);
            Require(dailyLevelLoader)?.Init(levelManager);
            Require(boosterTutorial)?.Init(save, boosterManager, boosterButtons);
            Require(levelStatsRecorder)?.Init(save, screens, levelManager, gridManager, boosterManager, levelStars);
            Require(mechanicIntro)?.Init(save);
            Require(tutorialController)?.Init(save, levelManager, gridManager);
            Require(levelStars)?.Init(s.Stars, levelManager, lives, levelTimer, gridSpawner);
            Require(buyBoosterPopup)?.Init(s.Coins, levelManager, boosterManager);
            Require(moreTimePopup)?.Init(s.Coins, ads, levelManager, economy);
            Require(winPopup)?.Init(s.Coins, ads, screens, levelManager, levelStars, economy);
            Require(losePopup)?.Init(screens, levelManager);
            Require(quitLevelPopup)?.Init(screens, levelManager, gridManager, energyManager);
            Require(levelTimer)?.Init(screens, levelManager, gridManager, lives);
            Require(gridSpawner)?.Init(levelManager);
            Require(levelHud)?.Init(levelManager);
            Require(levelIntroSequencer)?.Init(levelManager, gridManager, tutorialController, gridSpawner, new ILevelIntro[] { boosterTutorial, mechanicIntro, difficultyIntro });
            Require(lowTimeVignette)?.Init(levelManager, levelTimer);
            Require(mechanicIntroPopup)?.Init(levelManager);
            Require(dragAndDrop)?.Init(gridManager, boosterManager, gridSpawner);
            Require(hintMarker)?.Init(gridManager, visuals, gridSpawner, tutorialHand);
            Require(tutorialHand)?.Init(gridManager);
            Require(blockFeedback)?.Init(gridManager, levelTimer, gridSpawner);
            Require(dragFeedback)?.Init(gridManager, gridSpawner);
            Require(gameplayFeedback)?.Init(gridManager, visuals, gridSpawner, Require(mainCamera));
            Require(gameHapticEvents)?.Init(gridManager, boosterManager, levelTimer);
            Require(boosterUseFx)?.Init(boosterManager, boosterButtons, timerUI, mainCamera);
            Require(freezeOverlayFx)?.Init(boosterManager, levelTimer);
            Require(revealBanner)?.Init(boosterManager);
            Require(gridManager)?.Init(levelTimer, visuals);
            Require(freezeTutorialDemo)?.Init(levelTimer, lives);
            Require(freezeButtonCountdown)?.Init(levelTimer, lives);
            Require(freezeIndicator)?.Init(levelTimer);
            Require(timeStarBar)?.Init(levelTimer, levelStars, lives);
            Require(timerUI)?.Init(levelTimer);
            foreach (BoosterButton b in boosterButtons) Require(b)?.Init(s.Boosters, boosterManager, levelTimer);

            // Economy
            Require(refillEnergyPopup)?.Init(s.Coins, ads, levelManager, energyManager, economy.refillAdsEnergy, economy.refillCoinCost);

            // Meta: chỉ nhận contract của feature khác
            Require(dailyRewardManager)?.Init(save, s.Coins, boosterManager, energyManager, clock, ads, screens, boosterManager, gameFlowUI);
            Require(dailyChallengeManager)?.Init(save, dailyRewardManager, reachedLevel, clock, dailyLevelLoader);
            Require(dailyChallengePanel)?.Init(ads, screens, dailyRewardManager, dailyChallengeManager);
            Require(dailyRewardPopup)?.Init(dailyRewardManager);
            Require(luckyWheelPopup)?.Init(s.Stars, dailyRewardManager, dailyRewardManager);
            Require(profilePopup)?.Init(s.Profile, screens, levelManager);
            Require(profileStatsRecorder)?.Init(s.Profile);
            Require(settingsPopup)?.Init(screens, levelManager, gameFlowUI);
            Require(noticePopup)?.Init(screens, levelManager);
            foreach (SettingsButton b in settingsButtons) Require(b)?.Init(gameFlowUI);

            // Game
            Require(gameFlowUI)?.Init(s.Profile, iap, screenFlow, levelManager, boosterManager, energyManager, popups);
            Require(screenFlow)?.Init(levelManager, gridManager, tutorialController);
            // Main Menu: mỗi tab tự nối view con của nó
            Require(shopPanel)?.Init(s.Shop, s.Coins, energyManager, gameFlowUI);
            Require(homePanel)?.Init(screenFlow, levelManager, s.Profile, s.Stars, dailyRewardManager, s.Coins, energyManager, gameFlowUI);
            Require(leaderboardPanel)?.Init(s.Profile);
            Require(menuTabBar)?.Init(dailyChallengeManager);
            Require(gameAudioEvents)?.Init(screenFlow, levelManager, gridManager, boosterManager, levelTimer, gridSpawner);
            DebugCheats.Init(s, new GameScene { Screens = screenFlow, Levels = levelManager, Grid = gridManager, Boosters = boosterManager, Timer = levelTimer, Energy = energyManager, DailyReward = dailyRewardManager, LivesEnergy = lives, MoreTime = moreTimePopup });
        }

        private void OnDestroy()
        {
            (Services?.Ads as IDisposable)?.Dispose(); // hủy quảng cáo giả lập đang chờ
        }

        /// <summary>Config con thiếu (GameConfig chưa gán) thì dùng giá trị mặc định trong code để game vẫn chạy.</summary>
        private T ConfigOrDefault<T>(T value) where T : ScriptableObject
        {
            if (value != null)
            {
                return value;
            }
            Debug.LogError($"[GameBootstrap] Thiếu {typeof(T).Name} trong GameConfig (Tools/PawBlock/Builders/Setup Game Config): dùng mặc định", this);
            return ScriptableObject.CreateInstance<T>();
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
