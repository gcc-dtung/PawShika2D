# 11 – Tutorial mở khóa booster, popup cơ chế, Level 1 4x4

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-05 11:56 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 11:23 (05/10) | Tutorial mở khóa booster | Booster khóa tới khi qua tutorial: tới level mở thì tối màn hình, chỉ nút đó sáng + bàn tay + lời nhắc; bấm thì mở khóa, lần dùng đầu miễn phí |
| 11:33 (05/10) | Level 1 4x4 + popup cơ chế | Level 1 thay bằng lưới 4x4 như bản gốc; popup giới thiệu Hướng / Khóa / "?" lần đầu gặp (ảnh popup_tutorial_2/3/4), chạy trước tutorial booster |
| 11:56 (05/10) | Sửa lỗi confetti lần thắng thứ 2 | Từ lần thắng thứ 2 confetti chỉ hiện vài mảnh đứng im ở 2 mép: UIParticle bị tạm dừng sau khi popup đóng; giờ Resume trước khi bắn |

---

## 11:23 – Tutorial mở khóa booster

### Mục tiêu / Vấn đề

Theo 2 video bản gốc:
- Thanh booster ban đầu khóa cả 3 ("Level N").
- Tới level chỉ định: màn hình tối, chỉ nút booster đó sáng có quầng, bàn tay nhấn vào nút, lời nhắc ở trên ("Hint will automatically complete one correct region!"...).
- Bấm nút thì booster chạy ngay (Hint đặt 1 khối; Reveal hiện banner chọn số rồi giải mọi vùng cùng số), sau đó booster mở khóa.

Trước đây booster "mở" chỉ bằng so số level, không có tutorial và không lưu lại.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Booster/BoosterTutorial.cs` (trên `Manager`):
  - Sau mỗi lần nạp level, chờ đang chơi thật (không Menu / popup / tutorial lần đầu, lưới hiện xong, băng HARD xong) + 0.3 s.
  - Chọn booster chưa mở có `levelUnlock` nhỏ nhất mà level hiện tại đã tới. Mỗi lần nạp level tối đa 1 tutorial.
  - `Active` / `Target` (tĩnh); `Complete` lưu `unlockedBoosters` và đóng lớp tối.
  - Không tìm thấy nút / giao diện thì mở khóa luôn để không kẹt.
- `UI/HUD/BoosterTutorialView.cs` – `Canvas_HUD/BoosterTutorial`:
  - Canvas riêng (override sorting, order 5) phủ cả màn hình; nền tối 0.75 chặn lưới và nút khác.
  - Khung lời nhắc `khung_reveal2` đè khung LEVEL, chữ lấy từ `BoosterData.detail`.
  - Bàn tay `tutorial_hand` nhấn lặp lên nút.
  - Nút được nhấc trên nền tối bằng Canvas override sorting (order 6, thêm Canvas + GraphicRaycaster lúc chạy) và bật quầng sáng.
- `SaveData.unlockedBoosters` + `IsBoosterUnlocked` / `UnlockBooster`.
- `BoosterButton.SetHighlight`: quầng sáng xoay khi tutorial chỉ vào.
- `TutorialBuilder`: Build Tutorial dựng thêm `Canvas_HUD/BoosterTutorial`, gắn `BoosterTutorial` lên `Manager`. Đã chạy và lưu `SampleScene`.
- `DebugCheats.UnlockAllBoosters` / `LockAllBoosters` + Debug Panel mục Booster: **Mở khóa tất cả**, **Khóa lại tất cả**.

**Sửa**

- `BoosterManager`:
  - `IsUnlocked` đọc `SaveData.unlockedBoosters`, không so level nữa; booster đang là đích tutorial cũng tính là mở.
  - `Press` trong lúc tutorial chỉ nhận đúng nút → `BoosterTutorial.Complete` → lần dùng này miễn phí (`_freeUse`), dùng được cả khi hết lượt; Reveal miễn phí tới khi chọn số; nạp level thì bỏ.
- `GridManager.IsInputLocked`: thêm `BoosterTutorial.Active`.
- Docs: `ProjectStructure.md` (Booster, HUD, scene, thứ tự Canvas, builder, cheats), `README.md` (mục Build Boosters), `TutorialGuide.md` (mục 7: tutorial mở khóa booster, thêm booster mới, kiểm tra).
- `Fredoka-Bold SDF.asset`: TMP tự thêm glyph vào atlas động khi hiện chữ mới.

### Tác dụng

- Người chơi gặp từng booster đúng lúc, được dùng thử miễn phí 1 lần.
- Người chơi cũ đã qua level mở sẽ được giới thiệu lần lượt ở các lần vào level kế tiếp.
- Level mở và lời nhắc sửa trong `BoosterDatabase.asset` (Hint 5, Khiên 7, Reveal 9).

### Kiểm tra (Play mode, save đặt level 5 + chưa mở booster nào; đã trả lại `save.json` và `save.json.bak` sau khi thử)

- Level 5:
  - Tối màn hình, nút Hint sáng có quầng + số lượt 3, bàn tay; Khiên / Reveal vẫn khóa LV 7 / LV 9.
  - Bấm Hint: tự đặt khối số 3, lớp tối tắt, lượt vẫn 3, `unlockedBoosters` có Hint.
- Level 9:
  - Hiện tutorial Khiên (bỏ qua Reveal vì mỗi lần 1 cái); Hint đã mở nhưng bị tối.
  - Bấm Khiên: có khiên, lượt vẫn 3.
- Chơi lại level 9:
  - Hiện tutorial Reveal; bấm vào chế độ chọn số; chạm số 9 → đặt khối; lượt Reveal vẫn 1.
- Chơi lại lần nữa: không còn tutorial; bấm Hint trừ lượt 3 → 2.
- Khung lời nhắc lúc đầu ở y −400 đè hàng trên của lưới 5x5 → dời lên y −180 (đè khung LEVEL đang tối như bản gốc), chụp lại: lưới không bị che.
- Console không lỗi.

---

## 11:33 – Level 1 4x4 + popup giới thiệu cơ chế

### Mục tiêu / Vấn đề

1. Level 1 cũ (`ShikaCat/Level_001`: 3 ô ngang, số 3) trùng bước 1 của tutorial; bản gốc vào Level 1 là lưới 4x4.
2. Có sẵn 3 ảnh hướng dẫn cơ chế (`popup_tutorial_2/3/4`) chưa dùng; level giới thiệu cơ chế (Mechanic Injector) chưa có lời giải thích.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Levels/Custom/Level_Custom_Level01_4x4.asset`: đúng Level 1 trong video bản gốc.
  - Hàng trên → dưới: `4 . 3 .` / `. . . 2` / `. . 4 .` / `. 3 . .`; lời giải như video (cột 4, hàng 3, cột 3, khối 2 ngang, khối vuông 4).
  - Thay vào ô 1 của `LevelDatabase` (`ShikaCat/Level_001` vẫn giữ asset, chỉ bỏ khỏi database).
- `Gameplay/Tutorial/MechanicIntro.cs` (trên `Manager`):
  - Sau mỗi lần nạp level, khi lưới hiện xong / băng HARD xong / không tutorial khác + 0.15 s: level có cơ chế chưa xem thì mở popup lần lượt theo thứ tự Hướng → Khóa → "?".
  - Ok thì lưu `SaveData.seenMechanics`.
- `UI/Popups/MechanicIntroPopup.cs` (`PopupView`): tiêu đề, giải thích, ảnh, nút Ok; mở thì tạm dừng ván, Ok thì chơi tiếp.
- `TutorialBuilder`: dựng `Canvas_Popup/Popups/MechanicIntroPopup` như popup "Orientation" bản gốc (khung `popup_tutorial` 9-slice, chữ tím đậm, ảnh 800x400, nút xanh Ok), gắn `MechanicIntro` + 3 ảnh. Đã chạy và lưu `SampleScene`.
- `DebugCheats.ResetMechanicIntros` + Debug Panel: **Xem lại popup cơ chế**.

**Sửa**

- `SaveData.seenMechanics`.
- `BoosterTutorial`: chờ thêm `MechanicIntro.Active`.
- Docs: `ProjectStructure.md`, `README.md`, `TutorialGuide.md` (Cách A đã làm, cách thêm cơ chế).

### Tác dụng

- Level 1 khác bước tutorial, giống bản gốc.
- Người chơi được giải thích cơ chế ngay level đầu gặp nó (theo lịch Mechanic Injector: Hướng level 25, Khóa 43, "?" 60). Level có cả cơ chế mới lẫn booster mới: popup cơ chế trước, tutorial booster sau.

### Lưu ý

- Level 1 gốc có **2 lời giải**: phần 3x3 còn lại xếp được 3 dọc + 2 ngang + 4 vuông, hoặc 3 ngang dưới + 2 dọc + 4 vuông giữa. Game chấm bằng solver nên cách nào cũng đúng, không trừ mạng oan.
  - Validator thường: hợp lệ. Kiểm "không cần đoán": báo phải chọn sau 2 khối.
  - Level Difficulty: D = 34.2 (chuỗi `11411`), cao hơn level 2 (12.5) vì bước T4. Muốn 1 lời giải thì sửa trong Level Editor.
- Chạy lại Import ShikaCat Levels sẽ đưa Level 1 về bản ShikaCat; bản Custom bị thêm vào cuối database.
- Mechanic Injector bỏ qua ô 1 (level Custom), không ảnh hưởng vì level 1 vốn không có cơ chế.

### Kiểm tra (Play mode, save đặt level 1 + đã mở booster + chưa xem cơ chế; đã trả lại save sau khi thử)

- Level 1: lưới 4x4 đúng như video.
- Level 25: popup "Orientation" (ảnh Vertical / Square / Horizontal), ván Paused; Ok → Playing, `seenMechanics = Constraint`.
- Level 43: popup "Locked Number". Level 60 (khóa lại booster để thử chồng): popup "Hidden Number" trước, Ok xong ~0.3 s thì tutorial Hint hiện.
- Chơi lại level 60: không popup, không tutorial. Console không lỗi.

---

## 11:56 – Sửa lỗi confetti từ lần thắng thứ 2

### Vấn đề

Lần thắng đầu confetti màn WELL DONE bắn / rơi bình thường. Từ lần thắng thứ 2 chỉ thấy vài mảnh đứng im ở 2 mép màn hình (chỗ phát chùm bắn).

### Nguyên nhân

- `UIConfetti.OnDisable` (popup thắng đóng) gọi `UIParticle.Stop()` + `Clear()`. Trong package UIParticle, cả hai đều đặt `isPaused = true`.
- `UIParticleRenderer` không mô phỏng khi `isPaused`.
- Lần sau `UIConfetti` gọi thẳng `ParticleSystem.Play()`, hàm này không tắt cờ đó → hạt sinh ra ở vị trí phát rồi đứng im.

### Thay đổi

- `UI/Common/UIConfetti.cs`: thêm `Unpause()` (gọi `UIParticle.Resume()` nếu đang `isPaused`) ở đầu `Burst()` và `StartRain()`.

### Kiểm tra (Play mode, đã trả lại save sau khi thử)

- Thắng level 2: `isPaused = False`, 109 hạt.
- Next → level 3: sau khi popup đóng `isPaused = True` (đúng trạng thái gây lỗi).
- Thắng level 3: `isPaused = False`, 110 hạt; mưa confetti có y trung bình −9.9 (đang rơi); ảnh chụp confetti bay khắp màn hình.
