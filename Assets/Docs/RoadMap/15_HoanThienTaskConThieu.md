# 15 – Hoàn thiện task còn thiếu trong Estimate

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `9ed6c29` "Hồ sơ người chơi, giờ server, Cheat Panel, 60 FPS, rung haptic" (nhánh `Day-6`) |
| Thời gian push | 2026-10-09 13:41 |
| Cập nhật lần cuối | 2026-10-09 13:41 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 01:00 (09/10) | Hoàn thiện task còn thiếu (trừ Assembly / Meta / Build) | Khung LEVEL dùng art gốc; coin thắng theo độ khó; màn "Coming soon" khi hết level; ô hướng dùng art `khung_direc`; hiệu ứng băng Khiên động; đồng hồ giờ server chống chỉnh giờ máy; ghi số liệu chơi thật + xuất CSV để chỉnh độ khó |
| 01:35 (09/10) | Popup Hồ sơ người chơi | Bấm avatar ở Menu mở hồ sơ: avatar (12 mèo, đổi được), tên (sửa được), ID, tháng bắt đầu, LVL, 6 ô thống kê; cùng popup xem được hồ sơ người khác (cho bảng xếp hạng sau này) |
| 11:15 (09/10) | Cheat Panel trong bản build + mở khóa 60 FPS | Bấm 10 lần góc trên trái (hoặc F1) mở Cheat Panel ngay trên điện thoại; Android hết bị khóa 30 FPS |
| 11:45 (09/10) | Rung điện thoại kiểu haptic | Rung ngắn có cường độ theo từng sự kiện (đặt khối, sai, booster, khiên, thắng, thua) thay cho 1 cú rung dài khi sai |
| 12:10 (09/10) | Sửa chữ WELL DONE không đi vào từng chữ | Chữ lại hiện dần từng ký tự như 09_LevelEditor (bị mất từ lần tối ưu ở 14) |

---

## 01:00 (09/10) – Hoàn thiện task còn thiếu trong Estimate

### Mục tiêu / Vấn đề

Các task còn dở trong `Estimate_Nguyễn Đức Tùng.xlsx`, trừ Assembly definition + test, nhóm Meta & UX và Build Demo:
Art HUD khung LEVEL (0 %), coin thắng theo độ khó (0 %), UI khi hết level (0 %), art mũi tên (30 %),
playtest + chỉnh độ khó theo dữ liệu thật (0 %), hiệu ứng băng Khiên (50 %, ảnh tĩnh), chống chỉnh giờ máy (20 %, mới chặn lùi giờ).

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Clock/TrustedClockModel.cs`: luật đồng hồ tin cậy (C# thuần): có giờ server thì giờ = giờ server lúc đồng bộ + thời gian đơn điệu (`realtimeSinceStartup`, không đổi khi chỉnh giờ máy); offline thì giờ máy nhưng không nhỏ hơn mốc đã lưu; `IsDeviceClockOff` khi lệch > 5 phút.
- `Gameplay/Clock/TrustedClock.cs`: lấy giờ server bằng header `Date` của request HEAD HTTPS (Google → Cloudflare → Microsoft), bù nửa thời gian khứ hồi; đồng bộ khi mở game và khi quay lại app (cách ≥ 120 s); phát `Synced`. Mốc lưu ở `SaveData.clock.lastTrustedUtc`.
- `Gameplay/Analytics/LevelStatsRecorder.cs` (trên `Manager`): ghi số liệu chơi thật từng level vào `SaveData.levelStats` (lượt, thắng, thua, bỏ dở, hết mạng, nước sai, booster, thời gian chơi, lượt thắng nhanh nhất). Lượt chỉ tính khi người chơi thao tác (level nạp sẵn ở nền Menu không bị đếm); thời gian chỉ tính lúc `Playing`, dừng khi app vào nền.
- `Editor/Level/LevelStatsExporter.cs`: Tools → PawBlock → Level Stats: xuất CSV từ save máy này hoặc gộp nhiều `save.json` của người chơi thử; cột khóa `name` khớp cửa sổ Level Difficulty → "Nạp dữ liệu thực tế" (tương quan Spearman với độ khó lý thuyết).
- `AllLevelsCompletedEvent`; `SaveData.allLevelsCleared`, `ClockSave`, `LevelStatSave`.
- Scene: `ShieldFrost` (Image `ice_effect` phủ toàn `Canvas_HUD`, không chắn bấm), `LevelStatsRecorder` trên `Manager`.

**Sửa**

- `EconomyConfig`: `winCoinRewardHard` = 20, `winCoinRewardVeryHard` = 30 (theo `gamePlay_CoinWinLevelHard / Expert` của RemoteConfig gốc), `WinCoinReward(difficulty)`; `WinPopup` thưởng theo độ khó level vừa thắng (x2 vẫn gấp đôi).
- `LevelManager.NextLevel`: level cuối → `allLevelsCleared`, phát `AllLevelsCompletedEvent`; `AutoAdvance` thì quay về level 1, có `GameFlowUI` thì chờ. `RestartFromFirst()`, `IsLastLevel`.
- `GameFlowUI`: nghe `AllLevelsCompletedEvent` → NoticePopup "COMING SOON" ("You cleared all N levels!…"), Continue → level 1.
- `CellView`: art ô hướng `badgeSquareSprite / badgeTallSprite / badgeWideSprite` (= `khung_direc_1/2/3` bản gốc), vẽ Simple, giữ tỉ lệ ảnh; không gán thì dùng 9-slice cũ. `Cell.prefab`: gán 3 sprite, màu badge trắng.
- `Canvas_HUD/…/LevelFrame`: sprite `khung1` (khung tím, ô tối chứa hàng tim) thay ô màu phẳng.
- `ShieldIndicator`: bật → viền băng toàn màn lóe rồi tan, lớp băng đóng lại (OutBack) rồi thở, bông tuyết lấp lánh (pool 10 Image tạo 1 lần), vệt sáng lướt định kỳ; chặn lỗi → lớp băng rung, phồng, vỡ, mảnh băng văng 4 phía, xoay và tan.
- `EnergyManager`, `DailyRewardManager`: dùng `TrustedClock` (thay `DateTimeOffset.UtcNow` / `DateTime.Now`), tính lại khi đồng bộ xong.
- `SaveSystem.Upgrade`: tạo `clock`, `levelStats` cho save cũ.

### Tác dụng

- Thưởng coin đúng bản gốc theo độ khó; hết level có màn thông báo thay vì lặng lẽ quay vòng.
- Ô hướng và khung LEVEL dùng art gốc, hết ảnh debug / ô màu phẳng.
- Khiên có phản hồi rõ khi bật và khi cứu một lần sai.
- Chỉnh giờ máy tới / lùi không còn hồi năng lượng hay nhận điểm danh sớm khi có mạng; offline vẫn chặn lùi giờ.
- Có dữ liệu thật để chỉnh đường độ khó: phát bản build cho người chơi thử, gom `save.json`, xuất CSV, nạp vào Level Difficulty.

### Kiểm tra

- Unity biên dịch 0 lỗi, 0 cảnh báo.
- Play mode (lưu tạm `save.json`, xong trả lại):
  - Level 32: chụp màn hình khung LEVEL `khung1`, ô hướng `khung_direc_1/2` (scale 0.73 / 0.63, góc bo).
  - Bật Khiên (DOTween chậm 0.08x để chụp): viền băng toàn màn alpha ~0.53 rồi tan; bông tuyết lấp lánh; chặn lỗi → mảnh băng văng ra, còn 3 mạng.
  - Level cuối (203, Normal): WinPopup hiện 10 / x2 20, nhận +10 coin → "COMING SOON – You cleared all 203 levels!", Continue → Level 1, Playing. `WinCoinReward`: Normal 10, Hard 20, VeryHard 30.
  - `TrustedClock`: đồng bộ được giờ server (máy lệch −2 s). Giả lập `TrustedClockModel`: offline lùi giờ 1000 → 500 vẫn 1000; đồng bộ server 2000 khi máy để 9000 → lệch −7000, nghi ngờ = true; sau 30 s đơn điệu máy chỉnh 99999 → 2030; máy lùi về 0 → 2031. Đọc header `Date` RFC 1123 đúng.
  - `LevelStatsRecorder`: PlayLevel 5 từ Menu, đi sai 1 lần, chơi lại, thắng → attempts 2, quits 1, wins 1, mistakes 1 (lần nạp ở nền Menu không bị đếm).
- Phần còn lại của "Playtest toàn bộ level": cần người chơi thật, chưa làm.

---

## 01:35 (09/10) – Popup Hồ sơ người chơi

### Mục tiêu / Vấn đề

Bấm avatar ở Main Menu thì hiện hồ sơ như mẫu (thẻ người chơi + "Thống kê chung"). Sau này làm bảng xếp hạng thì bấm vào 1 người để xem hồ sơ của họ bằng chính popup này.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Profile/PlayerProfile.cs`: hồ sơ để hiển thị (dữ liệu thuần, chỉ đọc): ID, tên, avatar, ngày bắt đầu, level, danh sách `ProfileStat`, `IsLocal`. `ProfileStatType` (FirstTryWins, CoinsEarned, LevelsCleared, HardWins, BoostersUsed, BestWinStreak), `ProfileStats.Label / Format` ("1,250", "1.2M").
- `Gameplay/Profile/PlayerProfileService.cs`: hồ sơ máy này trong `SaveData.profile`: tạo lần đầu (ID 8 số ngẫu nhiên, tên "Player xxxx", ngày bắt đầu theo `TrustedClock`), `Local()`, `Rename` (2–16 ký tự, bỏ ký tự điều khiển), `SetAvatar`, event `Changed`.
- `Gameplay/Profile/ProfileStatsRecorder.cs` (trên `Manager`): thắng ngay lượt đầu (theo `levelStats`), level cao nhất đã thắng, thắng Hard / Very Hard, chuỗi thắng (thua hẳn về 0) + kỷ lục, coin nhận được (`CoinWallet.Changed`), booster đã dùng. Bỏ qua màn tutorial.
- `Gameplay/Profile/AvatarDatabase.cs` + `Configs/AvatarDatabase.asset`: 12 mèo ShikaCat `Cats_InGame` (thứ tự = AvatarId, chỉ thêm vào cuối).
- `UI/Popups/ProfilePopup.cs`: `Show(PlayerProfile)`. Hồ sơ của mình: nút EDIT (ô nhập tên, tắt rich text), bấm avatar → lớp chọn 4 x 3 mèo (viền vàng ở mèo đang chọn), tự vẽ lại khi `PlayerProfileService.Changed`. Hồ sơ người khác: ẩn EDIT / CHANGE, avatar không bấm được. Mở giữa ván thì tạm dừng.
- `UI/Popups/ProfileStatTile.cs`, `UI/Menu/AvatarButton.cs` (avatar Menu hiện mèo đang chọn, bấm → `GameFlowUI.OpenProfile`).
- `Editor/UI/ProfileBuilder.cs` (Tools → PawBlock → Builders → Build Profile): dựng `Canvas_Popup/Popups/ProfilePopup` (khung như popup cài đặt, thẻ trắng, huy hiệu LVL `daily_back`, ruy băng `daily_ribbon`, ô `khung_popup_4`; icon: cúp, coin, sao, đầu lâu mèo, bóng đèn, tim, lịch), avatar Menu, `AvatarDatabase`, `ProfileStatsRecorder`.

**Sửa**

- `GameFlowUI`: `profilePopup`, `OpenProfile()`, `ShowProfile(PlayerProfile)` (bảng xếp hạng gọi hàm này).
- `MainMenuBuilder.BuildAvatar`: khung `khung3` + mèo đang chọn + `AvatarButton` (thay ảnh `khung_avatar` cố định).
- `SaveData.profile` (`ProfileSave`), `SaveSystem.Upgrade` tạo cho save cũ.

### Tác dụng

- Có màn hồ sơ: người chơi đổi tên / avatar, xem thành tích.
- Bảng xếp hạng sau này chỉ cần tạo `PlayerProfile` từ dữ liệu server (`IsLocal = false`) rồi gọi `GameFlowUI.ShowProfile`, không phải sửa popup.

### Kiểm tra

- Unity biên dịch 0 lỗi; Build Profile chạy lại được (sửa lỗi duyệt Transform khi đang xóa con).
- Play mode (lưu tạm `save.json`, xong trả lại), có chụp màn hình:
  - Bấm avatar Menu → popup mở, tạo hồ sơ ID 8 số, "Player xxxx", 10/2026, LVL 12; `CoinWallet.Add(50)` → COINS EARNED 50.
  - Bấm avatar → lớp chọn mèo; chọn mèo 4 → avatar popup và Menu cùng đổi sang `Image_Cat_Purple`.
  - Đổi tên " x " → bị từ chối (ngắn), tên có `<b>` hiện nguyên văn (tắt rich text).
  - Hồ sơ giả của người khác (ID 20399774, 1,250 coin…) → chỉ xem, không có EDIT / CHANGE.
  - Thắng level 28 (Hard) lượt đầu → FirstTry 1, Hard 1, chuỗi 1/1, Levels cleared 28; thua level 30 → chuỗi 0/1.
- Sửa sau phản hồi: chữ EDIT bị chìm vào nền xanh (cỡ 34, viền mỏng) → nút 204 x 106, chữ 56, preset viền dày `Outline Green Bold` (`GameFontSetup.OutlineGreenBold`); ô tên thu hẹp để không đè nút.
- Font `Fredoka-Bold SDF` bị Unity tự thêm glyph lúc test tên tiếng Việt: đã `git restore`.

## 11:15 (09/10) – Cheat Panel trong bản build + mở khóa 60 FPS

**Thêm**

- `Gameplay/Cheats/CheatPanel.cs`: panel cheat chạy cả trong APK. Tự tạo lúc mở game (`RuntimeInitializeOnLoadMethod`), không cần đặt vào scene. Mở: bấm nhanh 10 lần (mỗi lần cách ≤ 0.5 s) vào góc trên trái (25% rộng x 8% cao), hoặc phím F1 / ` . Vẽ bằng IMGUI, 4 tab Level / Tài nguyên / Booster / Khác, gọi `DebugCheats` như Debug Panel Editor. Lúc mở có Canvas trong suốt `sortingOrder` cao nhất chặn chạm xuống UI và lưới. Kéo dọc để cuộn. Tab Khác: chọn FPS 30/60/90/120, bật bộ đếm FPS góc trên phải (nhớ qua PlayerPrefs), tốc độ game, điểm danh, lưu save. Bỏ khỏi bản phát hành: thêm define `DISABLE_CHEATS`.
- `Gameplay/Util/FrameRateSetup.cs`: trên mobile đặt `vSyncCount = 0`, `Application.targetFrameRate = 60` trước khi nạp scene.

### Nguyên nhân giật

- Code không đặt `Application.targetFrameRate` → Android / iOS mặc định **khóa 30 FPS** (Android còn bỏ qua `vSyncCount` trong Quality). Quality mặc định của Android là Medium (MSAA tắt) nên không phải do đồ họa nặng.

### Kiểm tra

- Unity biên dịch 0 lỗi; Play mode mở panel → hiển thị đúng 4 tab, trạng thái level / coin / mạng / năng lượng; console không lỗi.

## 11:45 (09/10) – Rung điện thoại kiểu haptic

**Thêm**

- `Gameplay/Feedback/Haptics.cs` + enum `HapticType` (Light / Medium / Heavy / Success / Failure). Android: lấy `Vibrator`, API 26+ dùng `VibrationEffect.createOneShot` / `createWaveform` (có cường độ nếu máy hỗ trợ), máy cũ `vibrate(ms)`; không lấy được Vibrator thì `Handheld.Vibrate()` (giữ tham chiếu để Unity tự thêm quyền `VIBRATE`). iOS: `Assets/Plugins/iOS/HapticsIOS.mm` (`UIImpactFeedbackGenerator`, `UINotificationFeedbackGenerator`).
- `Gameplay/Feedback/GameHapticEvents.cs`: gắn trên object `AudioManager` trong `SampleScene`, nối rung vào event có sẵn (giống `GameAudioEvents`).
- Cheat Panel → tab Khác → "Thử rung": 5 nút thử từng kiểu (bỏ qua cài đặt Rung).

**Sửa**

- `AudioManager.Vibrate(HapticType type = Medium)` gọi `Haptics.Play` thay vì `Handheld.Vibrate()` (cũ: rung dài ~0.5 s, không chỉnh được, chỉ khi đặt sai).
- `GameplayFeedback`: bỏ rung máy (chuyển sang `GameHapticEvents`), chỉ còn rung camera.

### Tác dụng

- Đặt khối có "tick" nhẹ, sai rung mạnh, thắng / thua có nhịp riêng; tắt Rung trong Cài đặt là tắt hết.

### Kiểm tra

- Unity biên dịch 0 lỗi; biên dịch thử script cho target Android (`CompilePlayerScripts`) qua, nhánh `UNITY_ANDROID` không lỗi.
- Play mode: `GameHapticEvents` có trên `AudioManager`, gọi đủ 5 kiểu không lỗi (Editor không rung); tab Khác hiện hàng nút Thử rung. Chưa test trên máy thật.

## 12:10 (09/10) – Sửa chữ WELL DONE không đi vào từng chữ

**Sửa**

- `UI/Common/TMPLetterPopIn.cs`: `Play()` / `Hide()` bỏ cờ `_hidden` **trước** `Prepare()`.

### Nguyên nhân

- Ở RoadMap 14 (tối ưu Update) việc ẩn chữ chuyển sang nghe `TMPro_EventManager.TEXT_CHANGED_EVENT`. `Prepare()` gọi `ForceMeshUpdate()` → TMP phát sự kiện ngay lập tức, lúc đó `_hidden` vẫn `true` nên `OnTextChanged` làm trong suốt cả lưới, rồi `Prepare` mới chép lưới gốc → bản gốc có alpha 0. Kết quả: suốt hiệu ứng chữ vô hình, hết hiệu ứng `Finish()` mới hiện cả cụm "WELL DONE!" một lần (trông như không có hiệu ứng).

### Kiểm tra

- Play mode, thắng level, đo alpha lưới chữ theo thời gian: trước khi sửa t = 0 → 0.72 s alpha luôn 0, 0.92 s hiện đủ; sau khi sửa số chữ đã hiện 0 → 3 → 5 → 9 (t = 0 / 0.22 / 0.42 / 0.62 s), đúng như thiết kế ở 09.
- Test làm save nhảy về level 1: đã đưa về level 12 như trước khi test (coin không đổi, số liệu thắng có thể cộng thêm 2 ván test).
