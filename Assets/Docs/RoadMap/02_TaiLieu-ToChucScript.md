# 02 – Tài liệu và tổ chức thư mục Script

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `08ce8ec` "Hiện tại vẫn ổn nên push" (nhánh `Day-1`) |
| Thời gian push | 2026-10-02 16:31 |
| Cập nhật lần cuối | 2026-10-02 17:12 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 13:48 | Tài liệu Docs | README, TaskBreakdown, ProjectStructure |
| 13:53 | Tổ chức thư mục Script | Chia Gameplay / UI / Editor theo tính năng |
| Chiều (trước 16:31) | Quyết định CatFaceAnimator | Giữ script, không dùng Animator (không đổi file) |

---

## 13:48 – Tài liệu dự án

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Docs/README.md` | Giới thiệu, luật chơi, cách chạy, nguồn tài nguyên, trạng thái, lỗi đã biết |
| Thêm | `Docs/TaskBreakdown.md` | Quyết định, câu hỏi mở, việc theo 6 giai đoạn, rủi ro |
| Thêm | `Docs/ProjectStructure.md` | Thư mục, vai trò script, scene, luồng nước đi, event, quy ước |

**Tác dụng:** người mới đọc README là chạy được, đọc TaskBreakdown là biết việc còn lại; ProjectStructure là nơi tra quy ước (tọa độ, sortingOrder).

---

## 13:53 – Tổ chức lại thư mục Script

Di chuyển 24 file bằng `AssetDatabase.MoveAsset` (giữ GUID trong `.meta`):

```text
Script/
├── Gameplay/
│   ├── Core/       GridManager, GridSpawner, Cell, ClueCell
│   ├── Level/      LevelManager, LevelDefinition, LevelDatabase
│   ├── Lives/      LivesManager
│   ├── Block/      PlacedBlock, CatFaceAnimator, ColorType
│   ├── Input/      InputManager, DragAndDropController, SelectionBox, DragCountBubble
│   ├── Solver/     ShikakuSolver
│   ├── Feedback/   GameplayFeedback
│   ├── Visuals/    GameplayVisuals, BackgroundFitter
│   └── Util/       Singleton
├── UI/HUD/         LevelHUD, LivesUI
└── Editor/
    ├── Level/      ShikaCatLevelImporter
    └── Visuals/    GameplayVisualsSetup
```

**Tác dụng:**
- Tìm file theo tính năng; tính năng mới có chỗ rõ ràng (`Gameplay/Booster/`, `UI/Popups/`…).
- Không mất tham chiếu trong scene / prefab (đã kiểm tra: 0 component Missing).
- Chưa dùng `.asmdef` nên không phải sửa code.

**Lưu ý:** thư mục `Editor` phải giữ đúng tên; `Singleton` nên chuyển ra `Script/Common/Util` khi UI cũng dùng.

---

## Chiều – Quyết định: giữ CatFaceAnimator, không dùng Animator

Không sửa file nào.

| | Script hiện tại | Animator |
| --- | --- | --- |
| Chi phí mỗi khung hình | Một phép tính + gán sprite khi đổi khung | Máy trạng thái, blend, lấy mẫu curve |
| Dữ liệu cần | 1 danh sách sprite / bộ mặt | 10 clip + controller + override controller |
| Bộ nhớ sprite | Như nhau | Như nhau |

Mỗi bộ mặt chỉ là 1 vòng lặp, không có trạng thái để chuyển, nên Animator không mang lại lợi ích. Xem lại khi cần biểu cảm phản ứng theo gameplay, hoặc khi cần giảm bộ nhớ atlas mặt mèo (nén texture, giảm số khung).
