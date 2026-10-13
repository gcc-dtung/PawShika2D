# Paw Block – Bản làm lại trên Unity

Game giải đố Shikaku dựa trên [Paw Block Puzzle](https://play.google.com/store/apps/details?id=com.phoenix.pawblock.puzzle): kéo hình chữ nhật trên lưới để tạo khối mèo, phủ kín lưới trước khi hết giờ. Dự án portfolio (không có tài khoản store / ads thật: IAP và quảng cáo là bản giả lập).

- Unity **2022.3.62f3**, URP 2D, TextMeshPro, uGUI, Input System, DOTween (`Assets/Plugins/Demigiant`), spine-unity 4.1, UI Particle
- Scene: `Assets/_Project/Scenes/SampleScene.unity`
- Kiến trúc module, thư mục, class, scene, event: [ProjectStructure.md](ProjectStructure.md)
- **Tài liệu theo tính năng + hướng dẫn mở rộng (thêm cơ chế, booster, popup…)**: [Features/](Features/00_README.md); mỗi feature có `Assets/_Project/Features/<Tên>/Docs/README.md`
- Quy tắc code: [Refactor/UnityCodingGuidelines.md](Refactor/UnityCodingGuidelines.md) (chung) + [Refactor/ProjectConventions.md](Refactor/ProjectConventions.md) (riêng dự án); kế hoạch refactor đã làm: [Refactor/RefactorPlan.md](Refactor/RefactorPlan.md)
- Nhật ký thay đổi theo lần push: [RoadMap/](RoadMap/00_README.md)
- Công thức độ khó level: [DifficultyFormula.md](DifficultyFormula.md); sửa / tạo tutorial: [TutorialGuide.md](TutorialGuide.md); kế hoạch tính năng ban đầu: [TaskBreakdown.md](TaskBreakdown.md)

---

## Luật chơi

1. Lưới có các ô số. Kéo chọn một hình chữ nhật để tạo khối.
2. Mỗi khối phải chứa **đúng 1 ô số** và có **số ô bằng giá trị** của ô số đó.
3. Phủ kín lưới (mọi khối đúng) trước khi **đồng hồ** về 0 là thắng.

### Đồng hồ và sao (như bản gốc)

| Tình huống | Kết quả |
| --- | --- |
| Vùng hợp luật | Đặt khối mèo ngay (không hỏi lời giải), chữ khen, mọi ổ khóa giảm 1 |
| Sai số ô, 0 hoặc nhiều ô số, đè khối khác, ô còn khóa, sai hướng | Khung co xám, hủy thao tác (không phạt) |
| Đặt nhầm | Chạm khối để gỡ (mất thời gian – đó là "hình phạt") |
| Lưới kín nhưng khối "?" sai số ô | Mèo sai rung, chạm để gỡ |
| Hết giờ | Popup Thêm giờ: xem ads +30 s / 900 coin +60 s; đóng thì thua (**−1 năng lượng**) |

- Thời gian mỗi level: `8 s + 0.25 s/ô + 5.5–6.5 s/khối`, tối thiểu 45 s (hoặc đặt riêng trong Level Editor). Đồng hồ chỉ chạy khi người chơi thao tác được.
- Sao theo thời gian còn lại: > 60 % = 3, > 20 % = 2, còn giây nào = 1. Sao cộng coin thưởng và vào ví sao (quay Lucky Wheel).
- Booster: **Hint** (chỉ chỗ 1 vùng đúng), **Đóng băng** (dừng đồng hồ 20 s), **Reveal** (đặt mọi khối của 1 số).

### Ô đặc biệt

| Loại | Hiển thị | Luật |
| --- | --- | --- |
| Void | Ô bị bỏ, lưới có hình lạ | Không thuộc khối nào |
| Ô khóa | Ổ khóa mèo + số bước | Mỗi khối đặt làm mọi khóa giảm 1 (gỡ khối thì khóa lại). Về 0 thì lộ số. Còn khóa thì không chọn được |
| Ô ẩn | `?` | Không kiểm tra số ô lúc đặt; số thật chỉ dùng để chấm khi lưới kín. Không bao giờ lộ số |
| Hướng | Ô nền sau số: cao / vuông / dẹt | Khối phải dọc / vuông / ngang |

Dữ liệu level ShikaCat gốc **không có** ô đặc biệt; cơ chế được gắn bằng **Mechanic Injector** ([Features/03](Features/03_CoChe-Mechanic.md)).

---

## Chạy dự án

1. Mở `Assets/_Project/Scenes/SampleScene.unity`, bấm Play. Scene, prefab, config đã có sẵn; không cần chạy builder nào.
   - Lần đầu (save mới, đang ở level 1): vào thẳng **tutorial** 4 bước (Skip để bỏ qua), xong thì vào Level 1.
   - Các lần sau: Main Menu (Home) → **PLAY** để vào level đang chơi. Tab Shop bên trái, tab Daily Challenge bên phải (mở từ level 21).
2. Công cụ (menu **Tools → PawBlock**, chi tiết [Features/13](Features/13_Editor-Builder-Debug.md)):
   - **Level Editor** (sửa / làm level, ▶ chơi thử), **Level Generator**, **Mechanic Injector**, **Level Difficulty**, **Validate Levels**.
   - **Debug Panel** (Ctrl + Shift + D khi Play): đổi level, thắng / hết giờ ngay, cộng coin / sao / năng lượng / booster, sang ngày mới, tốc độ game. Trong bản build: **Cheat Panel** (bấm nhanh 3 lần góc trên trái).
   - **Save Data** → Log Save / Open Save Folder / Delete Save.
3. Test: **Window → General → Test Runner → EditMode**, chạy 4 assembly `PawBlock.*.Tests` (160 test).
4. Build Android: **File → Build Settings → Android** (IL2CPP, ARMv7 + ARM64; texture nén ASTC theo preset).

Dữ liệu người chơi lưu trong 1 file JSON `save.json` ở `Application.persistentDataPath` (Windows: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/PawShika/`), mỗi feature 1 section ([Features/11](Features/11_Save.md)). Save đời cũ (v1, PlayerPrefs) tự chuyển sang v2 lần đầu mở bản mới, giữ `save.v1.bak`.

---

## Nguồn tài nguyên

| Nguồn | Nội dung dùng | Ghi chú |
| --- | --- | --- |
| Paw Block (IL2CPP) | Sprite, âm thanh, Spine, cấu hình booster / shop / daily | Bản gốc ở `Assets/ThirdParty/PawBlock` (không sửa); code chỉ còn khung, logic tự viết lại. Asset đang dùng đã copy vào `Art/` của từng module |
| ShikaCat | 205 level kèm lời giải, ảnh mèo | Bản gốc ở `Assets/ThirdParty/ShikaCat` (gồm `Level_Data/shikacat-levels.json`) |
| Ảnh chơi thử bản gốc | Tham chiếu gameplay và hiệu ứng | Không đưa vào project |

---

## Trạng thái

- Gameplay, meta (năng lượng, Shop, điểm danh, Daily Challenge, Lucky Wheel, hồ sơ, cài đặt), tutorial, game feel: xong (RoadMap 01–18).
- Refactor P0–P9 theo [RefactorPlan](Refactor/RefactorPlan.md) (RoadMap 19–28): module + asmdef, save theo section, bỏ Singleton / Find lúc chạy, UI prefab, asset theo module + Sprite Atlas + ASTC. Số liệu so với trước refactor: [Refactor/Baseline.md](Refactor/Baseline.md).

### Vấn đề đã biết

- `ThirdParty/PawBlock/09_Shaders_Materials/Shaders/` có bản rip `TextMeshPro/Distance Field` (và Mobile, Sprite) của game gốc, trùng tên shader thật của TMP nhưng không vẽ được: material nào lấy shader bằng `Shader.Find` sẽ ra chữ thành ô đen. Font Fredoka đã tạo lại và gán `TMP_SDF-Mobile` theo đường dẫn (`GameFontSetup`).
- Bộ mặt mèo `boltie_01` bị bỏ vì khung hình bị xoay khi đóng gói atlas; còn 10 bộ.
- Quảng cáo có thưởng đang giả lập (luôn thành công sau 0.5 giây); IAP luôn thất bại ("Purchase Failed!") vì chưa có SDK.
- Chưa thử trên máy Android thật sau P8 (nén ASTC).
