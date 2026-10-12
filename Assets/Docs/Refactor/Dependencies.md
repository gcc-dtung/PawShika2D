# Dependencies – phụ thuộc sai chiều

> Đo lần đầu sau **P1** ([RefactorPlan.md](RefactorPlan.md)): mỗi file đã có namespace nên `using PawBlock.…` cho thấy ai đang gọi ai.
> Đầu vào cho P2 (contracts / event), P3 (service), P4 (bỏ Singleton). Sau mỗi giai đoạn đo lại, mục tiêu **0** ở P6 (asmdef sẽ chặn lúc compile).

| | |
| --- | --- |
| Đo lúc | 2026-10-12 13:10, nhánh `Refactor-P2-Contracts` (sau P2) |
| Tổng | **86** `using` sai chiều trong **55** file (P1: 89 / 57) |

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
| Features.Gameplay → Features.Economy | 10 | BoosterManager, BuyBoosterPopup, LevelStars, LevelTimer, MoreTimePopup, QuitLevelPopup, StarRatingTests, StarsBuilder, TimeStarBar, WinPopup |
| Features.DailyChallenge → Features.DailyReward | 4 | DailyChallengeBuilder, DailyChallengeManager, DailyChallengePanel, DailyGiftBubble |
| Features.DailyChallenge → Features.Gameplay | 4 | DailyChallengeBuilder, DailyChallengeDatabase, DailyChallengeManager, DailyGiftBubble |
| Features.Economy → Game | 4 | CoinHUD, EnergyHUD, EnergyManager, RefillEnergyPopup |
| Features.DailyReward → Features.Gameplay | 3 | DailyRewardBuilder, DailyRewardManager, DailyRewardSlot |
| Features.DailyReward → Game | 3 | DailyRewardBuilder, DailyRewardButton, DailyRewardManager |
| Features.Profile → Features.Gameplay | 3 | ProfileBuilder, ProfilePopup, ProfileStatsRecorder |
| Features.Profile → Game | 3 | AvatarButton, ProfileBuilder, ProfilePopup |
| Features.Settings → Game | 3 | SettingsBuilder, SettingsButton, SettingsPopup |
| Shared → Game | 3 | BackgroundPatternBuilder, NoticePopup, UIZoneBuilder |
| Core.Foundation → Features.Gameplay | 2 | FeelCurves, FeelCurvesTests |
| Features.DailyChallenge → Game | 2 | DailyChallengeBuilder, DailyChallengePanel |
| Features.Economy → Features.Gameplay | 2 | EconomyConfig, RefillEnergyPopup |
| Features.Gameplay → Features.DailyChallenge | 2 | LevelHUD, LevelManager |
| Features.LuckyWheel → Features.DailyChallenge | 2 | LuckyWheelBuilder, LuckyWheelPopup |
| Features.LuckyWheel → Features.DailyReward | 2 | LuckyWheelBuilder, LuckyWheelPopup |
| Features.LuckyWheel → Features.Economy | 2 | LuckyWheelButton, LuckyWheelPopup |
| Features.LuckyWheel → Game | 2 | LuckyWheelBuilder, LuckyWheelButton |
| Features.Settings → Features.Gameplay | 2 | SettingsBuilder, SettingsPopup |
| Features.Shop → Features.Economy | 2 | ShopBuilder, ShopService |
| Features.Shop → Features.Gameplay | 2 | ShopBuilder, ShopService |
| Shared → Features.Gameplay | 2 | NoticePopup, UIButtonSounds |
| Core.Foundation → Game | 1 | RewardedAds |
| Features.DailyReward → Features.Economy | 1 | DailyRewardManager |
| Features.LuckyWheel → Features.Gameplay | 1 | LuckyWheelBuilder |
| Features.Profile → Features.Economy | 1 | ProfileStatsRecorder |
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

## Cách đo lại

Đếm dòng `using PawBlock.X;` trong file có `namespace PawBlock.Y` mà X ở tầng cao hơn Y, hoặc X, Y là hai feature khác nhau (bỏ hậu tố `.Tests`).
