# 12 – Sửa bàn tay booster, xếp lại thứ tự level, cơ chế sớm hơn

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `ed5b3b2` "Xếp lại level theo độ khó, cơ chế sớm hơn, Khiên kéo sai thử" (nhánh `Day-4`) |
| Thời gian push | 2026-10-05 19:04 |
| Cập nhật lần cuối | 2026-10-05 19:04 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 17:43 (05/10) | Bàn tay nằm dưới nút booster | Bàn tay tutorial mở khóa booster bị nút che (cả 3 booster); giờ vẽ trên nút |
| 18:24 (05/10) | Xếp lại level, cơ chế sớm hơn, Khiên ở level Hard | Bỏ level 4 (và mọi level 1 khối); xếp theo độ khó với đỉnh / level nghỉ mỗi 10 level; mọi cơ chế giới thiệu trước level 17; Khiên mở ở level Hard đầu tiên (19) |
| 18:59 (05/10) | Hover biểu đồ độ khó, Khiên thứ 2 + kéo sai thử | Rê chuột trên biểu đồ Level Difficulty hiện level; Khiên mở ở level 7 (thứ 2), tutorial bắt kéo sai để thấy khiên chặn lỗi |

---

## 17:43 – Bàn tay tutorial booster nằm dưới nút

### Vấn đề

Tutorial mở khóa booster: bàn tay bị nút booster che mất, chỉ lộ phần cổ tay (lỗi ở cả Hint, Khiên, Reveal).

### Nguyên nhân

Thứ tự vẽ:
- Nền tối và bàn tay cùng nằm trong Canvas `BoosterTutorial` (override sorting, order 5).
- Nút booster được nhấc lên order 6 để nổi trên nền tối, nên vẽ đè lên bàn tay.

### Thay đổi

- `UI/HUD/BoosterTutorialView.cs`: `PlayHand` gắn (nếu chưa có) Canvas override sorting cho bàn tay, order = order nền + 2.
  - Thứ tự giờ là: nền tối (5) < nút (6) < bàn tay (7).
  - Bàn tay không nhận chạm (`raycastTarget` tắt) nên không cần GraphicRaycaster; bấm nút vẫn bình thường.

### Kiểm tra (Play mode, đã trả lại save sau khi thử)

- Level 5, booster chưa mở: tutorial Hint, bàn tay vẽ trên nút (ảnh chụp: ngón trỏ đè lên icon bóng đèn).
- Lưu ý: lần thử đầu Unity đang ở Play mode sẵn, biên dịch lại giữa Play làm phiên đó lỗi `NullReferenceException` (GridSpawner.Spawn). Đã thoát Play, trả save, vào Play lại mới thử.

---

## 18:24 – Xếp lại thứ tự level, cơ chế sớm hơn, Khiên ở level Hard

### Yêu cầu

- Xóa level 4.
- Khiên mở ở level không cần khiên (level 7, rất dễ) nên không thể hiện được cách dùng.
- Cơ chế xuất hiện quá muộn (Hướng 25, Khóa 43, "?" 60): cần hiện hết trong khoảng level 25 trở xuống.
- Sắp xếp lại thứ tự level cho hợp lý.

### Phân tích trước khi sửa

- Level 4 = `ShikaCat/Level_004`: 1 cột 3 ô, 1 số. `Level_006` (2x2), `Level_007` (3x2) cũng chỉ 1 khối.
- Độ khó gốc theo thứ tự ShikaCat nhảy lung tung (vd. level 15 D 56, level 16 D 22).
- 17 level nhiều lời giải nằm trong khoảng 15–55.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Editor/Level/LevelOrderTool.cs` – Tools → PawBlock → Builders → **Reorder Levels** (`Apply()` gọi được từ code):
  1. Level 1 = `Custom/Level_Custom_Level01_4x4`. Bỏ `Level_001`, `Level_004` và mọi level chỉ 1 khối (tutorial đã dạy). Level Test / Custom khác giữ cuối.
  2. Chấm D lý thuyết bản gốc, **bỏ phần T4** (level nhiều lời giải chấm cao giả), xếp tăng dần.
  3. Level 1–10 chỉ tăng dần. Từ level 11, mỗi khối 10:
     - ...9 là đỉnh: level khó nhất trong khối này + khối sau; không lấy level nhiều lời giải vì không gắn được khóa / "?".
     - ...0 là level nghỉ (dễ nhất khối); còn lại tăng dần.
  4. Đánh dấu ...9 từ level 19: Hard, chia 30 dư 29: Very Hard (ghi vào bản gốc ShikaCat).
  5. Chạy Mechanic Injector cho cả database.

**Sửa**

- `MechanicInjectorSettings` (code mặc định + asset):
  - Void 5, Hướng 8, Khóa 12, "?" 16 (mỗi đợt 2 level).
  - Ưu tiên cơ chế mới nhất trong 10 level; kết hợp từ level 30.
  - `minCluesAfterVoid` 3 (level đầu nhỏ vẫn giới thiệu được Void).
- `MechanicInjector`:
  - Level Hard / Very Hard chỉ gắn "?" → khóa → Void, không dùng mũi tên hướng (mũi tên cho thêm thông tin, làm dễ đi).
  - Gắn hỏng thì thay theo thứ tự khóa / "?" / Void (`Plan.hard`).
- `BoosterDatabase.asset`: Khiên `levelUnlock` 7 → 19 (level Hard đầu tiên).
- `BoosterTutorial`: tutorial Khiên ở level Hard thì lời nhắc thêm "Tough level! " (mô tả gốc vẫn dùng chung với popup mua).
- `LevelDatabase.asset`: 203 level (1 Custom + 201 ShikaCat / Mechanic + Test); 131 level có cơ chế (Void 42, Hướng 42, Khóa 46, "?" 52); 13 Hard + 6 Very Hard.
- Docs: `README.md`, `ProjectStructure.md`, `TutorialGuide.md`, `DifficultyFormula.md`.

### Tác dụng

- Cơ chế xuất hiện lần đầu: Void level 5, Hướng 8 (popup Orientation), Khóa 12, "?" 16. Booster: Hint 5, Reveal 9, Khiên 19.
- D trung bình theo khối 10 level tăng dần: 20 → 28 → 32 → 34 … → 51 (khối 191–200).
- Cả 19 level Hard / Very Hard đều có D cao hơn trung bình khối của nó (vd. 19: 44 so với 28, 49: 61 so với 31, 199: 67 so với 51).
- Lần chạy đầu (trước khi sửa Injector + bỏ T4): level 19 (Hard) chỉ D 21 so với trung bình khối 26, level 199 chỉ D 18 (Void + hướng làm dễ đi).

### Còn lại

- Level 1 (4x4 gốc, 2 lời giải) có D 34, cao hơn level 2–10 vì bước T4. Game chấm bằng solver nên không trừ mạng oan.
- Level thường có "?" đôi khi khó hơn đỉnh kế bên (vd. 28: D 58, 29 Very Hard: D 35).
- Tiến độ trong save lưu theo số thứ tự nên lệch so với trước.
- Level 7 (3x3, 3 khối) lên lịch Void nhưng không gắn được (cần ≥ 4 khối); Hướng chưa mở nên không có cơ chế thay.

### Kiểm tra

- `LevelValidator` toàn database: 0 lỗi.
- Play mode level 19 (Hard, có "?" + khóa): băng HARD xong thì tutorial Khiên với lời nhắc "Tough level! Blocks your next mistake in this level - no life lost!", bàn tay trên nút. Đã trả lại save.

---

## 18:59 – Hover biểu đồ độ khó; Khiên giới thiệu thứ 2 + bắt kéo sai

### Yêu cầu

1. Level Difficulty: rê chuột vào thanh độ khó thì biết đó là level bao nhiêu.
2. Khiên giới thiệu thứ 2 (sau Hint), và khi giới thiệu phải yêu cầu người chơi kéo sai cho rõ cách dùng.

### Thay đổi (Thêm / Sửa / Xóa)

**Sửa**

- `LevelDifficultyWindow`:
  - `wantsMouseMove` + vẽ lại khi di chuột.
  - Rê chuột trên biểu đồ: viền trắng quanh cột, hộp "Level n · tên / D · giây · T1/T2/T3/T4 · Hard" (+ chỉ số thực tế nếu đã nạp CSV), sát mép phải thì lật sang trái.
  - Bấm cột: mở Level Editor ở level đó.
- `BoosterDatabase.asset`: Khiên `levelUnlock` 19 → 7. Thứ tự giới thiệu: Hint 5, Khiên 7, Reveal 9.
- `BoosterTutorial`: bước 2 cho Khiên (`Demonstrating`):
  1. Bấm Khiên xong 0.2 s: nền tối tắt, lưới mở, lời nhắc "Now make a wrong move like this - the Shield will block it!".
  2. `TutorialHand` kéo mẫu 1 vùng đúng số ô nhưng làm lưới hết lời giải (`FindWrongMove`: số nhỏ nhất, thử mọi hình chữ nhật đúng diện tích chứa số đó, lấy cái `Check` sẵn sàng mà `IsSolvableWith` sai). Đặt đúng thay vì sai thì chỉ lại.
  3. `LivesManager.onShieldBlocked` → "The Shield saved you - no life lost!" 2 s rồi tắt.
  4. Thắng / nạp level thì bỏ.
- `BoosterTutorialView`:
  - `ShowMessage(text)`: chỉ khung lời nhắc, tắt nền tối (con "Dim") và bàn tay UI, không chặn thao tác.
  - Khung xuống y −390 (dưới hàng tim) để thấy lớp băng khiên.
  - `Show` trả khung về vị trí dựng sẵn.
- Docs: `README.md`, `ProjectStructure.md`, `TutorialGuide.md` (mục 7: bước kéo sai thử; sửa dòng kiểm tra bị lặp "Reveal").

### Kiểm tra (Play mode, level 7 = 3x3 có 3, 2, 4; Hint đã mở; đã trả lại save)

- Tutorial Khiên hiện ở level 7; bấm → có khiên, lưới mở, bàn tay kéo mẫu số 2 dọc lên trên (vùng sai `(0,1) 1x2`).
- Kéo đúng vùng đó: mèo cau có + rung ra, mạng vẫn 3, khiên hết, lời nhắc "The Shield saved you - no life lost!".
- Sau khi dời khung: thấy rõ khung LEVEL 7 + hàng tim có lớp băng, khung lời nhắc ngay dưới.
- Lần chạy đầu lỗi biên dịch thiếu `using DG.Tweening` (Kill / SetLink là extension DOTween), đã thêm.
