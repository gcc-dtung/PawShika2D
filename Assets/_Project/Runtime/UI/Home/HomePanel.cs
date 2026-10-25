using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.LuckyWheel;
using PawBlock.Features.Profile;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Home của Main Menu (HomePanel.prefab): nút PLAY, avatar, Daily, Lucky, coin / năng lượng.
    /// Ghép nhiều feature nên nằm ở Game. GameBootstrap chỉ gọi Init của tab, tab nối tiếp cho view con.
    /// Nút bánh răng (SettingsButton) là widget dùng chung cả HUD nên Bootstrap nối riêng.
    /// </summary>
    public class HomePanel : MenuPanel
    {
        [Header("View con (Editor tự điền)")]
        [SerializeField] private PlayButtonView play;
        [SerializeField] private AvatarButton avatar;
        [SerializeField] private DailyRewardButton dailyReward;
        [SerializeField] private LuckyWheelButton luckyWheel;
        [SerializeField] private CoinHUD coins;
        [SerializeField] private EnergyHUD energy;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(ScreenFlow screens, LevelManager levels, PlayerProfileService profile, ICurrencyWallet stars,
            DailyRewardManager rewards, CoinWallet coinWallet, EnergyManager energyManager, IMenuPopups popups)
        {
            if (play != null) play.Init(screens, levels);
            if (avatar != null) avatar.Init(profile, popups);
            if (dailyReward != null) dailyReward.Init(rewards, popups);
            if (luckyWheel != null) luckyWheel.Init(stars);
            if (coins != null) coins.Init(coinWallet);
            if (energy != null) energy.Init(energyManager, popups);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (play == null) play = GetComponentInChildren<PlayButtonView>(true);
            if (avatar == null) avatar = GetComponentInChildren<AvatarButton>(true);
            if (dailyReward == null) dailyReward = GetComponentInChildren<DailyRewardButton>(true);
            if (luckyWheel == null) luckyWheel = GetComponentInChildren<LuckyWheelButton>(true);
            if (coins == null) coins = GetComponentInChildren<CoinHUD>(true);
            if (energy == null) energy = GetComponentInChildren<EnergyHUD>(true);
        }
#endif
    }
}
