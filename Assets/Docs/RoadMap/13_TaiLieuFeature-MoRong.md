# 13 – Tài liệu theo tính năng, hướng dẫn mở rộng

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `9e99a2c` "Docs theo tính năng, popup hết booster mới, nút xám, GameConfig SO" (nhánh `Day-5`) |
| Thời gian push | 2026-10-08 11:11 |
| Cập nhật lần cuối | 2026-10-08 11:11 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 00:27 (07/10) | Docs từng tính năng + cách thêm cơ chế | Thư mục `Docs/Features/`: 13 file theo tính năng, `HowTo_ThemMechanic`, `HowTo_MoRong`; đánh dấu `// [MECHANIC]` ở mọi điểm mở rộng trong code |
| 08/10 | Popup hết booster theo ref, nút xám | Khung chung kiểu REVEAL cho cả 3 booster; nút mua thiếu coin chuyển xám (shader UI_Grayscale + `UIGrayscaleWhenDisabled`) |
| 08/10 | Gom số liệu vào SO | `Assets/_Project/Configs/` + `Resources/GameConfig`; `EconomyConfig`, `LivesEnergyConfig`; bỏ số lặp |

---

## 00:27 – Docs từng tính năng, đánh dấu điểm mở rộng

### Mục tiêu / Vấn đề

Cần tài liệu đọc theo từng tính năng để hiểu dự án, và hướng dẫn thêm cơ chế mới (đang nằm rải ở ~12 file, dễ sót).

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Docs/Features/00_README.md`: mục lục, kiến trúc 1 trang, 5 quy tắc dự án, thứ tự đọc.
- `Docs/Features/01`–`13`: Lưới / kéo thả, Mạng / vòng đời ván, Cơ chế, Level + công cụ, Booster, Năng lượng, Economy / Shop, Điểm danh, Tutorial, UI / Menu / Popup, Save, Âm thanh / phản hồi, Editor / Builder / Debug. Mỗi file: làm gì, file liên quan, luồng, event, sửa / mở rộng ở đâu, lưu ý.
- `Docs/Features/HowTo_ThemMechanic.md`: checklist + ví dụ đầy đủ cơ chế "Mép" qua 11 bước (data, ClueCell, BoardModel, Solver, CellView, booster, popup, Level Editor / Validator, Mechanic Injector, độ khó, test); cơ chế theo ô lưới; gợi ý refactor `IClueMechanic` khi > 6 cơ chế.
- `Docs/Features/HowTo_MoRong.md`: công thức chung + checklist; thêm booster, popup, gói Shop, quà điểm danh, âm thanh, dữ liệu save, tab Menu, hệ thống nghe event, cheat, gắn SDK.
- Chú thích `// [MECHANIC]` (25 chỗ) trong `LevelDefinition`, `ClueCell`, `BoardModel`, `ShikakuSolver`, `CellView`, `PlacedBlockView`, `MechanicIntro`, `BoosterManager`, `BoosterTutorial`, `GridSpawner`, `LevelEditorWindow`, `LevelValidator`, `LevelDifficultyAnalyzer`, `MechanicInjectorSettings`, `MechanicInjector`, `TutorialBuilder`.

**Sửa**

- `SaveSystem.Upgrade`: thêm khởi tạo `daily` khi null (cùng kiểu với `energy`, `settings`), khớp hướng dẫn thêm dữ liệu save.
- `Docs/README.md`, `ProjectStructure.md`: link tới `Features/`.

### Tác dụng

- Đọc hiểu từng tính năng không cần mở code; tìm `[MECHANIC]` ra đủ mọi chỗ phải sửa khi thêm cơ chế.

### Kiểm tra

- Đối chiếu tên class / hàm / menu trong docs với code (grep).
- Chưa biên dịch trong Unity (Editor không mở lúc làm). Thay đổi code chỉ là chú thích + 1 dòng kiểm tra null. File `.meta` của `Docs/Features/` sẽ do Unity tạo khi mở dự án.

## Layout popup hết booster (Buy Booster)

- `PopupBuilder.BuildBuyBooster` dựng lại theo ref "REVEAL": khung chung `FramedPopup` (tab tiêu đề = tên booster, nút X góc trên phải), khung trắng chứa icon booster to + "x1" đè góc dưới phải, mô tả chữ tím 2 dòng; nút xanh viền kem `FramedButton` coin + giá. Bỏ dòng số dư coin dưới popup (ref không có) và field `balanceText` trong `BuyBoosterPopup`. Áp dụng chung cho Hint / Freeze / Reveal (nội dung lấy từ `BoosterData`).
- Menu mới `Tools/PawBlock/Builders/Build Buy Booster Popup`: chỉ dựng lại popup này.
- Kiểm tra: biên dịch không lỗi, đã chạy menu và chụp màn đối chiếu ref.
- Nút mua khi không đủ coin chuyển **xám** (thay vì tối màu): shader `PawBlock/UI/Grayscale` (`09_Shaders_Materials/Shaders/UI_Grayscale.shader`, material `UI_Grayscale.mat` tạo bởi builder), component `UIGrayscaleWhenDisabled` (đổi material mọi Image con + chữ sang preset `Outline Gray` khi `interactable = false`), `PopupBuilder.GrayWhenDisabled` gắn vào nút và đặt `disabledColor` trắng. Dùng lại được cho nút khác.

## Gom số liệu vào ScriptableObject (GameConfig)

- Thư mục mới `Assets/_Project/Configs/`: chứa config dự án đang dùng. `BoosterDatabase`, `ShopDatabase`, `DailyRewardDatabase`, `AudioDatabase`, `DifficultyTheme` được chuyển từ `GameAssets/10_Data_Configs` sang (giữ GUID nên scene / prefab trỏ đúng), bản sao để lại chỗ cũ làm lưu trữ. Các builder (Audio, Booster, Shop, Daily, Difficulty) dùng đường dẫn mới `GameConfigSetup.Folder`.
- SO mới (`Script/Gameplay/Config/`): `EconomyConfig` (coin thắng, giá / số mạng hồi, giá / số năng lượng nạp, thời gian quảng cáo giả lập), `LivesEnergyConfig` (`maxLives`, `maxEnergy`, `refillSeconds`), `GameConfig` ở `Resources/GameConfig.asset` trỏ tới mọi config + `GameplayVisuals`. Thiếu asset thì dùng giá trị mặc định trong code kèm cảnh báo.
- Bỏ `[SerializeField]` số liệu khỏi `WinPopup`, `ReviveLivesPopup`, `RefillEnergyPopup`, `RewardedAds`, `LivesManager`, `EnergyManager` → đọc `GameConfig.Economy` / `GameConfig.LivesEnergy` (lý do: builder dựng lại popup làm mất số chỉnh trong Inspector; giá 900 bị lặp 2 nơi).
- Bỏ số lặp: `shopTab` (CoinHUD, ScreenFlow) → `MainMenuNavigator.ShopTab`; `minOrthoSize` của LevelManager → `PlayZone.DefaultMinOrthoSize`.
- Menu mới `Tools/PawBlock/Builders/Setup Game Config` (chạy lại được, không ghi đè số đã chỉnh).
- Kiểm tra: biên dịch không lỗi; chạy Setup, kiểm tra scene trỏ vào `Configs/`, giá trị đọc đúng (10 / 900 / 900 / 3 / 5 / 1800, khớp số cũ trong scene); vào Play không lỗi.
