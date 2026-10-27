# 01 – Lưới, kéo thả, kiểm tra nước đi

> Đường dẫn tính từ `Assets/_Project/Runtime/Gameplay/`. Tổng quan feature: [Gameplay README](../Modules/Gameplay.md).

## Tính năng làm gì

Người chơi kéo 1 hình chữ nhật trên lưới. Vùng hợp luật thì hiện mèo xem trước; thả tay là **đặt luôn** (cơ chế thời gian như bản gốc: không hỏi lời giải, không trừ gì). Đặt nhầm thì chạm vào khối để gỡ. Lưới kín và mọi khối đúng số ô là thắng; áp lực đến từ đồng hồ đếm ngược ([02](02_ThoiGian-VongDoiVan.md)).

## File liên quan

| Lớp | File | Việc |
| --- | --- | --- |
| Input | `Input/InputManager` | Đọc chuột / chạm (Input System mới) |
| | `Input/DragAndDropController` | Raycast tìm ô (registry `Collider2D → CellView` của `GridSpawner`); chạm rồi thả trên khối đã đặt → gỡ; đang Reveal thì chuyển chạm cho `BoosterManager` |
| | `Input/DragSelection` | **Model** vùng đang kéo: không phủ khối đã đặt / Void, vuốt nhanh vẫn đi qua từng ô |
| Điều phối | `Board/GridManager` | Nối input ↔ `BoardModel` ↔ view; phát event |
| Luật | `Board/BoardModel` | `Check(rect)` → `PlacementResult`; `Place`; `Remove`; `IsFull`; `IsSolved(wrong)`; `TrySolveHint` / `TrySolveRemaining` |
| | `Board/CellModel`, `Board/ClueCell` | Dữ liệu 1 ô / 1 ô số (khóa, ẩn, hướng) |
| | `Solver/ShikakuSolver` | Backtracking (clue ít lựa chọn nhất trước – MRV); dùng cho Hint / Reveal / kiểm tra level |
| Sinh lưới | `Board/GridSpawner` | Tạo / tái sử dụng `CellView`, bỏ ô Void, hiệu ứng xuất hiện, phát `gridCellEvent` |
| | `Board/GridGeometry` | Ô ↔ world (`CellSpacing` 1.2), ô "dính" dưới ngón tay |
| View | `Board/CellView` | Số, ổ khóa, ô nền hướng; ô nền đổi theo loại (`clueSprite` ô số, `mysterySprite` "?", `fuzzySprite` số mơ hồ, trống = sprite prefab); `HitBox` cho raycast |
| | `Input/SelectionBoxView` | Khung màu theo ngón tay, giãn mượt (DOTween), co xám khi thả sai |
| | `Block/PlacedBlockView` | Khối mèo: xem trước → `Confirm` / `PlayWrong` / `PlayRemove` / `PlayWinFlash` |
| | `Input/DragCountBubble` | Bong bóng đếm số ô |

## Luồng 1 nước đi

```text
Chạm ô ─► GridManager.BeginSelection   (ô Void / đã có khối / IsInputLocked → bỏ qua)
Kéo   ─► GridManager.UpdateSelection ─► DragSelection.MoveTo ─► SelectionBoxView, onSelectionChanged
                                     └► BoardModel.Check == Ready → chờ khung giãn xong → mèo xem trước
Thả   ─► GridManager.EndSelection ─► BoardModel.Check(rect)
          ├ Ignored (chạm 1 ô trống)                     → không làm gì
          ├ BlockedCell / ClueCount / Locked / WrongShape / WrongSize
          │                                              → khung co xám, onInvalidMove (không phạt)
          └ Ready → BoardModel.Place (giảm khóa; "?" giữ ẩn) → onBlockPlaced (+ onClueUnlocked)
                   → lưới kín? ├ IsSolved      → onGameWin (cả lưới lóe sáng)
                               └ còn khối "?" sai số ô → PlayWrong + onMistake (chạm để gỡ)
Chạm khối đã đặt ─► GridManager.TryRemoveBlockAt ─► BoardModel.Remove → onBlockRemoved (đứt combo)
```

`BoardModel.Check` kiểm tra theo thứ tự (dừng ở lỗi đầu tiên):

1. Vùng chứa ô Void / ô đã có khối → `BlockedCell`
2. Không có hoặc nhiều hơn 1 ô số → `ClueCount`
3. Ô số còn khóa → `Locked`
4. Sai hướng ngang / dọc / vuông → `WrongShape`
5. Sai số ô (ô "?" bỏ qua bước này) → `WrongSize`

### Vì sao không so với lời giải lưu sẵn?

17/205 level ShikaCat có **nhiều hơn 1 lời giải**, nên vùng hợp luật nào cũng được đặt. Khối đặt nhầm chỉ làm người chơi kẹt (không còn chỗ cho ô khác) và tốn thời gian gỡ — đó là "hình phạt" của cơ chế thời gian. Riêng ô "?" người chơi không biết số, nên kiểm tra số ô dời tới lúc lưới kín (`IsSolved`).

Solver giới hạn `MaxNodes = 500000`; vượt giới hạn thì coi như còn lời giải.

## Khi nào lưới bị khóa thao tác

`GridManager.IsInputLocked` đúng khi: đã thắng, hết giờ, đang chờ khung giãn sau khi thả, lưới đang hiện, hoặc **`InputGate.IsLocked`** (`Runtime/Core/Foundation/Input/InputGate`).

`InputGate` là khóa theo "chủ khóa": ai cần chặn thì `InputGate.Lock(this)`, xong thì `Unlock(this)`. Đang khóa bởi:

| Chủ khóa | Khi nào |
| --- | --- |
| `LevelManager` | Ván không ở `Playing` (popup đang mở, Menu, thắng / hết giờ / thua) |
| `LevelIntroSequencer` | Từ lúc nạp level tới khi các intro (băng HARD, popup cơ chế, tutorial booster) chạy xong |
| `BoosterManager` | Reveal đang tự đặt lần lượt từng khối |
| `BoosterUseFx`, `FreezeOverlayFX` | Hiệu ứng dùng booster đang chạy |

`InputGate.Describe()` in ra ai đang khóa (tiện khi debug lưới "bị đơ").

> Thêm một màn che / hiệu ứng mới cần chặn kéo thả: gọi `InputGate.Lock(this)` / `Unlock(this)` (nhớ Unlock trong `OnDisable`), không sửa `GridManager`.

## Event (`GridManager`, UnityEvent trong Gameplay)

| Event | Khi nào | Ai nghe |
| --- | --- | --- |
| `onBlockPlaced(BlockModel)` | Đặt khối | `GameplayFeedback` (chữ khen, combo), `BlockFeedback`, `HintMarker`, `TutorialController`, `LevelStatsRecorder`, `GameAudioEvents`, `GameHapticEvents` |
| `onBlockRemoved(BlockModel)` | Gỡ khối | `BlockFeedback`, `TutorialController`, `LevelStatsRecorder`, `GameAudioEvents`, `GameHapticEvents` |
| `onMistake(center, size)` | Lưới kín nhưng khối "?" sai số ô | `GameplayFeedback`, `GameAudioEvents`, `GameHapticEvents` |
| `onInvalidMove(result, center, size)` | Thả vùng sai luật | `GameAudioEvents` (DragCancel), `GameHapticEvents` (Soft); muốn phản hồi theo lý do thì đọc `PlacementResult` |
| `onClueUnlocked(CellModel)` | Ổ khóa về 0 | `GameAudioEvents` |
| `onSelectionChanged`, `onDragCellChanged` | Đang kéo | `DragFeedback`, `GameAudioEvents` (tick), `GameHapticEvents` |
| `onGameWin` | Thắng | `LevelManager`, `GameplayFeedback`, `TutorialController` |

## Booster / tutorial đặt khối thế nào

`GridManager.PlaceSolved(rect)` đặt thẳng 1 khối của lời giải (không qua kéo thả); khối sai đang đè lên vùng thì gỡ trước. Reveal và Debug "Win now" dùng hàm này; Hint chỉ **chỉ chỗ** (`HintMarker`), người chơi tự kéo.

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Đổi cảm giác kéo (tốc độ giãn khung, ease, khoảng hở) | Inspector `SelectionBoxView` (`resizeDuration`, `resizeEase`, `inset`, `blockedExtend`) |
| Ô "dính" dưới ngón tay, cửa sổ combo | Inspector `GridManager` (`cellStick`, `comboWindow`) |
| Thêm phản hồi theo lý do sai luật | Nghe `GridManager.onInvalidMove` (có `PlacementResult`) |
| Thêm luật đặt khối mới | `BoardModel.Check` + `ShikakuSolver` (phải khớp nhau) – xem [HowTo_ThemMechanic](HowTo_ThemMechanic.md) |
| Đổi kích thước ô / khoảng cách | `GridSpawner.CellSpacing`, prefab `Prefabs/InGame/Cell` |

## Lưu ý

- Tọa độ: `x` trái → phải, `y` **dưới → trên**, gốc (0, 0) góc dưới-trái. ShikaCat đếm hàng từ trên xuống, importer đã lật.
- Khối mèo, chữ khen lấy từ `ObjectPool` – đừng `Instantiate` / `Destroy` trong lúc chơi.
- Luật trong `BoardModel.Check` và trong solver **phải giống nhau**; lệch nhau thì Hint / Reveal chỉ ra vùng mà người chơi không kéo được.
