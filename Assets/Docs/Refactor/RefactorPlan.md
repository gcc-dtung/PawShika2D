# Refactor Plan – PawBlock

> Kế hoạch đưa dự án hiện tại về đúng [UnityCodingGuidelines.md](UnityCodingGuidelines.md).
> Liệt kê những gì cần **thêm / sửa / xóa / di chuyển**, chia thành các giai đoạn nhỏ; **sau mỗi giai đoạn game vẫn chạy được**.

| | |
| --- | --- |
| Trạng thái | P0, P1, P2 xong (2026-10-12), tiếp P3 |
| Unity | 2022.3.62f3 |
| Phạm vi | Toàn bộ `Assets/_Project/Script` (199 file), `SampleScene`, prefab, asset |
| Cập nhật lần cuối | 2026-10-12 |

---

## Mục lục

1. [Mục tiêu và nguyên tắc thực hiện](#1-mục-tiêu-và-nguyên-tắc-thực-hiện)
2. [Hiện trạng](#2-hiện-trạng)
3. [Kiến trúc đích và bảng ánh xạ](#3-kiến-trúc-đích-và-bảng-ánh-xạ)
4. [Các giai đoạn](#4-các-giai-đoạn)
5. [Estimate](#5-estimate)
6. [Smoke test sau mỗi giai đoạn](#6-smoke-test-sau-mỗi-giai-đoạn)
7. [Rủi ro chung](#7-rủi-ro-chung)

---

## 1. Mục tiêu và nguyên tắc thực hiện

### Mục tiêu

- Code chia theo module `Core (Foundation + Contracts) ← Shared ← Features ← Game` (guide mục 4.1).
- Feature không biết nhau; giao tiếp qua event / contract trong `Core/Contracts` (guide 4.2).
- Không Singleton cho game logic; không cờ static; không `Find` / `GetComponent` / `AddComponent` lúc chạy (guide 3, 8.3).
- UI là prefab; `PopupView` đúng guide 10.6.
- Asset gọn, có Sprite Atlas (guide 9).
- **Hành vi game không đổi.** Người chơi không nhận ra khác biệt, save cũ đọc được.

### Nguyên tắc thực hiện

| # | Nguyên tắc |
| --- | --- |
| 1 | **Mỗi giai đoạn = một nhánh `refactor/<pN>-<tên>` = một lần push.** Merge xong mới sang giai đoạn sau |
| 2 | **Không trộn refactor với tính năng mới / sửa bug** trong cùng commit. Gặp bug → ghi lại, sửa ở commit `fix:` riêng |
| 3 | **Game chạy được sau mỗi giai đoạn**: compile sạch, test xanh, smoke test (mục 6) qua |
| 4 | **Tạo / di chuyển / đổi tên file trong Unity Editor** (không ngoài Explorer) để `.meta` giữ GUID, scene / prefab không mất tham chiếu |
| 5 | **Format (`{}` cho `if` một dòng, access modifier...) chỉ áp cho file đang sửa**, không format hàng loạt |
| 6 | Mỗi giai đoạn ghi RoadMap (một file / push, quy ước `Docs/RoadMap/00_README.md`) |
| 7 | Sửa script có `[SerializeField]` mới → **kéo lại tham chiếu** trong scene / prefab ngay trong giai đoạn đó, chạy thử trước khi commit |
| 8 | Ngoại lệ riêng của dự án ghi vào `Docs/Refactor/ProjectConventions.md`, không sửa guide chung |

---

## 2. Hiện trạng

Số liệu đo ngày 2026-10-12.

| Hạng mục | Hiện trạng | Vi phạm guide |
| --- | --- | --- |
| Singleton game logic (`Singleton<T>`) | `LevelManager` (101 lần gọi), `GridManager` (69), `BoosterManager` (48), `ScreenFlow` (41), `LevelTimer` (41), `EnergyManager` (36), `DailyRewardManager` (18), `LevelStars` (7), `DailyChallengeManager` (6), `TutorialController`, `RewardedAds`, `AudioManager` | 3 |
| Singleton tự nạp từ `Resources` | `GameConfig.Instance`, `GameplayVisuals.Instance` | 3, 9.3 |
| Static class giữ trạng thái | `SaveSystem` (21 file dùng), `CoinWallet` (12), `InputGate` (7), `StarWallet` (5), `BoosterInventory` (5), `PlayerProfileService` (5), `TrustedClock` (5), `IAPService` (3), `ShopService`, `DebugCheats` | 2 (D), 4.2 |
| Cờ static cho nơi khác đọc | `TutorialController.Active` / `Pending`, `ShopService.NoAds`, `DebugCheats.InMenu` / `TimerPaused` | 4.4 |
| Phụ thuộc ngược Gameplay → UI | `ScreenFlow.Instance` được gọi từ `Gameplay/Analytics`, `Audio`, `Cheats`, `Daily`, `Timer` | 4.1 |
| Tìm / lấy / gắn component lúc chạy | `GetComponent` 40 file (67 chỗ), `FindObjectOfType…` 20 file (24), `AddComponent` 14 file (18), `Camera.main` 6 file, `transform.Find` 3 file, `TryGetComponent` 2 file | 8.3 |
| Namespace / `.asmdef` | 0 / 0 (mọi thứ trong `Assembly-CSharp`) | 4.1, 5 |
| Cấu trúc thư mục | Chia `Gameplay/` (24 thư mục, lẫn cả meta: Daily, Economy, Profile, LuckyWheel...) và `UI/` theo loại | 5 |
| UI | Toàn bộ popup / menu / HUD nằm thẳng trong `SampleScene.unity` (2,9 MB, ~940 GameObject). 7 prefab, chỉ gameplay. 15 builder `Editor/UI` dựng thẳng vào scene (chỉ `WinScreenBuilder` xuất prefab) | 10 |
| `PopupView` | 12 popup kế thừa (tốt). Nhưng: `GetComponent<CanvasGroup>` trong `Awake` / `Open`; biết `LevelLoadedEvent`; thiếu `Closed` / `OnClosed`; chưa `abstract` | 8.3, 10.6 |
| `SaveData` | Một class chứa dữ liệu của **mọi** feature (coins, stars, boosters, energy, daily, challenge, clock, profile, settings, noAds, tutorial, levelStats...) | 4.1 |
| Định dạng | ~1.566 câu `if` một dòng không `{}` trong 169 file | 7.1 (chỉ sửa khi đụng file) |
| Test | 10 file / 46 test trong `Editor/Tests` | 13 (vị trí, asmdef) |
| Asset | `GameAssets` 63 MB, `ShikaCat_GameAssets` 91 MB, `Texture2D` 47 MB; 0 Sprite Atlas; `Configs/` trùng `GameAssets/10_Data_Configs` | 5, 9 |

**Điểm đã tốt, giữ nguyên:** `EventBus<T>`, `InputGate`, `ILevelIntro` + `LevelIntroSequencer`, Booster dùng Strategy + Factory, tách `…Model` / `…View` ở gameplay, Object Pool cho khối / chữ khen, quy tắc DOTween, 46 test logic.

---

## 3. Kiến trúc đích và bảng ánh xạ

### 3.1 Cây thư mục đích

```text
Assets/_Project/
├── Core/
│   ├── Foundation/        PawBlock.Core.Foundation.asmdef
│   └── Contracts/         PawBlock.Core.Contracts.asmdef   (Events/, Interfaces/, Types/)
├── Shared/                PawBlock.Shared.asmdef
├── Features/
│   ├── Gameplay/          PawBlock.Gameplay.asmdef
│   ├── Economy/  Shop/  DailyReward/  DailyChallenge/  LuckyWheel/  Profile/  Settings/
│   └── PawBlock.Meta.asmdef          (chung cho mọi feature meta)
├── Game/                  PawBlock.Game.asmdef
├── Scenes/  Settings/  Resources/
ThirdParty/  Plugins/  Docs/
```

Namespace gốc: `PawBlock` (`PawBlock.Core.Foundation`, `PawBlock.Core.Contracts`, `PawBlock.Shared`, `PawBlock.Features.Gameplay`, `PawBlock.Features.Shop`, `PawBlock.Game`...).

### 3.2 Ánh xạ script cũ → mới

| Đích | Lấy từ (đường dẫn cũ trong `Script/`) |
| --- | --- |
| **Core/Foundation** | `Gameplay/Events/EventBus`; `Gameplay/Save/*` (SaveSystem, SaveRunner; `SaveData` tách ở P3); `Gameplay/Audio/AudioManager`, `AudioDatabase`; `Gameplay/Clock/*`; `Gameplay/Feedback/Haptics`, `FeelCurves`, `ProceduralSprites`; `Gameplay/Util/*` (FrameRateSetup, TweenSetup, TextColor, Singleton); `Gameplay/Input/InputGate`; `Gameplay/Economy/RewardedAds`, `IAPService` (thành service + bản Fake ở P3); `Editor/Save/SaveDataMenu` |
| **Core/Contracts** | `Gameplay/Events/GameEvents` (tách 1 struct / file); `Gameplay/Level/GameState`; `BoosterType` (đang nằm trong `BoosterDatabase.cs`); `Gameplay/Audio/SoundId`; interface mới ở P2 |
| **Shared** | `UI/Common/*` trừ `SettingsButton` (SafeAreaZone, BackgroundCover, UIButtonSounds, NoClickSound, UIButtonPress, ButtonPress, ButtonPressIgnore, NestedScrollRect, TMPLetterPopIn, UIConfetti, UIGrayscaleWhenDisabled); `UI/Popups/PopupView`, `NoticePopup`; `Gameplay/Visuals/BackgroundFitter`; `Editor/UI/GameFontSetup`, `UIZoneBuilder`; `Editor/Visuals/BackgroundPatternBuilder` |
| **Features/Gameplay** | `Gameplay/Core/*` → `Board/`; `Gameplay/Block/*`; `Gameplay/Booster/*` (kèm `Effects/`, `BoosterInventory`); `Gameplay/Input/*` trừ InputGate; `Gameplay/Intro/*`; `Gameplay/Level/*` (kèm `Generator/`, DifficultyTheme); `Gameplay/Solver/*`; `Gameplay/Timer/*`; `Gameplay/Stars/LevelStars`, `StarRating`; `Gameplay/Tutorial/*`; `Gameplay/Feedback/BlockFeedback`, `DragFeedback`, `GameplayFeedback`, `GameHapticEvents`; `Gameplay/Visuals/GameplayVisuals`, `BoardCameraFitter`, `PlayZone`; `Gameplay/Analytics/LevelStatsRecorder`; `UI/HUD/*`; `UI/Popups/WinPopup`, `LosePopup`, `MoreTimePopup`, `QuitLevelPopup`, `MechanicIntroPopup`, `BuyBoosterPopup`; `Editor/Level/*`; `Editor/UI/BoosterBuilder`, `DifficultyBuilder`, `StarsBuilder`, `TutorialBuilder`, `WinScreenBuilder`; `Editor/Visuals/GameplayVisualsSetup`; test: HiddenClue, LockClue, SelectionHint, HintBooster, LevelGenerator, StarRating, FeelPlayMode |
| **Features/Economy** | `Gameplay/Economy/CoinWallet`; `Gameplay/Stars/StarWallet`; `Gameplay/Config/EconomyConfig`, `LivesEnergyConfig`; `Gameplay/Energy/*`; `UI/Menu/CoinHUD`; `UI/HUD/EnergyHUD`; `UI/Popups/RefillEnergyPopup` |
| **Features/Shop** | `Gameplay/Economy/ShopDatabase`, `ShopService`; `UI/Menu/ShopCardView`; `Editor/UI/ShopBuilder` |
| **Features/DailyReward** | `Gameplay/Daily/DailyReward*` (Database, Model, Manager); `UI/Popups/DailyRewardPopup`, `DailyRewardSlot`; `UI/Menu/DailyRewardButton`; `Editor/UI/DailyRewardBuilder` |
| **Features/DailyChallenge** | `Gameplay/Daily/DailyChallenge*`; `UI/Menu/DailyChallengePanel`, `DailyDayCell`, `DailyGiftSlot`, `DailyGiftBubble`, `DailyTabBadge`; `Editor/UI/DailyChallengeBuilder`; test DailyChallengeModel |
| **Features/LuckyWheel** | `Gameplay/LuckyWheel/*`; `UI/Popups/LuckyWheelPopup`; `UI/Menu/LuckyWheelButton`; `Editor/UI/LuckyWheelBuilder`; test LuckyWheel |
| **Features/Profile** | `Gameplay/Profile/*`; `UI/Popups/ProfilePopup`, `ProfileStatTile`; `UI/Menu/AvatarButton`; `Editor/UI/ProfileBuilder` |
| **Features/Settings** | `UI/Popups/SettingsPopup`, `SettingToggleView`; `UI/Common/SettingsButton`; `Editor/UI/SettingsBuilder` |
| **Game** | `UI/Menu/ScreenFlow`, `MainMenuNavigator`, `MenuSwipe`, `MenuTabButton`, `MenuShortcutButton`, `PlayButtonView`; `UI/Popups/GameFlowUI`; `Gameplay/Audio/GameAudioEvents`; `Gameplay/Config/GameConfig`; `Gameplay/Cheats/*`; `Editor/Config/GameConfigSetup`, `Editor/Debug/DebugPanelWindow`, `Editor/Audio/AudioSetup`, `Editor/UI/MainMenuBuilder`; **mới:** `GameBootstrap` |
| **Tách ra nhiều nơi** | `Editor/UI/PopupBuilder` (dựng nhiều popup của nhiều feature) → tách thành builder của từng feature ở P7; `FeelCurvesTests` → `Core/Foundation/Tests` |

> Lý do `GameAudioEvents`, `Cheats`, `GameConfig` nằm ở **Game**: chúng nghe / chạm vào nhiều feature cùng lúc, chỉ Game được biết mọi feature.

---

## 4. Các giai đoạn

| GĐ | Tên | Thay đổi hành vi? | Phụ thuộc |
| --- | --- | --- | --- |
| P0 | Chuẩn bị, mốc so sánh | Không | — |
| P1 | Thư mục + namespace | Không | P0 |
| P2 | Contracts và event giữa feature | Không | P1 |
| P3 | Service hạ tầng, Bootstrap, Save theo phần + migrate | Không (save chuyển định dạng) | P2 |
| P4 | Bỏ Singleton game logic, cờ static | Không | P3 |
| P5 | Bỏ Find / GetComponent / AddComponent lúc chạy | Không | P4 |
| P6 | Assembly Definition | Không | P5 |
| P7 | UI thành prefab, sửa PopupView | Không | P6 |
| P8 | Asset, Sprite Atlas, tối ưu | Không (ảnh giữ nguyên) | P7 |
| P9 | Tài liệu, dọn dẹp | Không | P8 |

---

### P0 – Chuẩn bị và mốc so sánh

**Mục tiêu:** có điểm so sánh trước / sau và công cụ tự kiểm tra; chưa đổi code game.

**Thêm**
- [x] Nhánh từ `Day-7` sau khi push xong việc đang dở. Dùng nhánh `Refactor` (Windows không phân biệt hoa thường: đã có `Refactor` thì không tạo được `refactor/...`); các giai đoạn sau: `Refactor-P1-...`.
- [x] `.editorconfig` ở gốc repo: Allman, bắt buộc `{}`, access modifier tường minh, quy tắc đặt tên (guide 6.1, 7.1). Mức **warning**, không error (vì code cũ chưa sửa).
- [x] Package `Microsoft.Unity.Analyzers` (qua NuGet / IDE): không thêm DLL vào project; Rider có sẵn inspection Unity, VS Code dùng bản kèm extension Unity (xem `ProjectConventions.md` mục 5).
- [x] `Docs/Refactor/ProjectConventions.md`: ngoại lệ riêng dự án:
  - Quy tắc DOTween hiện có (giữ tween trong field, `SetLink`, `Kill` trong `OnDisable`, `OnKill` chỉ xóa field nếu đúng tween).
  - Comment tiếng Việt.
  - Format chỉ áp cho file đang sửa.
  - Menu Editor giữ `Tools/PawBlock/…`.
- [x] `Docs/Refactor/Baseline.md`: ghi mốc:
  - Kết quả test (hiện 104 ca, không phải 46).
  - Số **Batches / SetPass** (Frame Debugger) ở 3 màn: Menu, màn chơi level 10x10, popup Win.
  - **Build Report** (Android): tổng dung lượng, top 20 asset nặng.
  - Danh sách smoke test (mục 6) đã chạy qua (P0 chạy được một phần qua MCP, phần còn lại chạy tay).
- [x] Sao lưu 3 file `save.json` mẫu (người mới; đã chơi ~30 level có booster / daily / profile; có năng lượng vô hạn đang chạy) vào `Docs/Refactor/SaveSamples/` để test migrate ở P3.

**Kiểm tra / Xong khi:** Baseline.md có đủ số liệu; IDE hiện cảnh báo analyzer; không file game nào đổi.

---

### P1 – Thư mục và namespace

**Mục tiêu:** code nằm đúng chỗ theo bảng 3.2, có namespace. **Không đổi một dòng logic.**

**Thêm**
- [x] Cây thư mục 3.1 (chỉ thư mục có nội dung). `Booster/Effects`, `Level/Generator` gộp lên 1 cấp (tối đa 4 tầng).
- [x] `Core/Contracts/Events/`: tách `GameEvents.cs` thành 1 struct / file (`LevelLoadedEvent`, `GameStateChangedEvent`, `ScreenChangedEvent`, `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent`, `StarsChangedEvent`...) + `IGameEvent`.
- [x] `Core/Contracts/Types/BoosterType.cs` (tách khỏi `BoosterDatabase.cs`), `GameState.cs`, `SoundId.cs`.

**Di chuyển** (trong Unity, theo bảng 3.2)
- [x] Toàn bộ `Script/Gameplay`, `Script/UI`, `Script/Editor` vào module đích (198 file bằng `AssetDatabase.MoveAsset`, GUID giữ nguyên 198 / 198). Thư mục `Editor/` của từng module giữ tên `Editor` (Unity cần để không đưa vào build trước khi có asmdef).
- [x] `Editor/Tests/*` → `<module>/Tests/Editor/`.

**Sửa**
- [x] Mỗi file: bọc `namespace PawBlock.<Module>[.<Feature>] { … }` dạng block, thêm `using` cần thiết (207 file, 257 type). **Không** sửa format ở P1 (đụng mọi file → thành format hàng loạt); xem `ProjectConventions.md` mục 7.
- [x] Grep tham chiếu theo **tên type dạng chuỗi**: `Type.GetType`, `GetType().Name`, `"PawBlock…"`, tên class trong `MenuItem`, JSON / SerializeReference (hiện dùng `JsonUtility` cho `SaveData` → không lưu tên type, an toàn).

**Xóa**
- [x] Thư mục cũ `Script/Gameplay`, `Script/UI`, `Script/Editor` khi đã trống (xóa cả `Script/`).

**Rủi ro:** scene / prefab mất script (Missing Script) nếu di chuyển ngoài Unity hoặc tên class ≠ tên file.
**Kiểm tra:** compile sạch; 46 test xanh; mở `SampleScene` + 7 prefab không có Missing Script; smoke test.
**Xong khi:** không còn file trong `Script/`; mọi file có namespace.

**Kết quả P1:** compile sạch; 104 / 104 test; scene 0 Missing Script (1 368 MonoBehaviour), 7 prefab 0, 365 ScriptableObject đọc được; không UnityEvent / `SerializeReference` nào lưu tên type của dự án; smoke test qua MCP (Menu, chơi, đặt khối, Đóng băng, hết giờ, thua, chơi lại, thắng, Shop, Settings, Profile, Daily Reward, nạp năng lượng, tab Daily, Lucky Wheel) không lỗi Console. Phụ thuộc sai chiều lộ ra: [Dependencies.md](Dependencies.md) (89 `using` / 57 file).

---

### P2 – Contracts và event giữa feature

**Mục tiêu:** có đủ "kênh" để feature nói chuyện với nhau mà không biết nhau (guide 4.2). Chỉ thêm định nghĩa + phát / nghe song song cách cũ; **chưa bỏ** cách gọi cũ.

**Thêm – Event** (`Core/Contracts/Events/`), chỉ cái nào có bên nghe ở feature khác:

| Event | Phát từ | Nghe ở |
| --- | --- | --- |
| `LevelStartedEvent(level, isDaily)` | Gameplay | Profile, Analytics, Game (audio) |
| `LevelCompletedEvent(level, stars, timeLeft, isDaily)` | Gameplay | Economy (thưởng coin / sao), Profile, DailyChallenge, Game |
| `LevelFailedEvent(level, reason)` | Gameplay | Profile, Game |
| `CoinsChangedEvent(balance, delta)` | Economy | CoinHUD, Shop, Game (audio) |
| `StarsBalanceChangedEvent(balance)` | Economy | LuckyWheel |
| `EnergyChangedEvent(value, isInfinite)` | Economy | Game (PlayButton), Gameplay |
| `BoosterCountChangedEvent(type, count)` | Gameplay | Shop, HUD |
| `ShopOpenRequestedEvent(section)` | bất kỳ | Game |
| `RefillEnergyRequestedEvent` | Game / Gameplay | Economy |
| `BuyBoosterRequestedEvent(type)` | Gameplay | Game / Shop |

**Thêm – Interface** (`Core/Contracts/Interfaces/`):

| Interface | Cài đặt bởi | Dùng bởi |
| --- | --- | --- |
| `ICurrencyWallet` (`Balance`, `TrySpend`, `Add`) | Economy (`CoinWallet`) | Shop, Gameplay (mua booster, thêm giờ), DailyReward, LuckyWheel |
| `IStarWallet` | Economy (`StarWallet`) | LuckyWheel |
| `IBoosterInventory` (`GetCount`, `Add`, `TryConsume`) | Gameplay (`BoosterInventory`) | Shop, DailyReward, LuckyWheel |
| `IEnergyService` (`Current`, `IsInfinite`, `TryConsume`, `AddInfiniteMinutes`) | Economy (`EnergyManager`) | Game, Shop, DailyReward |
| `ISettingsService` (`Music`, `Sound`, `Vibrate`) | Settings | Core/Audio, Haptics (qua Game) |
| `IAdsFreeStatus` (`NoAds`) | Shop | Game (quảng cáo) |

> Danh sách là **dự kiến**. Khi làm, interface nào không có feature thứ hai gọi thì **không tạo** (guide 4.2).

**Sửa**
- [x] Nơi phát: thêm `EventBus<…>.Raise(...)` cạnh code cũ.
- [x] Các class "sở hữu" cài interface (chưa đổi nơi gọi).

**Kiểm tra:** compile; test; smoke test; thêm unit test cho `CoinWallet` / `BoosterInventory` qua interface.
**Xong khi:** mọi event / interface trong bảng có ít nhất 1 nơi phát / cài đặt; `Core/Contracts` không có logic.

**Kết quả P2** (rà từng mục với code thật, guide 4.2: chỉ tạo khi feature khác dùng):

| Mục | Làm | Phát / cài đặt ở | Bên nghe / gọi ở feature khác (P4 chuyển sang) |
| --- | --- | --- | --- |
| `CoinsChangedEvent(balance, delta)` | Có | `CoinWallet.Set` | Profile (`coinsEarned`), Game (âm coin) |
| `StarsBalanceChangedEvent(balance, delta)` | Có | `StarWallet.Set` | LuckyWheel (nút, popup) |
| `LevelCompletedEvent(number, levelId, isDaily, dailyDay, isHard)` | Có | `LevelManager.SetState(Won)`, không phát khi tutorial | Profile, DailyChallenge |
| `LevelFailedEvent(number, levelId, isDaily)` | Có | `LevelManager.SetState(Lost)` | Economy (trừ năng lượng), Profile |
| `BoosterUsedEvent(type)` | Có (mới, plan chưa có) | `BoosterManager.Consume` | Profile (`BoosterManager.onBoosterUsed`) |
| `ShopOpenRequestedEvent` | Có | `RefillEnergyPopup.GoShop` | Game (`ScreenFlow.ShowShop`) |
| `LevelStartedEvent` | Không | — | Trùng `LevelLoadedEvent` đã có |
| `EnergyChangedEvent`, `BoosterCountChangedEvent` | Không | — | Chỉ feature sở hữu tự nghe |
| `RefillEnergyRequestedEvent`, `BuyBoosterRequestedEvent` | Không | — | Chỉ Game mở 2 popup này |
| `ICurrencyWallet` (`Balance`, `TrySpend`, `Add`) | Có, dùng chung cho coin và sao (thay `IStarWallet`) | `CoinWallet`, `StarWallet` (`static class` → `sealed class`, cài tường minh; API static giữ tới P3) | Gameplay, Shop, DailyReward, LuckyWheel |
| `IBoosterInventory` (`GetCount`, `Add` theo `BoosterType`) | Có | `BoosterManager` (giữ `BoosterDatabase`; `BoosterInventory` là static theo `BoosterData`) | Shop, DailyReward |
| `IEnergyService` (`ConsumeOne`, `AddInfinite`) | Có | `EnergyManager` | Gameplay (`QuitLevelPopup`), Shop, DailyReward |
| `IRewardGranter` (`Grant(DailyRewardItem)`) | Có (mới, plan chưa có) | `DailyRewardManager` (gọi `GrantItem` static) | DailyChallenge (quà mốc), LuckyWheel (ô trúng) |
| `ISettingsService`, `IAdsFreeStatus` | Không | — | Settings nằm ở `AudioManager` (Core) mà feature được gọi; NoAds chỉ Shop đọc |

- `LevelLoadedEvent`: bỏ field `LevelDefinition Level` (Contracts phụ thuộc Gameplay), thay `string LevelId`. Intro đầu level (`ILevelIntro`) nhận struct nội bộ Gameplay `LevelIntroContext` (số, `LevelDefinition`, Daily) do `LevelIntroSequencer` ghép từ `GridSpawner.LevelData`.
- `SaveSystem.UseInMemoryForTests(data)`: test chạy trên save trong bộ nhớ, không ghi `save.json`; bỏ khi có `ISaveService` (P3).
- Test mới: `CurrencyWalletTests` 15, `BoosterInventoryTests` 6, `RewardGranterTests` 2. Smoke test Play mode: mọi event mới phát đúng dữ liệu, không lỗi Console.
- `using` sai chiều: 89 → 86 (hết `Core.Contracts → Features.Gameplay`; 2 database không còn cần DailyReward), xem [Dependencies.md](Dependencies.md).
- **Lỗi có sẵn phát hiện (chưa sửa, làm commit `fix:` riêng):** `FeelPlayModeTests` vào Play mode với `SampleScene` đang mở → game chạy thật và **ghi `save.json` của máy** (chắc chắn `clock.lastTrustedUtc` mỗi lần chạy; 1 lần thấy thêm 1 ván thắng nhưng không loại trừ được do chơi tay). Cách sửa: test tự mở scene rỗng, hoặc dùng save bộ nhớ (lưu ý `ResetStatics` khi vào Play mode).
- Thêm `IRewardGranter` + chuyển `DailyRewardItem` / `DailyRewardKind` sang `Core/Contracts/Types` (DailyChallenge, LuckyWheel cùng gọi `DailyRewardManager.GrantItem`); dữ liệu quà trong 3 database đọc đúng (10 / 11 / 8 món), asset không đổi. Test: 23 mới → 127 / 127.

---

### P3 – Service hạ tầng, Bootstrap, Save theo phần

**Mục tiêu:** bỏ static giữ trạng thái, có một chỗ nối mọi thứ (`GameBootstrap`), mỗi feature sở hữu phần save của mình.

**Thêm**
- [ ] `Core/Foundation/Save/ISaveService` + `JsonFileSaveService`: một file `save.json`, bên trong là các **section theo key** (`"economy"`, `"gameplay"`, `"profile"`...). API: `Load<T>(key)`, `Save<T>(key, data)`, `MarkDirty()`. Giữ cơ chế an toàn hiện có của `SaveSystem` (ghi `.tmp`, giữ `.bak`, gom ghi qua `SaveRunner`, ghi khi app vào nền).
- [ ] **Migrate v1 → v2** (`SaveMigrator`): đọc `save.json` cũ (có `version = 1`, các field phẳng), chia vào section tương ứng, ghi `version = 2`, giữ file cũ thành `save.v1.bak`. Giữ luôn bước chuyển từ PlayerPrefs cũ đang có.
- [ ] Mỗi feature có class save riêng `[Serializable]`: `EconomySave` (coins, stars, energy, noAds), `GameplaySave` (currentLevel, boosters, unlockedBoosters, seenMechanics, tutorialDone, allLevelsCleared, levelStats), `DailyRewardSave`, `DailyChallengeSave`, `ProfileSave`, `SettingsSave`, `ClockSave` (Foundation).
- [ ] `Core/Foundation/Services`: `IAdsService` + `FakeAdsService` (từ `RewardedAds`: chờ 0.5 s rồi thành công), `IIAPService` + `FakeIAPService` (từ `IAPService`: luôn thất bại), `IClock` + `TrustedClockService` (từ `TrustedClock`).
- [ ] `Game/GameBootstrap` (`[DefaultExecutionOrder(-1000)]`): tạo service → tạo model / service của feature → `Init(...)` theo thứ tự rõ ràng (guide 4.5). Dài quá 150 dòng → tách `GameplayInstaller`, `MetaInstaller`.

**Sửa**
- [ ] `CoinWallet`, `StarWallet`, `BoosterInventory`, `PlayerProfileService`, `ShopService`: static → class thường, cài interface P2, nhận `ISaveService` qua constructor. Nơi gọi `CoinWallet.Add(...)` → dùng instance được `Init` truyền vào (12 + 5 + 5 + 5 file).
- [ ] 21 file đang gọi `SaveSystem.Data.xxx` → đọc / ghi qua phần save của feature mình.
- [ ] `TrustedClock` (5 file), `RewardedAds` (1), `IAPService` (3) → qua interface.

**Xóa**
- [ ] `SaveData` dạng phẳng (sau khi migrate chạy đúng), `SaveSystem` static, `RewardedAds`, `IAPService` static.

**Rủi ro:** mất dữ liệu người chơi nếu migrate sai → **test migrate trước khi xóa code cũ**.
**Kiểm tra:**
- Unit test `SaveMigrator` với 3 file mẫu P0: mọi giá trị sang đúng section.
- Chạy game với từng file mẫu: coin, level, booster, năng lượng, daily, hồ sơ giữ nguyên.
- Người mới (không có file): tạo save v2 mặc định.
- Smoke test.

**Xong khi:** không còn static giữ trạng thái ngoài hạ tầng có chủ đích (`EventBus`, `InputGate`); Bootstrap là nơi duy nhất tạo service.

---

### P4 – Bỏ Singleton game logic và cờ static

**Mục tiêu:** không còn `.Instance` cho game logic (guide 3, 4.2, 4.4).

**Cách thay (áp cho mọi Singleton):**

| Nơi gọi | Thay bằng |
| --- | --- |
| Cùng feature (VD Gameplay gọi `GridManager`) | `[SerializeField]` kéo trong Inspector, hoặc `Init(...)` |
| Feature khác chỉ cần **biết đã xảy ra** | Event P2 |
| Feature khác cần **hỏi / ra lệnh** | Contract interface P2, Bootstrap truyền vào |
| UI / Game cần object của feature | Bootstrap / Inspector |

**Thứ tự** (mỗi mục 1 commit, chạy smoke test sau mỗi mục):
1. [ ] `ScreenFlow` (41) – trước tiên vì đang bị Gameplay gọi ngược: Gameplay nghe `ScreenChangedEvent` thay cho `ScreenFlow.Instance.IsMenu`.
2. [ ] `LevelManager` (101; 11 file ở popup) – popup màn chơi nằm cùng feature Gameplay → `[SerializeField]`; Profile / Daily / Analytics → event.
3. [ ] `GridManager` (69).
4. [ ] `BoosterManager` (48) – HUD cùng feature → `[SerializeField]`; Shop / Daily / Profile → `IBoosterInventory` + event.
5. [ ] `LevelTimer` (41) – 7 file HUD → `[SerializeField]`.
6. [ ] `EnergyManager` (36) → `IEnergyService` + `EnergyChangedEvent`.
7. [ ] `DailyRewardManager` (18), `DailyChallengeManager` (6).
8. [ ] `LevelStars` (7).
9. [ ] `TutorialController` + bỏ `static Active` / `Pending` → thuộc tính instance; nơi khác hỏi qua tham chiếu (cùng Gameplay) hoặc event.
10. [ ] `GameConfig.Instance`, `GameplayVisuals.Instance`: bỏ `Resources.Load`; Bootstrap giữ tham chiếu `[SerializeField]` và truyền config con cho từng feature.
11. [ ] Cờ static: `ShopService.NoAds` → `IAdsFreeStatus`; `DebugCheats.InMenu` / `TimerPaused` → Cheats (ở Game) giữ tham chiếu.

**Giữ lại (hạ tầng):** `AudioManager` → đổi sang Singleton bản an toàn (guide 3: không tự tạo object, `LogError` khi thiếu, xóa trong `OnDestroy`).

**Sửa:** `Core/Foundation/Util/Singleton<T>` → bỏ nhánh `FindObjectOfType` / tự tạo GameObject.

**Kiểm tra:** grep `\.Instance` trong `Features/` và `Game/` chỉ còn `AudioManager`; smoke test đầy đủ; test xanh.
**Xong khi:** 12 Singleton game logic + 2 config `.Instance` + 4 cờ static đã bỏ.

---

### P5 – Bỏ Find / GetComponent / AddComponent lúc chạy

**Mục tiêu:** đúng guide 8.3 (~58 file).

| Loại | Số | Cách sửa |
| --- | --- | --- |
| `GetComponent` cùng object | 40 file / 67 chỗ | `[SerializeField]` + tự điền trong `Reset()`, kiểm tra trong `OnValidate()` (`#if UNITY_EDITOR`). Gán cho mọi instance trong scene / prefab |
| `FindObjectOfType…` | 20 file / 24 chỗ | Phần lớn đã hết sau P4 (do `Singleton<T>` dùng Find); còn lại → Inspector / Bootstrap |
| `AddComponent` | 14 file / 18 chỗ | Component có sẵn trong prefab / scene, bật / tắt. VD: `AudioManager` tạo `AudioSource` → pool `AudioSource` sẵn trong prefab; `SaveRunner` / `TrustedClock` tự tạo runner → object có sẵn trong scene Boot / Bootstrap; `CheatPanel` dựng UI bằng code → prefab |
| `Camera.main` | 6 file / 7 chỗ | `[SerializeField] Camera` |
| `transform.Find` | 3 file / 4 chỗ | `[SerializeField]` trỏ object con |
| Raycast → `GetComponent` / `TryGetComponent` | 2 file | Registry `Dictionary<Collider2D, CellView>` trong `GridSpawner` (đăng ký khi spawn, gỡ khi trả pool) |

**Kiểm tra:** grep các API trên ngoài `Editor/`, `Tests/` chỉ còn trong `Reset` / `OnValidate`; Console không có `NullReferenceException`; smoke test.
**Xong khi:** grep sạch.

---

### P6 – Assembly Definition

**Mục tiêu:** compiler tự chặn phụ thuộc sai (guide 4.1).

**Thêm**
- [ ] DOTween: *Tools > Demigiant > DOTween Utility Panel > Create ASMDEF* để có assembly `DOTween.Modules`.
- [ ] Runtime: `PawBlock.Core.Foundation`, `PawBlock.Core.Contracts`, `PawBlock.Shared`, `PawBlock.Gameplay`, `PawBlock.Meta`, `PawBlock.Game` (reference theo bảng guide 4.1; thêm `Unity.TextMeshPro`, `DOTween.Modules`, `spine-unity` nơi cần).
- [ ] Editor-only cho **mỗi** thư mục `Editor/`: `PawBlock.<Module>.Editor`.
- [ ] Test: `PawBlock.<Module>.UnitTests` (Editor only), `…IntegrationTests` cho `FeelPlayModeTests`.

**Sửa:** mọi lỗi compile xuất hiện = phụ thuộc sai còn sót → sửa bằng event / contract (P2) hoặc chuyển type về Contracts. **Không** thêm reference ngược để "cho chạy".

**Kiểm tra:** compile sạch; Test Runner thấy đủ 46+ test; build Android thành công (bắt lỗi `using UnityEditor` lọt vào runtime).
**Xong khi:** `Assembly-CSharp` không còn script nào của dự án.

---

### P7 – UI thành prefab, sửa PopupView

**Mục tiêu:** đúng guide 10.

**Sửa – `PopupView`** (`Shared`)
- [ ] `[SerializeField] CanvasGroup group` + `Reset()`, bỏ `GetComponent`.
- [ ] Bỏ nghe `LevelLoadedEvent` khỏi base → `GameFlowUI` (Game) nghe và đóng popup đang mở.
- [ ] Thêm `event Action<PopupView> Closed`, hook `OnOpened()` / `OnClosed()`; lớp con dùng hook thay vì override `Open` / `Close`.
- [ ] Đánh dấu `abstract`.

**Thêm – prefab** (theo guide 10.1, 10.8)

| Prefab | Đặt ở |
| --- | --- |
| Widget: nút chính / phụ / đóng, ô coin, ô sao, toggle, tab, badge, `PopupFrame` | `Shared/Prefabs/Widgets/` |
| Popup màn chơi: Win, Lose, MoreTime, QuitLevel, MechanicIntro, BuyBooster; HUD | `Features/Gameplay/Prefabs/` |
| RefillEnergy; Shop + ShopCard; DailyReward + Slot; DailyChallengePanel + DayCell + GiftSlot; LuckyWheel; Profile + StatTile; Settings | `Features/<Tên>/Prefabs/` |
| MainMenu (ghép các tab), màn chơi tổng | `Game/Prefabs/` |
| Notice popup | `Shared/Prefabs/` |

- [ ] Instance đặt sẵn trong scene, tắt; mở thì bật (guide 10.4).
- [ ] Canvas con cho mỗi popup / màn hình; Canvas riêng cho `TimerUI`, `TimeStarBar`, `LowTimeVignette`.
- [ ] Tắt Raycast Target trên ảnh / chữ không bấm.

**Sửa – builder**
- [ ] 15 builder trong `Editor/UI` (đã chia về feature ở P1): xuất prefab bằng `PrefabUtility.SaveAsPrefabAsset` (như `WinScreenBuilder`), không dựng vào scene. `PopupBuilder` tách thành builder của từng feature.
- [ ] Builder không còn cần → xóa (sau khi prefab đã lưu và chỉnh tay).

**Kiểm tra:** mở từng prefab trong Prefab Mode không có tham chiếu `None` ngoài field "gán từ ngoài"; `SampleScene` nhỏ đi rõ rệt; smoke test mọi popup; số Batches ≤ mốc P0.
**Xong khi:** scene chỉ còn khung + instance prefab.

---

### P8 – Asset, Sprite Atlas, tối ưu

**Mục tiêu:** đúng guide 5, 9.

**Di chuyển**
- [ ] Bản gốc chưa sửa: `GameAssets/`, `ShikaCat_GameAssets/`, `Texture2D/` → `ThirdParty/PawBlock/`, `ThirdParty/ShikaCat/` (giữ cấu trúc gốc).
- [ ] Asset **đang được tham chiếu** → `Art/`, `Audio/` của feature dùng nó; dùng ≥ 2 feature → `Shared/Art`, `Shared/Audio`. (Dùng *Select Dependencies* trên scene / prefab để lấy danh sách.)
- [ ] `Levels/`, `Configs/` → `Features/Gameplay/Configs/Levels/`, `Configs/` của feature tương ứng.

**Thêm**
- [ ] Sprite Atlas: `Shared` (nút, khung, icon coin / sao / tim), `Gameplay` (ô, khối, khóa, mũi tên, chữ khen), mỗi feature meta một atlas. UI: tắt Allow Rotation, Tight Packing; Padding 4; tắt mipmap; max 2048.
- [ ] Preset import texture theo thư mục (ASTC 6x6, UI 4x4, background 8x8; tắt Read/Write, mipmap).

**Xóa**
- [ ] `GameAssets/10_Data_Configs` (trùng `Configs/`) sau khi xác nhận không còn tham chiếu.
- [ ] Asset trong `ThirdParty` không dùng: **không xóa** (giữ bản gốc), build cũng không đưa vào.

**Sửa:** `Resources/` chỉ còn thứ bắt buộc (DOTweenSettings, TMP). `GameConfig` / `GameplayVisuals` đã ra khỏi `Resources` ở P4.

**Kiểm tra:** Frame Debugger: Batches giảm so với P0 ở 3 màn; Build Report: dung lượng ≤ P0; nhìn bằng mắt không lỗi viền / mờ; chạy trên máy Android thật.
**Xong khi:** 0 asset dự án nằm ngoài `_Project` / `ThirdParty`; có atlas; số liệu tốt hơn P0.

---

### P9 – Tài liệu và dọn dẹp

**Thêm**
- [ ] `Docs/README.md` cho feature lớn: Gameplay, Economy, Shop, DailyChallenge (guide 5).

**Sửa**
- [ ] `Docs/ProjectStructure.md`: viết lại theo cấu trúc mới (hoặc thay bằng link tới `README.md` từng feature).
- [ ] `Docs/Features/*`, `HowTo_MoRong.md`, `HowTo_ThemMechanic.md`, `TaskBreakdown.md`: cập nhật đường dẫn, tên class, cách giao tiếp mới.
- [ ] (Tùy chọn) `SampleScene` → `Main.unity`.
- [ ] Rà checklist guide mục 15 trên toàn dự án; ghi phần còn nợ vào `ProjectConventions.md`.

**Không làm lúc này:** tách `Core/Foundation` thành package UPM (chờ dự án thứ hai, guide 16).

**Xong khi:** tài liệu không còn đường dẫn / tên class cũ.

---

## 5. Estimate

Đơn vị: **giờ**. Real Time và Tiến độ cập nhật khi làm.

| Topic | Mô tả | Task | Time | Real Time | Tiến độ | Note |
| --- | --- | --- | --- | --- | --- | --- |
| P0 Chuẩn bị | Mốc so sánh, công cụ | Nhánh, chạy test, smoke test lần đầu | 1 | 0.5 | 100% | Smoke test một phần |
| | | Frame Debugger 3 màn + Build Report Android | 1.5 | 0.5 | 100% | Đo bằng Profiler counter + build qua MCP |
| | | `.editorconfig` + Unity Analyzers | 1 | 0.5 | 100% | |
| | | `ProjectConventions.md`, `Baseline.md`, lưu 3 save mẫu | 1 | 0.5 | 100% | |
| P1 Thư mục + namespace | Không đổi logic | Tạo cây thư mục, di chuyển 199 file trong Unity | 3 | 0.4 | 100% | Script mapping + `MoveAsset` qua MCP |
| | | Thêm namespace + `using` cho 199 file | 4 | 0.5 | 100% | Script tự bọc + dò `using` |
| | | Tách GameEvents, BoosterType, GameState, SoundId sang Contracts | 1 | 0.1 | 100% | |
| | | Kiểm tra Missing Script, test, smoke test | 1 | 0.5 | 100% | |
| P2 Contracts | Event + interface | Định nghĩa ~10 event, phát song song code cũ | 2 | 0.5 | 100% | 6 event (4 trong plan bỏ vì không có bên nghe) |
| | | ~6 interface, cài vào class sở hữu | 2 | 0.4 | 100% | 3 interface; `LevelIntroContext` |
| | | Unit test qua interface | 1 | 0.5 | 100% | 23 test; save trong bộ nhớ |
| P3 Service + Save | Bootstrap, save theo phần | `ISaveService` + `JsonFileSaveService` (giữ tmp / bak / runner) | 3 | | 0% | |
| | | Save class từng feature + `SaveMigrator` v1 → v2 + test 3 file mẫu | 4 | | 0% | Không được mất dữ liệu |
| | | Sửa 21 file dùng `SaveSystem.Data` | 4 | | 0% | |
| | | Ads / IAP / Clock service + Fake | 2 | | 0% | |
| | | Wallet / Inventory / Profile / Shop: static → instance | 3 | | 0% | |
| | | `GameBootstrap` + nối Init | 3 | | 0% | |
| P4 Bỏ Singleton | 12 Singleton + 4 cờ static | ScreenFlow (sửa phụ thuộc ngược) | 2 | | 0% | |
| | | LevelManager (101 chỗ) | 5 | | 0% | Lớn nhất |
| | | GridManager, BoosterManager | 5 | | 0% | |
| | | LevelTimer, EnergyManager | 3.5 | | 0% | |
| | | Daily*, LevelStars, TutorialController | 3 | | 0% | |
| | | GameConfig / GameplayVisuals ra khỏi Resources; cờ static; Singleton an toàn | 2.5 | | 0% | |
| P5 Bỏ Find / GetComponent | ~58 file | GetComponent → SerializeField + Reset, gán lại scene / prefab | 6 | | 0% | |
| | | AddComponent → có sẵn trong prefab | 3 | | 0% | |
| | | Find, Camera.main, transform.Find, registry raycast | 2 | | 0% | |
| P6 Asmdef | 6 runtime + Editor + Test | DOTween asmdef, tạo asmdef, reference | 2 | | 0% | |
| | | Sửa lỗi compile lộ ra | 3 | | 0% | Khó đoán trước |
| | | Build Android kiểm tra | 1 | | 0% | |
| P7 UI prefab | PopupView + prefab | Sửa PopupView 4 điểm + GameFlowUI | 2 | | 0% | |
| | | Widget dùng chung thành prefab | 3 | | 0% | |
| | | ~17 popup / màn hình / HUD thành prefab, đặt instance | 8 | | 0% | |
| | | Tách Canvas, Raycast Target | 1.5 | | 0% | |
| | | Sửa / xóa 15 builder | 5 | | 0% | |
| P8 Asset | Di chuyển, atlas, preset | Di chuyển ThirdParty + Art theo feature | 4 | | 0% | |
| | | Sprite Atlas + Preset texture | 3 | | 0% | |
| | | Đo lại, so với P0, test máy thật | 2 | | 0% | |
| P9 Tài liệu | Docs | README feature, ProjectStructure, Features/*, HowTo | 4 | | 0% | |
| **Tổng** | | | **~112.5** | **4.9** | **16%** | Chưa tính smoke test lặp lại mỗi giai đoạn (~0.5 h × 10) |

---

## 6. Smoke test sau mỗi giai đoạn

Chạy trong Editor (Play mode) sau mỗi giai đoạn; P6, P8 chạy thêm trên máy Android.

**Khởi động và save**
- [ ] Mở game lần đầu (xóa save): vào menu, không lỗi Console.
- [ ] Tắt / mở lại: coin, level, booster, năng lượng, hồ sơ, settings giữ nguyên.
- [ ] (Từ P3) Mở với 3 file save mẫu v1: dữ liệu đúng sau migrate.

**Màn chơi**
- [ ] Bấm Play → level hiện, intro chạy, kéo thả đặt khối, chạm khối để gỡ.
- [ ] Thắng: popup Win, sao theo thời gian, cộng coin / sao.
- [ ] Hết giờ: popup thêm giờ (+30 s quảng cáo giả lập, +60 s trả coin) → chơi tiếp / thua → popup Lose → chơi lại.
- [ ] Booster Hint, Freeze, Reveal: dùng được, trừ lượt; hết lượt → popup mua booster; tutorial booster lần đầu.
- [ ] Level có cơ chế (khóa, ẩn số, hướng): popup giới thiệu cơ chế lần đầu.
- [ ] Quit level giữa chừng → về menu.

**Meta**
- [ ] Năng lượng: trừ khi chơi, hết → popup refill; năng lượng vô hạn hiển thị đúng.
- [ ] Shop: mở, cuộn, mua bằng IAP → "Purchase Failed" (giả lập); mua bằng coin nếu có.
- [ ] Daily Reward: nhận thưởng ngày, không nhận lại được trong ngày.
- [ ] Daily Challenge: chọn ngày, chơi, nhận quà mốc.
- [ ] Lucky Wheel: quay bằng sao, nhận thưởng.
- [ ] Profile: đổi tên / avatar, số liệu thống kê đúng.
- [ ] Settings: tắt / bật nhạc, âm thanh, rung có tác dụng.

**Khác**
- [ ] Cheat Panel mở được (10 lần góc trên trái / F1), các cheat chính chạy.
- [ ] Console không có Error / Exception trong suốt lượt test.
- [ ] 46+ test xanh.

---

## 7. Rủi ro chung

| Rủi ro | Giảm bằng |
| --- | --- |
| Mất tham chiếu trong scene / prefab khi di chuyển / đổi tên | Chỉ thao tác trong Unity; commit nhỏ; kiểm tra Missing Script sau mỗi bước; `git diff` scene để thấy GUID đổi bất thường |
| Mất dữ liệu save người chơi | Migrate có test với file mẫu; giữ `save.v1.bak`; không xóa code đọc v1 trong ít nhất 1 phiên bản |
| Lỗi thứ tự khởi tạo khi bỏ Singleton | Bootstrap gọi `Init` theo thứ tự rõ ràng; không dựa vào `Awake` của object khác |
| Merge conflict `SampleScene.unity` | Làm tuần tự từng giai đoạn trên một nhánh; P7 tách UI ra prefab giúp giảm conflict về sau; cấu hình UnityYAMLMerge |
| Giai đoạn kéo dài, nhánh lệch xa | Mỗi giai đoạn merge ngay khi xong; không bắt đầu tính năng mới trên nhánh refactor |
| Over-engineering khi thêm interface / event | Chỉ thêm khi có feature thứ hai thật sự dùng (guide 1, 4.2); rà lại danh sách P2 trước khi code |
| Lỗi compile hàng loạt ở P6 | P1–P5 đã dọn phần lớn phụ thuộc; P6 chỉ còn sót lẻ |
