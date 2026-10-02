# 05 – Tách logic / view, feel kéo, mèo xem trước, mèo rung đỏ khi sai

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-03 00:40 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 21:20 | Tách logic / view + feel kéo theo video ref | Logic là C# thuần `…Model`, hiển thị là `…View`; khung chọn giãn mượt; đủ value thì hiện mèo xem trước; thả sai lời giải thì mèo rung đỏ |
| 21:35 | Màu khối = màu khung chọn | Khung chọn màu nào thì mèo xem trước và khối đặt ra màu đó |
| 22:05 | Khung chọn không phủ khối / Void (theo `refdrag`) | Bị chặn thì giữ vùng hợp lệ trước đó, mép nảy nhẹ; ngón tay ngoài lưới vẫn kéo được |
| 22:30 | Thử giãn dây thun khi bị chặn | Không đúng ý, đã quay lại như 22:05 |
| 23:00 | Bị chặn: mép lấp khe rồi kẹt (theo hình vẽ, thử nghiệm) | Khung ôm sát ô; mép bị chặn trượt sang dừng trước khối; bỏ nảy |
| 23:30 | Feel đủ value 1: viền màu thu bên trong | Mèo xem trước đặc ngay (bỏ trong / thở / bật); viền màu mèo nở từ trong ra mép rồi tắt |
| 00:10 (03/10) | Đặt sai: viền đỏ bên ngoài thay cho nhuộm đỏ (thử cách A) | Màu mèo giữ nguyên; viền đỏ nháy khi rung; co nhỏ + mờ – chưa ưng |
| 00:40 (03/10) | Đặt sai: thử cách B – mèo chuyển xám | `rejectStyle` chọn được A / B / A+B trong Inspector, mặc định B |

---

## 21:20 – Tách logic / view, feel kéo theo video ref

### Mục tiêu

1. Tách riêng logic và view thành 2 script để giai đoạn polish chỉ cần sửa view.
2. Theo video `vidref`: khung chọn **giãn ra** sang ô mới thay vì nhảy (bị khựng). Vùng kéo đủ value thì **hiện mèo xem trước kèm hiệu ứng dù chưa thả tay**; thả tay là nhận luôn.
3. Theo video `refwrog`, nhưng thay khung chọn bằng hình khối mèo: đủ value, thả tay mà sai lời giải thì **mèo ngả đỏ, rung ngang rồi biến mất**, trừ 1 mạng.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Core/GridGeometry.cs` | Struct quy đổi ô / vùng → world (dùng chung spawner và view) |
| Thêm | `Gameplay/Core/BoardModel.cs` | **Logic** lưới: `Check(rect)` → `PlacementResult` (Ignored, Ready, BlockedCell, ClueCount, Locked, WrongShape, WrongSize), `IsSolvableWith`, `Place` (lộ số ẩn, chiếm ô, giảm khóa), `IsFull` |
| Thêm | `Gameplay/Core/CellModel.cs` | **Logic** ô: vị trí, `ClueCell`, khối chiếm; event `Changed` |
| Thêm | `Gameplay/Block/BlockModel.cs` | **Logic** khối: vùng, ô, ô số, màu |
| Đổi tên + sửa | `Cell.cs` → `Gameplay/Core/CellView.cs` | Chỉ còn hiển thị; `Bind(CellModel)` / `Unbind()` |
| Đổi tên + sửa | `PlacedBlock.cs` → `Gameplay/Block/PlacedBlockView.cs` | Chỉ còn hiển thị, dựng hình theo `GridGeometry`. Thêm `ShowPreview` (trong 85 %, "thở"), `Confirm` (bật), `PlayReject` (đỏ → rung 0.45 s → mờ 0.15 s) |
| Đổi tên + sửa | `SelectionBox.cs` → `Gameplay/Input/SelectionBoxView.cs` | `Show(center, size)` + `SmoothDamp` tâm và kích thước (mép giãn mượt, 0.05 s); `SetFilled` mờ khung khi đang có mèo xem trước |
| Sửa | `Gameplay/Core/GridManager.cs` | Chỉ điều phối: kéo → `Check` → preview; thả → `Confirm` / `PlayReject` + trừ mạng / `onInvalidMove(PlacementResult, …)` |
| Sửa | `Gameplay/Core/GridSpawner.cs` | Tạo `CellModel` cho mỗi ô, dùng `GridGeometry` |
| Sửa | `Gameplay/Feedback/GameplayFeedback.cs` | Bỏ vùng chớp đỏ (thay bằng mèo đỏ); giữ rung camera + rung máy; khen theo `BlockModel` |
| Sửa | `Gameplay/Block/CatFaceAnimator.cs` | `SetColor` để tô đỏ / làm mờ mặt mèo |
| Sửa | `DragAndDropController`, `LivesManager`, `ClueCell` | Đổi type sang `CellView`, `CancelSelection` |
| Sửa | `Docs/ProjectStructure.md`, `Docs/README.md` | Quy ước `…Model` / `…View`, luồng nước đi, event, sortingOrder |

File cũ đổi tên **kèm `.meta`** nên GUID giữ nguyên: reference trong `SampleScene`, `Cell.prefab`, `PlacedBlock.prefab` không bị mất.

### Tác dụng

- Polish hiệu ứng chỉ cần sửa `PlacedBlockView`, `SelectionBoxView`, `CellView`; mọi thông số (thời gian, biên độ rung, màu đỏ, độ trong preview) đã đưa ra Inspector.
- Ô số ẩn "?": vì người chơi không biết số nên mèo xem trước hiện khi các luật khác đã đạt; thả sai kích thước thì mèo rung đỏ như nước đi sai.
- `MistakeFlash.prefab` không còn được dùng (chưa xóa).
- Thả tay khi chưa đủ điều kiện vẫn hủy lặng lẽ, không trừ mạng; `onInvalidMove` đã mang `PlacementResult` để sau này làm phản hồi cho người chơi.

### Kiểm tra (Play mode, level 16)

| Bước | Kết quả |
| --- | --- |
| Kéo 2x2 quanh ô số 9 | Không có mèo; khung chọn hiện |
| Kéo tiếp tới 3x3 | 1 mèo xem trước; khung chọn trượt 2.4 → 3.6 rồi alpha về 0 |
| Thả tay | Khối được đặt, ô bị chiếm, mèo đặc lại |
| Kéo 3x1 đủ value nhưng sai lời giải → thả | Mèo đỏ rung, mạng 3 → 2, ô không bị chiếm; hết anim khối trả về pool |
| Đủ → lùi 1 ô → đủ lại → hủy | Preview 1 → 0 → 1 → 0 |
| Console | Không lỗi, không cảnh báo mới |

---

## 21:35 – Màu khối mèo = màu khung chọn

### Vấn đề

Khung chọn random 1 màu mỗi lượt kéo, còn khối mèo lấy màu theo `ClueData.color` của level → kéo màu xanh nhưng mèo hiện màu khác.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Input/SelectionBoxView.cs` | Thêm `ColorType` (màu của lượt kéo hiện tại) |
| Sửa | `Gameplay/Core/GridManager.cs` | Lưu `_dragColor` từ khung chọn; truyền vào `ShowPreview` và `Place` |
| Sửa | `Gameplay/Core/BoardModel.cs` | `Place(check, color, unlocked)` |
| Sửa | `Gameplay/Block/BlockModel.cs` | Nhận màu qua constructor; bỏ `ColorFor` (màu theo clue) |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `ShowPreview(rect, clueCell, color, geometry)` |
| Sửa | `Docs/ProjectStructure.md` | Ghi chú màu khối lấy từ khung chọn |

### Tác dụng

Khung chọn, mèo xem trước và khối đã đặt luôn cùng 1 `ColorType`. `ClueData.color` trong level không còn được dùng để tô khối.

### Kiểm tra (Play mode, level 16)

Đặt 5 khối theo lời giải; mỗi khối: màu khung = sprite thân mèo xem trước = màu khối đã đặt (Gray, Yellow, Cyan, Orange, Pink). Đã đối chiếu 10 `ColorType` với sprite thân / tai trong `GameplayVisuals` (đúng tên màu). Console không lỗi.

---

## 22:05 – Khung chọn không phủ lên khối đã đặt / ô Void (video `refdrag`)

### Mục tiêu

Theo video `refdrag`: đã đặt khối thì khung chọn không kéo lên trên khối được, ô Void cũng vậy. Phân tích frame:
- Ngón tay kéo đè lên khối / Void / ra ngoài lưới: khung dừng sát mép vật cản, bong bóng hiện số ô đã kẹp.
- **Giây 3.68–4.03:** ngón tay đã ở dưới lưới nhưng đang ở cột của mèo vàng → vùng tới ngón tay đè khối nên khung **giữ nguyên** 2 ô; ngón tay lùi sang cột trống → khung giãn ngay xuống 2x2. Tức là quy tắc là *giữ vùng hợp lệ trước đó*, không tự tìm vùng lớn nhất.
- Ngón tay ngoài lưới được quy về ô ở mép lưới.
- Khung vẫn được phủ qua ô số khác (chỉ khối đã đặt và Void chặn) – đã xác nhận.
- Thêm hiệu ứng nhẹ khi bị chặn (yêu cầu thêm, ref không có).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Input/DragSelection.cs` | **Logic** vùng kéo: `Begin`, `MoveTo(fingerCell, board)` đi từng ô từ vị trí cũ tới mới (vuốt nhanh không kẹt), chỉ nhận ô đích nếu `IsAreaFree`; trả `DragStep` (đổi vùng, vừa bị chặn, hướng chặn) |
| Sửa | `Gameplay/Core/BoardModel.cs` | `GetCell`, `IsAreaFree(rect)` |
| Sửa | `Gameplay/Core/GridGeometry.cs` | `WorldToCell` (làm tròn + kẹp trong lưới) |
| Sửa | `Gameplay/Core/GridManager.cs` | API mới `BeginSelection(cell)` / `UpdateSelection(world)` / `EndSelection()` / `CancelSelection()`, `SelectionCellCount`; bỏ `OnDrawGrid`, `OnSelect`, `CountCellsInRange` |
| Sửa | `Gameplay/Input/DragAndDropController.cs` | Raycast chỉ khi bắt đầu kéo; khi kéo gửi vị trí world của ngón tay (không cần trúng ô) |
| Sửa | `Gameplay/Input/SelectionBoxView.cs` | `Bump(dir)`: mép phía bị chặn phồng ra 0.18 rồi co về, dao động tắt dần 0.22 s (chỉnh trong Inspector) |
| Sửa | `Docs/ProjectStructure.md` | Bảng script, luồng nước đi |

### Tác dụng

- Không thể bắt đầu kéo trên khối đã đặt / ô Void; khung chọn không bao giờ phủ lên chúng → lỗi `BlockedCell` khi thả tay gần như không còn xảy ra.
- Ngón tay trượt ra ngoài lưới, đè lên khối hay Void vẫn điều khiển được khung (không còn "đứng hình" vì raycast trượt).
- Bump chỉ chạy khi *vừa* bị chặn hoặc đổi hướng bị chặn, không lặp mỗi frame.

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Level 16, đặt 3x3 ở (0,4); bắt đầu kéo trên khối | Không kéo được |
| Bắt đầu (4,6) → (3,6) | 2 ô, không bump |
| → (1,6) đè khối | Giữ 2 ô, bump |
| → (1,5) chéo, cột vẫn đè khối | Giữ 2 ô (giống ref 3.68) |
| → (3,5) cột trống | Giãn 4 ô (giống ref 4.03) |
| Ngón tay ra ngoài lưới trên-phải | 3 ô (kẹp về mép lưới) |
| Vuốt nhanh (3,0) → (6,3) trong 1 frame | 16 ô |
| Level 206, ô Void (5,6): bắt đầu trên Void / kéo lên Void / kéo xa qua Void | Không kéo được / 1 ô / 1 ô |
| Ảnh chụp: kéo từ (4,6) tới (0,4) | Khung 2x3 dừng sát mép khối tím |
| Console | Không lỗi |

---

## 22:30 – Thử mép giãn kiểu dây thun khi bị chặn → đã bỏ

Thử: ngón tay vượt mép vì bị chặn thì mép khung giãn theo có lực cản (tối đa 0.2), khung vẽ dưới khối mèo (order 15). Không đúng ý → quay lại nguyên trạng đợt 22:05 (nảy `Bump`, khung order 30). Không còn thay đổi nào trong code.

---

## 23:00 – Bị chặn: mép khung lấp khe rồi kẹt lại (theo hình vẽ, thử nghiệm)

### Mục tiêu

Theo hình vẽ: kéo tới ô trống thì khung bình thường (ôm sát ô); kéo tới khối đã đặt thì khung "đi sang nhưng kẹt lại" – mép phía đó lấp khe, dừng ngay trước khối. Bỏ nảy. Không hợp thì quay lại.

### Vấn đề tỉ lệ

Khung cũ phủ tới giữa khe giữa các ô, khối mèo chiếm 96 % ô → lúc bình thường khung đã gần như chạm khối, không còn khe để thấy mép trượt sang. Đã gửi ảnh mô phỏng đúng tỉ lệ; chốt: thu khung lại ôm sát ô.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Input/SelectionBoxView.cs` | Bỏ `Bump`. `inset` 0.1: mỗi mép thụt vào so với ranh giới ô. `SetBlocked(dir)`: mép phía bị chặn trượt ra `blockedExtend` 0.1 (về đúng ranh giới ô, sát khối bên cạnh), giữ nguyên khi ngón tay còn đẩy, co về khi lùi; chuyển động dùng chung `SmoothDamp` của khung. Vẫn vẽ trên khối (order 30) |
| Sửa | `Gameplay/Input/DragSelection.cs` | Bỏ `DragStep`; `MoveTo` trả về bool; thêm `BlockDir` (ô dưới ngón tay nằm ngoài mép nào của vùng) |
| Sửa | `Gameplay/Core/GridGeometry.cs` | `WorldToCellUnclamped` |
| Sửa | `Gameplay/Core/GridManager.cs` | Mỗi lần kéo gửi `SetBlocked`: hướng bị chặn + ngón tay ra ngoài lưới ở mép vùng |
| Sửa | `Docs/ProjectStructure.md` | Bảng script, luồng nước đi |

### Kiểm tra (Play mode)

| Bước | Hướng bị chặn | Số ô |
| --- | --- | --- |
| Level 16, kéo (4,6) → (3,4) bình thường | (0, 0) – khung ôm sát ô, có khe với mèo | 6 |
| Ngón tay đè khối ở (1,5) | (−1, 0) – chỉ mép trái trượt sang sát mèo | 6 |
| Lùi về (3,5) | (0, 0) | 4 |
| Ngón tay ra ngoài lưới bên phải | (1, 0) | 12 |
| Ra ngoài góc trên-phải | (1, 1) | 3 |
| Level 206, kéo lên Void (5,6) | (1, 0) | 1 |
| Lỗi tìm thấy khi thử | Lần đầu so ngón tay với góc đích → mép trên cũng trượt sai; sửa thành so với các mép của vùng | |
| Console | Không lỗi | |

---

## 23:30 – Feel lúc đủ value: viền màu thu bên trong (thử feel 1)

### Phân tích 2 video ref

| Video | Lúc diện tích = value |
| --- | --- |
| `reffullvalue` (giao diện sáng) – **feel 1, inbound theo màu** | Viền màu dày hiện thụt vào trong mép vùng, trong ~0.15–0.2 s nở ra đúng mép, mỏng dần rồi mờ. Không vượt ra ngoài vùng |
| `reffullvalue2` (giao diện tối) – **feel 2, outbound trắng** | Mèo hiện ra ngả trắng rồi đậm dần lên đúng màu (~0.3 s); quầng trắng trong suốt quanh mèo lan ra ngoài mép, to dần và mờ (~0.2 s). Mèo đặc, không thở |

Chốt: thử feel 1 trước; viền dùng màu mèo; bỏ cú bật nhẹ của mèo xem trước (kèm bỏ độ trong 85 % và nhịp thở cho giống ref).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `GameAssets/03_Gameplay_Blocks_Grid/Blocks/RingBlock.png` | Viền bo góc trắng 128 px (bán kính 40, nét 14, khử răng cưa), Sprite 100 PPU, 9-slice border 44 |
| Sửa | `Prefabs/Gameplay/PlacedBlock.prefab` | Thêm con `Ring` (SpriteRenderer Sliced, order 24, tắt sẵn), gán vào `PlacedBlockView.ring` |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `ShowPreview`: mèo đặc ngay, không bật, không thở. `RingIn`: màu mèo pha 45 % trắng, bắt đầu thụt 0.12 vào trong, nở ra mép (ease-out), nét dày 1 → 0.35 (scale; size = kích thước / scale), alpha 1 − p², 0.2 s. Thông số ra Inspector. Bỏ `previewAlpha`, `previewBreath*`, `Update` |
| Sửa | `Docs/ProjectStructure.md` | Bảng script, prefab, sortingOrder |

### Ghi chú

- Viền cùng màu thân mèo sẽ không thấy được, nên pha trắng (`ringWhiten` 0.45).
- Tai mèo trong prefab đang order 23 (tài liệu ghi 19) nên viền đặt 24.
- Feel 2 chưa làm.

### Kiểm tra (Play mode, level 16, `Time.timeScale` 0.04 để chụp)

4 ảnh liên tiếp: viền vàng nhạt dày thụt trong → nở sát mép → mỏng dần → tắt. Mèo đặc ngay từ đầu. Console không lỗi; `timeScale` trả về 1.

---

## 00:10 (2026-10-03) – Đặt sai: viền đỏ bên ngoài thay cho nhuộm đỏ (thử cách A)

### Vấn đề

Nhuộm đỏ bằng phép nhân màu (`body.color = màu × đỏ`) chỉ làm tối: mèo vàng / cam ra đỏ đẹp nhưng xanh dương, tím, xanh lá thành nâu đục.

### Các cách đã đề xuất

| | Cách | Ghi chú |
| --- | --- | --- |
| **A** | Giữ màu mèo; viền đỏ ngoài mép nháy khi rung; co nhỏ + mờ | Đỏ chỉ nằm trên nền tối của lưới → sạch với mọi màu. **Đang thử** |
| B | Đổi sprite sang mèo xám (`Gray_Block`/`Gray_Ear`) khi rung | Xám của bộ sprite là màu be, có thể không đủ gắt |
| C | Tấm đỏ bán trong phủ lên (trộn, không nhân) | Đều màu nhưng vẫn là chèn đỏ lên mèo |
| D | A + B | Rõ nhất, hơi nhiều |

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `RejectRoutine`: không nhuộm thân; dùng con `Ring` (sprite `RingBlock`) màu `rejectRingColor`, nằm ngoài mép `rejectRingOutset` 0.05, nét `rejectRingThickness` 0.6, alpha nháy `rejectRingBlinks` 2 nhịp trong lúc rung; sau đó co về `rejectShrinkTo` 0.75 (ease-in) và mờ cả mèo lẫn viền. Viền đặt bằng `localPosition` nên rung theo khối. Bỏ `rejectTint` |
| Sửa | `Docs/ProjectStructure.md`, `Docs/README.md` | Mô tả phản hồi khi sai |

### Kiểm tra (Play mode, level 16, ép màu Purple, `timeScale` 0.015)

Vùng (1,0)–(3,0) đủ value nhưng sai lời giải: mạng 3 → 2, ô không bị chiếm. Ảnh: mèo tím giữ nguyên màu, viền đỏ gọn ngoài mép, khối lệch ngang giữa các ảnh (đang rung). Trả `timeScale` 1: khối co lại, biến mất, trả về pool. Console không lỗi.

Ghi chú: Play mode bị thoát giữa chừng 1 lần khi chụp (ảnh chụp ở Edit mode, không có khối) → vào lại Play và chụp lại.

---

## 00:40 (2026-10-03) – Đặt sai: thử cách B (mèo chuyển xám)

### Mục tiêu

Cách A (viền đỏ ngoài mép) chưa ưng → thử B: thân + tai đổi sang bộ sprite mèo xám trong lúc rung, rồi mờ đi, không dùng đỏ.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `enum RejectStyle { GrayOut, RedRing, GrayAndRedRing }`, field `rejectStyle` (mặc định `GrayOut`) để so sánh / quay lại trong Inspector. `Recolor(color)`: đổi sprite thân, tai và màu số giữ nguyên kích thước (tách `SetBodyColor` từ `SetupBody`). `RejectRoutine`: GrayOut → `Recolor(Gray)`; viền đỏ chỉ chạy khi style có RedRing. Bước co nhỏ + mờ dùng chung (`rejectShrinkTo` 0.75; đặt 1 nếu chỉ muốn mờ) |
| Sửa | `Docs/ProjectStructure.md`, `Docs/README.md` | Mô tả phản hồi khi sai |

### Kiểm tra (Play mode, level 16, `timeScale` 0.015)

Đặt 1 khối tím đúng bên cạnh để so sánh; vùng (1,0)–(3,0) màu tím đủ value nhưng sai lời giải → mèo đổi sang sprite Gray, mạng 3 → 2, ô không bị chiếm; trả `timeScale` 1 → khối sai về pool, khối tím còn nguyên. Khối lấy lại từ pool dựng lại sprite theo màu mới nên màu xám không dính. Console không lỗi.

### Ghi chú

Sprite "Gray" của bộ asset là màu be / kem → nhìn giống 1 con mèo màu khác hơn là "hỏng". Nếu không ổn: cần sprite xám thật (tạo từ sprite màu bằng cách khử bão hòa), hoặc quay lại A / thử C.
