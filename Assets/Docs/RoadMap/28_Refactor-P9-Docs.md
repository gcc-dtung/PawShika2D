# 28 – Refactor P9: tài liệu và dọn dẹp

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | — (nhánh `Refactor-P9-Docs`) |
| Thời gian push | — |
| Cập nhật lần cuối | 2026-10-13 23:30 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 23:03 (13/10) | Merge P8 | `Refactor` fast-forward tới `3fbc456`, tạo nhánh `Refactor-P9-Docs` |
| 23:09 (13/10) | P9.1 – README feature | 7 README trong `Features/<F>/Docs/` |
| 23:09 (13/10) | P9.2 – ProjectStructure | Viết lại theo module, scene, event hiện tại |
| 23:22 (13/10) | P9.3 – tài liệu tính năng | 16 file `Docs/Features` + 2 HowTo + TutorialGuide + README + DifficultyFormula theo code hiện tại |
| 23:23 (13/10) | P9.4 – comment code | 24 file, chỉ comment / chữ Header |
| 23:24 (13/10) | P9.5 – scene | `SampleScene` → `Main` |
| 23:27 (13/10) | P9.6 – checklist | Rà guide mục 15, ghi nợ vào ProjectConventions mục 14 |

---

## P9.1 – README feature

| Feature | Nội dung chính |
| --- | --- |
| Gameplay | Bảng class theo thư mục (Board, Block, Input, Level, Solver, Timer, Stars, Booster, Intro, Tutorial, Feedback, UI, Visuals, Analytics), tool Editor, event phát, interface cài |
| Economy | Ví coin / sao, năng lượng, HUD, popup nạp; hai kênh báo đổi (event trong feature + event contract) |
| Shop | `ShopService` chỉ biết contract; cách gắn SDK IAP thật |
| DailyReward | Điểm danh + cài `IRewardGranter` / `IRewardIcons` cho DailyChallenge, LuckyWheel |
| DailyChallenge | Level ngày do Gameplay sinh qua `IDailyLevelLoader`, quà qua `IRewardGranter` |
| LuckyWheel | Trao quà trước khi diễn vòng quay |
| Profile | Chỉ số lấy hoàn toàn từ event contract |

## P9.3 – tài liệu tính năng

- Đường dẫn: `Gameplay/Core/...`, `UI/HUD/...`, `Script/...` → `Features/<F>/Scripts/...`, `Core/...`, `Shared/...`, `Game/...`.
- Save: `SaveData.x` / `SaveSystem` → section (`"gameplay"`, `"economy"`...) + `ISaveService.Get<T>(key)` / `MarkDirty`.
- Kiến trúc: `.Instance` / `Singleton<T>` → `Init` từ `GameBootstrap`; builder dựng UI → prefab (Prefab Mode).
- Cơ chế: mạng / Khiên / popup Hồi mạng (đã bỏ từ `Old-Core`) → đồng hồ / Đóng băng / Thêm giờ; `02_Mang-VongDoiVan` → `02_ThoiGian-VongDoiVan` (qua `AssetDatabase`, GUID giữ).
- Đối chiếu code: listener của `GridManager` / `LevelTimer` / `BoosterManager`, âm / rung nối ở đâu, chủ khóa `InputGate`, thứ tự intro, API `IBoosterHost` / `ILevelIntro` / `ISaveService`, nút Debug Panel.
- `TaskBreakdown.md`: giữ nguyên nội dung, thêm ghi chú "tài liệu lịch sử".

## P9.6 – checklist guide 15

| Mục | Kết quả |
| --- | --- |
| `using` sai chiều | 0 (1 trong test, chấp nhận) |
| `.Instance` game logic | 0 |
| Find / GetComponent lúc chạy | Chỉ `CheatPanel` (ngoại lệ) |
| Field public trong MonoBehaviour | 0 |
| Tween / event | Có hủy (listener nút con cùng vòng đời: chấp nhận) |
| Thư mục, atlas, Resources | Đạt |
| Còn nợ | Hook rỗng `PopupView`, `Debug.Log` thường ở 15 file, tên cũ `shield` / `ShieldFrost`, Shop chưa atlas, chưa thử máy Android thật |

- Kiểm tra: 160 / 160 test; Play `Main.unity` với save mẫu (Menu → level 23), Console 0 lỗi; save thật giữ nguyên.
