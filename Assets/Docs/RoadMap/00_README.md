# RoadMap – Nhật ký thay đổi

**Mỗi lần push là một file.** Mọi thay đổi chưa push được ghi dồn vào file cuối cùng (trạng thái "Chưa push"). Khi push xong, file đó được chốt (ghi commit + giờ push) và thay đổi tiếp theo sang file mới với số kế tiếp.

## Quy tắc đặt tên

```text
NN_TenNganKhongDau.md
```

- `NN`: số thứ tự 2 chữ số (`01`, `02`, … `99`), tăng 1 sau mỗi lần push. Không đánh lại số cũ.
- Tên ngắn, không dấu, nối bằng `-`, mô tả nội dung chính của lần push.
- Ngày giờ ghi **trong** file, không đưa vào tên.

## Mẫu nội dung

```markdown
# NN – Tiêu đề

| | |
| --- | --- |
| Trạng thái | Chưa push / Đã push |
| Commit | <hash> "<message>" (khi đã push) |
| Thời gian push | YYYY-MM-DD HH:MM (khi đã push) |
| Cập nhật lần cuối | YYYY-MM-DD HH:MM |

## Tóm tắt
Bảng các đợt thay đổi trong lần push này.

## <Giờ> – <Tên đợt>
### Mục tiêu / Vấn đề
### Thay đổi (Thêm / Sửa / Xóa)
### Tác dụng
### Kiểm tra
```

## Danh sách

| # | Trạng thái | Push | Nội dung |
| --- | --- | --- | --- |
| [01](01_Core-GameplayVaCoCheMang.md) | Đã push | 2026-10-02 13:46 · `681d08c` | Phân tích, cơ chế mạng, Giai đoạn 1–2 (solver, 205 level, khối mèo, HUD) |
| [02](02_TaiLieu-ToChucScript.md) | Đã push | 2026-10-02 16:31 · `08ce8ec` | Tài liệu Docs, tổ chức thư mục Script, quyết định giữ CatFaceAnimator |
| [03](03_ToiUu-PrefabPool.md) | Đã push | 2026-10-02 19:39 · `7f9a805` | SelectionBox, ClueCell C# thuần, prefab + ObjectPool, sửa lỗi pool |
| [04](04_TaiMeo-LevelTest.md) | Đã push | 2026-10-02 20:52 · `67574be` | Tai mèo nhỏ lại; level test mọi cơ chế (level 206); kiểm tra level; hướng Square + ô nền điều hướng |
| [05](05_TachLogicView-FeelKeo.md) | Đã push | 2026-10-03 00:31 · `aaf54da` | Tách logic (`…Model`) / view (`…View`); khung trượt mượt, không phủ khối / Void, mép kẹt khi bị chặn; mèo xem trước + viền màu khi đủ value; màu khối = màu khung; đặt sai: mèo xám (chọn kiểu trong Inspector) |
| [06](06_Popup-GameState.md) | Chưa push | | Giai đoạn 3: `GameState`, popup Thắng / Hồi mạng / Thua / Xác nhận chơi lại, coin + ads giả lập; animation chuyển sang DOTween (bỏ viền đỏ còn 1 mạng) |

Giờ push lấy từ `git reflog origin/Day-1`; giờ từng đợt lấy từ thời gian sửa file và ảnh chụp thử.
