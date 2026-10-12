# 22 – Refactor P3: service, Bootstrap, save theo phần

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | 5 commit `6c7b77a` … `8daa9a9` (P3.1 – P3.4 + tài liệu), cuối "Refactor P3: tài liệu P3, chốt RoadMap 21" (nhánh `Refactor-P3-ServiceSave`) |
| Thời gian push | 2026-10-12 16:39 |
| Cập nhật lần cuối | 2026-10-12 16:39 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 14:20 (12/10) | P3.1 – `ISaveService`, `JsonFileSaveService` | Save v2 chia section theo key, giữ ghi an toàn `.tmp` / `.bak`, section chưa nạp giữ nguyên văn, chỉ bộ nhớ cho test. 20 test |
| 14:45 (12/10) | P3.2 – section theo feature, `SaveMigrator` | 8 section (`economy`, `shop`, `gameplay`, `dailyReward`, `dailyChallenge`, `profile`, `settings`, `clock`); v1 → v2 + `save.v1.bak` + PlayerPrefs cũ. 11 test với 3 save mẫu; bản sao save thật khớp 16 / 16 field |
| 15:10 (12/10) | P3.3 – `GameBootstrap`, save v2 chạy thật | Bootstrap + `GameServices` + Editor tự điền tham chiếu; ví / kho / hồ sơ / Shop bỏ static; 20 file đọc section qua `Init`; xóa `SaveSystem`. Play với 3 save mẫu + không có file đều đúng |
| 15:25 (12/10) | P3.4 – quảng cáo, IAP, đồng hồ qua interface | `IAdsService` / `IIAPService` / `IClock` + bản Fake / instance; xóa `RewardedAds`, `IAPService`, `TrustedClock` static. 160 / 160 test, build Android |

---

## Cảnh báo: save v2 và nhánh cũ

- Lần **Play đầu tiên** trên nhánh này sẽ chuyển `save.json` của máy sang **v2** (bản v1 giữ ở `save.v1.bak` cùng thư mục).
- **Nhánh cũ** (`Day-7`, `main`, `Refactor` trước P3...) **không đọc được v2**: `SaveSystem` cũ sẽ coi như save trống (level 1, 0 coin) và ghi đè. Muốn quay lại nhánh cũ: đổi tên `save.v1.bak` thành `save.json` trước khi Play.
- Lúc làm P3, save thật của máy dev được trả về **bản v1 gốc** sau mỗi lần thử (md5 khớp).

## P3.1 – `ISaveService`, `JsonFileSaveService`

**Thêm** (`Core/Foundation/Save`)

| File | Nội dung |
| --- | --- |
| `JsonSections` | Tách / ghép object JSON cấp ngoài theo key (hiểu chuỗi có escape, object / mảng lồng); JsonUtility không đọc được Dictionary |
| `ISaveService` | `Get<T>(key)` (object sống, cùng instance), `MarkDirty()`, `SaveNow()` |
| `JsonFileSaveService` | File `{ "version": 2, "savedAt": ..., "<key>": { ... } }`; đọc `save.json` → `.tmp` → `.bak`; ghi `.tmp` → giữ `.bak` → đổi tên; section chưa `Get` ghi lại nguyên văn; `path = null` chỉ bộ nhớ; file đời cũ để migrator đọc (`FileVersion`, `LoadedText`) và **không cho ghi khi chưa migrate** (tránh mất section) |
| `SaveRunner` | Gom ghi cho service (`Ensure(service)`) |
| `Tests/Editor/JsonFileSaveServiceTests` | 22 test (tách JSON hợp lệ / lỗi, đọc ghi, giữ section, `.bak`, file hỏng, v1 không ghi đè, chỉ bộ nhớ, xóa) |

## P3.2 – section theo feature, `SaveMigrator`

- Class save về feature sở hữu, mỗi class `const string Key`: `EconomySave` (coins, stars, energy), `ShopSave` (noAds), `GameplaySave` (level, booster, tutorial, cơ chế, levelStats + hàm booster), `DailyRewardSave` (đổi tên từ `DailySave`), `DailyChallengeSave`, `ProfileSave`, `SettingsSave` (Core/Audio), `ClockSave` (Core/Clock), `EnergySave`, `BoosterSave`, `LevelStatSave`.
- `Game/Scripts/Save/SaveMigrator`: v1 → 8 section, giữ `save.v1.bak`, chuyển PlayerPrefs của bản trước nữa (xóa key khi ghi file xong).
- `Game/Tests/Editor/SaveMigratorTests`: 11 test – mọi field v1 của 3 file mẫu sang đúng section (so văn bản JSON; số thực so theo float32 vì file mẫu do Python ghi), mọi field v1 phải có chỗ đến, `save.v1.bak` giữ nguyên byte, v2 không chạy lại, file rất cũ thiếu object lồng.

## P3.3 – `GameBootstrap`, save v2 chạy thật

**Thêm**

| File | Nội dung |
| --- | --- |
| `Game/Scripts/GameBootstrap` | `[DefaultExecutionOrder(-1000)]`: save + migrate → settings, đồng hồ → `GameServices` → `Init` từng component (manager → Shop → Gameplay → Economy → meta → Game → cheat). Thiếu tham chiếu thì `LogError` |
| `Game/Scripts/GameServices` | Ví coin / sao, kho booster, hồ sơ, Shop (+ đồng hồ, quảng cáo, IAP từ P3.4) |
| `Game/Editor/GameBootstrapCollector` | Điền mọi field `Component` / `Component[]` của Bootstrap bằng component trong scene (kể cả object tắt) khi lưu scene / vào Play; menu `Tools/PawBlock/Game/Collect Bootstrap References` |
| `SampleScene` | GameObject `GameBootstrap` (23 tham chiếu, sau P3.4 là 24) |

**Sửa**

- `CoinWallet`, `StarWallet`, `BoosterInventory`, `PlayerProfileService`, `ShopService`: static → instance nhận `ISaveService`; `ShopService` chỉ dùng contract (ví, kho, năng lượng).
- `Init(...)` cho `LevelManager`, `BoosterManager`, `BoosterTutorial`, `LevelStatsRecorder`, `MechanicIntro`, `TutorialController`, `LevelStars`, `BuyBoosterPopup`, `MoreTimePopup`, `WinPopup`, `BoosterButton`, `EnergyManager`, `RefillEnergyPopup`, `CoinHUD`, `ShopCardView`, `DailyRewardManager`, `DailyChallengeManager`, `LuckyWheelButton`, `LuckyWheelPopup`, `AvatarButton`, `ProfilePopup`, `ProfileStatsRecorder`, `GameFlowUI`; `AudioManager.InitSettings`, `DebugCheats.Init`.
- `ProfileStatsRecorder` nghe `LevelCompletedEvent` (thêm `Attempts`), `LevelFailedEvent`, `CoinsChangedEvent`, `BoosterUsedEvent` thay vì hỏi `LevelManager` / `BoosterManager` / đọc save Gameplay. `LuckyWheel*` nghe `StarsBalanceChangedEvent`, trao quà qua `IRewardGranter`. `GameAudioEvents` nghe `CoinsChangedEvent`.
- `PlayerProfileService`, `DailyChallengeManager` nhận level hiện tại qua `Func<int>` do Game truyền (không đọc section Gameplay).
- `DailyRewardManager.GrantItem` thành hàm instance dùng contract.
- Công cụ Editor (`LevelEditorWindow`, `LevelStatsExporter` – đọc được cả save v1 lẫn v2 khi gộp, `SaveDataMenu`) mở `JsonFileSaveService` riêng ngoài Play mode.
- Test (`CurrencyWalletTests`, `BoosterInventoryTests`, `RewardGranterTests`) dùng save chỉ bộ nhớ; `FeelPlayModeTests` giữ thêm `save.v1.bak`.

**Xóa:** `SaveSystem`; `SaveData` → `Game/Scripts/Save/SaveDataV1` (chỉ để migrate).

## P3.4 – quảng cáo, IAP, đồng hồ

- `IAdsService` + `FakeAdsService` (chờ `adsSimulatedDuration` giây rồi thành công, như `RewardedAds`), `IIAPService` + `FakeIAPService` (luôn thất bại), `IClock` + `TrustedClockService` (instance, luật giữ nguyên; `TrustedClockRunner` tách file).
- Gỡ component `RewardedAds` khỏi `Manager` trong `SampleScene`; `PopupBuilder` không gắn nữa. Foundation hết phụ thuộc `GameConfig` (Game).
- Xóa `RewardedAds`, `IAPService`, `TrustedClock`.

## Kiểm tra

- 160 / 160 test EditMode; compile sạch; `SampleScene` + 7 prefab 0 Missing Script; build Android 0 lỗi.
- Play với `save_v1_played.json`: 2135 coin, 7 sao, level 22 (nút PLAY "Level 23"), Hint 15, hồ sơ 39 level / chuỗi 22, daily, challenge, nhạc tắt, 16 số liệu level đúng; file thành v2 + `save.v1.bak`. Thắng (+3 sao, chuỗi 23), Continue (+35 coin, `coinsEarned` +35), Freeze 28 → 27, Lucky (−10 sao, +50 coin qua `IRewardGranter`), tắt rung → thoát → mở lại: giữ nguyên.
- `save_v1_new.json`: 0 coin, tutorial lần đầu chạy. `save_v1_infinite_energy.json`: vô hạn, trừ năng lượng không tác dụng. Không có file: tạo v2 mặc định, tutorial, hồ sơ mới.
- Quảng cáo +30 s (Time's up → chơi tiếp), x2 màn thắng (+40 coin); IAP lỗi hiện popup "Purchase Failed!"; đồng bộ giờ server qua google.com. Console 0 lỗi.
- `using` sai chiều 86 → 75.

## Ghi chú

- Có sẵn, chưa sửa: `EnergyManager.InfiniteSecondsLeft` ép `(int)` tràn khi hạn vô hạn quá xa (mẫu tới năm 2100 hiện "00:00").
- Static còn lại: `EventBus`, `InputGate` (chủ đích), `DebugCheats` (debug), cờ settings `AudioManager` và các Singleton (P4).
