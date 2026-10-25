# 13 – Công cụ Editor, Debug Panel, Cheat Panel

Code Editor nằm trong `Runtime/Editor/<mảng>/` (`Core`, `UI`, `Gameplay`, `App`), assembly `PawBlock.Editor` chỉ chạy trong Unity Editor (không vào bản build).

> Từ P7 **không còn builder dựng UI bằng code**: UI là prefab, sửa trong Prefab Mode ([10](10_UI-Menu-Popup.md)). Menu "Builders" còn lại chỉ tạo / điền asset dữ liệu.

## Menu Tools → PawBlock

| Menu | Việc | File | Đọc thêm |
| --- | --- | --- | --- |
| **Level Editor** | Sửa / tạo / xếp level, ▶ chơi thử | `Runtime/Editor/Gameplay/Level/LevelEditorWindow` | [04](04_Level-DuLieuVaCongCu.md) |
| **Level Generator** | Sinh level theo seed hàng loạt | `LevelGeneratorWindow` | [16](16_DailyChallenge.md) |
| **Mechanic Injector** | Gắn cơ chế tự động theo lịch | `MechanicInjectorWindow` | [04](04_Level-DuLieuVaCongCu.md), [03](03_CoChe-Mechanic.md) |
| **Level Difficulty** | Độ khó lý thuyết, đánh dấu Hard, so số liệu thật | `LevelDifficultyWindow` | [../DifficultyFormula.md](../DifficultyFormula.md) |
| **Validate Levels** | Kiểm tra cả database | `LevelValidator` | [04](04_Level-DuLieuVaCongCu.md) |
| **Debug Panel** (Ctrl + Shift + D) | Cheat khi Play | `Runtime/Editor/App/DebugPanelWindow` | bên dưới |
| **Save Data** → Log / Open Folder / Delete | Xem / xóa `save.json` | `Runtime/Editor/Core/SaveDataMenu` | [11](11_Save.md) |
| **UI → Setup Buttons** | Gắn `ButtonPress`, thu danh sách nút cho mọi scene / prefab (tự chạy khi lưu scene / prefab, vào Play) | `Runtime/Editor/UI/UIAutoSetup` | [10](10_UI-Menu-Popup.md) |
| **Game → Collect Bootstrap References** | Điền mọi field của `GameBootstrap` (tự chạy khi lưu scene / vào Play) | `Runtime/Editor/App/GameBootstrapCollector` | |

### Builders (tạo / điền asset)

| Menu | Việc | File |
| --- | --- | --- |
| Import ShikaCat Levels | 205 level gốc (từ `ThirdParty/ShikaCat`) + `LevelDatabase` (ghi đè thứ tự) | `Runtime/Editor/Gameplay/Level/ShikaCatLevelImporter` |
| Reorder Levels | Xếp level theo độ khó + chạy Mechanic Injector | `LevelOrderTool` |
| Setup Gameplay Visuals | 9-slice khối, cắt atlas mặt mèo, `GameplayVisuals.asset` | `Runtime/Editor/Gameplay/Builders/GameplayVisualsSetup` |
| Setup Audio | Bổ sung `AudioDatabase`, gán vào `AudioManager` | `Runtime/Editor/App/AudioSetup` |
| Setup Game Config | Nối `GameConfig` tới config của từng feature (tìm theo kiểu) | `Runtime/Editor/App/GameConfigSetup` |
| Apply Game Font | Fredoka làm font mặc định, preset viền | `Runtime/Editor/UI/GameFontSetup` |

Mọi tool tạo / di chuyển asset qua `AssetDatabase` để giữ GUID; đường dẫn viết cứng trong tool phải đổi theo khi dời thư mục ([ProjectConventions](../Refactor/ProjectConventions.md) mục 13).

### Dựng sẵn object con cho component

Component cần object con (hiệu ứng, overlay...) có `[ContextMenu("EditorBuild")]` dùng `EditorChildren` (`Runtime/Core/Foundation/Util`) để tạo sẵn trong prefab, thay vì tạo lúc chạy. Tham chiếu tự điền trong `OnValidate` (`#if UNITY_EDITOR`).

## Debug Panel

Cửa sổ Editor gọi `Runtime/Cheat/DebugCheats` (static) – chỉ dùng API có sẵn, nên popup / save / hiệu ứng chạy như chơi thật.

| Mục | Lệnh |
| --- | --- |
| Level | Trước / Chơi lại / Sau, đổi level, đổi + vào ván, vào ván bỏ qua năng lượng, chơi lại tutorial, xem lại popup cơ chế |
| Thắng / Thua | Thắng ngay, đặt 1 khối đúng, hết giờ (popup Thêm giờ), thua hẳn |
| Coin, Sao ★ | +100 / +1000 / +10000 / = 0 / đặt; sao +10 / +50 / −10 / = 0 / đặt |
| Đồng hồ | −10 s / +10 s / +60 s / đầy giờ / dừng, Đóng băng |
| Năng lượng | +1 / đầy / 0 / bỏ qua 1 lần chờ / vô hạn 5 phút |
| Booster | +1 / +10 / = 0 từng loại, tất cả +10, mở / khóa tất cả |
| Khác | Sang ngày mới, reset điểm danh, tốc độ game, thử IAP, lưu save |

**Cheat Panel trong bản build** (`Runtime/Cheat/CheatPanel`, IMGUI): bấm nhanh 3 lần góc trên trái màn hình (hoặc F1 / ` khi có bàn phím); gọi cùng `DebugCheats`. Đây là ngoại lệ duy nhất được tự tạo / tìm object lúc chạy ([ProjectConventions](../Refactor/ProjectConventions.md) mục 10).

Thêm cheat: thêm hàm static vào `DebugCheats` (gọi API công khai của component), rồi thêm nút trong `DebugPanelWindow` (và `CheatPanel` nếu cần trong build).
