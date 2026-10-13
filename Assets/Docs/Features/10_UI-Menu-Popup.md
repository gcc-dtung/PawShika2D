# 10 – UI: Main Menu, màn chơi, HUD, Popup

> Từ P7 mọi UI là **prefab** (không còn builder dựng bằng code). Quy tắc prefab / popup: [ProjectConventions](../Refactor/ProjectConventions.md) mục 12.

## 3 Canvas

| Canvas (scene) | Prefab | sortingOrder | Bật khi | Chứa |
| --- | --- | --- | --- | --- |
| `Canvas_HUD` | `Features/Gameplay/Prefabs/GameplayHUD` | 0 | Trong ván | `UIZone` (safe area): `PlayZone`, `TopPanel` (LEVEL + đồng hồ + thanh sao, bánh răng), `DownPanel/Booster` (3 `BoosterButton`), `RevealBanner`, `TutorialPanel`; `FreezeOverlayFX`, `DifficultyIntro`, `BoosterTutorial` (Canvas con, order 5), `BoosterFx` |
| `Canvas_Menu` | `Game/Prefabs/MainMenu` | 10 | Ở Menu | `Panels` (`ShopPanel` / `HomePanel` / `DailyPanel` trượt ngang), `AlwayOn` (`MenuTabBar`) |
| `Canvas_Popup` | (trong scene) | 20 | Luôn bật | `Popups` (`GameFlowUI` + 12 instance popup prefab) – dùng chung cho Menu và ván |

Mỗi popup / tab có Canvas con; phần đổi liên tục (đồng hồ, thanh sao, viền đỏ, nút booster) có Canvas riêng để không làm cả HUD dựng lại.

## Chuyển màn (`Game/Scripts/ScreenFlow`, `IScreenNavigator`)

```text
Mở game ─► TutorialController.Pending? ─► EnterTutorial
                 └ không ─► ShowMenu (Canvas_Menu bật, HUD tắt, LevelManager.Pause)
PLAY ─► ScreenFlow.Play ─► GameFlowUI.RequestPlay (năng lượng) ─► EnterGame (HUD bật, LevelManager.Retry)
Daily ─► IScreenNavigator.PlayDaily(load) ─► vào màn chơi rồi DailyChallenge nạp level ngày
Popup Thua / Cài đặt → HOME ─► ShowMenu
ShopOpenRequestedEvent (thiếu coin, bấm coin, Starter / No Ads) ─► về Menu + tab Shop
```

Mỗi lần đổi màn phát `ScreenChangedEvent`. Level vẫn được nạp sẵn ở nền khi ở Menu (để nút PLAY biết "Level n" và mức khó), nhưng bị `Pause`.

## Main Menu (`Game/Scripts/Menu`)

- `MainMenuNavigator`: giữ `MenuPanel[]` (Shop 0 / Home 1 / Daily 2), trượt ngang, vuốt chuyển tab (`MenuSwipe`), `onTabChanged`. Không biết nội dung tab.
- Mỗi tab là 1 prefab có script gốc kế thừa `MenuPanel` (Shared), tự `Init` view con:
  - `ShopPanel` (Game): thẻ Shop, `CoinHUD`, `EnergyHUD`.
  - `HomePanel` (Game): `PlayButtonView` (Level n, mặt nút theo độ khó), `AvatarButton`, `DailyRewardButton`, `LuckyWheelButton`, `MenuShortcutButton` (Starter, No Ads), `CoinHUD`, `EnergyHUD`, `SettingsButton`.
  - `DailyChallengePanel` (DailyChallenge): lịch tháng ([16](16_DailyChallenge.md)).
- `MenuTabBar`: thanh tab dưới (tab chọn rộng hơn, nền trượt theo, vạch ngăn), event `TabClicked`, chấm đỏ tab Daily.

## Popup

Mọi popup kế thừa **`PopupView`** (Shared, abstract): `Open()` / `Close()` / `CloseImmediate()`, hook `OnOpened()` / `OnClosed()`, event `Closed`; nền tối `Dim` chặn bấm, bảng bật lên (DOTween, **thời gian thực** – chạy cả khi `timeScale = 0`), âm PopupOpen / Close. Popup nhận dữ liệu bằng hàm riêng (`Show...`) rồi gọi `Open()`.

**`GameFlowUI`** (`Game/Scripts`, trên `Canvas_Popup/Popups`, cài `IMenuPopups`) là chỗ duy nhất quyết định popup nào mở:

| Nguồn | Popup |
| --- | --- |
| `GameStateChangedEvent`: Won / OutOfTime / Lost | `WinPopup` / `MoreTimePopup` / `LosePopup` |
| `BoosterManager.onBuyRequested` | `BuyBoosterPopup` |
| `IIAPService.PurchaseFailed` | `NoticePopup` |
| `IMenuPopups.OpenSettings` (nút bánh răng) | `SettingsPopup` (→ Home trong ván → `OpenQuitLevel` → `QuitLevelPopup`) |
| PLAY / Retry thiếu năng lượng, `OpenEnergyRefill` (nút tim) | `RefillEnergyPopup` |
| `OpenDailyReward` (nút Daily) / tự mở | `DailyRewardPopup` |
| `OpenProfile` (avatar); BXH sau này → `ShowProfile(PlayerProfile)` | `ProfilePopup` (hồ sơ của mình: đổi tên / avatar; người khác: chỉ xem) |
| `AllLevelsCompletedEvent`, `ShowNotice(msg, title, onClose)` | `NoticePopup` dùng chung |
| Nút Lucky (`LuckyWheelButton.popup`) | `LuckyWheelPopup` |
| `MechanicIntro` | `MechanicIntroPopup` |

Popup mở giữa ván gọi `IGamePause.Pause()` khi mở và `Resume()` khi đóng (để lưới khóa, đồng hồ dừng).

Nạp level mới thì `GameFlowUI` (nghe `LevelLoadedEvent`) đóng mọi popup đang mở có `CloseOnLevelLoad` (`NoticePopup` = false, `RefillEnergyPopup` = false khi đang bắt buộc).

## Thành phần UI dùng chung (`Shared/Scripts/UI`, `Shared/Prefabs/Widgets`)

| Component | Việc |
| --- | --- |
| `SafeAreaZone` | Neo vùng UI tránh tai thỏ / thanh home |
| `PlayZone` (Gameplay) | Vùng lưới phải nằm trọn; `BoardCameraFitter` căn camera theo vùng này |
| `BackgroundCover` | Ảnh nền phủ kín, không méo |
| `ButtonPress` + `UIButtonPress` | Cảm giác nhấn lún (`UIAutoSetup` gắn sẵn trong Editor khi lưu scene / prefab, lúc chạy không `AddComponent`) |
| `UIButtonSounds` | Âm Click + rung nhẹ cho mọi Button (`NoClickSound` / `NoClickVibrate` để bỏ) |
| `UIGrayscaleWhenDisabled` | Nút tắt chuyển xám |
| `TMPLetterPopIn`, `UIConfetti` | Chữ hiện từng ký tự, confetti (WinPopup) |
| `NestedScrollRect` | Cuộn dọc nằm trong vùng vuốt ngang |
| Widget prefab | `CloseButton`, `Button_Primary`, `SettingsButton` |

## Sửa UI

Mở prefab trong Prefab Mode (`Features/<F>/Prefabs`, `Shared/Prefabs`, `Game/Prefabs`), sửa rồi lưu. Không sửa instance trong scene rồi Apply All. Tham chiếu tới object ngoài prefab: qua `Init` từ `GameBootstrap` (Collector tự thu khi lưu scene / vào Play).

## Sửa / mở rộng

- Thêm popup: [HowTo_MoRong.md → Thêm popup](HowTo_MoRong.md#thêm-popup).
- Thêm tab Menu: [HowTo_MoRong.md → Thêm tab / màn hình](HowTo_MoRong.md#thêm-tab--màn-hình-menu).
