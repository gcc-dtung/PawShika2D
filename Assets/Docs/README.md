# Paw Block – Bản làm lại với cơ chế mạng

Game giải đố Shikaku dựa trên [Paw Block Puzzle](https://play.google.com/store/apps/details?id=com.phoenix.pawblock.puzzle). Thay đổi chính so với bản gốc: **bỏ đồng hồ đếm ngược, thay bằng 3 mạng mỗi level**.

- Unity **2022.3.62f3**, URP 2D, TextMeshPro, uGUI, DOTween (`Assets/Plugins/Demigiant`)
- Scene chính: `Assets/Scenes/SampleScene.unity`
- Kế hoạch chi tiết và các quyết định: [TaskBreakdown.md](TaskBreakdown.md)
- **Tài liệu theo từng tính năng + hướng dẫn mở rộng (thêm cơ chế, booster, popup…)**: [Features/](Features/00_README.md)
- Cấu trúc thư mục, script, luồng dữ liệu: [ProjectStructure.md](ProjectStructure.md)
- Nhật ký thay đổi theo thứ tự thời gian: [RoadMap/](RoadMap/00_README.md)
- Công thức độ khó level: [DifficultyFormula.md](DifficultyFormula.md)
- Sửa / tạo tutorial: [TutorialGuide.md](TutorialGuide.md)

---

## Luật chơi

1. Lưới có các ô số. Kéo chọn một hình chữ nhật để tạo khối.
2. Mỗi khối phải chứa **đúng 1 ô số** và có **số ô bằng giá trị** của ô số đó.
3. Phủ kín lưới là thắng.

### Cơ chế mạng

| Tình huống | Kết quả | Mạng |
| --- | --- | --- |
| Khối hợp lệ và lưới vẫn còn lời giải | Đặt khối mèo, hiện chữ khen, mọi ổ khóa giảm 1 | Giữ nguyên |
| Khối đúng số ô nhưng đặt xong thì lưới **không còn lời giải** | Mèo chuyển xám, rung rồi co lại biến mất, camera rung, điện thoại rung | **−1** |
| Sai số ô, 0 hoặc nhiều ô số, đè khối khác, chứa ô còn khóa, sai hướng mũi tên | Chỉ hủy thao tác | Giữ nguyên |
| Hết mạng | Khóa lưới, popup Hồi mạng: xem ads +1, trả 900 coin +3, đóng thì thua (popup Retry, **−1 năng lượng**) | 0 |

- Mọi level đều có **3 mạng**. Không có sao, không có Undo.
- Không so với lời giải lưu sẵn vì 17/205 level có nhiều hơn một lời giải. `ShikakuSolver` kiểm tra xem lưới còn giải được hay không.
- Khối đã đặt không xóa được: mọi khối đặt được đều nằm trong ít nhất một lời giải.

### Ô đặc biệt

| Loại | Hiển thị | Luật |
| --- | --- | --- |
| Ô khóa | Ổ khóa mèo + số bước | Mỗi khối đặt đúng làm mọi khóa giảm 1. Về 0 thì lộ số. Còn khóa thì không chọn được. |
| Ô ẩn | `?` | Không kiểm tra số ô; số thật chỉ dùng để chấm khi lưới kín. Không bao giờ lộ số (khối mèo đặt lên cũng hiện `?`). |
| Mũi tên ngang | Mũi tên mờ | Khối phải rộng hơn cao |
| Mũi tên dọc | Mũi tên mờ, xoay 90° | Khối phải cao hơn rộng |

Dữ liệu level ShikaCat gốc **không có** ô đặc biệt nào; cơ chế được gắn bằng **Mechanic Injector** (131/205 level, xem bên dưới).

---

## Chạy dự án

1. Mở `Assets/Scenes/SampleScene.unity`.
2. Lần đầu (hoặc khi đổi asset), chạy các menu Editor:
   - **Tools → PawBlock → Builders → Import ShikaCat Levels**: tạo 205 `LevelDefinition` trong `Assets/_Project/Levels/ShikaCat/` và `LevelDatabase.asset`.
   - **Tools → PawBlock → Builders → Setup Gameplay Visuals**: đặt 9-slice cho khối, cắt atlas mặt mèo, tạo `Assets/_Project/Resources/GameplayVisuals.asset`.
   - **Tools → PawBlock → Builders → Build Popups**: dựng lại popup Win / Hồi mạng / Thua / Cài đặt / Mua booster trong `Canvas_HUD` (scene đã có sẵn, chỉ chạy khi sửa builder).
   - **Tools → PawBlock → Builders → Build Boosters**: dựng lại 3 nút booster, banner Reveal, khung băng Khiên, gắn `BoosterManager`.
     Booster khóa tới khi qua **tutorial mở khóa**: lần đầu vào level ≥ `levelUnlock` (Hint 5, Khiên 7, Reveal 9 trong `BoosterDatabase.asset`) màn hình tối, chỉ nút đó sáng + bàn tay; bấm → mở khóa, lần dùng đầu miễn phí. Khiên có thêm bước "kéo sai thử": bàn tay chỉ 1 cách kéo sai trên lưới, người chơi kéo sai → khiên chặn, không mất mạng. Debug Panel → Booster: Mở khóa / Khóa lại tất cả.
   - **Tools → PawBlock → Builders → Build Shop**: tab Shop (Starter Pack, Remove Ads, No Ads Bundle, Special Bundle, Coin); dữ liệu gói trong `ShopDatabase.asset`, mua bằng VND qua `IAPService` (chưa SDK: "Purchase Failed!").
   - **Tools → PawBlock → Builders → Build Difficulty**: level Hard / Very Hard – `DifficultyTheme`, nút PLAY có tab mức khó, khung HUD, hiệu ứng vào màn. Level nào Hard / Very Hard: đánh dấu trong Level Difficulty (hoặc Level Editor).
   - **Tools → PawBlock → Debug Panel** (Ctrl + Shift + D, dùng khi Play): đổi level, thắng / thua ngay, cộng coin / mạng / năng lượng / booster, sang ngày điểm danh, tốc độ game.
   - **Tools → PawBlock → Builders → Build Win Screen**: màn thắng (WELL DONE, sao, huy hiệu mèo, confetti, coin bay) + material lóe sáng cho khối mèo. Confetti cần package **UI Particle** (`com.coffee.ui-particle`, đã có trong `Packages/manifest.json`, cài từ git `mob-sakai/ParticleEffectForUGUI` tag `v4.14.1`).
   - **Tools → PawBlock → Builders → Build Daily Reward**: điểm danh hằng ngày – `DailyRewardDatabase` (quà 7 ngày), `DailyRewardManager`, popup, nút Daily dưới Starter.
   - **Tools → PawBlock → Builders → Build Settings**: nút bánh răng góc trên phải HUD (thay nút chơi lại + tim năng lượng) và popup cài đặt Nhạc / Âm / Rung.
   - **Tools → PawBlock → Builders → Build Main Menu**: dựng lại thanh tab Shop / Home / Progress, thanh trên (năng lượng + coin), nút PLAY, 2 panel tạm Shop / Progress trong `Canvas_Menu` (giữ nguyên phần còn lại của `HomePanel`), gắn `ScreenFlow`. Chạy sau Build Popups.
   - **Tools → PawBlock → Builders → Setup Audio**: tạo / bổ sung `AudioDatabase`, object `AudioManager`, âm bấm nút trên các canvas (scene đã có sẵn).
   - **Tools → PawBlock → Builders → Build Tutorial**: dựng lại giao diện tutorial (TutorialPanel, bàn tay), gắn `TutorialController` (scene đã có sẵn). Xem lại tutorial: Debug Panel → Chơi lại tutorial. Sửa / thêm bước, tạo tutorial mới: [TutorialGuide.md](TutorialGuide.md).
3. Sửa / làm level: **Tools → PawBlock → Level Editor** (kéo hình chữ nhật để xếp khối, số tự = diện tích; Kiểm tra; ▶ Chơi thử vào thẳng level). Level mới nằm ở `Assets/_Project/Levels/Custom/`.
   - Thứ tự level: **Tools → PawBlock → Builders → Reorder Levels** xếp level ShikaCat theo độ khó lý thuyết (bỏ level 001, 004 và mọi level chỉ có 1 khối), mỗi khối 10 level có đỉnh ở ...9 (Hard; ...29/59/89 Very Hard) và level nghỉ ở ...0, rồi chạy lại Mechanic Injector (giới thiệu Void 5, Hướng 8, Khóa 12, "?" 16).
   - Level 1 là `Levels/Custom/Level_Custom_Level01_4x4` (lưới 4x4 như bản gốc, thay `ShikaCat/Level_001` trùng bước 1 tutorial). Chạy lại Import ShikaCat Levels sẽ đưa Level 1 về bản ShikaCat (bản Custom bị đẩy xuống cuối), kéo lại bằng Level Editor.
   - Lần đầu gặp cơ chế Hướng / Khóa / "?" hiện popup giới thiệu (ảnh `popup_tutorial_4/2/3`), lưu đã xem; Debug Panel → Xem lại popup cơ chế.
   - Gắn cơ chế cho level có sẵn: **Tools → PawBlock → Mechanic Injector** (lịch trong `Levels/MechanicInjectorSettings.asset`; Xem trước → Áp dụng; Khôi phục bản gốc để bỏ). Level có cơ chế nằm ở `Assets/_Project/Levels/Mechanic/`, bản gốc ShikaCat không bị sửa. Chạy lại Import ShikaCat Levels sẽ đưa database về bản gốc, cần Áp dụng lại.
   - Độ khó lý thuyết + đánh dấu Hard / Very Hard: **Tools → PawBlock → Level Difficulty** (tick cột Hard / V.Hard, hoặc Áp dụng gợi ý; Xuất CSV / Nạp dữ liệu thực tế để so). Công thức: [DifficultyFormula.md](DifficultyFormula.md).
4. Bấm Play: lần đầu (save mới, đang ở level 1) vào thẳng **tutorial** 4 bước (Skip để bỏ qua), xong thì vào Level 1; các lần sau vào Main Menu (Home) → bấm **PLAY** để vào level đang chơi. Kéo chuột trên lưới để tạo khối. Thua → HOME để về Menu.

Dữ liệu người chơi (level đang chơi, coin, lượt booster, năng lượng, cài đặt Nhạc / Âm / Rung) lưu trong 1 file JSON `save.json` ở `Application.persistentDataPath` (Windows: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/TestProject/`). Xem / mở thư mục / xóa: **Tools → PawBlock → Save Data** (Log Save, Open Save Folder, Delete Save). Bản trước lưu PlayerPrefs: lần đầu chạy bản mới tự chuyển sang file rồi xóa key cũ. Thử năng lượng: chuột phải component `EnergyManager` → Debug (Set 0, Skip 1 refill...).

---

## Nguồn tài nguyên

| Nguồn | Nội dung dùng | Ghi chú |
| --- | --- | --- |
| `E:\PawBlock` (Paw Block, IL2CPP) | Sprite, âm thanh, Spine, cấu hình booster/shop | Code chỉ còn khung, không có thân hàm; logic tự viết lại |
| `E:\Data` (ShikaCat) | 205 level kèm lời giải | `shikacat-levels.json`, đã chép vào `Assets/_Project/ShikaCat_GameAssets/` |
| `E:\game_view` | 13 ảnh chơi thử Level 26 | Tham chiếu gameplay và hiệu ứng |

---

## Trạng thái

| Giai đoạn | Nội dung | Trạng thái |
| --- | --- | --- |
| 1 | Mạng trong màn | Xong |
| 2 | Gameplay một level đầy đủ | Xong (còn 3 việc tồn đọng) |
| 3 | Popup Thắng / Hồi mạng / Thua, HUD | Gần xong (còn banner độ khó, làm đẹp HUD) |
| 4 | Booster Hint, Khiên, Reveal | Xong |
| 5 | Năng lượng ngoài sảnh (giữ, đổi icon) | Xong (hiện ở HUD trong màn, chờ màn Home) |
| 6 | Meta, Shop, Ads, Firebase | Đang làm: Main Menu 3 tab, màn Home (PLAY, năng lượng, coin), Menu ↔ màn chơi |

### Vấn đề đã biết

- `GameAssets/09_Shaders_Materials/Shaders/` có bản rip `TextMeshPro/Distance Field` (và Mobile, Sprite) từ game gốc, trùng tên với shader thật của TMP nhưng không vẽ được: material nào lấy shader bằng `Shader.Find` sẽ ra chữ thành ô đen (lý do Fredoka từng "hỏng"). Font Fredoka đã tạo lại và gán `TMP_SDF-Mobile` theo đường dẫn (`GameFontSetup`).
- Sprite mũi tên đang là ảnh debug (`arrow-right@2x.png`).
- Bộ mặt mèo `boltie_01` bị bỏ vì khung hình bị xoay khi đóng gói atlas; còn 10 bộ.
- Quảng cáo có thưởng đang giả lập (luôn thành công sau 0.5 giây); chưa có Shop nên nút trả coin bị mờ khi thiếu coin.
