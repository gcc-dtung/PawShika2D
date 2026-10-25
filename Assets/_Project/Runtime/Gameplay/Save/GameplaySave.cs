using System;
using System.Collections.Generic;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Section "gameplay" của save v2: tiến độ level, lượt booster, tutorial / cơ chế đã xem, số liệu từng level.
    /// Thêm field mới: đặt giá trị mặc định ngay tại khai báo (file cũ thiếu field thì giữ mặc định).
    /// </summary>
    [Serializable]
    public class GameplaySave
    {
        public const string Key = "gameplay";

        public int currentLevel;
        public List<BoosterSave> boosters = new List<BoosterSave>();
        public List<string> unlockedBoosters = new List<string>(); // booster đã qua tutorial mở khóa (tên BoosterType)
        public List<string> seenMechanics = new List<string>();    // cơ chế đã xem popup giới thiệu (tên MechanicIntro.Kind)
        public bool tutorialDone;     // đã xong / bỏ qua tutorial lần đầu
        public bool allLevelsCleared; // đã thắng level cuối ít nhất 1 lần (màn "Coming soon")
        public List<LevelStatSave> levelStats = new List<LevelStatSave>(); // số liệu chơi thật từng level (LevelStatsRecorder)

        public bool IsBoosterUnlocked(BoosterType type) => unlockedBoosters.Contains(type.ToString());

        public void UnlockBooster(BoosterType type)
        {
            if (!IsBoosterUnlocked(type))
            {
                unlockedBoosters.Add(type.ToString());
            }
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
            if (b == null)
            {
                boosters.Add(new BoosterSave { type = type.ToString(), count = count });
            }
            else
            {
                b.count = count;
            }
        }
    }
}
