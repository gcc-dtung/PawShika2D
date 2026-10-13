# 03 – Cơ chế (Mechanic): Void, Hướng, Khóa, "?"

> Code ở `Assets/_Project/Features/Gameplay/` (`Scripts/Board`, `Scripts/Solver`, `Scripts/Tutorial/MechanicIntro`, `Editor/Level`).

## Tổng quan

Dữ liệu ShikaCat gốc **không có** cơ chế nào. Cơ chế được gắn vào level bằng **Mechanic Injector** (Editor) hoặc tay trong **Level Editor**, lưu trong `LevelDefinition`.

Có 2 kiểu cơ chế, nằm ở 2 chỗ khác nhau trong dữ liệu:

| Kiểu | Lưu ở | Ví dụ |
| --- | --- | --- |
| Theo **Ô** (ảnh hưởng hình lưới) | `LevelDefinition.cellOverrides[]` (`CellType`) | Void |
| Theo **Ô số** (ảnh hưởng 1 clue) | `ClueData.mechanic` (`ClueMechanic`) | Hướng, Khóa, "?" |

```csharp
public struct ClueMechanic
{
    public ClueConstraint constraint; // None / Horizontal / Vertical / Square
    public int lockSteps;             // 0 = không khóa
    public bool hidden;               // true = hiện "?"
}
```

## Từng cơ chế

| Cơ chế | Người chơi thấy | Luật (`BoardModel.Check`) | Solver | Khi đặt khối đúng | Popup giới thiệu |
| --- | --- | --- | --- | --- | --- |
| **Void** | Ô bị bỏ, lưới có hình lạ | Vùng chứa Void → `BlockedCell` | Ô Void = `blocked` | – | Không |
| **Hướng** | Ô nền sau số: cao / vuông / dẹt | Sai hình → `WrongShape` | `MatchesConstraint` lọc ứng viên | – | "Orientation" |
| **Khóa** | Ổ khóa + số bước, che số và hướng | Còn khóa → `Locked` | (game thật: biết hết; Validator: chưa biết số / hướng) | **Mọi** ổ khóa −1; gỡ khối thì khóa lại (`ClueCell.SyncLock`: bước gốc − số khối khác trên lưới) | "Locked Number" |
| **"?"** | Dấu "?" thay số | Không kiểm tra số ô | Validator: chưa biết số, lời giải phải phủ kín lưới | Không đổi: luôn hiện "?" (cả trên khối mèo), số thật chỉ dùng để chấm khi lưới kín | "Hidden Number" |

### Ai làm gì cho mỗi cơ chế

```text
LevelDefinition (data) ─► ClueCell (trạng thái lúc chơi: LockSteps, IsHidden)
                          ├► BoardModel.Check / Place   (luật)
                          ├► ShikakuSolver              (lời giải, nước sai)
                          ├► CellView.Refresh           (hiển thị trên ô)
                          ├► PlacedBlockView.SetNumber  (số trên khối mèo)
                          └► MechanicIntro              (popup lần đầu gặp)
Editor: LevelEditorWindow (sửa tay) · LevelValidator (luật dữ liệu + "không cần đoán")
        MechanicInjector (gắn tự động) · LevelDifficultyAnalyzer (độ khó)
```

## Quy tắc "không cần đoán"

Khóa và "?" **giấu thông tin**. Nếu giấu sai chỗ, người chơi phải đoán, đặt nhầm rồi mất thời gian gỡ. Vì vậy `LevelValidator` mô phỏng người chơi:

1. Người chơi chưa biết số của ô "?" và ô còn khóa, chưa biết hướng của ô còn khóa.
2. Mỗi bước chỉ đặt khối **chắc chắn đúng** (mọi cách đặt khác của số đó làm lưới hết lời giải).
3. Kẹt giữa chừng → lỗi "Phải đoán". Level đó không được dùng.

Mechanic Injector chạy Validator sau mỗi lần gắn; sai thì hoàn tác và thử chỗ khác. Level gốc vốn phải đoán (nhiều lời giải) thì không bao giờ nhận Khóa / "?".

## Luật dữ liệu (Validator báo lỗi)

- Vừa khóa vừa "?" → cấm (mở khóa xong vẫn không biết số).
- `lockSteps` ≥ số clue → cấm (không đủ khối khác để mở).
- Ngang / Dọc trên số 1 → cấm. Vuông trên số không chính phương → cấm.

## Lịch xuất hiện (Mechanic Injector)

`Assets/_Project/Features/Gameplay/Configs/Levels/MechanicInjectorSettings.asset`:

| Cơ chế | Level giới thiệu | Số level giới thiệu | Số lượng / level |
| --- | --- | --- | --- |
| Void | 5 | 2 | 1–2 khối bỏ |
| Hướng | 8 | 2 | 2–3 số |
| Khóa | 12 | 2 | 1–2 số (2–5 bước) |
| "?" | 16 | 2 | 1–2 số |

Sau giai đoạn giới thiệu: 70 % level có cơ chế, level chia hết cho 10 là level nghỉ, từ level 30 có 25 % kết hợp 2 cơ chế, level Hard / Very Hard luôn 2 / 3 cơ chế (ưu tiên "?" → Khóa → Void, không dùng Hướng vì Hướng làm level dễ hơn). Chi tiết tool: [04 Level](04_Level-DuLieuVaCongCu.md).

## Popup giới thiệu (`MechanicIntro`)

`MechanicIntro` là 1 `ILevelIntro` (Order 10, sau băng HARD, trước tutorial booster). Sau khi lưới hiện xong, level có cơ chế người chơi chưa xem → popup (ván tạm dừng). Đã xem lưu ở section save `"gameplay"` (`GameplaySave.seenMechanics`, tên `MechanicIntro.Kind`). Void không có popup. Xem lại: Debug Panel → Xem lại popup cơ chế.

## Thêm cơ chế mới

Xem [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md) – có ví dụ đầy đủ và checklist. Trong code tìm `// [MECHANIC]`.
