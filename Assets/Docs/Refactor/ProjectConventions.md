# Project Conventions – PawBlock

> Ngoại lệ / bổ sung **riêng dự án này** cho [UnityCodingGuidelines.md](UnityCodingGuidelines.md).
> Guide chung không sửa; chỗ nào dự án làm khác thì ghi ở đây, kèm lý do.

| | |
| --- | --- |
| Áp dụng từ | P0 – [RefactorPlan.md](RefactorPlan.md) |
| Cập nhật lần cuối | 2026-10-12 |

---

## 1. DOTween

Dự án dùng DOTween cho mọi animation, không dùng coroutine (đã quen từ RoadMap 06).

| # | Quy tắc | Lý do |
| --- | --- | --- |
| 1 | Mỗi tween giữ trong field (`Tween` / `Sequence`), không bắn rồi quên | Kill được khi phát lại / tắt object |
| 2 | Luôn `SetLink(gameObject)` | Object bị hủy thì tween chết theo, không lỗi null |
| 3 | `Kill()` trong `OnDisable` (và khi phát lại, khi trả về pool) | Object pool tắt / bật lại không còn tween cũ chạy dở |
| 4 | `OnKill` chỉ xóa field **nếu field vẫn trỏ đúng tween đó** (`if (_tween == t) _tween = null;`) | `Kill` gọi trong callback của chính tween bị DOTween hoãn; xóa thẳng sẽ xóa nhầm tween mới |
| 5 | Thứ cần trả về trạng thái gốc khi bị hủy (camera, scale, chữ khen về pool) đặt trong `OnKill` | `OnKill` chạy cả khi xong lẫn khi bị hủy giữa chừng |
| 6 | Popup / UI chạy lúc game dừng dùng `SetUpdate(true)` | Chạy theo thời gian thực, không bị `timeScale` |
| 7 | Sức chứa khởi tạo 500 tween / 150 sequence (`TweenSetup`) | Tránh DOTween tự nới sức chứa giữa ván (cảnh báo + giật) |

## 2. Comment và tài liệu

- **Comment tiếng Việt** trong toàn bộ code (guide 12 yêu cầu thống nhất một ngôn ngữ; dự án chọn tiếng Việt).
- Tên type / method / biến vẫn **tiếng Anh**, không dấu.
- Tài liệu trong `Assets/Docs/` viết tiếng Việt; RoadMap mỗi lần push một file (`Docs/RoadMap/00_README.md`).

## 3. Định dạng: chỉ áp cho file đang sửa

- `.editorconfig` ở gốc repo đặt mức **warning**, không error, vì code cũ chưa theo hết (vd `if (x) return;` một dòng, thiếu `private` trước hàm Unity).
- **Không format hàng loạt.** Chỉ sửa định dạng ở file đang sửa vì lý do khác, để diff refactor dễ đọc và ít conflict.
- Không dùng "Reformat Code" cho cả thư mục / solution.

## 4. Menu Editor

- Giữ gốc `Tools/PawBlock/…` cho mọi `MenuItem` (hiện 33 menu), thay vì `Tools/<Feature>/…` của guide 6.4.
- Lý do: người dùng đã quen; gom một chỗ dễ tìm. Bên trong có thể chia `Tools/PawBlock/<Feature>/…`.

## 5. Analyzer / IDE

- Không thêm `Microsoft.Unity.Analyzers.dll` vào `Assets/`:
  - **Rider** (External Script Editor hiện tại) có sẵn inspection Unity tương đương.
  - **VS Code**: extension *Unity* (`visualstudiotoolsforunity.vstuc`) đã kèm `Microsoft.Unity.Analyzers`; package `com.unity.ide.visualstudio` tự đưa vào `.csproj` khi chọn VS Code trong *Preferences → External Tools*.
- Nhờ vậy Unity không chạy analyzer lúc compile (không thêm cảnh báo vào Console, không rủi ro lệch phiên bản Roslyn).

## 6. Test

- Test của dự án nằm trong `Assembly-CSharp-Editor` (`Script/Editor/Tests/`), tới P6 mới tách asmdef.
- Chạy test **lọc theo assembly `Assembly-CSharp-Editor`**: Test Runner còn có ~890 test của package `com.besty.unity-skills` (vài test của package fail sẵn, không liên quan game).
