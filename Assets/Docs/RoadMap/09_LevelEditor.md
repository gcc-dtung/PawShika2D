# 09 – Level Editor, Audio, Save JSON, Setting, Daily, Win, Hard level, Shop

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-04 11:30 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 17:32 (03/10) | Level Editor | Cửa sổ Tools → PawBlock → Level Editor: sửa / xếp level trực quan, tạo mới, nhân bản, đổi thứ tự trong database, kiểm tra (có lời giải, không cần đoán, duy nhất), giải, chơi thử |
| 18:10 (03/10) | Audio Manager | Nhạc nền + âm hiệu ứng theo sự kiện game, âm nút / popup, tiếng mèo; cài đặt Nhạc / Âm / Rung lưu PlayerPrefs |
| 19:40 (03/10) | Save JSON | Dữ liệu người chơi gom vào 1 file `save.json` (SaveData + SaveSystem), ghi an toàn có bản dự phòng, tự chuyển PlayerPrefs cũ; bỏ hẳn PlayerPrefs |
| 01:06 (04/10) | Sửa lỗi ô "?" | Chạm 1 ô vào ô "?" (số thật 6) được tính là đúng và lộ số 6; giờ khối sai số ô của ô "?" bị tính là nước đi sai |
| 01:30 (04/10) | Punch nút | Mọi Button tương tác được: ấn lún nhỏ, thả tay nảy lên rồi về (ButtonPunch, tự gắn theo canvas) |
| 01:45 (04/10) | Setting trong ván | Bỏ nút Replay và tim năng lượng trên HUD; nút bánh răng mở popup Setting (Music / Sound / Vibrate, HOME) dùng chung với nút Settings ở Main Menu |
| 02:35 (04/10) | Popup Setting như ref | Thân cao + tab tiêu đề rộng (sprite `popup_1_body` tách từ popup_1), Home vàng trong thân, chữ phiên bản; ở Menu ẩn Home và thu ngắn thân |
| 02:45 (04/10) | Popup Refill Lives như ref | Bấm tim năng lượng: "REFILL LIVES?" (tim có số, chữ mời, đồng hồ, Refill 900 / Free live), cùng khung với Setting |
| 02:55 (04/10) | Refill thiếu coin → Shop, nền popup tối hơn | Nút Refill luôn sáng (trừ khi đầy), thiếu coin thì sang tab Shop; Dim mọi popup 0.75 → 0.88 |
| 03:20 (04/10) | Tab Menu mượt hơn + vuốt chuyển panel | Bỏ punch trên nút tab (nguyên nhân giật), icon tab nảy nhẹ; vuốt trái / phải trên panel để chuyển tab |
| 04:00 (04/10) | Starter / No Ads lắc mời bấm, nhấn nút tối đi | Theo video Menu bản gốc: icon nhấc + lắc định kỳ thay cho nhún; nhấn nút thì tối đi thay cho punch (đổi `ButtonPunch` → `ButtonPress`) |
| 04:20 (04/10) | Nhấn nút có cảm giác lún | Khung viền đứng yên, mặt nút + chữ lún vào và tối đi (PLAY, Refill, Free live); nút 1 ảnh lún cả nút; thả thì nảy nhẹ về |
| 05:10 (04/10) | Điểm danh hằng ngày | Nút Daily (quyển lịch, khung hồng tím như Starter, lắc + chấm đỏ) mở popup 7 ngày; lỡ ngày giữ tiến độ, ô lỡ xem quảng cáo để bù |
| 06:00 (04/10) | Game feel + màn thắng + confetti | Lưới lóe sáng khi thắng; màn WELL DONE (sao, huy hiệu mèo tia sáng xoay, confetti UI), coin bay vào ô coin khi nhận |
| 06:40 (04/10) | Cài UIParticle, confetti bằng ParticleSystem | Package `com.coffee.ui-particle` 4.14.1; confetti chuyển từ Image UI cập nhật bằng C# sang ParticleSystem (native) vẽ qua UIParticle, 1 mesh / 1 draw call |
| 07:10 (04/10) | Chữ WELL DONE đi vào từng chữ | Bỏ sao; chữ to ngay trên mèo, mỗi ký tự to → về cỡ chuẩn, hiện dần, lần lượt |
| 07:50 (04/10) | Debug Panel | Cửa sổ Editor: đổi level, thắng / thua, + coin / mạng / năng lượng / booster, điểm danh, tốc độ game |
| 08:05 (04/10) | Popup điểm danh 3 + 3 + 1 | Bố cục như "Đăng nhập 7 ngày" bản gốc, giữ art hiện tại |
| 08:30 (04/10) | QUIT LEVEL | Đang chơi bấm Home → cảnh báo; Quit mất 1 năng lượng rồi về Home, X chơi tiếp |
| 09:20 (04/10) | Level Hard / Very Hard | Nút PLAY có tab mức khó, khung HUD, hiệu ứng vào màn (băng HARD / EXTREME LEVEL + mèo đầu lâu) |
| 09:45 (04/10) | Sửa: PLAY level khó không bấm được | Image gốc của nút (vùng nhận chạm) bị tắt khi ẩn khung thường |
| 10:10 (04/10) | Sửa: PLAY khó nhấn lộ nền, băng chữ hụt 2 mép | Nền tối btn_hard_2 / btn_very_hard_2 sau mặt nút; băng chữ kéo dài quá 2 mép màn |
| 10:30 (04/10) | Popup NOTICE + IAPService | Mua bằng VND chưa có SDK → "Purchase Failed!" như bản gốc |
| 11:30 (04/10) | Tab Shop | Như video bản gốc: Starter Pack, Remove Ads, No Ads Bundle, 5 Special Bundle, lưới Coin; cuộn dọc, vuốt ngang vẫn chuyển tab; mua VND qua IAPService |

---

## 17:32 – Level Editor

### Yêu cầu

Làm tool để chỉnh sửa, xếp một level.

### Cửa sổ `Tools → PawBlock → Level Editor`

| Phần | Chức năng |
| --- | --- |
| Chọn level | Database + danh sách "n. tên", ◀ ▶; ô Level kéo thả asset bất kỳ; chọn LevelDefinition trong Project thì cửa sổ đổi theo |
| Asset | **Tạo mới** (5x5 trống, `Levels/Custom/Level_Custom_<thời gian>.asset`, chèn sau level đang chọn), **Nhân bản** (bản sao sang Custom), **▲ Lên / ▼ Xuống** (thứ tự trong database), **Bỏ khỏi DB** / **Thêm vào DB** (không xóa asset) |
| Kích thước | Rộng x cao (1–12), Áp dụng (bỏ phần ra ngoài lưới); số mạng (`mistakeLimit`) |
| Công cụ | **Khối**: kéo hình chữ nhật → 1 khối + số = diện tích ở ô bắt đầu kéo, xóa khối / số / Void cũ trong vùng. **Số**: bấm chọn số, ô trống → thêm số (trong khối chưa có số thì số = diện tích khối). **Void**: bấm / kéo tô ô bỏ trống. **Xóa**: xóa số + khối chứa ô. Chuột phải = Xóa |
| Lưới | y = 0 ở dưới như trong game; khối tô màu theo id; số, "?n" (ẩn), →/↑/□ (hướng), Ln (khóa n bước); viền vàng = số đang chọn |
| Thanh trạng thái | Tổng số / số ô chơi được (vàng khi lệch), số lượng số, số ô đã phủ khối |
| Số đang chọn | Giá trị, Hướng, Khóa (bước), Ẩn "?", Màu; Xóa số |
| Kiểm tra | `LevelValidator.Validate` (dữ liệu, có lời giải, không cần đoán với ô ? / khóa) + **lời giải duy nhất** (mỗi số thử mọi cách đặt khác lời giải; còn giải tiếp được = có lời giải thứ 2, báo cả 2 cách) |
| Giải | Lưu lời giải của solver vào `solution` (hiện thành khối) |
| Chơi thử | Lưu, đặt `PawBlock_CurrentLevel`, vào Play mode; `LevelEditorPlayTest` (InitializeOnLoad + SessionState) bỏ qua Menu, gọi `ScreenFlow.Play` |
| Lưu / Undo | Mọi thao tác có Undo (Ctrl+Z); nút Lưu ghi asset |

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Editor/Level/LevelEditorWindow.cs` | Cửa sổ Level Editor + `LevelEditorPlayTest`; `AppendCustomLevels` |
| Sửa | `Editor/Level/ShikaCatLevelImporter.cs` | Import lại thì thêm level trong `Levels/Custom` vào cuối database (trước đây sẽ mất) |

### Kiểm tra

| Bước | Kết quả |
| --- | --- |
| Xếp 4 khối trên lưới 4x3 | 4 số đúng diện tích, 4 khối; Kiểm tra báo nhiều lời giải (lưới xếp theo hàng cũng được) – đúng |
| Kéo khối chồng lên 2 khối cũ | 2 khối cũ + số của chúng bị bỏ |
| Kiểm tra level 206 (test mọi cơ chế) | "Có đúng 1 lời giải, giải được không cần đoán" (7 ms) |
| Kiểm tra level 31 (ShikaCat) | Có nhiều lời giải: số 8 tại (2, 5) đặt được 4x2 từ (0, 4) hoặc từ (1, 4) |
| Tạo mới → xếp 6 khối → Chơi thử | Vào Play mode, bỏ qua Menu, vào thẳng level 2 = level vừa tạo, state Playing |

Sau khi thử đã bỏ level thử khỏi database, xóa `Levels/Custom` và trả `PawBlock_CurrentLevel` = 17.

### Ghi chú

- Sửa level trong `Levels/ShikaCat` sẽ bị ghi đè khi chạy lại Import ShikaCat Levels (cửa sổ có cảnh báo); muốn giữ thì Nhân bản sang Custom.
- Import lại: level Custom được thêm vào cuối database, vị trí đã xếp bị mất.
- Đổi thứ tự trong database làm lệch level đang lưu ở `PawBlock_CurrentLevel` của máy đang chơi.
- Game vẫn nhận mọi cách giải (chỉ tính sai khi lưới hết lời giải), nên "nhiều lời giải" là cảnh báo về độ chặt của level, không phải lỗi.

---

## 18:10 – Audio Manager

### Yêu cầu

Làm audio manager để làm sound cho game.

### Thiết kế

- `AudioManager.Play(SoundId.X)` ở bất cứ đâu; clip, âm lượng, cao độ, cooldown chỉnh trong `AudioDatabase` (Inspector), không sửa code.
- Âm gắn vào sự kiện có sẵn qua `GameAudioEvents` (không rải code âm thanh vào từng script gameplay); popup tự kêu trong `PopupView`; nút UI tự kêu nhờ `UIButtonSounds` trên canvas.
- Cài đặt Nhạc / Âm thanh / Rung lưu PlayerPrefs, có sự kiện `SettingsChanged` để popup cài đặt dùng sau.

### Âm đã gán (Setup Audio)

| SoundId | Clip | Khi nào |
| --- | --- | --- |
| Click | `01_Audio/UI_SFX/click` | Bấm mọi nút UI |
| PopupOpen / PopupClose | ShikaCat `SC_SFX_UI_Popup_Open_01` / `_Close_01` | Mở / đóng popup |
| Coin | `earn_coin` | Coin tăng |
| LevelStart | `load_level` | Lưới xuất hiện (không kêu khi đang ở Menu) |
| BlockPlace | `block_true` (cao độ 0.95–1.08) | Đặt đúng khối |
| Mistake | ShikaCat `SC_Gameplay_Mistake_01` | Đặt sai (trừ mạng) |
| Unlock | `unlock_obs2` | Ô khóa mở |
| Win / Lose / OutOfLives | `win` / `failed` / `warning` | Thắng / thua / hết mạng |
| Booster | `booster2` | Dùng booster |
| ShieldOn / ShieldBlock | `freeze_appear` / `freeze_start` | Khiên (băng) bật / chặn 1 lần sai |
| CatHappy | Happy, Joyful, Cute, Cool, Blink | Kèm đặt đúng, 35 % |
| CatSad | Angry1, Angry2, Sad, Dizzy, Scare | Kèm đặt sai, 80 % |
| Nhạc nền | `BGM/BGM` (Streaming), âm lượng 0.45 | Lặp từ lúc mở game |

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Audio/SoundId.cs`, `AudioDatabase.cs`, `AudioManager.cs`, `GameAudioEvents.cs` | Xem `ProjectStructure.md` |
| Thêm | `UI/Common/UIButtonSounds.cs`, `NoClickSound.cs` | Âm Click cho mọi Button trong canvas |
| Thêm | `Editor/Audio/AudioSetup.cs` | Tools → PawBlock → Setup Audio |
| Thêm | `10_Data_Configs/AudioDatabase.asset` | 16 âm + nhạc nền |
| Sửa | `UI/Popups/PopupView.cs` | Âm mở / đóng |
| Sửa | `Gameplay/Feedback/GameplayFeedback.cs` | Rung qua `AudioManager.Vibrate` (tôn trọng cài đặt Rung) |
| Sửa | `01_Audio/BGM/BGM.ogg.meta` | Load Type = Streaming |
| Sửa | `Scenes/SampleScene.unity` | Object `AudioManager`; `UIButtonSounds` trên Canvas_Menu / HUD / Popup |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Mở game | Nhạc nền BGM phát, fade lên 0.45 |
| Bấm PLAY (gọi onClick thật) | Click + LevelStart (`load_level`) |
| Mở popup nạp năng lượng | PopupOpen |
| `MusicOn` false → true | Nhạc fade xuống / lên, `SettingsChanged` x2 |
| Console | Không lỗi |

### Ghi chú

- Chưa có popup cài đặt; nút bánh răng ở Home chưa gắn. Khi làm: đọc / ghi `AudioManager.MusicOn`, `SoundOn`, `VibrateOn`.
- Tiếng mèo có thể hơi nhiều khi đặt liên tục: chỉnh `catHappyChance` / `catSadChance` trên `GameAudioEvents` hoặc cooldown trong `AudioDatabase`.

---

## 19:40 – Lưu dữ liệu người chơi bằng JSON

### Yêu cầu

Chuyển dữ liệu người chơi (đang rải ~11 key PlayerPrefs) sang JSON.

### Thiết kế

- `SaveData`: 1 object chứa hết (level, coin, booster, năng lượng, cài đặt), có `version` để nâng cấp cấu trúc.
- `SaveSystem` (static): đọc lười, `MarkDirty` gom thay đổi, ghi an toàn (`save.json.tmp` → giữ `save.json.bak` → đổi tên), đọc dự phòng khi file chính hỏng.
- `SaveRunner`: object ẩn ghi sau 0.5 s và khi app vào nền / thoát.
- Lần đầu chạy bản mới: chuyển key PlayerPrefs cũ sang file, ghi thành công mới xóa key cũ.
- Booster lưu theo tên loại (không theo số enum) để thêm / đổi thứ tự `BoosterType` không lệch dữ liệu.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Save/SaveData.cs`, `SaveSystem.cs`, `SaveRunner.cs` | Xem `ProjectStructure.md` |
| Thêm | `Editor/Save/SaveDataMenu.cs` | Tools → PawBlock → Save Data: Log Save, Open Save Folder, Delete Save |
| Sửa | `Gameplay/Economy/CoinWallet.cs` | `SaveData.coins` |
| Sửa | `Gameplay/Booster/BoosterInventory.cs` | `SaveData.boosters`; chưa có (-1) thì tặng `valueStart` |
| Sửa | `Gameplay/Energy/EnergyManager.cs` | `SaveData.energy` (-1 = lần đầu, đầy) |
| Sửa | `Gameplay/Level/LevelManager.cs` | `SaveData.currentLevel`, chỉ ghi khi đổi level |
| Sửa | `Gameplay/Audio/AudioManager.cs` | `SaveData.settings` |
| Sửa | `Editor/Level/LevelEditorWindow.cs` | Chơi thử ghi level vào save (ngoài Play mode ghi file ngay) |

### Kiểm tra

| Bước | Kết quả |
| --- | --- |
| Chuyển dữ liệu (máy đang test) | PlayerPrefs level 17, coin 120, booster 0/2/0, năng lượng 4 (mốc 1791017412), nhạc bật → `save.json` đúng y hệt; key cũ đã xóa |
| Vào Play | Home "Level 18", coin 120, năng lượng 5 (đã qua 30 phút nên hồi 1), Khiên 2 |
| `CoinWallet.Add(5)` | Ngay sau: file chưa đổi (đang gom); sau 0.8 s: file 125, có `.bak`, không còn `.tmp` |
| Ghi hỏng `save.json` | Cảnh báo, đọc `save.json.bak`, ghi lại file chính |
| Console | Không lỗi |

Sau khi thử đã trả coin về 120.

### Ghi chú

- File: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/TestProject/save.json` (Editor Windows). Reset game: Tools → PawBlock → Save Data → Delete Save (thay cho Clear All PlayerPrefs).
- Chưa chống sửa file (JSON chữ thường). Khi cần: thêm checksum HMAC hoặc mã hóa, chống thật sự phải kiểm ở server.
- `.bak` là bản trước lần ghi cuối: khôi phục từ `.bak` có thể lùi 1 thay đổi.

---

## 01:06 (04/10) – Sửa lỗi ô "?" nhận khối sai số ô

### Lỗi

Level 206: chạm 1 lần vào ô "?" (số thật 6) -> đặt được khối 1 ô, ô lộ số 6, không trừ mạng. Lưới còn lỗ trống không bao giờ lấp được.

### Nguyên nhân

`BoardModel.Check` bỏ qua kiểm tra số ô với ô "?" (người chơi không biết số) và để solver quyết định. Nhưng `ShikakuSolver.BuildState` chỉ đánh dấu clue nằm trong khối đã đặt là "đã giải", không so diện tích khối với số của clue. Khi không bật kiểm tra phủ kín, các clue còn lại vẫn xếp vừa -> solver báo "còn lời giải" -> khối được đặt, `RevealClue` lộ số.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Solver/ShikakuSolver.cs` | `BuildState`: khối đã đặt chứa clue biết số mà diện tích khác số -> không có lời giải. Khối 1 ô trên ô "?" số 6 giờ là nước đi sai (mèo rung, trừ mạng), số vẫn ẩn |

### Kiểm tra

| Bước | Kết quả |
| --- | --- |
| Biên dịch Unity | Không lỗi |

---

## 01:30 (04/10) – Hiệu ứng punch khi bấm nút

### Yêu cầu

Hiệu ứng punch khi ấn các button có thể tương tác trong game.

### Thiết kế

- Ấn xuống: lún còn 0.9 (0.08 s). Thả tay trên nút: nảy lên 1.08 (0.1 s) rồi về kích thước cũ (0.18 s, OutBack). Thả tay ngoài nút (không tính là bấm): về luôn, không nảy.
- Chỉ chạy khi `Selectable.IsInteractable()` (tính cả CanvasGroup).
- Gắn tự động giống âm click: `UIButtonPunches` trên canvas thêm `ButtonPunch` cho mọi Button lúc Awake. Muốn tắt / chỉnh riêng 1 nút: gắn sẵn `ButtonPunch` (bỏ tick = tắt).
- Nút đang có tween scale khác (PLAY thở, Booster nảy khi dùng): tween đó tạm dừng trong lúc punch, xong thì chạy tiếp từ đúng chỗ -> không giật. Booster vừa bấm thì bỏ cú nảy "đã dùng" để không chồng 2 hiệu ứng.
- Tween chạy thời gian thực (`SetUpdate(true)`), nút tắt giữa chừng thì trả về kích thước gốc.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Common/ButtonPunch.cs` | Hiệu ứng lún / nảy cho 1 nút |
| Thêm | `UI/Common/UIButtonPunches.cs` | Trên canvas: gắn `ButtonPunch` cho mọi Button bên trong |
| Sửa | `UI/HUD/BoosterButton.cs` | `OnUsed`: nút đang punch thì không nảy thêm |
| Sửa | `Scenes/SampleScene.unity` | `UIButtonPunches` trên Canvas_Menu, Canvas_HUD, Canvas_Popup (30 nút) |

### Kiểm tra (Play mode, tua tween)

| Bước | Kết quả |
| --- | --- |
| Ấn nút PLAY | Lún 0.900; tween thở bị tạm dừng |
| Thả trên nút | 0.987 → 1.079 → 0.992 → 1.000; tween thở chạy lại |
| Nút Settings `interactable = false` | Không có hiệu ứng |
| Thả ngoài nút | Về 1.000, không vượt 1.0 |
| Console | Không lỗi |

---

## 01:45 (04/10) – Popup Setting thay nút Replay và tim năng lượng

### Yêu cầu

Trong gameplay bỏ tim năng lượng (heal), thay bằng nút Setting; bỏ nút Replay. Setting có 3 cài đặt: tắt nhạc, tắt SFX, tắt rung (theo ảnh ref).

### Thiết kế

- HUD: góc trên phải là nút bánh răng (cùng kiểu nút Settings ở Main Menu: `khung_setting_3` + `btn_setting`); góc trái để trống.
- Popup như ref: tab "SETTING", nút X, khung trắng (`khung_popup_1`, đặt viền 9-slice 60) chứa 3 nút tròn: bật = `btn_setting2` (xanh), tắt = `btn_setting1` (xám) + gạch đỏ `off`; chữ Music / Sound / Vibrate bên dưới. Nút HOME dưới popup như các popup khác.
- Cài đặt dùng sẵn `AudioManager.MusicOn / SoundOn / VibrateOn` (lưu `save.json`). Vừa bật rung thì rung thử 1 cái.
- Mở trong ván: hủy lượt kéo, `Pause`; X → `Resume`; HOME → về Main Menu. Trong ván chỉ mở được khi đang chơi (không chồng lên popup Win / Thua...).
- Nút Settings ở Main Menu (trước chưa làm gì) giờ mở cùng popup, ẩn nút HOME.
- Bỏ hẳn `ConfirmReplayPopup` (không còn nút Replay).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Popups/SettingsPopup.cs` | Popup cài đặt, tạm dừng / chơi tiếp, HOME |
| Thêm | `UI/Popups/SettingToggleView.cs` | 1 nút bật / tắt Music / Sound / Vibrate |
| Thêm | `UI/Common/SettingsButton.cs` | Nút bánh răng → `GameFlowUI.OpenSettings` |
| Thêm | `Editor/UI/SettingsBuilder.cs` | Tools → PawBlock → Build Settings: nút trên HUD + popup, không dựng lại popup khác |
| Xóa | `UI/Popups/ConfirmReplayPopup.cs` | Không còn nút Replay |
| Sửa | `UI/Popups/GameFlowUI.cs` | Bỏ `RequestReplay` / `confirmReplayPopup`; thêm `settingsPopup`, `OpenSettings` |
| Sửa | `UI/HUD/LevelHUD.cs` | Chỉ còn chữ `LEVEL n` |
| Sửa | `Editor/UI/PopupBuilder.cs` | Dựng `SettingsPopup` thay `ConfirmReplayPopup`; `EnsureSliced` |
| Sửa | `Editor/UI/EnergyBuilder.cs` | Không dựng tim năng lượng trên HUD nữa, chỉ gắn `EnergyManager` |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Nút Settings có `SettingsButton` |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Settings |
| Sửa | `khung_popup_1.png.meta` | Viền 9-slice 60 |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Vào ván, bấm bánh răng | Popup mở, ván `Paused` |
| Tắt Music + Vibrate | Nút xám + gạch đỏ (ảnh chụp), Sound vẫn xanh; `MusicOn = false`, `VibrateOn = false` |
| X | Popup đóng, ván `Playing` |
| HOME | Về Main Menu, popup đóng |
| Mở từ Main Menu | Nút HOME ẩn; X thì vẫn ở Menu |
| Cài đặt | Đã trả về bật cả 3 sau khi thử |
| Console | Không lỗi |

### Ghi chú

- Setting → HOME giữa ván rồi PLAY lại không tốn năng lượng (giống nút Replay cũ). Ghi ở Câu hỏi mở trong `TaskBreakdown.md`.

---

## 02:35 (04/10) – Popup Setting dựng lại theo ảnh ref

### Yêu cầu

Popup Setting giống ảnh ref (bản cũ dùng nguyên `popup_1`: tab nhỏ, thân thấp, nút HOME nằm ngoài thân).

### Thiết kế

- Ref là khung `popup_1` nhưng tab tiêu đề rộng hơn, thân cao hơn -> không kéo giãn được `popup_1` (tab nằm giữa ảnh). Tách bằng script (PIL) ra `Popups_Dialogs/popup_1_body.png` (552x392): xóa tab, lấp mép trên bằng cột mép ngay cạnh tab. Thử cắt riêng tab nhưng ảnh gốc cắt mất viền vàng phía trên tab -> dùng luôn sprite thân (thu nhỏ) làm tab, trông như ref.
- `popup_1_body`: Sprite, 9-slice 130; `pixelsPerUnitMultiplier` 1.4 để viền vàng ~14 px như ref.
- Số đo lấy từ ảnh ref (màn 900 px = canvas 1080, x1.2): thân 920x800 (tâm +150), tab 690x200 nhô 60 px trên mép thân, X (432, 348), khung trắng 780x372, nút tròn 192 cách nhau 246, chữ 56, Home `btn_yellow_5_0` 358x150 trong thân, "Ver ..." dưới thân.
- Chữ dùng font game: "SETTING" viền tím (`OutlinePurple`), "Home" viền nâu, phiên bản lấy `Application.version`.
- Ở Main Menu ẩn Home -> thân ngắn đi 180 px, mép trên giữ nguyên (con của Panel neo mép trên, chữ phiên bản neo mép dưới).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `GameAssets/04_UI_Graphics/Popups_Dialogs/popup_1_body.png` | Khung popup_1 không có tab |
| Sửa | `Editor/UI/PopupBuilder.cs` | `BuildSettings` theo ref; `Anchor`; `EnsureSliced` đặt luôn kiểu Sprite |
| Sửa | `UI/Popups/SettingsPopup.cs` | `versionText`; `menuHeightCut` (thu ngắn thân ở Menu) |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Settings |

### Kiểm tra (Play mode, ảnh chụp)

| Bước | Kết quả |
| --- | --- |
| Mở trong ván | Giống ref: tab rộng, 3 nút tròn, Home vàng trong thân, "Ver 1.0" dưới; thân 920x800 |
| Mở ở Main Menu | Không có Home, thân ngắn lại, không còn khoảng trống |
| Lớp tối (Dim) | Có phủ (thử tô đỏ thì phủ kín); ảnh chụp trông nhạt do giữ kênh alpha |
| Console | Không lỗi |

### Ghi chú

- Phiên bản đang là `1.0` (Player Settings → Version). Đổi ở đó thì chữ trong popup đổi theo.

---

## 02:45 (04/10) – Popup "REFILL LIVES?" theo ảnh ref

### Yêu cầu

Bấm số năng lượng (tim) để nạp thì hiện popup như ảnh ref.

### Thiết kế

- Khung giống popup Setting: tách phần dựng khung ra `FramedPopup` (thân `popup_1_body`, tab tiêu đề, nút X) và `WhiteBox`, dùng cho cả 2 popup.
- Trong khung trắng: tim `heart_1` 250 px có số năng lượng (chữ trắng viền đỏ); "Get more lives to / continue playing" (tím); ô đồng hồ `khung_popup_4` (9-slice 45) với đồng hồ tai mèo `btn_time_2` đè mép trái, giờ `mm:ss` tới lần hồi kế (đầy: FULL).
- Nút viền kem như ref = 2 lớp ghép (`FramedButton`): nền `btn_green_2` + mặt `btn_green_1` (Refill, coin 900); nền `btn_yellow_6_0` + mặt `btn_yellow_5_0` (Free live + `btn_video2`). Mặt nút nhích lên 4 px theo ảnh gốc; tắt thì mặt xám (disabledColor 0.55).
- Số đo từ ảnh ref (màn 858 px = canvas 1080, x1.259): thân 920x820 (tâm +166), khung trắng 778x424, nút 345x145 ở (-213 / 206, -277).
- Bỏ số dư coin trong popup (ref không có; thanh trên Menu đã hiện coin).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Editor/UI/PopupBuilder.cs` | `BuildRefillEnergy` theo ref; thêm `FramedPopup`, `WhiteBox`, `FramedButton`, `GameText`; `BuildSettings` dùng khung chung (giữ nguyên số đo) |
| Sửa | `UI/Popups/RefillEnergyPopup.cs` | Số trong tim chỉ là năng lượng hiện có; đồng hồ không còn "Next in" |
| Sửa | `Scenes/SampleScene.unity` | Thay `RefillEnergyPopup` (giữ thứ tự, nối lại `GameFlowUI`), dựng lại `SettingsPopup` |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Năng lượng 4/5, bấm tim trên Menu | Popup mở: tim "4", đồng hồ 30:00 / 29:36; ảnh render 1080x1920 giống ref |
| Coin 130 < 900 | Nút Refill xám, không bấm được; Free live bấm được |
| Nạp đầy (Debug Full) | Popup tự đóng |
| Console | Không lỗi |

Sau khi thử đã trả năng lượng về 5/5.

### Ghi chú

- Ref để nút Refill sáng cả khi thiếu coin; đã làm theo ở đợt 02:55 (thiếu coin → Shop).
- Lớp tối sau popup (Dim đen alpha 0.75) trông nhẹ hơn ref vì project dùng Linear color space: nền chỉ tối đi khoảng một nửa (điểm ảnh 0.90 → 0.48). Đã tăng lên 0.88 ở đợt 02:55.
- Game view lúc thử bị thu nhỏ nên ảnh chụp ScreenCapture không có UI; đã render bằng cách tạm chuyển canvas sang Screen Space - Camera trong Play mode.

---

## 02:55 (04/10) – Refill thiếu coin nhảy vào Shop; nền popup tối hơn

### Yêu cầu

1. Bấm Refill khi thiếu coin thì nhảy vào Shop (như ref).
2. Tăng độ tối nền sau popup lên 0.88 cho mọi popup.

### Thiết kế

- Nút Refill chỉ xám khi năng lượng đã đầy (hoặc đang chờ quảng cáo); thiếu coin vẫn sáng.
- Bấm khi thiếu coin: đóng popup như X nhưng không gọi `onCancel` (vd không mở lại popup Thua), bỏ `onReady`, rồi `ScreenFlow.ShowShop()`.
- `ScreenFlow.ShowShop()`: đang trong ván thì về Menu trước (ván tạm dừng như HOME), rồi chọn tab `shopTab` (mặc định 0). Đang ở Menu thì trượt sang, vừa từ ván ra thì nhảy luôn.
- Dim: hằng `PopupBuilder.DimAlpha = 0.88`; sửa luôn 6 popup có sẵn trong scene.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `UI/Popups/RefillEnergyPopup.cs` | Refill luôn bấm được khi chưa đầy; thiếu coin → `GoShop` |
| Sửa | `UI/Menu/ScreenFlow.cs` | `shopTab`, `ShowShop()` |
| Sửa | `Editor/UI/PopupBuilder.cs` | `DimAlpha = 0.88` |
| Sửa | `Scenes/SampleScene.unity` | Dim của Win, Hồi mạng, Thua, Cài đặt, Mua booster, Nạp năng lượng = 0.88 |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Menu, năng lượng 4/5, coin 130, bấm Refill | Popup đóng, sang tab Shop (0); năng lượng / coin không đổi |
| Trong ván, `ShowRequired(onReady, onCancel)`, bấm Refill | Về Menu, tab Shop; không gọi `onCancel` / `onReady` |
| Đủ coin (cộng tạm 900), bấm Refill | Năng lượng 5/5, trừ 900 (về lại 130), popup đóng, vẫn ở tab Home |
| Độ tối | Cùng điểm trên nút PLAY: kênh xanh 121 (0.75) → 85 (0.88) |
| Console | Không lỗi |

Sau khi thử đã trả năng lượng về 5/5, coin 130.

---

## 03:20 (04/10) – Thanh tab Menu: bấm mượt hơn, vuốt để chuyển panel

### Yêu cầu

Theo video thao tác Menu của bản gốc: bấm tab bên dưới có feel tốt hơn thay vì giật 1 cái; thêm vuốt trái / phải để chuyển panel.

### Phân tích

- Video ref (30 fps): panel trượt ngang ~10 frame (~0.33 s), nhanh lúc đầu rồi chậm dần; icon tab mới phóng to + nhô lên dần, chữ hiện dần.
- Bản mình: trượt panel đã gần như vậy (OutCubic 0.3 s); bật panel khi đổi tab chỉ ~1 ms nên không phải khựng khung hình.
- Nguyên nhân giật: `ButtonPunch` (thêm lúc 01:30) chạy cả trên nút tab: cả tab co 0.9 rồi nảy 1.08 trong khi nền tab chọn (object riêng, không co) đang trượt -> tab lệch khỏi nền.

### Thiết kế

- Nút tab có `ButtonPunch` tắt (UIButtonPunches bỏ qua nút đã có).
- Icon tab vừa chọn: OutBack (vượt nhẹ rồi về: scale 1 → ~1.33 → 1.3); tab bỏ chọn thu về nhanh hơn (0.6 thời gian, OutCubic).
- Vuốt (`MenuSwipe` trên `Panels` → `MainMenuNavigator`): panel theo ngón tay; ở tab đầu / cuối không kéo ra ngoài được (lúc đầu làm cản 30%, đổi theo yêu cầu lúc 03:30); thả tay: kéo quá 20% bề rộng hoặc vuốt nhanh hơn 900 đơn vị/giây thì sang tab kề, không thì trượt về; thời gian trượt tỉ lệ quãng còn lại (tối thiểu 45% của 0.3 s). Chạm khi đang trượt: dừng tại chỗ, kéo tiếp từ đó. Kéo dọc thì bỏ qua (để dành danh sách cuộn trong Shop).
- Kéo bắt đầu trên nút (PLAY, Starter...): EventSystem hủy click; `ButtonPunch` thấy đang kéo thì chỉ về kích thước cũ, không nảy.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Menu/MenuSwipe.cs` | Nhận kéo trên panel, tính vận tốc, gọi navigator |
| Sửa | `UI/Menu/MainMenuNavigator.cs` | `BeginSwipe` / `Swipe` / `EndSwipe`; tách `Go(index, duration)`; giữ bật panel đang hiện khi trượt |
| Sửa | `UI/Menu/MenuTabButton.cs` | `SetSelected(selected, duration)`: OutBack khi chọn, thu nhanh khi bỏ chọn |
| Sửa | `UI/Common/ButtonPunch.cs` | Thả tay khi đang kéo: không nảy |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Tab có `ButtonPunch` tắt; `MenuSwipe` trên `Panels` |
| Sửa | `Scenes/SampleScene.unity` | Như builder |

### Kiểm tra (Play mode, giả lập kéo)

| Bước | Kết quả |
| --- | --- |
| Home, kéo trái 150 (14%), chậm | Trượt về Home |
| Home, kéo trái 250 (23%), chậm | Sang Progress |
| Home, kéo trái 100, vuốt nhanh (1500/s) | Sang Progress |
| Progress (cuối), kéo trái 400 | Panel đứng yên (x = 0); vuốt nhanh rồi thả vẫn ở Progress |
| Shop (đầu), kéo phải 300 | Panel đứng yên; kéo ngược sang trái 200 thì panel đi theo bình thường |
| Kéo qua `MenuSwipe` 500 phải | Sang tab trái; panel khuất tắt sau khi trượt xong |
| Chạm giữa lúc đang trượt (tab 2 → 0) | Panel dừng tại chỗ; thả tay về panel đang chiếm phần lớn màn hình |
| Icon tab | Shop: 1.12 → 1.32 → 1.30, y 130 → 181 → 175; Home thu về 1.0 sau ~0.15 s |
| Console | Không lỗi |

### Ghi chú

- Khi Shop có danh sách cuộn dọc (ScrollRect), ScrollRect sẽ nhận kéo trước `MenuSwipe`: cần chuyển kéo ngang từ ScrollRect lên `MenuSwipe` nếu muốn vuốt trên danh sách.

---

## 04:00 (04/10) – Starter / No Ads lắc mời bấm; nhấn nút tối đi như bản gốc

### Yêu cầu

Theo video Menu bản gốc (`refmenu.mp4`), sửa 2 chỗ khác: hiệu ứng của Starter và No Ads; hiệu ứng khi nhấn button.

### Phân tích video (30 fps, 480x1072)

- Starter: cứ ~62 frame (~2 s) rương nhấc lên ~5 px video (~12 px canvas), lắc nghiêng trái / phải vài nhịp rồi hạ xuống, mỗi lần ~20 frame (~0.65 s); vòng tròn xanh, ruy băng, chữ đứng yên. Bản mình: cả nút nhún lên xuống liên tục.
- No Ads: nhịp ~94 frame (~3 s), lệch pha với Starter; TV nhấc lên, to ra một chút, nghiêng nhẹ.
- Nhấn nút PLAY: không co lại; độ sáng 194 → 146 trong ~3 frame (x0.75), giữ tối suốt lúc giữ tay, thả thì sáng lại; kích thước không đổi. Bản mình: lún 0.9 rồi nảy 1.08.
- (Không yêu cầu, chưa làm) PLAY thở ±~2 % nhịp ~1 s (mình 4 %, 1.8 s); thỉnh thoảng có vệt sáng quét qua PLAY.

### Thiết kế

- `btm_chest_stater.png` dính liền vòng tròn + rương + ruy băng -> tách bằng script (PIL / OpenCV) ra 3 lớp cùng khung 200x192: `starter_back` (vòng tròn vẽ lại: tâm (100.5, 89), bán kính 89, viền (126,181,235) 6 px, trong (26,69,177); + ruy băng), `starter_chest` (màu ấm r > b + 25, đáy kéo dài 16 px phần bị ruy băng che để khi nhấc không lộ đáy cụt), `starter_ribbon` (xanh sáng nửa dưới, đè đáy rương). Ghép lại ở trạng thái nghỉ lệch trung bình 1/255 so với ảnh gốc.
- `MenuShortcutButton`: bỏ nhún; định kỳ icon nhấc + to + lắc nhỏ dần rồi hạ (OutBounce). Pivot icon đặt ở đáy (rương: đáy rương; TV: chân TV) để lắc như đặt trên mặt phẳng. Starter nhấc 12, to 5 %, lắc 9°, nghỉ 1.4 s; No Ads nhấc 10, to 10 %, lắc 6°, nghỉ 2.35 s, chờ đầu 1.3 s (lệch pha).
- Nhấn nút: đổi `ButtonPunch` → `ButtonPress` (giữ GUID .meta nên scene không mất tham chiếu), `UIButtonPunches` → `UIButtonPress`. Mặc định `darken`: giữ tay thì mọi hình / chữ của nút tối 0.75 (`CrossFadeColor`, 0.08 s), nền có ColorTint thì để Button tự tô (pressedColor 0.78); kéo ra ngoài nút thì sáng lại, kéo vào lại thì tối. Punch giữ làm tùy chọn (`punch`, tắt mặc định).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `GameAssets/04_UI_Graphics/Icons/starter_back.png`, `starter_chest.png`, `starter_ribbon.png` | 3 lớp tách từ `btm_chest_stater.png` |
| Sửa | `UI/Menu/MenuShortcutButton.cs` | Lắc mời bấm định kỳ thay nhún (`icon`, `interval`, `firstDelay`, `lift`, `scaleUp`, `angle`, `swings`) |
| Đổi tên + sửa | `UI/Common/ButtonPunch.cs` → `ButtonPress.cs` | `darken` (mặc định) + `punch` (tùy chọn) |
| Đổi tên | `UI/Common/UIButtonPunches.cs` → `UIButtonPress.cs` | Gắn `ButtonPress` |
| Sửa | `UI/HUD/BoosterButton.cs` | Dùng `ButtonPress` |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Starter 3 lớp, pivot đáy, thông số lắc; tab có `ButtonPress` tắt; `SetPivotKeepPlace` |
| Sửa | `Editor/UI/PopupBuilder.cs` | `EnsureSprite` |
| Sửa | `Scenes/SampleScene.unity` | Dựng lại Starter / No Ads (giữ thứ tự, nối navigator) |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Render 8 thời điểm của 1 lần lắc | Rương nhấc + nghiêng, vòng tròn / ruy băng / chữ đứng yên, ruy băng đè đáy rương; TV nhấc + to + nghiêng |
| Nhấn PLAY (giữ) | Fill, chữ PLAY, Level: 0.75; kích thước 1.000 |
| Thả | Về 1.0 |
| Scene | 3 `ButtonPress` (tab, tắt), 3 `UIButtonPress` (canvas), không script mất |
| Console | Không lỗi |

### Ghi chú

- Muốn lại punch: bật `punch` trên `ButtonPress` của nút đó (hoặc gắn sẵn `ButtonPress` có `punch` cho nút cần).

---

## 04:20 (04/10) – Nhấn nút: phần bên trong lún xuống, viền ngoài đứng yên

### Yêu cầu

Ở bản gốc khi nhấn, phần bên trong nút co lại và tối đi, viền trắng bên ngoài không đổi. Bản 04:00 chỉ tối đi nên thiếu cảm giác nhấn xuống; muốn giống cũ hơn.

### Phân tích video (frame 74–104, nút PLAY)

Viền kem ngoài giữ nguyên vị trí / màu. Mặt xanh: lúc giữ thì tối đi, phần sáng thu vào trong, quanh mặt hiện viền xanh đậm (như mặt nút lún xuống trong khung); thả thì sáng lại.

### Thiết kế

- Nút có khung + mặt riêng (con tên `Face` hoặc `Fill` – tự tìm, hoặc gán `face`): khung (ảnh của nút) đứng yên; mọi con trực tiếp (mặt, chữ, icon) thu `insetScale` 0.94 quanh tâm nút (scale + vị trí) và tối 0.78 -> lộ phần nền tối của khung quanh mặt. Button đang tô khung (ColorTint) thì chuyển `targetGraphic` sang mặt để khung không tối.
- Nút 1 ảnh: cả nút thu 0.93 và tối (giữ cơ chế tạm dừng tween scale khác trên nút).
- Thả tay trên nút: nảy nhẹ về (OutBack overshoot 3, 0.18 s) như bản punch cũ; kéo ra ngoài / vuốt: về luôn (OutQuad), kéo vào lại thì lún lại.
- PLAY: `targetGraphic` = `Fill` (builder + scene).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `UI/Common/ButtonPress.cs` | Chế độ khung (lún phần bên trong) / nút 1 ảnh (lún cả nút); bỏ tùy chọn punch riêng |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | PLAY `targetGraphic` = mặt nút |
| Sửa | `UI/HUD/BoosterButton.cs` | Chú thích |
| Sửa | `Scenes/SampleScene.unity` | PLAY `targetGraphic` = `Fill` |

### Kiểm tra (Play mode, render lúc thường / lúc giữ)

| Nút | Lúc giữ |
| --- | --- |
| PLAY | Khung giữ nguyên, Fill + chữ 0.94, lộ viền xanh đậm quanh mặt |
| Refill (khung `btn_green_2` + mặt) | Như trên, lộ viền xanh đậm |
| Free live | Như trên, lộ viền nâu |
| X (1 ảnh) | Cả nút 0.93 |
| Thả | Về 1.0 |
| Console | Không lỗi |

---

## 05:10 (04/10) – Điểm danh hằng ngày

### Yêu cầu

Thêm mục giống Starter / No Ads: điểm danh hằng ngày (hình quyển lịch), có khung hoặc hiệu ứng. Chu kỳ 7 ngày, mỗi ngày một phần quà; lỡ ngày thì giữ tiến độ, không đánh dấu ngày đã lỡ (mở cho đề xuất).

### Thiết kế

- Nút Daily (cột trái, dưới Starter): `icon_daily` trong khung tròn + ruy băng như Starter, tông hồng tím (`daily_back` / `daily_ribbon` đổi màu từ lớp của Starter), chữ "Daily", chấm đỏ "!" (`badge_red`, vẽ bằng script). Còn quà hôm nay: lắc mời bấm (nhịp ~2.5 s, lệch pha với Starter / No Ads) + chấm đỏ; nhận rồi: đứng yên, ẩn chấm.
- Luật: chu kỳ 7 ngày theo lịch máy tính từ ngày đầu mở game; ô i = ngày thứ i của chu kỳ. Lỡ ngày: ô đó để xám, không đánh dấu, vẫn sang ngày sau (không reset về ngày 1); ngày 7 vẫn nhận nếu hôm đó vào. Hết 7 ngày hoặc vắng hơn 7 ngày: chu kỳ mới từ hôm nay. Đồng hồ máy lùi: coi như vẫn ngày đầu (không nhận lại được).
- Đề xuất thêm (bật mặc định, tắt bằng `recoverMissedWithAd`): ô ngày lỡ có icon video, xem quảng cáo để nhận bù.
- Quà mặc định (sửa trong `DailyRewardDatabase.asset`): 1: 100 coin · 2: Hint x1 · 3: 200 coin · 4: Shield x1 · 5: 300 coin · 6: Reveal x1 · 7: 500 coin + mỗi booster x1 + 30 phút năng lượng vô hạn.
- Popup "DAILY REWARD" (khung chung với Cài đặt / Nạp năng lượng): hàng trên ngày 1–4, hàng dưới 5, 6 và ô 7 rộng (nền vàng nhạt); hôm nay viền vàng nhấp nháy; đã nhận mờ + tick; lỡ xám + video; CLAIM → nhận xong thay bằng "Next reward in hh:mm:ss". Mở game lần đầu trong ngày còn quà thì popup tự mở (ở Main Menu).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Daily/DailyRewardDatabase.cs`, `DailyRewardModel.cs`, `DailyRewardManager.cs` | Dữ liệu quà, luật chu kỳ, trao quà |
| Thêm | `UI/Popups/DailyRewardPopup.cs`, `DailyRewardSlot.cs`; `UI/Menu/DailyRewardButton.cs` | Popup, ô quà, nút Daily |
| Thêm | `Editor/UI/DailyRewardBuilder.cs` | Tools → PawBlock → Build Daily Reward |
| Thêm | `GameAssets/10_Data_Configs/DailyRewardDatabase.asset` | Quà 7 ngày |
| Thêm | `GameAssets/04_UI_Graphics/Icons/daily_back.png`, `daily_ribbon.png`, `badge_red.png` | Khung hồng tím, chấm đỏ |
| Sửa | `Gameplay/Save/SaveData.cs` | `DailySave` (`cycleStart`, `claimedMask`) |
| Sửa | `UI/Popups/GameFlowUI.cs` | `dailyRewardPopup`, `OpenDailyReward` |
| Sửa | `UI/Menu/MenuShortcutButton.cs` | `badge`, `SetAttention` (bật / tắt lắc + chấm), `tab < 0` = không chuyển tab |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | `BuildDaily`, `RebuildDaily`, `IconSprite` |
| Sửa | `Editor/UI/PopupBuilder.cs` | Build Popups có popup điểm danh; mở `FramedPopup` / `FramedButton` / `GameText` cho builder khác |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Daily Reward |

### Kiểm tra (Play mode, Debug Next Day để sang ngày)

| Bước | Kết quả |
| --- | --- |
| Menu | Nút Daily dưới Starter, chấm đỏ "!" |
| Mở popup, CLAIM ngày 1 | +100 coin; ô 1 mờ + tick; CLAIM thay bằng "Next reward in …"; nút Daily hết chấm, thôi lắc |
| Bỏ ngày 2, sang ngày 3 | Ô 2 xám + icon video, ô 3 viền vàng |
| Bấm ô 2 (quảng cáo giả lập) | Hint +1, ô 2 thành đã nhận |
| Tới ngày 7 (lỡ 3–6), CLAIM | +500 coin, mỗi booster +1, năng lượng vô hạn 1800 s; ô 3–6 vẫn xám (bù được) |
| Ngày 8 | Chu kỳ mới, ngày 1 nhận được |
| Console | Không lỗi |

Sau khi thử đã trả save về bản sao lưu (coin 210, booster như cũ, không có năng lượng vô hạn).

### Ghi chú

- Ngày tính theo đồng hồ máy (offline): chỉnh giờ máy tới trước vẫn nhận được; muốn chặn cần giờ server.
- Lần test đầu dùng `DOTween.CompleteAll()` (bỏ qua callback) nên quảng cáo giả lập không gọi lại; lộ ra lỗi thật đã sửa: đang có quảng cáo khác thì `Claim` không chờ (trước đó sẽ kẹt chờ mãi).

---

## 06:00 (04/10) – Game feel lúc thắng, màn WELL DONE, confetti

### Yêu cầu

Làm theo 2 video bản gốc: đoạn game feel và hình ảnh lúc thắng, cả confetti; kiểm tra asset đã có particle system chưa.

### Phân tích video (30 fps)

- Đặt khối cuối: ~2 frame sau cả lưới sáng bừng (pastel) ~10 frame rồi về; ~0.6 s sau chuyển ngay sang màn thắng.
- Màn thắng (nền tối, lưới mờ phía sau): ô coin trên cùng, banner "WELL DONE!", 3 sao xám; huy hiệu tròn có mèo bật ra, sau lưng tia sáng xoay + quầng sáng; confetti bắn 2 bên rồi rơi liên tục; sao nảy vàng lần lượt; nút "Continue 10" rồi "x2 Coin 20".
- Bấm Continue: mèo thu lại, coin bung ra rồi bay thành dòng lên ô coin, số tăng; sang màn kế.

### Asset / particle

- Có module Particle System của Unity nhưng **không có package UIParticle** (`UIParticleProjectSettings.asset` chỉ là cấu hình chép từ bản gốc); Particle System không vẽ trên Canvas Overlay -> confetti làm bằng Image UI (`UIConfetti`).
- Dùng: `light_win1` (quầng), `light_win2` (tia), `khung_win1` + `cat_1` (huy hiệu), `star` / `star2`, `coin`. Tạo thêm 4 mảnh confetti trắng (`05_VFX_Particles/Confetti/`: chữ nhật, vuông, tròn, ruy băng) để tô màu lúc chạy. Có sẵn `Procedural_Confetti`, `firework_*` nhưng không cần.

### Thiết kế

- `PlacedBlockView.PlayWinFlash`: lớp sprite trắng đặc (GUI/Text Shader, `BlockFlash.mat` gán trong prefab để có trong bản build) đè thân + tai, alpha 0 → 0.6 (0.08 s) → 0 (0.32 s); `GridManager` gọi cho mọi khối khi thắng (chờ 0.1 s).
- `WinPopup` viết lại (dựng bằng `WinScreenBuilder`): không bảng, nền tối 0.95; vị trí theo tỉ lệ đo trên video (tâm +605 banner, +346 sao, +46 huy hiệu, −509 nút); sao = tim còn lại / tối đa x 3 (tối thiểu 1). Nhịp mở: banner 0 s, huy hiệu + quầng 0.15 s, tia 0.2 s, confetti 0.2 s, sao từ 0.65 s mỗi 0.28 s (to 1.35 rồi về), Continue rồi x2 Coin. Nhận: coin vào ví ngay, hiển thị đếm theo từng coin bay tới (ô coin nảy, âm Coin), xong 0.35 s thì `NextLevel`.
- `UIConfetti`: pool Image (tối đa 160) trong Canvas con; trọng lực 1500, lực cản 1.6, lắc ngang, xoay, lật (scale Y = cos); `Burst` 2 x 45 mảnh chéo lên từ 2 mép, mưa 22 mảnh / giây; 7 màu.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Common/UIConfetti.cs` | Confetti UI |
| Thêm | `Editor/UI/WinScreenBuilder.cs` | Tools → PawBlock → Build Win Screen; BlockFlash.mat + gán prefab |
| Thêm | `GameAssets/05_VFX_Particles/Confetti/*.png`, `09_Shaders_Materials/Materials/BlockFlash.mat` | Mảnh confetti, material lóe sáng |
| Sửa | `UI/Popups/WinPopup.cs` | Màn thắng mới, coin bay |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `PlayWinFlash`, `flashMaterial` |
| Sửa | `Gameplay/Core/GridManager.cs` | Lóe sáng cả lưới khi thắng (`winFlashDelay`) |
| Sửa | `Editor/UI/PopupBuilder.cs` | `BuildWin` → `WinScreenBuilder`; `NewPopup` internal |
| Sửa | `Prefabs/Gameplay/PlacedBlock.prefab`, `Scenes/SampleScene.unity` | Gán material, dựng màn thắng |

### Kiểm tra (Play mode: đặt lời giải bằng `PlaceSolved` để thắng thật)

| Bước | Kết quả |
| --- | --- |
| Thắng | 6 khối lóe sáng pastel (ảnh render), mặt / số vẫn rõ |
| Màn thắng tự mở sau 0.8 s | Banner, 3 sao (đủ 3 tim), huy hiệu + tia, confetti, 2 nút (render 5 thời điểm) |
| Continue | Huy hiệu thu lại, coin bung rồi bay lên ô coin; 310 → 320; level 8 → 9; màn thắng đóng |
| Console | Không lỗi |

Sau khi thử đã trả save về bản sao lưu (level 8, 310 coin).

---

## 06:40 (04/10) – Cài UIParticle, confetti chuyển sang ParticleSystem

### Yêu cầu

Cài UIParticle và dùng để tối ưu hiệu năng confetti.

### Thiết kế

- Package: `com.coffee.ui-particle` 4.14.1 (MIT, mob-sakai/ParticleEffectForUGUI), cài bằng git URL `...ParticleEffectForUGUI.git?path=Packages/src#v4.14.1` (ghim tag để không tự lên bản mới). Phụ thuộc: uGUI + module ParticleSystem (đã có).
- Bản cũ (05:x): mỗi mảnh 1 GameObject + Image, C# cập nhật vị trí / xoay / scale / màu từng mảnh mỗi frame (tối đa 160), Canvas con dựng lại mesh mỗi frame.
- Bản mới: 3 ParticleSystem (mô phỏng trong engine, native) con của 1 `UIParticle`; UIParticle gom tất cả thành 1 mesh vẽ trong uGUI (đúng thứ tự với UI, vẫn trên Canvas Overlay). Không còn GameObject cho từng mảnh, không vòng lặp C# mỗi frame.
- 1 material / 1 draw call: `ConfettiUI.mat` (UI/Default – UIParticle không hỗ trợ shader built-in khác) + atlas `confetti_sheet.png` 2x2 (chữ nhật, vuông, tròn, ruy băng vẽ ở 128 px / ô, trắng để tô màu); Texture Sheet Animation chọn 1 ô ngẫu nhiên / mảnh. Màu: Gradient Fixed 7 màu, chế độ RandomColor.
- Chuyển động giữ như bản cũ: trọng lực (1.5 bắn / 0.6 mưa), Limit Velocity drag 1.6, Noise thấp (lắc ngang), Rotation Over Lifetime trục X (lật) + Z (xoay), Color Over Lifetime mờ 20 % cuối.
- `UIConfetti` giữ nguyên API (`Burst`, `StartRain`, `StopRain`, `AliveCount`) nên `WinPopup` không đổi; vị trí phát (mép trái / phải, mép trên) tính từ kích thước rect lúc gọi.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Packages/manifest.json` | `com.coffee.ui-particle` (git, tag v4.14.1) |
| Thêm | `GameAssets/05_VFX_Particles/Confetti/confetti_sheet.png`, `09_Shaders_Materials/Materials/ConfettiUI.mat` | Atlas + material |
| Xóa | `GameAssets/05_VFX_Particles/Confetti/confetti_rect / square / circle / ribbon.png` | Thay bằng atlas |
| Sửa | `UI/Common/UIConfetti.cs` | Điều khiển ParticleSystem qua UIParticle |
| Sửa | `Editor/UI/WinScreenBuilder.cs` | Dựng UIParticle + 3 ParticleSystem (cấu hình module bằng code), material |
| Sửa | `Scenes/SampleScene.unity` | Dựng lại màn thắng |

### Kiểm tra (Play mode, tua bằng `ParticleSystem.Simulate`, render ảnh)

| Bước | Kết quả |
| --- | --- |
| Cài package | Unity Package Manager báo thành công 4.14.1 (lần đầu lỗi mạng "Empty reply from server", thử lại được) |
| Builder | UIParticle scale 100, 3 ParticleSystem, `ConfettiUI.mat` = UI/Default + `confetti_sheet` |
| Bắn | 0.3 s: chùm trái ở x −510…−120, y +250…+560 (đơn vị canvas); mảnh đúng hình atlas, màu ngẫu nhiên, có mảnh đang lật |
| Số mảnh | 0.15 s: 93 → 2.5 s: 144 (bắn + mưa) |
| Console | Không lỗi |

### Ghi chú

- File save lúc thử có coin 330 / level 9 (ghi lúc 1791061785, trước khi cài package): là tiến độ người dùng tự chơi, không trả về bản sao lưu cũ; lần thử này không bấm nhận thưởng.

---

## 07:10 (04/10) – Bỏ sao, chữ WELL DONE to và đi vào từng chữ

### Yêu cầu

Game không có sao: bỏ sao; chữ "WELL DONE!" to hơn, đặt ngay trên con mèo; xuất hiện từng chữ một, mỗi chữ to rồi nhỏ dần về cỡ chuẩn (đi vào dần dần, không hiện đột ngột). Có đề xuất hay hơn thì nói.

### Thiết kế

- Bỏ 3 sao (UI + `EarnedStars` + âm). Chữ: cỡ 156 (tự co tới 100 nếu màn hẹp), tâm +392 (đỉnh huy hiệu +261), rộng 1040.
- `TMPLetterPopIn`: sửa đỉnh lưới chữ TMP trong LateUpdate (sau layout, trước khi vẽ). Mỗi ký tự: scale 2.4 → 1 (OutBack 1.7), xoay ngẫu nhiên ±16° → 0 (ease out), alpha 0 → 1 trong 35 % đầu; ký tự i bắt đầu ở i x 0.06 s, kéo dài 0.38 s → 10 ký tự ~0.92 s. Xong thì `ForceMeshUpdate` về chữ gốc. Giữ màu / gradient / viền vì chỉ đổi vị trí đỉnh và alpha.
- `WinPopup`: chữ bắt đầu 0.05 s (ẩn trước), huy hiệu + tia + confetti như cũ; 2 nút bật sau khi chữ cuối đáp xuống (0.05 + Duration + 0.05). Bấm nhận khi chữ đang chạy: `Finish` hiện đủ chữ.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Common/TMPLetterPopIn.cs` | Hiệu ứng chữ từng ký tự |
| Sửa | `UI/Popups/WinPopup.cs` | Bỏ sao; `banner` là `TMPLetterPopIn`; nút chờ chữ xong |
| Sửa | `Editor/UI/WinScreenBuilder.cs` | Chữ to ngay trên huy hiệu, gắn `TMPLetterPopIn`; bỏ sao |
| Sửa | `Scenes/SampleScene.unity` | Dựng lại màn thắng |

### Kiểm tra (Play mode, render 7 thời điểm)

0.05 s: "W" to, mờ, nghiêng → 0.18 s: "WEL" (L đang thu) → 0.32 s: "WELL D" → 0.46 s: "WELL DON" → 0.6 s: "WELL DONE!" (dấu ! đang vào) → 0.8 s / 0.95 s: đủ chữ đúng cỡ. Toàn màn: chữ ngay trên huy hiệu, nút dưới. Console không lỗi.

---

## 07:50 (04/10) – Debug Panel (đổi level, cheat)

### Yêu cầu

Tool để chuyển giữa các level khi debug, cheat thắng / thua, + vàng, + máu, booster...

### Thiết kế

- `DebugCheats` (runtime, static): mọi lệnh cheat ở 1 chỗ, gọi API sẵn có của manager (không sửa thẳng dữ liệu) nên hiệu ứng, popup, lưu save giống chơi thật. Dùng lại được cho panel trong game sau này.
- `DebugPanelWindow` (Editor, Tools → PawBlock → Debug Panel, Ctrl + Shift + D): dock được cạnh Game view, tự cập nhật số liệu khi Play; không có trong bản build.
  - Trạng thái: level x/206, Menu / trong ván, GameState, coin, mạng (+ khiên), năng lượng.
  - Level: Trước / Chơi lại / Sau, thanh trượt chọn level → Đổi level / Đổi + vào ván, vào ván level hiện tại bỏ qua năng lượng.
  - Thắng / thua (chỉ khi đang chơi): Thắng ngay (đặt hết lời giải → lóe sáng + màn thắng thật), Đặt 1 khối đúng, −1 mạng, Hết mạng (popup Hồi mạng), Thua hẳn (popup Thua, trừ năng lượng).
  - Coin +100 / +1000 / +10000 / = 0 / đặt số; mạng +1 / đầy / khiên; năng lượng +1 / đầy / 0 / bỏ qua chờ / vô hạn 5 phút; booster từng loại +1 / +10 / = 0, tất cả +10.
  - Khác: điểm danh sang ngày / reset, tốc độ game x0.25–x4 (popup / UI chạy thời gian thực nên không đổi), lưu save ngay.
- API thêm: `LevelManager.LevelCount`, `BoosterInventory.SetCount`, `ScreenFlow.EnterGameNow` (vào ván không kiểm tra năng lượng), `DailyRewardManager.DebugNextDay / DebugReset` public.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Cheats/DebugCheats.cs` | Lệnh cheat |
| Thêm | `Editor/Debug/DebugPanelWindow.cs` | Cửa sổ Debug Panel |
| Sửa | `Gameplay/Level/LevelManager.cs` | `LevelCount` |
| Sửa | `Gameplay/Booster/BoosterInventory.cs` | `SetCount` |
| Sửa | `UI/Menu/ScreenFlow.cs` | `EnterGameNow` |
| Sửa | `Gameplay/Daily/DailyRewardManager.cs` | Debug Next Day / Reset public |

### Kiểm tra (Play mode, gọi DebugCheats)

| Lệnh | Kết quả |
| --- | --- |
| Đổi level 25 khi ở Menu | Level 25, vẫn ở Menu |
| Đổi + vào ván 30 | Level 30, trong ván, Playing |
| Sau / Trước x2 | 31 → 29 |
| Coin +1000 / đặt 500 | 340 → 1340 → 500 |
| Hint +10 / = 2 | 0 → 10 → 2 |
| −1 mạng / đầy / khiên | 2 → 3, có khiên |
| Đặt 1 khối đúng / Thắng ngay | 1 khối; thắng |
| Thua hẳn | Lost, popup Thua mở, popup Hồi mạng đóng, năng lượng 5 → 4 |
| Console | Không lỗi |

Lỗi tìm ra khi thử: bản đầu "Thua hẳn" chờ cố định 1.2 s rồi `GiveUp`; nếu hẹn giờ chạy trước khi LevelManager đổi sang OutOfLives thì không thua. Đã sửa: chờ đúng sự kiện `onStateChanged(OutOfLives)`.

Sau khi thử đã trả save về bản sao lưu (level 10, 340 coin, năng lượng 5, booster như cũ).

---

## 08:05 (04/10) – Popup điểm danh: bố cục 3 + 3 + 1

### Yêu cầu

Đổi bố cục popup điểm danh theo ảnh bản gốc ("Đăng nhập 7 ngày"): 2 hàng đầu mỗi hàng 3 ô, hàng cuối 1 ô ngày 7; giữ art hiện tại.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Editor/UI/DailyRewardBuilder.cs` | Thân 920x1160 (tâm +60); ô 252x240, cách 12; hàng 1 (ngày 1–3) tâm +290, hàng 2 (4–6) +38, hàng 3 ngày 7 rộng 780 tâm −214 (icon 100, cách 34); CLAIM / đồng hồ −445 |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Daily Reward |

### Kiểm tra

Render trong Play mode: 3 + 3 + 1 đúng như ref, ô ngày 7 đủ 5 món (500 coin, 3 booster, 30m vô hạn), đồng hồ / CLAIM dưới cùng trong thân popup. Chỉ mở popup, không nhận quà (save không đổi). Console không lỗi.

---

## 08:30 (04/10) – Cảnh báo QUIT LEVEL khi về Home giữa ván

### Yêu cầu

Đang chơi mà về Home thì cảnh báo (như ảnh bản gốc "QUIT LEVEL – You will lose -1"); vẫn về thì mất 1 máu (năng lượng).

### Thiết kế

- `QuitLevelPopup` (khung chung): khung trắng "You will lose" + tim vỡ `heart_2` với "-1" (chữ trắng viền đỏ), nút đỏ viền kem "Quit" (`btn_red_2_0` + `btn_red_1_0`), X.
- Cài đặt → Home khi đang trong ván: đóng Cài đặt, ván vẫn tạm dừng, mở QUIT LEVEL. Ở Menu nút Home vẫn ẩn như cũ.
- Quit: `EnergyManager.ConsumeOne("Bỏ dở level")` (năng lượng vô hạn thì không trừ – như thua) → về Main Menu. X: chơi tiếp (`Resume`).
- `EnergyManager.ConsumeOne` dùng chung cho thua level và bỏ dở level. Chốt câu hỏi mở trong TaskBreakdown (trước đây bỏ dở level miễn phí).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Popups/QuitLevelPopup.cs` | Popup cảnh báo |
| Sửa | `UI/Popups/SettingsPopup.cs` | `quitPopup`; Home trong ván mở QUIT LEVEL |
| Sửa | `Gameplay/Energy/EnergyManager.cs` | `ConsumeOne(reason)` |
| Sửa | `UI/Popups/GameFlowUI.cs` | `quitLevelPopup` (đóng khi đổi level) |
| Sửa | `Editor/UI/PopupBuilder.cs`, `SettingsBuilder.cs` | `BuildQuitLevel`; Build Settings / Build Popups dựng kèm |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Settings |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Trong ván: Cài đặt → Home | Cài đặt đóng, QUIT LEVEL mở, ván Paused (ảnh render giống ref) |
| X | Popup đóng, ván Playing, vẫn trong ván |
| Home → Quit (save đang có năng lượng vô hạn) | Về Menu, năng lượng 5 → 5 (không trừ khi vô hạn) |
| Tắt vô hạn rồi Home → Quit | Về Menu, năng lượng 5 → 4 |
| Cài đặt ở Menu | Không có nút Home |
| Console | Không lỗi |

Sau khi thử đã trả save về bản sao lưu (level 12, 1150 coin, năng lượng 5, vô hạn còn nguyên).

---

## 09:20 (04/10) – Level Hard / Very Hard

### Yêu cầu

Theo video bản gốc: một số level Very Hard có khung bên ngoài, vào màn chơi có hiệu ứng thể hiện; level Hard tương tự nhưng hình khác.

### Phân tích video (30 fps)

- Main Menu: nút PLAY tím, khung viền kem có tab "Very Hard" phía trên; thỉnh thoảng có vệt sáng quét.
- HUD trong ván: ô LEVEL có khung tím, tab "Very hard" phía trên.
- Vào màn: lưới hiện xong (~0.5 s) → băng sọc cam + băng tím "EXTREME LEVEL" trượt chéo từ góc trên trái vào giữa (~0.3 s), nền tối dần, mèo đầu lâu tím bật lên trên băng, giữ ~1.4 s, đầu lâu thu, băng trượt ra phía trên trái, nền sáng lại. Tổng ~2.2 s.
- Asset có sẵn đủ: `btn_hard_1` / `btn_very_hard_1` (mặt nút), `khung_home_hard` (khung PLAY có tab), `khung_hard1` / `khung_expert1` (khung HUD có tab), `khung_hard_2` / `khung_expert_2` (đầu lâu cam / tím), `khung_hard_3` / `khung_expert_3` (băng HARD LEVEL / EXTREME LEVEL), `khung_expert_4` (dải sọc).

### Thiết kế

- Dữ liệu: `LevelDefinition.difficulty` (Normal / Hard / VeryHard), chỉnh trong Level Editor (cạnh ô Mạng). Dữ liệu ShikaCat không có độ khó → builder đánh dấu mẫu level 20 = Hard, 30 = Very Hard (chỉ khi đang Normal).
- `DifficultyTheme.asset`: mỗi mức khó: chữ tab, mặt nút PLAY, màu chữ "Level n", màu chữ tab, viền chữ PLAY, khung HUD, viền chữ tab HUD, đầu lâu, băng chữ.
- Nút PLAY: con `HardFrame` (khung kem có tab, thân trùng thân nút, đứng yên khi nhấn nhờ `ButtonPressIgnore`); level khó thì ẩn khung thường, mặt đổi màu, chữ PLAY viền đỏ / tím.
- HUD: con `HardFrame` có tab bao ô LEVEL; nền ô đổi sang `rounded_white` (vẽ thêm, 9-slice) để góc vuông không lòi ra ngoài khung bo.
- `DifficultyIntro` (Canvas_HUD, trên cùng): như phân tích; chờ `GridSpawner.IsIntroPlaying` xong mới chạy, khóa thao tác cả lúc chờ; không chạy khi level nạp lúc ở Main Menu. Âm mới `SoundId.HardLevel` (warning.ogg).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Level/DifficultyTheme.cs` | `LevelDifficulty`, `DifficultyStyle`, `DifficultyTheme` |
| Thêm | `UI/HUD/DifficultyIntro.cs` | Hiệu ứng vào màn |
| Thêm | `UI/Common/ButtonPressIgnore.cs` | Con của nút không lún khi nhấn |
| Thêm | `Editor/UI/DifficultyBuilder.cs` | Tools → PawBlock → Build Difficulty |
| Thêm | `10_Data_Configs/DifficultyTheme.asset`, `Frames_Borders/rounded_white.png` | Theme, nền ô LEVEL bo góc |
| Sửa | `Gameplay/Level/LevelDefinition.cs` | `difficulty` |
| Sửa | `Editor/Level/LevelEditorWindow.cs` | Chọn độ khó |
| Sửa | `UI/Menu/PlayButtonView.cs`, `UI/HUD/LevelHUD.cs` | Đổi giao diện theo độ khó |
| Sửa | `UI/Common/ButtonPress.cs` | Bỏ qua `ButtonPressIgnore` |
| Sửa | `Gameplay/Core/GridManager.cs` | Khóa thao tác khi `DifficultyIntro.Playing` |
| Sửa | `Gameplay/Audio/SoundId.cs`, `Editor/Audio/AudioSetup.cs`, `AudioDatabase.asset` | `HardLevel` |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Build Main Menu dựng kèm phần mức khó của nút PLAY |
| Sửa | `Levels/ShikaCat/Level_020.asset`, `Level_030.asset`, `Scenes/SampleScene.unity` | Mẫu độ khó, dựng giao diện |

### Kiểm tra (Play mode, render)

| Bước | Kết quả |
| --- | --- |
| Main Menu level 20 / 30 | Nút đỏ tab "Hard" / tím tab "Very Hard", chữ PLAY viền đỏ / tím |
| HUD level 20 / 30 / 12 | Khung đỏ tab Hard / tím tab Very Hard / thường (không khung) |
| Vào ván level 30 (7 thời điểm) | Băng trượt vào, đầu lâu bật, giữ, thu, băng trượt ra; thao tác bị khóa trong lúc chạy, xong mở lại |
| Hard vs Very Hard | Băng đỏ HARD LEVEL + đầu lâu cam / băng tím EXTREME LEVEL + đầu lâu tím |
| Console | Không lỗi |

Ghi chú: ảnh render chuyển canvas sang Screen Space - Camera để chụp nên số trên lưới đè lên băng; trong game Canvas_HUD là Overlay nên băng luôn vẽ trên lưới. Sau khi thử đã trả save về bản sao lưu (level 12, 1150 coin).

---

## 09:45 (04/10) – Sửa: nút PLAY ở level Hard / Very Hard không bấm được

### Lỗi

Level 20 (và 30): bấm PLAY không có tác dụng.

### Nguyên nhân

`PlayButtonView.ApplyDifficulty` ẩn khung thường bằng `frame.enabled = false`, mà `frame` là Image gốc của nút – Graphic duy nhất nhận raycast (mặt nút, chữ, khung có tab đều `raycastTarget = false`). Tắt nó thì EventSystem không chạm trúng gì của nút.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `UI/Menu/PlayButtonView.cs` | Giữ Image gốc bật, chỉ đặt alpha 0 khi level khó (trả màu gốc khi level thường) |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Raycast UI tại tâm PLAY, level 20 / 30 / 12 | Trúng `PlayButton`, có handler click (trước: không trúng gì ở 20 / 30) |
| Click PLAY ở level 20 | Vào ván; hiệu ứng HARD LEVEL chạy, xong thì mở thao tác lưới |
| Thắng level 19 → Continue → level 20 (chạy frame thật) | Hiệu ứng xong, lưới không khóa, không có UI che lưới |

Ghi chú: project tắt Domain Reload (Enter Play Mode Options), biến static giữ giá trị giữa các lần Play; `DifficultyIntro.Playing` vẫn luôn được trả về false khi ẩn / tắt nên không bị kẹt.

Sau khi thử đã trả save về bản sao lưu.

---

## 10:10 (04/10) – Sửa: nút PLAY khó khi nhấn lộ nền phòng; băng HARD / EXTREME LEVEL hụt 2 mép

### Lỗi

1. Nhấn PLAY ở level Hard / Very Hard: mặt nút lún vào (ButtonPress) để lộ nền phòng giữa mặt nút và viền kem (viền với nút như tách rời).
2. Hiệu ứng vào màn: băng chữ ngắn hơn màn hình, 2 đầu cắt thẳng lơ lửng (bản gốc băng chạy quá 2 mép).

### Nguyên nhân / cách sửa

1. Nút thường có nền tối `btn_green_4` sau mặt nút nên lún vào lộ viền tối; nút khó chỉ có khung kem `khung_home_hard` rỗng ở giữa, Image gốc lại trong suốt. Thêm `DifficultyStyle.playBacker` = `btn_hard_2` / `btn_very_hard_2` (nền tối có viền kem) cho Image gốc; level khó thì thu Image gốc về đúng bề rộng khung có tab (525) để viền kem của nền không lòi ra 2 bên. Level thường trả lại sprite / kích thước cũ.
2. `khung_hard_3` / `khung_expert_3` cắt thẳng 2 đầu, kéo giãn thì méo chữ. Tạo bằng script (numpy) `khung_hard_3_wide.png` / `khung_expert_3_wide.png` (2543x631): nối 650 px băng trơn mỗi bên theo đúng độ nghiêng (0.106), lấy cột cách mép 90 px và trộn dần 140 px để không lộ đường nối; chữ giữ nguyên. `DifficultyIntro` hiện băng theo tỉ lệ `bannerScale` 0.8 (≈ 2034 rộng > màn 1080); băng xuất phát xa hơn `offscreen` (−2200, 900).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Frames_Borders/khung_hard_3_wide.png`, `khung_expert_3_wide.png` | Băng chữ kéo dài |
| Sửa | `Gameplay/Level/DifficultyTheme.cs` | `playBacker` |
| Sửa | `UI/Menu/PlayButtonView.cs` | Nền tối + bề rộng khi level khó |
| Sửa | `UI/HUD/DifficultyIntro.cs` | `bannerScale`, `offscreen` xa hơn |
| Sửa | `Editor/UI/DifficultyBuilder.cs` | Theme có `playBacker`, băng `_wide`; nâng cấp theme cũ (`UpgradeTheme`) |
| Sửa | `DifficultyTheme.asset`, `Scenes/SampleScene.unity` | Chạy Build Difficulty |

### Kiểm tra (render Play mode)

| Bước | Kết quả |
| --- | --- |
| PLAY level 20 / 30 / 12, thường | Như cũ; rộng 525 / 525 / 564 |
| PLAY level 20 / 30 / 12, đang nhấn | Lộ viền tối nâu / tím / xanh trong viền kem, không còn khe hở, không viền đôi |
| Băng HARD LEVEL / EXTREME LEVEL | Chạy quá cả 2 mép màn, không thấy đường nối |
| Console | Không lỗi |

Sau khi thử đã trả save về bản sao lưu.

---

## 10:30 (04/10) – Popup NOTICE, mua bằng VND báo "Purchase Failed!"

### Yêu cầu

Như ảnh bản gốc: mọi thao tác mua bằng tiền VND hiện popup "NOTICE – Purchase Failed! – Continue".

### Thiết kế

- Game chưa có nút mua bằng VND (tab Shop còn là màn tạm; Starter / No Ads chuyển sang Shop) nên làm sẵn nền: `IAPService.Purchase(productId, onDone)` – chưa có SDK thì luôn thất bại, phát `PurchaseFailed`; khi tích hợp chỉ thay thân hàm. Nút mua trong Shop / gói Starter / No Ads sau này gọi hàm này.
- `NoticePopup` dùng chung (khung chung như Cài đặt / QUIT LEVEL): tiêu đề, khung trắng nội dung (tự co chữ), nút xanh viền kem "Continue", X. `GameFlowUI.ShowNotice`; GameFlowUI nghe `PurchaseFailed`.
- Debug Panel: nút "Thử mua bằng VND (IAP)".

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Economy/IAPService.cs` | Mua VND (chưa SDK) |
| Thêm | `UI/Popups/NoticePopup.cs` | Popup thông báo |
| Sửa | `UI/Popups/GameFlowUI.cs` | `noticePopup`, `ShowNotice`, nghe `PurchaseFailed` |
| Sửa | `Editor/UI/PopupBuilder.cs` | `BuildNotice`; Tools → PawBlock → Build Notice Popup; Build Popups dựng kèm |
| Sửa | `Gameplay/Cheats/DebugCheats.cs`, `Editor/Debug/DebugPanelWindow.cs` | Thử mua VND |
| Sửa | `Scenes/SampleScene.unity` | Thêm NoticePopup |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Ở Shop, `IAPService.Purchase` | onDone(false), popup NOTICE "Purchase Failed!" (ảnh giống ref) |
| Continue | Đóng |
| Trong ván, mua thất bại → X | Ván Paused khi mở, Playing sau khi đóng |
| Console | Không lỗi |

---

## 11:30 (04/10) – Tab Shop

### Yêu cầu

Làm giao diện Shop theo video bản gốc.

### Phân tích video (30 fps, 480x1072)

Mái hiên đỏ trắng trên cùng có ô năng lượng + coin; danh sách cuộn dọc (cuộn dưới mái hiên):
1. Starter Pack (khung vàng, tem "90% Off"): 3000 coin, Hint / Freeze / Reveal x1 – 66000 VND.
2. "Remove Ads": dòng "No more disruptive ads" – 210000; thẻ No Ads Bundle (2500, x1 x1 x1, ∞1h) – 263000.
3. "Special Bundle": Fail (1000, x1 x1 x1, ∞30m) 158000 · Mini (2000, x1 x1, ∞1h) 158000 · Super (6000, x3 x3 x1, ∞1h) 289000 · Mega (12000, x6 x6 x2, ∞3h) 553000 · Deluxe (18000, x9 x9 x3, ∞6h) 816000.
4. "Coin": lưới 3 x 2 – 1000 / 5000 / 10000 / 25000 / 50000 / 100000 coin: 52000 / 237000 / 421000 / 816000 / 1600000 / 2650000 VND.

### Asset / dữ liệu

- Có sẵn đủ: `frame_starter`, `frame_pack`, `frame_pack_ads1` (1008x521), `frame_pack_ads` (dòng, 9-slice), `frame_coin` (323x421), `title_ads` / `title_bundle` / `title_coin`, `sale_pop2`, `icon_bundle_1..6`, `icon_noadsbundle`, `icon_coin_*`, `no_ads_2`, `bg_shop_1` (mái hiên). Kích thước khung gốc khớp video (thẻ ~ 1000 rộng).
- `*PackShop.asset` gốc (14 gói: productId, coin, iLife phút, freeze, hint, reveal) khớp số trong video; script gốc không có trong project → chép số sang `ShopDatabase.asset` (freeze = Shield).

### Thiết kế

- Dữ liệu `ShopDatabase` / `ShopProduct`; `ShopCardView` đọc lúc chạy (sửa asset không cần dựng lại UI).
- Mua: `ShopService.Buy` → `IAPService.Purchase` (chưa SDK → popup NOTICE "Purchase Failed!"); thành công → `Grant` (coin, booster, năng lượng vô hạn, `SaveData.noAds`); dòng Remove Ads ẩn khi đã noAds.
- Bố cục: `NestedScrollRect` + VerticalLayoutGroup (lề trên 400 để thẻ đầu nằm dưới mái hiên, lề dưới 330 cho thanh tab, cách 26), thẻ native 1008x521; tọa độ trong thẻ theo pixel khung gốc (ô coin x 454–632, ô vật phẩm x 664–943, y 56–298; tên + nút giá ở thanh dưới); ô vật phẩm 2x2 cố định (Hint, Shield / Reveal, ∞) như bản gốc (Mini Offer trống ô Reveal). Lưới coin GridLayoutGroup 3 cột. Mái hiên + ô năng lượng / coin (y −170) đè lên danh sách.
- Kéo ngang trên danh sách chuyển lên `MenuSwipe` (`NestedScrollRect`) → vuốt vẫn chuyển tab (đã ghi chú ở đợt 03:20).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Economy/ShopDatabase.cs`, `ShopService.cs` | Dữ liệu, mua / trao quà |
| Thêm | `UI/Menu/ShopCardView.cs`, `UI/Common/NestedScrollRect.cs` | Thẻ, cuộn lồng trong vuốt |
| Thêm | `Editor/UI/ShopBuilder.cs`, `10_Data_Configs/ShopDatabase.asset` | Tools → PawBlock → Build Shop |
| Sửa | `Gameplay/Save/SaveData.cs` | `noAds` |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Shop dùng ShopBuilder (bỏ màn tạm); `BuildEnergyPill` / `BuildCoinPill` internal |
| Sửa | `Scenes/SampleScene.unity` | Chạy Build Shop |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Render đầu / giữa / cuối danh sách | Giống video: Starter (tem 90% Off), Remove Ads, No Ads Bundle, Fail / Mini / Super ..., Deluxe, lưới Coin |
| Bấm giá Starter, bấm ô 1000 coin | Popup "Purchase Failed!", coin không đổi |
| Kéo ngang 480 trên danh sách | Panel theo tay, thả → sang tab Home |
| Kéo dọc | Danh sách cuộn, không đổi tab |
| Console | Không lỗi |
