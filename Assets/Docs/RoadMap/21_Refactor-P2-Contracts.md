# 21 – Refactor P2: contracts và event giữa feature

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-12 14:00 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 13:12 (12/10) | Refactor P2 – contracts và event | 6 event + 4 interface trong `Core/Contracts`, phát / cài song song code cũ (chưa đổi nơi gọi); `LevelLoadedEvent` hết phụ thuộc Gameplay; type quà sang Contracts; `SaveSystem` có chế độ bộ nhớ cho test; 23 test mới (127 / 127). Phát hiện `FeelPlayModeTests` ghi save thật |
| 14:00 (12/10) | fix: test Play mode ghi save của máy | `FeelPlayModeTests` sao lưu / trả lại byte `save.json` / `.bak` / `.tmp`; nguyên nhân là `TrustedClock` tự đồng bộ giờ khi vào Play mode. 127 / 127, save không đổi |

---

## 13:12 (12/10) – Refactor P2: contracts và event giữa feature

### Mục tiêu

Giai đoạn P2 của [RefactorPlan.md](../Refactor/RefactorPlan.md): có đủ "kênh" (event / interface trong `Core/Contracts`) để feature nói chuyện với nhau mà không biết nhau. Chỉ thêm định nghĩa + phát / cài đặt song song cách cũ; P4 mới đổi nơi gọi.

### Cách chọn

Rà từng mục trong bảng dự kiến của plan với code thật, giữ quy tắc guide 4.2 "chỉ tạo khi feature khác thật sự nghe / gọi":

- **Tạo:** event có bên nghe ở feature khác (Profile nghe coin, LuckyWheel nghe sao, Profile / DailyChallenge / Energy đang nghe trạng thái rồi hỏi ngược `LevelManager.Instance`...).
- **Không tạo:** `LevelStartedEvent` (trùng `LevelLoadedEvent`), `EnergyChangedEvent` / `BoosterCountChangedEvent` (chỉ feature sở hữu tự nghe), `RefillEnergyRequestedEvent` / `BuyBoosterRequestedEvent` (chỉ Game mở), `ISettingsService` (settings ở `AudioManager` trong Core), `IAdsFreeStatus` (chỉ Shop đọc).
- **Thêm ngoài plan:** `BoosterUsedEvent` (Profile nghe `onBoosterUsed`), `IRewardGranter` (DailyChallenge, LuckyWheel cùng gọi `DailyRewardManager.GrantItem`).

### Thay đổi

**Thêm** (`Core/Contracts`)

| File | Nội dung |
| --- | --- |
| `Events/CoinsChangedEvent`, `StarsBalanceChangedEvent` | `(balance, delta)` |
| `Events/LevelCompletedEvent` | `(number, levelId, isDaily, dailyDay, isHard)`: đủ dữ liệu Profile, DailyChallenge cần |
| `Events/LevelFailedEvent` | `(number, levelId, isDaily)` |
| `Events/BoosterUsedEvent` | `(type)` |
| `Events/ShopOpenRequestedEvent` | Yêu cầu mở Shop |
| `Interfaces/ICurrencyWallet` | `Balance`, `TrySpend`, `Add` – dùng chung cho coin và sao |
| `Interfaces/IBoosterInventory` | `GetCount`, `Add` theo `BoosterType` |
| `Interfaces/IEnergyService` | `ConsumeOne`, `AddInfinite` |
| `Interfaces/IRewardGranter` | `Grant(DailyRewardItem)` |
| `Types/DailyRewardItem`, `DailyRewardKind` | Chuyển từ `DailyRewardDatabase.cs` (3 feature dùng); class `[Serializable]` thường nên dữ liệu asset không đổi |

**Thêm** (khác)

| File | Nội dung |
| --- | --- |
| `Features/Gameplay/Scripts/Intro/LevelIntroContext.cs` | Struct nội bộ Gameplay (số level, `LevelDefinition`, Daily) cho `ILevelIntro` |
| `Features/Economy/Tests/Editor/CurrencyWalletTests.cs` | 15 test: coin + sao qua `ICurrencyWallet` (số dư, cộng, tiêu đủ / thiếu / âm, event, 2 instance chung dữ liệu) |
| `Features/Gameplay/Tests/Editor/BoosterInventoryTests.cs` | 6 test qua `IBoosterInventory` (lượt khởi đầu, cộng, bỏ qua số âm / booster không có) |
| `Features/DailyReward/Tests/Editor/RewardGranterTests.cs` | 2 test trao coin qua `IRewardGranter` |

**Sửa**

| File | Nội dung |
| --- | --- |
| `CoinWallet`, `StarWallet` | `static class` → `sealed class : ICurrencyWallet` (cài tường minh gọi API static cũ, mọi nơi gọi giữ nguyên); phát event số dư |
| `BoosterManager` | `: IBoosterInventory` (`GetCount` / `Add` tìm `BoosterData` trong database rồi gọi `BoosterInventory`); phát `BoosterUsedEvent` |
| `EnergyManager` | `: IEnergyService` (hàm có sẵn) |
| `DailyRewardManager` | `: IRewardGranter` (gọi `GrantItem` static) |
| `LevelManager` | `LevelLoadedEvent` mang `level.name`; phát `LevelCompletedEvent` / `LevelFailedEvent` cùng lúc state Won / Lost (không phát khi tutorial) |
| `LevelLoadedEvent` | Bỏ `LevelDefinition Level` (Contracts phụ thuộc Gameplay), thay `string LevelId` |
| `ILevelIntro`, `LevelIntroSequencer`, `DifficultyIntro`, `MechanicIntro`, `BoosterTutorial` | Nhận `LevelIntroContext` (sequencer ghép từ `GridSpawner.LevelData`, gán trước khi event phát) |
| `LevelStatsRecorder` | Đọc `e.LevelId` |
| `RefillEnergyPopup` | Phát `ShopOpenRequestedEvent` cạnh `ScreenFlow.ShowShop()` |
| `SaveSystem` | `UseInMemoryForTests(data)`: không đọc / ghi file khi test (bỏ ở P3) |
| 5 file DailyReward / DailyChallenge / LuckyWheel | Thêm `using PawBlock.Core.Contracts` cho type quà; bỏ 3 `using` thừa (2 database, `MechanicIntro`) |
| `Docs/Refactor/RefactorPlan.md` | Kết quả P2 (bảng làm / không làm + lý do), estimate thực tế 1.4 h / dự kiến 5 h, tổng 16 % |
| `Docs/Refactor/Dependencies.md` | Đo lại: 86 `using` sai chiều / 55 file (P1: 89 / 57) |
| `Docs/Refactor/ProjectConventions.md` | Mục 6: test đụng save phải dùng save bộ nhớ |

### Lỗi gặp và sửa

- Đổi `LevelLoadedEvent` làm 3 intro (đọc `level.Level` với tên biến khác `e`) lỗi compile CS1061, lần dò đầu bỏ sót → chuyển sang `LevelIntroContext`.
- Script thêm `using` lại thêm nhầm 2 `using` do tên type nằm trong chuỗi (như P1) → bỏ.

### Lỗi có sẵn phát hiện (sửa ở mục 14:00 bên dưới)

Test Play mode ghi `save.json` của máy.

### Kiểm tra

- Compile sạch; 127 / 127 test (104 cũ + 23 mới); 23 test mới chạy riêng không đổi 1 byte `save.json`.
- Dữ liệu quà 3 database đọc đúng sau khi type đổi namespace (10 / 11 / 8 món, khớp YAML); không asset / scene / prefab nào đổi.
- Smoke test Play mode qua MCP, nghe mọi event mới bằng log: vào level 39 (`LevelLoadedEvent` id `Level_086`; băng HARD nhận đúng level qua context), Đóng băng (`BoosterUsedEvent`, nút 28 → 27), cộng / tiêu 1 coin (2 `CoinsChangedEvent`), thắng (`StarsBalanceChangedEvent` +3, `LevelCompletedEvent` hard = true), thua (`LevelFailedEvent`, popup Failed), popup nạp năng lượng → Shop (`ShopOpenRequestedEvent`). Console 0 lỗi. Save trả về bản trước khi test.
- Build Android (IL2CPP) 0 lỗi / 0 cảnh báo.

## 14:00 (12/10) – fix: test Play mode ghi save của máy

**Lỗi:** chạy test (Test Runner) làm đổi `save.json` của máy: `clock.lastTrustedUtc` mỗi lần chạy.

**Nguyên nhân:** `FeelPlayModeTests` đã mở scene trống, nhưng vào Play mode thì `TrustedClock.Boot` (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`) tự đồng bộ giờ server và ghi `SaveSystem.Data.clock` → `SaveRunner` ghi file. `SaveSystem.UseInMemoryForTests` (P2) không giữ qua Play mode vì `ResetStatics` (`SubsystemRegistration`) chạy lại; dự án tắt Domain Reload nên dữ liệu save của Play mode còn trong bộ nhớ sau khi thoát. Ván thắng thấy trong save lúc 13:01 không phải do test (scene trống, không có LevelManager) – nhiều khả năng do chơi tay.

**Sửa:** `FeelPlayModeTests` thêm `[OneTimeSetUp]` đọc byte `save.json` / `.bak` / `.tmp` (ghi nhớ file nào không có), `[OneTimeTearDown]` gọi `SaveSystem.UseInMemoryForTests(null)` (bỏ bản Play mode trong bộ nhớ) rồi ghi trả byte, xóa file test tạo thêm. Không đổi code game.

**Kiểm tra:** md5 `save.json` + `.bak` trước / sau chạy 7 test Play mode và cả bộ 127 test: giống hệt; dữ liệu trong bộ nhớ Editor khớp file (coin, clock), `IsDirty = false`; không còn `.tmp`.

