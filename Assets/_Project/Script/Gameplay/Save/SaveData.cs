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
    public bool noAds; // đã mua Remove Ads / No Ads Bundle

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

[Serializable]
public class DailySave
{
    public long cycleStart = -1; // ngày bắt đầu chu kỳ điểm danh (số ngày từ 1/1/1970 theo lịch máy), -1 = chưa có
    public int claimedMask;      // bit i = đã nhận quà ngày i + 1
}
