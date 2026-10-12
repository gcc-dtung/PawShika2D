# 18 – Game feel: kéo và đặt khối

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `c3b16a4` "Game feel kéo/đặt, Đóng băng gốc, Sao, Daily Challenge, Vòng quay" (nhánh `Day-7`) |
| Thời gian push | 2026-10-12 11:36 |
| Cập nhật lần cuối | 2026-10-12 11:36 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 20:35 (10/10) | Bong bóng số khi kéo mượt hơn | Hiện ra phóng to từ 0 (nảy nhẹ), thả tay thu nhỏ về 0 rồi mới tắt, số đổi thì chữ nảy lên; không còn bật / tắt cái rụp |
| 20:55 (10/10) | Game feel kéo / đặt khối (4 ý ưu tiên) | Bong bóng đổi màu theo trạng thái (tím / xanh đủ / đỏ sai); tick cao dần theo số ô + "ding" khi đủ; khối đặt xuống dẹt rồi bật (squash & stretch, đáy đứng yên); ô trống quanh khối nảy lan như sóng |
| 23:10 (10/10) | Booster chậm lại; đóng băng kiểu băng viền màn hình | Hiệu ứng booster 0.6 → 1.2 s, khóa thao tác (đồng hồ dừng) trong lúc bay; tấm băng vuông phủ lưới → băng bám viền màn hình kiểu Fruit Ninja (cạnh trái / phải mỏng, góc dày), lan vào khi bật, vỡ ra khi hết |
| 23:00 (10/10) | Phản hồi khi dùng booster; Hint chỉ gợi ý (tách khỏi Reveal) | Icon booster bay ra giữa màn hình (tia sáng, chữ tên, tối nhẹ) rồi lao tới mục tiêu; Hint không đặt hộ nữa mà hiện mèo mờ + bàn tay kéo ở vùng đúng. 53 test xanh |
| 22:50 (10/10) | Chạm màn hình mới bắt đầu tính giờ | Level mới / chơi lại: đồng hồ đứng ở tổng thời gian, chữ giờ nhấp nháy; chạm màn hình lần đầu (sau intro, không có popup) mới chạy |
| 22:20 (10/10) | Ô LEVEL nền trắng ở khung Hard / Very Hard; DOTween nới sức chứa | Nền bo góc (sprite trắng) giờ được tô tím đậm như nền thường, chữ "LEVEL n" trắng hiện lại (lần đầu thử đổi màu chữ, người dùng báo khung vẫn trắng → bỏ, tô nền); khởi tạo DOTween 500 tween / 150 sequence lúc mở game |
| 22:05 (10/10) | Ô "?" không lộ số | Đặt khối bất kỳ lên "?" trước đây lộ luôn số thật (lộ đáp án); giờ "?" giữ ẩn cả trên khối mèo, số thật chỉ dùng để chấm. 49 test xanh |
| 21:50 (10/10) | Toàn bộ game feel còn lại (14 ý) + tài liệu | Ô số sáng / lắc khi kéo, bong bóng bám mượt, ô "dính", khung co xám khi thả sai; hạt bụi, mèo phản ứng; combo; thanh tiến độ; slow-motion khối cuối; viền đỏ + nhạc nhanh khi sắp hết giờ; "+30s" bay; tấm băng + mèo run; gỡ khối nhảy rồi rơi. 46 test xanh. `Docs/Features/14_GameFeel.md` |
| 21:20 (10/10) | Test cho game feel (thay cách kiểm tra bằng `Goto`) | Công thức tách ra `FeelCurves` (hàm thuần); 31 test: 28 EditMode (công thức + `SelectionHint` qua `BoardModel.Check`) + 3 chạy Play mode thật từng frame (sóng ô, tắt ô giữa chừng, squash khối) |
| 06:10 (11/10) | Sao theo thời gian (như bản gốc) | Thanh thời gian 3 mốc sao 60 / 20 / 0 % dưới khung LEVEL (thay thanh % ô), tụt qua mốc thì sao vàng tan + "bụp"; màn thắng có tiêu đề ảnh Perfect / Great / Nice / Well done theo số sao + 3 sao đập xuống; lưu sao cao nhất mỗi level. 15 test mới xanh |
| 06:10 (11/10) | Asset gốc chưa dùng: việc nhỏ | Hoa văn chân mèo trên nền màn chơi; khung `khung_reveal` trên từng ô của vùng Hint; tutorial booster thành đèn rọi `CircleMask`; icon booster to trong gói Shop / quà; coin bay lật quanh trục Y + âm `earrn_reward`; thêm tiếng mèo Sly / Lying / Dumb |
| 06:35 (11/10) | Daily Challenge + Level Generator | Tab thứ 3 "Daily" (thay khung tạm Progress): lịch tháng, level 10 x 10 sinh theo seed ngày, chơi bù ngày lỡ bằng quảng cáo, quà mốc 7 / 14 / 21 ngày nhập từ `DailyChallengeData.txt` gốc; cửa sổ Level Generator sinh hàng loạt level đúng 1 lời giải. 83 test xanh |
| 07:30 (11/10) | Gỡ khối thì ổ khóa đóng lại | Khóa = số bước gốc − số khối khác đang trên lưới: khóa đã mở mà chạm gỡ 1 khối thì khóa lại 1 bước (trước đây giữ nguyên mở). 5 test mới xanh |
| 08:10 (11/10) | Băng HARD / EXTREME LEVEL rơi xuống rồi bay ngược lên | Thay trượt chéo từ góc trên trái: băng rơi thẳng từ trên xuống (nảy quá 1 chút), lúc ra chùng xuống rồi vọt lên khỏi mép trên, như video bản gốc |
| 09:00 (12/10) | Cảnh báo Spine `Noti.png` | Tắt `Alpha Is Transparency` trên ảnh premultiplied alpha (atlas `pma:true`), hết cảnh báo |
| 11:30 (12/10) | Coding Guidelines + Refactor Plan | Bộ quy tắc Unity dùng chung mọi dự án (`Refactor/UnityCodingGuidelines.md`) và kế hoạch refactor PawBlock 10 giai đoạn (`Refactor/RefactorPlan.md`); chưa đổi code |

---

## 20:35 (10/10) – Bong bóng số khi kéo mượt hơn

Yêu cầu: số hiện lúc đang kéo cần feel ổn hơn; lúc bắt đầu xuất hiện hoặc lúc mất đi thì phóng to / thu nhỏ, tránh cảm giác giật.

**Sửa**

- `Gameplay/Input/DragCountBubble.cs` (DOTween):
  - Hiện: scale 0 → scale gốc (lấy từ prefab / scene lúc hiện lần đầu) trong `showDuration` 0.2 s, `Ease.OutBack` (nảy nhẹ). Số đầu tiên đi cùng hiệu ứng này, không nảy thêm.
  - Ẩn: thu về 0 trong `hideDuration` 0.14 s, `Ease.InBack`, xong mới `SetActive(false)`; trong lúc thu vẫn giữ số cũ.
  - Đang thu nhỏ dở mà kéo lại thì phóng to tiếp từ cỡ hiện tại (không nhảy về 0).
  - Số ô đổi khi đang kéo: chữ nảy to thêm `countPunch` 20% trong 0.16 s (nền đứng yên). Đổi số liên tục thì nảy lại từ cỡ gốc, không cộng dồn.
  - Bị tắt giữa chừng (`OnDisable`): dừng tween, trả cỡ chữ, lần sau hiện lại từ đầu.
  - Các số trên chỉnh trong Inspector (nhóm "Hiệu ứng").

**Kiểm tra**

- Unity biên dịch lại, Console không lỗi / cảnh báo.
- Chưa thử trong Play mode / máy thật.

---

## 20:55 (10/10) – Game feel kéo / đặt khối

Yêu cầu: triển khai dần các ý cải thiện game feel trong gameplay; làm trước 4 ý ưu tiên (đổi màu bong bóng, tick cao dần, squash & stretch, ô xung quanh dội).

**Thêm**

- `Core/GridManager.cs`: enum `SelectionHint` (Neutral / Ready / Invalid), property `SelectionHint`, event `onSelectionChanged(int count, SelectionHint)` mỗi khi vùng kéo đổi; hết kéo (thả tay / hủy) phát `(0, Neutral)`. Invalid chỉ khi chắc chắn sai: thừa ô, ≥ 2 ô số, ô số khóa, vùng chứa ô bị chặn, sai hướng mà đã đủ / thừa ô. Thiếu ô, chưa có ô số, ô "?" → Neutral (kéo tiếp còn có thể đúng).
- `Audio/SoundId.cs`: `DragTick` (40), `DragReady` (41). `Editor/Audio/AudioSetup.cs`: DragTick = `SC_SFX_UI_Button_01` (0.13 s, vol 0.45, cooldown 0.03), DragReady = `SC_SFX_Gameplay_Box_Created_01` (vol 0.6). Đã chạy Setup Audio (thêm 2 âm, âm cũ giữ nguyên).
- `Audio/AudioManager.cs`: `PlayPitched(id, pitch)` (nhân thêm cao độ).
- `Core/CellView.cs`: `PlayBump(delay, amount, duration)` – ô phồng rồi về (lên nhanh, xuống chậm), bỏ qua khi đang intro; hủy khi tắt / Unbind.

**Sửa**

- `Input/DragCountBubble.cs` + `DragAndDropController.cs`: `Show(pos, count, hint)`; màu nền chuyển mượt (0.12 s) giữa màu prefab (đang kéo) / `readyColor` xanh / `invalidColor` đỏ. Lần hiện đầu đặt màu ngay, không tween.
- `Audio/GameAudioEvents.cs`: nghe `onSelectionChanged` – vừa thành Ready → DragReady (cả khi chạm ô số 1); số ô đổi → DragTick với cao độ `dragBasePitch` 0.9 × 2^(nửa cung/12), nửa cung theo ngũ cung `{0, 2, 4, 7, 9, 12, 14, 16}` (quá mảng thì giữ nốt cuối; co lại thì thấp xuống). Không tick lúc mới chạm. Tắt bằng ô `dragSounds`.
- `Block/PlacedBlockView.cs`: `PopIn` (phóng đều 8%) → squash & stretch: `d = squashAmount · (1-p)² · sin(p·π·squashWobbles)`, scale (1+d, 1-d), tâm hạ theo chiều cao để đáy khối đứng yên. Mặc định 0.34 s / 0.12 / 2.5 nửa nhịp. Bỏ field `popDuration`, `popScale`.
- `Feedback/GameplayFeedback.cs`: `PlayRipple` khi đặt khối – ô trống cách khối 1..`rippleRadius` (2) vòng (tính cả chéo) nảy `rippleAmount` 6% × giảm dần theo vòng, trễ 0.04 s + 0.05 s / vòng, 0.22 s. Tắt bằng ô `ripple`.

**Tác dụng**

- Nhìn bong bóng là biết vùng đang đủ / sai, không phải dò mèo xem trước.
- Kéo có nhịp âm thanh; nghe "ding" là biết thả tay được.
- Đặt khối có trọng lượng (đáp xuống), lưới phản hồi quanh chỗ đặt.

**Kiểm tra** (Play mode, Level 36 – 7×7)

- Kéo (0,6) → (1,6) → (2,6) → (3,6) → về (2,6) ở ô số 3: hint Neutral → Ready → Invalid → Ready; bong bóng tím → xanh → đỏ → xanh (ảnh chụp đúng màu); tick ô thứ 2 cao độ 1.01; vào Ready phát Box_Created.
- Thả tay: khối đặt; ô (1,5) vòng 1 phồng tới 1.060, ô (1,4) vòng 2 trễ hơn, nhỏ hơn.
- Squash khối 3×4 (tua tween): dẹt 1.077 / 0.923 ở 0.07 s → dài 0.979 / 1.021 ở 0.17 s → về 1; đáy không đổi.
- Console không lỗi. Có log DOTween tăng capacity Sequence 50 → 125 (từ ~49 Sequence intro lưới 7×7, có từ trước).
- Chưa nghe thử thực tế / chưa thử trên máy thật.

---

## 21:20 (10/10) – Test cho game feel

Yêu cầu: lần trước kiểm tra squash bằng `Tween.Goto` qua lệnh tạm (không lưu, gọi `Confirm()` lần 2 lên khối đã đặt, đọc field private bằng reflection, không xét delay / frame thật). Thay bằng test lưu trong repo, chạy lại được.

**Thêm**

- `Gameplay/Feedback/FeelCurves.cs`: hàm thuần `Squash`, `BottomAnchorOffset`, `Bump`, `RingDistance`, `RippleAmount`, `RippleDelay`, `DragPitch`. View chỉ gọi các hàm này trong tween.
- `Editor/Tests/FeelCurvesTests.cs` (EditMode): squash đứng yên ở p = 0 / 1, dẹt trước rồi dài, không vượt biên độ, ngang + dọc = 2, biên độ tắt dần, kẹp p; đáy đứng yên; ô nảy về 1 và đỉnh ở giữa; vòng ô (cả chéo), độ nảy / độ trễ theo vòng; cao độ tick (ô 1 = gốc, 12 nửa cung = gấp đôi, tăng dần, quá mảng giữ nốt cuối, mảng rỗng).
- `Editor/Tests/SelectionHintTests.cs` (EditMode, qua `BoardModel.Check` như lúc chơi): thiếu / đủ / thừa ô, ô số 1, chưa có ô số, 2 ô số, ô khóa, ô Void, sai hướng chưa đủ / đã đủ ô, ô "?" không bao giờ đỏ vì số ô.
- `Editor/Tests/FeelPlayModeTests.cs`: `EnterPlayMode` trong scene trống riêng (trả lại scene cũ khi xong; scene chưa lưu thì bỏ qua test để không mất thay đổi), `Time.captureDeltaTime` = 1/60 nên mỗi frame đúng 1/60 s kể cả khi editor chậm. Sóng ô: vòng 2 nảy trễ hơn đúng khoảng `stepDelay` (± 2 frame), nhẹ hơn, không co dưới 1, về đúng 1. Tắt ô giữa lúc nảy → về 1, tween cũ không chạy tiếp. Squash: dựng `PlacedBlock.prefab` thật, `Show()` → nhịp đầu dẹt, có nhịp dài, không vượt `squashAmount`, đáy thân (`body.bounds.min.y`) lệch < 0.005, cuối về scale 1 và đúng vị trí.

**Sửa**

- `PlacedBlockView.PopIn`, `CellView.PlayBump`, `GameplayFeedback.PlayRipple`, `GameAudioEvents` dùng `FeelCurves` (kết quả như cũ).
- `SelectionHint` chuyển sang `Core/BoardModel.cs`, cách tính thành property `PlacementCheck.Hint` (trước là hàm private trong GridManager).
- Scene: lưu lại (chỉ thêm giá trị mặc định của các field mới: `ripple…`, `dragSounds…`, `onSelectionChanged`…; đã so bản sao trước khi lưu).

**Ghi chú**

- Script game chưa có asmdef (nằm trong Assembly-CSharp) nên test đặt ở `Editor/Tests` (Assembly-CSharp-Editor), chạy trong Test Runner → **EditMode**; test Play mode vẫn nằm ở tab EditMode.

**Kiểm tra**

- Test Runner EditMode: 31/31 qua (~7–9 s).
- Thử cố tình đảo dẹt ↔ dài trong `FeelCurves.Squash`: `Squash_DetTruocRoiMoiDai` và test Play mode squash đều đỏ ("Nhịp đầu phải dẹt"); trả lại công thức → 31/31 xanh.
- Sau khi chạy: scene về SampleScene, không dirty, thoát Play mode, `captureDeltaTime` = 0; Console không lỗi.

---

## 21:50 (10/10) – Toàn bộ game feel còn lại

Yêu cầu: triển khai hết các ý game feel đã đề xuất, rồi xuất 1 file md cho những feel đã làm.

Tài liệu đầy đủ (từng hiệu ứng, số mặc định, field chỉnh, test): **[Docs/Features/14_GameFeel.md](../Features/14_GameFeel.md)**.

**Thêm**

- `Feedback/DragFeedback.cs`: ô số trong vùng kéo phóng to; phủ ≥ 2 ô số thì lắc.
- `Feedback/BlockFeedback.cs`: hạt bụi khi đặt; mèo kề bên nhảy + đổi mặt; gỡ khối thì ô bên dưới nảy; đóng băng: tấm băng phủ lưới + mèo run lần lượt.
- `Feedback/ProceduralSprites.cs`: sprite vẽ bằng code (chấm tròn, viên thuốc 9-slice, viền vignette, tấm băng 9-slice, ô trắng).
- `UI/HUD/LowTimeVignette.cs` (trên `Canvas_HUD`): viền đỏ đập theo giây khi dưới 10 s.
- `UI/HUD/LevelProgressBar.cs` (trên `LevelFrame`): thanh % ô đã lấp.
- `FeelCurves`: `StickyCell`, `ComboPitch`, `PraiseTier`; class `ComboCounter`.
- `SoundId.DragCancel` (42) = `SC_SFX_UI_Popup_Close_01` (chạy Setup Audio).

**Sửa**

- `GridManager`: ô "dính" (`cellStick` 0.15), `SelectionRect`, `Combo` (`comboWindow` 2.5 s, gỡ khối / lưới mới thì đứt), `TryGetView`; thả tay ở vùng sai gọi `SelectionBoxView.HideRejected`.
- `SelectionBoxView.HideRejected`: khung xám, co 82 %, mờ dần 0.24 s rồi tắt; kéo lượt mới thì hết xám.
- `DragCountBubble`: bám ngón tay mượt (`followSharpness` 22).
- `CellView`: `SetHighlighted`, `PlayConflictShake`.
- `PlacedBlockView`: `PlayRemove` nhảy lên rồi rơi (0.34 s); `PlayNudge`, `PlayShiver`, `ReactFace`; mèo xem trước thành khối thì đổi biểu cảm.
- `GameplayFeedback`: chữ khen theo combo (lẻ Nice / Like → Great / Wow → Beauty / Amazing → Perfect, to thêm 12 % / bậc); khối cuối: slow-motion 0.35 → 1 trong 0.6 s + zoom 4 %.
- `GameAudioEvents`: âm đặt khối cao dần theo combo; "bụp" khi thả sai; nhạc × 1.08 khi sắp hết giờ (`AudioManager.SetMusicPitch`).
- `GameHapticEvents`: thả sai → rung Soft.
- `TimerUI`: cộng giờ → "+Ns" bật giữa màn hình rồi bay vào đồng hồ.
- Scene: gắn `DragFeedback` + `BlockFeedback` (Manager), `LowTimeVignette` (Canvas_HUD), `LevelProgressBar` (LevelFrame); scene sạch trước khi lưu.

**Sửa sau khi thử trong Play mode**

- Lời khen khi đặt lẻ ra cả "Perfect" (bậc 0 chọn ngẫu nhiên mọi chữ) → bậc 0 chỉ Nice / Like.
- Tint đóng băng là chữ nhật cạnh cứng → tấm băng 9-slice bo góc, giữa nhạt, mép đậm.
- Viền đỏ quá dày / đậm, lấn gần hết màn hình → chỉ còn sát mép, nền 0.14, đỉnh 0.4.
- Viền đỏ / nhạc không bật khi cheat nhảy thẳng qua ngưỡng (`onTimeWarning` chỉ phát khi đồng hồ tự đếm qua) → nghe thêm `onTimeChanged`; `SetMusicPitch` bỏ qua khi gọi lặp cùng đích.

**Kiểm tra**

- Play mode (Level 36 – 7×7):
  - Kéo phủ 3 ô số → 3 số × 1.22, bong bóng đỏ; thả tay → khung xám co 2.2 → 1.80 rồi tắt, số về × 1.
  - Đặt 3 khối liên tiếp → combo 1 / 2 / 3, chữ khen to dần, 12 hạt bụi mỗi lần; thanh tiến độ 8 / 49.
  - Gỡ khối → combo về 0.
  - Đóng băng → tấm băng phủ lưới.
  - 9 s không băng → viền đỏ bật, nhạc 1.08.
  - Revive +30s → chữ "+30s" tạo ra, giờ lên 0:28.
  - Đặt hết lời giải → timeScale 0.35 rồi về 1.00, camera về đúng cỡ, thanh đầy.
- Test Runner EditMode: **46/46 qua** (31 `FeelCurvesTests` + 8 `SelectionHintTests` + 7 `FeelPlayModeTests`). Console không lỗi.
- `ProjectSettings.asset` hiện "M" trong git nhưng chỉ khác ký tự xuống dòng (Unity ghi lại), nội dung không đổi.
- Chưa nghe thử âm thanh / chưa thử trên máy thật.

---

## 22:05 (10/10) – Ô "?" không lộ số

Yêu cầu: ô "?" không cần hiện ra là số bao nhiêu; hiện tại match 1 vùng bất kỳ vào "?" thì luôn hiện số thật.

Nguyên nhân: `BoardModel.Place` luôn gọi `RevealClue()` cho ô số của khối. `Check` không xét số ô của "?" (nhánh đồng hồ cũng không hỏi solver), nên khối sai số ô vẫn đặt được và lộ số thật = lộ đáp án.

**Sửa**

- `Core/BoardModel.cs`: `Place` không lộ số nữa; "?" giữ ẩn suốt ván (kể cả gỡ khối). Số thật vẫn dùng ở `IsSolved` (lưới kín mà khối "?" sai số ô → mèo rung đỏ như cũ).
- Xóa `ClueCell.Reveal()`, `CellModel.RevealClue()` (không còn chỗ dùng).
- `PlacedBlockView.Confirm`: sửa chú thích (khối mèo trên "?" hiện "?").
- Docs: `README.md` (bảng cơ chế), `TaskBreakdown.md`, `Features/01`, `Features/03`, `Features/HowTo_ThemMechanic.md` bỏ mô tả "đặt đúng thì lộ số".

**Thêm**

- `Editor/Tests/HiddenClueTests.cs`: đặt sai số ô → vẫn ẩn, không phát `Changed`, `IsSolved` báo sai; đặt đúng → vẫn ẩn, thắng; gỡ khối → vẫn ẩn.

**Kiểm tra**

- Test Runner EditMode: 49/49 qua.
- Play mode Level 36: ô "?" ở (3,2) số thật 6, ràng buộc dọc. Kéo dọc 2 ô → Ready, thả tay → khối đặt, ô và khối mèo đều hiện "?" (ảnh chụp). Kéo ngang → WrongShape (đúng luật dọc).

---

## 22:20 (10/10) – Ô LEVEL ở khung Hard / Very Hard; sức chứa DOTween

Yêu cầu: lỗi UI ở level Hard (có thể cả Very Hard) – ảnh: khung Hard không có chữ "LEVEL n", lưới 8 cột như tràn mép phải.

**Nguyên nhân**

- `LevelHUD.SetLevel`: mức khó đổi nền ô LEVEL sang `rounded_white` (để góc vuông không lòi ra khỏi khung) nhưng không tô màu → ô LEVEL thành nền trắng, chữ "LEVEL n" trắng biến mất. Có từ commit `6f48193`, không liên quan game feel.
- Lưới không tràn: đo trong Play mode, lưới 8 × 8 nằm từ x = 13 đến 454 trên màn 467 px, khớp vùng chơi (PlayZone); ảnh chụp bị cắt mép phải (thanh công cụ tab Game cũng cụt).

**Sửa**

- `UI/HUD/LevelHUD.cs`: thêm `roundedBackgroundColor` `(0.204, 0.204, 0.365)` (màu giữa của nền thường `khung1`, đo từ sprite); nền bo góc → tô màu này, level thường → trả màu gốc của Image. Chữ giữ trắng như level thường.
- Lần đầu sửa bằng cách đổi màu chữ theo mức khó (`DifficultyStyle.hudLevelTextColor`: Hard đỏ, Very Hard tím). Người dùng báo khung vẫn trắng → gỡ field đó, trả lại `DifficultyTheme.cs` và 2 asset `DifficultyTheme` như cũ (git checkout, chỉ có các dòng vừa thêm).
- `Gameplay/Util/TweenSetup.cs` (mới): `DOTween.Init().SetCapacity(500, 150)` trước khi scene nạp. Trước đó DOTween tự nới 200 / 50 → 500 / 125 giữa lúc chơi (log "Max Tweens reached", cấp phát lại có thể khựng 1 frame) do hiệu ứng mới (hạt bụi, sóng ô…).

**Kiểm tra** (Play mode)

- Level 37 (Hard) và Level 49 (Very Hard): nền `rounded_white` tô `(0.204, 0.204, 0.365)`, chữ trắng, viền đỏ / tím (ảnh chụp). Level 36 (thường): `khung1`, màu Image trắng như cũ. Trả lại đúng level đang chơi (save `currentLevel` = 36, tức Level 37).
- Xóa Console, vào Level 37, đặt 5 / 11 khối liên tiếp: không còn log DOTween.

---

## 22:50 (10/10) – Chạm màn hình mới bắt đầu tính giờ

Yêu cầu: khi bắt đầu level thì phải chạm vào màn hình, đồng hồ mới bắt đầu chạy.

**Sửa**

- `Timer/LevelTimer.cs`:
  - `waitForFirstTouch` (mặc định bật); `ResetTimer` (level mới / chơi lại) đặt trạng thái chờ, trừ màn luyện tập.
  - `Update`: khi đồng hồ đủ điều kiện chạy (qua intro, lưới đã hiện, không popup / InputGate) mà đang chờ, chỉ chạy tiếp khi `Pointer.current.press.wasPressedThisFrame` (Input System; chuột / ngón đầu tiên). Chạm trong lúc intro / popup không tính; chạm trúng nút (booster…) cũng tính.
  - Thêm `IsWaitingForTouch`, event `onWaitingForTouchChanged(bool)`, `StartNow()` (bỏ chờ).
  - `IsPractice = true` thì bỏ trạng thái chờ (tutorial không tính giờ, khỏi nhấp nháy).
  - Cộng giờ (Revive / cheat) không đặt lại trạng thái chờ.
- `UI/HUD/TimerUI.cs`: đang chờ chạm → chữ giờ + icon nhấp nháy alpha 1 ↔ 0.4, chu kỳ 1.1 s (thời gian thực; `waitingMinAlpha`, `waitingPeriod`); bắt đầu chạy → rõ hẳn lại.

**Kiểm tra** (Play mode, Level 37 Hard, 92 s)

- Vào màn: chờ chạm, 92.0. Sau 9 s (đã qua intro "HARD LEVEL", lưới mở thao tác) vẫn 92.0, chữ giờ nhấp nháy (ảnh chụp).
- Giả lập nhấn chuột (Input System `QueueStateEvent`) lúc 15.4 s → hết chờ; lúc 19.0 s còn 88.4 (chạy đúng 3.6 s), chữ rõ lại.
- Chơi lại (`Retry`) → chờ chạm lại, 90 / 90.
- Test EditMode lần đầu: 4 test Play mode đỏ vì `NullReferenceException` ở `TweenSetup.Init` (thêm lúc 22:20): project tắt domain reload nên từ lần vào Play thứ 2 DOTween đã "initialized" và `DOTween.Init()` trả về null, `.SetCapacity()` nối sau bị null. Sửa: gọi `DOTween.Init()` rồi `DOTween.SetTweensCapacity(500, 150)` riêng.
- Sau khi sửa: test EditMode 49/49 qua; vào Play 2 lần liên tiếp Console không lỗi.

---

## 23:00 (10/10) – Phản hồi khi dùng booster; Hint chỉ gợi ý

Yêu cầu: dùng booster phải có phản hồi (như icon nhảy ra giữa màn hình); booster 1 (Hint) và 3 (Reveal) đang trùng nhau (đều tự đặt khối đúng). Người dùng chọn cách 2: **Hint chỉ gợi ý, không đặt hộ**; Reveal giữ nguyên.

**Thêm**

- `Gameplay/Booster/HintMarker.cs`: mèo mờ nhấp nháy (`GridManager.RentGhost`, pool khối) + bản sao `TutorialHand` diễn kéo chéo vùng; tắt khi đặt đúng / đè khối khác / lưới mới / ván dừng; event `Changed` cho nút Hint sáng.
- `UI/HUD/BoosterUseFx.cs` (trên `Canvas_HUD`): icon bật ra giữa màn hình trên chùm tia sáng (`ProceduralSprites.Rays`) + chữ tên booster, tối 30 %, rồi lao tới mục tiêu và lóe sáng; tổng ≈ 0.6 s, chạy thời gian thực.
- `ProceduralSprites.Rays`: chùm 12 tia vẽ bằng code.
- `Editor/Tests/HintBoosterTests.cs`: chọn vùng nhỏ nhất; bỏ vùng ô số khóa; ưu tiên vùng trống dù lớn hơn; không có vùng → false.

**Sửa**

- `Effects/HintBooster.cs`: không gọi `PlaceSolved` nữa; `PickRegion` (bỏ ô khóa → ưu tiên vùng chưa bị đè → nhỏ nhất) → `Consume(type, tâm vùng)` + `ShowHint`. `IsActive` = gợi ý đang hiện (bấm lại không tốn lượt).
- `Effects/BoosterEffect.cs` (`IBoosterHost`): `Consume(type, Vector3? target)`, `ShowHint(rect)`, `IsHintShowing`.
- `BoosterManager`: `effectDelay` 0.6 s (Reveal bắt đầu đặt khối và Hint hiện sau khi icon tới); `LastTarget`; tự thêm `HintMarker`; `onModeChanged(Hint, …)` khi gợi ý bật / tắt.
- `Effects/RevealBooster.cs`: `Consume` kèm vị trí ô vừa chạm.
- `GridManager`: `RentGhost(rect)` / `ReturnGhost(view)`. `PlacedBlockView.SetAlpha`.
- `BoosterDatabase` (cả `Configs/` và `GameAssets/10_Data_Configs/`): mô tả Hint "Hint will automatically complete one correct region!" → "Hint shows where one correct region goes!" (lời nhắc tutorial mở khóa đọc từ đây).
- Scene: gắn `BoosterUseFx` (Canvas_HUD), `HintMarker` (Manager); scene sạch trước khi lưu.
- Docs: `Features/05_Booster.md`, `Features/14_GameFeel.md` (mục 6).

**Kiểm tra** (Play mode Level 38; kéo dài tạm thời gian hiệu ứng trong phiên thử để chụp ảnh, không lưu)

- Bấm Hint: bóng đèn giữa màn hình + tia sáng + "HINT!", màn tối (ảnh); lượt 3 → 2, nút Hint sáng. Icon tới nơi: mèo mờ hồng ở vùng số 2 (6,0)–(6,1) + bàn tay kéo (ảnh).
- Bấm Hint lần 2 khi gợi ý đang hiện: lượt vẫn 2.
- Kéo đúng vùng gợi ý → khối đặt, gợi ý tắt (mèo mờ trả pool, bàn tay ẩn, nút hết sáng).
- Reveal chạm ô số 4: kính lúp + "REVEAL!" (ảnh), lưới khóa; icon tới nơi → đặt 3 khối số 4, lưới mở lại.
- Freeze: đồng hồ đứng ngay; icon bay tới đúng vị trí đồng hồ (233, 746 px).
- Sau thử: trả lại lượt booster (Hint 3, Freeze 59, Reveal 1) và lưu; level giữ 38. Console không lỗi.
- Test EditMode 53/53 qua.

**Chưa làm**

- Giá Hint (1200 coin / 3 lượt) chưa đổi dù Hint yếu hơn trước – chờ người dùng quyết.

---

## 23:10 (10/10) – Booster chậm lại; đóng băng kiểu băng viền màn hình

Yêu cầu: hiệu ứng booster hơi nhanh; hiệu ứng đóng băng chưa ổn – thay vì khối băng vuông bao lấy bàn chơi, làm kiểu Fruit Ninja (viền điện thoại có băng).

**Thêm**

- `UI/HUD/FreezeScreenFrost.cs` (trên `Canvas_HUD`): băng viền màn hình vẽ bằng code theo tỉ lệ màn hình (270 px ngang). Cạnh trái / phải mỏng 0.035 bề ngang, trên / dưới 0.1, 4 góc mảng tròn 0.24; mép trong lởm chởm (fBm Perlin), đậm sát mép, vân tinh thể trắng (ridged noise), màu `(0.45, 0.8, 1)`. Bật: lan vào (1.3 → 1, 0.5 s) rồi thở nhẹ; hết hạn (`onFreezeEnded`): rõ + co 1 nhịp rồi vỡ lùi ra (0.45 s); bị hủy: tan 0.35 s.

**Sửa**

- `BoosterUseFx`: 0.28 / 0.1 / 0.22 s → bật 0.42 / giữ 0.45 / bay 0.32 s; `InputGate.Lock` từ lúc bấm tới khi icon tới (đồng hồ dừng, không mất giờ vì hiệu ứng). `BoosterManager.effectDelay` 0.6 → 1.2 s. Đặt lại số trong scene (component đã lưu số cũ).
- `BlockFeedback`: bỏ tấm băng vuông phủ lưới (`FreezeTint`), giữ mèo run khi đóng băng. `ProceduralSprites.Frost` xóa (không còn dùng).
- `FreezeIndicator.frostOverlay` (lớp `ice_effect` lóe toàn màn hình, object `ShieldFrost`) gỡ trong scene để không chồng 2 lớp băng; object `ShieldFrost` vẫn còn (alpha 0, không dùng).
- Docs: `Features/14_GameFeel.md` (4.3, 6.1), `Features/05_Booster.md`.

**Lỗi gặp khi làm, đã sửa**

- Băng ra xám, mờ: viết `Mathf.SmoothStep(0.82, 1, t)` như `smoothstep` của shader; `Mathf.SmoothStep(a, b, t)` của Unity là nội suy a → b nên sát mép alpha chỉ 0.18. Sửa: `SmoothStep(0, 1, InverseLerp(0.82, 1, t))`.
- Băng dày 7.5 % đều 4 cạnh che nửa cột ô ngoài cùng (lưới 7 cột sát mép) → tách độ dày cạnh trái / phải, trên / dưới, góc.
- Script dọn `ProceduralSprites` cắt sai chỗ làm `Rays` bị lặp (lỗi biên dịch CS0102) → xóa bản lặp + `Frost`.
- DOTween cảnh báo "Infinite loops aren't allowed inside a Sequence" (tween thở lặp vô hạn nằm trong Sequence) → tween thở tách riêng, chạy khi hiện xong.

**Kiểm tra** (Play mode Level 38)

- Đóng băng (gọi `LevelTimer.Freeze`, không tốn lượt): băng trắng xanh quanh viền, góc dày, cạnh trái / phải chỉ phủ lề, lưới nhìn rõ (ảnh). Hết 2.5 s → vỡ và tắt.
- Hint: thao tác khóa từ lúc bấm, mở lại lúc 1.20 s, mèo mờ hiện lúc 1.21 s.
- Sau thử: trả lượt booster về như lúc vào (Hint 2, Freeze 58, Reveal 0), level giữ nguyên. Console không lỗi / cảnh báo.
- Test EditMode 53/53 qua.

---

## 23:20 (10/10) – Đóng băng theo video tham khảo (đang làm dở)

Yêu cầu: bỏ băng tinh thể viền màn hình; làm hiệu ứng booster Đóng băng giống video (lúc bấm + hiệu ứng viền).

**Đã làm**

- `UI/HUD/FreezeBoosterFx.cs` (Canvas_HUD, sprite `snowflake.png`): khói + đồng hồ mèo bật ra giữa bàn chơi, lắc như reo, mặt hiện bông tuyết, lóe trắng, sóng trắng + quầng sáng lan hết màn hình, sao băng lớn, bông tuyết bắn ra, đồng hồ nhỏ lại biến mất. Khóa thao tác tới lúc sóng nổ.
- `FreezeBooster`: trừ lượt ngay, đồng hồ đóng băng lúc sóng nổ (`IBoosterHost.AfterEffect`, `BoosterManager.freezeEffectDelay` 1.55 s). `BoosterUseFx` bỏ qua Freeze.
- `UI/HUD/FreezeScreenFrost.cs` viết lại: ánh xanh lạnh ở viền màn hình + dải sáng đáy + tuyết rơi liên tục trong lúc đóng băng (thay băng tinh thể).
- `UI/HUD/FreezeButtonCountdown.cs` (trên nút Freeze): đĩa tối, số giây xanh lá, vòng tiến trình đếm ngược.
- `ProceduralSprites.Ring`, `Disc`.

**Kiểm tra**: biên dịch sạch; Play mode: đồng hồ bật ra + reo đúng, đóng băng lúc nổ, ánh xanh viền + nút "20" có vòng (ảnh). Lượt booster thử đã trả lại.

**Tiếp tục ở mục 01:30 (11/10) bên dưới.**

---

## 01:30 (11/10) – Đóng băng: sửa lỗi không tắt + chỉnh theo video

Yêu cầu: viền quá đậm; bông tuyết phải mờ hơn, thưa hơn, bay vào từ mọi phía; hết thời gian mà hiệu ứng không tắt; hiệu ứng giữa màn hình cần kim đồng hồ quay, bàn tay bấm nút, bông tuyết nổ giữa màn hình. Làm lần lượt từng việc.

**1. Hết thời gian mà hiệu ứng không tắt (lỗi có từ trước trong `LevelTimer`)**

- Nguyên nhân: `Update` trừ `_freezeLeft` xuống <= 0 rồi mới gọi `SetFrozen(0)`; lúc đó `IsFrozen` đã false nên `SetFrozen` tưởng không đổi, không phát `onFreezeChanged(false)`. Băng trên đồng hồ vẫn vỡ (nghe `onFreezeEnded`), còn tuyết / viền xanh / đếm ngược trên nút / quầng sáng nút Freeze không bao giờ tắt.
- Sửa: tính `left` riêng, còn > 0 thì gán; hết thì `SetFrozen(0)` khi `_freezeLeft` vẫn > 0 rồi `onFreezeEnded`.
- Thêm: ván kết thúc (Won / OutOfTime / Lost) mà còn đóng băng thì tắt đóng băng (đồng hồ không chạy nữa nên đóng băng không tự hết); Paused giữ nguyên.
- `FreezeScreenFrost`: hết đóng băng thì bông đang bay mờ dần trong `fadeOut` (0.5 s) thay vì bay nốt.
- Kiểm tra: đóng băng 3 s, hết lúc 3.8 s, mọi hiệu ứng tắt sạch lúc 4.3 s (trước sửa: sau 8 s vẫn còn).

**2–3. Viền nhạt hơn; tuyết thưa, mờ, bay vào từ 4 cạnh (`FreezeScreenFrost`)**

- Viền xanh alpha 0.55 → 0.28, dải sáng đáy 0.35 → 0.2.
- Tuyết: 9 → 4 bông / giây, tối đa 16, cỡ 18–46, alpha 0.3–0.6; sinh ngay ngoài 1 cạnh bất kỳ (cạnh dài ra nhiều hơn), bay về 1 điểm gần giữa (± 25 %) với tốc độ 80–160, lắc ngang; hiện dần 0.4 s, tan ở 40 % cuối và tan hết khi đi được 60 % quãng (`travel`) nên không che bàn chơi.

**4. Hiệu ứng giữa màn hình (`FreezeBoosterFx`), theo video phóng to**

- Đồng hồ xoay tít (540° → 0) từ làn khói rồi phóng to.
- Bông tuyết trên mặt quay như kim: 1440° nhanh rồi chậm dần tới lúc nổ; mặt lóe trắng (`FaceFlash`) sau lần bấm cuối rồi bông tuyết rõ hẳn.
- Bàn tay trắng (`tutorial_hand.png`, xoay 150° cho ngón trỏ chỉ xuống, gốc xoay ở đầu ngón) bay vào, bấm nút trên đỉnh 3 lần (0.6 / 0.85 / 1.1 s), đồng hồ giật mỗi lần + rung Selection; giữa các lần đồng hồ lắc như reo; rút ra trước khi nổ.
- Nổ (1.55 s): cả màn lóe xanh (`boardFlash` alpha 0.25), sóng trắng + quầng sáng (0.3) lan hết màn hình, sao băng lớn, 28 bông tuyết cỡ 30–110 bay xa 0.3–1.15 × 560, xoay, mờ dần.
- Scene: gán `hand`, `popDuration` 0.35, `burstFlakes` 28, `burstDistance` 560, `boardFlash`; `FreezeScreenFrost` đặt lại các số mới.

**Kiểm tra** (Play mode Level 38; làm chậm `DOTween.unscaledTimeScale` chỉ trong phiên thử để chụp)

- Ảnh: bàn tay chỉ xuống đỉnh đồng hồ, bông tuyết mặt đang quay; ngay sau nổ: màn lóe xanh, vòng sóng, nút "20"; sau đó: sao băng xanh + bông tuyết to nhỏ bắn tung (giống khung 5.55 s của video).
- Đóng băng: viền nhạt, bông tuyết thưa quanh 4 cạnh (ảnh).
- Lượt booster thử đã trả lại (Freeze 57), level giữ nguyên. Console không lỗi. Test EditMode 53/53.
- Scene báo "chưa lưu" mỗi lần thoát Play mode (Enter Play Mode Options tắt reload scene); đã kiểm tra file scene không có object tạo lúc chạy bị lưu lẫn.

---

## 02:00 (11/10) – Đóng băng y hệt bản gốc (Spine + hạt gốc)

Yêu cầu: làm hiệu ứng y hệt video, nhất là đồng hồ có bàn tay bấm; kiểm tra asset có sẵn không (sprite sheet hay Spine), không có thì tìm trong `E:\PawBlock`.

**Kiểm tra**

- Project có sẵn `GameAssets/02_Cat_Characters_Spine/Spine_Freeze/` (`Freeze.skel.bytes` Spine **4.1.24**, `Freeze.atlas.txt`, `Freeze.png`: thân đồng hồ, nút `NutBam`, tay `Hand1–3`, bông tuyết `Center`, tia `FX` / `Touch_FX`) → là **Spine**, không phải sprite sheet. Project chưa có thư viện Spine (2 asset SkeletonData / Atlas cũ trỏ tới `spine-unity.dll` của game gốc → thiếu script).
- `E:\PawBlock\ExportedProject` (project xuất từ game gốc): script `FreezeOverlayFX` gồm `handStartFreezeSG` (SkeletonGraphic), `freezeFX` (UIParticle), `freezeBorderState` (CanvasGroup). Thân hàm trống (IL2CPP) nên đọc cấu hình từ scene `Gameplay.unity`: `HandStartFreeze` (0, −26), `Booster_FreezeFX` (4 hệ hạt, UIParticle scale 500), `FreezeBorders` (fade 0.4; `Snow` 3 hệ hạt, scale 150; `Border` = `ice_effect` phủ toàn màn). Canvas gốc 1080×1920 khớp ngang như project.
- Skeleton: 1 animation `Start` 2.367 s, sự kiện `Press` @ 1.67 s.

**Thêm / sửa**

- `Packages/manifest.json`: `com.esotericsoftware.spine.spine-csharp` + `spine-unity` (Git, nhánh `4.1`) → spine-csharp 4.1.2, spine-unity 4.1.43 (người dùng đồng ý; giấy phép Spine Runtimes do người dùng cân nhắc). Bộ nhập Spine tạo lại `Freeze_SkeletonData` / `Freeze_Atlas` / material.
- Xóa 5 shader Spine chép từ game gốc trong `09_Shaders_Materials/Shaders/` (`Spine/Skeleton`, `Spine/SkeletonGraphic*`) – trùng tên shader của package, không material nào dùng.
- `Freeze.png`: tắt *Alpha Is Transparency* (atlas `pma:true`, theo cảnh báo của spine-unity).
- `Prefabs/Gameplay/Freeze/Booster_FreezeFX.prefab`, `FreezeSnow.prefab`: chép nguyên YAML ParticleSystem + UIParticle từ scene gốc (script: `extract_particles` trong scratchpad), đổi GUID material và script `UIParticle` sang của project.
- Material `Twinkle`, `Spark_White`, `snowflake`, `Circle_Blue`: gán lại texture (đang trỏ GUID texture của game gốc nên mất).
- `UI/HUD/FreezeOverlayFX.cs` (mới, trên `Canvas_HUD/FreezeOverlayFX`): chạy Spine `Start`, khóa thao tác tới sự kiện `Press` → nổ `Booster_FreezeFX`; `FreezeBorders` theo `onFreezeChanged` (fade 0.4). UIParticle / ảnh không chắn chạm.
- Xóa `FreezeBoosterFx.cs` (đồng hồ dựng tay) và `FreezeScreenFrost.cs` (viền + tuyết vẽ bằng code); gỡ 2 component thiếu script trên `Canvas_HUD`. `BoosterUseFx` luôn bỏ qua Freeze. `BoosterManager.freezeEffectDelay` 1.55 → 1.67.

**Kiểm tra** (Play mode Level 38; làm chậm Spine + tween thời gian thực chỉ trong phiên thử để chụp)

- Ảnh: đồng hồ Spine + bàn tay trên nút vàng như video; ngay sau `Press`: vòng `Circle_Blue` lớn, bông tuyết + tia bắn quanh đồng hồ (khớp khung 5.55–5.7 s video); trong lúc đóng băng: viền `ice_effect` xanh trên / dưới + tuyết rơi + nút "20" (khớp khung 7 s+).
- Thao tác khóa từ lúc bấm tới `Press`; đồng hồ đóng băng đúng lúc nổ; hết animation thì ẩn đồng hồ.
- Đóng băng 3 s hết tự nhiên → viền + tuyết + đếm ngược tắt (5.0 → 5.1 s).
- Lượt Freeze thử đã trả lại (56). Console không lỗi. Test EditMode 53/53.

**Còn lại / lưu ý**

- 16 material khác trong `09_Shaders_Materials/Materials` cũng trỏ texture theo GUID game gốc (mất texture): `Amazing`, `Beauty`, `Great`, `Like`, `Nice`, `Perfect`, `Wow`, `Paper_2x2`, `Paper_Sheet`, `Spark_Yellow`, `Star_Sheet_Yellow`, `Twinkle_White`, `icon_coin`, `Light`, `BlockFlash`, `UI_Grayscale` (một số có thể không cần texture) – chưa sửa.
- Mèo run khi đóng băng là phần thêm của project, video gốc không có.

## 02:12 (11/10) – Gán lại texture cho 14 material chép từ game gốc

**Vấn đề:** các material trong `09_Shaders_Materials/Materials` chép từ `E:\PawBlock\ExportedProject` vẫn trỏ `_MainTex` theo GUID texture của game gốc (không có trong project) → `mainTexture == null`.

**Cách làm:** đọc GUID `_MainTex` trong `.mat` → tìm `.meta` có GUID đó trong `ExportedProject/Assets/Texture2D` (cả 14 khớp đúng tên) → gán texture cùng tên trong project qua `AssetDatabase` (`mainTexture`, `SetDirty`, `SaveAssets`). Ưu tiên thư mục GameAssets theo chức năng, không dùng bản trùng ở `11_Original_Atlases_Textures`.

| Material | Texture gán | Thư mục |
|---|---|---|
| `Amazing`, `Beauty`, `Great`, `Like`, `Nice`, `Perfect`, `Wow` | cùng tên | `02_Cat_Characters_Spine/Praise_Badges` |
| `Paper_2x2`, `Paper_Sheet` | cùng tên | `04_UI_Graphics/Dev_Debug_UI` |
| `Spark_Yellow`, `Star_Sheet_Yellow`, `Twinkle_White` | cùng tên | `05_VFX_Particles/Sparks_Twinkle` |
| `Light` | `Light` | `05_VFX_Particles/Light_Glow` |
| `icon_coin` | `icon_coin` | `11_Original_Atlases_Textures` (bản duy nhất; trùng MD5 với file gốc. `Icon_Coin.png` ở `ShikaCat_GameAssets` là ảnh khác) |
| `BlockFlash` | **để trống** | Material do project tự tạo (`WinScreenBuilder`, shader `GUI/Text Shader`), dùng trên SpriteRenderer của `PlacedBlock.prefab` → texture lấy từ sprite của renderer. Game gốc không có material này. |
| `UI_Grayscale` | **để trống** | Material do project tự tạo (`PopupBuilder`, shader `PawBlock/UI/Grayscale`), dùng cho Image trong `SampleScene` → UI tự truyền sprite vào `_MainTex`. Game gốc không có material này. |

**Kiểm tra:** đọc lại 20 material qua `AssetDatabase` → 14 material trên có texture đúng tên; 4 material đã sửa trước (`Twinkle`, `Spark_White`, `snowflake`, `Circle_Blue`) giữ nguyên; `BlockFlash`, `UI_Grayscale` vẫn `null` (đúng ý đồ). Chưa vào Play mode xem từng hiệu ứng.

## 02:25 (11/10) – Hint: số trên mèo mờ khó đọc

**Vấn đề:** dùng booster Hint, số trên mèo mờ nhấp nháy khó đọc. Mèo mờ hạ alpha cả số (0.25–0.6) nên số nhạt của ô bên dưới (màu tím nhạt) lộ lên, chồng với số sẫm mờ của mèo → 2 số lẫn vào nhau.

**Sửa:** `PlacedBlockView.SetAlpha` (chỉ `HintMarker` dùng): thân / tai / mặt vẫn nhấp nháy, số giữ đặc màu sẫm theo màu mèo (như khối đã đặt). Số của mèo và số của ô cùng font, cùng cỡ nên số đặc che kín số ô.

**Kiểm tra:** Play Level 38, bật gợi ý, đặt alpha mèo ở mức thấp nhất 0.25 → số "2" sẫm, rõ trên nền mèo. Console không lỗi.

## 06:10 (11/10) – Sao theo thời gian: thanh 3 mốc + màn thắng có sao

**Vì sao:** game gốc có sao nhưng project chưa có (`WinPopup` ghi "Game không có sao"). Đọc lại `ExportedProject`: `WinUI.cs` có `earnedStars`, `starCGs`, `starSlamScale / Duration / Punch / Interval`, `titleImages[4]`; scene có `SafeArea/TimeAndStar/TimeProgress` (đang bật) với 3 mốc tên `60` / `20` / `0` đặt đúng 60 % / 20 % / 0 % chiều rộng phần chạy (530 px). Thân hàm gốc đã bị strip nên luật tính sao tự đặt theo các mốc này.

**Luật:** còn > 60 % thời gian = 3 sao, > 20 % = 2 sao, còn giây nào = 1 sao, hết giờ = 0 sao. Sao chỉ giảm (hết giờ rồi thêm giờ vẫn 0 sao). Tiêu đề: 3 Perfect, 2 Great, 1 Nice, 0 Well done.

**Code**

- `Gameplay/Stars/StarRating.cs` (mới): `StarRating` (hàm thuần `FromRatio`, `MarkerRatio`, `Title`), `WinTitle`, `StarTracker` (giữ số sao, chỉ giảm).
- `Gameplay/Stars/LevelStars.cs` (mới, cùng `LevelTimer`): nghe `onTimeChanged` / lưới mới, phát `StarsChangedEvent` (`GameEvents.cs`).
- `LivesEnergyConfig`: `threeStarRatio` 0.6, `twoStarRatio` 0.2.
- `UI/HUD/TimeStarBar.cs` (mới): nền `tin_trnh2`, phần còn lại `tin_trnh1` (Sliced, co chiều rộng nên 2 đầu luôn bo tròn), mốc sao `star2` xám + `star` vàng; mất sao → sao vàng phồng 1.8 lần + mờ dần; lưới mới → 3 sao bật lần lượt; đóng băng → phần còn lại ngả xanh băng. `tin_trnh1/2.png` (Grid_Cells) chính là `tiến_trình1/2` gốc (so từng pixel với bản cắt từ atlas Gameplay: trùng), nên không cắt thêm.
- `WinPopup`: bỏ chữ TMP "WELL DONE!" (`TMPLetterPopIn` không còn dùng ở đâu), thay bằng 4 ảnh tiêu đề bật 1 theo số sao; sao vàng `star_2` đập xuống từ 2.6 lần trong 0.18 s, cách nhau 0.18 s, chạm thì nảy + âm `StarEarn` cao dần + rung nhẹ; bóng `star_5` cố định. Vị trí theo `WinUI` gốc: tiêu đề +633, sao (-172, 407) nghiêng 32.5°, (-3, 437), (164, 405) nghiêng -35.3°.
- Ảnh mới cắt từ atlas `EndGame` theo `m_Rect` trong `ExportedProject/Assets/Sprite/*.asset`: `04_UI_Graphics/Popups_Dialogs/well_done_1.png`, `nice_1.png`, `great_1.png`, `perfect_1.png`.
- `LevelStatSave.bestStars`: sao cao nhất khi thắng (`LevelStatsRecorder`).
- Âm mới (`SoundId`, Setup Audio): `StarLost` = `poof_star` (`GameAudioEvents`, không phát khi tụt về 0 sao vì đã có âm hết giờ), `StarEarn` = `unlock_obs2`.
- `Editor/UI/StarsBuilder.cs` (mới, **Tools → PawBlock → Builders → Build Stars**): thêm `LevelStars`, dựng `LevelFrame/TimeBar` (640x70 = gốc 549x60 x 1.166), gọi lại Build Win Screen. `WinScreenBuilder` dựng tiêu đề + sao.
- Xóa `UI/HUD/LevelProgressBar.cs` (thanh % ô đã lấp): bản gốc đặt thanh thời gian ở đúng chỗ đó. Sửa `Features/14_GameFeel.md` cho khớp.

**Kiểm tra**

- Test EditMode `StarRatingTests` 15/15 (ngưỡng đúng mốc, tracker chỉ giảm, thêm giờ không lấy lại sao, tổng 0 bỏ qua).
- Play Level 12: thanh 3 sao vàng dưới khung; đặt còn 50 % → sao 3 xám; thắng → "GREAT!" 2 sao; Continue → coin bay vào ô coin. Hết giờ → thêm 30 s → thắng → "WELL DONE!" 3 sao xám. Console không lỗi.

## 06:10 (11/10) – Asset gốc chưa dùng: hoa văn nền, khung ô gợi ý, đèn rọi, icon to, coin lật, tiếng mèo

- **Hoa văn nền** (`GameplayUI/BG_Pattern` gốc): `Editor/Visuals/BackgroundPatternBuilder.cs` (mới, **Build Background Pattern**) thêm SpriteRenderer con `Pattern` của nền `bg`, vẽ Tiled `BG_Pattern.png` (đặt Mesh Full Rect + Wrap Repeat), ô lát 1.55 đơn vị = 198 px / màn 1920 như gốc. Thứ tự vẽ: `bg` -1 → -2, `Pattern` -1, ô lưới 0+. Hoa văn rất nhạt (alpha ảnh tối đa 24 %) như gốc.
- **Khung ô chờ gợi ý** (`Cell/WaitSameHint` gốc): `GameplayVisuals.hintCellFrame` = `khung_reveal.png` (9-slice 44 px, Setup Gameplay Visuals). `HintMarker` đặt 1 khung trên mỗi ô của vùng gợi ý (order 9: trên ô, dưới mèo), hiện mờ dần 0.25 s rồi nhấp nháy cùng nhịp mèo mờ.
- **Đèn rọi tutorial booster** (`TutorialBoosterUI/CircleMask(F)` gốc): `BoosterTutorialView.spotlightSprite` = `CircleMask.png`. Nền tối đổi thành ảnh `CircleMask` (cạnh = nút x 2) tại nút + 4 dải tối cùng màu (tự tạo lúc chạy, cùng chặn chạm); góc các mảnh gán thẳng bằng `offsetMin / offsetMax` để mép trùng nhau từng số. Lần đầu có chồng 0.5 đơn vị để "chống khe" → hiện vệt tối 1 px (đo pixel: 44 → 18), bỏ chồng thì đều màu. `TutorialBuilder` gán sẵn sprite.
- **Icon booster to** (gốc dùng `HintBig` / `FreezeBig` / `RevealBig` cho ô vật phẩm trong gói Shop, Thua / Thêm giờ, quà Daily): `BoosterData.iconBig` + `ItemIcon`; `BoosterDatabase.asset` gán 3 icon; `ShopBuilder` và `DailyRewardSlot` dùng `ItemIcon`; 21 icon trong các gói Shop của scene đã đổi. Nút booster HUD, popup mua, ô thống kê Profile giữ icon nút.
- **Coin bay ở màn thắng**: lật liên tục quanh trục Y (0.45 s / vòng, lệch pha ngẫu nhiên). Không dùng `CoinFbf.png`: khung hình đó vẽ đồng xu chữ "T", khác đồng xu chân mèo `coin.png` dùng khắp game; game gốc cũng để `CoinFly/CoinFbf` tắt, Animator không có controller. Bấm Continue / x2 phát `earrn_reward` (`SoundId.Reward`).
- **Tiếng mèo**: game gốc gán theo mặt (`AudioClipDataBase.faceAudioClips`): mochi → Sly + Lying, dozy → Dumb + Sad. Thêm Sly, Lying vào `CatHappy`, Dumb vào `CatSad`. `AudioSetup` chạy lại giờ bổ sung clip mới vào âm đã có (giữ volume / pitch / cooldown đã chỉnh).

**Kiểm tra:** Play Level 12: hoa văn có trên nền (soi tăng tương phản), Hint hiện 2 khung trên vùng 2 ô dưới mèo mờ, đèn rọi quanh nút Freeze đều màu. Console không lỗi. Chưa xem lại tab Shop / popup điểm danh sau khi đổi icon.

## 06:35 (11/10) – Daily Challenge + Level Generator

**Vì sao gộp:** game gốc sinh level Daily lúc chạy (`LevelGenerator.GenerateDailyChallenge(seed, 10, 10)`, `GameplayController.DailyBoardWidth / Height = 10`), nên tool sinh level người dùng muốn làm chính là lõi của Daily. Thân hàm gốc đã bị strip; luật lấy từ tên field / tooltip (`DailyChallengeUI`: `progressMaxDays` 21, mốc 7 / 14 / 21, `MissScanMaxDays` 400, `frameStreak / Miss / Today`, màu chữ; `SaveManager.DailyUnlockLevel` 21) và layout từ scene.

**Level Generator**

- `Gameplay/Level/Generator/LevelGenerator.cs` (mới, C# thuần, chạy được cả lúc chơi): chia lưới thành khối chữ nhật theo seed (`System.Random`), 1 ô số ngẫu nhiên mỗi khối, đổi chỗ ô số / chia lại tới khi **đúng 1 lời giải**. Preset Easy 6 x 6 / Normal 7 x 7 / Hard 9 x 9 / Daily 10 x 10.
- `ShikakuSolver.CountSolutions(level, limit)`: `Backtrack` nhận thêm `limit` (đếm tới limit lời giải); `TrySolve` gọi với limit 1 nên hành vi cũ giữ nguyên.
- `Editor/Level/LevelGeneratorWindow.cs` (mới, **Tools → PawBlock → Level Generator**): sinh N level từ seed đầu, cột độ khó (`LevelDifficultyAnalyzer`), sắp theo độ khó, xem trước lưới / lời giải, lưu vào `Levels/Generated` (tùy chọn thêm cuối `LevelDatabase`), mở trong Level Editor.
- Đo 20 seed / preset: 80/80 đúng 1 lời giải, ≤ 1 ms trung bình (Daily tối đa 6 ms). Độ khó trung bình 30 / 30 / 34 / 36 – ngang level 11–100 của database.

**Daily Challenge**

- `Gameplay/Daily/DailyChallengeModel.cs` (luật, C# thuần), `DailyChallengeDatabase.cs` (+ `Configs/DailyChallengeDatabase.asset`), `DailyChallengeManager.cs` (cùng `LevelManager`); `SaveData.challenge { startDay, doneDays, claimedGifts }`.
- `LevelManager.LoadDaily / IsDaily / DailyDay / PlayCurrent`; `Retry` chơi lại đúng level Daily; `NextLevel` bỏ qua khi Daily. `LevelLoadedEvent.IsDaily`.
- Ván Daily: HUD + popup Thua ghi ngày (`OCT 11`); `LevelStatsRecorder` không ghi; `ProfileStatsRecorder` không tăng `levelsCleared` (lúc Daily `CurrentLevelNumber` là level thường chưa thắng); `BoosterTutorial` không chạy.
- `ScreenFlow.PlayDaily(load)`; nút PLAY thường gọi `PlayCurrent` thay `Retry` (không thì vừa chơi Daily xong bấm PLAY lại vào Daily); `ShowMenu` mở tab Daily nếu vừa chơi Daily. `WinPopup` Continue ở ván Daily → về Menu.
- UI (`UI/Menu/`): `DailyChallengePanel`, `DailyDayCell`, `DailyGiftSlot` (tia sáng `tia_sang` xoay bằng DOTween thay `LightFXImage.controller`), `DailyGiftBubble` (`dialog.png` 9-slice + `ArrowDialog 1.png`), `DailyTabBadge` (`btn_lock2`, `badge_red`).
- `Editor/UI/DailyChallengeBuilder.cs` (mới, **Build Daily Challenge**): database (nhập CSV), manager, `DailyPanel` thay `ProgressPanel`, tab "Daily" + `icon_daily`. `MainMenuBuilder` gọi builder này thay khung tạm (xóa `BuildProgressPlaceholder`). `DailyRewardManager.GrantItem` thành public để dùng chung.
- Asset gốc đã dùng: `daily_khung_1–3`, `daily_ttrinh_1–4`, `daily_bg1`, `hop_qua_1_1 / 2_1 / 3_1`, `tia_sang`, `btn_next`, `btn_yellow_5 / 6`, `btn_lock2`, `icon_cup`, `icon_tick`, `icon_daily`, `dialog`, `ArrowDialog 1`, `DailyChallengeData.txt`.

**Kiểm tra**

- Test EditMode 83/83 (thêm `DailyChallengeModelTests` 7, `LevelGeneratorTests` 8).
- Play (save level 35, đã sao lưu và trả lại sau khi thử): tab Daily như gốc (ảnh chụp); Play → level `Daily_20261011` 10 x 10, HUD "OCT 11", 3:07; thắng → Continue về tab Daily, ô 11 cam, thanh 1/21, nút xám, chấm đỏ tắt. Dời ngày bắt đầu về 1/10 + đánh dấu 6 ngày: ô lỡ tím đậm, nút Play vàng, hộp mốc 7 sáng; bấm nhận → Hint 20 → 21, năng lượng vô hạn +15 phút, hộp mờ + tick, bong thoại 3 món. Play vàng → quảng cáo giả → level ngày 10/10. Thua → popup ghi "OCT 10", Home → tab Daily. PLAY ở Home sau đó → level thường 36.
- Lỗi tìm thấy khi thử: mũi tên bong thoại bị xếp vào layout (Icon đã có `LayoutElement`, thêm cái thứ 2 với `ignoreLayout` không có tác dụng) → dùng lại `LayoutElement` sẵn có; đã kiểm tra lại.
- Console không lỗi. Cửa sổ Level Generator: gọi Sinh + Sắp theo độ khó qua code, vẽ lại không lỗi; chưa nhìn giao diện cửa sổ bằng mắt.

**Còn lại**

- Chưa có thông báo "Daily Challenge đã mở" lúc vừa tới level 21 (gốc có `IsDailyChallengeUnlockCelebrated`).
- Level Daily chưa có cơ chế (Void / hướng / khóa / "?"): `MechanicInjector` nằm trong Editor.

## 06:40 (11/10) – Daily: số mốc 7 / 14 / 21 bị hộp quà che

**Vấn đề:** người dùng báo số mốc dưới thanh tiến trình bị ẩn: ô vàng `daily_ttrinh_4` đặt tâm -40 so với thanh, hộp quà (đáy -52) vẽ đè lên nửa trên, số nằm giữa ô nên bị che.

**Sửa** (`DailyChallengeBuilder.Marker`): ô mốc hạ xuống tâm -66, số dời xuống nửa dưới ô (-8) → hộp quà chỉ đè mép trên ô vàng, số hiện đủ. Chữ "+1" dựng ở trạng thái tắt (trước đây chỉ tắt trong `Awake` của panel; dựng lại lúc đang Play thì `Awake` chạy trước khi gán field nên "+1" lộ ở đầu thanh).

**Kiểm tra:** Build Daily Challenge, Play, tab Daily: 7 / 14 / 21 hiện rõ dưới 3 hộp quà. Lưu scene ở chế độ Edit; save Editor trả lại bản trước khi thử.

## 06:43 (11/10) – Daily: bấm hộp quà khác mất 1 lần chạm

**Vấn đề:** đang mở bong thoại quà, bấm hộp quà khác thì lần đầu chỉ đóng bong thoại (lớp chặn trong suốt phủ màn hình nuốt chạm), phải bấm lần nữa mới mở cái mới.

**Sửa:** bỏ lớp chặn `Blocker` (`DailyChallengeBuilder.BuildBubble`). `DailyGiftBubble.Update`: ngón tay vừa chạm xuống ngoài khung bong thoại (`Pointer.current.press.wasPressedThisFrame` + `RectangleContainsScreenPoint`) thì ẩn, chạm vẫn tới nút bên dưới. Ẩn xảy ra lúc chạm xuống, mở bong thoại mới lúc nhả tay (onClick) nên 1 lần bấm là đổi sang hộp mới; bấm ô ngày / nút / tab khi đang mở bong thoại cũng chạy luôn.

**Kiểm tra:** Play, tab Daily: mở quà 7 → bấm hộp 14 → bong thoại "Win 14 days" ngay. Giả lập nhấn chuột vào tab Shop khi bong thoại đang mở → bong thoại đóng và chuyển tab Shop trong cùng 1 lần nhấn. Console không lỗi; save Editor trả lại bản trước khi thử.

## 06:47 (11/10) – Sao có tác dụng: cộng coin thưởng khi thắng

**Yêu cầu:** sao sau khi thắng cần có tác dụng.

**Làm:** mỗi sao cộng `EconomyConfig.coinsPerStar` (5) coin vào thưởng thắng: Normal 10 + 5 x sao (3 sao = 25), Hard 20 + ..., Very Hard 30 + ...; nút x2 gấp đôi cả phần sao. `EconomyConfig.WinCoinReward(difficulty, stars)` (hàm cũ không tham số sao giữ lại làm mức nền).
- `WinPopup`: 2 nút bật **trước** khi sao đập xuống (trước đây bật sau), số coin bắt đầu ở mức nền rồi mỗi sao chạm xuống thì cộng 5, hàng "coin + số" nảy; sao đập xong mới nhận bấm. Nhận coin theo số sao thật (`LevelStars`).
- Test `StarRatingTests.CoinThuongCongTheoSao` (4 case).

**Kiểm tra:** test EditMode 87/87. Play Level 12 còn 50 % giờ → 2 sao: nút hiện 10 / 20 lúc mở, sau 2 sao thành 20 / 40; Continue → coin 420 → 440. Save Editor trả lại bản trước khi thử.

## 06:56 (11/10) – Vòng quay may mắn (tiêu sao)

**Yêu cầu:** sao có giá trị lâu dài: vòng quay may mắn, mỗi lần quay tốn sao, mở từ nút trên Main Menu như Daily / No Ads.

**Làm**

- **Ví sao** `Gameplay/Stars/StarWallet.cs` (mới, như `CoinWallet`), `SaveData.stars`. `LevelStars`: ván thắng (cả Daily, không tính màn luyện tập) cộng số sao của ván vào ví.
- `Gameplay/LuckyWheel/LuckyWheelDatabase.cs` (mới, + `Configs/LuckyWheelDatabase.asset`): `spinCost` 10 sao, 8 ô theo chiều kim đồng hồ từ đỉnh (quà dùng chung `DailyRewardItem`, trọng số, màu): coin 50 (25), Hint x1 (15), coin 100 (15), Freeze x1 (12), coin 200 (8), Reveal x1 (8), vô hạn 15 phút (10), coin 500 (2) – tổng 95.
- `Gameplay/LuckyWheel/LuckyWheelModel.cs` (mới, C# thuần): `Pick(trọng số, roll)`, `CenterAngle`, `TargetRotation` (quay theo chiều kim đồng hồ ≥ n vòng, dừng đúng ô dưới kim).
- `UI/Popups/LuckyWheelPopup.cs` (mới): SPIN → trừ sao, chọn ô, **trao quà ngay** (tắt app giữa lúc quay không mất), quay 5 vòng / 4 s `OutQuart`, kim nảy + tick mỗi ô, dừng lệch ngẫu nhiên trong ô; ô trúng nhấp sáng, "You got ..." + icon, âm nhận thưởng. Đang quay khóa SPIN / X; thiếu sao: nút xám + "Win levels to earn stars!".
- `UI/Menu/LuckyWheelButton.cs` (mới): nút "Lucky" trên Home (đối xứng nút Daily, `star_2` + `daily_back` / `daily_ribbon`), ô kem dưới nút hiện số sao, đủ sao quay thì lắc + chấm đỏ.
- `Editor/UI/LuckyWheelBuilder.cs` (mới, **Build Lucky Wheel**): database, popup (vòng vẽ từ `Circle.png`: mỗi ô 1 Image Filled Radial360 xoay tới cung của nó, đường kẻ trắng, viền vàng / tím, trục `star_2`, kim `ArrowDialog 1` lật), nút. `MainMenuBuilder.BuildLucky` / `RebuildLucky`; dựng lại Menu cũng có nút.
- Game gốc không có vòng quay / asset vòng quay: vòng dựng hoàn toàn bằng hình tròn + màu.

**Kiểm tra**

- Test EditMode 99/99 (thêm `LuckyWheelTests` 12: chọn ô theo trọng số, trọng số 0 không ra, góc tâm, quay ≥ 5 vòng theo chiều kim đồng hồ và dừng đúng ô từ nhiều góc đầu).
- Play: Home có nút Lucky + "25"; mở popup, SPIN → sao 25 → 15, Hint 20 → 21, dừng ở 73.2° (ô Hint 67.5° ± 13.5°), ô nhấp sáng, "You got x1!". Thắng level 3 sao → ví 0 → 3. Hết sao → SPIN xám. Bố cục chỉnh 1 lần (số sao sát kim, dòng kết quả sát nút). Save Editor trả lại bản trước khi thử.

## 06:58 (11/10) – Cheat: thêm / bớt sao

- `DebugCheats.AddStars(n)` (âm = bớt, không xuống dưới 0), `SetStars(n)`.
- Cheat Panel (trong game, tab Tài nguyên): mục "Sao ★ (Vòng quay may mắn) · số" với +10 / +50 / -10 / = 0; dòng trạng thái thêm "★ n"; tiêu đề Coin hiện số coin.
- Debug Panel (Editor): mục "Coin / Sao ★" thêm hàng +10 / +50 / -10 / = 0 và ô nhập + Đặt; dòng trạng thái thêm "★ n".

**Kiểm tra:** Play: 0 → +50 = 50 → -10 = 40 → đặt 5 → -99 = 0 → đặt 30; mở Cheat Panel tab Tài nguyên thấy mục Sao ★ · 30 và "★ 30" ở dòng trạng thái. Console không lỗi; save Editor trả lại bản trước khi thử.


## 07:30 (11/10) – Gỡ khối thì ổ khóa đóng lại

**Lỗi:** khóa đã mở, chạm 1 khối để gỡ thì khóa vẫn mở (đặt / gỡ liên tục là mở được mọi khóa mà không cần giải).

**Sửa**

- `ClueCell.StepLock()` → `SyncLock(otherBlocks)`: khóa còn lại = `lockSteps` gốc − số khối khác đang trên lưới (khối nằm trên chính ô khóa không tính, khớp Validator "N khối khác").
- `CellModel.SyncLock` bọc lại, gọi `Changed` để `CellView` vẽ lại ổ khóa; trả về true khi vừa mở (vẫn phát `onClueUnlocked`).
- `BoardModel`: `Place` / `Remove` / `ClearBlocks` đều gọi `SyncLocks` → đặt khối thì mở dần, gỡ khối thì khóa lại. Thêm `CanPlaceReplacing(rect, replaced)`: thử gỡ khối sai rồi trả lưới về, vì gỡ có thể làm ô số khóa lại.
- `GridManager.PlaceSolved` (Hint / Reveal / Cheat đặt đè khối sai) dùng `CanPlaceReplacing` thay cho tự Release / Occupy (cách cũ không biết khóa sẽ đóng lại → gỡ khối xong mới thấy không đặt được).
- Khối đã đặt trên ô khóa giữ nguyên khi ô đó khóa lại (gỡ khối đó thì thấy ổ khóa). Lưới kín luôn đủ khối nên lúc thắng mọi khóa đã mở.
- Docs: `03_CoChe-Mechanic.md`, `HowTo_ThemMechanic.md`.

**Kiểm tra**

- `Editor/Tests/LockClueTests.cs` (mới, 5 test): đặt đủ khối thì mở; gỡ khối sau khi mở thì khóa lại và `Check` trả `Locked`, đặt lại thì mở; khối trên chính ô khóa không tính; `CanPlaceReplacing` false khi gỡ làm khóa đóng và lưới giữ nguyên; `ClearBlocks` về số bước gốc.
- EditMode: LockClueTests + HintBoosterTests + SelectionHintTests + FeelPlayModeTests 24/24 xanh, console không lỗi.

## 08:10 (11/10) – Băng HARD / EXTREME LEVEL rơi xuống rồi bay ngược lên

**Yêu cầu:** hiệu ứng băng vào level Hard / Very Hard giống video bản gốc: rơi xuống rồi đi ngược lên, không trượt chéo.

**Phân tích video** (30 fps): băng rơi thẳng từ trên xuống trong ~0.4 s, nảy quá chỗ đứng 1 chút rồi đứng; đầu lâu bật ra; giữ ~1.2 s; đầu lâu thu lại; băng chùng xuống ~0.13 s rồi vọt lên khỏi mép trên ~0.27 s. Dải sọc đi trước băng chữ cả lúc vào lẫn lúc ra; khi bay lên băng chữ lật nghiêng xuống phải.

**Sửa** `UI/HUD/DifficultyIntro.cs`

- Bỏ `offscreen` (lệch chéo trên trái) → `dropHeight` 1600: vào từ / ra về phía trên chỗ đứng.
- Vào: `DOAnchorPos` `OutBack` (dải sọc 1.1, băng chữ 1.4, trễ 0.06 s) + xoay từ `enterTilt` (-10° / 4°) về 0.
- Ra: `InBack` 1.2 (chùng xuống ~85 đơn vị rồi vọt lên), dải sọc trước băng chữ 0.06 s, xoay tới `exitTilt` (-6° / -12°) `InOutSine`; `slideOut` 0.36 → 0.42 s, `hold` 1.3 → 1.25 s. Xong thì trả góc xoay về 0.
- `Editor/UI/DifficultyBuilder.cs`: bỏ dòng gán `offscreen`. Scene không cần dựng lại (trường mới lấy giá trị mặc định).
- `ProjectStructure.md` cập nhật mô tả.

**Kiểm tra:** Play level 39 (Hard), dừng sequence ở 0.12 / 0.25 / 0.4 / 1.0 / 1.95 / 2.1 / 2.18 s và chụp: băng từ trên rơi vào, nảy quá rồi về giữa, đầu lâu hiện, chùng xuống, dải sọc vọt lên trước, băng chữ theo sau ra khỏi mép trên — khớp các khung 146 / 158 / 170 / 206 / 210 / 212 của video. Console không lỗi.

## 09:00 (12/10) – Cảnh báo Spine `Noti.png`

**Lỗi:** Spine báo "It is recommended to disable `Alpha Is Transparency` on `Premultiply alpha` textures" cho `Spine_Noti_Emoji/Noti.png` + `Noti_Material.mat`.

**Nguyên nhân:** `Noti.atlas.txt` xuất với `pma:true` (premultiplied alpha), material để `_StraightAlphaInput: 0` (đúng), nhưng import ảnh lại bật `Alpha Is Transparency` — Unity tô lại màu viền ô trong suốt, với ảnh PMA có thể ra viền sáng / tối.

**Sửa:** `Noti.png.meta` `alphaIsTransparency: 1 → 0` (giống `Spine_Freeze/Freeze.png` đã sửa trước). Không đổi material.

**Kiểm tra:** reimport ảnh + atlas, console sạch, chỉ còn log import atlas.

## 11:30 (12/10) – Coding Guidelines + Refactor Plan

**Yêu cầu:** thống nhất quy tắc code / kiến trúc (SOLID, pattern, kiến trúc "vừa đủ", không over-engineering) dùng cho mọi dự án sau, và lập kế hoạch đưa PawBlock về đúng quy tắc.

**Thêm**

- `Docs/Refactor/UnityCodingGuidelines.md` (dùng chung mọi dự án Unity): triết lý vừa đủ; SOLID; 7 pattern (khi nào dùng / không); kiến trúc module `Core (Foundation + Contracts) ← Shared ← Features ← Game`, feature không biết nhau, giao tiếp qua event / contract interface, chia feature thô, `.asmdef` theo quy mô; Bootstrap; cấu trúc thư mục theo feature; đặt tên; định dạng code; quy tắc Unity (lifecycle, serialize, **cấm Find / GetComponent / AddComponent lúc chạy**, hiệu năng); asset (Sprite Atlas, nén, `Resources`, Addressables); Prefab và UI (base popup, Canvas); test; git; checklist; code dùng lại giữa dự án (package UPM).
- `Docs/Refactor/RefactorPlan.md`: hiện trạng đo được (12 Singleton game logic, static giữ state, phụ thuộc ngược Gameplay → UI, ~58 file dùng Find / GetComponent / AddComponent, 0 namespace / asmdef, UI nằm hết trong scene...); bảng ánh xạ 199 script sang module mới; 10 giai đoạn P0–P9 (chuẩn bị → thư mục + namespace → contracts → service + Bootstrap + save theo phần có migrate v1 → v2 → bỏ Singleton → bỏ Find / GetComponent → asmdef → UI thành prefab → asset + atlas → tài liệu), mỗi giai đoạn có Thêm / Sửa / Xóa / Kiểm tra; estimate ~112.5 giờ; smoke test.

**Tác dụng:** có chuẩn chung để review code và lộ trình refactor từng bước, game chạy được sau mỗi giai đoạn. Chưa đổi code game.

**Kiểm tra:** đối chiếu bảng ánh xạ với `find Assets/_Project/Script -name '*.cs'`: đủ 199 file; số mục tham chiếu trong plan khớp guide.
