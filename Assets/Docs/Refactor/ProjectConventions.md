# Project Conventions – PawBlock

> Ngoại lệ / bổ sung **riêng dự án này** cho [UnityCodingGuidelines.md](UnityCodingGuidelines.md).
> Guide chung không sửa; chỗ nào dự án làm khác thì ghi ở đây, kèm lý do.

| | |
| --- | --- |
| Áp dụng từ | P0 – [RefactorPlan.md](RefactorPlan.md) |
| Cập nhật lần cuối | 2026-10-12 (P2) |

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

- Test của dự án nằm trong `Assembly-CSharp-Editor` (`<module>/Tests/Editor/`, từ P1), tới P6 mới tách asmdef.
- Chạy test **lọc theo assembly `Assembly-CSharp-Editor`**: Test Runner còn có ~890 test của package `com.besty.unity-skills` (vài test của package fail sẵn, không liên quan game).
- Test đụng tới save (ví, kho booster, quà...) **bắt buộc** gọi `SaveSystem.UseInMemoryForTests(new SaveData { ... })` ở `[SetUp]` và `UseInMemoryForTests(null)` ở `[TearDown]`: ở Edit mode `MarkDirty()` ghi thẳng `save.json` của máy. P3 thay bằng `ISaveService` giả.
- `FeelPlayModeTests` hiện vẫn ghi `save.json` khi vào Play mode (lỗi có sẵn, xem RefactorPlan P2): chạy cả bộ test thì sao lưu save trước, hoặc chạy lọc theo tên class.

## 7. Namespace và thư mục (từ P1)

| Quy tắc | Ví dụ | Lý do |
| --- | --- | --- |
| **Một namespace cho mỗi module / feature**, thư mục con không thêm cấp (khác ví dụ `Core.Foundation.Save` của guide 5) | `Core/Foundation/Save/SaveSystem.cs` → `PawBlock.Core.Foundation`; `Features/Gameplay/Scripts/Board/BoardModel.cs` → `PawBlock.Features.Gameplay` | Ít `using`; thư mục con trong một module gọi thẳng nhau (guide 5) |
| Code Editor dùng **cùng namespace** với module, không thêm `.Editor` | `Features/Gameplay/Editor/Level/LevelEditorWindow.cs` → `PawBlock.Features.Gameplay` | Namespace có đoạn `Editor` che mất class `UnityEditor.Editor` (`: Editor` báo lỗi) |
| Test: namespace module + `.Tests` | `PawBlock.Features.LuckyWheel.Tests` | Tách khỏi code thật, vẫn thấy type của module |
| Không đặt type trùng tên namespace (`Gameplay`, `Shop`, `Profile`, `Settings`, `Game`, `Core`…) | — | Trong `PawBlock.*`, tên đó sẽ trỏ vào namespace |
| `Core` và `Shared` không có `Scripts/`; feature và `Game` có `Scripts/` | `Core/Foundation/Audio/`, `Features/Shop/Scripts/` | Theo cây thư mục guide 5 |
| Tối đa 4 tầng từ `_Project/`: gộp `Booster/Effects` vào `Booster/`, `Level/Generator` vào `Level/` | `Features/Gameplay/Scripts/Booster/FreezeBooster.cs` | Guide 5 |

- P1 **không** sửa định dạng (`if` một dòng, `private` thiếu…) dù đụng mọi file: chỉ bọc namespace + thụt lề + `using`, để diff chỉ có thay đổi cơ học. Định dạng sửa dần khi sửa logic từng file (mục 3).
- Phụ thuộc sai chiều lộ ra sau khi có namespace: xem [Dependencies.md](Dependencies.md).
