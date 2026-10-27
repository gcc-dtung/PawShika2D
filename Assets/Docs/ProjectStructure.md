# Cấu trúc dự án

> Tổ chức theo mẫu dự án Snacky_Dash (2026-10-25): mọi thứ của game trong `Assets/_Project`, chia theo **loại** (code / dữ liệu / prefab / nguồn) rồi mới tới mảng tính năng; 3 assembly. Quy ước riêng: [Refactor/ProjectConventions.md](Refactor/ProjectConventions.md).
> Chi tiết từng mảng (phụ thuộc, class chính, cách nối): [Modules/](Modules/) (bảng dưới).

## Mảng code và chiều phụ thuộc

```text
Core/Contracts  ←  Core/Foundation  ←  UI/Framework  ←  Gameplay, Economy, Shop, Profile, Settings, LiveOps/*  ←  App, UI/Home
(event, interface,   (EventBus, Save,     (PopupView,      (luật từng mảng)                                       (GameBootstrap, ScreenFlow,
 enum dùng chung)     Audio, Clock, Ads…)  widget UI)                                                              GameFlowUI, Main Menu)
```

- Mũi tên = "được dùng bởi". Mảng tính năng **không** tham chiếu mảng tính năng khác; giao tiếp qua event / interface trong `Runtime/Core/Contracts`.
- `App` là nơi duy nhất biết mọi mảng: tạo service, gọi `Init` cho từng component (`GameBootstrap`); màn hình trộn nhiều mảng ở `Prefabs/UI/Screens`.
- Toàn bộ code runtime chung 1 assembly (`PawBlock.Runtime`) nên compiler **không** còn chặn tham chiếu sai chiều: tự kiểm khi review. Namespace giữ như cũ (`PawBlock.Features.<F>`, `PawBlock.Shared`, `PawBlock.Game`…) để nhìn `using` là biết phụ thuộc; xem [Refactor/Dependencies.md](Refactor/Dependencies.md).

| Mảng (`Runtime/…`) | Namespace | Nội dung | Tài liệu |
| --- | --- | --- | --- |
| `Core/Contracts` | `PawBlock.Core.Contracts` | 13 event (`IGameEvent`), 10 interface, 5 type dùng chung | — (xem bảng dưới) |
| `Core/Foundation` | `PawBlock.Core.Foundation` | Hạ tầng không dính luật game: EventBus, Save, Audio, Clock, Ads / IAP, InputGate, Haptics, Util | — |
| `UI/Framework` | `PawBlock.Shared` | `PopupView`, `TutorialPopup`, `MenuPanel`, widget / helper UI | — |
| `Gameplay` | `PawBlock.Features.Gameplay` | Lõi màn chơi | [Gameplay](Modules/Gameplay.md) |
| `Economy` | `PawBlock.Features.Economy` | Ví coin / sao, năng lượng | [Economy](Modules/Economy.md) |
| `Shop` | `PawBlock.Features.Shop` | Gói IAP | [Shop](Modules/Shop.md) |
| `Profile` | `PawBlock.Features.Profile` | Hồ sơ người chơi, chỉ số | [Profile](Modules/Profile.md) |
| `Settings` | `PawBlock.Features.Settings` | Popup cài đặt (nhạc / âm / rung) | — |
| `Teams` | `PawBlock.Features.Teams` | Đội giả lập offline: danh sách, tìm, tạo, vào / rời | [Teams](Modules/Teams.md) |
| `LiveOps/DailyReward` | `PawBlock.Features.DailyReward` | Điểm danh 7 ngày, trao quà / icon quà | [DailyReward](Modules/DailyReward.md) |
| `LiveOps/DailyChallenge` | `PawBlock.Features.DailyChallenge` | Level mỗi ngày, lịch tháng, quà mốc | [DailyChallenge](Modules/DailyChallenge.md) |
| `LiveOps/LuckyWheel` | `PawBlock.Features.LuckyWheel` | Vòng quay tiêu sao | [LuckyWheel](Modules/LuckyWheel.md) |
| `LiveOps/WeeklyContest` | `PawBlock.Game` | Cuộc thi tuần (`WeeklyContest`, bot `WeeklyBots`) | [10](Features/10_UI-Menu-Popup.md) |
| `UI/Home` | `PawBlock.Game` | Main Menu: tab, Home, Leaderboard, nút khóa, hồn ma | [10](Features/10_UI-Menu-Popup.md) |
| `App` | `PawBlock.Game` | Composition root, luồng màn hình, popup flow, config gốc, âm thanh theo event | — (mục App dưới) |
| `Save`, `Cheat` | `PawBlock.Game` | Migrate save v1 → v2; cheat trong bản build | — |

Assembly: `PawBlock.Runtime` (`Runtime/`), `PawBlock.Editor` (`Runtime/Editor/`, chỉ Editor), `PawBlock.Tests` (`Runtime/Tests/`, EditMode, 192 test).

---

## Thư mục

```text
Assets/
├── _Project/
│   ├── Data/
│   │   ├── Config/      GameConfig, AudioDatabase, BoosterDatabase, DifficultyTheme, EconomyConfig, GameplayVisuals,
│   │   │                LivesEnergyConfig, ShopDatabase, TeamsDatabase, AvatarDatabase, DailyRewardDatabase, DailyChallengeDatabase, LuckyWheelDatabase
│   │   └── Levels/      LevelDatabase, LevelDifficultySettings, MechanicInjectorSettings; Custom/ ShikaCat/ Mechanic/ Test/ (Generated/ do tool tạo)
│   ├── Prefabs/
│   │   ├── InGame/      Cell, PlacedBlock, DragCountBubble, MistakeFlash, Praise, Wall, Freeze/
│   │   └── UI/
│   │       ├── Popups/  mọi popup (Win, Lose, MoreTime, QuitLevel, BuyBooster, MechanicIntro, Settings, Profile, DailyReward,
│   │       │            RefillEnergy, LuckyWheel, Notice, TutorialPopup (base) + WeeklyTutorialPopup (variant))
│   │       ├── Screens/ Home/ (MainMenu, các tab: Shop / Leaderboard / Home / TeamsPanel / Collection, HomeScene, GhostMascot, LockedMenuButton, MenuTabBar; DailyPanel giữ cho liveops)  Game/ (GameplayHUD)
│   │       └── Widgets/ Button_Primary, CloseButton, SettingsButton; <Mảng>/ (CoinHUD, EnergyHUD, BoosterButton, thẻ Shop, ô Daily…)
│   ├── Runtime/         PawBlock.Runtime.asmdef
│   │   ├── Core/        Contracts/ (Events, Interfaces, Types)  Foundation/ (Audio, Clock, Events, Feedback, Input, Save, Services, Util)
│   │   ├── Gameplay/    Analytics, Block, Board, Booster, Config, Feedback, Input, Intro, Level, Save, Solver, Stars, Timer, Tutorial, UI, Visuals
│   │   ├── Economy/  Shop/  Profile/  Settings/  Teams/
│   │   ├── LiveOps/     DailyReward/  DailyChallenge/  LuckyWheel/  WeeklyContest/
│   │   ├── UI/          Framework/ (dùng chung)  Home/ (Main Menu)
│   │   ├── App/         GameBootstrap, GameServices, GameScene, GameConfig, ScreenFlow, GameFlowUI, GameAudioEvents
│   │   ├── Save/  Cheat/
│   │   ├── Editor/      PawBlock.Editor.asmdef — App/ (Collector, DebugPanel, GameConfigSetup, AudioSetup)  Core/  Gameplay/ (Level tools, Builders)  UI/
│   │   └── Tests/       PawBlock.Tests.asmdef — App/  Core/  Gameplay/  Economy/  DailyReward/  DailyChallenge/  LuckyWheel/  Teams/
│   ├── Scenes/          Main.unity (scene duy nhất)
│   ├── Settings/        URP 2D, Presets/ (nén texture)
│   └── Source/          tài nguyên gốc
│       ├── Art/
│       │   ├── UI/      Common/ (SharedAtlas)  Home/ (GameAtlas; Backgrounds, Halloween, HalloweenRip (gitignore))  Gameplay/  DailyChallenge/  Economy/  Settings/  Shop/
│       │   ├── InGame/  Common/ (Grid, Cats)  Gameplay/ (Grid, Praise_Badges, VFX, Spine)  DailyChallenge/ (Grid, VFX)
│       │   ├── Avatars/ Fonts/
│       │   └── Atlases/ 7 Sprite Atlas (mỗi atlas vẫn gói đúng các thư mục cũ của nó)
│       ├── Audio/       BGM, Cat_Voices, Gameplay_SFX, UI_SFX, ShikaCat/
│       └── Effect/      Shaders/  Materials/ (Common, Gameplay)
├── ThirdParty/
│   ├── PawBlock/        Bản gốc Paw Block (01_Audio … 11_Original_Atlases_Textures, Texture2D): không sửa, không tham chiếu
│   └── ShikaCat/        Bản gốc ShikaCat (gồm Level_Data/shikacat-levels.json)
├── Plugins/             DOTween (Demigiant), plugin rung iOS
├── TextMesh Pro/        Font mặc định TMP
├── Resources/           Chỉ DOTweenSettings
└── Docs/                Tài liệu (file này, Features/, Modules/, Refactor/, RoadMap/)
```

Đặt file mới: code vào `Runtime/<mảng>`, config / level vào `Data/`, prefab vào `Prefabs/`, ảnh / âm / font / shader vào `Source/` (ảnh UI phải nằm dưới `…/Art/UI/` để preset nén ASTC 4x4 tự áp). Asset / atlas / preset: [ProjectConventions](Refactor/ProjectConventions.md) mục 13.

---

## Runtime/Core

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

## Runtime/UI/Framework

| Class | Vai trò |
| --- | --- |
| `PopupView` (abstract) | Base popup: mở / đóng có hiệu ứng, `OnOpened` / `OnClosed`, event `Closed`, `CloseOnLevelLoad` |
| `MenuPanel` (abstract) | Gốc 1 tab Main Menu: `Rect`, `SetSelected`, `OnShown` / `OnHidden` |
| `NoticePopup` | Popup thông báo chung |
| `ConfirmPopup` | Popup hỏi lại Có / Không (khung Halloween) |
| `ButtonPress`, `UIButtonPress`, `UIButtonSounds`, `NoClickSound`, `NoClickVibrate`, `ButtonPressIgnore`, `UIGrayscaleWhenDisabled` | Cảm giác bấm, âm / rung khi bấm, nút xám khi tắt |
| `SafeAreaZone`, `BackgroundCover`, `BackgroundFitter`, `NestedScrollRect` | Bố cục theo màn hình |
| `MenuShortcutButton`, `SettingsButton` | Nút phụ trên Home (mời bấm, mở Shop), nút bánh răng |
| `UIConfetti`, `TMPLetterPopIn`, `RewardText` | Hiệu ứng / chữ quà |
| `Editor/UIAutoSetup`, `Editor/GameFontSetup` | Gắn sẵn ButtonPress / danh sách nút; áp font chung |

## Runtime/App, UI/Home, Save, Cheat

| Class | Vai trò |
| --- | --- |
| `GameBootstrap` | Composition root: tạo `GameServices` (save, đồng hồ, ads, IAP, ví, kho, hồ sơ, Shop), gọi `Init` cho mọi component theo thứ tự |
| `GameServices`, `GameScene` | Service dùng chung; object scene cho Cheat / Debug |
| `GameConfig` | Config gốc (`Data/Config/GameConfig`) trỏ tới config của các mảng |
| `ScreenFlow` (`IScreenNavigator`) | Menu ↔ màn chơi, phát `ScreenChangedEvent`, mở tab Shop khi có `ShopOpenRequestedEvent` |
| `GameFlowUI` (`IMenuPopups`) | Nối `GameState` với popup, mở popup dùng chung, đóng popup khi đổi level |
| `GameAudioEvents` | Nối âm thanh vào event của game |
| `UI/Home/` | `MainMenuNavigator`, `MenuTabBar`, `MenuTabButton`, `MenuSwipe`, `ShopPanel`, `HomePanel`, `PlayButtonView`, `LeaderboardPanel` + `LeaderboardRow`, `LockedTabPanel`, `LockedMenuButton` |
| `Save/` | `SaveMigrator`, `SaveDataV1` (save v1 → v2) |
| `Cheat/` | `DebugCheats`, `CheatPanel` (trong bản build) |
| `Editor/App/` | `GameBootstrapCollector` (tự điền tham chiếu Bootstrap), `DebugPanelWindow`, `GameConfigSetup`, `AudioSetup` |

---

## Scene `Main`

Sắp theo mẫu Snacky_Dash: nhóm `[Managers]` trên cùng, rồi các nhóm ngăn cách `---- … ----` (object rỗng ở gốc toạ độ, chỉ để gom). Chỉ đổi cha, không đổi component, nên mọi tham chiếu giữ nguyên.

```text
[Managers]
├── GameBootstrap        GameBootstrap, SaveRunner, TrustedClockRunner
├── Manager              GridManager, InputManager, DragAndDropController, LevelTimer, LevelManager, GameplayFeedback,
│                        BoosterManager, EnergyManager, ScreenFlow, DailyRewardManager, TutorialController, BoosterTutorial,
│                        MechanicIntro, LevelIntroSequencer, FreezeTutorialDemo, BoardCameraFitter, LevelStatsRecorder,
│                        ProfileStatsRecorder, DragFeedback, BlockFeedback, HintMarker, LevelStars, DailyChallengeManager,
│                        CatFaceTicker, DailyLevelLoader
│   └── HintFrameTemplate, DustTemplate (tắt)   mẫu cho pool
└── AudioManager         AudioManager, GameAudioEvents, GameHapticEvents; Music + Sfx_0..7 (AudioSource đặt sẵn)
---- MÀN CHƠI ----
├── bg                   BackgroundFitter (nền Halloween `GameBG`; Pattern tắt)
├── SpawnGrid            GridSpawner (+ BoardFrame)
├── BoardDecor           BoardDecor: hồn ma ló sau góc trái, mạng nhện góc phải (bám góc khung theo cỡ lưới)
├── SelectionBox (tắt)   SelectionBoxView
├── DragCountBubble      [prefab] tắt sẵn, bật khi kéo
└── TutorialHand (tắt)   mẫu bàn tay (HintMarker nhân bản)
---- ĐIỀU KHIỂN ----
├── Main Camera          Camera orthographic (BoardCameraFitter chỉnh size / vị trí)
└── Global Light 2D
---- GIAO DIỆN ----
├── Canvas_HUD           [prefab GameplayHUD] HUD màn chơi, thanh booster, tutorial; LowTimeVignette, BoosterUseFx
├── Canvas_Menu          [prefab MainMenu] MainMenuNavigator; lồng ShopPanel / LeaderboardPanel / HomePanel / TeamsPanel / CollectionPanel / MenuTabBar
└── Canvas_Popup         luôn bật
    └── Popups           GameFlowUI; 13 instance popup prefab (tắt sẵn), mỗi popup Canvas + GraphicRaycaster riêng
EventSystem              InputSystemUIInputModule
```

Tool Editor tìm object theo tên trong **cả cây** (không chỉ gốc scene): `GameBootstrapCollector`, `AudioSetup`, `UIAutoSetup`.

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

**Giữa các mảng** (`EventBus<T>`, struct trong `Runtime/Core/Contracts/Events`):

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

**Trong một mảng** (`UnityEvent` / `event Action`, bên nghe cùng mảng hoặc App):

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
