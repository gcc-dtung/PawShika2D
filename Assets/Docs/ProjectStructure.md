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
│   ├── Level/      LevelManager, LevelDefinition, LevelDatabase
│   ├── Lives/      LivesManager
│   ├── Block/      BlockModel, PlacedBlockView, CatFaceAnimator, ColorType
│   ├── Input/      InputManager, DragAndDropController, DragSelection, SelectionBoxView, DragCountBubble
│   ├── Solver/     ShikakuSolver
│   ├── Feedback/   GameplayFeedback
│   ├── Visuals/    GameplayVisuals, BackgroundFitter
│   └── Util/       Singleton
├── UI/
│   └── HUD/        LevelHUD, LivesUI
└── Editor/         Chỉ chạy trong Editor (tên thư mục "Editor" là bắt buộc)
    ├── Level/      ShikaCatLevelImporter
    └── Visuals/    GameplayVisualsSetup
```

Quy tắc đặt file mới:
- Code chạy trong màn chơi, không phải Canvas → `Gameplay/<tính năng>/`. Tính năng mới (Booster, Audio...) tạo thư mục riêng cùng cấp.
- Code điều khiển Canvas / popup → `UI/<nhóm>/`, ví dụ `UI/Popups/` cho WinUI, LoseUI, ReviveLivesPopup ở Giai đoạn 3.
- Công cụ Editor → `Editor/<tính năng>/`.
- Dự án chưa dùng `.asmdef`, mọi script nằm chung `Assembly-CSharp`; di chuyển file không cần sửa `using`.
- **Tách logic và view:** dữ liệu và luật là class C# thuần tên `…Model` (không biết gì về SpriteRenderer / transform); phần hiển thị là MonoBehaviour tên `…View` (chỉ đọc model để vẽ, không tự quyết định luật). `GridManager` nối hai bên. Polish chỉ cần sửa file `…View`.

### Gameplay/Core

| File | Vai trò |
| --- | --- |
| `GridSpawner` | Sinh `CellView` + `CellModel` từ `LevelDefinition`, bỏ ô Void, phát `gridCellEvent` |
| `GridManager` | Điều phối: nhận vùng kéo → hỏi `BoardModel` → điều khiển `SelectionBoxView` / `PlacedBlockView`, trừ mạng, phát event |
| `GridGeometry` | Struct C# thuần: tọa độ ô / vùng → world; `WorldToCell` (kẹp trong lưới) |
| `BoardModel` | **Logic** C# thuần: các ô, các khối đã đặt, `Check(rect)` → `PlacementResult`, gọi solver, đặt khối, giảm khóa, kiểm tra kín lưới |
| `CellModel` | **Logic** C# thuần: vị trí, `ClueCell`, khối đang chiếm; event `Changed` khi ô số đổi |
| `CellView` | **View**: bật/tắt object con có sẵn trong prefab (số, ổ khóa, ô nền điều hướng); là đích raycast khi kéo |
| `ClueCell` | **Logic** C# thuần: giá trị, số bước khóa, ẩn/hiện, hướng |

### Gameplay/Level, Lives, Solver

| File | Vai trò |
| --- | --- |
| `LevelManager` | Nạp level từ `LevelDatabase`, lưu tiến độ, chuyển level, zoom camera |
| `LevelDefinition` | ScriptableObject level: kích thước, ô Void, clue, lời giải, luật |
| `LevelDatabase` | ScriptableObject chứa danh sách level |
| `LivesManager` | 3 mạng, trừ / hồi mạng, báo hết mạng |
| `ShikakuSolver` | Backtracking (MRV). `IsSolvable` để phát hiện nước sai; `TrySolve` cho Hint/Reveal |

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
| `SelectionBoxView` | Khung màu mờ phủ vùng đang kéo, random 1 `ColorType` mỗi lượt kéo (mèo xem trước và khối đặt ra dùng đúng màu này); mép trượt mượt khi đổi ô, mờ đi khi đang hiện mèo xem trước. Bình thường ôm sát ô (`inset` 0.1); bị chặn (khối, Void, mép lưới) thì mép phía đó trượt sang lấp khe, dừng ngay trước vật cản (`blockedExtend` 0.1) |
| `DragCountBubble` | Bong bóng số ô bám ngón tay |
| `GameplayFeedback` | Chữ khen khi đúng; rung camera, rung máy khi sai |

### Gameplay/Visuals, Util

| File | Vai trò |
| --- | --- |
| `GameplayVisuals` | ScriptableObject gom sprite; nạp qua `Resources.Load("GameplayVisuals")` |
| `BackgroundFitter` | Giữ nền phủ kín camera khi zoom |
| `Singleton` | Base class `Singleton<T>` và `DontDestroyOnLoadSingleton<T>` |

### UI/HUD

| File | Vai trò |
| --- | --- |
| `LevelHUD` | Chữ `LEVEL n`, nút chơi lại |
| `LivesUI` | Hàng tim trên Canvas |

### Editor

| File | Menu | Việc làm |
| --- | --- | --- |
| `Level/ShikaCatLevelImporter` | Tools → PawBlock → Import ShikaCat Levels | JSON → `LevelDefinition` + `LevelDatabase` |
| `Visuals/GameplayVisualsSetup` | Tools → PawBlock → Setup Gameplay Visuals | 9-slice khối, cắt atlas mặt mèo, tạo `GameplayVisuals.asset` |

---

## Scene `SampleScene`

```text
Main Camera            Camera orthographic, LevelManager chỉnh size/vị trí
Global Light 2D
SpawnGrid              GridSpawner
Manager                GridManager, InputManager, DragAndDropController,
                       LivesManager, LevelManager, GameplayFeedback
SelectionBox           SelectionBoxView + SpriteRenderer
bg                     SpriteRenderer + BackgroundFitter
Canvas_HUD             Canvas Overlay, CanvasScaler 1080x1920
├── DownPanel/Booster  3 nút booster (chưa có logic)
└── TopPanel           LevelHUD
    ├── ReplayButton
    └── LevelFrame
        ├── LevelText
        └── Hearts     LivesUI
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
                       │                    └► BoardModel.Check == Ready ? PlacedBlockView.ShowPreview (mèo xem trước)
                       └► GridManager.EndSelection (thả tay) ─► BoardModel.Check
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
                              ▼                              │
                     onOutOfLives ─► LevelManager.Retry      ▼
                                                     LevelManager.NextLevel
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
| `LevelManager` | `onLevelLoaded(number)` | `LevelHUD` |

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
