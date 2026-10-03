# Cấu trúc dự án

## Thư mục

```text
Assets/
├── Docs/                               Tài liệu (README, TaskBreakdown, file này)
├── Scenes/
│   └── SampleScene.unity               Scene chơi duy nhất
├── Settings/                           URP 2D (Renderer2D, UniversalRP)
├── TextMesh Pro/                       Font LiberationSans SDF đang dùng
└── _Project/
    ├── Script/                         Code game, chia theo Gameplay / UI / Editor (chi tiết bên dưới)
    ├── Prefabs/
    │   ├── Cell.prefab                 Ô lưới: Visual (CellBG), Text (TMP), Arrow, Lock (Shackle, Body, LockText), BoxCollider2D
    │   └── Gameplay/                   Prefab dùng với ObjectPool (không tạo GameObject lúc chơi)
    │       ├── PlacedBlock.prefab      Thân, Number, Face, Ears, Ring (viền đủ value, tắt sẵn)
    │       ├── Praise.prefab           Chữ khen
    │       ├── MistakeFlash.prefab     Vùng chớp đỏ khi sai (WhiteBlock 9-slice)
    │       └── DragCountBubble.prefab  Bong bóng số ô (1 instance đặt sẵn trong scene)
    ├── Levels/
    │   ├── LevelDatabase.asset         Danh sách 205 level theo thứ tự chơi
    │   └── ShikaCat/Level_001..205     LevelDefinition sinh từ JSON ShikaCat
    ├── Resources/
    │   └── GameplayVisuals.asset       Sprite khối, tai, mặt mèo, ổ khóa, chữ khen...
    ├── SO_LevelDefinition.asset        Level mẫu 4x4, chỉ dùng khi scene không có LevelManager
    ├── GameAssets/                     Asset trích từ Paw Block (xem README.md trong đó)
    │   ├── 01_Audio  02_Cat_Characters_Spine  03_Gameplay_Blocks_Grid
    │   ├── 04_UI_Graphics  05_VFX_Particles  06_Fonts  07_Animations
    │   └── 08_Prefabs  09_Shaders_Materials  10_Data_Configs  11_Original_Atlases_Textures
    ├── ShikaCat_GameAssets/            Asset trích từ ShikaCat, gồm Level_Data/shikacat-levels.json
    └── Texture2D/                      Texture rời trích từ ShikaCat (panel, icon, nút, mèo...)
```

---

## Script

```text
Script/
├── Gameplay/
│   ├── Core/       GridManager, GridSpawner, GridGeometry, BoardModel, CellModel, CellView, ClueCell
│   ├── Level/      LevelManager, GameState, LevelDefinition, LevelDatabase
│   ├── Lives/      LivesManager
│   ├── Economy/    CoinWallet, RewardedAds
│   ├── Booster/    BoosterDatabase (+ BoosterType, BoosterData), BoosterInventory, BoosterManager
│   ├── Energy/     EnergyModel, EnergyManager
│   ├── Block/      BlockModel, PlacedBlockView, CatFaceAnimator, ColorType
│   ├── Input/      InputManager, DragAndDropController, DragSelection, SelectionBoxView, DragCountBubble
│   ├── Solver/     ShikakuSolver
│   ├── Feedback/   GameplayFeedback
│   ├── Visuals/    GameplayVisuals, BackgroundFitter, PlayZone
│   └── Util/       Singleton
├── UI/
│   ├── Common/     SafeAreaZone, BackgroundCover
│   ├── HUD/        LevelHUD, LivesUI, BoosterButton, RevealBanner, ShieldIndicator, EnergyHUD
│   ├── Menu/       ScreenFlow, MainMenuNavigator, MenuTabButton, PlayButtonView, CoinHUD, MenuShortcutButton
│   └── Popups/     PopupView, GameFlowUI, WinPopup, ReviveLivesPopup, LosePopup, ConfirmReplayPopup, BuyBoosterPopup, RefillEnergyPopup
└── Editor/         Chỉ chạy trong Editor (tên thư mục "Editor" là bắt buộc)
    ├── Level/      ShikaCatLevelImporter
    ├── UI/         PopupBuilder, BoosterBuilder, EnergyBuilder, MainMenuBuilder
    └── Visuals/    GameplayVisualsSetup
```

Quy tắc đặt file mới:
- Code chạy trong màn chơi, không phải Canvas → `Gameplay/<tính năng>/`. Tính năng mới (Booster, Audio...) tạo thư mục riêng cùng cấp.
- Code điều khiển Canvas / popup → `UI/<nhóm>/`, ví dụ `UI/Popups/` cho WinUI, LoseUI, ReviveLivesPopup ở Giai đoạn 3.
- Công cụ Editor → `Editor/<tính năng>/`.
- Dự án chưa dùng `.asmdef`, mọi script nằm chung `Assembly-CSharp`; di chuyển file không cần sửa `using`.
- **Animation dùng DOTween, không dùng coroutine.** Mỗi tween được giữ trong field (`Tween` / `Sequence`), gắn `SetLink(gameObject)`, và bị `Kill()` trong `OnDisable` (kèm khi phát lại / trả về pool). Callback `OnKill` chỉ xóa field nếu field vẫn trỏ đúng tween đó (Kill gọi trong callback có thể bị DOTween hoãn). Cái gì cần trả về trạng thái gốc khi bị hủy (camera, tim, chữ khen về pool) thì làm trong `OnKill`. Popup dùng `SetUpdate(true)` để chạy theo thời gian thực.
- **Tách logic và view:** dữ liệu và luật là class C# thuần tên `…Model` (không biết gì về SpriteRenderer / transform); phần hiển thị là MonoBehaviour tên `…View` (chỉ đọc model để vẽ, không tự quyết định luật). `GridManager` nối hai bên. Polish chỉ cần sửa file `…View`.

### Gameplay/Core

| File | Vai trò |
| --- | --- |
| `GridSpawner` | Sinh `CellView` + `CellModel` từ `LevelDefinition`, bỏ ô Void, phát `gridCellEvent`; hiệu ứng xuất hiện (ô nảy lần lượt thứ tự ngẫu nhiên, rồi số hiện dần; `IsIntroPlaying` khóa thao tác) |
| `GridManager` | Điều phối: nhận vùng kéo → hỏi `BoardModel` → điều khiển `SelectionBoxView` / `PlacedBlockView`, trừ mạng, phát event. Vùng đủ điều kiện: chờ khung giãn hết vào vùng mới hiện mèo; thả tay khi khung chưa giãn xong thì khóa thao tác, chờ giãn xong rồi mới đặt / báo sai |
| `GridGeometry` | Struct C# thuần: tọa độ ô / vùng → world; `WorldToCell` (kẹp trong lưới) |
| `BoardModel` | **Logic** C# thuần: các ô, các khối đã đặt, `Check(rect)` → `PlacementResult`, gọi solver, đặt khối, giảm khóa, kiểm tra kín lưới |
| `CellModel` | **Logic** C# thuần: vị trí, `ClueCell`, khối đang chiếm; event `Changed` khi ô số đổi |
| `CellView` | **View**: bật/tắt object con có sẵn trong prefab (số, ổ khóa, ô nền điều hướng); là đích raycast khi kéo; `PlayIntro` / `StopIntro` (nảy + hiện dần, DOTween) |
| `ClueCell` | **Logic** C# thuần: giá trị, số bước khóa, ẩn/hiện, hướng |

### Gameplay/Level, Lives, Solver

| File | Vai trò |
| --- | --- |
| `LevelManager` | Nạp level từ `LevelDatabase`, lưu tiến độ, chuyển level, zoom camera; giữ `GameState` (`Pause`/`Resume`, `Revive`, `GiveUp`), phát `onStateChanged` sau `stateChangeDelay` 0.8 s. Scene không có `GameFlowUI` thì tự chuyển như cũ |
| `GameState` | Enum: Playing, Paused, Won, OutOfLives, Lost. Chỉ Playing nhận thao tác kéo |
| `LevelDefinition` | ScriptableObject level: kích thước, ô Void, clue, lời giải, luật |
| `LevelDatabase` | ScriptableObject chứa danh sách level |
| `LivesManager` | 3 mạng, trừ / hồi mạng, báo hết mạng; khiên (`ActivateShield`, chặn 1 lần `LoseLife`, mất khi `ResetLives`) |
| `ShikakuSolver` | Backtracking (MRV). `IsSolvable` để phát hiện nước sai; `TrySolve` cho Hint/Reveal |

### Gameplay/Economy

| File | Vai trò |
| --- | --- |
| `CoinWallet` | Static, coin lưu PlayerPrefs `PawBlock_Coins`: `Add`, `TrySpend`, event `Changed` |
| `RewardedAds` | Quảng cáo có thưởng, đặt trên `Manager`. Hiện giả lập: chờ 0.5 s rồi trả thành công. Thay thân `ShowRoutine` khi gắn SDK |

### Gameplay/Booster

| File | Vai trò |
| --- | --- |
| `BoosterDatabase` | ScriptableObject + `BoosterType` (Hint 0, Shield 1, Reveal 2) + `BoosterData`. Tên field giữ như bản gốc; asset ở `GameAssets/10_Data_Configs/BoosterDatabase.asset` |
| `BoosterInventory` | Static, số lượt lưu PlayerPrefs `PawBlock_Booster_{type}`; lần đầu tặng `valueStart`; event `Changed` |
| `BoosterManager` | Trên `Manager`. `Press(type)`: chưa mở → bỏ qua; hết lượt → `onBuyRequested`; Hint đặt khối nhỏ nhất của lời giải; Khiên bật `LivesManager`; Reveal vào chế độ chọn số (`HandleCellTap` từ `DragAndDropController`), đặt lần lượt mọi khối cùng số (DOTween, cách 0.12 s, `IsBusy` khóa lưới). Chỉ trừ lượt khi có tác dụng. `Buy(type)` trừ coin |

### Gameplay/Energy

| File | Vai trò |
| --- | --- |
| `EnergyModel` | **Logic** C# thuần (nhận `now` từ ngoài): năng lượng, mốc bắt đầu đếm hồi, hạn vô hạn; `Refresh` cộng bù theo thời gian đã trôi, `TryConsume`, `Add`, `AddInfinite`, `SecondsToNext` |
| `EnergyManager` | Trên `Manager`. Tối đa 5, hồi 1/1800 s; nghe `GameState.Lost` → −1; lưu PlayerPrefs; tick mỗi giây (`onTick`) cho đồng hồ, `onEnergyChanged`. Context menu Debug: Set 0 / Set Full / Skip 1 refill / Infinite 5 min |

### Gameplay/Block

| File | Vai trò |
| --- | --- |
| `BlockModel` | **Logic** C# thuần: vùng, các ô chiếm, ô số, màu (= màu khung chọn của lượt kéo tạo ra khối) |
| `PlacedBlockView` | **View** khối mèo: thân 9-slice, tai, số, mặt, viền `Ring`. Trạng thái: xem trước (đặc ngay, viền màu mèo thu bên trong 0.2 s) → `Confirm` (bật) hoặc `PlayReject` (rung, co nhỏ + mờ; `rejectStyle`: `GrayOut` mèo xám – mặc định / `RedRing` viền đỏ ngoài mép / `GrayAndRedRing`) |
| `CatFaceAnimator` | Chạy chuỗi khung hình mặt mèo trên SpriteRenderer |
| `ColorType` | Enum 10 màu + bảng màu RGB |

### Gameplay/Input, Feedback

| File | Vai trò |
| --- | --- |
| `InputManager` | Đọc chuột/chạm, chuyển cho `DragAndDropController` |
| `DragAndDropController` | Raycast tìm ô lúc bắt đầu kéo; khi kéo đổi vị trí ngón tay sang world rồi chuyển cho `GridManager`; cập nhật bong bóng |
| `DragSelection` | **Logic** C# thuần của vùng đang kéo: không bao giờ phủ lên khối đã đặt / ô Void (bị chặn thì giữ vùng hợp lệ trước đó), vuốt nhanh thì đi qua từng ô ở giữa; `BlockDir` = mép nào đang bị ngón tay đẩy vào vật cản |
| `SelectionBoxView` | Khung màu mờ phủ vùng đang kéo, random 1 `ColorType` mỗi lượt kéo (mèo xem trước và khối đặt ra dùng đúng màu này); đổi ô thì giãn / co tới vùng mới trong `resizeDuration` 0.15 s (`resizeEase` OutQuad, tween DOTween, chỉnh trong Inspector), giãn xong báo `WhenSettled`; mờ đi khi đang hiện mèo xem trước. Bình thường ôm sát ô (`inset` 0.1); bị chặn (khối, Void, mép lưới) thì mép phía đó trượt sang lấp khe, dừng ngay trước vật cản (`blockedExtend` 0.1) |
| `DragCountBubble` | Bong bóng số ô bám ngón tay |
| `GameplayFeedback` | Chữ khen khi đúng; rung camera, rung máy khi sai |

### Gameplay/Visuals, Util

| File | Vai trò |
| --- | --- |
| `GameplayVisuals` | ScriptableObject gom sprite; nạp qua `Resources.Load("GameplayVisuals")` |
| `BackgroundFitter` | Nền world (`bg`) đi theo camera; scale trong scene là tối thiểu: đã phủ kín camera thì giữ, chưa kín thì phóng vừa đủ |
| `Singleton` | Base class `Singleton<T>` và `DontDestroyOnLoadSingleton<T>` |

### UI/HUD

| File | Vai trò |
| --- | --- |
| `LevelHUD` | Chữ `LEVEL n`, nút chơi lại |
| `LivesUI` | Hàng tim trên Canvas |
| `BoosterButton` | 1 nút booster: icon, số lượt (`btn_number`), dấu `+` (`btn_add_0`), ổ khóa "LV n", quầng sáng xoay khi đang bật; bấm → `BoosterManager.Press` |
| `RevealBanner` | "TAP A NUMBER TO REVEAL" + X (`khung_reveal2`, `revel_X`), đè lên thanh booster khi đang chọn số |
| `ShieldIndicator` | Khung băng `ice_effect_2` phủ hàng tim khi có khiên; khiên chặn lỗi thì phồng to rồi mờ |
| `EnergyHUD` | Icon tim (`icon_hear_2`), số năng lượng, đồng hồ / FULL / ∞; có `infiniteSprite` (ô ở Home) thì khi vô hạn đổi sang tim ∞ (`icon_hear_infinity2`) và ẩn số; bấm khi chưa đầy → `GameFlowUI.OpenEnergyRefill` |

### UI/Menu

| File | Vai trò |
| --- | --- |
| `MainMenuNavigator` | Trên `Canvas_Menu`. Panel xếp ngang theo thứ tự tab, đổi tab thì trượt (`duration` 0.3 s, `ease` OutCubic); tab chọn rộng `selectedWidth` 408, nền `home_tab1` trượt theo, tab khác chia đều; xong thì tắt panel khuất màn hình. Tab / nền tab chọn neo bottom center. `Select(index)`, `onTabChanged`; `LayoutInEditor` (chuột phải component) xếp sẵn trong Editor |
| `ScreenFlow` | Trên `Manager`. Menu: bật `Canvas_Menu`, tắt `Canvas_HUD`, `LevelManager.Pause` (cả khi level nạp lúc mở game). `Play()` → `GameFlowUI.RequestPlay` (kiểm tra năng lượng) → tắt menu, bật HUD, `LevelManager.Retry` (nạp lại level từ đầu). `ShowMenu()` (popup Thua → HOME). `onScreenChanged` |
| `PlayButtonView` | Nút PLAY: "Level n" theo `onLevelLoaded`, thở nhẹ (DOTween lặp, kill khi tắt) → `ScreenFlow.Play` |
| `CoinHUD` | Số coin (nghe `CoinWallet.Changed`, icon nảy); bấm → tab Shop |
| `SafeAreaZone` | UI Zone: neo RectTransform theo `Screen.safeArea` (chọn cạnh trên / dưới / trái / phải) để UI tránh tai thỏ, thanh home; chỉ chạy khi Play, tự cập nhật khi màn hình đổi |
| `BackgroundCover` | Ảnh nền UI phủ kín RectTransform cha: kích thước trong scene là tối thiểu, chưa kín thì phóng đều vừa đủ (tính cả lệch tâm) |
| `MenuShortcutButton` | Nút phụ trên Home (Starter, No Ads): nhún nhẹ lên xuống (DOTween), bấm → chuyển tab (mặc định Shop) |
| `MenuTabButton` | 1 tab: icon to 1.3x + nhô lên, hiện chữ khi được chọn; vạch ngăn `home_tab3` chỉ giữa 2 tab không chọn: đổi ngay khi bắt đầu trượt, vạch mới hiện dần cả quãng, vạch cũ mờ nhanh (`dividerHideRatio`) |

### UI/Popups

| File | Vai trò |
| --- | --- |
| `PopupView` | Base: CanvasGroup, nền tối `Dim` chắn bấm, `Panel` bật lên (scale 0.6 → 1), dùng thời gian thực |
| `GameFlowUI` | Trên `Canvas_Popup/Popups`. Nghe `onStateChanged`: Won → Win, OutOfLives → Hồi mạng, Lost → Thua; `RequestReplay` cho nút chơi lại; `RequestPlay` cho nút PLAY; `OpenEnergyRefill` (HUD / Menu). Hết năng lượng mà PLAY / Retry → `RefillEnergyPopup.ShowRequired` (X: PLAY → ở lại Menu, Retry → mở lại popup Thua) |
| `WinPopup` | `LEVEL n` / COMPLETED!, +10 coin; x2 (ads) hoặc Next → `NextLevel` |
| `ReviveLivesPopup` | Ads +1 mạng, 900 coin +3 mạng (mờ khi thiếu coin), X → `GiveUp` |
| `ConfirmReplayPopup` | Mở thì `Pause`; YES → `Retry`, NO → `Resume` |
| `BuyBoosterPopup` | Hết lượt booster: icon, mô tả, "x n", giá coin (mờ khi thiếu), X; mở thì `Pause` |
| `RefillEnergyPopup` | Nạp năng lượng: "n/5", "Next in mm:ss", ads +1, 900 coin nạp đầy. `ShowOptional` (từ HUD, có X) / `ShowRequired(onReady, onCancel)` (có năng lượng thì tự đóng rồi gọi `onReady`; có `onCancel` thì hiện X); tạm dừng ván khi mở |
| `LosePopup` | FAILED, "−1 🐟"; RETRY (qua `GameFlowUI` kiểm tra năng lượng), HOME → `ScreenFlow.ShowMenu` |

### Editor

| File | Menu | Việc làm |
| --- | --- | --- |
| `Level/ShikaCatLevelImporter` | Tools → PawBlock → Import ShikaCat Levels | JSON → `LevelDefinition` + `LevelDatabase` |
| `Visuals/GameplayVisualsSetup` | Tools → PawBlock → Setup Gameplay Visuals | 9-slice khối, cắt atlas mặt mèo, tạo `GameplayVisuals.asset` |
| `UI/PopupBuilder` | Tools → PawBlock → Build Popups | Tạo `Canvas_Popup` (Overlay 1080x1920, sortingOrder 20) nếu chưa có, xóa rồi dựng lại `Popups` trong đó (xóa cả bản cũ trong `Canvas_HUD`), nối `GameFlowUI` cho LevelHUD / EnergyHUD / ScreenFlow, thêm `RewardedAds` vào `Manager` |
| `UI/MainMenuBuilder` | Tools → PawBlock → Build Main Menu | `Canvas_Menu`: CanvasScaler 1080x1920, sortingOrder 10; `Panels` (ShopPanel tạm, HomePanel + `PlayButton`, `Starter`, `NoAds`, `TopBar` (Avatar, Energy, Coin, Settings – ô kem `khung_pill_kem`), ProgressPanel tạm, mỗi panel `RectMask2D`); `AlwayOn/TabBar`; `MainMenuNavigator`; `ScreenFlow` trên `Manager`; chữ viền dùng preset của `GameFontSetup` |
| `UI/UIZoneBuilder` | Tools → PawBlock → Build UI Zone | `Canvas_HUD/UIZone` (4 cạnh) chứa PlayZone, TopPanel, DownPanel, RevealBanner; gọi Build Main Menu để dựng `HomePanel/UIZone` (cạnh trên). Builder HUD tìm theo `PopupBuilder.HudRoot` (UIZone nếu có) |
| `UI/PlayZoneBuilder` | Tools → PawBlock → Build Play Zone | Dựng `Canvas_HUD/PlayZone` (giãn theo màn hình, lề trên 300 / dưới 310 / hai bên 30), gán cho `LevelManager.playZone` |
| `UI/GameFontSetup` | Tools → PawBlock → Apply Game Font | Fredoka Bold làm font mặc định TMP (fallback LiberationSans), gán shader `TMP_SDF-Mobile` theo đường dẫn, tạo preset viền `Fredoka-Bold SDF - Outline Purple / Green / Brown`, đổi font mọi chữ LiberationSans trong scene và prefab `Assets/_Project` |
| `UI/EnergyBuilder` | Tools → PawBlock → Build Energy | Dựng `TopPanel/Energy` (EnergyHUD) góc trên phải, thêm `EnergyManager` vào `Manager` |
| `UI/BoosterBuilder` | Tools → PawBlock → Build Boosters | Dựng phần con của 3 nút booster, `RevealBanner`, `TopPanel/LevelFrame/Shield`; thêm `BoosterManager` vào `Manager` |

---

## Scene `SampleScene`

```text
Main Camera            Camera orthographic, LevelManager chỉnh size/vị trí
Global Light 2D
SpawnGrid              GridSpawner
Manager                GridManager, InputManager, DragAndDropController,
                       LivesManager, LevelManager, GameplayFeedback, RewardedAds, BoosterManager,
                       EnergyManager, ScreenFlow
SelectionBox           SelectionBoxView + SpriteRenderer
bg                     SpriteRenderer + BackgroundFitter
Canvas_HUD             Canvas Overlay, CanvasScaler 1080x1920, sortingOrder 0; ScreenFlow bật khi vào màn chơi, tắt ở Menu
└── UIZone             SafeAreaZone (4 cạnh): mọi UI của HUD nằm trong safe area
    ├── PlayZone       PlayZone + Image (tắt, bật `showZone` để xem vùng): lưới luôn nằm trọn trong vùng này
    ├── TopPanel       LevelHUD
    │   ├── ReplayButton
    │   ├── LevelFrame / LevelText, Hearts (LivesUI), Shield (ShieldIndicator, khung băng)
    │   └── Energy     EnergyHUD (Bg khung_refill, Heart, Count, Timer)
    ├── DownPanel/Booster  Booster1..3: BoosterButton (Hint, Shield, Reveal) / Glow, Icon, Count, Add, Lock
    └── RevealBanner   RevealBanner + CanvasGroup (ẩn khi không chọn số)
Canvas_Popup           Canvas Overlay 1080x1920, sortingOrder 20, luôn bật (dùng chung Menu và màn chơi)
└── Popups             GameFlowUI (dựng bằng Build Popups)
    ├── WinPopup  ReviveLivesPopup  LosePopup  ConfirmReplayPopup  BuyBoosterPopup  RefillEnergyPopup   (tắt sẵn)
    └── mỗi popup: Dim, Panel (popup_1 x1.6) / Title, nội dung, nút
Canvas_Menu            Canvas Overlay 1080x1920, sortingOrder 10 (trên HUD), MainMenuNavigator
├── Panels             ShopPanel | HomePanel | ProgressPanel (trượt ngang, RectMask2D)
│   └── HomePanel      Background (BackgroundCover), PlayButton,
│                      UIZone (SafeAreaZone cạnh trên): Starter, NoAds,
│                      TopBar (Avatar, Energy: EnergyHUD tim + số, đồng hồ, "+"; Coin: CoinHUD; Settings) – chỉ ở tab Home
└── AlwayOn            Lớp luôn hiện (không chặn bấm)
    └── TabBar         1080x200 neo bottom center: Background (home_tab2), SelectedTab (home_tab1), Tab_Shop / Tab_Home / Tab_Progress (neo bottom center)
EventSystem
DragCountBubble        Instance của prefab, để tắt sẵn; DragAndDropController bật khi kéo
```

Khối mèo, chữ khen không đặt sẵn trong scene: `GridManager` và `GameplayFeedback` tạo chúng từ prefab qua `ObjectPool` ở lần dùng đầu, sau đó tái sử dụng. Ô lưới cũng được `GridSpawner` tái sử dụng giữa các level.

---

## Luồng một nước đi

```text
InputManager ─► DragAndDropController ─► GridManager.BeginSelection (chạm 1 ô trống)
                       │
                       ├► GridManager.UpdateSelection (kéo) ─► DragSelection.MoveTo (kẹp, không phủ khối / Void)
                       │                    ├► SelectionBoxView.Show (mép trượt mượt) / SetBlocked (mép bị chặn lấp khe)
                       │                    └► BoardModel.Check == Ready ? (chờ khung giãn xong) PlacedBlockView.ShowPreview
                       └► GridManager.EndSelection (thả tay) ─► BoardModel.Check (Ready mà khung chưa giãn xong: chờ)
                              │
              ┌───────────────┼──────────────────────────────┐
              ▼               ▼                              ▼
     Chưa Ready          Ready, BoardModel.IsSolvableWith?  Ready + còn lời giải
     onInvalidMove            │ không                        │
     (PlacementResult)        ▼                              ▼
                     PlacedBlockView.PlayReject          BoardModel.Place (giảm khóa)
                     onMistake + LivesManager.LoseLife   PlacedBlockView.Confirm, onBlockPlaced
                              ▼                              │ phủ kín lưới?
                     LivesUI, GameplayFeedback               ▼
                              │ hết mạng                  onGameWin
                              ▼                              ▼ (0.8 s)
                     onOutOfLives ─(0.8 s)► OutOfLives   Won ─► WinPopup ─► NextLevel
                                 ReviveLivesPopup ─► Revive (Playing, giữ lưới)
                                                 └─► GiveUp ─► Lost ─► LosePopup ─► Retry
```

## Event chính

| Nguồn | Event | Người nghe |
| --- | --- | --- |
| `GridSpawner` | `gridCellEvent(List<CellView>)` | `GridManager`, `LivesManager` |
| `GridManager` | `onBlockPlaced(BlockModel)` | `GameplayFeedback` |
| `GridManager` | `onMistake(center, size)` | `GameplayFeedback` |
| `GridManager` | `onInvalidMove(PlacementResult, center, size)` | (chưa dùng, dành cho phản hồi khi thả sai luật) |
| `GridManager` | `onClueUnlocked(CellModel)` | (chưa dùng, dành cho âm thanh/hiệu ứng mở khóa) |
| `GridManager` | `onGameWin` | `LevelManager` |
| `LivesManager` | `onLivesChanged(current, max)`, `onLifeLost(current)` | `LivesUI` |
| `LivesManager` | `onOutOfLives` | `LevelManager` |
| `LevelManager` | `onLevelLoaded(number)` | `LevelHUD`, `GameFlowUI` |
| `LevelManager` | `onStateChanged(GameState)` | `GameFlowUI`, `BoosterManager`, `EnergyManager` (Lost → −1), `RefillEnergyPopup` |
| `LivesManager` | `onShieldChanged(bool)`, `onShieldBlocked` | `BoosterButton`, `ShieldIndicator` |
| `BoosterManager` | `onBuyRequested(type)` | `GameFlowUI` → `BuyBoosterPopup` |
| `BoosterManager` | `onRevealModeChanged(bool)`, `onBoosterUsed(type)` | `BoosterButton`, `RevealBanner` |
| `BoosterInventory` | `Changed(type, count)` (event C#) | `BoosterButton` |
| `EnergyManager` | `onEnergyChanged(current, max)`, `onTick` | `EnergyHUD`, `RefillEnergyPopup` |

---

## Quy ước

### Tọa độ lưới

- `x` = cột (trái → phải), `y` = hàng (**dưới → trên**), gốc (0, 0) ở góc dưới-trái.
- Ô `(x, y)` nằm tại world `((x - (w-1)/2) * 1.2, (y - (h-1)/2) * 1.2)`, lưới căn giữa gốc tọa độ.
- `GridSpawner.CellSpacing = 1.2`.
- ShikaCat đếm hàng từ trên xuống; importer lật: `y = rows - 1 - row`.

### Thứ tự vẽ (sortingOrder)

| Order | Thành phần |
| --- | --- |
| 0 | Cell (SortingGroup; bên trong: nền 0, mũi tên 5, số 6, ổ khóa 6–8) |
| 19 | Tai mèo |
| 20 | Thân khối |
| 21 | Mặt mèo |
| 22 | Số trên khối |
| 24 | Viền đủ value (`Ring`) |
| 30 | SelectionBoxView (khung đang kéo) |
| 40–41 | Bong bóng số ô |
| 60 | Chữ khen |

### Dữ liệu level (`LevelDefinition`)

| Trường | Ý nghĩa |
| --- | --- |
| `size`, `height` | Số cột, số hàng (`height = 0` nghĩa là lưới vuông) |
| `cellOverrides` | Ô `Void` (không chơi được) |
| `clues[]` | `x`, `y`, `value`, `color`, `mechanic { constraint, lockSteps, hidden }` |
| `solution[]` | Lời giải tham khảo (`clueId`, `x`, `y`, `width`, `height`) – solver không phụ thuộc vào nó |
| `rules.mistakeLimit` | Hiện không dùng; số mạng lấy từ `LivesManager.maxLives` |
