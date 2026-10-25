# Hướng dẫn làm tutorial

> Code ở `Assets/_Project/Runtime/Gameplay/` (`Tutorial/`, `Booster/`, `Intro/`, `UI/`); UI trong prefab `Prefabs/UI/Screens/Game/GameplayHUD` (sửa trong Prefab Mode, builder cũ đã xóa ở P7); đã xem lưu ở section save `"gameplay"`.

Tài liệu này mô tả tutorial lần đầu mở game (4 bước, làm theo video bản gốc), tutorial mở khóa booster (mục 7) và các bước để **sửa** hoặc **tạo** tutorial mới (vd. giới thiệu cơ chế Khóa / "?" / Hướng).

## 1. Tutorial hoạt động thế nào

Tutorial chơi bằng **gameplay thật**: lưới, kéo thả, khối mèo, báo sai, chữ khen đều là của ván thường. Tutorial chỉ thêm 3 thứ:

1. **Dữ liệu từng bước**: lưới nhỏ, lời nhắc, đường kéo mẫu cho bàn tay.
2. **Giao diện**: tiêu đề "Tutorial", nút Skip, vạch tiến độ, khung lời nhắc.
3. **Ngoại lệ trong luật**: không có đồng hồ / năng lượng; thắng một bước không hiện màn thắng; không phát `LevelLoadedEvent` / `LevelCompletedEvent` (không tính chỉ số).

| File | Vai trò |
| --- | --- |
| `Tutorial/TutorialLevels.cs` | **Dữ liệu**: danh sách bước (lời nhắc, `LevelDefinition` dựng trong code, gợi ý bàn tay) |
| `Tutorial/TutorialController.cs` | **Điều khiển** (trên `Manager`): khi nào chạy, nạp bước, sang bước kế, bàn tay, Skip, kết thúc |
| `Tutorial/TutorialHand.cs` | Bàn tay kéo mẫu (object `TutorialHand`, world space, sortingOrder 80) |
| `UI/TutorialView.cs` | Giao diện `GameplayHUD/UIZone/TutorialPanel` (prefab) |

Những chỗ trong code có hỏi `TutorialController`:

| Chỗ | Làm gì |
| --- | --- |
| `ScreenFlow.Start` (Game) | `TutorialController.IsPending` → `EnterTutorial()` thay cho Main Menu |
| `LevelManager.Start` | `IsPending` → không nạp level (tutorial tự nạp) |
| `LevelManager.LoadTutorial(level)` | Dựng lưới (camera tự căn) + state Playing + `IsPractice` (thắng không sang WinPopup); **không** lưu level hiện tại, **không** phát `LevelLoadedEvent` (HUD level, intro, booster không đổi) |
| `LevelManager.OnWin` | `IsPractice` → bỏ qua (không sang WinPopup) |
| `GameplaySave.tutorialDone` | Đã xong / Skip tutorial |

Luồng:

```text
Mở game ─► ScreenFlow.Start ─► Pending? (chưa tutorialDone và đang ở level 1)
                                 │ có
                                 ▼
                    EnterTutorial: tắt Menu, bật HUD, TutorialController.Begin
                                 │
      ┌──────────────────────────┴─────────────────────────┐
      ▼                                                     │
 LoadStep(i): view.SetStep, LevelManager.LoadTutorial,      │
              0.6 s sau hiện bàn tay (gợi ý đầu chưa đặt)   │
      │                                                     │
      ├─ đặt đúng 1 khối (onBlockPlaced) ─► bàn tay sang gợi ý kế
      ├─ đặt nhầm ─► chạm để gỡ như ván thường              │
      └─ phủ kín lưới (onGameWin) ─► chờ 1.1 s ─► còn bước? ─┘
                                                  │ hết / Skip
                                                  ▼
                 Finish: tutorialDone = true, ẩn panel + bàn tay,
                         LevelManager.LoadLevel(level hiện tại) ─► Level 1
```

## 2. Sửa một bước có sẵn

Mọi bước nằm trong `TutorialLevels.Create()`:

```csharp
// 3. Lưới 3x3: 3 (cột trái) + 6 (2 cột phải)
Step s3 = NewStep("Each number tells you how many cells its rectangle should cover.", "tutorial_3", 3, 3,
    Clue(0, 0, 2, 3), Clue(1, 1, 2, 6));
Solve(s3.level, (0, 0, 0, 1, 3), (1, 1, 0, 2, 3));
s3.hints.Add((new Vector2Int(0, 2), new Vector2Int(0, 2), new Vector2Int(0, 0)));
s3.hints.Add((new Vector2Int(1, 2), new Vector2Int(1, 2), new Vector2Int(2, 0)));
steps.Add(s3);
```

| Hàm | Tham số |
| --- | --- |
| `NewStep(message, id, width, height, clues...)` | Lời nhắc (tiếng Anh như game), id duy nhất, rộng × cao |
| `Clue(id, x, y, value)` | Số `value` ở ô (x, y). `id` dùng cho lời giải và màu |
| `Solve(level, (clueId, x, y, w, h)...)` | Lời giải: góc dưới trái (x, y) + kích thước của khối mỗi số |
| `hints.Add((clue, from, to))` | Bàn tay kéo từ ô `from` tới ô `to`. Hiện khi số ở ô `clue` chưa có khối; các gợi ý theo thứ tự |

**Tọa độ:** x tính từ trái sang, **y = 0 là hàng dưới cùng** (giống trong game và Level Editor). Vẽ lưới ra giấy với hàng trên cùng là y = height − 1 cho đỡ nhầm.

Ví dụ lưới bước 4 (2 ở góc trên trái, 4 ở giữa phải, 3 ở giữa dưới):

```text
y=2   2  .  .        Clue(0, 0, 2, 2)  -> khối (0,1) cao 2
y=1   .  .  4        Clue(1, 2, 1, 4)  -> khối (1,1) 2x2
y=0   .  3  .        Clue(2, 1, 0, 3)  -> khối (0,0) 3x1
     x=0 1  2
```

**Quy tắc khi làm lưới:**

- Tổng các số = số ô của lưới (trừ ô Void).
- Lưới phải có **đúng 1 lời giải**, nếu không bàn tay có thể chỉ khác cách người chơi đang làm. Lưới tutorial nhỏ nên tự đếm được; muốn chắc thì kiểm bằng validator (mục 5).
- Từ ô `from` kéo tới ô `to` phải ra **đúng khối của lời giải** (2 ô là 2 góc đối nhau của khối).
- Lời nhắc ngắn, 1–2 dòng. Khung tự co chữ (44 → 30) nếu dài.

## 3. Thêm / bớt bước

1. Thêm (hoặc xóa) một khối `Step ... steps.Add(...)` trong `TutorialLevels.Create()`. Thứ tự trong danh sách là thứ tự chơi.
2. **Số bước thay đổi**: bớt bước thì vạch thừa tự ẩn (`TutorialView.dots`); thêm bước thì mở prefab `GameplayHUD`, nhân bản 1 vạch trong `UIZone/TutorialPanel/Dots` và thêm vào mảng `dots` của `TutorialView`, lưu prefab.

Bước có cơ chế (Void, khóa, "?", hướng) dùng được luôn, vì `LevelDefinition` dựng trong code giống hệt level thường:

```csharp
Step s5 = NewStep("Locked numbers open after you place other blocks.", "tutorial_5", 3, 3,
    Clue(0, 0, 2, 3), Clue(1, 1, 2, 6));
s5.level.clues[1].mechanic.lockSteps = 1;                        // số 6 khóa 1 bước
// s5.level.clues[0].mechanic.hidden = true;                     // ô "?"
// s5.level.clues[0].mechanic.constraint = ClueConstraint.Vertical; // hướng dọc
// s5.level.cellOverrides = new[] { new CellOverrideData { x = 2, y = 2, type = CellType.Void } };
Solve(s5.level, (0, 0, 0, 1, 3), (1, 1, 0, 2, 3));
s5.hints.Add((new Vector2Int(0, 2), new Vector2Int(0, 2), new Vector2Int(0, 0)));
steps.Add(s5);
```

Có Void thì nhớ tổng số = số ô **trừ** ô Void.

## 4. Chỉnh giao diện / cảm giác

| Muốn đổi | Ở đâu |
| --- | --- |
| Vị trí, cỡ chữ, kích thước khung lời nhắc, vạch | Prefab `GameplayHUD` → `UIZone/TutorialPanel` (Prefab Mode) |
| Màu vạch sáng / tối, panel nào bị ẩn khi tutorial | Inspector `TutorialView` trên `TutorialPanel` (`dotActive`, `dotInactive`, `hideWhileActive`) |
| Tốc độ bàn tay (trượt, mờ, nghỉ, độ nhấn) | Inspector `TutorialHand` (`moveDuration`, `fadeDuration`, `loopDelay`, `pressScale`) |
| Bàn tay to / nhỏ, đầu ngón lệch tâm ô | Object `TutorialHand` trong scene: `localScale` (0.75), vị trí con `Sprite` (đầu ngón ở gốc object) |
| Thời gian chờ sang bước kế | Inspector `Manager` → `TutorialController.nextStepDelay` (1.1 s) |
| Sprite | `tutorial_hand` (`Source/Art/InGame/Common/Grid/Grid_Cells`), `tutorial_dot` (`Source/Art/InGame/Gameplay/Grid/Grid_Cells`), `popup_tutorial` (9-slice viền 70, `Source/Art/UI/Gameplay/Popups_Dialogs`) |

## 5. Kiểm tra

1. **Chơi lại tutorial mà không xóa save:** vào Play → Debug Panel (Ctrl + Shift + D) → mục Level → **Chơi lại tutorial**. Xong / Skip thì quay về level đang chơi.
2. **Thử như người chơi mới:** Debug Panel → **Bỏ cờ đã xem tutorial**, rồi đưa level về 1 (Đổi level = 1) → thoát Play → Play lại. Tutorial chỉ tự chạy khi đang ở level 1, để người chơi cũ không bị bắt chơi lại. Hoặc dùng Tools → PawBlock → Save Data → Delete Save.
3. **Kiểm lời giải một bước** (Editor, ngoài Play), ví dụ qua MCP `execute_code` hoặc một menu tạm:

   ```csharp
   foreach (var step in TutorialLevels.Create())
   {
       var report = LevelValidator.Validate(step.level, alwaysCheckGuessing: true);
       Debug.Log($"{step.level.id}: {(report.IsValid ? "OK" : string.Join("; ", report.errors))}");
   }
   ```

   `alwaysCheckGuessing: true` bắt luôn trường hợp lưới có nhiều lời giải.
4. **Đi hết các bước**, mỗi bước kiểm:
   - vạch đúng bước;
   - lời nhắc đúng;
   - bàn tay kéo đúng khối và chuyển sang số kế sau khi đặt;
   - gỡ khối đặt nhầm được;
   - bước cuối xong thì vào Level 1, HUD hiện lại.
5. Thử bằng MCP: Unity phải **được focus** thì Play mode mới chạy đúng tốc độ. Nếu không, thời gian game trôi rất chậm và tween / chuyển bước như bị treo.

## 6. Tạo một tutorial mới (vd. giới thiệu cơ chế)

Tutorial lần đầu là trường hợp "chạy trước mọi thứ". Tutorial cơ chế thường hiện **lúc gặp cơ chế lần đầu**. Có 2 cách, chọn theo mức độ cần dạy.

### Cách A: popup giải thích (nhẹ, nên làm trước)

> **Đã làm** cho Hướng / Khóa / "?": `MechanicIntro` (`Tutorial/`) + `MechanicIntroPopup` (`UI/`, prefab `Prefabs/Popups/MechanicIntroPopup`), danh sách `GameplaySave.seenMechanics`. Muốn thêm cơ chế (vd. Void): thêm giá trị vào `MechanicIntro.Kind`, điều kiện trong `NewMechanics`, nội dung trong `ShowNext`, field ảnh + gán trên component `MechanicIntro` trong scene. Các bước dưới là cách nó được làm.

Hợp với cơ chế nhìn là hiểu. Project đã có sẵn ảnh minh họa: `popup_tutorial_2` (Khóa), `popup_tutorial_3` ("?"), `popup_tutorial_4` (Hướng: Vertical / Square / Horizontal).

1. **Đã xem**: lưu tên cơ chế (`Kind.ToString()`) vào `GameplaySave.seenMechanics` (danh sách có sẵn; thêm cơ chế không cần field mới).
2. **Popup**: class kế thừa `PopupView` (như `NoticePopup`): ảnh minh họa, 1 dòng giải thích, nút OK. Prefab trong `Prefabs/UI/Popups`, instance tắt sẵn trong `Canvas_Popup/Popups`.
3. **Điểm kích hoạt**: `MechanicIntro` là 1 `ILevelIntro` (Order 10); `ShouldPlay` xem level vừa nạp có cơ chế nào mà chưa xem:

   ```csharp
   public bool ShouldPlay(LevelIntroContext ctx)
   {
       bool hasLock = ctx.Level.clues.Any(c => c.mechanic.lockSteps > 0);
       return hasLock && !Data.seenMechanics.Contains(Kind.Lock.ToString()); // Data = save.Get<GameplaySave>(GameplaySave.Key)
   }
   ```

   `LevelIntroSequencer` chờ lưới hiện xong và chạy băng HARD (Order 0) trước, rồi mới gọi `Play` mở popup.
4. **Đóng popup**: thêm tên vào `seenMechanics`, `save.MarkDirty()`, popup tự `Resume()` ván (`IGamePause`).
5. Level giới thiệu cơ chế đã có sẵn lịch trong **Mechanic Injector** (cột "Giới thiệu …"): popup sẽ rơi đúng vào các level đó.

### Cách B: màn hướng dẫn tương tác (giống tutorial lần đầu)

Hợp với cơ chế cần thao tác mới dạy được, vd. khóa phải đặt khối khác trước.

1. **Dữ liệu**: tạo hàm mới trả về danh sách `Step` cho cơ chế, vd. `TutorialLevels.CreateLock()`, cùng cấu trúc như mục 2–3, thường 1–2 lưới nhỏ.
2. **Điều khiển**: tổng quát hóa `TutorialController` để nhận danh sách bước và "việc làm khi xong":

   ```csharp
   public void Begin(List<TutorialLevels.Step> steps, System.Action onFinish) { ... }
   ```

   - Tutorial lần đầu: `Begin(TutorialLevels.Create(), () => LoadLevel(current))`.
   - Tutorial cơ chế: `Begin(TutorialLevels.CreateLock(), () => _levels.Retry())`, tức chơi xong quay lại đúng level vừa định vào.
   - `Finish()` đặt cờ đã xem tương ứng thay cho `tutorialDone` cố định.
3. **Kích hoạt**: như bước 3 Cách A, nhưng thay vì mở popup thì gọi `Begin(...)` trước khi người chơi kéo (vd. ngay khi bấm PLAY vào level đó).
4. **Giao diện**: dùng lại `TutorialView` (tiêu đề, Skip, vạch, lời nhắc). Muốn tiêu đề khác ("Locked numbers") thì thêm field `title` vào `TutorialView` và tham số vào `Begin`.
5. Nhờ `LoadTutorial` bật `IsPractice`, các ngoại lệ có sẵn vẫn đúng: không WinPopup, không tính chỉ số.

### Checklist cho tutorial mới

- [ ] Dữ liệu bước: lưới đúng tổng, 1 lời giải (validator), gợi ý kéo đúng khối.
- [ ] Đã xem lưu trong section `"gameplay"` (field mới có giá trị mặc định).
- [ ] Điểm kích hoạt: chỉ chạy khi chưa xem; không chạy chồng lên popup / băng HARD / lưới đang xuất hiện.
- [ ] Kết thúc / Skip: đặt cờ, `MarkDirty`, trả người chơi về đúng chỗ (level 1, hoặc level vừa vào).
- [ ] Trong lúc tutorial: không chạy đồng hồ, không trừ năng lượng, không hiện WinPopup, HUD thừa bị ẩn.
- [ ] Debug Panel có nút chơi lại / bỏ cờ để test nhanh.
- [ ] UI sửa trong prefab, đã lưu prefab / scene.
- [ ] Cập nhật [Gameplay README](Modules/Gameplay.md), `Features/09_Tutorial.md`, RoadMap.

## 7. Tutorial mở khóa booster

Booster (Hint, Đóng băng, Reveal) **khóa** tới khi người chơi qua tutorial của nó. Làm theo video bản gốc: tới level mở khóa thì màn hình tối, chỉ nút booster đó sáng (quầng xoay), bàn tay nhấn liên tục vào nút, lời nhắc ở trên. Người chơi phải bấm nút đó; booster chạy ngay và lần này **không trừ lượt**.

| File | Vai trò |
| --- | --- |
| `Booster/BoosterTutorial.cs` | Trên `Manager`, 1 `ILevelIntro` (Order 20): booster nào, mở khóa (`GameplaySave.unlockedBoosters`), phát `BoosterTutorialCompletedEvent` |
| `Booster/FreezeTutorialDemo.cs` | Trên `Manager`: bước 2 của Đóng băng ("đồng hồ đã đứng yên") |
| `Intro/LevelIntroSequencer.cs` | Trên `Manager`: chạy lần lượt các `ILevelIntro` sau khi nạp level, khóa lưới trong lúc chạy |
| `UI/BoosterTutorialView.cs` | `GameplayHUD/BoosterTutorial` (Canvas con, order 5): nền tối, lời nhắc, bàn tay; nhấc nút booster lên trên nền tối (Canvas có sẵn trên nút) |
| `BoosterManager` | `IsUnlocked` đọc save (hoặc `TutorialTarget`); `BeginTutorial(type)` / `EndTutorial`; `Press` trong lúc tutorial chỉ nhận đúng nút, phát `TutorialPressed`, lần dùng miễn phí |
| `BoosterButton.SetHighlight` | Bật quầng sáng xoay cho nút đang được chỉ |
| `Data/Config/BoosterDatabase.asset` | `levelUnlock` (level mở), `detail` (lời nhắc), `valueStart` (lượt tặng) |

**Khi nào hiện:** sau mỗi lần nạp level, `LevelIntroSequencer` chờ tới khi đang chơi thật (không ở Menu, không popup, không tutorial lần đầu), lưới đã hiện xong và băng HARD đã chạy xong, thêm 0.3 s. Lúc đó nó chọn booster **chưa mở** có `levelUnlock` nhỏ nhất mà level hiện tại đã tới. Mỗi lần nạp level chỉ hiện **1** tutorial. Người chơi cũ đã qua nhiều level sẽ được giới thiệu lần lượt ở các lần vào level kế tiếp.

**Đổi level mở / lời nhắc:** sửa `levelUnlock` / `detail` trong `BoosterDatabase.asset` (Inspector). Ổ khóa trên nút tự hiện "LV n" theo `levelUnlock`.

**Thêm booster mới có tutorial:**

1. Thêm giá trị vào `BoosterType`, dòng mới trong `BoosterDatabase.asset` (`levelUnlock`, `detail`, `icon`, `valueStart`...).
2. Thêm 1 instance `BoosterButton` trong prefab `GameplayHUD` (`UIZone/DownPanel/Booster`) với `type` đúng loại; lưu scene để Collector thu vào Bootstrap.
3. Viết tác dụng trong 1 class `XBooster : BoosterEffect` (+ 1 dòng trong `BoosterEffectFactory`) và gọi `Host.Consume(Type)` khi booster thực sự có tác dụng (để lần miễn phí được tính đúng).
4. Không cần sửa `BoosterTutorial`: nó đọc danh sách từ database và tìm nút theo `BoosterButton.Type`.

**Đóng băng – bước 2** (đồng hồ đứng yên nhưng người chơi dễ không để ý):

1. Bấm Đóng băng xong `startDelay` (0.2 s): nền tối tắt, lưới mở, khung lời nhắc xuống thấp (`BoosterTutorialView.messageOnlyY`): "The timer is frozen for 20 seconds - take your time!".
2. Hiện `messageTime` (2.5 s) rồi tắt; người chơi kéo tiếp bình thường. Thắng / nạp level khác thì bỏ bước này.
3. Chữ: Inspector `Manager` → `FreezeTutorialDemo` (`frozenMessage`, `{0}` = số giây). Booster khác muốn có bước riêng: component mới nghe `EventBus<BoosterTutorialCompletedEvent>`.

**Chỉnh giao diện:** vị trí / cỡ khung lời nhắc, độ tối nền, bàn tay trong prefab `GameplayHUD` → `BoosterTutorial` (Prefab Mode). Độ lệch đầu ngón tay so với tâm nút: Inspector `BoosterTutorial` → `BoosterTutorialView.handOffset`. Thời gian chờ trước khi tối màn: `BoosterTutorial.startDelay`.

**Kiểm tra:** Debug Panel → Booster → **Khóa lại tất cả**, rồi Đổi + vào ván level ≥ level mở (vd. 5). Lần lượt thấy Hint (level ≥ 5), Đóng băng (≥ 7), Reveal (≥ 9), mỗi lần nạp level một cái. Đóng băng: bấm xong thì lời nhắc "The timer is frozen…", đồng hồ đứng. Kiểm:

- chỉ nút được chỉ bấm được, lưới và nút khác bị chặn;
- bấm thì booster chạy và số lượt không giảm;
- lần dùng sau thì trừ lượt;
- nạp lại level thì không hiện lại.

**Mở khóa tất cả** để bỏ qua khi test việc khác.
