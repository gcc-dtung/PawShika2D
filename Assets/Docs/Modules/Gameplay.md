# Gameplay

Lõi màn chơi Shikaku "mèo": lưới, kéo thả tạo khối, luật đặt / gỡ, cơ chế ô (Void, Hướng, Khóa, "?"), đồng hồ đếm ngược, sao theo thời gian, booster, tutorial, intro đầu level, level + công cụ làm level. Code ở `Runtime/Gameplay/` (assembly chung `PawBlock.Runtime`); thư mục con trong đó gọi thẳng nhau.

Chi tiết hành vi: [Docs/Features](../Features/00_README.md) (01 lưới, 03 cơ chế, 04 level, 05 booster, 09 tutorial, 14 game feel, 15 sao).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Runtime` (chung); chỉ dùng `Core.Contracts`, `Core.Foundation`, `UI/Framework` (không tham chiếu mảng khác) |
| Phát event | `LevelLoadedEvent`, `GameStateChangedEvent`, `LevelCompletedEvent`, `LevelFailedEvent`, `AllLevelsCompletedEvent` (LevelManager); `StarsChangedEvent` (LevelStars); `BoosterUsedEvent` (BoosterManager); `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent` (BoosterTutorial) |
| Nghe event | Chủ yếu event của chính Gameplay (đồng bộ HUD / hiệu ứng) |
| Cài interface | `IGamePause` (LevelManager), `IBoosterInventory` + `IBoosterCatalog` (BoosterManager), `IDailyLevelLoader` (DailyLevelLoader) |
| Dùng interface | `ICurrencyWallet` (coin, sao), `IEnergyService` (bỏ dở level), `IScreenNavigator`, `IAdsService`, `ISaveService`, `IClock` |
| Save | Section `"gameplay"` (`GameplaySave`: tiến độ level, lượt booster, tutorial / cơ chế đã xem, số liệu từng level) |
| Config | `Data/Config/`: `BoosterDatabase`, `DifficultyTheme`, `EconomyConfig`, `LivesEnergyConfig`, `GameplayVisuals`, `Levels/` (`LevelDatabase` + `LevelDefinition`) |

## Class chính

| Thư mục | Class | Vai trò |
| --- | --- | --- |
| `Board/` | `BoardModel`, `CellModel`, `ClueCell` (C# thuần) | Luật lưới: kiểm tra vùng chọn, đặt / gỡ khối, khóa, thắng |
| | `GridManager` | Nối input với `BoardModel` và view, phát event đặt / gỡ / sai |
| | `GridSpawner`, `CellView`, `GridGeometry` | Dựng lưới, ô, registry `Collider2D → CellView` cho raycast |
| `Block/` | `BlockModel`, `PlacedBlockView`, `CatFaceAnimator` + `CatFaceTicker` | Khối mèo (pool), mặt mèo chạy chung 1 Update |
| `Input/` | `InputManager` → `DragAndDropController` → `DragSelection`; `SelectionBoxView`, `DragCountBubble` | Kéo thả (Input System mới), khung chọn, bong bóng đếm ô |
| `Level/` | `LevelManager` | Vòng đời ván (`GameState`), nạp level, chuyển level, `IGamePause` |
| | `LevelDefinition`, `LevelDatabase`, `LevelGenerator`, `DailyLevelLoader`, `DifficultyTheme` | Dữ liệu level, sinh level theo seed, level Daily |
| `Solver/` | `ShikakuSolver` | Giải / đếm lời giải (Hint, Reveal, kiểm tra level) |
| `Timer/`, `Stars/` | `LevelTimer`; `LevelStars`, `StarRating` | Đồng hồ đếm ngược, Freeze; sao theo thời gian còn lại |
| `Booster/` | `BoosterManager`, `BoosterEffect` + `BoosterEffectFactory` (Hint / Freeze / Reveal), `BoosterInventory`, `BoosterTutorial` | Booster (Strategy + Factory), lượt, mở khóa bằng tutorial |
| `Intro/` | `ILevelIntro`, `LevelIntroSequencer` | Hàng đợi intro sau khi nạp level (băng HARD → popup cơ chế → tutorial booster) |
| `Tutorial/` | `TutorialController`, `TutorialLevels`, `TutorialHand`, `MechanicIntro` | Tutorial lần đầu, popup cơ chế lần đầu gặp |
| `Feedback/` | `GameplayFeedback`, `BlockFeedback`, `DragFeedback`, `GameHapticEvents` | Game feel: chữ khen, combo, hạt bụi, rung |
| `UI/` | HUD (`LevelHUD`, `TimerUI`, `TimeStarBar`, `BoosterButton`...), popup (`WinPopup`, `LosePopup`, `MoreTimePopup`, `QuitLevelPopup`, `BuyBoosterPopup`, `MechanicIntroPopup`) | Giao diện màn chơi |
| `Visuals/` | `PlayZone`, `BoardCameraFitter`, `GameplayVisuals`, `BoardDecor` | Camera vừa lưới, sprite màn chơi, đồ trang trí bám góc khung bảng |
| `Analytics/` | `LevelStatsRecorder` | Ghi số liệu chơi thật từng level (xuất CSV trong Editor) |

Prefab: `Prefabs/UI/Screens/Game/GameplayHUD` (HUD + thanh booster), `Popups/*`, `Cell`, `PlacedBlock`, `DragCountBubble`, `BoosterButton`, `Praise`, `MistakeFlash`, `Freeze/`.

## Cách dùng

Mọi component nhận dependency qua `Init` do `GameBootstrap` gọi (trước `Awake` của chúng); tham chiếu trong scene / prefab do Editor tự điền.

```csharp
// GameBootstrap (rút gọn)
levelManager.Init(save, gridManager, levelTimer, tutorialController);
boosterManager.Init(save, services.Boosters, services.Coins, gridManager, levelTimer, lives, hintMarker);
levelStars.Init(services.Stars, levelManager, lives, levelTimer, gridSpawner);
winPopup.Init(services.Coins, ads, screenFlow, levelManager, levelStars, economy);

// Feature khác chỉ biết contract
EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);   // ProfileStatsRecorder, DailyChallengeManager
levelLoader.LoadDaily(day);                                   // DailyChallenge, qua IDailyLevelLoader
```

## Công cụ Editor

| Menu | Việc |
| --- | --- |
| `Tools/PawBlock/Level Editor` | Vẽ / sửa level, chơi thử |
| `Tools/PawBlock/Validate Levels` | Kiểm tra mọi level có đúng 1 lời giải |
| `Tools/PawBlock/Mechanic Injector` | Thêm cơ chế (Hướng, Khóa, "?") vào level có sẵn |
| `Tools/PawBlock/Level Difficulty` | Độ khó lý thuyết + so với số liệu thật |
| `Tools/PawBlock/Level Generator` | Sinh level theo seed hàng loạt |
| `Tools/PawBlock/Builders/Import ShikaCat Levels`, `Reorder Levels` | Nhập level bản gốc, xếp lại thứ tự |
| `Tools/PawBlock/Builders/Setup Gameplay Visuals` | Điền `GameplayVisuals` từ `Source/Art/InGame/Gameplay/`, `Source/Art/UI/Gameplay/` |

## Ghi chú kiến trúc

- **Model / View:** luật ở class C# thuần (`BoardModel`, `DragSelection`, `StarRating`, `LevelGenerator`, `ShikakuSolver`) nên test được không cần Play mode (`Runtime/Tests/Gameplay/Editor`, 9 file).
- **Booster = Strategy + Factory:** thêm booster mới chỉ sửa `BoosterEffectFactory` + thêm 1 class `BoosterEffect` (xem [HowTo_MoRong](../Features/HowTo_MoRong.md)).
- **Intro = hàng đợi:** mỗi lớp phủ đầu level là 1 `ILevelIntro` có `Order`, `LevelIntroSequencer` chạy lần lượt và khóa lưới qua `InputGate`.
- **Cơ chế mới:** tìm chú thích `// [MECHANIC]` (xem [HowTo_ThemMechanic](../Features/HowTo_ThemMechanic.md)).
- `EconomyConfig` / `LivesEnergyConfig` nằm ở Gameplay vì Gameplay dùng nhiều nhất; Economy nhận số cần thiết (`maxEnergy`, `refillSeconds`...) dạng số qua `Init`, không biết class config.
