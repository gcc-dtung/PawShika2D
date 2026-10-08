# 09 – Tutorial và hướng dẫn trong game

Dự án có 3 loại hướng dẫn, chạy độc lập và **nhường nhau** (không bao giờ chồng lên nhau):

| Loại | Khi nào | File chính | Lưu đã xem |
| --- | --- | --- | --- |
| **Tutorial lần đầu** (4 bước) | Save mới, đang ở level 1 | `TutorialController`, `TutorialLevels`, `TutorialView`, `TutorialHand` | `SaveData.tutorialDone` |
| **Popup cơ chế** | Lần đầu gặp Hướng / Khóa / "?" | `MechanicIntro`, `MechanicIntroPopup` | `SaveData.seenMechanics` |
| **Tutorial mở khóa booster** | Lần đầu tới level ≥ `levelUnlock` | `BoosterTutorial`, `BoosterTutorialView` | `SaveData.unlockedBoosters` |

Hướng dẫn chi tiết sửa bước / tạo tutorial mới: **[../TutorialGuide.md](../TutorialGuide.md)**.

## Thứ tự ưu tiên sau khi nạp level

```text
LevelManager.onLevelLoaded
   │  chờ: lưới hiện xong (GridSpawner.IsIntroPlaying) + băng HARD xong (DifficultyIntro.Playing)
   ▼
MechanicIntro (chờ 0.15 s) ── có cơ chế mới? → popup, Pause ván → Ok → lưu, popup kế
   │  (popup làm ván không Playing nên BoosterTutorial tự chờ)
   ▼
BoosterTutorial (chờ 0.3 s) ── có booster tới level mở? → màn tối + bàn tay → bấm → mở khóa
```

Mỗi hệ thống kiểm tra các cờ tĩnh của hệ thống khác trước khi chạy: `TutorialController.Active`, `MechanicIntro.Active`, `BoosterTutorial.Active`, `DifficultyIntro.Playing`, `ScreenFlow.IsMenu`.

> Thêm một loại hướng dẫn / màn che mới: cho nó một cờ tĩnh `Active`, và thêm cờ đó vào điều kiện "được chạy chưa" của các hệ thống còn lại (`MechanicIntro.CanStartNow`, `BoosterTutorial`), và vào `GridManager.IsInputLocked` nếu cần chặn kéo.

## Xem lại khi test

Debug Panel (Ctrl + Shift + D khi Play): **Chơi lại tutorial**, **Xem lại popup cơ chế**, **Booster → Khóa lại tất cả**.

## Thêm popup cho cơ chế mới

Xem [HowTo_ThemMechanic.md – Bước 7](HowTo_ThemMechanic.md#bước-7--popup-giới-thiệu).
