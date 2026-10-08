# 11 – Lưu dữ liệu người chơi

## Tính năng làm gì

Toàn bộ dữ liệu người chơi nằm trong **1 file JSON** `save.json` ở `Application.persistentDataPath` (Windows: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/TestProject/`).

| Field (`SaveData`) | Ai ghi |
| --- | --- |
| `currentLevel` | `LevelManager.LoadLevel` |
| `coins` | `CoinWallet` |
| `boosters` (tên loại + số lượt), `unlockedBoosters` | `BoosterInventory`, `BoosterTutorial` |
| `energy { energy, refillStart, infiniteUntil }` | `EnergyManager` |
| `settings { music, sound, vibrate }` | `AudioManager` |
| `daily { cycleStart, claimedMask }` | `DailyRewardManager` |
| `noAds` | `ShopService` |
| `tutorialDone`, `seenMechanics` | `TutorialController`, `MechanicIntro` |

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Save/SaveData` | Lớp dữ liệu (JsonUtility: chỉ field public / `[Serializable]`) |
| `Gameplay/Save/SaveSystem` | Static: `Data` (nạp lười), `MarkDirty()`, `Save()`, `Delete()`, `Upgrade` |
| `Gameplay/Save/SaveRunner` | Object ẩn DontDestroyOnLoad: gom thay đổi, ghi sau 0.5 s; luôn ghi khi app vào nền / mất focus / thoát |
| `Editor/Save/SaveDataMenu` | **Tools → PawBlock → Save Data**: Log Save, Open Save Folder, Delete Save |

## Cách dùng trong code

```csharp
SaveSystem.Data.coins += 10;   // sửa trực tiếp
SaveSystem.MarkDirty();        // báo cần ghi (SaveRunner ghi sau 0.5 s; trong Editor ngoài Play thì ghi ngay)
```

Không gọi `Save()` liên tục – `MarkDirty` đã gom lại.

## An toàn dữ liệu

- Ghi: `save.json.tmp` → đổi file cũ thành `.bak` → đổi `.tmp` thành `save.json`. Đọc: file chính → `.tmp` → `.bak`.
- Lần đầu chạy bản dùng JSON: tự chuyển dữ liệu PlayerPrefs cũ rồi xóa key.
- Static được reset khi vào Play (đúng cả khi tắt Domain Reload).

## Thêm dữ liệu cần lưu

Xem [HowTo_MoRong.md → Thêm dữ liệu save](HowTo_MoRong.md#thêm-dữ-liệu-save). Quy tắc ngắn:

1. Thêm field vào `SaveData` **kèm giá trị mặc định ngay tại khai báo** (file cũ không có field thì giữ mặc định).
2. Object lồng nhau mới (`class XxxSave`) → thêm dòng `if (data.xxx == null) data.xxx = new XxxSave();` trong `SaveSystem.Upgrade`.
3. **Đổi ý nghĩa** field cũ → tăng `SaveData.CurrentVersion`, chuyển đổi trong `Upgrade` (`if (data.version < 2) {...}`).
4. Lưu enum bằng **tên** (`ToString()`), không bằng số – như `boosters`, `seenMechanics`.

## Lưu ý khi test

Test chạy `LevelManager.LoadLevel` sẽ đổi `currentLevel` trong save thật. Ghi lại giá trị trước và trả lại sau khi test (hoặc dùng Delete Save).
