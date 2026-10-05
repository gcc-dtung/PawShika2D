# 10 – Mechanic Injector: gắn cơ chế cho level có sẵn

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-05 11:11 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 09:45 (05/10) | Mechanic Injector | Tool gắn Void / hướng / khóa / "?" vào 205 level ShikaCat theo lịch; 131 level có cơ chế, tất cả qua LevelValidator |
| 10:05 (05/10) | Xóa tool lỗi thời | Bỏ Build Energy, Build Play Zone, Create Test Level (đã chạy xong, kết quả nằm sẵn trong scene / asset) |
| 10:40 (05/10) | Độ khó lý thuyết + đánh dấu Hard / Very Hard | Tool Level Difficulty: mô phỏng giải, điểm D / thời gian / rủi ro theo công thức có trọng số; tick Hard / Very Hard; CSV ra / vào để so thực tế; đánh dấu 19 Hard + 8 Very Hard theo gợi ý |
| 10:45 (05/10) | Gom menu Builders | Tools → PawBlock chỉ còn tool dùng thường xuyên; 14 tool dựng scene / asset vào Tools → PawBlock → Builders |
| 10:57 (05/10) | Tutorial lần đầu | 4 bước như video bản gốc (3 ô ngang, 3 ô dọc, 3 + 6, 2 + 4 + 3), lời nhắc, vạch tiến độ, bàn tay kéo mẫu, Skip; không trừ mạng; xong vào thẳng Level 1 |
| 11:11 (05/10) | Tài liệu làm tutorial | `Docs/TutorialGuide.md`: cách tutorial chạy, sửa / thêm bước, chỉnh giao diện, kiểm tra, tạo tutorial cơ chế mới (popup hoặc màn tương tác) + checklist |

---

## 09:45 – Mechanic Injector

### Mục tiêu / Vấn đề

205 level ShikaCat chưa dùng cơ chế nào (0 level có khóa / "?" / hướng, 1 level có Void) dù gameplay và Level Editor đã hỗ trợ đủ. Cần thêm cơ chế mà không phải sửa tay từng level, và level sau khi gắn vẫn giải được không cần đoán.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Script/Editor/Level/MechanicInjectorSettings.cs` + `Levels/MechanicInjectorSettings.asset`: lịch cơ chế. Mặc định: giới thiệu Void từ level 12, Hướng 25, Khóa 40, "?" 60 (mỗi đợt 3 level); level thường 70% có cơ chế; level chia hết cho 10 là level nghỉ; từ level 70 có 25% kết hợp 2 cơ chế; level Hard / Very Hard luôn kết hợp 2 / 3 cơ chế; cơ chế mới nhất được ưu tiên ×3 trong 15 level sau đợt giới thiệu.
- `Script/Editor/Level/MechanicInjector.cs` (C# thuần):
  - `MakePlans`: lập lịch cho cả database. Đợt giới thiệu lấy N level **phù hợp** đầu tiên từ `introLevel`; level gốc phải đoán (17 level nhiều lời giải, đều trong khoảng 15–55) không nhận khóa / "?". Cơ chế chỉ xuất hiện ở level thường sau khi đã được giới thiệu.
  - `Inject`: nhân bản level, gắn lần lượt Void → Hướng → Khóa → "?"; mỗi lần gắn chạy `LevelValidator.Validate`, sai thì hoàn tác và thử chỗ khác (tối đa `maxAttempts`); gắn hỏng thì thay bằng cơ chế đã mở khác (trừ level giới thiệu).
  - **Void**: bỏ hẳn 1 khối của lời giải (ô thành Void, xóa số). Không thể sinh thêm lời giải, vì mọi lời giải mới cộng khối đã bỏ sẽ là lời giải của level cũ. Ưu tiên khối sát mép / sát Void và khối to; giữ lưới liền một mảng, tối đa 30% ô bị bỏ, còn ít nhất 5 khối; cắt hàng / cột ngoài cùng toàn Void.
  - **Hướng**: gắn đúng hình khối trong lời giải; chỉ chọn số mà mũi tên loại bớt được cách đặt (trọng số = số cách bị loại).
  - **Khóa**: 2–5 bước (level giới thiệu: 2), không vượt số khối còn lại.
  - **"?"**: chỉ giữ khi vẫn giải được không cần đoán.
  - Random `System.Random` gieo seed FNV-1a từ `seed` + số level nên chạy lại ra y hệt.
- `Script/Editor/Level/MechanicInjectorWindow.cs` – `Tools → PawBlock → Mechanic Injector`: sửa cấu hình ngay trong cửa sổ; chọn khoảng level; **Xem trước** (chỉ lập lịch), **Áp dụng** (ghi `Levels/Mechanic/Level_NNN.asset` cùng tên bản gốc, ghi đè giữ GUID, rồi thay vào `LevelDatabase`; level không có cơ chế thì trả về bản gốc và xóa bản Mechanic cũ), **Khôi phục bản gốc**, **Validate Levels**; danh sách kết quả từng level (✓ đã gắn, ! ghi chú), bấm tên level để mở Level Editor. `ApplyRange(from, to)` để gọi từ code. Level Custom / Test trong database được giữ nguyên.

**Sửa**

- `LevelValidator.Validate(level, alwaysCheckGuessing)`: tham số mới để kiểm tra "không cần đoán" cả khi level chưa có ô "?" / khóa (dùng để biết level gốc có gắn được khóa / "?" không).
- `LevelEditorWindow`: cảnh báo khi sửa level trong `Levels/Mechanic` (Áp dụng lại sẽ ghi đè, muốn giữ thì Nhân bản sang Custom).
- `LevelDatabase.asset`: 131 ô trỏ sang `Levels/Mechanic`.

### Tác dụng

- 131/205 level có cơ chế: Void 45, Hướng 42, Khóa 31, "?" 35 (một số level kết hợp). Level 1–11 giữ nguyên để làm quen.
- Bản gốc trong `Levels/ShikaCat` không bị sửa; Khôi phục bản gốc trả database về như cũ.
- Đổi lịch / seed trong `MechanicInjectorSettings.asset` rồi Áp dụng lại là ra bộ level mới.

### Kiểm tra

- Áp dụng toàn bộ: 7.1 s lần đầu (tạo asset), 0.7 s lần sau; 131 level có cơ chế, 0 level không gắn đủ theo lịch; `LevelValidator` toàn database: 0 lỗi.
- Chạy lại lần 2: hash 131 file trong `Levels/Mechanic` không đổi (kết quả tất định).
- Play mode: level 48 (2 vùng Void, lưới bậc thang) và level 53 (2 ô khóa 2 / 4 bước) hiển thị đúng; đã trả lại `save.json` như trước khi thử.
- Sửa lỗi trong lúc thử: lần Áp dụng thứ 2 báo "0 level có cơ chế" vì `Save` hủy bản tạm khi ghi đè asset cũ, rồi mới kiểm tra `result.level != null`. Level vẫn sinh đúng, chỉ sai số đếm; giờ lấy cờ trước khi lưu.

---

## 10:05 – Xóa tool lỗi thời

### Mục tiêu / Vấn đề

Menu Tools → PawBlock có nhiều tool chỉ dùng 1 lần để chuyển đổi bản cũ, đã chạy xong. Chỉ xóa nhóm này; các builder UI còn lại giữ để sửa UI sau này.

### Thay đổi (Thêm / Sửa / Xóa)

**Xóa**

- `Editor/UI/EnergyBuilder.cs` (Build Energy): gắn `EnergyManager` vào `Manager` và xóa tim năng lượng cũ trên HUD. Scene đã có cả hai.
- `Editor/UI/PlayZoneBuilder.cs` (Build Play Zone): dựng `Canvas_HUD/PlayZone`. Object đã có trong scene; `UIZoneBuilder` chỉ chuyển object này vào UIZone, không cần builder dựng lại.
- `Editor/Level/TestLevelBuilder.cs` (Create Test Level): asset `Levels/Test/Level_Test_AllMechanics.asset` vẫn giữ.

**Sửa**

- `ShikaCatLevelImporter`: chuyển `AppendTestLevels` từ `TestLevelBuilder` sang (private), Import ShikaCat Levels vẫn giữ level test ở cuối database.
- `ProjectStructure.md`, `README.md`: bỏ 3 tool đã xóa; README sửa câu "level ShikaCat chưa có ô đặc biệt" thành trỏ sang Mechanic Injector.

### Tác dụng

Menu Tools → PawBlock bớt 3 mục; không còn code tham chiếu tới 3 class đã xóa.

### Kiểm tra

- Biên dịch lại trong Unity: không lỗi console; `TestLevelBuilder` không còn trong `Assembly-CSharp-Editor`, `ShikaCatLevelImporter` / `MechanicInjectorWindow` vẫn nạp được.
- Grep toàn `Assets/_Project`: không còn tham chiếu `TestLevelBuilder`, `EnergyBuilder`, `PlayZoneBuilder`.

---

## 10:40 – Độ khó lý thuyết + đánh dấu Hard / Very Hard

### Mục tiêu / Vấn đề

Cần một thước đo độ khó **theo lý thuyết** cho từng level, có công thức rõ ràng, để sau này so với số liệu thực tế (tỉ lệ thắng, thời gian, số lần sai) và chỉnh lại. Đồng thời cần đánh dấu level Hard / Very Hard bằng tick thay vì chỉ có 2 level mẫu (20, 30).

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Script/Editor/Level/LevelDifficultyAnalyzer.cs`: mô phỏng người chơi giải từng khối; mỗi bước dùng kỹ thuật thấp nhất còn tác dụng: T1 đơn trực tiếp, T2 loại trừ 1 bước (cách đặt làm số khác hết chỗ / ô trống không ai phủ được), T3 thử sâu (`ShikakuSolver`), T4 không có nước bắt buộc (level nhiều lời giải). Có tính tới ô "?" / khóa chưa biết số, ô khóa chưa đặt được. Cho ra các chỉ số N, A, độ mơ hồ M, t1–t4, đường hẹp W, chuỗi bước. Tính điểm D, thời gian T, rủi ro R; có hàm Spearman / Pearson.
- `Script/Editor/Level/LevelDifficultySettings.cs` + `Levels/LevelDifficultySettings.asset`: trọng số công thức, tham số thời gian / rủi ro, tỉ lệ gợi ý Hard 10% / Very Hard 4%, chỉ gợi ý từ level 15.
- `Script/Editor/Level/LevelDifficultyWindow.cs` – `Tools → PawBlock → Level Difficulty`:
  - Biểu đồ D theo level: màu theo đánh dấu, vạch trắng là gợi ý khác đánh dấu, chấm xanh là dữ liệu thực tế.
  - Bảng: D (thanh), giây, rủi ro, T1/T2/T3/T4, gợi ý, **tick Hard / V.Hard**, chuỗi bước + cơ chế.
  - Lọc "Chỉ hiện Hard / Very Hard"; sắp theo level / D.
  - Nút Phân tích lại / Áp dụng gợi ý / Bỏ hết đánh dấu / Xuất CSV / Nạp dữ liệu thực tế (CSV cột `level` / `id` / `name` + cột số → ρ Spearman với D, giây, rủi ro).
  - Sửa trọng số trong cửa sổ thì điểm tính lại ngay, không phải mô phỏng lại.
  - Tick level trong `Levels/Mechanic` ghi cả bản gốc ShikaCat, để Mechanic Injector chạy lại vẫn giữ đánh dấu.
- `Docs/DifficultyFormula.md`: mô phỏng, chỉ số, công thức D / T / R, ý nghĩa trọng số, cách gợi ý, quy trình so với thực tế (định dạng CSV, Spearman, hồi quy lại trọng số bằng Python), giới hạn, kết quả lần đầu.

**Sửa**

- `DifficultyBuilder`: bỏ `MarkSampleLevels` (tự đánh dấu level 20 Hard / 30 Very Hard khi chạy Build Difficulty), vì chạy lại builder sẽ ghi đè đánh dấu mới.
- 27 level (`Levels/Mechanic` + bản gốc `Levels/ShikaCat`): `difficulty` theo gợi ý. **Hard**: 42, 44, 45, 53, 74, 76, 98, 102, 103, 105, 113, 123, 129, 135, 173, 185, 196, 198, 203. **Very Hard**: 65, 78, 143, 145, 168, 175, 182, 205. Level 20 / 30 về Normal.
- `ProjectStructure.md`, `README.md`: thêm tool, bỏ câu "đánh dấu mẫu level 20 / 30".

### Tác dụng

- Mỗi level có điểm D, thời gian và rủi ro ước tính, tính được lại bất kỳ lúc nào (sau khi sửa level hoặc chạy Injector). Có CSV để ghép với Analytics.
- Level 20 / 30 trước đây đánh dấu khó nhưng D chỉ 26.6 / 18.6 (toàn bước T1). Đánh dấu mới dựa trên D.
- Lưu ý: Mechanic Injector cho level Hard / Very Hard kết hợp 2 / 3 cơ chế. Bộ level hiện tại sinh khi 20 / 30 còn là Hard; chạy lại Injector thì 27 level mới đánh dấu sẽ có thêm cơ chế (khó hơn), 20 / 30 sẽ thành level thường.

### Kiểm tra

- Phân tích 206 level: ~0.14 s, không lỗi. D thấp nhất 10.0, trung vị 37.4, cao nhất 59.1. Kỹ thuật khó nhất: T1 43 level, T2 138, T3 13, T4 12 (đúng nhóm level nhiều lời giải).
- Spearman thử: (1..5) với (5,6,7,8,7) = 0.821; (1..4) với (4..1) = −1.000.
- Mở cửa sổ (biên dịch + vẽ): console không lỗi. Áp gợi ý: database có 19 Hard, 8 Very Hard; bản gốc ShikaCat của level Mechanic cũng đổi (vd. `ShikaCat/Level_143` = VeryHard).
- Chưa xem được giao diện cửa sổ bằng ảnh chụp (computer-use không chụp được cửa sổ Unity), cần xem lại bằng mắt.

---

## 10:45 – Gom menu Builders

### Mục tiêu / Vấn đề

Menu Tools → PawBlock có 22 mục lẫn lộn tool dùng hằng ngày với tool dựng scene / asset chỉ chạy khi sửa builder.

### Thay đổi (Thêm / Sửa / Xóa)

**Sửa**

- `MenuItem` có `priority`. Menu chính theo thứ tự: Level Editor (1), Mechanic Injector (2), Level Difficulty (3), Validate Levels (4), rồi sau vạch ngăn Debug Panel (20, Ctrl + Shift + D) và Save Data (21–23).
- 14 tool vào **Tools → PawBlock → Builders** (priority 100–113, xếp theo tên): Apply Game Font, Build Boosters / Daily Reward / Difficulty / Main Menu / Notice Popup / Popups / Settings / Shop / UI Zone / Win Screen, Import ShikaCat Levels, Setup Audio, Setup Gameplay Visuals.
- Chuỗi nhắc đường dẫn menu trong code (comment, log lỗi, HelpBox: `LevelManager`, `LevelDatabase`, `AudioManager`, `AudioDatabase`, `MechanicInjectorWindow`, các builder) và `ProjectStructure.md`, `README.md` đổi sang `Tools → PawBlock → Builders → …`.

### Tác dụng

Menu chính chỉ còn 6 mục cho người làm level / test; tool dựng nằm gọn trong 1 menu con.

### Kiểm tra

- Biên dịch: console không lỗi. `Menu.MenuItemExists`: `Tools/PawBlock/Builders/Build Shop` có, `Tools/PawBlock/Build Shop` không còn, `Level Editor` / `Level Difficulty` vẫn ở menu chính.
- Grep code + Docs: không còn đường dẫn menu cũ của 14 tool.

---

## 10:57 – Tutorial lần đầu

### Mục tiêu / Vấn đề

Làm tutorial như video bản gốc (`tutorial.mp4`): mở game lần đầu vào thẳng 4 màn hướng dẫn, xong thì vào Level 1.

Video gồm: loading → "Tutorial" + Skip + 4 vạch tiến độ + khung lời nhắc.
1. Hàng 3 ô, bàn tay kéo ngang ("First, drag to create a rectangle with 3 cells.").
2. Cột 3 ô, kéo dọc ("Try another 3-cell rectangle in a different direction.").
3. 3x3 có số 3 và 6 ("Each number tells you how many cells its rectangle should cover.").
4. 3x3 có 2, 4, 3, tự giải ("Now, draw a rectangle around every number using the correct number of cells.").

Sau đó vào Level 1.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Tutorial/TutorialLevels.cs`: 4 bước, `LevelDefinition` dựng trong code (không cần asset), lời giải, gợi ý bàn tay (số, ô bắt đầu, ô kết thúc).
- `Gameplay/Tutorial/TutorialController.cs` (trên `Manager`):
  - `Pending`: chưa `tutorialDone` và đang ở level 1, nên người chơi cũ không bị bắt chơi lại.
  - Nạp bước bằng `LevelManager.LoadTutorial`; thắng bước thì chờ 1.1 s (lưới lóe sáng) rồi sang bước kế; đặt khối thì bàn tay chuyển sang số kế.
  - Xong / Skip: lưu `tutorialDone`, nạp level hiện tại.
- `Gameplay/Tutorial/TutorialHand.cs`: bàn tay `tutorial_hand` (world, order 80) lặp: hiện → nhấn → trượt → nhấc → mờ; ẩn khi đang kéo.
- `UI/HUD/TutorialView.cs`: tiêu đề, Skip, vạch tiến độ (`tutorial_dot`, bước hiện tại sáng), khung lời nhắc (`popup_tutorial` 9-slice, nảy khi đổi bước); ẩn `TopPanel` / `DownPanel` trong lúc tutorial.
- `Editor/UI/TutorialBuilder.cs` – Tools → PawBlock → Builders → Build Tutorial: dựng `Canvas_HUD/UIZone/TutorialPanel`, object `TutorialHand` (đầu ngón tay ở gốc object), gắn `TutorialController`. Đã chạy và lưu `SampleScene`.

**Sửa**

- `SaveData`: thêm `tutorialDone`.
- `LevelManager`:
  - `Start` không nạp level khi tutorial chờ chạy.
  - Thêm `LoadTutorial(level)`: dựng lưới, căn camera, state Playing; không lưu level hiện tại, không phát `onLevelLoaded`.
  - `OnWin` bỏ qua khi đang tutorial (không hiện WELL DONE).
- `GridManager`: thêm `IsSelecting`; đặt sai trong tutorial chỉ báo sai, không trừ mạng.
- `ScreenFlow`: `Start` vào `EnterTutorial()` khi `TutorialController.Pending` (tắt Menu, bật HUD, `Begin`).
- `DebugCheats` + Debug Panel (mục Level): **Chơi lại tutorial**, **Bỏ cờ đã xem tutorial**.
- `ProjectStructure.md`, `README.md`.

### Tác dụng

Người chơi mới học luật qua 4 màn nhỏ trước Level 1; người chơi cũ (đã qua level 1) không bị ảnh hưởng.

### Kiểm tra (Play mode, save đặt về level 1 + chưa xem tutorial, đã trả lại save sau khi thử)

- Mở game: vào thẳng tutorial, HUD thường ẩn, bước 1 có bàn tay kéo từ ô trái sang ô phải, đầu ngón tay đúng tâm ô.
- Kéo bằng `GridManager.BeginSelection / UpdateSelection / EndSelection`:
  - bước 1 → 2 (vạch 2 sáng, bàn tay kéo dọc) → 3 (đặt 3 xong bàn tay chuyển sang số 6) → 4;
  - đặt sai ở bước 4 (số 2 nằm ngang): mèo cau có rồi rung ra, mạng vẫn 3;
  - giải xong: chữ khen như ván thường, sau ~1 s vào Level 1, HUD hiện lại, `tutorialDone = true`.
- Debug "Chơi lại tutorial" rồi Skip: về Level 1, TopPanel bật lại, panel tutorial tắt.
- Console không lỗi.
- Lưu ý khi test bằng MCP: Unity không focus thì Play mode chạy rất chậm (6.8 s game / ~80 s thực); focus cửa sổ Unity thì chạy bình thường.

---

## 11:11 – Tài liệu làm tutorial

### Mục tiêu / Vấn đề

Ghi lại các bước làm tutorial để sau này tự sửa / thêm bước hoặc làm tutorial mới mà không phải đọc lại code.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Docs/TutorialGuide.md`:
  1. Cách tutorial chạy: file nào làm gì, chỗ nào trong code hỏi `TutorialController`, sơ đồ luồng.
  2. Sửa một bước: `NewStep` / `Clue` / `Solve` / `hints`, tọa độ y = 0 ở dưới, ví dụ vẽ lưới, quy tắc (tổng số, 1 lời giải, gợi ý đúng khối).
  3. Thêm / bớt bước (chạy lại Build Tutorial khi số bước đổi), bước có cơ chế.
  4. Bảng chỉnh giao diện / cảm giác (builder, Inspector, sprite).
  5. Kiểm tra: Debug Panel, thử như người chơi mới, đoạn code kiểm lời giải bằng `LevelValidator`, lưu ý focus Unity khi thử bằng MCP.
  6. Tạo tutorial mới cho cơ chế:
     - Cách A: popup với ảnh có sẵn `popup_tutorial_2/3/4`, cờ đã xem, điểm kích hoạt.
     - Cách B: màn tương tác, tổng quát hóa `TutorialController.Begin(steps, onFinish)`.
     - Checklist.

**Sửa**

- `README.md`: liên kết `DifficultyFormula.md`, `TutorialGuide.md` ở đầu trang và ở mục Build Tutorial.
- `ProjectStructure.md`: liên kết từ mục Gameplay/Tutorial.

### Kiểm tra

- Đoạn kiểm lời giải trong tài liệu chạy trong Editor: `tutorial_1..4` đều OK (`LevelValidator.Validate(level, alwaysCheckGuessing: true)`).
