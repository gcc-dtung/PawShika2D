# 04 – Sửa tai mèo, level test, kiểm tra level, ô nền điều hướng

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `67574be` "Tai mèo, level test, kiểm tra level, ô nền điều hướng" (nhánh `Day-1`) |
| Thời gian push | 2026-10-02 20:52 |
| Cập nhật lần cuối | 2026-10-02 20:53 |

## Tóm tắt

| Giờ | Đợt |
| --- | --- |
| 19:52 | Tai mèo nhỏ lại, chỉ chạm chân khối bên trên và vẽ đè lên nó |
| 19:58 | Level test gom mọi cơ chế (level 206) |
| 20:46 | Giữ cơ chế "?" cũ, bộ kiểm tra level (không cần đoán), hướng Square + ô nền điều hướng |

---

## 19:52 – Tai mèo nhỏ lại và vẽ đè lên khối bên trên

### Vấn đề

- Tai rộng 0,62 ô (sprite 82×23) nên cao khoảng 0,17 ô, trong khi khe giữa hai khối chỉ có 0,04 ô → tai cắm sâu vào khối bên trên.
- Ears có `Sorting Order = 19`, thấp hơn Body (20) → bị thân khối bên trên che.
- Game gốc: đỉnh tai vừa chạm chân con mèo bên trên và vẽ đè lên.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `PlacedBlock.prefab` | Ears: `Sorting Order` 19 → 23, z 0.01 → -0.01 |
| Sửa | `PlacedBlock.cs` | Thêm hằng `BodyFill = 0.96f` (thay số viết cứng ở `Init`) |
| Sửa | `PlacedBlock.cs` | `SetupEars`: đỉnh tai = mép trên khối + khe `(1 - BodyFill)` + `earOverlap`; phần còn lại lún vào thân khối |
| Thêm | `PlacedBlock.cs` | `[SerializeField] earWidth = 0.36f`, `earOverlap = 0f` (tỉ lệ theo ô, chỉnh được trong Inspector) |

### Tác dụng

- Tai nhỏ hơn (cao khoảng 0,1 ô), không còn cắm vào khối bên trên.
- Tai vẽ trên thân mọi khối; Face (21) và Number (22) nằm giữa ô nên không bị đè.
- Đổi khe hở chỉ cần sửa `BodyFill`, tai tự khớp theo.

### Kiểm tra

Unity biên dịch lại: không có lỗi. Chụp màn hình level test (19:58): tai nhỏ, đỉnh tai chạm chân khối trên và vẽ đè lên.

---

## 19:58 – Level test gom mọi cơ chế

### Mục tiêu

Có 1 level để thử nhanh mọi cơ chế hiện có thay vì phải tìm trong 205 level ShikaCat (vốn không có Void, mũi tên, khóa, ô ẩn).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Editor/Level/TestLevelBuilder.cs` | Menu `Tools/PawBlock/Create Test Level (All Mechanics)` tạo/cập nhật level và thêm vào cuối `LevelDatabase` |
| Thêm | `Levels/Test/Level_Test_AllMechanics.asset` | Level 6×7, 14 khối, lời giải duy nhất |
| Sửa | `ShikaCatLevelImporter.cs` | Nhập lại level ShikaCat vẫn giữ các level trong `Levels/Test` ở cuối database |
| Sửa | `LevelDatabase.asset` | Thêm level test ở index 205 (level 206) |

Bố cục (y = 6 trên cùng, `#` = Void):

```text
y6  A A B B B #      A 4      B 3 (→)
y5  A A C D D D      C 3 (↑)  D 6 (?)
y4  E E C D D D      E 4
y3  E E C F G G      F 2 (↑)  G 4 (khóa 3)
y2  H H H F G G      H 3
y1  I J J J J K      I 2      J 4 (→)  K 2
y0  I L L M N K      L 2      M 1      N 1
```

Cơ chế có trong level: lưới không vuông, ô Void, mũi tên ngang/dọc, ổ khóa, ô ẩn `?`, khối 1 ô, khối xếp chồng (xem tai mèo), nước đi sai trừ mạng, thắng.

### Kiểm tra

Play level 206, gọi `GridManager.OnSelect` theo kịch bản:

| Thao tác | Kết quả |
| --- | --- |
| Chọn G khi còn khóa | Hủy, không trừ mạng |
| B kéo dọc (sai mũi tên) | Hủy |
| Vùng chứa ô Void | Hủy |
| K = (4,1)-(5,1) | Đúng luật nhưng hết lời giải → trừ 1 mạng (3 → 2) |
| Đặt A, B, C rồi chọn G | Khóa đã mở, đặt được |
| D (ô ẩn, 6 ô) | Đặt được, lộ số 6 |
| Đặt nốt 14 khối | Thắng |

Đã đặt lại `PawBlock_CurrentLevel` = 12 sau khi thử.

### Ghi chú

- `LevelRules.mistakeLimit` chưa được dùng: số mạng lấy từ `LivesManager.maxLives`.

---

## 20:46 – Đánh giá cơ chế, kiểm tra level, ô nền điều hướng

### Quyết định

| Cơ chế | Kết luận |
| --- | --- |
| Khóa | Giữ. Chưa biết số và hướng; mọi vùng chứa ô khóa bị hủy; đặt đủ số khối thì mở và hiện số |
| `?` | **Giữ cơ chế cũ**: có số thật, người chơi phải suy ra từ khối xung quanh; đặt sai kích thước mà lưới hết lời giải thì trừ mạng; đặt đúng thì hiện số. Kính lúp (sau này) soi số thật |
| Điều hướng | Giữ, là gợi ý. Thêm dạng vuông. Đổi mũi tên đè lên số thành ô nền sau con số (giống game gốc) |
| Khóa + `?` | Cấm (mở khóa xong vẫn không biết số) |

Điều kiện để `?` và khóa công bằng: level phải **giải được không cần đoán** khi chưa biết số của `?` và ô còn khóa.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `LevelDefinition.cs` | `ClueConstraint.Square = 3` |
| Sửa | `ShikakuSolver.cs` | `MatchesConstraint` thêm Square (w == h). Overload `TrySolve(..., unknownValue, unknownConstraint, out solution, out aborted)`: clue chưa biết số có thể rộng bất kỳ, lời giải phải phủ kín ô trống (có cắt nhánh: ô trống không còn khối nào phủ được). `GetCandidates`. Đường chạy trong game không đổi |
| Thêm | `Editor/Level/LevelValidator.cs` | Menu `Tools/PawBlock/Validate Levels`. Kiểm tra data (trong lưới, không trùng, không trên Void, tổng số = số ô, hướng hợp lệ với số, khóa + `?`, số bước khóa) + có lời giải + mô phỏng không cần đoán |
| Sửa | `TestLevelBuilder.cs` | A thành Square; tạo xong tự chạy kiểm tra |
| Sửa | `Cell.cs` | `arrow` → `orientationBadge`: ô nền 9-slice cao / vuông / dẹt sau con số |
| Sửa | `Cell.prefab` | Object `Arrow` → `OrientationBadge`, sprite `WhiteBlock` (Sliced), scale 0.25 |

Mô phỏng "không cần đoán": lặp lại — tìm 1 clue (không còn khóa) chỉ có đúng 1 vùng đặt mà lưới còn lời giải (với `?` và ô khóa coi như chưa biết số) → đặt, giảm mọi khóa 1. Không tìm được trước khi đặt hết = phải đoán.

### Kiểm tra

- `Validate Levels`: 206/206 level hợp lệ.
- Level sai cố ý (dựng trong bộ nhớ từ level 206), đều bị bắt:

| Sửa | Báo |
| --- | --- |
| L và M đều `?` | Phải đoán sau 12/14 khối |
| G khóa 14 bước | Khóa 14 bước nhưng chỉ có 13 khối khác |
| G khóa + `?` | Vừa khóa vừa `?` |
| K Square, số 2 | 2 không phải số chính phương |
| M dọc, số 1 | Số 1 không thể Vertical |
| A, B, E khóa 10 bước | Phải đoán sau 7/14 khối |

- Lỗi tìm được trong lúc làm: khi `?` là khối cuối, solver không kiểm tra phủ kín (vì không còn clue chưa biết nào mở) → báo nhầm "phải đoán". Sửa: kiểm tra phủ kín khi level có bất kỳ clue chưa biết số nào.
- Play level 206: ô nền hiện đúng (4 vuông, 3 và 4 dẹt, 3 và 2 cao), số dễ đọc. `PawBlock_CurrentLevel` đặt lại về 0 (giá trị trước khi thử).
