# 11 – Lưu dữ liệu người chơi

> Hạ tầng ở `Assets/_Project/Core/Foundation/Save/`; mỗi feature giữ class section của mình; chuyển save cũ ở `Game/Scripts/Save/`. Quy tắc: [ProjectConventions](../Refactor/ProjectConventions.md) mục 8.

## Tính năng làm gì

Toàn bộ dữ liệu người chơi nằm trong **1 file JSON** `save.json` ở `Application.persistentDataPath` (Windows: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/PawShika/`). Từ P3 là **save v2**: cấp ngoài có `version`, `savedAt` và **mỗi feature 1 section** (key + class `[Serializable]` của feature đó):

```json
{ "version": 2, "savedAt": 1791900000, "economy": { ... }, "gameplay": { ... }, "shop": { ... }, ... }
```

| Key | Class (module) | Nội dung | Ai ghi |
| --- | --- | --- | --- |
| `"economy"` | `EconomySave` (Economy) | `coins`, `stars`, `energy { energy, refillStart, infiniteUntil }` | `CoinWallet`, `StarWallet`, `EnergyManager` |
| `"gameplay"` | `GameplaySave` (Gameplay) | `currentLevel`, `boosters` (tên loại + số lượt), `unlockedBoosters`, `seenMechanics`, `tutorialDone`, `allLevelsCleared`, `levelStats` | `LevelManager`, `BoosterInventory`, `BoosterTutorial`, `MechanicIntro`, `TutorialController`, `LevelStatsRecorder` |
| `"shop"` | `ShopSave` (Shop) | `noAds` | `ShopService` |
| `"dailyReward"` | `DailyRewardSave` (DailyReward) | `cycleStart`, `claimedMask` | `DailyRewardManager` |
| `"dailyChallenge"` | `DailyChallengeSave` (DailyChallenge) | `startDay`, `doneDays`, `claimedGifts` | `DailyChallengeManager` |
| `"profile"` | `ProfileSave` (Profile) | ID, tên, avatar, ngày bắt đầu, chỉ số | `PlayerProfileService`, `ProfileStatsRecorder` |
| `"settings"` | `SettingsSave` (Foundation) | `music`, `sound`, `vibrate` | `AudioManager` |
| `"clock"` | `ClockSave` (Foundation) | `lastTrustedUtc` | `TrustedClockService` |

## File liên quan

| File | Việc |
| --- | --- |
| `Core/Foundation/Save/ISaveService` | `Get<T>(key)` (object sống, cùng instance mỗi lần), `MarkDirty()`, `SaveNow()` |
| `Core/Foundation/Save/JsonFileSaveService` | Cài đặt: đọc / ghi file, tách section bằng `JsonSections`, section chưa đọc giữ nguyên chữ (feature cũ / mới không làm mất dữ liệu của nhau) |
| `Core/Foundation/Save/SaveRunner` | Đặt sẵn trong scene (cùng `GameBootstrap`): gom thay đổi, ghi sau 0.5 s; luôn ghi khi app vào nền / mất focus / thoát |
| `Game/Scripts/Save/SaveMigrator`, `SaveDataV1` | Chuyển save v1 (1 class phẳng) / PlayerPrefs đời trước sang v2, giữ `save.v1.bak` |
| `Core/Foundation/Editor/SaveDataMenu` | **Tools → PawBlock → Save Data**: Log Save, Open Save Folder, Delete Save |

## Cách dùng trong code

```csharp
// Feature nhận ISaveService qua Init (GameBootstrap tạo 1 JsonFileSaveService)
private EconomySave Data => _save.Get<EconomySave>(EconomySave.Key);

Data.coins += 10;   // sửa trực tiếp object sống
_save.MarkDirty();  // báo cần ghi (SaveRunner ghi sau 0.5 s; trong Editor ngoài Play thì ghi ngay)
```

Không gọi `SaveNow()` liên tục – `MarkDirty` đã gom lại. Feature chỉ đọc section của mình; cần dữ liệu feature khác thì qua contract (vd. Profile nhận `Func<int>` level hiện tại).

## An toàn dữ liệu

- Ghi: `save.json.tmp` → đổi file cũ thành `.bak` → đổi `.tmp` thành `save.json`. Đọc: file chính → `.tmp` → `.bak`.
- Mở game: `GameBootstrap` đọc save rồi `SaveMigrator.Migrate` (v1 → v2, hoặc PlayerPrefs → v2) **trước** khi feature nào đọc. Bản v1 giữ ở `save.v1.bak`. **Nhánh trước P3 không đọc được save v2.**
- Test dùng save trong bộ nhớ, không đụng file thật.

## Thêm dữ liệu cần lưu

Xem [HowTo_MoRong.md → Thêm dữ liệu save](HowTo_MoRong.md#thêm-dữ-liệu-save). Quy tắc ngắn:

1. Field mới của feature có sẵn: thêm vào class section của feature **kèm giá trị mặc định ngay tại khai báo** (file cũ thiếu field thì giữ mặc định).
2. Feature mới: tạo `class XxxSave` `[Serializable]` có `public const string Key = "xxx"`, đọc bằng `save.Get<XxxSave>(XxxSave.Key)`. Không cần sửa file chung.
3. **Đổi ý nghĩa** field cũ → tăng `JsonFileSaveService.CurrentVersion`, thêm bước chuyển trong `SaveMigrator` + test (`Game/Tests/Editor/SaveMigratorTests`, 3 save mẫu ở `Docs/Refactor/SaveSamples`).
4. Lưu enum bằng **tên** (`ToString()`), không bằng số – như `boosters`, `seenMechanics`.

## Lưu ý khi test

Play trong Editor đọc / ghi save thật của máy. Muốn thử với save mẫu: sao lưu thư mục save trước, chép mẫu từ `Docs/Refactor/SaveSamples`, thử xong trả lại (hoặc Delete Save).
