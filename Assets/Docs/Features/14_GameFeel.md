# 14 – Game feel trong màn chơi

Tổng hợp các hiệu ứng "feel" đã làm cho gameplay (nhánh `Day-7`, 10/10/2026): hiệu ứng làm gì, nằm ở script nào, số mặc định, chỉnh / tắt ở đâu, test nào kiểm tra.

Nguyên tắc chung:

- Công thức (đường cong squash, sóng ô, cao độ, combo, ô "dính"…) là **hàm thuần** trong `Gameplay/Feedback/FeelCurves.cs`. View chỉ gọi các hàm này trong tween, nên test được mà không cần Play mode.
- Hiệu ứng nghe **event có sẵn** (`GridManager`, `LevelTimer`, `EventBus`) và không sửa luật chơi. Tắt một component là tắt cả nhóm hiệu ứng của nó.
- Không thêm ảnh mới: chấm tròn, thanh viên thuốc, viền đỏ, tấm băng đều vẽ bằng code (`Gameplay/Feedback/ProceduralSprites.cs`).
- DOTween được khởi tạo sẵn 500 tween / 150 sequence lúc mở game (`Gameplay/Util/TweenSetup.cs`), tránh tự nới sức chứa giữa lúc chơi.
- Số liệu chỉnh trong Inspector. Bảng dưới ghi **giá trị mặc định**.

## Tổng quan

| # | Nhóm | Hiệu ứng | Script chính |
| --- | --- | --- | --- |
| 1 | Kéo | Bong bóng số phóng to khi hiện, thu nhỏ khi ẩn, chữ nảy khi đổi số | `Input/DragCountBubble` |
| 2 | Kéo | Bong bóng đổi màu: tím (đang kéo) / xanh (đủ) / đỏ (sai) | `DragCountBubble`, `Core/BoardModel` (`PlacementCheck.Hint`) |
| 3 | Kéo | Bong bóng bám ngón tay mượt | `DragCountBubble` |
| 4 | Kéo | Tick cao dần theo số ô + "ding" khi đủ | `Audio/GameAudioEvents` |
| 5 | Kéo | Ô số trong vùng kéo phóng to; phủ ≥ 2 ô số thì lắc | `Feedback/DragFeedback`, `Core/CellView` |
| 6 | Kéo | Ô dưới ngón tay "dính" ở ranh giới 2 ô | `Core/GridManager`, `FeelCurves.StickyCell` |
| 7 | Thả tay | Vùng sai: khung xám, co lại, mờ dần + tiếng "bụp" + rung mềm | `Input/SelectionBoxView`, `GameAudioEvents`, `GameHapticEvents` |
| 8 | Đặt khối | Squash & stretch (dẹt rồi dài, đáy đứng yên) | `Block/PlacedBlockView` |
| 9 | Đặt khối | Ô trống quanh khối nảy lan như sóng | `Feedback/GameplayFeedback` |
| 10 | Đặt khối | Hạt bụi màu mèo bung ra từ mép khối | `Feedback/BlockFeedback` |
| 11 | Đặt khối | Mèo vừa đặt đổi biểu cảm; mèo kề bên nhảy nhẹ + đổi mặt | `PlacedBlockView`, `BlockFeedback` |
| 12 | Combo | Đặt nhanh liên tiếp: âm cao dần, chữ khen mạnh / to dần | `GridManager` (đếm), `GameAudioEvents`, `GameplayFeedback` |
| 13 | Tiến độ | Thanh thời gian 3 mốc sao dưới khung LEVEL (thay thanh % ô cũ), mất sao thì sao vàng tan | `UI/HUD/TimeStarBar`, xem [15_Sao.md](15_Sao.md) |
| 14 | Thắng | Khối cuối: slow-motion + camera zoom nhẹ | `GameplayFeedback` |
| 15 | Đồng hồ | Dưới 10 s: viền đỏ đập theo giây + nhạc nhanh lên | `UI/HUD/LowTimeVignette`, `GameAudioEvents`, `AudioManager` |
| 16 | Đồng hồ | Cộng giờ: chữ "+30s" bay vào đồng hồ | `UI/HUD/TimerUI` |
| 17 | Đồng hồ | Đóng băng như bản gốc: Spine đồng hồ + bàn tay bấm nút, nổ hạt, viền `ice_effect` + tuyết; nút đếm ngược | `UI/HUD/FreezeOverlayFX`, `FreezeButtonCountdown`, `BlockFeedback` |
| 18 | Gỡ khối | Mèo nhảy bật lên rồi rơi xuống mờ dần; ô bên dưới nảy lại | `PlacedBlockView`, `BlockFeedback` |
| 19 | Booster | Icon bay ra giữa màn hình (tia sáng, chữ tên) rồi lao tới mục tiêu; Hint gợi ý bằng mèo mờ + bàn tay | `UI/HUD/BoosterUseFx`, `Booster/HintMarker` |

---

## 1. Lúc kéo

### 1.1 Bong bóng đếm ô (`Gameplay/Input/DragCountBubble.cs`)

| Hiệu ứng | Mặc định | Field |
| --- | --- | --- |
| Hiện: scale 0 → gốc, nảy nhẹ | 0.2 s, `OutBack` | `showDuration`, `showEase` |
| Ẩn: thu về 0 rồi mới tắt (vẫn giữ số cũ) | 0.14 s, `InBack` | `hideDuration`, `hideEase` |
| Số đổi: chữ nảy to thêm rồi về | +20 %, 0.16 s | `countPunch`, `countPunchDuration` |
| Màu nền theo trạng thái | Tím (màu prefab) / xanh `readyColor` / đỏ `invalidColor`, chuyển trong 0.12 s | `readyColor`, `invalidColor`, `colorDuration` |
| Bám ngón tay mượt (lerp không phụ thuộc FPS) | độ bám 22 (0 = dính cứng như cũ) | `followSharpness` |

- Đang thu nhỏ dở mà kéo lại: phóng to tiếp từ cỡ đang có.
- Lần hiện đầu đứng ngay dưới ngón tay, không trượt từ chỗ cũ.
- Trạng thái lấy từ `GridManager.SelectionHint` (= `PlacementCheck.Hint`):
  - **Ready**: thả tay là đặt được.
  - **Invalid**: chắc chắn sai (thừa ô, ≥ 2 ô số, ô số còn khóa, vùng chứa ô bị chặn, sai hướng mà đã đủ ô).
  - **Neutral**: kéo tiếp còn có thể đúng (thiếu ô, chưa có ô số, ô "?").

### 1.2 Âm tick khi kéo (`Gameplay/Audio/GameAudioEvents.cs`)

| Âm | Khi nào | Clip | Field |
| --- | --- | --- | --- |
| `DragTick` | Số ô thay đổi (không phát lúc mới chạm) | `SC_SFX_UI_Button_01` (0.13 s), vol 0.45 | `dragBasePitch` 0.9, `dragSemitones` `{0,2,4,7,9,12,14,16}` |
| `DragReady` | Vùng vừa thành Ready (cả khi chạm ô số 1) | `SC_SFX_Gameplay_Box_Created_01`, vol 0.6 | |

- Cao độ = `0.9 × 2^(nửa cung / 12)`, theo thang ngũ cung nên dễ nghe. Quá mảng thì giữ nốt cuối; co vùng lại thì thấp xuống.
- Tắt bằng ô `dragSounds`.

### 1.3 Ô số trong vùng kéo (`Gameplay/Feedback/DragFeedback.cs` + `CellView`)

| Hiệu ứng | Mặc định |
| --- | --- |
| Ô số (hoặc ổ khóa) nằm trong vùng: số phóng to | × 1.22, 0.16 s `OutBack`; ra khỏi vùng về lại trong 0.12 s |
| Vùng vừa phủ ≥ 2 ô số: các số lắc ngang 1 nhịp | 0.28 s, biên độ 0.07 world |

- Tắt bằng ô `clueHighlight` trên `DragFeedback`.

### 1.4 Ô "dính" khi kéo (`GridManager.cellStick`)

- Ngón tay còn cách tâm ô hiện tại không quá **0.5 + 0.15 ô** (mỗi trục) thì vẫn giữ ô đó, tránh vùng chọn nhảy qua lại khi ngón tay rung ở ranh giới.
- Đặt `cellStick` = 0 để tắt.

### 1.5 Thả tay ở vùng sai

| Phần | Mặc định | Ở đâu |
| --- | --- | --- |
| Khung chuyển xám ngay, co còn 82 %, mờ dần rồi mới tắt | 0.24 s, xám `(0.55, 0.55, 0.62)` | `SelectionBoxView.HideRejected` (`rejectDuration`, `rejectShrink`, `rejectGray`) |
| Tiếng "bụp" | `DragCancel` = `SC_SFX_UI_Popup_Close_01`, pitch 0.78–0.85, vol 0.5 | `GameAudioEvents` (nghe `onInvalidMove`) |
| Rung mềm | `HapticType.Soft` | `GameHapticEvents` |

- Bắt đầu kéo lượt mới giữa lúc khung đang co xám: khung hiện lại ngay với màu thường.

---

## 2. Lúc đặt khối

### 2.1 Squash & stretch (`PlacedBlockView.PopIn`, `FeelCurves.Squash`)

- Công thức: `d = amount · (1 − p)² · sin(p · π · wobbles)`, scale = `(1 + d, 1 − d)`.
- Diễn biến: dẹt (khoảng 8 %) → dài (khoảng 2 %) → đứng yên.
- Tâm khối hạ theo chiều cao (`FeelCurves.BottomAnchorOffset`) nên **đáy khối không xê dịch**.
- Mặc định: `squashDuration` 0.34 s, `squashAmount` 0.12, `squashWobbles` 2.5.

### 2.2 Sóng ô quanh khối (`GameplayFeedback.PlayRipple`)

- Ô trống cách khối 1–2 vòng (tính cả chéo) nảy lên.
- Vòng 1 phồng 6 %, vòng 2 khoảng 3 %.
- Thời điểm: trễ 0.04 s, mỗi vòng ra ngoài trễ thêm 0.05 s, mỗi lần nảy 0.22 s.
- Field: `ripple`, `rippleRadius`, `rippleAmount`, `rippleStartDelay`, `rippleStepDelay`, `rippleDuration`.

### 2.3 Hạt bụi (`BlockFeedback.BurstDust`)

- 12 hạt tỏa ra 4 phía từ mép khối, màu mèo pha 35 % trắng.
- Hạt bay ra chậm dần, nhỏ lại và mờ đi trong khoảng 0.45 s.
- Hạt nằm trên khối và dưới khung chọn (sorting 26).
- Dùng pool tạo bằng code (không cần prefab).
- Field: `dust`, `dustCount`, `dustSize`, `dustDistance`, `dustDuration`, `dustWhiten`.

### 2.4 Mèo phản ứng

- **Mèo vừa đặt**: mèo xem trước thành khối thật thì đổi sang biểu cảm khác (`PlacedBlockView.ReactFace`).
- **Mèo kề bên** (cạnh hoặc chéo khối mới): nhảy lên 0.12 world trong 0.26 s, lần lượt trễ 0.06–0.12 s, rồi đổi biểu cảm (`PlayNudge`).
- Tắt bằng `BlockFeedback.neighbourNudge`.

---

## 3. Combo, tiến độ, khối cuối

### 3.1 Combo

- **Đếm**: `GridManager.Combo` dùng `ComboCounter`. Lần đặt sau cách lần trước ≤ 2.5 s (`comboWindow`) thì combo + 1, quá thì về 1. Gỡ khối hoặc đổi lưới thì đứt combo.
- **Âm**: âm đặt khối cao thêm 6 % mỗi bậc, tối đa × 1.3 (`GameAudioEvents.comboPitchStep`, `comboPitchMax`).
- **Chữ khen**: bậc theo `FeelCurves.PraiseTier`, mỗi bậc chữ to thêm 12 % (`comboPraiseScaleStep`).

| Combo | Lời khen | Khi nào hiện |
| --- | --- | --- |
| 1 (đặt lẻ) | Nice / Like | Như cũ: khối ≥ 2 ô, xác suất 60 % |
| 2 | Great / Wow | Lần nào cũng hiện |
| 3 | Beauty / Amazing | Lần nào cũng hiện |
| 4+ | Perfect | Lần nào cũng hiện |

### 3.2 Thanh thời gian 3 mốc sao (`UI/HUD/TimeStarBar.cs`, con `TimeBar` của `LevelFrame`)

Thay thanh % ô đã lấp (`LevelProgressBar`, đã xóa) vì bản gốc đặt thanh thời gian có sao ở đúng chỗ này. Chi tiết: [15_Sao.md](15_Sao.md).

### 3.3 Khối cuối (`GameplayFeedback.OnGameWin`)

- **Slow-motion**: `Time.timeScale` xuống 0.35 rồi nhanh dần về tốc độ cũ trong 0.6 s thời gian thực. Tốc độ cheat x2 / x5 đang bật vẫn được giữ.
- **Camera zoom**: zoom vào 4 % rồi trả lại đúng cỡ cũ trong 0.7 s.
- Component bị tắt giữa chừng thì tốc độ và cỡ camera vẫn được trả lại.
- Field: `winSlowMo`, `slowMoScale`, `slowMoDuration`, `winZoom`, `winZoomDuration`.

---

## 4. Đồng hồ

### 4.1 Sắp hết giờ

- **Viền đỏ** (`UI/HUD/LowTimeVignette.cs`, gắn trên `Canvas_HUD`):
  - Viền đỏ sát mép màn hình, nằm dưới các nút HUD và không chắn bấm.
  - Độ đậm nền 0.14; mỗi giây đập lên 0.4 rồi lắng về (0.45 s), cùng nhịp với icon đồng hồ và rung Tick.
- **Nhạc** (`GameAudioEvents` → `AudioManager.SetMusicPitch`): nhạc nền nhanh lên × 1.08 (`lowTimeMusicPitch`), về × 1 khi hết điều kiện.
- **Tắt khi**: đóng băng, được cộng giờ lên trên ngưỡng, hết giờ, thắng / thua, đổi level.
- Cả hai nghe thêm `onTimeChanged`, nên cheat nhảy thẳng qua ngưỡng 10 s vẫn bật / tắt đúng.

### 4.0 Chạm để bắt đầu tính giờ (`LevelTimer.waitForFirstTouch`)

- Level mới / chơi lại: đồng hồ đứng ở tổng thời gian; chữ giờ + icon nhấp nháy (alpha 1 ↔ 0.4, chu kỳ 1.1 s).
- Chạm màn hình lần đầu khi đã thao tác được (qua intro, không có popup) → đồng hồ chạy, chữ rõ lại. Chạm trúng nút cũng tính.
- Màn luyện tập không chờ. Tắt bằng `waitForFirstTouch` trên `LevelTimer`; `StartNow()` để bỏ chờ bằng code.

### 4.2 Cộng giờ "+Ns" (`TimerUI.FlyAddedText`)

- Chữ "+30s" (màu xanh lá, cùng font với đồng hồ) phóng to giữa màn hình (0.35 s `OutBack`), giữ 0.25 s.
- Sau đó bay vòng cung vào chữ giờ, nhanh dần và nhỏ lại (0.45 s); chạm tới thì icon đồng hồ nảy.
- Chạy theo thời gian thực.

### 4.3 Đóng băng (như bản gốc – `UI/HUD/FreezeOverlayFX.cs`)

Dựng theo cấu trúc `FreezeOverlayFX` của game gốc (đọc từ `E:\PawBlock\ExportedProject`), dùng đúng asset gốc:

| Object (`Canvas_HUD/FreezeOverlayFX`) | Nội dung |
| --- | --- |
| `HandStartFreeze` | Spine `SkeletonGraphic` – `Spine_Freeze/Freeze_SkeletonData` (Spine 4.1.24), animation `Start` 2.37 s: đồng hồ mèo hiện ra, bàn tay bấm nút; giữa màn hình, lệch xuống 26; chạy thời gian thực |
| `Booster_FreezeFX` | Prefab `Prefabs/Gameplay/Freeze/Booster_FreezeFX` (`UIParticle` + 4 hệ hạt gốc: `Twinkle`, `Spark_Blue`, `snowflake`, `Circle_Blue`) – nổ lúc bàn tay bấm |
| `FreezeBorders` | CanvasGroup mờ vào / ra 0.4 s: `Snow` (prefab `FreezeSnow`: 2 `Spark_White` + `snowflake` rơi lặp) + `Border` (ảnh `ice_effect` phủ toàn màn) |

- Bấm Freeze → animation `Start`, khóa thao tác. Sự kiện Spine **`Press` (1.67 s)** → nổ `Booster_FreezeFX`, mở thao tác; cùng lúc đồng hồ đóng băng (`BoosterManager.freezeEffectDelay` = 1.67) → `FreezeBorders` hiện. Hết animation → ẩn đồng hồ.
- Hết đóng băng (hoặc ván kết thúc) → `FreezeBorders` mờ ra 0.4 s rồi tắt.
- Nút Freeze: `FreezeButtonCountdown` (đĩa tối, số giây xanh lá, vòng đếm ngược). Băng trên đồng hồ HUD: `FreezeIndicator`. Mèo run lần lượt: `BlockFeedback` (bản gốc không có).
- Hạt dùng thông số y nguyên bản gốc (chép YAML ParticleSystem), material `09_Shaders_Materials/Materials/{Twinkle, Spark_White, snowflake, Circle_Blue}`.

---

## 5. Gỡ khối (`PlacedBlockView.PlayRemove` + `BlockFeedback.OnBlockRemoved`)

- 35 % thời gian đầu: mèo bật lên 0.35 world, người dài ra.
- Phần còn lại: rơi xuống thấp hơn chỗ cũ 0.25 world (nhanh dần), co còn 60 % và mờ dần. Tổng 0.34 s.
- Các ô bên dưới nảy 8 % sau 0.12 s.
- Field: `removeDuration`, `removeHopHeight`, `removeFallDepth`, `removeShrinkTo`; `BlockFeedback.removeBump*`.

---

## 6. Booster

### 6.1 Dùng booster (`UI/HUD/BoosterUseFx.cs`, trên `Canvas_HUD`)

| Bước | Thời gian | Hiệu ứng |
| --- | --- | --- |
| Bật ra | 0.42 s | Icon bay từ nút ra giữa màn hình, phóng × 2.3 (`OutBack`); chùm tia sáng vàng xoay sau lưng; chữ "HINT!" / "FREEZE!" / "REVEAL!" bật lên; màn tối 30 % |
| Giữ | 0.45 s | |
| Lao tới mục tiêu | 0.32 s | Icon nhỏ lại, lao tới: Hint → tâm vùng gợi ý, Reveal → ô số vừa chạm, Freeze → đồng hồ; tia, chữ, lớp tối tan |
| Tới nơi | 0.28 s | Lóe sáng tròn tại chỗ + rung nhẹ (Light) |

- Tổng ≈ 1.2 s = `BoosterManager.effectDelay`: mèo mờ của Hint và khối của Reveal hiện **sau khi icon tới**.
- Trong lúc icon bay: khóa thao tác (`InputGate`) nên đồng hồ cũng dừng – không mất giờ vì hiệu ứng.
- Chạy theo thời gian thực. Nút booster vẫn nảy (`BoosterButton`), âm Booster + rung Medium như cũ.

### 6.2 Hint chỉ gợi ý (`Booster/HintMarker.cs` + `Effects/HintBooster.cs`)

- Mèo mờ (lấy từ pool khối, như mèo xem trước, màu ngẫu nhiên) nhấp nháy alpha 0.6 ↔ 0.25, chu kỳ 1 s, đúng vùng cần đặt.
- Bàn tay (bản sao `TutorialHand`) diễn kéo chéo từ góc trên trái → góc dưới phải của vùng, lặp lại; ẩn khi người chơi đang kéo.
- Chọn vùng (`HintBooster.PickRegion`): bỏ vùng có ô số còn khóa → ưu tiên vùng chưa bị khối nào đè → vùng nhỏ nhất.
- Tắt khi đặt đúng vùng, đặt khối khác đè lên vùng, đổi level / lưới mới, ván dừng. Đang hiện thì nút Hint sáng và bấm lại không tốn lượt.

## Gắn trong scene

| Component | GameObject |
| --- | --- |
| `DragFeedback`, `BlockFeedback` | `Manager` (cùng `GameplayFeedback`) |
| `LowTimeVignette` | `Canvas_HUD` |
| `TimeStarBar` (dựng bằng **Build Stars**) | `Canvas_HUD/UIZone/TopPanel/LevelFrame/TimeBar` |
| `BoosterUseFx` | `Canvas_HUD` |
| `FreezeOverlayFX` (+ con `HandStartFreeze`, `Booster_FreezeFX`, `FreezeBorders`) | `Canvas_HUD/FreezeOverlayFX` |
| `FreezeButtonCountdown` | nút Freeze (`Booster2`) |
| `HintMarker` | `Manager` (cùng `BoosterManager`; thiếu thì `BoosterManager` tự thêm) |

**Âm mới** (`DragTick`, `DragReady`, `DragCancel`) được thêm bằng **Tools → PawBlock → Builders → Setup Audio**. Lệnh này chỉ thêm âm còn thiếu, âm cũ giữ nguyên.

Viền đỏ, chữ "+Ns" và tấm băng đều tự tạo lúc chạy. Nếu dựng lại HUD bằng builder mà mất `LowTimeVignette`, chỉ cần gắn lại component; không phải gán reference nào.

## Test

Mở **Window → General → Test Runner → tab EditMode** (script game chưa tách asmdef nên test nằm ở `Script/Editor/Tests`). Tổng **46 test**:

| File | Kiểm tra |
| --- | --- |
| `FeelCurvesTests` (31) | Squash: dẹt trước, không vượt biên độ, tắt dần, đáy đứng yên. Ô nảy, vòng ô, độ nảy / độ trễ sóng. Cao độ tick. Ô "dính". Combo (cửa sổ, reset), cao độ combo, bậc lời khen. |
| `SelectionHintTests` (8) | Neutral / Ready / Invalid qua `BoardModel.Check`: thiếu / đủ / thừa ô, ô số 1, chưa có ô số, 2 ô số, ô khóa, ô Void, sai hướng, ô "?". |
| `FeelPlayModeTests` (7) | Chạy Play mode thật, mỗi frame cố định 1/60 s: sóng ô (trễ, nhẹ dần, về 1); ô tắt giữa lúc nảy; squash khối từ prefab thật; khung co xám rồi tắt và lượt kéo mới hết xám; ô số phóng to / về / lắc xong về chỗ; bong bóng trượt mượt tới ngón tay; gỡ khối nhảy lên rồi rơi, mờ dần, gọi `onDone`. |

- Test Play mode mở scene trống riêng rồi trả lại scene cũ. Nếu scene đang có thay đổi chưa lưu, 3 test này tự bỏ qua để không làm mất thay đổi.
- Đã thử cố tình làm sai công thức squash (đảo dẹt ↔ dài): test công thức và test Play mode đều báo đỏ.

## Chưa làm / lưu ý

- **Chưa nghe thử âm thanh thực tế, chưa thử trên điện thoại.** Số liệu là mức khởi đầu, cần chơi thử để chỉnh.
- Tấm băng phủ theo khung chữ nhật của lưới, nên lưới có ô Void ở góc thì băng vẫn phủ lên chỗ trống đó.
- Mèo kề bên chỉ "giật mình" (nhảy + đổi mặt), chưa có hình mèo quay đầu nhìn sang vì art không có hướng nhìn.
- Combo đếm cả khối do booster Hint / Reveal đặt.
