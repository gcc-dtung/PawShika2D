# 13 – Công cụ Editor: Builder, Debug Panel

Mọi code trong `Script/Editor/` chỉ chạy trong Unity Editor (không vào bản build).

## Menu Tools → PawBlock

| Menu | Việc | Đọc thêm |
| --- | --- | --- |
| **Level Editor** | Sửa / tạo / xếp level, ▶ chơi thử | [04](04_Level-DuLieuVaCongCu.md) |
| **Mechanic Injector** | Gắn cơ chế tự động theo lịch | [04](04_Level-DuLieuVaCongCu.md), [03](03_CoChe-Mechanic.md) |
| **Level Difficulty** | Độ khó lý thuyết, đánh dấu Hard | [../DifficultyFormula.md](../DifficultyFormula.md) |
| **Validate Levels** | Kiểm tra cả database | [04](04_Level-DuLieuVaCongCu.md) |
| **Debug Panel** (Ctrl + Shift + D) | Cheat khi Play | bên dưới |
| **Save Data** → Log / Open Folder / Delete | Xem / xóa `save.json` | [11](11_Save.md) |

### Builders (dựng asset / UI bằng code)

| Menu | Dựng |
| --- | --- |
| Import ShikaCat Levels | 205 level gốc + `LevelDatabase` (ghi đè thứ tự) |
| Reorder Levels | Xếp level theo độ khó + chạy Mechanic Injector |
| Setup Gameplay Visuals | 9-slice khối, cắt atlas mặt mèo, `GameplayVisuals.asset` |
| Setup Audio | `AudioDatabase`, object `AudioManager`, âm nút |
| Apply Game Font | Fredoka làm font mặc định, preset viền |
| Build Popups | `Canvas_Popup/Popups` + mọi popup cơ bản, `GameFlowUI` |
| Build Notice Popup | Popup NOTICE |
| Build Main Menu | `Canvas_Menu` (tab, Home, TopBar), `ScreenFlow` |
| Build UI Zone | Safe area cho HUD / Home |
| Build Shop | Tab Shop + `ShopDatabase.asset` |
| Build Boosters | 3 nút booster, RevealBanner, khung băng, `BoosterManager` |
| Build Settings | Nút bánh răng + `SettingsPopup` |
| Build Daily Reward | Database, manager, popup, nút Daily |
| Build Win Screen | Màn WELL DONE, confetti, material lóe sáng |
| Build Difficulty | `DifficultyTheme`, giao diện Hard / Very Hard |
| Build Tutorial | TutorialPanel, bàn tay, BoosterTutorial, MechanicIntroPopup + controller |

**Thứ tự khi dựng lại từ đầu:** Import ShikaCat Levels → Setup Gameplay Visuals → Apply Game Font → Build Popups → Build Main Menu → Build UI Zone → các Build còn lại → Setup Audio → Reorder Levels.

### Viết builder mới

Builder là `static class` trong `Editor/UI/`, có hàm `[MenuItem("Tools/PawBlock/Builders/Build Xxx")]`:

1. Tìm cha bằng đường dẫn cố định (`PopupBuilder.HudRoot`, `Canvas_Popup/Popups`…), **xóa object cũ cùng tên** rồi tạo lại → chạy nhiều lần vẫn ra một kết quả.
2. Tái dùng tiện ích của `PopupBuilder` (`LoadSprite`, `EnsureSprite`, `Assign` gán field `[SerializeField]` bằng `SerializedObject`…) thay vì viết lại.
3. Đánh dấu scene đã đổi (`EditorSceneManager.MarkSceneDirty`) để Unity hỏi lưu.
4. Ghi menu mới vào bảng trên và vào `ProjectStructure.md`.

## Debug Panel

Cửa sổ Editor gọi `Gameplay/Cheats/DebugCheats` (static) – chỉ dùng API có sẵn, nên popup / save / hiệu ứng chạy như chơi thật.

| Mục | Lệnh |
| --- | --- |
| Level | Trước / Chơi lại / Sau, đổi level, đổi + vào ván, chơi lại tutorial, xem lại popup cơ chế |
| Thắng / Thua | Thắng ngay, đặt 1 khối đúng, −1 mạng, hết mạng, thua hẳn |
| Coin | +100 / +1000 / +10000 / = 0 / đặt |
| Mạng / Năng lượng | +1 mạng, đầy, khiên; năng lượng +1 / đầy / 0 / bỏ qua chờ / vô hạn |
| Booster | +1 / +10 / = 0 từng loại, mở / khóa tất cả |
| Khác | Sang ngày mới, reset điểm danh, tốc độ game, thử IAP, lưu save |

Thêm cheat: thêm hàm static vào `DebugCheats` (gọi API công khai của manager), rồi thêm nút trong `DebugPanelWindow` ở mục tương ứng.
