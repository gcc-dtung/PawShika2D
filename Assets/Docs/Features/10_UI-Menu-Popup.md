# 10 – UI: Main Menu, màn chơi, HUD, Popup

## 3 Canvas

| Canvas | sortingOrder | Bật khi | Chứa |
| --- | --- | --- | --- |
| `Canvas_HUD` | 0 | Trong ván | `UIZone` (safe area): `PlayZone`, `TopPanel` (LEVEL, tim, khiên, bánh răng), `DownPanel/Booster`, `RevealBanner`, `TutorialPanel`; `BoosterTutorial` (Canvas con, order 5) |
| `Canvas_Menu` | 10 | Ở Menu | `Panels` (Shop / Home / Progress trượt ngang), `AlwayOn/TabBar` |
| `Canvas_Popup` | 20 | Luôn bật | `Popups` (`GameFlowUI` + mọi popup) – dùng chung cho Menu và ván |

## Chuyển màn (`ScreenFlow`, trên `Manager`)

```text
Mở game ─► TutorialController.Pending? ─► EnterTutorial
                 └ không ─► ShowMenu (Canvas_Menu bật, HUD tắt, LevelManager.Pause)
PLAY ─► ScreenFlow.Play ─► GameFlowUI.RequestPlay (năng lượng) ─► EnterGame (HUD bật, LevelManager.Retry)
Popup Thua / Cài đặt → HOME ─► ShowMenu
Thiếu coin ─► ShowShop (về Menu + tab Shop)
```

Level vẫn được nạp sẵn ở nền khi ở Menu (để nút PLAY biết "Level n" và mức khó), nhưng bị `Pause`.

## Main Menu

- `MainMenuNavigator`: tab Shop (0) / Home (1) / Progress (2), trượt ngang, vuốt chuyển tab (`MenuSwipe`), `onTabChanged`.
- Home: `PlayButtonView` (Level n, mặt nút theo độ khó), `MenuShortcutButton` (Starter, No Ads), `DailyRewardButton`, `TopBar` (năng lượng, coin, bánh răng).
- Progress: panel tạm, chưa có nội dung.

## Popup

Mọi popup kế thừa **`PopupView`**: `Open()` / `Close()` / `CloseImmediate()`, hook `OnOpen()`; nền tối `Dim` chặn bấm, bảng bật lên (DOTween, **thời gian thực** – chạy cả khi `timeScale = 0`), âm PopupOpen / Close.

**`GameFlowUI`** (trên `Canvas_Popup/Popups`) là chỗ duy nhất quyết định popup nào mở:

| Nguồn | Popup |
| --- | --- |
| `EventBus<GameStateChangedEvent>`: Won / OutOfLives / Lost | `WinPopup` / `ReviveLivesPopup` / `LosePopup` |
| `BoosterManager.onBuyRequested` | `BuyBoosterPopup` |
| `IAPService.PurchaseFailed` | `NoticePopup` |
| Nút bánh răng → `OpenSettings` | `SettingsPopup` (→ Home trong ván → `QuitLevelPopup`) |
| PLAY / Retry thiếu năng lượng, nút tim | `RefillEnergyPopup` |
| Nút Daily / tự mở | `DailyRewardPopup` |
| `ShowNotice(msg, title, onClose)` | `NoticePopup` dùng chung |

Popup mở giữa ván phải gọi `LevelManager.Pause()` khi mở và `Resume()` khi đóng (để lưới khóa).

Nạp level mới thì mọi popup đang mở **tự đóng** (`PopupView` nghe `LevelLoadedEvent`). Popup cần giữ lại thì override `CloseOnLevelLoad` (`NoticePopup` = false, `RefillEnergyPopup` = false khi đang bắt buộc).

## Thành phần UI dùng chung

| Component | Việc |
| --- | --- |
| `SafeAreaZone` | Neo vùng UI tránh tai thỏ / thanh home |
| `PlayZone` | Vùng lưới phải nằm trọn; `BoardCameraFitter` căn camera theo vùng này |
| `BackgroundCover` | Ảnh nền phủ kín, không méo |
| `ButtonPress` + `UIButtonPress` | Cảm giác nhấn lún (tự gắn cho mọi Button trong canvas) |
| `UIButtonSounds` | Âm Click cho mọi Button (`NoClickSound` để bỏ) |
| `TMPLetterPopIn`, `UIConfetti` | Chữ hiện từng ký tự, confetti (WinPopup) |
| `NestedScrollRect` | Cuộn dọc nằm trong vùng vuốt ngang |

## Builder: UI được dựng bằng code

Gần như toàn bộ UI được tạo bởi menu **Tools → PawBlock → Builders → …** (`Script/Editor/UI/*Builder.cs`). Builder xóa rồi dựng lại object, gán sprite / font / tham chiếu.

> **Sửa layout UI: sửa trong file Builder rồi chạy lại menu.** Sửa tay trong scene sẽ bị ghi đè lần build sau. Chỉnh số liệu Inspector (thời gian tween, giá…) thì builder giữ nguyên nếu builder không gán field đó.

Thứ tự build khi dựng lại từ đầu: Build Popups → Build Main Menu → Build UI Zone → các builder còn lại. Danh sách đầy đủ: [13](13_Editor-Builder-Debug.md).

## Sửa / mở rộng

- Thêm popup: [HowTo_MoRong.md → Thêm popup](HowTo_MoRong.md#thêm-popup).
- Thêm tab Menu (vd. Progress thật): [HowTo_MoRong.md → Thêm tab / màn hình](HowTo_MoRong.md#thêm-tab--màn-hình-menu).
