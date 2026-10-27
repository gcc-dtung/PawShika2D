# 04 – Level: dữ liệu và công cụ

> Asset level ở `Assets/_Project/Data/Levels/` (viết tắt `Levels/`); tool ở `Runtime/Editor/Gameplay/Level/`; code chạy ở `Runtime/Gameplay/Level/`.

## Dữ liệu

| Asset | Nội dung |
| --- | --- |
| `Levels/LevelDatabase.asset` | Danh sách `LevelDefinition` theo **thứ tự chơi** (level n = phần tử n − 1) |
| `Levels/ShikaCat/Level_001..205` | Level gốc nhập từ JSON ShikaCat – **không sửa tay** (Mechanic Injector đọc từ đây) |
| `Levels/Mechanic/Level_NNN` | Bản đã gắn cơ chế, thay bản gốc trong database |
| `Levels/Custom/` | Level tự làm bằng Level Editor (Level 1 = `Level_Custom_Level01_4x4`) |
| `Levels/Test/`, `Levels/Generated/` | Level thử; level do Level Generator lưu (tạo khi lưu lần đầu) |
| `Data/Config/DifficultyTheme.asset` | Giao diện level Hard / Very Hard |

`LevelDefinition`:

| Trường | Ý nghĩa |
| --- | --- |
| `size`, `height` | Số cột, số hàng (`height = 0` = vuông) |
| `cellOverrides[]` | Ô đặc biệt theo vị trí (hiện chỉ có `Void`) |
| `clues[]` | `id`, `x`, `y`, `value`, `color`, `mechanic` (xem [03](03_CoChe-Mechanic.md)) |
| `solution[]` | Lời giải tham khảo – game **không** dựa vào, chỉ tool dùng |
| `difficulty` | `Normal` / `Hard` / `VeryHard` |
| `rules.timeLimit` | Giây của level (0 = tự tính, [02](02_ThoiGian-VongDoiVan.md)) |

## Luồng nạp level lúc chơi

`LevelManager.LoadLevel(index)` → lưu `GameplaySave.currentLevel` (section `"gameplay"`) → `GridSpawner.Spawn(level)` (sinh ô, phát `gridCellEvent`) → `GridManager` tạo `BoardModel` mới, `LevelTimer` tính lại giờ, `LevelStars` về 3 sao, `BoardCameraFitter` căn camera → `EventBus<LevelLoadedEvent>` (HUD, `GameFlowUI` đóng popup, `BoosterManager`, `LevelIntroSequencer` → băng HARD / popup cơ chế / tutorial booster cùng nghe). Daily Challenge: `LevelManager.LoadDaily(level, day)` với level sinh trong bộ nhớ ([16](16_DailyChallenge.md)).

## Công cụ Editor

| Menu | Việc | File |
| --- | --- | --- |
| Tools → PawBlock → **Level Editor** | Vẽ khối (số tự = diện tích), đặt số / Void / xóa; sửa hướng, khóa, "?", Hard; Kiểm tra; Giải; ▶ Chơi thử vào thẳng level; đổi thứ tự, nhân bản | `LevelEditorWindow` |
| Tools → PawBlock → **Level Generator** | Sinh hàng loạt level theo seed (preset Easy / Normal / Hard / Daily, đúng 1 lời giải), xem độ khó, lưu vào `Levels/Generated` (+ cuối database). Chi tiết: [16_DailyChallenge.md](16_DailyChallenge.md) | `LevelGeneratorWindow`, `Runtime/Gameplay/Level/LevelGenerator` |
| Tools → PawBlock → **Validate Levels** | Chạy `LevelValidator` cho cả database, log lỗi / cảnh báo | `LevelValidator` |
| Tools → PawBlock → **Mechanic Injector** | Gắn cơ chế theo lịch: Xem trước → Áp dụng / Khôi phục bản gốc | `MechanicInjector*` |
| Tools → PawBlock → **Level Difficulty** | Độ khó lý thuyết D, biểu đồ, tick Hard / Very Hard, xuất / nạp CSV | `LevelDifficulty*`, công thức: [../DifficultyFormula.md](../DifficultyFormula.md) |
| Builders → **Reorder Levels** | Xếp ShikaCat theo độ khó, đỉnh ...9 / nghỉ ...0, đánh dấu Hard, rồi chạy lại Injector | `LevelOrderTool` |
| Builders → **Import ShikaCat Levels** | JSON → 205 `LevelDefinition` + database (**ghi đè thứ tự**, mất bản Mechanic trong database) | `ShikaCatLevelImporter` |

### LevelValidator kiểm tra gì

1. **Dữ liệu**: clue trong lưới, không trùng, không trên Void, số > 0, tổng các số = số ô chơi được, luật kết hợp cơ chế.
2. **Có lời giải** (`ShikakuSolver`).
3. **Không cần đoán** (chỉ khi có Khóa / "?"): xem [03](03_CoChe-Mechanic.md#quy-tắc-không-cần-đoán).

### Mechanic Injector chạy thế nào

```text
MakePlans(settings, levels gốc)   → mỗi level: danh sách cơ chế + lý do (giới thiệu / thường / kết hợp / nghỉ / Hard)
Inject(level gốc, plan, settings) → nhân bản level; với mỗi cơ chế:
    TryVoid / TryConstraint / TryLock / TryHidden
        chọn chỗ (random theo seed + số level) → gắn → LevelValidator
        sai → Snapshot.Restore, thử chỗ khác (tối đa maxAttempts)
    gắn hỏng → thay bằng cơ chế đã mở khác (level giới thiệu thì không thay)
MechanicInjectorWindow            → lưu Levels/Mechanic/Level_NNN, thay vào database
```

Cùng settings + cùng seed = kết quả y hệt (hash FNV-1a). Đổi `seed` = bộ level khác.

### Thứ tự thường làm khi đổi level

1. Sửa / thêm level (Level Editor) hoặc đổi trọng số độ khó.
2. **Reorder Levels** (tự chạy lại Mechanic Injector).
3. **Level Difficulty** → kiểm tra biểu đồ, tick Hard nếu cần.
4. **Validate Levels** → không còn lỗi.
5. Play thử bằng ▶ trong Level Editor hoặc Debug Panel → Level.

## Level Hard / Very Hard

`LevelDefinition.difficulty` khác `Normal` thì: nút PLAY đổi mặt + tab mức khó, chữ "Level n" trắng đổi viền (đỏ / tím, `playLevelOutline`), khung LEVEL trên HUD có tab, băng "HARD / EXTREME LEVEL" chạy khi vào ván (`DifficultyIntro`), Mechanic Injector gắn 2–3 cơ chế khó. Giao diện mỗi mức lấy từ `DifficultyTheme.asset`.

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Thêm level tay | Level Editor → Mới (vào `Levels/Custom`) → Kiểm tra → đặt vị trí trong database |
| Đổi lịch cơ chế | Sửa `MechanicInjectorSettings.asset` → Mechanic Injector → Xem trước → Áp dụng |
| Thêm mức khó mới (vd. Nightmare) | Thêm giá trị **cuối** `LevelDifficulty` (`Runtime/Gameplay/Level/DifficultyTheme.cs`), thêm 1 `DifficultyStyle` trong `DifficultyTheme.asset` (HUD, nút PLAY, băng intro đọc qua `theme.Get(difficulty)` nên tự nhận). Rồi xem các chỗ so sánh trực tiếp: `EconomyConfig.WinCoinReward` (coin thắng), `LevelOrderTool` (đánh dấu tự động), `MechanicInjector.MakeNormalPlan` (số cơ chế), `BoosterTutorial` (bỏ qua level khó) |
| Thêm trường cho level (vd. booster cấm dùng) | Thêm field vào `LevelDefinition` (giá trị mặc định = hành vi cũ), sửa Level Editor để chỉnh, đọc ở runtime |

## Lưu ý

- Chạy lại **Import ShikaCat Levels** đưa database về bản gốc: phải chạy lại Reorder Levels (hoặc Mechanic Injector → Áp dụng), và kéo Level 1 Custom về đầu.
- Đừng sửa tay file trong `Levels/Mechanic/` – lần Áp dụng sau sẽ ghi đè. Muốn sửa 1 level cụ thể: nhân bản sang `Custom` rồi sửa.
