# Dependencies – phụ thuộc sai chiều

> Đo lần đầu sau **P1** ([RefactorPlan.md](RefactorPlan.md)): mỗi file đã có namespace nên `using PawBlock.…` cho thấy ai đang gọi ai.
> Đầu vào cho P2 (contracts / event), P3 (service), P4 (bỏ Singleton). Sau mỗi giai đoạn đo lại, mục tiêu **0** ở P6 (asmdef sẽ chặn lúc compile).

| | |
| --- | --- |
| Đo lúc | 2026-10-13 03:20, nhánh `Refactor-P6-Asmdef` (sau P6) |
| Tổng | **1** `using` sai chiều trong **1** file (P1: 89 / 57, P2: 86 / 55, P3: 75 / 52, P4: 80 / 53, P5: 76 / 49). Từ P6 asmdef chặn lúc compile |

## Quy tắc (guide 4.1)

```text
Core (Foundation, Contracts)  ←  UI/Framework (PawBlock.Shared)  ←  mảng tính năng (PawBlock.Features.*)  ←  App / UI/Home (PawBlock.Game)
```

- Chỉ được `using` sang tầng bên trái (thấp hơn).
- Feature không `using` feature khác (giao tiếp qua `Runtime/Core/Contracts`).
- `Runtime/Core/Contracts` không `using` `Runtime/Core/Foundation` hay tầng cao hơn.

## Hiện trạng

| Từ → tới | Số file | File |
| --- | --- | --- |
| Features.DailyReward → Features.Economy | 1 | RewardGranterTests (test tích hợp quà → ví coin, lúc đo cùng assembly `PawBlock.Meta`; nay mọi mảng chung `PawBlock.Runtime`) |

## Assembly (từ 2026-10-25)

Gộp theo mẫu Snacky_Dash còn `PawBlock.Runtime`, `PawBlock.Editor`, `PawBlock.Tests` ([ProjectConventions](ProjectConventions.md) mục 11): compiler **không** còn chặn chiều sai, nên đo lại bằng cách dưới mỗi lần thêm mảng / sự kiện mới. Bảng dưới là cách chia cũ (để đối chiếu).

## Assembly (P6 → 2026-10-24, nay đã gộp)

| Assembly | Thư mục | Được tham chiếu |
| --- | --- | --- |
| `PawBlock.Core.Contracts` | `Runtime/Core/Contracts` | — |
| `PawBlock.Core.Foundation` | `Runtime/Core/Foundation` | Contracts, DOTween.Modules |
| `PawBlock.Shared` | `Shared` | Contracts, Foundation, TMP, UI, DOTween.Modules, UIParticle |
| `PawBlock.Gameplay` | `Features/Gameplay` | Contracts, Foundation, Shared + TMP, UI, DOTween.Modules, InputSystem, Spine, UIParticle |
| `PawBlock.Meta` | `Features/` (trừ Gameplay): Economy, Shop, DailyReward, DailyChallenge, LuckyWheel, Profile, Settings | Contracts, Foundation, Shared + TMP, UI, DOTween.Modules, InputSystem |
| `PawBlock.Game` | `Game` | Tất cả |
| `*.Editor` | `Runtime/Editor/Core`, `Runtime/Editor/UI`, `Runtime/Editor/Gameplay`, `Runtime/Editor/App` | Module chứa nó + những gì module đó được biết; `Game.Editor` biết mọi Editor khác |
| `*.Tests` | `Runtime/Tests/Core`, `Runtime/Tests/Gameplay`, `Runtime/Tests/Economy` (+ asmref DailyChallenge / DailyReward / LuckyWheel), `Runtime/Tests/App` | Như module + TestRunner, NUnit |

## Nhận xét (số liệu lúc P1)

- **`→ Game` (39)** chủ yếu `GameFlowUI`, `ScreenFlow.Instance`, `GameConfig`, `MainMenuNavigator`: P2 thay bằng event (`ScreenChangedEvent` đã có) + interface, P3–P4 bỏ Singleton, `GameConfig` chia config về từng feature.
- **`→ Features.Economy` (16)**: `CoinWallet`, `StarWallet`, `EnergyManager`, `LivesEnergyConfig` (số giây của đồng hồ) → P2 interface `ICurrencyWallet`, `IEnergy` trong Contracts; config thời gian về Gameplay.
- **`→ Features.Gameplay` từ meta feature (17)**: chủ yếu `LevelManager.Instance` (26 chỗ), `BoosterManager` / `BoosterDatabase` / `BoosterData` (26), `LevelDefinition`, `LevelDifficulty`, `LevelGenerator` (Daily) → P2 event / interface (`ILevelLauncher`, `IBoosterInventory`…), dữ liệu booster dùng chung sang Contracts.
- **DailyChallenge / LuckyWheel → DailyReward (8)**: dùng chung `DailyRewardItem` / `DailyRewardKind` (và `DailyRewardDatabase` lấy icon quà) → type quà chuyển sang `Runtime/Core/Contracts/Types` (≥ 2 feature dùng, guide 5).
- **Builder trong `Editor/`** chiếm 24 dòng: dựng UI nhiều feature cùng lúc, P7 tách theo feature.

## Thay đổi qua từng giai đoạn

| Giai đoạn | Tổng | Ghi chú |
| --- | --- | --- |
| P1 | 89 / 57 file | Lần đầu đo |
| P2 | 86 / 55 file | Hết `Core.Contracts → Features.Gameplay` (`LevelLoadedEvent` mang `LevelId`); `DailyRewardItem` / `DailyRewardKind` sang Contracts nên 2 database (DailyChallenge, LuckyWheel) bỏ `using DailyReward`. Phần còn lại chờ P4 chuyển nơi gọi sang event / interface của P2 |
| P3 | 75 / 52 file | Script Shop chỉ dùng contract (còn `ShopBuilder`, P7); `ProfileStatsRecorder` nghe event (hết `→ Economy`, `→ Gameplay`); LuckyWheel / DailyChallenge trao quà qua `IRewardGranter`; ví sao qua `ICurrencyWallet` + event. Còn lại chủ yếu `.Instance` (P4) và builder (P7) |
| P4 | 80 / 53 file | Hết `.Instance` của game logic. Tăng do `LivesEnergyConfig` (Economy) nay truyền thẳng vào 5 file Gameplay thay vì qua `GameConfig.LivesEnergy` (Game). Các dòng `→ Game` còn lại chủ yếu `GameConfig` / builder: P6 chuyển config con về feature, P7 tách builder |
| P5 | 76 / 49 file | `IMenuPopups` thay `GameFlowUI` ở nút Settings / Avatar / năng lượng; `UIButtonSounds` dùng marker `NoClickVibrate` thay `BoosterButton` |
| P6 | 1 / 1 file | Config về Gameplay, `IDailyLevelLoader`, `MenuShortcutButton` về Shared, builder về `Runtime/Editor/App`, rồi tạo asmdef: compiler chặn mọi chiều sai trừ Meta ↔ Meta |

## Cách đo lại

Đếm dòng `using PawBlock.X;` trong file có `namespace PawBlock.Y` mà X ở tầng cao hơn Y, hoặc X, Y là hai feature khác nhau (bỏ hậu tố `.Tests`).
