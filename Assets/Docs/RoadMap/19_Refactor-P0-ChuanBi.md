# 19 – Refactor P0: chuẩn bị và mốc so sánh

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-12 11:55 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 11:55 (12/10) | Refactor P0 – chuẩn bị, mốc so sánh | `.editorconfig` (Allman, `{}`, access modifier, đặt tên; mức warning); `ProjectConventions.md` (DOTween, comment tiếng Việt, chỉ format file đang sửa, menu `Tools/PawBlock`, analyzer qua IDE); `Baseline.md` (104 test, Batches / SetPass 3 màn, APK 58.9 MB + asset nặng nhất, smoke test một phần); 3 save mẫu v1. Không đổi code game |

---

## 11:55 (12/10) – Refactor P0: chuẩn bị, mốc so sánh

### Mục tiêu

Giai đoạn P0 của [RefactorPlan.md](../Refactor/RefactorPlan.md): có mốc đo trước / sau và công cụ tự kiểm tra, chưa đổi code game.

### Thay đổi

**Thêm**

| File | Nội dung |
| --- | --- |
| `.editorconfig` (gốc repo) | UTF-8, 4 dấu cách; C#: ngoặc Allman, bắt buộc `{}` (IDE0011), access modifier tường minh (IDE0040), namespace dạng block (IDE0160); đặt tên theo guide 6.1 (type / method / property / event PascalCase, interface `I…`, `const` / `static readonly` PascalCase, biến cục bộ / tham số camelCase, field private `_camelCase`). Mức **warning**; field chỉ suggestion vì `[SerializeField] private` (camelCase) không tách được bằng editorconfig |
| `Docs/Refactor/ProjectConventions.md` | Ngoại lệ riêng dự án: 7 quy tắc DOTween, comment tiếng Việt, chỉ format file đang sửa, menu `Tools/PawBlock/…`, analyzer qua IDE, chạy test lọc assembly `Assembly-CSharp-Editor` |
| `Docs/Refactor/Baseline.md` | Mốc đo: test, Batches / SetPass 3 màn, Build Report Android, smoke test, mô tả save mẫu; có cột "Sau" để điền ở P8 |
| `Docs/Refactor/SaveSamples/` | `save_v1_new.json` (người mới), `save_v1_played.json` (save thật level 22, booster / daily / challenge / profile), `save_v1_infinite_energy.json` (năng lượng vô hạn tới 2100) |

**Sửa**

| File | Nội dung |
| --- | --- |
| `Docs/Refactor/RefactorPlan.md` | Trạng thái "P0 xong"; tick checklist P0; nhánh dùng `Refactor` (Windows không cho tạo `refactor/...` khi đã có `Refactor`); estimate P0 thực tế 2 h / dự kiến 4.5 h, tổng 4 % |

**Không thêm** `Microsoft.Unity.Analyzers.dll` vào project: External Script Editor là Rider (có inspection Unity sẵn), VS Code đã có bản kèm extension Unity.

### Số liệu mốc

| | Giá trị |
| --- | --- |
| Test (`Assembly-CSharp-Editor`) | 104 / 104 đạt, 9.5 s |
| Menu | 63 batches / 38 SetPass |
| Màn chơi level 39 (10 x 10) | 58 / 46 |
| Popup Win | 133–136 / 94–97 (confetti) |
| APK Android | 58.9 MB; texture 140 MB / 145.5 MB asset; 4 atlas mặt mèo Spine 15.8 MB mỗi cái |

### Tác dụng

Có số để so khi refactor xong (P8), có file save thật để test migrate (P3), IDE báo lệch quy tắc ngay khi sửa file.

### Kiểm tra

- Test EditMode 104 / 104 xanh; Console sạch khi vào Menu → level 39 → thắng.
- Build Android 0 lỗi / 0 cảnh báo.
- Đo xong trả `save.json` máy dev về bản gốc.
- `git status`: không file code / scene / prefab nào đổi.

### Ghi chú

- Đổi nhánh làm Unity import lại asset Spine Freeze / Noti: material trỏ sang shader GUID không tồn tại (`b3e79…`) và mất material Additive của Freeze → đã `git restore` về bản đúng; sau đó Unity chỉ ghi lại các file đó với kiểu xuống dòng LF (nội dung không đổi), không commit.
- Xóa thư mục rỗng trùng `Assets/Editor 1` do Unity tạo.
- Smoke test mới chạy một phần (danh sách trong `Baseline.md` mục 4); phần còn lại cần chạy tay.
