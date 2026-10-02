# Paw Block – Bản làm lại với cơ chế mạng

Game giải đố Shikaku dựa trên [Paw Block Puzzle](https://play.google.com/store/apps/details?id=com.phoenix.pawblock.puzzle). Thay đổi chính so với bản gốc: **bỏ đồng hồ đếm ngược, thay bằng 3 mạng mỗi level**.

- Unity **2022.3.62f3**, URP 2D, TextMeshPro, uGUI
- Scene chính: `Assets/Scenes/SampleScene.unity`
- Kế hoạch chi tiết và các quyết định: [TaskBreakdown.md](TaskBreakdown.md)
- Cấu trúc thư mục, script, luồng dữ liệu: [ProjectStructure.md](ProjectStructure.md)

---

## Luật chơi

1. Lưới có các ô số. Kéo chọn một hình chữ nhật để tạo khối.
2. Mỗi khối phải chứa **đúng 1 ô số** và có **số ô bằng giá trị** của ô số đó.
3. Phủ kín lưới là thắng.

### Cơ chế mạng

| Tình huống | Kết quả | Mạng |
| --- | --- | --- |
| Khối hợp lệ và lưới vẫn còn lời giải | Đặt khối mèo, hiện chữ khen, mọi ổ khóa giảm 1 | Giữ nguyên |
| Khối đúng số ô nhưng đặt xong thì lưới **không còn lời giải** | Vùng chớp đỏ, camera rung, điện thoại rung | **−1** |
| Sai số ô, 0 hoặc nhiều ô số, đè khối khác, chứa ô còn khóa, sai hướng mũi tên | Chỉ hủy thao tác | Giữ nguyên |
| Hết mạng | Khóa lưới (tạm thời: tự chơi lại sau 1.5 giây) | 0 |

- Mọi level đều có **3 mạng**. Không có sao, không có Undo.
- Không so với lời giải lưu sẵn vì 17/205 level có nhiều hơn một lời giải. `ShikakuSolver` kiểm tra xem lưới còn giải được hay không.
- Khối đã đặt không xóa được: mọi khối đặt được đều nằm trong ít nhất một lời giải.

### Ô đặc biệt

| Loại | Hiển thị | Luật |
| --- | --- | --- |
| Ô khóa | Ổ khóa mèo + số bước | Mỗi khối đặt đúng làm mọi khóa giảm 1. Về 0 thì lộ số. Còn khóa thì không chọn được. |
| Ô ẩn | `?` | Không kiểm tra số ô; solver quyết định đúng/sai. Đặt đúng thì lộ số. |
| Mũi tên ngang | Mũi tên mờ | Khối phải rộng hơn cao |
| Mũi tên dọc | Mũi tên mờ, xoay 90° | Khối phải cao hơn rộng |

Dữ liệu level ShikaCat hiện **chưa có** ô đặc biệt nào; code đã hỗ trợ, dữ liệu sẽ bổ sung sau.

---

## Chạy dự án

1. Mở `Assets/Scenes/SampleScene.unity`.
2. Lần đầu (hoặc khi đổi asset), chạy 2 menu Editor:
   - **Tools → PawBlock → Import ShikaCat Levels**: tạo 205 `LevelDefinition` trong `Assets/_Project/Levels/ShikaCat/` và `LevelDatabase.asset`.
   - **Tools → PawBlock → Setup Gameplay Visuals**: đặt 9-slice cho khối, cắt atlas mặt mèo, tạo `Assets/_Project/Resources/GameplayVisuals.asset`.
3. Bấm Play. Kéo chuột trên lưới để tạo khối.

Level đang chơi được lưu ở PlayerPrefs key `PawBlock_CurrentLevel`. Xóa key này (hoặc Edit → Clear All PlayerPrefs) để quay về level 1.

---

## Nguồn tài nguyên

| Nguồn | Nội dung dùng | Ghi chú |
| --- | --- | --- |
| `E:\PawBlock` (Paw Block, IL2CPP) | Sprite, âm thanh, Spine, cấu hình booster/shop | Code chỉ còn khung, không có thân hàm; logic tự viết lại |
| `E:\Data` (ShikaCat) | 205 level kèm lời giải | `shikacat-levels.json`, đã chép vào `Assets/_Project/ShikaCat_GameAssets/` |
| `E:\game_view` | 13 ảnh chơi thử Level 26 | Tham chiếu gameplay và hiệu ứng |

---

## Trạng thái

| Giai đoạn | Nội dung | Trạng thái |
| --- | --- | --- |
| 1 | Mạng trong màn | Xong |
| 2 | Gameplay một level đầy đủ | Xong (còn 3 việc tồn đọng) |
| 3 | Popup Thắng / Hồi mạng / Thua, HUD | Chưa làm |
| 4 | Booster Hint, Khiên, Reveal | Chưa làm |
| 5 | Năng lượng ngoài sảnh (giữ, đổi icon) | Chưa làm |
| 6 | Meta, Shop, Ads, Firebase | Chưa làm |

### Vấn đề đã biết

- Font `Fredoka-Bold SDF` lấy từ game gốc bị hỏng; tạo lại bằng code cũng lỗi. Đang dùng LiberationSans SDF. Cách sửa: Window → TextMeshPro → Font Asset Creator, nguồn `GameAssets/06_Fonts/Fredoka-Bold.ttf`.
- Sprite mũi tên đang là ảnh debug (`arrow-right@2x.png`).
- Bộ mặt mèo `boltie_01` bị bỏ vì khung hình bị xoay khi đóng gói atlas; còn 10 bộ.
- Thắng / hết mạng đang tự chuyển level sau 1.5 giây, chờ popup ở Giai đoạn 3.
