# 29 – Phản hồi khi kéo / thả sai

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | – |
| Thời gian push | – |
| Cập nhật lần cuối | 2026-10-20 09:07 |

## 09:13 – Phản hồi sai theo từng luật, đặt tại ô số

**Sửa**
- `DragFeedback`: ngoài phóng to ô số trong vùng kéo, giờ báo lỗi ngay tại ô số bị vi phạm, chỉ dùng chuyển động, không đổi màu.
  - Trong lúc kéo, vùng kéo vừa chuyển sang "chắc chắn sai" (hoặc chuyển sang lỗi khác hay ô số khác) thì lắc nhẹ 1 nhịp:
    - ≥ 2 ô số: các số lắc ngang (như cũ).
    - Ô còn khóa: ổ khóa lắc.
    - Sai hướng: số + ô hướng lắc **theo trục sai** (ô ngang kéo dọc thì lắc dọc, ô dọc kéo ngang thì lắc ngang, ô vuông theo cạnh dài).
    - Thừa ô: số lắc.
  - Ngón tay đẩy vào khối đã đặt: khối lùi theo hướng đẩy rồi bật về, đổi biểu cảm. Đang để bật để xem thử, tắt ở ô `blockPush`.
  - Thả tay bị từ chối: lắc lại mạnh hơn (`releaseShake`; ô khóa dùng `lockedShake`) và số / ổ khóa nảy lên (`releasePunch`). Thiếu ô chỉ nảy nhẹ (`tooSmallPunch`), không lắc.
- `CellView`: `PlayShake(axis, strength)` lắc số / ổ khóa cùng ô hướng theo trục; `PlayPunch(amount)` nảy số (cộng dồn với phóng to khi kéo). `PlayConflictShake` giờ gọi `PlayShake`.
- `PlacedBlockView`: `PlayPushed(dir)` (`pushDistance`, `pushDuration`).
- `GridManager`:
  - Thêm `SelectionCheck` (kết quả kiểm tra vùng đang kéo) và `LastRejected` (lượt thả bị từ chối gần nhất, đọc trong `onInvalidMove`).
  - Chạm 1 ô số (sai số ô / sai hướng) không còn bị tính là thao tác sai, được bỏ qua như chạm ô trống. Ô còn khóa vẫn báo.
- `GameHapticEvents`:
  - Vùng kéo vừa chuyển sang sai thì rung `Tick` (`onInvalidDrag`).
  - Thả tay sai thì rung `Warning` thay cho `Soft`; thiếu ô rung `Selection` (rất nhẹ); không có ô số nào thì rung `Soft`.

**Tác dụng**: người chơi biết sai ở ô nào, sai luật gì, kể cả khi không nhìn lúc thả tay, nhờ rung 2 nhịp và số nảy lên.

**Kiểm tra**: Unity biên dịch không lỗi. Chưa thử trên máy: cần xem độ mạnh lắc / nảy, nhất là cảm giác khi đẩy vào khối đã đặt.

## 09:38 – Bản thử cơ chế tường (hàng rào)

**Thêm**
- `LevelDefinition.walls` (`WallData`: ô `x, y` + `WallSide` Right / Top). Mặc định rỗng nên level cũ không cần sửa.
- `WallRules`: `Crosses` (vùng chứa cả 2 ô ở 2 bên tường), `TryFind`, `RandomOnSolution` (tường ngẫu nhiên trên ranh giới các khối của lời giải, lời giải luôn còn đúng).
- `WallView` + prefab `Prefabs/Wall.prefab`: thanh bo tròn màu gỗ sáng, nằm trên đường kẻ giữa 2 ô, vẽ trên khối mèo (sorting 24). Hiện cùng lúc với số. Bị đẩy thì cong ra rồi bật về. Chưa có art, dùng `ProceduralSprites.Pill`.
- Cheat panel, tab Level: "Thử cơ chế tường" 3 / 6 / 12 tường. Chép level đang chơi, thêm tường, chơi ở chế độ luyện tập (không lưu, không tính giờ / sao, thắng xong đứng yên).
- `WallTests` (4 test EditMode).

**Sửa**
- `BoardModel`: `IsAreaFree` và `Check` chặn vùng vắt qua tường (`BlockedCell`), nên khung chọn "kẹt" ở tường như kẹt vào khối.
- `ShikakuSolver`: lọc ứng viên vắt qua tường, nên Hint / Reveal / cheat giải đúng.
- `GridSpawner`: sinh / thu hồi `WallView` (`wallPrefab`, gán trong scene `Main`), `GetWallBetween`.
- `DragFeedback`: ngón tay đẩy qua tường thì tường cong ra (dùng chung ô `blockPush`).

**Chưa làm**: Level Generator / Mechanic Injector chưa đặt tường; chưa kiểm tra tường có giúp suy luận (chỉ đặt ngẫu nhiên trên ranh giới); intro / tutorial cơ chế; art hàng rào.

**Kiểm tra**: 164/164 test EditMode của dự án qua (3 test lỗi trong package UnitySkills bên thứ ba, không liên quan). Chạy thử trong Play mode: tường hiện đúng ranh giới, vùng vắt qua tường bị chặn cả khi kéo lẫn khi đặt, solver ra lời giải không vắt qua tường.

## 09:50 – Sửa phản hồi "đẩy vào" khối / tường

**Sửa**
- `DragFeedback`: vật bị đẩy giờ là **vật cản sát mép vùng kéo** theo hướng ngón tay (tường giữa ô mép và ô kế bên, hoặc khối ở ô kế bên), không còn là thứ nằm dưới ngón tay.
  - Trước: đẩy qua tường sang ô xa hơn rồi quay lại thì tường rung thêm lần nữa.
  - Trước: ngón tay lướt qua con mèo ở xa (không chặn vùng kéo) vẫn bị tính là đẩy vào mèo đó.
- `GridManager.UpdateSelection`: `onDragCellChanged` phát sau khi vùng kéo đã giãn theo ô mới, nên biết đúng ngón tay có đang bị chặn hay không.

**Kiểm tra**: lưới 5×1 dựng trong Play mode, có tường giữa ô 0 và 1, mèo ở ô 3–4. Kéo từ ô 0 qua lại 1 → 4 → 1: tường rung 1 lần, mèo ở xa không bị tính. Kéo từ ô 1 vào mèo rồi qua lại: mèo lùi 1 lần. 164/164 test EditMode qua.

## 09:53 – Popup giới thiệu cơ chế tường

**Thêm**
- `MechanicIntro.Kind.Wall`: level có tường mà người chơi chưa xem thì mở popup "Fence" như 3 cơ chế cũ. Câu giải thích: "Blocks can't cross a fence. The cells on each side belong to different blocks." Bấm Ok thì lưu vào `seenMechanics`. `Wall` thêm ở cuối enum nên save cũ không lệch.
- Ảnh minh họa `Features/Gameplay/Art/UI/Popups_Dialogs/popup_tutorial_wall.png` (804×412, cùng kiểu `popup_tutorial_2/3/4`, import chép từ `popup_tutorial_2`): lưới 6×3, khối mèo dừng sát hàng rào, vùng kéo vắt qua hàng rào bị gạch chéo. Gán vào `wallSprite` trong scene `Main`.
- `MechanicIntro.PlayNow(level)`: mở popup không qua `LevelIntroSequencer` (màn luyện tập không phát `LevelLoadedEvent`). `GameScene.MechanicIntro` để cheat gọi. Cheat "Thử cơ chế tường" giờ hiện popup nếu chưa xem.

**Kiểm tra**: Play mode, cheat 6 tường, popup "Fence" hiện đúng khung / ảnh / nút Ok. Không bấm Ok nên save chưa đánh dấu đã xem. Muốn xem lại sau khi bấm Ok thì dùng cheat "Bỏ cờ đã xem popup cơ chế".

## 09:58 – Tường thành hàng rào có cọc, cọc ở giao điểm đường kẻ

**Sửa**
- `WallView` dựng lại giống ảnh popup "Fence": thanh gỗ có viền đậm, 2 cọc tròn có viền ở 2 đầu.
  - Mỗi đoạn dài đúng 1 cạnh ô, cọc nằm ở **giao điểm các đường kẻ lưới**. 2 đoạn nối nhau (thẳng hoặc vuông góc) dùng chung 1 cọc nên ghép thành hàng rào liền / góc vuông.
  - Thứ tự vẽ: viền thanh (23) < thanh < viền cọc < cọc (26), nên cọc chung luôn đè lên cả 2 thanh.
  - Chỉnh ở Inspector: `wood`, `outline`, `barThickness`, `postSize`, `outlineWidth`, `sortingOrder`.
  - Intro đổi sang phóng to đều 2 chiều (trước giãn 1 chiều làm cọc méo).
- `Prefabs/Wall.prefab`: 6 object con (BarOutline, Bar, PostOutlineA/B, PostA/B) dựng bằng `WallView.EditorBuild` (menu chuột phải "Dựng sẵn object con"); bỏ SpriteRenderer trên gốc.

**Kiểm tra**: Play mode, cheat 14 tường. Cọc đúng giao điểm, các đoạn liền nhau và góc vuông khớp cọc chung. 164/164 test EditMode qua.

## 10:17 – Bản thử cơ chế ô số ở góc

**Thêm**
- `ClueMechanic.corner` (`ClueCorner`: None / TopLeft / TopRight / BottomLeft / BottomRight, theo hướng màn hình). Mặc định None nên level cũ không cần sửa.
- `ShikakuSolver.MatchesCorner`: luật dùng chung cho `BoardModel.Check` và solver.
- `PlacementResult.WrongCorner`: `Check` kiểm tra sau hướng, trước số ô. `SelectionHint` = Invalid ngay, vì vùng đã lố sang phía bên kia ô số thì kéo rộng thêm không sửa được.
- `CornerRules`: `CornersOf`, `AssignRandom` (gắn góc cho ô số đang nằm ở góc khối của lời giải, bỏ qua khối 1 ô).
- Hình trên ô: object con `CornerMark` trong `Cell.prefab`, sprite `Art/Grid/corner_bracket.png` (ngoặc ở góc trên trái, phủ cả ô, xoay cho 3 góc kia; ruột trắng tô tím nhạt, viền tím đậm). Ẩn khi ô còn khóa, hiện mờ dần cùng số ở intro, lắc cùng số.
- `DragFeedback`: sai góc thì số + dấu góc lắc theo trục vùng kéo bị lố.
- Popup giới thiệu `MechanicIntro.Kind.Corner` "Corner Number": "The number must sit in the marked corner of its block." Ảnh `popup_tutorial_corner.png`: ô 4 góc trên trái + khối đúng; ô 3 góc dưới phải + vùng lố sang phải bị gạch chéo.
- Cheat panel: "Thử cơ chế ô số ở góc" 3 / 6 / Tất cả. Gộp code chung với thử tường (`TryCloneCurrent`, `PlayPractice`).
- `CornerClueTests` (5 test).

**Chưa làm**: Level Generator / Mechanic Injector / Level Editor / Validator chưa biết cơ chế góc; chưa chọn góc có chủ đích (chỉ ngẫu nhiên); chưa có gợi ý vùng được phép khi chạm ô.

**Kiểm tra**: 169/169 test EditMode qua. Play mode, cheat "Tất cả": popup hiện đúng, dấu góc hiện đúng góc, solver ra lời giải đúng cả 7 ô góc.

## 10:23 – Dấu góc không chờm ra ngoài ô, thêm 4 vị trí cạnh

**Sửa**
- `corner_bracket.png` vẽ lại: ngoặc bo cùng tâm với góc bo của ô (CellBG bo ~26% cạnh), thụt vào trong. Trước đây góc ngoặc nằm trong vùng bo nên chờm ra ngoài ô.
- `ClueCorner` thêm `Top / Bottom / Left / Right`: ô số phải nằm ở hàng / cột ngoài cùng phía đó của khối, vị trí dọc theo cạnh tùy ý. Luật trong `ShikakuSolver.MatchesCorner` (dùng chung `Check` / solver).
- Sprite mới `Art/Grid/edge_bar.png`: vạch ngắn ở giữa cạnh trên ô, xoay cho 3 cạnh kia. `CellView` có `cornerSprite` / `edgeSprite` (gán trong `Cell.prefab`), đổi sprite theo loại dấu.
- `DragFeedback`: sai cạnh trên / dưới thì lắc dọc, trái / phải thì lắc ngang.
- `CornerRules`: `Corners`, `Edges`, `IsEdge`, `MatchesOf`; `AssignRandom` nhận danh sách loại dấu được gắn.
- Cheat: "Góc 6" / "Cạnh 6" / "Trộn tất cả".
- Popup: "The number must sit in the marked corner or side of its block." (ảnh vẫn chỉ minh họa góc).
- Test thêm `Canh_ChiCanNamTrenHangCotNgoaiCung`.

**Kiểm tra**: Play mode "Trộn tất cả": ngoặc góc nằm gọn trong ô, vạch cạnh hiện đúng phía. 170/170 test EditMode qua.

## 10:44 – Bản thử cơ chế mèo ghét nhau

**Thêm**
- `LevelDefinition.rivals` (`RivalData`: `clueA`, `clueB` theo `ClueData.id`; id đã kiểm tra duy nhất trong cả 203 level). Mặc định rỗng.
- `RivalRules`: `Touches` (chung 1 đoạn cạnh; chạm góc chéo thì được), `RivalsOf`, `RandomPairsOnSolution` (ghép cặp khối không chạm nhưng gần nhau, cách tối đa 1 ô, mỗi ô số tối đa 1 cặp).
- `BoardModel`: `RivalsOf`, `TouchedRival`; `Check` bước 6 trả `PlacementResult.RivalTouch` khi vùng chạm cạnh khối đã đặt của mèo ghét. `SelectionHint` = Invalid ngay.
- `ShikakuSolver`: lớp `Rivals` nhớ khối đã chọn / đã đặt của từng clue, loại ứng viên chạm cạnh khối mèo ghét trong lúc backtrack.
- `RivalLinkView` + prefab `Prefabs/RivalLink.prefab`: đường đứt màu san hô nối 2 ô số (sprite `Art/Grid/rival_dash.png` lát lặp, mesh Full Rect). Đầu đường dừng trước tâm ô, vẽ dưới số và dưới khối mèo. Hiện mờ dần cùng số. Vi phạm thì đường đỏ lên, dày ra rồi về.
- `GridSpawner`: sinh / thu hồi đường nối (`rivalLinkPrefab`), `GetRivalLink`.
- `DragFeedback`: chạm mèo ghét thì số lắc, mèo ghét nhảy lên đổi mặt (`PlayNudge`), đường nối đỏ lên.
- Popup `MechanicIntro.Kind.Rival` "Rival Cats": "Cats joined by a dashed line can't stand side by side. Their blocks must not share an edge." Ảnh `popup_tutorial_rival.png`: cặp cách 1 ô (dấu V), cặp đứng sát (dấu X).
- Cheat: "Thử cơ chế mèo ghét nhau" 1 / 2 / 4 cặp.
- `RivalTests` (4 test).

**Chưa làm**: Level Generator / Mechanic Injector / Level Editor / Validator chưa biết cơ chế; cặp chọn ngẫu nhiên (chưa chọn cặp giúp suy luận); chưa có icon trên ô số (chỉ có đường nối).

**Kiểm tra**: 174/174 test EditMode qua. Play mode 2 cặp: popup đúng; đường nối hiện đúng, nằm dưới mèo; lời giải không có cặp nào chạm; đặt khối thứ 2 sát khối mèo ghét thì `RivalTouch`.

## 10:51 – Bỏ cơ chế mèo ghét nhau

Thử xong thấy cơ chế dây nối (mèo ghét nhau) không ổn, gỡ toàn bộ bản thử ở mục 10:44.

**Xóa**
- File: `RivalRules.cs`, `RivalLinkView.cs`, `RivalTests.cs`, `Prefabs/RivalLink.prefab`, `Art/Grid/rival_dash.png`, `popup_tutorial_rival.png` (kèm .meta).
- Code: `LevelDefinition.rivals` / `RivalData`, `PlacementResult.RivalTouch`, phần mèo ghét trong `BoardModel`, `ShikakuSolver` (lớp `Rivals`, tham số `Backtrack`), `GridSpawner`, `DragFeedback`, `MechanicIntro.Kind.Rival`, cheat "Thử cơ chế mèo ghét nhau".
- Scene `Main`: tham chiếu `rivalLinkPrefab`, `rivalSprite`.

Tường và ô số góc / cạnh giữ nguyên.

**Kiểm tra**: Unity biên dịch không lỗi, không còn chỗ nào nhắc `Rival` trong code / prefab / scene. 170/170 test EditMode qua.

## 11:08 – Tường và ô số góc / cạnh vào các tool level; tường mở ở level 24, góc ở level 27

**Sửa**
- `MechanicKind` thêm `Wall`, `Corner`. `MechanicInjectorSettings`: luật mặc định + asset `MechanicInjectorSettings.asset`.
  - Wall: giới thiệu level 24, 2 level, mỗi level 3–5 đoạn.
  - Corner: giới thiệu level 27, 2 level, mỗi level 2–3 ô.
  - Level Hard / Very Hard không dùng 2 cơ chế này (cho thêm thông tin, làm level dễ đi).
- `MechanicInjector`:
  - `TryWall`: đoạn tường trên ranh giới 2 khối của lời giải. Chỉ chọn cạnh có cách đặt vắt qua (tường thực sự loại bớt nước đi); trọng số = số cách đặt bị loại, x3 nếu nối tiếp tường đã có (thành hàng rào liền / góc vuông).
  - `TryCorner`: dấu góc / cạnh đúng vị trí ô số trong khối của lời giải. Chỉ chọn số mà dấu loại bớt cách đặt, chọn dấu loại được nhiều nhất (ngang nhau thì ưu tiên góc). Level giới thiệu chỉ dùng góc. Bỏ ô khóa và số 1.
  - Mỗi lần gắn chạy `LevelValidator`, sai thì hoàn tác (`Snapshot` lưu cả tường).
  - `Trim` dời cả tường; bỏ khối thành Void thì xóa tường sát ô đó.
  - Gắn hỏng ở level thường thì thay bằng: hướng → góc → tường → Void → "?" → khóa.
  - `Apply(level, kinds, settings, seed)`: gắn thẳng vào 1 level (Level Generator dùng).
- `ShikakuSolver`: ô khóa che cả dấu góc / cạnh (như hướng) khi mô phỏng người chơi. Trước đây validator coi như người chơi đã biết góc của ô còn khóa, trong khi game ẩn dấu.
- `LevelValidator`: tường phải nằm giữa 2 ô chơi được, không trùng, không trên viền / sát Void; dấu góc / cạnh trên số 1 thì cảnh báo.
- `LevelEditorWindow`: công cụ "Tường" (bấm gần cạnh ô: bật / tắt tường), trường "Góc / cạnh" cho số đang chọn, vẽ tường (vạch gỗ + cọc) và dấu góc / cạnh trên lưới. Đổi cỡ lưới / bật Void thì bỏ tường không còn hợp lệ.
- `LevelGeneratorWindow`: hàng "Gắn cơ chế" (chọn loại bất kỳ) gắn ngay khi sinh qua `MechanicInjector.Apply`; danh sách ghi số phần đã gắn (rê chuột xem chi tiết); xem trước hiện tường và dấu góc / cạnh.
- `LevelDifficultyAnalyzer` / `LevelDifficultyWindow`: đếm tường, ô góc / cạnh; thêm cột `walls`, `corners` vào CSV.

**Áp dụng vào database**: Mechanic Injector chạy lại level 24 → 203 (level 1–23 không đổi lịch, đã kiểm tra không file nào ở vị trí 1–23 bị sửa).
- 118 level có cơ chế: Wall 22, Corner 20, Hidden 35, Lock 47, Void 30, Constraint 20; 0 level gắn thiếu theo lịch.
- Level 24, 25: tường (3 đoạn). Level 27, 28: ô số góc (2 ô). Lần đầu xuất hiện đúng level 24 / 27.
- Level 26 trở đi khác bản trước vì lịch random có thêm 2 cơ chế mới (121 file trong `Levels/Mechanic` đổi / thêm / xóa).

**Kiểm tra**:
- Validate Levels: 203 level, 0 lỗi, 0 cảnh báo.
- Play mode: vào level 24 thì popup "Fence" tự hiện, level 27 thì "Corner Number" tự hiện (không bấm Ok).
- Sinh 5 level Normal + gắn Wall + Corner bằng `Apply`: cả 5 hợp lệ.
- 170/170 test EditMode qua.
- Đã trả level hiện tại trong save về 29 và bỏ cờ đã xem Wall / Corner sau khi thử.
- Chưa mở thử bằng tay giao diện Level Editor / Level Generator (chỉ biên dịch + gọi API).

## 11:26 – Bỏ cheat thử cơ chế, thêm nút xóa hết dữ liệu

**Xóa**
- Cheat panel: mục "Thử cơ chế tường" và "Thử cơ chế ô số ở góc / cạnh" (tường / góc giờ có trong level thật từ 24 / 27).
- Code chỉ cheat đó dùng: `DebugCheats.PlayWallTest`, `PlayCornerTest`, `TryCloneCurrent`, `PlayPractice`; `MechanicIntro.PlayNow`; `GameScene.MechanicIntro`.

**Thêm**
- Cheat panel, tab cuối, mục Save: nút "Xóa hết dữ liệu, chơi lại từ đầu" (bấm 2 lần: lần 1 hiện nút "XÓA HẾT" / "Hủy" để không lỡ tay).
- `DebugCheats.WipeAllData`: `JsonFileSaveService.Delete()` (save.json + .tmp + .bak) rồi nạp lại scene. Game chạy như lần đầu cài: level 1, tutorial, coin / sao / booster / năng lượng / cài đặt / điểm danh / popup cơ chế về mặc định. Object scene cũ giữ section cũ nhưng service đã quên chúng nên không ghi đè lại.

**Kiểm tra**: sao lưu save.json, Play mode bấm xóa: game nạp lại, vào thẳng Tutorial bước 1; level 1, 0 coin, 0 sao, tutorialDone = false, chưa xem popup nào; console không lỗi. Đã chép lại save.json của máy dev từ bản sao lưu.

## 11:40 – Popup giới thiệu ô trống (Void)

Level 5 vừa giới thiệu Void (Mechanic Injector) vừa mở booster đầu tiên, nhưng Void chưa từng có popup nên người chơi chỉ thấy tutorial booster, trông như popup cơ chế bị mất. Không phải lỗi thứ tự: chuỗi intro vẫn chạy popup cơ chế (Order 10) trước tutorial booster (Order 20).

**Thêm**
- `MechanicIntro.Kind.Void` (thêm cuối enum): level có ô Void mà chưa xem thì mở popup "Empty Cells": "Gaps in the board are not cells. Blocks can't cover them, so fill only the cells you see." Đứng đầu hàng đợi (Void được giới thiệu sớm nhất).
- Ảnh `Art/UI/Popups_Dialogs/popup_tutorial_void.png` (cùng kiểu các popup cơ chế khác): lưới 6×3 có lỗ trống, khối đúng nằm cạnh lỗ, vùng kéo phủ qua ô trống bị gạch chéo. File `.meta` chép thiết lập import của `popup_tutorial_corner.png` (guid / spriteID mới). Gán `voidSprite` trong scene `Main` bằng cách sửa YAML (Unity MCP mất kết nối lúc làm).

**Lưu ý**: người chơi đã qua level 5 từ trước sẽ thấy popup này ở lần tới vào level có ô trống (chưa có cờ đã xem).

**Kiểm tra**: chưa chạy được Unity (MCP mất kết nối), chỉ đọc lại code; cần mở Unity để biên dịch, xem ảnh import đúng và chơi thử level 5 sau khi xóa dữ liệu.

## Cập nhật độ khó level (2026-10-16)

- Công thức độ khó thêm `wWall` (1.5) và `wCorner` (2) cho cơ chế tường và ô số ở góc / cạnh (`LevelDifficultySettings`, `LevelDifficultyAnalyzer.Score`); trước đó chỉ đếm, chưa tính điểm.
- Chạy lại Level Difficulty trên 203 level, áp dụng gợi ý: 19 Hard + 8 Very Hard (số liệu và danh sách ở `DifficultyFormula.md` mục 8).

## 2026-10-20 09:07 – Shop spawn thẻ từ ShopDatabase, hiệu ứng thẻ bay vào

**Sửa**
- `ShopPanel`: không còn đặt tay từng thẻ trong prefab. Lúc `Init` spawn 1 thẻ cho mỗi gói trong `ShopDatabase` theo bảng `slots` (section → prefab + chỗ đặt: `parent`, `after` = chèn ngay sau tiêu đề mục). Thêm gói: bấm + trong `ShopDatabase.products` rồi điền; bỏ gói: bấm -. Thứ tự trong list = thứ tự hiện.
- `ShopCardView`: bỏ `database` / `productId`, nhận data qua `Bind(product, shop)`. Thêm `body` (con chứa hình của thẻ, để tween không đụng vị trí LayoutGroup đặt) và `background`.
- `ShopProduct`: thêm `background` (nền thẻ, trống = giữ nền prefab). No Ads Bundle dùng chung prefab với Bundle, nền `frame_pack_ads1` đặt trong database.

**Thêm**
- Prefab `ShopStarterCard`, `ShopRemoveAdsCard`, `ShopBundleCard` (tách từ thẻ cũ trong ShopPanel, bọc hình vào `Body`); ô coin dùng lại `ShopCoinCard`.
- `ShopIntro` (trên gốc ShopPanel): lần đầu mở tab Shop trong mỗi lần chạy game, thẻ trượt từ ngoài vào xen kẽ trái / phải (OutBack), ô coin phóng to từ 0, cách nhau 0.06 s (giãn tối đa 8 thẻ). Mở lại trong cùng lần chạy thì không chạy. Cờ static, reset khi vào Play. Sang tab khác giữa chừng thì nhảy tới cuối.

**Kiểm tra**: Play mode: 14 thẻ spawn đúng thứ tự (Starter, tiêu đề Remove Ads, Remove Ads, No Ads Bundle, tiêu đề Special Bundle, 5 bundle, lưới 6 coin); mở Shop thấy thẻ bay vào, xong khớp layout cũ; sang Home rồi quay lại Shop thì không chạy lại; console không lỗi. Chưa bấm thử mua trên thẻ mới.

**Lưu ý**: `MainMenu.prefab` còn override vị trí cũ của các thẻ đã xóa (vô hại, Unity bỏ qua).
