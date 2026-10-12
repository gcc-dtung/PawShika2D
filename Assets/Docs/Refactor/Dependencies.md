# Dependencies – phụ thuộc sai chiều

> Đo lần đầu sau **P1** ([RefactorPlan.md](RefactorPlan.md)): mỗi file đã có namespace nên `using PawBlock.…` cho thấy ai đang gọi ai.
> Đầu vào cho P2 (contracts / event), P3 (service), P4 (bỏ Singleton). Sau mỗi giai đoạn đo lại, mục tiêu **0** ở P6 (asmdef sẽ chặn lúc compile).

| | |
| --- | --- |
| Đo lúc | 2026-10-12 15:30, nhánh `Refactor-P3-ServiceSave` (sau P3) |
| Tổng | **75** `using` sai chiều trong **52** file (P1: 89 / 57, P2: 86 / 55) |

## Quy tắc (guide 4.1)

```text
Core (Foundation, Contracts)  ←  Shared  ←  Features/*  ←  Game
```

- Chỉ được `using` sang tầng bên trái (thấp hơn).
- Feature không `using` feature khác (giao tiếp qua `Core/Contracts`).
- `Core/Contracts` không `using` `Core/Foundation` hay tầng cao hơn.

## Hiện trạng

| Từ → tới | Số file | File |
| --- | --- | --- |
| Features.Gameplay → Game | 17 | BoosterBuilder, DifficultyBuilder, FreezeBooster, FreezeButtonCountdown, FreezeTutorialDemo, LevelEditorWindow, LevelStars, LevelStatsRecorder, LevelTimer, LosePopup, MoreTimePopup, QuitLevelPopup, StarsBuilder, TimeStarBar, TutorialBuilder, WinPopup, WinScreenBuilder |
| Features.Gameplay → Features.Economy | 8 | BoosterInventoryTests, LevelStars, LevelTimer, MoreTimePopup, QuitLevelPopup, StarRatingTests, StarsBuilder, TimeStarBar |
| Features.DailyChallenge → Features.Gameplay | 4 | DailyChallengeBuilder, DailyChallengeDatabase, DailyChallengeManager, DailyGiftBubble |
| Features.Economy → Game | 4 | CoinHUD, EnergyHUD, EnergyManager, RefillEnergyPopup |
| Features.DailyChallenge → Features.DailyReward | 3 | DailyChallengeBuilder, DailyChallengePanel, DailyGiftBubble |
| Features.DailyReward → Game | 3 | DailyRewardBuilder, DailyRewardButton, DailyRewardManager |
| Features.Profile → Game | 3 | AvatarButton, ProfileBuilder, ProfilePopup |
| Features.Settings → Game | 3 | SettingsBuilder, SettingsButton, SettingsPopup |
| Shared → Game | 3 | BackgroundPatternBuilder, NoticePopup, UIZoneBuilder |
| Core.Foundation → Features.Gameplay | 2 | FeelCurves, FeelCurvesTests |
| Features.DailyChallenge → Game | 2 | DailyChallengeBuilder, DailyChallengePanel |
| Features.DailyReward → Features.Gameplay | 2 | DailyRewardBuilder, DailyRewardSlot |
| Features.Economy → Features.Gameplay | 2 | EconomyConfig, RefillEnergyPopup |
| Features.Gameplay → Features.DailyChallenge | 2 | LevelHUD, LevelManager |
| Features.LuckyWheel → Features.DailyChallenge | 2 | LuckyWheelBuilder, LuckyWheelPopup |
| Features.LuckyWheel → Features.DailyReward | 2 | LuckyWheelBuilder, LuckyWheelPopup |
| Features.LuckyWheel → Game | 2 | LuckyWheelBuilder, LuckyWheelButton |
| Features.Profile → Features.Gameplay | 2 | ProfileBuilder, ProfilePopup |
| Features.Settings → Features.Gameplay | 2 | SettingsBuilder, SettingsPopup |
| Shared → Features.Gameplay | 2 | NoticePopup, UIButtonSounds |
| Features.DailyReward → Features.Economy | 1 | RewardGranterTests |
| Features.LuckyWheel → Features.Gameplay | 1 | LuckyWheelBuilder |
| Features.Shop → Features.Economy | 1 | ShopBuilder |
| Features.Shop → Features.Gameplay | 1 | ShopBuilder |
| Features.Shop → Game | 1 | ShopBuilder |

## Nhận xét (số liệu lúc P1)

- **`→ Game` (39)** chủ yếu `GameFlowUI`, `ScreenFlow.Instance`, `GameConfig`, `MainMenuNavigator`: P2 thay bằng event (`ScreenChangedEvent` đã có) + interface, P3–P4 bỏ Singleton, `GameConfig` chia config về từng feature.
- **`→ Features.Economy` (16)**: `CoinWallet`, `StarWallet`, `EnergyManager`, `LivesEnergyConfig` (số giây của đồng hồ) → P2 interface `ICurrencyWallet`, `IEnergy` trong Contracts; config thời gian về Gameplay.
- **`→ Features.Gameplay` từ meta feature (17)**: chủ yếu `LevelManager.Instance` (26 chỗ), `BoosterManager` / `BoosterDatabase` / `BoosterData` (26), `LevelDefinition`, `LevelDifficulty`, `LevelGenerator` (Daily) → P2 event / interface (`ILevelLauncher`, `IBoosterInventory`…), dữ liệu booster dùng chung sang Contracts.
- **DailyChallenge / LuckyWheel → DailyReward (8)**: dùng chung `DailyRewardItem` / `DailyRewardKind` (và `DailyRewardDatabase` lấy icon quà) → type quà chuyển sang `Core/Contracts/Types` (≥ 2 feature dùng, guide 5).
- **Builder trong `Editor/`** chiếm 24 dòng: dựng UI nhiều feature cùng lúc, P7 tách theo feature.

## Thay đổi qua từng giai đoạn

| Giai đoạn | Tổng | Ghi chú |
| --- | --- | --- |
| P1 | 89 / 57 file | Lần đầu đo |
| P2 | 86 / 55 file | Hết `Core.Contracts → Features.Gameplay` (`LevelLoadedEvent` mang `LevelId`); `DailyRewardItem` / `DailyRewardKind` sang Contracts nên 2 database (DailyChallenge, LuckyWheel) bỏ `using DailyReward`. Phần còn lại chờ P4 chuyển nơi gọi sang event / interface của P2 |
| P3 | 75 / 52 file | Script Shop chỉ dùng contract (còn `ShopBuilder`, P7); `ProfileStatsRecorder` nghe event (hết `→ Economy`, `→ Gameplay`); LuckyWheel / DailyChallenge trao quà qua `IRewardGranter`; ví sao qua `ICurrencyWallet` + event. Còn lại chủ yếu `.Instance` (P4) và builder (P7) |

## Cách đo lại

Đếm dòng `using PawBlock.X;` trong file có `namespace PawBlock.Y` mà X ở tầng cao hơn Y, hoặc X, Y là hai feature khác nhau (bỏ hậu tố `.Tests`).
