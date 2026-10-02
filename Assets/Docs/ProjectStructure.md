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
    │       ├── PlacedBlock.prefab      Thân, Number, Face, Ears
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
│   ├── Core/       GridManager, GridSpawner, Cell, ClueCell
│   ├── Level/      LevelManager, LevelDefinition, LevelDatabase
│   ├── Lives/      LivesManager
│   ├── Block/      PlacedBlock, CatFaceAnimator, ColorType
│   ├── Input/      InputManager, DragAndDropController, SelectionBox, DragCountBubble
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

### Gameplay/Core

| File | Vai trò |
| --- | --- |
| `GridSpawner` | Sinh `Cell` từ `LevelDefinition`, bỏ ô Void, phát `gridCellEvent` |
| `GridManager` | Nhận vùng kéo, kiểm tra hợp lệ, gọi solver, tạo khối, giảm khóa, kiểm tra thắng |
| `Cell` | Một ô lưới: vị trí, khối đang chiếm; sở hữu `ClueCell` và bật/tắt các object con có sẵn trong prefab (số, ổ khóa, mũi tên) |
| `ClueCell` | Class C# thuần (không phải MonoBehaviour): giá trị, số bước khóa, ẩn/hiện, hướng mũi tên |

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
| `PlacedBlock` | Khối mèo: thân 9-slice, tai, số, mặt, hiệu ứng bật |
| `CatFaceAnimator` | Chạy chuỗi khung hình mặt mèo trên SpriteRenderer |
| `ColorType` | Enum 10 màu + bảng màu RGB |

### Gameplay/Input, Feedback

| File | Vai trò |
| --- | --- |
| `InputManager` | Đọc chuột/chạm, chuyển cho `DragAndDropController` |
| `DragAndDropController` | Raycast tìm ô, quản lý kéo, cập nhật bong bóng |
| `SelectionBox` | Khung màu mờ phủ vùng đang kéo |
| `DragCountBubble` | Bong bóng số ô bám ngón tay |
| `GameplayFeedback` | Chữ khen khi đúng; chớp đỏ, rung camera, rung máy khi sai |

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
SelectionBox           SelectionBox + SpriteRenderer
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

Khối mèo, chữ khen, vùng chớp đỏ không đặt sẵn trong scene: `GridManager` và `GameplayFeedback` tạo chúng từ prefab qua `ObjectPool` ở lần dùng đầu, sau đó tái sử dụng. Ô lưới cũng được `GridSpawner` tái sử dụng giữa các level.

---

## Luồng một nước đi

```text
InputManager ─► DragAndDropController ─► GridManager.OnDrawGrid ─► SelectionBox
                       │                          (kéo)
                       └► GridManager.OnSelect (thả tay)
                              │
              ┌───────────────┼──────────────────────────────┐
              ▼               ▼                              ▼
     Không hợp lệ        ShikakuSolver: còn lời giải?    Hợp lệ + còn lời giải
     onInvalidMove            │ không                        │
                              ▼                              ▼
                     onMistake + LivesManager.LoseLife   CreatePlacedBlock
                              │                          StepLocks, onBlockPlaced
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
| `GridSpawner` | `gridCellEvent(List<Cell>)` | `GridManager`, `LivesManager` |
| `GridManager` | `onBlockPlaced(PlacedBlock)` | `GameplayFeedback` |
| `GridManager` | `onMistake(center, size)` | `GameplayFeedback` |
| `GridManager` | `onInvalidMove(center, size)` | (chưa dùng) |
| `GridManager` | `onClueUnlocked(Cell)` | (chưa dùng, dành cho âm thanh/hiệu ứng mở khóa) |
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
| 30 | SelectionBox (khung đang kéo) |
| 40–41 | Bong bóng số ô |
| 50 | Vùng chớp đỏ khi sai |
| 60 | Chữ khen |

### Dữ liệu level (`LevelDefinition`)

| Trường | Ý nghĩa |
| --- | --- |
| `size`, `height` | Số cột, số hàng (`height = 0` nghĩa là lưới vuông) |
| `cellOverrides` | Ô `Void` (không chơi được) |
| `clues[]` | `x`, `y`, `value`, `color`, `mechanic { constraint, lockSteps, hidden }` |
| `solution[]` | Lời giải tham khảo (`clueId`, `x`, `y`, `width`, `height`) – solver không phụ thuộc vào nó |
| `rules.mistakeLimit` | Hiện không dùng; số mạng lấy từ `LivesManager.maxLives` |
