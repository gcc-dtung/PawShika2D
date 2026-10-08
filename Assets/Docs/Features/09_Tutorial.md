# 09 – Tutorial và hướng dẫn trong game

Dự án có 3 loại hướng dẫn, chạy độc lập và **nhường nhau** (không bao giờ chồng lên nhau):

| Loại | Khi nào | File chính | Lưu đã xem |
| --- | --- | --- | --- |
| **Tutorial lần đầu** (4 bước) | Save mới, đang ở level 1 | `TutorialController`, `TutorialLevels`, `TutorialView`, `TutorialHand` | `SaveData.tutorialDone` |
| **Popup cơ chế** | Lần đầu gặp Hướng / Khóa / "?" | `MechanicIntro`, `MechanicIntroPopup` | `SaveData.seenMechanics` |
| **Tutorial mở khóa booster** | Lần đầu tới level ≥ `levelUnlock` | `BoosterTutorial`, `BoosterTutorialView` | `SaveData.unlockedBoosters` |

Hướng dẫn chi tiết sửa bước / tạo tutorial mới: **[../TutorialGuide.md](../TutorialGuide.md)**.

## Thứ tự ưu tiên sau khi nạp level

`LevelIntroSequencer` (trên `Manager`) chạy lần lượt mọi component có interface `ILevelIntro` trong scene theo `Order`:

```text
EventBus<LevelLoadedEvent>
   │  LevelIntroSequencer: hỏi ShouldPlay của từng intro; có ít nhất 1 → InputGate.Lock (khóa lưới ngay)
   │  mỗi intro chờ: ván Playing + lưới hiện xong + không đang kéo + không trong tutorial lần đầu, rồi StartDelay
   ▼
Order 0  DifficultyIntro (0 s)   ── level Hard / Very Hard? → băng HARD → xong
Order 10 MechanicIntro  (0.15 s) ── có cơ chế mới? → popup, Pause ván → Ok → lưu, popup kế → xong
Order 20 BoosterTutorial (0.3 s) ── có booster tới level mở? → màn tối + bàn tay → bấm → mở khóa → xong
   │  mỗi intro xong (onDone) → InputGate.Unlock ngay trong frame đó
   ▼
ShieldTutorialDemo (nghe BoosterTutorialCompletedEvent) ── Khiên: "kéo sai thử"
```

Các intro không biết nhau, không có cờ tĩnh kiểm tra lẫn nhau; thứ tự chỉ do `Order`.

> Thêm một loại hướng dẫn / màn che mới sau khi nạp level: viết component implement `ILevelIntro` (`Order`, `StartDelay`, `ShouldPlay`, `Play(level, onDone)`, `Cancel`) và đặt vào scene. Không sửa sequencer, `GridManager` hay các intro khác. Màn che không gắn với nạp level thì dùng `InputGate.Lock(this)` / `Unlock(this)`.

## Xem lại khi test

Debug Panel (Ctrl + Shift + D khi Play): **Chơi lại tutorial**, **Xem lại popup cơ chế**, **Booster → Khóa lại tất cả**.

## Thêm popup cho cơ chế mới

Xem [HowTo_ThemMechanic.md – Bước 7](HowTo_ThemMechanic.md#bước-7--popup-giới-thiệu).
