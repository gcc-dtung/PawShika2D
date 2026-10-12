# 17 – Old-Core: quay lại cơ chế đồng hồ

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `db32b48` "Old-Core: đồng hồ thay 3 mạng, Đóng băng, chạm gỡ khối, rung mới" (nhánh `Old-Core`) |
| Thời gian push | 2026-10-10 20:29 |
| Cập nhật lần cuối | 2026-10-10 20:29 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 16:05 (09/10) | Nhánh `Old-Core`: đồng hồ thay 3 mạng | Đồng hồ đếm ngược như bản gốc, đặt khối tự do + chạm để gỡ, Khiên → Đóng băng, popup Hết giờ, cheat đồng hồ |
| 16:40 (09/10) | Rung nhẹ hơn + thêm kiểu rung | 7 kiểu mới (Selection, Soft, Rigid, Warning, Tick, Rise, Thud); Android dùng primitive / mẫu có sẵn của hãng trước, iOS dùng Soft / Rigid / Selection / Warning |
| 16:55 (09/10) | Rung khi kéo sang ô khác + khi coin bay vào | Ngón tay kéo sang ô khác → tick nhẹ (kể cả khi vùng chọn bị chặn); mỗi đồng coin chạm ô coin ở màn thắng → tick nhẹ (kiểu `Coin`) |
| 17:05 (09/10) | Rung khi bấm nút | Mọi Button trong Canvas có `UIButtonSounds` rung nhẹ (kiểu `Button`), trừ nút booster |
| 17:15 (09/10) | Level Editor: Mạng → Thời gian | Ô "Thời gian (s)" thay ô "Mạng"; 0 = tự tính (hiện ước lượng), > 0 = số giây cố định của level |

---

## 16:05 (09/10) – Đồng hồ thay 3 mạng

Yêu cầu: có người nhận xét cơ chế tim không hay. Ở nhánh `Old-Core` quay về cơ chế timer của bản gốc; Khiên dùng để đóng băng thời gian; sửa cheat; ấn vào 1 ô để hủy khối đã đặt; không còn tim (mạng trong màn). Năng lượng ngoài sảnh (tim ở Main Menu) giữ nguyên vì bản gốc cũng có.

### Luật mới

- Thời gian mỗi level = 8 s + 0.25 s/ô + random 5.5–6.5 s/khối (mỗi ô số 1 khối), tối thiểu 45 s (số của `GameplayController` gốc, chỉnh ở `Configs/LivesEnergyConfig.asset`).
- Đồng hồ chỉ chạy khi người chơi thao tác được: ván `Playing`, lưới đã hiện, không có intro / popup / booster tự đặt khối (`InputGate` mở), không ở Menu, không phải màn tutorial.
- Vùng hợp luật (`BoardModel.Check` = Ready) là đặt luôn, **không hỏi solver**, không phạt. Đặt sai thì chạm vào khối (thả tay trên chính khối đó) để gỡ.
- Lưới kín: mọi khối đúng số ô → thắng. Khối ô `?` sai số ô (Check không xét số ô của `?`) → mèo sai rung + viền đỏ, không thắng, chạm để gỡ.
- Ổ khóa giảm 1 sau mỗi khối đặt; gỡ khối không khóa lại. Số `?` đã lộ giữ nguyên.
- Hết giờ → `GameState.OutOfTime` → popup **TIME'S UP!**: xem ads +30 s / 900 coin +60 s (chơi tiếp đúng lưới dở), X → Thua (−1 năng lượng).
- Booster **Đóng băng** (thay Khiên, cùng nút / icon `btn_freze`): dừng đồng hồ 20 s, lớp băng phủ đồng hồ; hết 20 s băng vỡ.
- Hint / Reveal / Thắng ngay: lời giải khớp các khối đã đặt; có khối sai làm lưới hết lời giải thì lấy lời giải từ lưới trống và gỡ khối sai đang đè lên chỗ đặt.

### Thêm / Đổi tên (giữ `.meta` nên scene không mất tham chiếu)

| Cũ | Mới | Nội dung |
| --- | --- | --- |
| `Gameplay/Lives/LivesManager` | `Gameplay/Timer/LevelTimer` | Đồng hồ: `Total`, `Remaining`, `Freeze(s)`, `AddTime(s)`, `SetRemaining`, `DebugPaused`; event `onTimeChanged`, `onTimeAdded`, `onTimeWarning` (dưới 10 s), `onTimeUp`, `onFreezeChanged`, `onFreezeEnded` |
| `UI/HUD/LivesUI` | `UI/HUD/TimerUI` | Icon `btn_clock` + "m:ss" dưới chữ LEVEL; dưới 10 s chữ đỏ + icon đập mỗi giây; đóng băng chữ xanh băng; cộng giờ icon nảy |
| `UI/HUD/ShieldIndicator` | `UI/HUD/FreezeIndicator` | Hiệu ứng băng cũ, nay phủ đồng hồ: bật khi đóng băng, vỡ khi hết hạn |
| `Booster/Effects/ShieldBooster` | `Booster/Effects/FreezeBooster` | Gọi `LevelTimer.Freeze(freezeSeconds)` |
| `Booster/ShieldTutorialDemo` | `Booster/FreezeTutorialDemo` | Sau tutorial mở khóa: "The timer is frozen for 20 seconds - take your time!" 2.5 s |
| `UI/Popups/ReviveLivesPopup` | `UI/Popups/MoreTimePopup` | Popup Hết giờ, thêm `adsLabel` / `coinLabel` ("+30s" / "+60s") |
| `BoosterType.Shield` | `BoosterType.Freeze` | Giá trị số giữ 1 (asset không đổi) |
| `GameState.OutOfLives` | `GameState.OutOfTime` | |
| `SoundId.OutOfLives / ShieldOn / ShieldBlock` | `OutOfTime / FreezeOn / FreezeEnd` | Thêm `TimeWarning` (`warningEndTime.ogg`), `BlockRemove` (`poof_star.ogg`) |

### Sửa

| File | Nội dung |
| --- | --- |
| `Core/BoardModel` | Bỏ `IsSolvableWith`; thêm `TrySolveHint`, `GetBlockAt`, `GetBlocksOverlapping`, `Remove`, `IsSolved(wrong)` |
| `Core/GridManager` | Thả tay = đặt (không solver); `TryRemoveBlockAt`, event `onBlockRemoved`; lưới kín mà sai → `PlayWrong` + `onMistake`; `PlaceSolved` gỡ khối sai đè lên; `IsGameOver` = hết giờ |
| `Block/PlacedBlockView` | Bỏ `PlayReject` / `RejectStyle` / mèo xám; thêm `PlayRemove` (co + mờ) và `PlayWrong` (rung + viền đỏ, khối ở lại) |
| `Input/DragAndDropController` | Chạm xuống khối đã đặt → chờ thả tay trên chính khối đó → gỡ; kéo sang chỗ khác thì thôi |
| `Level/LevelManager` | Nghe `LevelTimer.onTimeUp`; `Revive(seconds)` cộng giờ; tutorial đặt `LevelTimer.IsPractice` |
| `Config/LivesEnergyConfig` (+ asset) | Bỏ `maxLives`; thêm `baseSeconds`, `secondsPerCell`, `secondsPerBlockMin/Max`, `minSeconds`, `warningSeconds`, `freezeSeconds` |
| `Config/EconomyConfig` (+ asset) | `reviveAdsLives / reviveCoinLives / reviveCoinCost` → `moreTimeAdsSeconds` 30 / `moreTimeCoinSeconds` 60 / `moreTimeCoinCost` 900 |
| `BoosterDatabase.asset` (cả 2 bản) | Booster 1: tên `Freeze`, mô tả "Freeze the timer for 20 seconds!" |
| `Booster/HintBooster`, `RevealBooster` | Dùng `TrySolveHint`; Reveal chọn được cả ô số đang bị khối sai đè |
| `Booster/BoosterManager`, `BoosterEffect` | `IBoosterHost.Lives` → `Timer` |
| `Tutorial/TutorialController` | Gỡ khối thì bàn tay chỉ lại; bỏ qua gợi ý khi ô số đã có đúng khối gợi ý |
| `Audio/GameAudioEvents`, `Feedback/GameHapticEvents` | Âm / rung gỡ khối, sắp hết giờ, hết giờ, đóng băng |
| `Analytics/LevelStatsRecorder`, `Save/SaveData`, `Editor/Level/LevelStatsExporter` | `outOfLives` → `outOfTime`; "nước sai" = khối bị gỡ; cột CSV `out_of_time_per_attempt` |
| `Cheats/DebugCheats`, `CheatPanel`, `Editor/Debug/DebugPanelWindow` | Bỏ cheat mạng / khiên. Thêm: trạng thái đồng hồ, −10s / +10s / +60s / Đầy giờ, Dừng / Chạy đồng hồ, Đóng băng, Hết giờ, Thua hẳn (qua `OutOfTime`); Thắng ngay / Đặt 1 khối đúng chạy được cả khi lưới có khối sai |
| `Editor/UI/BoosterBuilder` | Menu mới **Tools/PawBlock/Builders/Build Timer HUD**: dựng `Timer` (thay `Hearts`), giữ lớp băng cũ (đổi tên `Shield` → `Freeze`, không mất bông tuyết / viền băng gán tay) |
| `Editor/UI/PopupBuilder` | `BuildMoreTime` thay `BuildRevive` |
| `Editor/UI/TutorialBuilder`, `ShopBuilder`, `DailyRewardBuilder`, `Economy/ShopDatabase` | Đổi sang `BoosterType.Freeze` (field `shield` trong ShopDatabase giữ tên để không mất số) |
| `Editor/Audio/AudioSetup` (+ `AudioDatabase.asset`) | Thêm 2 âm mới (chạy Setup Audio, âm cũ giữ nguyên) |
| Scene | Chạy Build Timer HUD; popup Hết giờ sửa tại chỗ (tiêu đề TIME'S UP!, icon đồng hồ, "+30s" / "+60s"); `GameFlowUI.revivePopup` → `moreTimePopup` (`FormerlySerializedAs`) |

### Tác dụng

- Áp lực thời gian như bản gốc thay cho phạt mạng; đặt sai không bị phạt ngay mà mất thời gian gỡ.
- Không còn hỏi solver mỗi lần thả tay (nhẹ hơn trên lưới lớn).
- Code chạm `LivesManager` / tim không còn; scene giữ nguyên tham chiếu nhờ đổi tên kèm `.meta`.

### Kiểm tra

Play mode (Level 5, 12, 13):

- Level 12 (7×7, 13 khối): tổng 94 s; đồng hồ đứng khi popup cơ chế mở, chạy khi chơi.
- Kéo 1 vùng hợp luật nhưng sai lời giải: khối được đặt, không phạt, ổ khóa 2 → 1. Chạm vào khối qua `DragAndDropController` → khối bị gỡ.
- Có khối sai: Hint đặt 1 khối đúng (trừ 1 lượt); Thắng ngay gỡ khối sai rồi thắng.
- Đóng băng: lượt 3 → 2, lớp băng phủ đồng hồ, giờ đứng yên 20 s.
- Hết giờ (cheat): popup TIME'S UP! → +30s → chơi tiếp, giờ 0 → 30. Thua hẳn → popup Thua.
- Console không lỗi / cảnh báo.

Chưa thử trên máy thật. Chưa có level ô `?` để thử trường hợp lưới kín mà sai số ô.

### Lưu ý

- Save cũ lưu booster theo tên: lượt / trạng thái mở khóa của "Shield" không chuyển sang "Freeze" (booster Đóng băng sẽ tặng lượt và chạy tutorial mở khóa lại).
- Docs trong `Features/` (02, 05, 01) vẫn mô tả cơ chế mạng của nhánh chính.

---

## 16:40 (09/10) – Rung nhẹ hơn + thêm kiểu rung

Yêu cầu: game rung khá nặng; thử các kiểu rung hợp với game (đã liệt kê trước đó).

**Sửa**

- `Gameplay/Feedback/Haptics.cs`: thêm `HapticType` Selection, Soft, Rigid, Warning, Tick, Rise, Thud (giá trị 5–11, cũ giữ 0–4). Android chọn cách tốt nhất máy hỗ trợ: Composition primitive (API 30+: CLICK, TICK, LOW_TICK, THUD, QUICK/SLOW_RISE, QUICK_FALL) → mẫu có sẵn `createPredefined` (API 29+) → OneShot / Waveform tự chỉnh (nhẹ hơn trước: Light 18 ms, Medium 28 ms, Heavy 45 ms). Selection / Tick quá nhẹ nên máy không có primitive / predefined thì bỏ qua. `LastMethod` cho biết lần rung cuối dùng cách nào.
- `Plugins/iOS/HapticsIOS.mm`: Impact có mức mạnh (iOS 13+), thêm Soft, Rigid, Selection, Warning.
- `Feedback/GameHapticEvents.cs`: kéo qua ô → Selection; đặt khối → Light; gỡ khối → Soft; mở khóa ô → Rigid; booster → Medium (trừ Đóng băng); đóng băng bật → Rise, băng vỡ → Thud; xuống dưới 10 s → Warning, sau đó mỗi giây → Tick; lưới kín mà sai → Heavy; thắng / thua → Success / Failure. Inspector tắt riêng được rung kéo ô và tick giây.
- `Core/GridManager.cs`: event `onSelectionChanged(int)` khi vùng kéo đổi.
- `Timer/LevelTimer.cs`: event `onLowTick(int)` mỗi giây khi dưới ngưỡng cảnh báo.
- `Cheats/CheatPanel.cs`: nút thử 12 kiểu rung xếp 3 nút / hàng, hiện cách rung lần cuối.

**Kiểm tra**

- Editor biên dịch không lỗi; biên dịch script cho Android (`PlayerBuildInterface.CompilePlayerScripts`) không lỗi.
- Chưa thử trên máy thật (gọi JNI chỉ kiểm tra được trên điện thoại).

---

## 16:55 (09/10) – Rung khi kéo sang ô khác + khi coin bay vào

Yêu cầu: rung nhẹ khi đang kéo mà ngón tay chạm sang ô khác; khi nhận coin, mỗi đồng coin bay lên chạm ô coin (+1) thì rung nhẹ.

**Sửa**

- `Core/GridManager.cs`: `onSelectionChanged(int)` → `onDragCellChanged(Vector2Int)`, phát mỗi khi ô dưới ngón tay đổi (trước chỉ phát khi vùng chọn giãn / co, kéo vào khối đã đặt / ô Void thì không rung).
- `Feedback/GameHapticEvents.cs`: nghe `onDragCellChanged` → `Selection`.
- `UI/Popups/WinPopup.cs`: mỗi đồng coin chạm ô coin → `AudioManager.Vibrate(HapticType.Coin)` (cùng lúc với âm coin). Coin chạm cách nhau 60 ms nên tách được từng nhịp.
- `Feedback/Haptics.cs`: thêm `HapticType.Coin` (primitive TICK 0.55 → EFFECT_TICK → 12 ms). Selection / Tick / Coin có rung dự phòng 10–12 ms trên máy chỉnh được cường độ; máy không chỉnh được thì bỏ qua (tránh rè).
- `Plugins/iOS/HapticsIOS.mm`: Coin = Impact Light mức 0.45.

**Kiểm tra**

- Editor biên dịch không lỗi; `Haptics.cs` biên dịch riêng với `UNITY_ANDROID` (csc của Unity) không lỗi. Biên dịch player Android qua `CompilePlayerScripts` bị Bee ngắt ("Tundra build interrupted"), không phải lỗi code.
- Chưa thử trên máy thật.

---

## 17:05 (09/10) – Rung khi bấm nút

Yêu cầu: thêm rung cho các button.

**Sửa**

- `UI/Common/UIButtonSounds.cs`: ngoài âm Click, mọi Button trong Canvas (kể cả đang tắt lúc mở game) gọi `AudioManager.Vibrate(HapticType.Button)` khi bấm. Bỏ qua nút booster (đã rung Medium khi booster có tác dụng; rung 2 lần trong 30 ms thì lần sau bị nuốt). Tắt bằng ô `vibrate` trong Inspector. `NoClickSound` chỉ tắt âm, vẫn rung.
- `Feedback/Haptics.cs`: thêm `HapticType.Button` (primitive CLICK 0.35 → EFFECT_TICK → 15 ms mức 70).
- `Plugins/iOS/HapticsIOS.mm`: Button = Impact Light mức 0.5.

**Kiểm tra**

- Editor biên dịch không lỗi; `Haptics.cs` biên dịch riêng với `UNITY_ANDROID` không lỗi. Chưa thử trên máy thật.

---

## 17:15 (09/10) – Level Editor: Mạng → Thời gian

Yêu cầu: trong Level Editor thay mạng bằng thời gian.

**Sửa**

- `Level/LevelDefinition.cs`: `LevelRules.mistakeLimit` → `timeLimit` (giây, 0 = tự tính). Đặt tên mới, không `FormerlySerializedAs`, để `mistakeLimit: 3` cũ của 337 level không thành 3 giây; mọi level cũ đọc ra 0 = tự tính như trước. Dòng `mistakeLimit` thừa trong file `.asset` tự mất khi level được lưu lại.
- `Timer/LevelTimer.cs`: `ComputeSeconds` dùng `rules.timeLimit` nếu > 0; thêm `EstimateAutoSeconds` (lấy giữa 5.5–6.5 s/khối) cho editor.
- `Editor/Level/LevelEditorWindow.cs`: hàng Kích thước: ô "Mạng" → "Thời gian (s)" + chữ bên cạnh "m:ss" hoặc "tự động ~m:ss" (cập nhật theo số ô / số khối đang sửa). Level mới tạo: `timeLimit` 0.
- `Editor/Level/ShikaCatLevelImporter.cs`: nhập level mới với `timeLimit` 0.

**Kiểm tra**

- Biên dịch không lỗi; Level_Custom 4x4: 0 → tự động ~45 s; Level 12 (7×7, 13 khối): ~93 s (khớp 94 s đo trong Play mode lúc trước, phần lẻ do random).
