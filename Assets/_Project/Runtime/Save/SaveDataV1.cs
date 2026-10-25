using System;
using System.Collections.Generic;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Profile;

namespace PawBlock.Game
{
    /// <summary>
    /// Save v1 (1 class phẳng chứa mọi feature) – chỉ để SaveMigrator đọc file đời cũ rồi chia sang section v2.
    /// Không dùng cho dữ liệu mới. Class con (EnergySave, ProfileSave...) là class section hiện tại cùng tên field,
    /// đổi chúng thì chạy lại SaveMigratorTests (3 file mẫu v1).
    /// </summary>
    [Serializable]
    public class SaveDataV1
    {
        public int version = 1;
        public long savedAt; // unix giây, lần ghi cuối

        public int currentLevel;
        public int coins;
        public int stars; // ví sao: cộng số sao mỗi lần thắng (StarWallet), tiêu ở Vòng quay may mắn
        public List<BoosterSave> boosters = new List<BoosterSave>();
        public EnergySave energy = new EnergySave();
        public SettingsSave settings = new SettingsSave();
        public DailyRewardSave daily = new DailyRewardSave();
        public DailyChallengeSave challenge = new DailyChallengeSave();
        public ClockSave clock = new ClockSave();
        public ProfileSave profile = new ProfileSave();
        public bool noAds; // đã mua Remove Ads / No Ads Bundle
        public bool tutorialDone; // đã xong / bỏ qua tutorial lần đầu
        public bool allLevelsCleared; // đã thắng level cuối ít nhất 1 lần (màn "Coming soon")
        public List<string> unlockedBoosters = new List<string>(); // booster đã qua tutorial mở khóa (tên BoosterType)

        public List<string> seenMechanics = new List<string>(); // cơ chế đã xem popup giới thiệu (tên MechanicIntro.Kind)

        public List<LevelStatSave> levelStats = new List<LevelStatSave>(); // số liệu chơi thật từng level (LevelStatsRecorder)

        public void SetBooster(BoosterType type, int count)
        {
            BoosterSave b = boosters.Find(x => x.type == type.ToString());
            if (b == null) boosters.Add(new BoosterSave { type = type.ToString(), count = count });
            else b.count = count;
        }
    }
}
