# 31 – Viền tối cho số đếm khi kéo và số booster

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | – |
| Thời gian push | – |
| Cập nhật lần cuối | 2026-10-23 11:03 |

## 13:55 – Số trắng ở bong bóng đếm và booster có viền tối

**Nguyên nhân**: chữ số ở `DragCountBubble` và `BoosterButton` dùng material mặc định `Fredoka-Bold SDF Material` (không viền). Material này cũng là material của số trên khối đã đặt (`PlacedBlock` → `Number`), nên thêm viền vào đó thì số trên khối cũng có viền.

**Sửa**: dùng material có sẵn `Shared/Fonts/Fredoka-Bold SDF - Cell Number.mat` (viền + bóng `#120A2E`, giống số trong ô lưới), không đụng material mặc định.
- `Gameplay/Prefabs/DragCountBubble.prefab` → `Count`.
- `Gameplay/Prefabs/BoosterButton.prefab` → `Count/Text` (số lượng) và `Lock/Level` ("LV n" khi khóa). 3 booster trong `GameplayHUD` theo prefab.
- Scene `Main`: instance `DragCountBubble` có override font / material cũ → bỏ override để theo prefab.

**Giữ nguyên**: `PlacedBlock` → `Number` vẫn material mặc định (không viền).

**Kiểm tra**: Play level 36, bật tạm bong bóng đếm "21" (`Captures/count_outline_after.png`): số trên bong bóng và số booster có viền tối. Console không lỗi.

## 14:10 – Bong bóng đếm không ra ngoài bảng

**Hiện tượng**: kéo vùng chọn ở cột / hàng sát mép, bong bóng (lệch lên trái so với ngón tay) bị đẩy ra ngoài bảng, mất một nửa.

**Sửa**
- `DragCountBubble.Show(..., Rect clampArea = default)`: có vùng thì vị trí đích của bong bóng bị kẹp sao cho **cả bong bóng** (nửa kích thước nền khi hiện đủ cỡ) nằm trong vùng; ngón tay ra ngoài thì bong bóng chạm mép và dừng. Không truyền vùng thì như cũ.
- `DragAndDropController.BoardArea()`: vùng bảng world = `Width × Spacing` × `Height × Spacing`, tâm ở gốc (theo `GridGeometry`), truyền vào `Show`.

**Kiểm tra**: Play level 36 (lưới 8×8, vùng ±4.8), gọi `Show` với ngón tay ngoài mép phải (x = 8.5): bong bóng dừng sát mép phải bảng (`Captures/drag_count_clamp.png`). Console không lỗi. Chưa thử kéo tay thật (giả lập `StartDrag` qua code không bắt đầu được vùng chọn).

## 14:25 – Bong bóng đếm được lên cao hơn bảng 1 ô

**Lý do**: kẹp đúng bằng bảng thì kéo ở hàng trên cùng, bong bóng không lên khỏi bảng được nên nằm đè lên vùng đang chọn.

**Sửa** `DragAndDropController.BoardArea()`: mép trên vùng kẹp nới thêm 1 ô (`Spacing`). Chiều ngang và mép dưới giữ đúng bằng bảng (không tràn màn hình).

**Kiểm tra**: Play level 36, gọi `Show` với ngón tay ngoài góc trên phải (9, 9): vùng (-4.8, -4.8) cao 10.8; bong bóng dừng trên hàng trên cùng 1 ô, sát mép phải bảng, không đè thanh sao (`Captures/drag_count_clamp_top.png`). Console không lỗi.

## 14:49 – CellSpacing 1.2 → 1.1: khối / khung chọn phủ kín ô, hết vệt sáng giữa các khối

**Nguyên nhân** (sau khi giảm `GridSpawner.CellSpacing` 1.2 → 1.1)
- Ô nền `CellBG` vẽ cố định 169 px / ppu 150 = **1.127** world (có viền sáng 5 px), không theo khoảng ô. Khối 1 ô = 1.1 × 0.96 = **1.056** → viền ô lòi ra quanh khối (khối "không phủ kín").
- Các ô nền chồng lên nhau (1.127 > 1.1), nên khe giữa 2 khối kề nhau chính là viền sáng của ô bên dưới (thêm Global Light 2D) → "luồng sáng".
- Khung chọn thụt cố định 0.1 mỗi mép → 1 ô chỉ còn 0.9, nhỏ hơn ô nền.

**Sửa**
- `GridSpawner.CellGap = 0.08`: ô nền vẽ đúng `CellSpacing - CellGap` (1.02 khi spacing 1.1), gọi `CellView.SetVisualSize` lúc tạo ô (phóng `Visual`, giữ tỉ lệ). Đổi spacing sau này thì ô tự theo. Khe ô 0.08 lớn hơn khe khối (1.1 × 0.04 = 0.044) nên khối luôn trùm kín ô.
- `SelectionBoxView`: bỏ field `inset` (0.1), mỗi mép thụt đúng `CellGap / 2` → khung phủ khít ô nền.

**Kiểm tra**: Play level 36, đặt 5 khối bằng `GridManager.PlaceSolved` (`Captures/cell_fill_after.png`): khối trùm kín ô, khe giữa khối kề nhau là nền tối, không còn viền sáng. Bật khung chọn 3×2 (`Captures/selection_fill_after.png`): phủ khít 6 ô. Console không lỗi. Test `FeelPlayModeTests` chỉ so tỉ lệ khung chọn, không phụ thuộc inset.

## 16:25 – Màn thắng: nút x2 nổ ra coin, Continue vào giữa; level > 15 về Menu; nút không xám; art nút Snacky Dash

**Yêu cầu**: x2 (quảng cáo) xem xong thì nút x2 nảy to rồi nổ ra coin bay lên ô coin và biến mất, Continue trượt vào giữa; Continue nhận coin rồi đi tiếp (level 1–15 sang level kế, từ 16 về Main Menu); nút không còn xám khi chưa cho bấm; art nút lấy từ Snacky Dash.

**Sửa**
- `WinPopup`:
  - Bấm x2 → `IAdsService.Show` (giả lập `FakeAdsService`) → `PopDouble()`: x2 phóng `doublePopScale` rồi thu về 0 và tắt, coin bung ra từ nút bay vào ô coin (cộng thêm 1 lần thưởng, tổng = gấp đôi), Continue `DOAnchorPosX(0)` vào giữa (`nextSlideDuration`). Bấm x2 2 lần / lúc đang chiếu quảng cáo chỉ tính 1 lần.
  - Continue → `Claim(CoinReward)` như cũ (huy hiệu thu lại, coin bay vào ô coin). Xong: Daily về Menu như cũ; level thường `CurrentLevelNumber > menuAfterLevel` (15, chỉnh trong Inspector) thì `ShowMenu()` rồi `NextLevel()` (level kế nạp lúc đang ở Menu nên bị tạm dừng, PLAY chơi tiếp level kế); còn lại `NextLevel()`.
  - Bỏ `SetButtons` (tắt `interactable`): 2 nút luôn bấm được, có hiệu ứng nhấn `ButtonPress`; chưa cho phép (intro, đang chờ quảng cáo, x2 đang nổ, đã nhận) thì cờ `_ready` / `_claimed` / `_doubled` chặn, không làm gì.
  - Tách đoạn coin bay thành `InsertCoinFlight(seq, start, from, to, count, delay)` dùng chung cho x2 và Continue. Mở lại popup: Continue về chỗ cũ, x2 bật lại.
- `WinPopup.prefab`: bỏ `UIGrayscaleWhenDisabled` ở 2 nút. Nút dựng như Snacky Dash: nền `btn_slant_base` (9-slice ngang), mặt `Face` = `btn_slant_front` + dải sáng `Highlight`, viền `Outline` (`ButtonPressIgnore`, đứng yên khi nhấn), tô màu bằng Image: Continue xanh lá (#2D8700 / #399F00 / #4FCD04 / #78F024), x2 cam vàng (#EF9400 / #F39600 / #FFBF0D / #FFE24C); icon quảng cáo `btn_video_rv` (bảng clapperboard xanh). Chữ / số coin giữ Fredoka như cũ. Nút 390×142.
- Asset mới ở `Features/Gameplay/Art/UI/Buttons/`: `btn_slant_base/front/highlight/outline.png` (cắt từ atlas `A_Common` của Snacky Dash, viền 9-slice lấy theo sprite gốc), `btn_video_rv.png` (`Icon_RV`). Repo Snacky Dash đầy đủ để ở `E:\Temp\Snacky_Dash` (không chép phần chưa dùng vào project).

**Kiểm tra**: Play, thắng giả lập (`GridManager.onGameWin`):
- Level 5 → x2: coin 795 → 820 (+25), x2 biến mất, Continue vào giữa (`Captures/win_buttons_intro.png`, `Captures/win_buttons_after_x2.png`) → Continue +25 → sang level 6, đang chơi.
- Level 3 → Continue ngay → sang level 4.
- Level 16 → Continue ngay → về Menu, nút PLAY "Level 17" (`Captures/win_level16_menu.png`).
- Level 16 → x2 bấm 2 lần (chỉ cộng 1 lần, `Captures/win_level16_after_x2.png`) → Continue → về Menu, level 17.
- Console không lỗi / cảnh báo. Sau khi thử đã trả save về level 39, coin 795. Chưa thử chạm tay thật trên máy (bấm qua `onClick`).

## 17:08 – Màn thắng theo bố cục Snacky Dash: số coin nhận được dưới huy hiệu mèo

**Yêu cầu**: giống UI Victory của Snacky Dash: x2 chỉ ghi "x2" + coin, Continue chỉ chữ "Continue"; số coin nhận được hiện dưới con mèo.

**Sửa**
- `WinPopup.prefab`:
  - Thêm `RewardRow` dưới `Medallion` (y -285): đống coin `icon_coin_3` + chữ "+N" (Fredoka 110, material `Outline Red Thick`), `HorizontalLayoutGroup` + `ContentSizeFitter`.
  - x2 bên trái (-215), Continue bên phải (215) như Snacky. Bỏ hàng coin + số (`Amount`) trên 2 nút. x2: icon quảng cáo + "x2" (76) + đống coin nhỏ `CoinPile` (`icon_coin_2_0`). Continue: chữ "Continue" (66) ở giữa.
- `WinPopup`:
  - `nextAmountText` / `doubleAmountText` → `rewardRow` / `rewardText`. "+N" bật cùng 2 nút, mỗi sao đập xuống thì cộng `coinsPerStar` và nảy.
  - x2 xem xong: nút nảy to rồi nổ ra `doubleBurstCoins` (6) đồng coin bay vào hàng "+N" (chưa vào ví); chạm thì số nhân đôi và nảy mạnh. x2 biến mất, Continue trượt vào giữa.
  - Continue: huy hiệu và hàng "+N" thu lại, coin bung ra **từ hàng "+N"** bay vào ô coin. Vào ví 1 lần: thưởng thường, hoặc gấp đôi nếu đã xem x2.
  - `InsertCoinFlight` nhận đích và callback từng đồng chạm, dùng chung cho 2 kiểu bay.

**Kiểm tra**: Play, thắng giả lập. `Captures/win_reward_row_intro.png` (+25 dưới mèo, 2 nút kiểu Snacky), `Captures/win_reward_row_after_x2.png` (+50, ví vẫn 795, Continue ở giữa).
- Level 5 → x2 → Continue: ví 795 → 845 (+50), sang level 6.
- Level 16 → Continue ngay: +25, về Menu, level 17.
- Level 16 → x2 → Continue: "+50", +50, về Menu.
- Level 3 → Continue ngay: +25, sang level 4.
- Console không lỗi / cảnh báo. Thử xong trả save về level 39, coin 795. Chưa chạm tay thật (bấm qua `onClick`).

## 2026-10-22 09:13 – Cơ chế mới: số mơ hồ (">5", "<5", "E" / "O", "3/5")

**Ý tưởng**: làm level khó hơn bằng cách **bớt thông tin**: ô số không cho đúng số ô mà chỉ cho 1 điều kiện. Người chơi phải xét nhiều kích thước khối cho cùng 1 số.

**Thêm**
- `Scripts/Board/AmbiguityRules.cs`: luật dùng chung `Accepts` (BoardModel.Check, BoardModel.IsSolved, ShikakuSolver), `MinArea` / `MaxArea` (phản hồi kéo thiếu / thừa), `Label` (chữ trên ô), `Describe` (Editor), `TryMake` (tạo điều kiện luôn chứa số thật).
- `LevelDefinition`: enum `ClueAmbiguity` (None, AtLeast, AtMost, Even, Odd, Either), `ClueMechanic.ambiguity` + `ambiguityValue` (ngưỡng / giá trị thứ hai). Mặc định tắt, level cũ không cần sửa. `value` vẫn là số ô thật của lời giải.
- Popup giới thiệu `MechanicIntro.Kind.Ambiguous` "Fuzzy Number" + ảnh `Art/UI/Popups_Dialogs/popup_tutorial_fuzzy.png` (vẽ bằng Pillow cùng kiểu ảnh Corner, gán `ambiguousSprite` trên `Manager` trong `Main`).
- `MechanicKind.Fuzzy` + `TryFuzzy` trong Mechanic Injector (giới thiệu level 32, 2 level, 1–2 số / level; level giới thiệu chỉ dùng a/b và >n). Level Hard ưu tiên "?" → số mơ hồ → khóa → Void.
- Test `FuzzyClueTests` (9 test): luật từng loại, phản hồi kéo, thắng khi khối thỏa điều kiện dù khác số thật, solver phải phủ kín, đếm nhiều lời giải, `TryMake`, nhãn.

**Sửa**
- `ClueCell`: `Label`, `MinArea`, `MaxArea`, `AcceptsArea`. `CellView` / `PlacedBlockView` hiện `Label` thay cho số.
- `BoardModel`: kiểm tra số ô bằng `AcceptsArea`; vùng sai chắc chắn khi số ô ≥ `MaxArea`; `IsSolved` chấm theo điều kiện (lời giải khác vẫn thắng nếu mọi khối thỏa).
- `ShikakuSolver`: lọc ứng viên bằng `Accepts`; level có số mơ hồ bật kiểm tra phủ kín (như ô "?").
- `DragFeedback`, `GameHapticEvents`: "thiếu ô" so với `MinArea`.
- `LevelValidator`: số mơ hồ bật kiểm tra "không cần đoán"; luật dữ liệu (không kèm "?", phải chứa số thật, Either khác số thật). `LevelDifficultyAnalyzer` đếm `fuzzy`, trọng số `wFuzzy` = 3 (`LevelDifficultySettings`, cột CSV). `LevelEditorWindow`: trường "Số mơ hồ" + ngưỡng, ký hiệu trên lưới. `LevelGeneratorWindow`: hiện điều kiện trong xem trước.
- Atlas `Fredoka-Bold SDF`: thêm `<` `>` (Fredoka không có glyph ≥ ≤ nên nhãn dùng ">5" = từ 6 ô, "<5" = tới 4 ô).
- `MechanicInjectorSettings.asset`: thêm rule Fuzzy.
- Database: **chỉ sinh lại 42 level có số mơ hồ theo lịch** (32, 33, 35–38, 44, 48, 66, 68, 108, 112, 115, 118, 122, 127–129, 140, 143, 145, 154, 158, 160, 161, 166–168, 172, 174, 175, 178, 184, 188, 190, 193, 195–198, 200, 201), cả 42 gắn được. Các level khác giữ nguyên (không chạy Áp dụng toàn bộ).
- Docs: `03_CoChe-Mechanic.md` (bảng cơ chế, luật dữ liệu, lịch), `DifficultyFormula.md` (mục 9).

**Kiểm tra**
- 82/82 test EditMode `PawBlock.Gameplay.Tests` qua. Console không lỗi.
- Validator cả database: chỉ còn 1 lỗi **có sẵn từ trước** ở level 51 (`Level_162`, tổng các số 70 ≠ 81 ô; file không bị sửa lần này).
- Play: level 32 hiện popup "Fuzzy Number" (`Captures/fuzzy_intro_popup.png`), ô "1/4" (`Captures/fuzzy_level32.png`); ô "1/4" đặt 1 ô được, 2 ô báo thiếu; giải hết bằng solver → thắng (`Captures/fuzzy_level32_solved.png`). Level 35 "O", "6/8" (`Captures/fuzzy_level35_EO.png`), level 36 ">6" (`Captures/fuzzy_level36-1.png`).
- Lần gắn đầu, 1 ô vừa có vạch cạnh vừa "EVEN" (nhãn cũ) bị vạch đè chữ → Injector không gắn số mơ hồ chung ô có dấu góc / hướng (và ngược lại), sinh lại 42 level.
- Save sau khi thử: trả level hiện tại về 42, bỏ cờ đã xem popup số mơ hồ. Level 32 đã được thắng 1 lần lúc thử (sao của level 32 có thể đã lưu).
- Chưa thử kéo tay thật, chưa chạy lại gợi ý Hard / Very Hard.

**Sửa thêm (theo góp ý)**: nhãn chẵn / lẻ "EVEN" / "ODD" dài → đổi thành **"E" / "O"** cỡ chữ như số thường; popup giải thích "E an even count, O an odd count"; vẽ lại ảnh popup ("E" thay "EVEN"). Test nhãn sửa theo, 82/82 qua. Play level 35: popup mới (`Captures/fuzzy_intro_popup_EO.png`), ô "O" (`Captures/fuzzy_level35_EO.png`). Save trả lại như cũ sau khi thử.

## 09:22 – Nhãn số mơ hồ thu nhỏ theo bề rộng (không chờm ra khỏi ô)

**Hiện tượng**: "9/11" trên khối mèo chờm ra ngoài ô số; ">12" / "<13" to hơn ô (nhãn cũ chỉ thu cố định 70–80%).

**Sửa** `AmbiguityRules.Label`: tính bề rộng thật của nhãn theo advance từng glyph Fredoka-Bold (đo bằng fontTools: "1" 379, "5" 493, "/" 489, "<" ">" 583, ...), nhãn rộng hơn 1000 / 1000 em (hơi hẹp hơn số "25") thì thu `<size=%>` cho vừa: ">5" 92%, ">12" 65%, "3/5" 64%, "9/11" 56%, "12/14" 42%. "E" / "O" và số thường giữ 100%. Dùng chung cho ô số và số trên khối mèo.

**Kiểm tra**: test nhãn thêm kiểm tra chữ hiện ra và thứ tự cỡ (82/82 qua). Play level 33: "9/11" trên khối mèo nằm gọn trong ô (`Captures/fuzzy_label_fit_level33.png`); level 166: ">7", "E" vừa ô, cỡ ngang số "20" (`Captures/fuzzy_label_fit_level166-1.png`). Save trả về level 33 như trước khi thử.

## 09:30 – Màn thắng: x2 làm "+N" tăng dần theo từng coin, kèm nảy và rung

**Yêu cầu**: khi bấm x2, coin bay vào chỗ tổng vàng "+N" thì số tăng dần theo từng đồng (không nhảy 1 lần), mỗi đồng chạm có rung nhẹ và nảy như coin bay lên ô coin trên cùng.

**Sửa** `WinPopup.PopDouble`: mỗi đồng chạm hàng "+N" thì số tăng 1 phần, chia đều từ thưởng thường tới gấp đôi (đồng cuối khớp đúng). Mỗi đồng chạm có `SoundId.Coin`, rung `HapticType.Coin` (tick nhẹ, cùng loại với coin bay vào ô coin trên cùng) và hàng "+N" nảy nhẹ (punch 0.15); đồng cuối nảy mạnh (0.45). Rung dồn đã có `Haptics.MinInterval` (30 ms) chặn; các đồng chạm cách nhau khoảng 60 ms. Sau đồng cuối luôn đặt lại đúng số gấp đôi (phòng thiếu coin trong pool). Continue giữ như cũ: coin bay lên ô coin trên cùng, ô đó đã đếm dần + rung + nảy từ trước.

**Kiểm tra**: Play, level 5 thắng giả lập → x2: "+N" đổi 25 → 29 → 33 → 37 → 42 → 46 → 50, mỗi bước cách khoảng 0.06 s (log `TMPro_EventManager.TEXT_CHANGED_EVENT`) → Continue: ví +50, sang level 6. Console không lỗi / cảnh báo. Thử xong trả save về level 36, coin 1265 (save lúc bắt đầu). Rung chỉ kiểm tra được lời gọi, chưa thử trên máy thật.

## 09:38 – Cơ chế mới: sương mù

**Luật**: mảng ô phủ mây che hết nội dung (số, khóa, dấu, kể cả việc có số hay không), không kéo khối vào được. Đặt 1 khối kề cạnh (hoặc đè) 1 mảng sương thì **cả mảng** (các ô sương liền cạnh) tan, không quay lại khi gỡ khối. Chéo góc không tính.

**Thêm**
- `CellType.Fog = 2` (ô chơi được, lưu trong `cellOverrides` như Void). `Scripts/Board/FogRules.cs`: `Cells`, `HasFog`, `Clears` (mảng nào tan khi đặt khối), `Patches`, `Overlaps` – dùng chung BoardModel và Validator.
- `CellModel.IsFogged` / `SetFog`; `BoardModel.Check` trả `PlacementResult.Fogged` (kiểm tra trước ô số để không lộ số dưới sương), `IsAreaFree` chặn ô sương (khung kéo dừng ở mép sương), `Place(..., fogCleared)` làm tan sương.
- `CellView` + `Cell.prefab`: lớp `Visual/FogCover` (sprite `Art/Grid/Grid_Cells/FogCover.png` vẽ bằng Pillow, cùng cỡ `CellBG` 169 px / ppu 150, sortingOrder 8). Còn sương thì ẩn nội dung; tan thì lớp mây phồng 1.35 lần + mờ dần trong 0.4 s, số hiện dần.
- Popup "Fog" (`MechanicIntro.Kind.Fog`, ảnh `popup_tutorial_fog.png`, gán `fogSprite` trong `Main`).
- `MechanicKind.Fog` + `TryFog` (Injector): mảng chữ nhật 2x2 (level thường tới 3x3) phủ ít nhất 1 ô số, không đè Void / sương cũ; giữ nếu Validator qua. Giới thiệu level 40, 2 level, 1–2 mảng. Level Hard: "?" → số mơ hồ → khóa → sương → Void.
- Level Editor: công cụ "Sương" (bấm / kéo bật tắt, giữ số / khối của ô), ô sương tô tím. Level Generator: xem trước tô tím ô sương.
- Test `FogTests` (5): chặn kéo vào sương không lộ số, đặt kề tan cả mảng, gỡ khối không quay lại, chéo không tan, tách mảng.

**Sửa**
- `LevelValidator`: luật dữ liệu (ô có 2 kiểu, mảng sương không kề ô nào ngoài sương = không bao giờ tan, sương quá nửa lưới cảnh báo). Kiểm tra "không cần đoán" khi còn sương: giải trên "level nhìn thấy" (bỏ ô số dưới sương, ô sương coi như ô trống, `ShikakuSolver.TrySolve(..., ignoreCoverage)` không bắt phủ kín vì khối của số dưới sương có thể phủ ô ngoài); 1 cách đặt chỉ chắc chắn đúng khi là cách duy nhất còn giải được và không đè sương; đặt xong cho sương tan theo `FogRules.Clears`.
- `HintBooster.IsLocked`: bỏ vùng còn ô sương. `RevealBooster`: chạm ô sương không chọn.
- Độ khó: đếm `fogPatches` / `fogCells`, trọng số `wFog` = 4 (cột CSV). Mô phỏng T1–T4 chưa tính thứ tự tan sương.
- Database: chỉ sinh lại **12 level** lịch xếp sương (40, 41, 44, 49, 112, 115, 127, 129, 154, 161, 166, 196), cả 12 gắn được. 9 level trong số đó trước có số mơ hồ, giờ lịch đổi sang sương nên mất số mơ hồ: còn 33 level có số mơ hồ.
- Docs: `03_CoChe-Mechanic.md`, `DifficultyFormula.md` mục 10.

**Kiểm tra**: 87/87 test EditMode qua. Validator cả database: chỉ còn lỗi cũ ở level 51. Play level 40: popup "Fog" (`Captures/fog_intro_popup.png`), mảng 2x2 phủ mây (`Captures/fog_level40.png`); đặt khối kề → cả mảng tan, lộ số "6" (`Captures/fog_level40_cleared.png`, chụp giữa lúc tan). Save trả về level 36, bỏ cờ đã xem popup "Fog". Chưa thử kéo tay thật, chưa thử Hint / Reveal khi còn sương.

## 10:02 – Sương mù vẽ theo mảng: 1 khung phủ cả mảng + dấu "?" ở giữa

**Yêu cầu**: thay lớp mây từng ô bằng 1 khung tím bo góc phủ gọn cả vùng mảng sương, dấu "?" ở giữa (art người dùng gửi).

**Thêm**
- `Art/Grid/Grid_Cells/FogTile.png` (khung tím, cắt sát từ ảnh gửi, 452 px, 9-slice viền 100, ppu 280, FullRect) và `FogMark.png` (dấu "?", 170 x 277).
- `Scripts/Board/FogView.cs` (object `SpawnGrid/FogView` trong `Main`, gán vào `GridSpawner.fogView`): mỗi mảng chữ nhật 1 khung Sliced cỡ đúng vùng ô (`số ô × CellSpacing - CellGap`, mép trùng mép ô ngoài cùng), dấu "?" cao 0.5 cạnh ngắn (tối đa 1.2 ô). Mảng méo (vẽ tay) thì mỗi ô 1 khung. Hiện: phóng từ 0 (OutBack) sau 0.25 s. Tan: khung phồng 1.15 lần + mờ 0.4 s. Pool lại khi đổi level.

**Sửa**
- `Cell.prefab`: bỏ lớp `Visual/FogCover`; xóa sprite `FogCover.png` (mây từng ô). `CellView` vẫn ẩn nội dung ô còn sương và cho hiện dần khi tan; lớp che riêng từng ô thành tùy chọn.
- Ảnh popup `popup_tutorial_fog.png` vẽ lại theo kiểu khung + "?".

**Kiểm tra**: 87/87 test EditMode qua. Play level 40: mảng 2x2 thành 1 khung, "?" ở giữa, nằm gọn trong 4 ô (`Captures/fog_patch_tile.png`); đặt khối kề → khung tan, lộ số "6" (`Captures/fog_patch_clearing.png`). Console không lỗi. Save trả về level 40.

**Sự cố lúc làm**: đã refresh / compile khi Unity đang ở Play mode (người dùng đang chơi) → 3 lỗi NullReference do reload giữa chừng; đã hỏi rồi thoát Play, gắn lại, console sạch.

## 10:19 – Thay art sương bằng bản người dùng cắt lại (hết viền trắng)

**Sửa** `Art/Grid/Grid_Cells/FogMark.png` (169 x 276) và `FogTile.png` (cắt sát lề trong suốt 25 px còn 450 x 449) bằng 2 ảnh người dùng cắt lại. Giữ `.meta` (9-slice viền 100, ppu 280) nên không phải gán lại.

**Kiểm tra**: Play level 40: khung + "?" không còn viền trắng, vẫn nằm gọn trong 2x2 ô (`Captures/fog_patch_tile_v2.png`). Save vẫn ở level 40.

## 11:02 – Cơ chế mới: số hàng / cột ở rìa

**Luật**: số bên trái 1 hàng (bên trên 1 cột) = số khối KHÁC NHAU đi qua hàng / cột đó. Không được đặt khối làm hàng / cột vượt số, hoặc lấp kín hàng / cột mà chưa đủ số khối. Để cơ chế làm level **khó hơn** (số rìa tự nó cho thêm thông tin), Injector gắn mỗi số rìa kèm giấu 1 ô số trên hàng / cột đó thành "?".

**Thêm**
- `LevelDefinition.lineClues` (`LineClueData`: `axis` Row / Column, `index`, `count`), mặc định rỗng.
- `Scripts/Board/LineRules.cs`: `Crosses`, `CellsOnLine`, `Count`, `Allows` (luật dùng chung BoardModel và solver), `FromSolution`, `MarginCells` (0.85 ô chừa ở rìa).
- `BoardModel`: `PlacementResult.WrongLineCount` (vùng sai chắc chắn), `IsLineFull`, `LineBlocks`; `IsSolved` kiểm tra lại mọi số rìa.
- `ShikakuSolver`: lớp `LineState` đếm khối / ô trống từng hàng, cột trong lúc backtrack (vi phạm thì bỏ nhánh, lá phải đúng số). Khi giải không bắt phủ kín (Validator lúc còn sương) thì bỏ luật số rìa (lỏng hơn = an toàn).
- `Scripts/Board/LineClueView.cs` (object `SpawnGrid/LineClueView` trong `Main`): chữ Fredoka viền tối, cỡ 7.5, cách mép lưới 0.45 ô; hàng / cột kín và đúng số thì chuyển xanh + nảy. `BoardCameraFitter` chừa 0.85 ô mỗi phía khi có số hàng / số cột.
- Popup "Line Count" (`MechanicIntro.Kind.Lines`, ảnh `popup_tutorial_lines.png`).
- `MechanicKind.Lines` + `TryLines` (Injector): chọn hàng / cột (level giới thiệu chỉ hàng), số tính từ lời giải, giấu 1 ô số có khối đi qua hàng / cột đó; Validator qua thì giữ. Giới thiệu level 46, 2 level, 1–2 số rìa.
- Level Editor: mục "Số hàng / cột ở rìa": sửa / xóa, thêm theo hàng / cột (số tự tính từ lời giải), "Tính lại mọi số từ lời giải".
- Validator: số rìa trong lưới, 1..độ dài, không trùng, khớp lời giải đã lưu. Độ khó: đếm `lines`, trọng số `wLine` = 2 (cột CSV).
- Test `LineClueTests` (5): đếm khối, solver loại lời giải, vượt số bị chặn, lấp kín thiếu khối bị chặn, thắng khi đúng mọi số.

**Database**: thêm rule Lines làm lịch đổi, nên sinh lại **69 level** có ít nhất 1 cơ chế mới theo lịch hiện tại (các level khác giữ nguyên). Kết quả: 44 level có số mơ hồ, 14 có sương, 11 có số rìa (47, 48, 58, 112, 115, 127, 129, 154, 161, 166, 196); không level nào thiếu cơ chế theo lịch. Validator: chỉ còn lỗi cũ ở level 51.

**Kiểm tra**: 92/92 test EditMode qua. Play level 47: popup "Line Count" (`Captures/lines_intro_popup.png`), số "2" bên trái hàng trên (`Captures/lines_level47.png`); đặt 2 khối đi qua hàng đó → số chuyển xanh (`Captures/lines_level47_done.png`). Level 58: số cột "2" ở trên, số hàng "6" bên trái (`Captures/lines_level58-1.png`). Lần đầu cỡ chữ 5 quá nhỏ → tăng 7.5. Console không lỗi. Save trả về level 40, bỏ cờ đã xem popup "Line Count". Chưa thử kéo tay thật.

## 11:16 – Nền khay dưới các ô; sương méo chia thành hình chữ nhật

**Thêm**
- `Art/Grid/Grid_Cells/BoardFrame.png`: chép từ `Shared/Art/UI/Frames_Borders/khung3.png` (khung tím có viền + bóng, cùng kiểu HUD; bản gốc đang được 4 prefab / scene dùng nên không sửa), 9-slice viền 28, ppu 80, FullRect.
- `GridSpawner.boardFrame` (object `SpawnGrid/BoardFrame` trong `Main`, sortingOrder -1, z -0.02 để không bị lớp hoa văn nền cùng order đè) + `BoardPadding` 0.22 + `BoardSize(w, h)`: khung phủ cả lưới (kể cả chỗ ô Void) = mép ô ngoài cùng + 0.22 mỗi phía, co giãn mỗi level.

**Sửa**
- `BoardCameraFitter`: chứa trọn khung nền (lấy kích thước lớn hơn giữa lưới và khung).
- Số hàng / cột ở rìa ra ngoài khung: `LineClueView.offsetCells` 0.45 → 0.7 (cả giá trị trong scene), `LineRules.MarginCells` 0.85 → 1.05.
- `FogView`: mảng sương không phải hình chữ nhật (vd. 2 mảng sương sát nhau ở level 49) trước vẽ lẻ từng ô, giờ chia tham lam thành vài hình chữ nhật lớn (lấy ô dưới-trái nhất, mở ngang hết cỡ rồi mở dọc), mỗi hình 1 khung "?"; cả mảng vẫn tan cùng lúc.

**Kiểm tra**: Play level 49 (có Void + sương): khay nền dưới mọi ô, chỗ Void lộ nền khay; sương thành 1 khung 3x3 + 1 khung 2x2 (`Captures/board_frame_level49_fogsplit.png`). Level 58: số cột / hàng nằm ngoài khung (`Captures/board_frame_level58.png`). Console không lỗi. Save trả về level 49 (lúc thử có lỡ để save ở level 58 một lúc, đã sửa lại).

## 14:16 – Số rìa: tab gắn khay, tiến độ khi kéo, báo khi bị chặn

**Sửa** `LineClueView` (viết lại):
- **Tab**: mỗi số nằm trong 1 tab (sprite `BoardFrame`, cạnh 0.95 ô) dính vào mép khay ở đầu hàng (trái) / đầu cột (trên), lấn 0.08 dưới mép khay. Kín + đúng số: tab và số chuyển xanh, nảy.
- **Tiến độ khi kéo**: vùng đang kéo đi qua hàng / cột thì tab hiện "số khối sau khi đặt / số cần" (vd. "1/6"), vàng nếu thả được, đỏ nếu sẽ sai luật.
- **Bị chặn** (`PlacementResult.WrongLineCount`, nghe `GridManager.onInvalidMove`): tab sai luật nháy đỏ "a/b" + lắc, cả hàng / cột lóe vạch đỏ (sprite trắng `rounded_white`, alpha 0.45) rồi mờ trong 0.45 s.
- `BoardModel.IsLineFull(line, rect)` thành public; `LineRules.MarginCells` 1.05 → 1.2 (chỗ cho tab). Gán `tabSprite`, `stripSprite`, `spriteMaterial` trên `SpawnGrid/LineClueView` trong `Main`.

**Kiểm tra**: 92/92 test EditMode qua, console sạch. Play level 58: tab "2" trên cột, "6" bên trái hàng (`Captures/lines_tabs_level58.png`); kéo vùng qua hàng "6" → tab "1/6" (`Captures/lines_tabs_drag_progress.png`); cột đủ 2 khối → tab xanh; hiệu ứng bị chặn (gọi trực tiếp, dừng giữa chừng để chụp) → tab đỏ "7/6" + vạch đỏ cả hàng (`Captures/lines_tabs_reject_v2.png`). Chưa thả tay thật vào vùng bị chặn vì số rìa (level thử không có sẵn vùng như vậy). Save trả về level 48. Sửa thêm: `Shared/.../rounded_white.png` mesh Tight → Full Rect (hết cảnh báo "Sprite Tiling" khi vẽ Sliced; Image UI dùng sprite này không ảnh hưởng).

## 14:35 – Số rìa không chặn nữa: tab "đã có/cần" vàng / xanh / đỏ, phóng to khi kéo qua

**Lý do (góp ý người dùng)**: chữ nhảy qua lại "1/3" ↔ "3" khi kéo khó chịu; vạch đỏ dưới khối khó thấy; kéo đúng số ô mà vẫn bị chặn vì số rìa khó hiểu.

**Sửa**
- Luật: `BoardModel.Check` bỏ bước kiểm tra số rìa (xóa `PlacementResult.WrongLineCount`, `LinesAllow`) → đặt khối tự do. `IsSolved` vẫn yêu cầu mọi số rìa đúng; sai thì các khối đi qua hàng / cột sai vào danh sách `wrong` (rung như khối "?" sai). Solver / Validator giữ luật (`LineRules.Allows`) nên lời giải và kiểm tra "không cần đoán" không đổi.
- `LineRules.StateOf`: thiếu = vàng, đủ = xanh, thừa hoặc kín mà vẫn thiếu = đỏ.
- `LineClueView` viết lại: chữ luôn "đã có/cần", chỉ đổi khi đặt / gỡ khối (đổi trạng thái thì nảy); vùng đang kéo đi qua thì tab phóng to 1.18 (không đổi chữ / màu). Bỏ vạch lóe + hiệu ứng báo chặn (field `stripSprite` cũ trong scene không còn dùng). Cỡ chữ 5.6 (scene cập nhật theo).
- Test: 2 test "bị chặn" thay bằng `VuotSoHang_VanDatDuoc_NhungKhongThang`, `TrangThaiTab`.

**Kiểm tra**: 92/92 test qua, console sạch. Play level 58: cột "2/2" xanh, hàng "3/6" vàng (`Captures/lines_tab_state_hover.png`); kéo qua hàng 6 → tab "3/6" phóng to, chữ giữ nguyên (`Captures/lines_tab_hover-1.png`). Chưa thấy trạng thái đỏ trong Play (chỉ qua test). Save trả về level 48.

## 2026-10-23 10:31 – Sương mù: tan khi viền kín khối, gỡ khối viền thì phủ lại

**Lý do (người dùng)**: luật cũ "đặt 1 khối kề là cả mảng tan" bị lợi dụng: đặt bừa 1 khối (đúng hay sai) để soi rồi gỡ.

**Luật mới**: 1 mảng sương tan khi MỌI ô chơi được kề cạnh mảng (viền, không tính Void / ngoài lưới) đều có khối. Gỡ 1 khối ở viền thì sương phủ lại, trừ khi đã có khối đặt vào trong vùng sương cũ (không che khối người chơi vừa đặt).

**Sửa**
- `FogRules`: bỏ `Clears`; thêm `Current(origin, isPlayable, isOccupied)` (tính lại các ô còn sương), `IsCleared`, `Border`.
- `BoardModel.SyncFog`: tính lại sau mọi lần đặt / gỡ / thử thay khối (`Place`, `Remove`, `CanPlaceReplacing`, `ClearBlocks`); sương gốc lấy từ level.
- `FogView`: mảng phủ lại thì khung hiện lại (phóng từ 0.6, OutBack); vẫn tan bằng phồng + mờ.
- `LevelValidator`: mô phỏng tan theo `FogRules.Current`. Suy luận của người chơi: khối chứa ô viền phải đặt trước khi sương tan nên không khối nào cắt ngang mép sương → "level nhìn thấy" coi ô sương là Void, vẫn bắt phủ kín phần còn lại (bỏ số rìa trong lúc còn sương, chỉ làm mô phỏng khó hơn). Luật dữ liệu mới: khối lời giải cắt ngang mép sương = lỗi (sương không bao giờ tan).
- `MechanicInjector.TryFog`: sương phủ TRỌN 1 khối lời giải (level thường ưu tiên 2 khối kề nhau, tổng ≤ 9 ô), không phủ quá nửa lưới, thử hết các cách.
- Popup "Fog": "Fill every cell around the fog to clear it. Open a gap and it comes back."; vẽ lại ảnh (3 khối lấp kín viền quanh mảng sương 1x2 → sương tan, lộ số 2).
- Test `FogTests` viết lại (6): chặn kéo vào sương, kín 1/3 rồi 2/3 viền chưa tan, kín viền thì tan cả mảng, gỡ khối viền phủ lại, có khối trong vùng cũ thì không phủ lại, viền bỏ qua Void / ngoài lưới, tách mảng.

**Database**: 14 level có sương theo luật cũ đều không qua Validator mới (khối dưới sương lan ra viền → kẹt) → sinh lại 14 level lịch xếp sương (40, 41, 44, 49, 53, 65, 67, 72, 74, 102, 104, 143, 145, 173), cả 14 gắn được. Validator: chỉ còn lỗi cũ ở level 51.

**Kiểm tra**: 93/93 test qua, console sạch. Play level 40 (sương 2x1 ở góc): đặt khối viền thứ 1 → sương còn; thứ 2 → tan (`Captures/fog_rule3_cleared.png`); gỡ 1 khối viền → khung "?" hiện lại (`Captures/fog_rule3_returned.png`). Save trả về level 49.

## 2026-10-23 10:51 – Bỏ cơ chế sương mù; tạm tắt Void

**Lý do (người dùng)**: sương mù không hợp với game; Void khoét ngẫu nhiên làm lưới xấu (chỉ giữ khi khoét theo hình có chủ đích, vd. đầu lâu, trái tim).

**Database**: `MechanicInjectorSettings.asset`: Void `introLevel` 5 → 0 (tắt), xóa rule Fog. Sinh lại mọi level có Void / sương do Injector tạo theo lịch mới (thay bằng cơ chế khác theo lịch, hoặc về level gốc). Còn 2 level có Void không do Injector: level 110 (`ShikaCat/Level_018`, Void có sẵn trong dữ liệu gốc) và level 202 (`Test/Level_Test_AllMechanics`) – giữ nguyên. Validator: chỉ còn lỗi cũ ở level 51.

**Xóa**: `FogRules.cs`, `FogView.cs`, `FogTests.cs`, art `FogTile.png` / `FogMark.png` / `popup_tutorial_fog.png`, object `SpawnGrid/FogView` trong `Main`. `CellType.Fog`, `CellModel.IsFogged / SetFog`, `PlacementResult.Fogged`, `BoardModel.SyncFog`, phần sương trong `CellView`, `GridSpawner`, Hint / Reveal, `MechanicIntro.Kind.Fog` (+ `fogSprite`), Validator (luật dữ liệu sương, "level nhìn thấy"), `ShikakuSolver.TrySolve(..., ignoreCoverage)`, độ khó (`fogPatches`, `fogCells`, `wFog`, cột CSV), Level Editor (công cụ Sương), Level Generator, `MechanicInjector.TryFog`. `MechanicKind.Fog` bỏ nhưng giữ số `Lines = 8` để asset không lệch.

**Giữ**: code Void (`CellType.Void`, `TryVoid`, công cụ Void trong Level Editor) – chỉ tắt trong lịch.

**Kiểm tra**: compile sạch, 87/87 test EditMode qua (bớt 6 test sương). Không còn level nào có kiểu ô lạ. Play level 40 (trước có sương): lưới đầy đủ, giải được (`Captures/no_fog_level40.png`), console sạch. Save trả về level 49.

## 2026-10-23 11:03 – Bỏ cơ chế số hàng / cột ở rìa

**Database**: xóa rule Lines khỏi `MechanicInjectorSettings.asset`; sinh lại 15 level có số rìa (47, 48, 49, 53, 58, 112, 115, 127, 129, 143, 145, 154, 161, 166, 196) theo lịch mới (ô "?" mà số rìa đã đổi lấy cũng mất theo). Validator: chỉ còn lỗi cũ ở level 51.

**Xóa**: `LineRules.cs`, `LineClueView.cs`, `LineClueTests.cs`, `popup_tutorial_lines.png`, object `SpawnGrid/LineClueView` trong `Main`. `LevelDefinition.lineClues` + `LineAxis` + `LineClueData`, phần số rìa trong `BoardModel` (`IsLineFull`, `LineBlocks`, `IsSolved`), `ShikakuSolver` (`LineState`, `BuildLines`), `GridSpawner`, `BoardCameraFitter` (chừa rìa), `MechanicIntro.Kind.Lines` (+ `linesSprite`), Validator, độ khó (`lines`, `wLine`, cột CSV), Level Editor (mục số rìa), `MechanicInjector.TryLines` (+ snapshot). `MechanicKind` không còn `Lines` (ghi chú 7 = sương, 8 = số rìa không dùng lại).

**Kiểm tra**: compile sạch, 82/82 test EditMode qua (bớt 5 test số rìa). Play level 58 (trước có số rìa): còn "?" + dấu góc theo lịch, giải được (`Captures/no_lines_level58.png`), console sạch. Save trả về level 49.
