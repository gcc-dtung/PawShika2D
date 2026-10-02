# 01 – Core: gameplay và cơ chế mạng

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `681d08c` "Core" (nhánh `Day-1`) |
| Thời gian push | 2026-10-02 13:46 |
| Cập nhật lần cuối | 2026-10-02 17:12 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| Sáng | Phân tích & kế hoạch | Hiểu game gốc, chốt 7 quyết định |
| Sáng (trước 10:50) | Cơ chế mạng bản đầu | `LivesManager`, `LivesUI`, nối `DeductLife()` |
| 10:50 – 11:10 | Giai đoạn 1–2 | Một level chơi được đầy đủ với 205 level ShikaCat |

---

## Sáng – Phân tích dự án gốc và lập kế hoạch

### Mục tiêu

Hiểu game Paw Block từ asset đã trích (`E:\PawBlock`) để thay cơ chế thời gian bằng cơ chế mạng. Không đổi code.

### Phát hiện

| Nguồn | Phát hiện |
| --- | --- |
| `E:\PawBlock\...\Scripts` (205 file) | Build IL2CPP: chỉ còn tên class, biến, chữ ký hàm. Logic phải tự viết lại. |
| `LevelGenerator`, `LevelDifficultyPlanner` | Level sinh ngẫu nhiên lúc chạy → không có file level |
| `GameplayController`, `MoreTimePopup`, `FreezeBooster` | Thời gian: 8 s + 0.25 s/ô + 5.5–6.5 s/khối, tối thiểu 45 s; sao theo thời gian |
| `EnergyUI`, `RefillLivesPopup` | "Lives" của bản gốc là năng lượng vào màn, khác mạng trong màn |
| `BoosterDatabase.asset` | Hint (lv 5, 1200), Freeze Time (lv 7, 1500, 20 s), Reveal (lv 9, 1000) |
| `E:\game_view` (13 ảnh Level 26) | Ô khóa giảm 1 sau mỗi khối đúng; ô `?`; bong bóng số ô; khối thành con mèo |
| Trang Google Play | Chữ khen NICE / AMAZING; banner HARD LEVEL; mô tả "No stressful timers" |

### Quyết định đã duyệt

1. Trừ mạng khi khối đúng số ô nhưng sai vị trí
2. 3 mạng cho mọi level
3. Bỏ sao
4. Không có Undo
5. Freeze Time → Khiên
6. Hết mạng → popup Hồi mạng rồi mới Lose
7. Giữ năng lượng ngoài sảnh, đổi icon

### Tác dụng

Chốt luật chơi trước khi code; chia việc thành 6 giai đoạn (`Docs/TaskBreakdown.md`).

---

## Sáng (trước 10:50) – Cơ chế mạng bản đầu

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `LivesManager.cs` | Số mạng theo `rules.mistakeLimit`, `LoseLife`, `AddLives`, `CalculateStars`, event |
| Thêm | `LivesUI.cs` | Hàng tim bằng SpriteRenderer, `heart_1` / `heart_2` |
| Sửa | `GridManager.cs` | `DeductLife()` gọi `LivesManager`; thêm `IsGameOver`, `IsInputLocked` |
| Sửa | `DragAndDropController.cs` | Kiểm tra `IsInputLocked` thay cho `IsGameWon` |

**Tác dụng:** đặt sai thì mất mạng, hết mạng thì khóa thao tác. Phần lớn được thay thế ngay ở đợt sau (3 mạng cố định, bỏ sao, LivesUI chuyển sang Canvas, kiểm tra bằng solver).

---

## 10:50 – 11:10 – Giai đoạn 1–2: gameplay một level đầy đủ

### Thêm

| File | Nội dung |
| --- | --- |
| `ShikakuSolver.cs` | Backtracking + MRV. Nước sai = đặt xong lưới không còn lời giải. 205 level giải trong 16 ms. |
| `LevelManager.cs` | Nạp level, lưu `PawBlock_CurrentLevel`, thắng → level kế, hết mạng → chơi lại, zoom camera |
| `LevelDatabase.cs` | Danh sách level |
| `GameplayVisuals.cs` | ScriptableObject gom sprite (`Resources/GameplayVisuals.asset`) |
| `CatFaceAnimator.cs` | Chạy chuỗi khung hình mặt mèo |
| `DragCountBubble.cs` | Bong bóng số ô khi kéo |
| `GameplayFeedback.cs` | Chữ khen; chớp đỏ, rung camera, rung máy khi sai |
| `LevelHUD.cs` | Chữ `LEVEL n`, nút chơi lại |
| `BackgroundFitter.cs` | Nền phủ kín camera khi zoom |
| `Editor/ShikaCatLevelImporter.cs` | Menu nhập 205 level từ `shikacat-levels.json` (lật trục y) |
| `Editor/GameplayVisualsSetup.cs` | Menu: 9-slice + Full Rect cho khối, cắt atlas Spine thành 10 bộ mặt mèo |
| `Levels/ShikaCat/Level_001..205.asset`, `Levels/LevelDatabase.asset` | Dữ liệu level |
| Scene: `Canvas_HUD/TopPanel` | Khung LEVEL, hàng tim, nút chơi lại |

### Sửa

| File | Nội dung |
| --- | --- |
| `GridManager.cs` | Hủy thao tác (không phạt) / trừ mạng (solver) / đặt khối; giảm khóa; bỏ chạm-để-xóa |
| `ClueCell.cs` | Ổ khóa, ô ẩn `?`, mũi tên ngang/dọc |
| `PlacedBlock.cs` | Khối mèo: thân màu, tai, mặt, số, hiệu ứng bật |
| `GridSpawner.cs` | Bỏ ô Void; nhường quyền chọn level cho `LevelManager` |
| `LivesManager.cs` | 3 mạng cố định, bỏ sao |
| `LivesUI.cs` | Chuyển sang UI Image trên Canvas |
| `DragAndDropController.cs` | Gắn bong bóng số ô |
| `Cell.prefab` | Font LiberationSans, chữ tự co (3–6.5) |

### Xóa

- Font `Fredoka-Bold Dynamic SDF.asset` tự tạo (hiển thị lỗi).
- Bộ mặt mèo `boltie_01` (khung bị xoay khi đóng gói, kích thước khác).

### Tác dụng

- Vòng chơi chạy được: vào level → chơi → thắng/thua → level kế / chơi lại.
- 17/205 level ShikaCat có nhiều lời giải; dùng solver nên không phạt nước đi hợp lệ.
- Code hỗ trợ ô khóa / ẩn / mũi tên, chờ dữ liệu level.

### Kiểm tra

Play mode trên level 9×9 có gắn tạm cơ chế: 4 khối đúng được đặt, ổ khóa mở đúng, ô ẩn lộ số; 1 nước sai → chớp đỏ, tim 3 → 2.

### Tồn đọng

Font Fredoka, sprite mũi tên debug, âm thanh; thắng / hết mạng tự chuyển sau 1.5 s (chờ popup Giai đoạn 3).
