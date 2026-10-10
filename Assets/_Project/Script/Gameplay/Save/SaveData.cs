using System;
using System.Collections.Generic;

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
    public List<BoosterSave> boosters = new List<BoosterSave>();
    public EnergySave energy = new EnergySave();
    public SettingsSave settings = new SettingsSave();
    public DailySave daily = new DailySave();
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

/// <summary>Lưu tên loại (không lưu số enum) để thêm / đổi thứ tự BoosterType không làm lệch dữ liệu.</summary>
[Serializable]
public class BoosterSave
{
    public string type;
    public int count;
}

[Serializable]
public class EnergySave
{
    public int energy = -1;      // -1 = chưa có: đầy
    public long refillStart;     // unix giây
    public long infiniteUntil;   // unix giây
}

[Serializable]
public class SettingsSave
{
    public bool music = true;
    public bool sound = true;
    public bool vibrate = true;
}

/// <summary>Số liệu chơi thật của 1 level (LevelStatsRecorder), khóa theo tên asset level.</summary>
[Serializable]
public class LevelStatSave
{
    public string id;            // tên asset LevelDefinition
    public int level;            // số thứ tự lúc chơi gần nhất
    public int attempts;         // số lượt bắt đầu
    public int wins;
    public int losses;           // thua hẳn (từ chối thêm giờ)
    public int quits;            // bỏ dở (chơi lại / đổi level / về Menu rồi chơi lại)
    public int outOfTime;        // số lần hết giờ (kể cả lần thêm giờ chơi tiếp)
    public int mistakes;         // nước đi sai (khối đặt rồi phải gỡ)
    public int boosters;         // booster đã dùng
    public float playSeconds;    // tổng thời gian ở trạng thái Playing
    public float bestWinSeconds; // lượt thắng nhanh nhất (0 = chưa thắng)
}

/// <summary>Hồ sơ người chơi máy này (PlayerProfileService) + chỉ số (ProfileStatsRecorder).</summary>
[Serializable]
public class ProfileSave
{
    public string playerId;    // 8 số ngẫu nhiên, tạo lần đầu
    public string name;        // tên hiển thị (đổi được)
    public int avatar;         // chỉ số trong AvatarDatabase
    public long createdAt;     // unix giây, lần đầu mở game (từ lúc có hồ sơ)
    public int firstTryWins;
    public long coinsEarned;
    public int levelsCleared;  // level cao nhất đã thắng
    public int hardWins;
    public int boostersUsed;
    public int winStreak;      // chuỗi thắng hiện tại
    public int bestWinStreak;
}

[Serializable]
public class ClockSave
{
    public long lastTrustedUtc; // mốc giờ tin cậy lớn nhất đã thấy (unix giây), TrustedClock chặn lùi giờ dưới mốc này
}

[Serializable]
public class DailySave
{
    public long cycleStart = -1; // ngày bắt đầu chu kỳ điểm danh (số ngày từ 1/1/1970 theo lịch máy), -1 = chưa có
    public int claimedMask;      // bit i = đã nhận quà ngày i + 1
}
