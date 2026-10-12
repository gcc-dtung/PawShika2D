# 01 – Lưới, kéo thả, kiểm tra nước đi

## Tính năng làm gì

Người chơi kéo 1 hình chữ nhật trên lưới. Vùng hợp luật thì hiện mèo xem trước; thả tay thì game hỏi solver "đặt khối này lưới còn giải được không?" → còn: đặt khối; hết: mèo xám rung, trừ 1 mạng. Phủ kín lưới là thắng.

## File liên quan

| Lớp | File | Việc |
| --- | --- | --- |
| Input | `Gameplay/Input/InputManager` | Đọc chuột / chạm |
| | `Gameplay/Input/DragAndDropController` | Raycast tìm ô bắt đầu; đổi vị trí ngón tay sang world; đang Reveal thì chuyển chạm cho `BoosterManager.HandleCellTap` |
| | `Gameplay/Input/DragSelection` | **Model** vùng đang kéo: không phủ khối đã đặt / Void, vuốt nhanh vẫn đi qua từng ô |
| Điều phối | `Gameplay/Core/GridManager` | Nối input ↔ `BoardModel` ↔ view; phát event; trừ mạng |
| Luật | `Gameplay/Core/BoardModel` | `Check(rect)` → `PlacementResult`; `IsSolvableWith`; `Place`; `IsFull` |
| | `Gameplay/Core/CellModel`, `ClueCell` | Dữ liệu 1 ô / 1 ô số (khóa, ẩn, hướng) |
| | `Gameplay/Solver/ShikakuSolver` | Backtracking (chọn clue ít lựa chọn nhất trước – MRV) |
| Sinh lưới | `Gameplay/Core/GridSpawner` | Tạo / tái sử dụng `CellView`, bỏ ô Void, hiệu ứng xuất hiện |
| | `Gameplay/Core/GridGeometry` | Ô ↔ world (`CellSpacing` 1.2) |
| View | `Gameplay/Core/CellView` | Số, ổ khóa, ô nền hướng; đích raycast |
| | `Gameplay/Input/SelectionBoxView` | Khung màu theo ngón tay, giãn mượt (DOTween) |
| | `Gameplay/Block/PlacedBlockView` | Khối mèo: xem trước → `Confirm` / `PlayReject` / `PlayWinFlash` |
| | `Gameplay/Input/DragCountBubble` | Bong bóng đếm số ô |

## Luồng 1 nước đi

```text
Chạm ô ─► GridManager.BeginSelection   (ô Void / đã có khối / IsInputLocked → bỏ qua)
Kéo   ─► GridManager.UpdateSelection ─► DragSelection.MoveTo ─► SelectionBoxView.Show
                                     └► BoardModel.Check == Ready → chờ khung giãn xong → mèo xem trước
Thả   ─► GridManager.EndSelection ─► BoardModel.Check(rect)
          ├ Ignored (chạm 1 ô trống)          → không làm gì
          ├ BlockedCell / ClueCount / Locked / WrongShape / WrongSize → onInvalidMove (KHÔNG trừ mạng)
          └ Ready → BoardModel.IsSolvableWith(rect)?
                 ├ không → PlacedBlockView.PlayReject + onMistake + LivesManager.LoseLife
                 └ có    → BoardModel.Place (giảm khóa; "?" giữ ẩn) → onBlockPlaced → IsFull? → onGameWin
```

`BoardModel.Check` kiểm tra theo thứ tự (dừng ở lỗi đầu tiên):

1. Vùng chứa ô Void / ô đã có khối → `BlockedCell`
2. Không có hoặc nhiều hơn 1 ô số → `ClueCount`
3. Ô số còn khóa → `Locked`
4. Sai hướng ngang / dọc / vuông → `WrongShape`
5. Sai số ô (ô "?" bỏ qua bước này) → `WrongSize`

### Vì sao không so với lời giải lưu sẵn?

17/205 level ShikaCat có **nhiều hơn 1 lời giải**. Game chỉ hỏi "lưới còn giải được không" (`ShikakuSolver.IsSolvable`), nên mọi cách đặt nằm trong một lời giải nào đó đều đúng. Hệ quả: khối đã đặt **không bao giờ sai**, nên không có Undo / xóa khối.

Solver có giới hạn `MaxNodes = 500000`; vượt giới hạn thì coi như "còn lời giải" để không phạt oan người chơi.

## Khi nào lưới bị khóa thao tác

`GridManager.IsInputLocked` = đúng khi bất kỳ điều sau đúng: đã thắng, hết mạng, đang chờ khung giãn sau khi thả, lưới đang hiện, hoặc **`InputGate.IsLocked`**.

`InputGate` (`Gameplay/Input/InputGate`) là khóa theo "chủ khóa": ai cần chặn thì `InputGate.Lock(this)`, xong thì `Unlock(this)`. Đang khóa bởi:

| Chủ khóa | Khi nào |
| --- | --- |
| `LevelManager` | Ván không ở `Playing` (popup đang mở, Menu, thắng / thua) |
| `LevelIntroSequencer` | Từ lúc nạp level tới khi các intro (băng HARD, popup cơ chế, tutorial booster) chạy xong |
| `BoosterManager` | Reveal đang tự đặt lần lượt từng khối |

`InputGate.Describe()` in ra ai đang khóa (tiện khi debug lưới "bị đơ").

> Thêm một màn che / hiệu ứng mới cần chặn kéo thả: gọi `InputGate.Lock(this)` / `Unlock(this)` (nhớ Unlock trong `OnDisable`), không sửa `GridManager`.

## Event

| Event (`GridManager`) | Khi nào | Ai nghe |
| --- | --- | --- |
| `onBlockPlaced(BlockModel)` | Đặt đúng | `GameplayFeedback` (chữ khen), `GameAudioEvents`, `TutorialController` |
| `onMistake(center, size)` | Đặt sai lời giải | `GameplayFeedback` (rung), `GameAudioEvents` |
| `onInvalidMove(result, center, size)` | Sai luật, chỉ hủy | (chưa dùng – chỗ để thêm phản hồi "sai số ô"…) |
| `onClueUnlocked(CellModel)` | Ổ khóa về 0 | `GameAudioEvents` |
| `onGameWin` | Phủ kín lưới | `LevelManager`, `TutorialController` |

## Booster / tutorial đặt khối thế nào

`GridManager.PlaceSolved(rect)` đặt thẳng 1 khối của lời giải (không qua kéo thả, không hỏi solver lại). Hint, Reveal, Debug "Win now" đều dùng hàm này.

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Đổi cảm giác kéo (tốc độ giãn khung, ease, khoảng hở) | Inspector `SelectionBoxView` (`resizeDuration`, `resizeEase`, `inset`, `blockedExtend`) |
| Đổi kiểu báo sai (mèo xám / viền đỏ) | `PlacedBlockView.rejectStyle` |
| Thêm phản hồi khi thao tác sai luật | Nghe `GridManager.onInvalidMove` (đã có `PlacementResult` để biết lý do) |
| Thêm luật đặt khối mới | `BoardModel.Check` + `ShikakuSolver.BuildCandidates` (phải khớp nhau) – xem [HowTo_ThemMechanic](HowTo_ThemMechanic.md) |
| Đổi kích thước ô / khoảng cách | `GridSpawner.CellSpacing`, prefab `Prefabs/Cell.prefab` |

## Lưu ý

- Tọa độ: `x` trái → phải, `y` **dưới → trên**, gốc (0, 0) góc dưới-trái. ShikaCat đếm hàng từ trên xuống, importer đã lật.
- Khối mèo, chữ khen lấy từ `ObjectPool` – đừng `Instantiate` / `Destroy` trong lúc chơi.
- Luật trong `BoardModel.Check` và trong solver **phải giống nhau**; lệch nhau sẽ ra lỗi "hiện mèo nhưng thả ra bị trừ mạng" hoặc ngược lại.
