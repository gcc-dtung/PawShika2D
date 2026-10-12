using System;
using System.Collections.Generic;
using PawBlock.Core.Contracts;
using PawBlock.Features.DailyChallenge;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;
using PawBlock.Features.Profile;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Toàn bộ dữ liệu người chơi, lưu thành 1 file JSON (SaveSystem). Chỉ field public / [Serializable] (JsonUtility).
    /// Thêm field mới: đặt giá trị mặc định ngay tại khai báo (file cũ thiếu field thì giữ mặc định);
    /// đổi ý nghĩa field cũ: tăng CurrentVersion và chuyển đổi trong SaveSystem.Upgrade.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
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

        public bool IsBoosterUnlocked(BoosterType type) => unlockedBoosters.Contains(type.ToString());

        public void UnlockBooster(BoosterType type)
        {
            if (!IsBoosterUnlocked(type)) unlockedBoosters.Add(type.ToString());
        }

        /// <summary>Số lượt booster; chưa có (lần đầu) thì -1 để BoosterInventory tặng lượt khởi đầu.</summary>
        public int GetBooster(BoosterType type)
        {
            BoosterSave b = boosters.Find(x => x.type == type.ToString());
            return b != null ? b.count : -1;
        }

        public void SetBooster(BoosterType type, int count)
        {
            BoosterSave b = boosters.Find(x => x.type == type.ToString());
            if (b == null) boosters.Add(new BoosterSave { type = type.ToString(), count = count });
            else b.count = count;
        }
    }
}
