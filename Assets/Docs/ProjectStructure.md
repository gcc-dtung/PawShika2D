# Cấu trúc dự án

> Kiến trúc module theo [Refactor/UnityCodingGuidelines.md](Refactor/UnityCodingGuidelines.md) (mục 4, 5); quy ước riêng của dự án ở [Refactor/ProjectConventions.md](Refactor/ProjectConventions.md).
> Chi tiết từng feature (phụ thuộc, class chính, cách nối): `Assets/_Project/Features/<Tên>/Docs/README.md` (bảng dưới).

## Module và chiều phụ thuộc

```text
Core/Contracts  ←  Core/Foundation  ←  Shared  ←  Features/*  ←  Game
(event, interface,   (EventBus, Save,     (PopupView,   (Gameplay; Economy,   (GameBootstrap, ScreenFlow,
 enum dùng chung)     Audio, Clock, Ads…)  widget UI)    Shop, Daily…)         GameFlowUI, Main Menu)
```

- Mũi tên = "được dùng bởi". Feature **không** tham chiếu feature khác; giao tiếp qua event / interface trong `Core/Contracts`.
- `Game` là nơi duy nhất biết mọi feature: tạo service, gọi `Init` cho từng component (`GameBootstrap`), ghép màn hình trộn nhiều feature (`Game/Prefabs`).
- Mỗi module là 1 assembly (`.asmdef`); compiler chặn tham chiếu sai chiều. Feature meta dùng chung `PawBlock.Meta` nên phải tự kiểm (`Docs/Refactor/Dependencies.md`).

| Module | Assembly | Nội dung | README |
| --- | --- | --- | --- |
| `Core/Contracts` | `PawBlock.Core.Contracts` | 13 event (`IGameEvent`), 10 interface, 5 type dùng chung | — (xem bảng dưới) |
| `Core/Foundation` | `PawBlock.Core.Foundation` | Hạ tầng không dính luật game: EventBus, Save, Audio, Clock, Ads / IAP, InputGate, Haptics, Util | — |
| `Shared` | `PawBlock.Shared` | `PopupView`, `MenuPanel`, widget / helper UI, art / font / âm thanh dùng ≥ 2 module | — |
| `Features/Gameplay` | `PawBlock.Gameplay` | Lõi màn chơi | [README](../_Project/Features/Gameplay/Docs/README.md) |
| `Features/Economy` | `PawBlock.Meta` | Ví coin / sao, năng lượng | [README](../_Project/Features/Economy/Docs/README.md) |
| `Features/Shop` | `PawBlock.Meta` | Gói IAP | [README](../_Project/Features/Shop/Docs/README.md) |
| `Features/DailyReward` | `PawBlock.Meta` | Điểm danh 7 ngày, trao quà / icon quà | [README](../_Project/Features/DailyReward/Docs/README.md) |
| `Features/DailyChallenge` | `PawBlock.Meta` | Level mỗi ngày, lịch tháng, quà mốc | [README](../_Project/Features/DailyChallenge/Docs/README.md) |
| `Features/LuckyWheel` | `PawBlock.Meta` | Vòng quay tiêu sao | [README](../_Project/Features/LuckyWheel/Docs/README.md) |
| `Features/Profile` | `PawBlock.Meta` | Hồ sơ người chơi, chỉ số | [README](../_Project/Features/Profile/Docs/README.md) |
| `Features/Settings` | `PawBlock.Meta` | Popup cài đặt (nhạc / âm / rung) — 2 script, không cần README | — |
| `Game` | `PawBlock.Game` | Composition root, luồng màn hình, Main Menu, save migrate, cheat | — (mục Game dưới) |

Editor / Test: `PawBlock.Core.Foundation.Editor`, `Shared.Editor`, `Gameplay.Editor`, `Game.Editor`; test `PawBlock.Core.Foundation.Tests`, `Gameplay.Tests`, `Meta.Tests`, `Game.Tests` (160 test, chạy EditMode).

---

## Thư mục

```text
Assets/
├── _Project/
│   ├── Core/
│   │   ├── Contracts/   Events/ Interfaces/ Types/
│   │   └── Foundation/  Audio/ Clock/ Events/ Feedback/ Input/ Save/ Services/ Util/ Editor/ Tests/
│   ├── Shared/          Scripts/UI/  Prefabs/ (NoticePopup, Widgets/)  Art/ (SharedAtlas)  Audio/  Fonts/  Configs/ (AudioDatabase)  Editor/
│   ├── Features/
│   │   ├── Gameplay/    Scripts/ (Board, Block, Input, Level, Solver, Timer, Stars, Booster, Intro, Tutorial, Feedback, UI, Visuals, Analytics, Config, Save)
│   │   │                Prefabs/  Art/  Configs/ (+ Levels/)  Editor/ (Level tools, Builders)  Tests/  Docs/
│   │   ├── Economy/  Shop/  DailyReward/  DailyChallenge/  LuckyWheel/  Profile/  Settings/
│   │   │                mỗi feature: Scripts/  Prefabs/  Art/  Configs/  Tests/  Docs/ (thư mục rỗng thì không tạo)
│   │   └── PawBlock.Meta.asmdef
│   ├── Game/            Scripts/ (GameBootstrap, ScreenFlow, GameFlowUI, Menu/, Save/, Cheats/)  Prefabs/ (MainMenu, Menu/)  Art/  Configs/ (GameConfig)  Editor/  Tests/
│   ├── Scenes/          SampleScene.unity (scene duy nhất)
│   └── Settings/        URP 2D, Presets/ (nén texture)
├── ThirdParty/
│   ├── PawBlock/        Bản gốc Paw Block (01_Audio … 11_Original_Atlases_Textures, Texture2D): không sửa, không tham chiếu
│   └── ShikaCat/        Bản gốc ShikaCat (gồm Level_Data/shikacat-levels.json)
├── Plugins/             DOTween (Demigiant), plugin rung iOS
├── TextMesh Pro/        Font mặc định TMP
├── Resources/           Chỉ DOTweenSettings
└── Docs/                Tài liệu (file này, Features/, Refactor/, RoadMap/)
```

Đặt file mới ở đâu: bảng "Đặt file ở đâu" trong guide mục 5; asset / atlas / preset: [ProjectConventions](Refactor/ProjectConventions.md) mục 13.

---

## Core

### Contracts

| Loại | Thành phần |
| --- | --- |
| Event (`EventBus<T>`) | `LevelLoadedEvent`, `GameStateChangedEvent`, `LevelCompletedEvent`, `LevelFailedEvent`, `AllLevelsCompletedEvent`, `StarsChangedEvent`, `StarsBalanceChangedEvent`, `CoinsChangedEvent`, `BoosterUsedEvent`, `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent`, `ScreenChangedEvent`, `ShopOpenRequestedEvent` |
| Interface | `ICurrencyWallet`, `IEnergyService`, `IBoosterInventory`, `IBoosterCatalog`, `IRewardGranter`, `IRewardIcons`, `IDailyLevelLoader`, `IGamePause`, `IScreenNavigator`, `IMenuPopups` |
| Type | `GameState`, `BoosterType`, `SoundId`, `DailyRewardItem`, `DailyRewardKind` |

Ai cài / ai dùng từng interface: chú thích trong file và bảng "Phụ thuộc" của README feature.

### Foundation

| Thư mục | Class | Vai trò |
| --- | --- | --- |
| `Events/` | `EventBus<T>`, `EventBusRegistry` | Kênh sự kiện theo kiểu struct |
| `Save/` | `ISaveService`, `JsonFileSaveService`, `JsonSections`, `SaveRunner` | Save v2 chia section theo feature, ghi gom (tmp / bak) |
| `Audio/` | `AudioManager` (Singleton hạ tầng duy nhất), `AudioDatabase`, `SettingsSave` | Nhạc nền, pool SFX, cài đặt nhạc / âm / rung |
| `Clock/` | `IClock`, `TrustedClockService`, `TrustedClockModel`, `TrustedClockRunner` | Giờ server, chống chỉnh giờ máy |
| `Services/` | `IAdsService`, `IIAPService`, `FakeAdsService`, `FakeIAPService` | Quảng cáo / IAP (chưa có SDK) |
| `Input/` | `InputGate` | Khóa thao tác lưới theo "chủ khóa" |
| `Feedback/` | `Haptics`, `FeelCurves`, `ProceduralSprites` | Rung, công thức game feel, sprite vẽ bằng code |
| `Util/` | `Singleton<T>`, `DayNumber`, `TextColor`, `TweenSetup`, `FrameRateSetup`, `EditorChildren` | Tiện ích |
| `Editor/` | `SaveDataMenu` | `Tools/PawBlock/Save Data/*` |

## Shared

| Class | Vai trò |
| --- | --- |
| `PopupView` (abstract) | Base popup: mở / đóng có hiệu ứng, `OnOpened` / `OnClosed`, event `Closed`, `CloseOnLevelLoad` |
| `MenuPanel` (abstract) | Gốc 1 tab Main Menu: `Rect`, `SetSelected`, `OnShown` / `OnHidden` |
| `NoticePopup` | Popup thông báo chung |
| `ButtonPress`, `UIButtonPress`, `UIButtonSounds`, `NoClickSound`, `NoClickVibrate`, `ButtonPressIgnore`, `UIGrayscaleWhenDisabled` | Cảm giác bấm, âm / rung khi bấm, nút xám khi tắt |
| `SafeAreaZone`, `BackgroundCover`, `BackgroundFitter`, `NestedScrollRect` | Bố cục theo màn hình |
| `MenuShortcutButton`, `SettingsButton` | Nút phụ trên Home (mời bấm, mở Shop), nút bánh răng |
| `UIConfetti`, `TMPLetterPopIn`, `RewardText` | Hiệu ứng / chữ quà |
| `Editor/UIAutoSetup`, `Editor/GameFontSetup` | Gắn sẵn ButtonPress / danh sách nút; áp font chung |

## Game

| Class | Vai trò |
| --- | --- |
| `GameBootstrap` | Composition root: tạo `GameServices` (save, đồng hồ, ads, IAP, ví, kho, hồ sơ, Shop), gọi `Init` cho mọi component theo thứ tự |
| `GameServices`, `GameScene` | Service dùng chung; object scene cho Cheat / Debug |
| `GameConfig` | Config gốc (`Configs/GameConfig`) trỏ tới config của các feature |
| `ScreenFlow` (`IScreenNavigator`) | Menu ↔ màn chơi, phát `ScreenChangedEvent`, mở tab Shop khi có `ShopOpenRequestedEvent` |
| `GameFlowUI` (`IMenuPopups`) | Nối `GameState` với popup, mở popup dùng chung, đóng popup khi đổi level |
| `GameAudioEvents` | Nối âm thanh vào event của game |
| `Menu/` | `MainMenuNavigator`, `MenuTabBar`, `MenuTabButton`, `MenuSwipe`, `ShopPanel`, `HomePanel`, `PlayButtonView` |
| `Save/` | `SaveMigrator`, `SaveDataV1` (save v1 → v2) |
| `Cheats/` | `DebugCheats`, `CheatPanel` (trong bản build) |
| `Editor/` | `GameBootstrapCollector` (tự điền tham chiếu Bootstrap), `DebugPanelWindow`, `GameConfigSetup`, `AudioSetup` |

---

## Scene `SampleScene`

```text
GameBootstrap        GameBootstrap, SaveRunner, TrustedClockRunner
Main Camera          Camera orthographic (BoardCameraFitter chỉnh size / vị trí)
Global Light 2D
SpawnGrid            GridSpawner
Manager              GridManager, InputManager, DragAndDropController, LevelTimer, LevelManager, GameplayFeedback,
                     BoosterManager, EnergyManager, ScreenFlow, DailyRewardManager, TutorialController, BoosterTutorial,
                     MechanicIntro, LevelIntroSequencer, FreezeTutorialDemo, BoardCameraFitter, LevelStatsRecorder,
                     ProfileStatsRecorder, DragFeedback, BlockFeedback, HintMarker, LevelStars, DailyChallengeManager,
                     CatFaceTicker, DailyLevelLoader
├── HintFrameTemplate, DustTemplate (tắt)   mẫu cho pool
SelectionBox (tắt)   SelectionBoxView
bg                   BackgroundFitter (+ Pattern)
Canvas_HUD           [prefab GameplayHUD] HUD màn chơi, thanh booster, tutorial; LowTimeVignette, BoosterUseFx
Canvas_Menu          [prefab MainMenu] MainMenuNavigator; lồng ShopPanel / HomePanel / DailyPanel / MenuTabBar
Canvas_Popup         luôn bật
└── Popups           GameFlowUI; 12 instance popup prefab (tắt sẵn), mỗi popup Canvas + GraphicRaycaster riêng
AudioManager         AudioManager, GameAudioEvents, GameHapticEvents; Music + Sfx_0..7 (AudioSource đặt sẵn)
EventSystem          InputSystemUIInputModule
DragCountBubble      [prefab] tắt sẵn, bật khi kéo
TutorialHand (tắt)   mẫu bàn tay (HintMarker nhân bản)
```

Khối mèo, chữ khen lấy từ prefab qua `ObjectPool` ở lần dùng đầu rồi tái sử dụng; ô lưới được `GridSpawner` dùng lại giữa các level. Lúc chạy không `Find` / `GetComponent` / `AddComponent`: mọi tham chiếu đặt sẵn trong scene / prefab (Editor tự điền).

Canvas: `Canvas_HUD` (order 0) < tutorial booster (Canvas override 5, nút được chỉ 6) < `Canvas_Menu` (10) < `Canvas_Popup` (20).

---

## Luồng một nước đi

```text
InputManager ─► DragAndDropController ─► GridManager.BeginSelection (chạm ô trống)
                       │                      (chạm khối đã đặt rồi thả trên nó ─► GridManager.TryRemoveBlockAt: gỡ)
                       ├► GridManager.UpdateSelection (kéo) ─► DragSelection.MoveTo (kẹp, không phủ khối / Void)
                       │        ├► SelectionBoxView (mép trượt mượt), DragCountBubble, DragFeedback
                       │        └► BoardModel.Check == Ready ? (khung giãn xong) PlacedBlockView.ShowPreview
                       └► GridManager.EndSelection (thả tay) ─► BoardModel.Check
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
          Chưa Ready (sai luật)            Ready (hợp luật: đặt luôn, không hỏi lời giải)
          khung co xám, onInvalidMove      BoardModel.Place (giảm khóa) ─► onBlockPlaced, onClueUnlocked
                                                 │ lưới kín?
                                   ┌─────────────┴──────────────┐
                                   ▼                            ▼
                       đúng hết: onGameWin              còn khối sai: PlayWrong + onMistake
                       ─► LevelManager: Won              (người chơi chạm để gỡ)
                       ─► GameFlowUI: WinPopup ─► NextLevel

LevelTimer hết giờ ─► LevelManager: OutOfTime ─► MoreTimePopup ─► Revive (thêm giờ, Playing)
                                                              └► GiveUp ─► Lost ─► LosePopup ─► Retry
```

## Event

**Giữa module** (`EventBus<T>`, struct trong `Core/Contracts/Events`):

| Event | Phát | Nghe (chính) |
| --- | --- | --- |
| `LevelLoadedEvent` | `LevelManager` | HUD (`LevelHUD`, `BoosterButton`...), `PlayButtonView`, `ScreenFlow`, `BoosterManager`, `LevelIntroSequencer`, `GameFlowUI` (đóng popup) |
| `GameStateChangedEvent` | `LevelManager` | `GameFlowUI`, `BoosterManager`, `EnergyManager` (Lost → −1), `RefillEnergyPopup`, `LevelTimer`, `GameAudioEvents`, `GameHapticEvents` |
| `LevelCompletedEvent`, `LevelFailedEvent` | `LevelManager` | `ProfileStatsRecorder`, `DailyChallengeManager` |
| `AllLevelsCompletedEvent` | `LevelManager` | `GameFlowUI` |
| `StarsChangedEvent` | `LevelStars` | `TimeStarBar`, `GameAudioEvents` |
| `CoinsChangedEvent` / `StarsBalanceChangedEvent` | `CoinWallet` / `StarWallet` | `ProfileStatsRecorder`, `GameAudioEvents` / `LuckyWheelButton`, `LuckyWheelPopup` |
| `BoosterUsedEvent` | `BoosterManager` | `ProfileStatsRecorder` |
| `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent` | `BoosterTutorial` | `BoosterButton`; `FreezeTutorialDemo` |
| `ScreenChangedEvent` | `ScreenFlow` | (dành cho hệ thống mới) |
| `ShopOpenRequestedEvent` | `CoinHUD`, `MenuShortcutButton`, `RefillEnergyPopup` | `ScreenFlow` |

**Trong module** (`UnityEvent` / `event Action`, bên nghe cùng module hoặc Game):

| Nguồn | Event | Nghe |
| --- | --- | --- |
| `GridSpawner` | `gridCellEvent` | `GridManager`, `LevelTimer`, `LevelStars`, `BoardCameraFitter`, `DragFeedback` |
| `GridManager` | `onBlockPlaced`, `onBlockRemoved`, `onMistake`, `onGameWin`, `onSelectionChanged`, `onDragCellChanged`, `onClueUnlocked`, `onInvalidMove` | `LevelManager` (thắng), `GameplayFeedback`, `BlockFeedback`, `HintMarker`, `TutorialController`, `LevelStatsRecorder`, `GameAudioEvents`, `GameHapticEvents` |
| `LevelTimer` | `onTimeChanged`, `onTimeUp`, `onTimeWarning`, `onLowTick`, `onTimeAdded`, `onFreezeChanged`, `onFreezeEnded` | `LevelManager` (hết giờ), `TimerUI`, `TimeStarBar`, `LowTimeVignette`, Freeze UI, âm thanh / rung |
| `BoosterManager` | `onBuyRequested`, `onModeChanged`, `onBoosterUsed`, `onTutorialChanged`, `TutorialPressed` | `GameFlowUI` (`BuyBoosterPopup`), `BoosterButton`, `RevealBanner`, `BoosterUseFx`, `BoosterTutorial` |
| `BoosterInventory` | `Changed` | `BoosterButton` |
| `CoinWallet`, `StarWallet` | `Changed` | `CoinHUD`, view trong Economy |
| `EnergyManager` | `onEnergyChanged`, `onTick` | `EnergyHUD`, `RefillEnergyPopup` |
| `DailyRewardManager` / `DailyChallengeManager` | `onChanged`, `onClaimed` / `Changed` | Popup, nút, badge của feature đó |
| `PopupView` | `Closed` | `GameFlowUI` |
| `MenuTabBar` | `TabClicked` | `MainMenuNavigator` |

---

## Quy ước

### Tọa độ lưới

- `x` = cột (trái → phải), `y` = hàng (**dưới → trên**), gốc (0, 0) ở góc dưới-trái.
- Ô `(x, y)` nằm tại world `((x - (w-1)/2) * 1.2, (y - (h-1)/2) * 1.2)`, lưới căn giữa gốc tọa độ (`GridSpawner.CellSpacing = 1.2`, `GridGeometry`).
- ShikaCat đếm hàng từ trên xuống; importer lật: `y = rows - 1 - row`.

### Thứ tự vẽ (sortingOrder, world)

| Order | Thành phần |
| --- | --- |
| −2, −1 | Nền `bg` |
| 0–8 | Cell (SortingGroup; nền 0, mũi tên 5, số 6, ổ khóa 6–8) |
| 9 | Khung gợi ý Hint |
| 20 / 21 / 22 / 23 / 24 | Thân khối / mặt mèo / số / tai / viền đủ value (`Ring`) |
| 26 | Hạt bụi khi đặt khối |
| 30 | Khung đang kéo (`SelectionBoxView`) |
| 40–41 | Bong bóng số ô |
| 60 | Chữ khen |
| 80 | Bàn tay tutorial |

### Dữ liệu level (`LevelDefinition`)

| Trường | Ý nghĩa |
| --- | --- |
| `size`, `height` | Số cột, số hàng (`height = 0` nghĩa là lưới vuông) |
| `cellOverrides` | Ô `Void` (không chơi được) |
| `clues[]` | `x`, `y`, `value`, `color`, `mechanic { constraint, lockSteps, hidden }` |
| `solution[]` | Lời giải tham khảo (`clueId`, `x`, `y`, `width`, `height`) – solver không phụ thuộc vào nó |
| `rules.timeLimit` | Giây của level (0 = tự tính theo `LivesEnergyConfig`) |
| `difficulty` | Normal / Hard / Very Hard (`DifficultyTheme`) |
