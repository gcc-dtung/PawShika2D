# 10 – UI: Main Menu, màn chơi, HUD, Popup

> Từ P7 mọi UI là **prefab** (không còn builder dựng bằng code). Quy tắc prefab / popup: [ProjectConventions](../Refactor/ProjectConventions.md) mục 12.

## 3 Canvas

| Canvas (scene) | Prefab | sortingOrder | Bật khi | Chứa |
| --- | --- | --- | --- | --- |
| `Canvas_HUD` | `Prefabs/UI/Screens/Game/GameplayHUD` | 0 | Trong ván | `UIZone` (safe area): `PlayZone`, `TopPanel` (LEVEL + đồng hồ + thanh sao, bánh răng), `DownPanel/Booster` (3 `BoosterButton`), `RevealBanner`, `TutorialPanel`; `FreezeOverlayFX`, `DifficultyIntro`, `BoosterTutorial` (Canvas con, order 5), `BoosterFx` |
| `Canvas_Menu` | `Prefabs/UI/Screens/Home/MainMenu` | 10 | Ở Menu | `Panels` (`ShopPanel` / `LeaderboardPanel` / `HomePanel` / `TeamsPanel` / `CollectionPanel` trượt ngang), `AlwayOn` (`MenuTabBar`) |
| `Canvas_Popup` | (trong scene) | 20 | Luôn bật | `Popups` (`GameFlowUI` + 12 instance popup prefab) – dùng chung cho Menu và ván |

Mỗi popup / tab có Canvas con; phần đổi liên tục (đồng hồ, thanh sao, viền đỏ, nút booster) có Canvas riêng để không làm cả HUD dựng lại.

## Chuyển màn (`Runtime/App/ScreenFlow`, `IScreenNavigator`)

```text
Mở game ─► TutorialController.Pending? ─► EnterTutorial
                 └ không ─► ShowMenu (Canvas_Menu bật, HUD tắt, LevelManager.Pause)
PLAY ─► ScreenFlow.Play ─► GameFlowUI.RequestPlay (năng lượng) ─► EnterGame (HUD bật, LevelManager.Retry)
Daily ─► IScreenNavigator.PlayDaily(load) ─► vào màn chơi rồi DailyChallenge nạp level ngày
Popup Thua / Cài đặt → HOME ─► ShowMenu
ShopOpenRequestedEvent (thiếu coin, bấm coin, Starter / No Ads) ─► về Menu + tab Shop
```

Mỗi lần đổi màn phát `ScreenChangedEvent`. Level vẫn được nạp sẵn ở nền khi ở Menu (để nút PLAY biết "Level n" và mức khó), nhưng bị `Pause`.

## Main Menu (`Runtime/UI/Home`)

- `MainMenuNavigator`: giữ `MenuPanel[]` (Shop 0 / Rank 1 / Home 2 / Teams 3 / Collection 4), trượt ngang, vuốt chuyển tab (`MenuSwipe`), `onTabChanged`. Không biết nội dung tab.
- Mỗi tab là 1 prefab có script gốc kế thừa `MenuPanel` (Shared), tự `Init` view con:
  - `ShopPanel` (Game): thẻ Shop, `CoinHUD`, `EnergyHUD`.
  - `HomePanel` (Game): `PlayButtonView` (Level n, mặt nút theo độ khó), `AvatarButton`, `DailyRewardButton`, `LuckyWheelButton`, `MenuShortcutButton` (Starter, No Ads), `CoinHUD`, `EnergyHUD`, `SettingsButton`.
    Theme Halloween: prefab con `HomeScene` (trời, trăng, dơi `UISpriteSequence` (khung dựng từ Spine gốc), nhà ma / mây `UIFloat`, đèn / nến `UIFlicker`, sân, rào, bí ngô, chổi) nằm dưới cùng; giữa sân là prefab `GhostMascot` (bóng + hồn ma, chạm thì nhún), ma nhỏ đi qua lại; 5 nút khóa trang trí là instance của prefab `LockedMenuButton` ("Lv X", mỗi nút override icon / nhãn / vị trí). Icon rương rau (`Locked_Chest/Content/Chest`) chạy `UISpriteSequence` từ sheet `HalloweenRip/Side/ChestAnim.png` (90 khung render từ Spine 4.2 `iconHomeLab`: `idle` 30 + `start` 60), vòng 12 s = `idle` x10 rồi `start` (nắp đóng sập, bật mở, rau trồi ra). Icon vạc hotpot (`Locked_Cauldron/Content/Chest`) tương tự: `HalloweenRip/Side/CauldronAnim.png` (120 khung `idle` 4 s của Spine `icon_hotpotHLW`: thìa khuấy, hồn ma nhấp nhô, nước sóng sánh), lặp liên tục. Icon Daily (`Daily/Content/Calendar`): `HalloweenRip/Side/CalendarAnim.png` (69 khung của Spine `Quest`, texture `Quest_0.png`: `idle` 20 + `click` 49), vòng ~15.6 s = `idle` x21 rồi `click` (đóng dấu tích, xé tờ lịch rơi xuống); Icon No Ads (`NoAds/Content/Tv`, Spine `No Ads`: `idle` 20 + `click` 41, vòng ~11.4 s, chữ ADS nảy rồi gạch chéo vẽ lại) và hộp quà Starter (`Starter/Content/Chest`, Spine `Gift_box`: `idle` 20 + `click` 18, vòng ~11.9 s, nảy kèm lấp lánh) cũng vậy, lệch pha `startOffset` 3 s / 7 s. `MenuShortcutButton` của Daily, No Ads, Starter bỏ trống `icon` nên không lắc mời bấm nữa (chỉ còn chấm đỏ / bấm); Lucky vẫn lắc. Prefab đều ở `Prefabs/UI/Screens/Home/`.
    Ảnh theme rip từ game khác nằm ở `Source/Art/UI/Home/HalloweenRip/` (gitignore, chỉ có trên máy); hồn ma tự gen ở `Source/Art/UI/Home/Halloween/`.
  - `LeaderboardPanel` (Game): bảng xếp hạng giả lập offline, 3 tab con: Weekly (bục top 3 có rương / avatar / ruy băng + danh sách, đồng hồ tới thứ Hai 00:00 UTC), Players (toàn thời gian, không bục), Teams (màn khóa). Đối thủ tên cố định, avatar lấy từ `AvatarDatabase`; dòng người chơi có viền xanh. Bootstrap `Init(profile, save, clock)`.
    Weekly: điểm người chơi = màn thắng trong tuần (`WeeklyContest`, section `"weeklyContest"`); điểm bot theo luật bám đuổi `WeeklyBots` (chỉnh trong Inspector `weeklyBots`): đang chơi thì không bot nào vượt, bot ngay dưới cách ≤ `maxLead`; bỏ chơi thì cứ `hoursPerRank` giờ thêm 1 bot vượt (≤ `maxDrop` bot, hơn ≤ `maxAhead` điểm, trần giảm dần theo hạng); quay lại mỗi màn thắng lấy lại 1 hạng. Players: điểm cố định, người chơi = cấp hiện tại.
  - `LockedTabPanel` (Game): tab Bộ sưu tập, chỉ là màn khóa tĩnh.
  - `TeamsPanel` (Game): tab Đội ([17](17_Teams.md)). `DailyChallengePanel` (prefab `DailyPanel`, [16](16_DailyChallenge.md)) tạm rút khỏi Menu từ 2026-10-25, code / prefab giữ nguyên để làm liveops.
- `MenuTabBar`: thanh tab dưới (tab chọn rộng hơn, nền trượt theo, vạch ngăn), event `TabClicked`; tab 4 là Teams (icon `IconTeam`), không còn chấm đỏ / khóa Daily.

## Popup

Mọi popup kế thừa **`PopupView`** (Shared, abstract): `Open()` / `Close()` / `CloseImmediate()`, hook `OnOpened()` / `OnClosed()`, event `Closed`; nền tối `Dim` chặn bấm, bảng bật lên (DOTween, **thời gian thực** – chạy cả khi `timeScale = 0`), âm PopupOpen / Close. Popup nhận dữ liệu bằng hàm riêng (`Show...`) rồi gọi `Open()`.

**`GameFlowUI`** (`Runtime/App`, trên `Canvas_Popup/Popups`, cài `IMenuPopups`) là chỗ duy nhất quyết định popup nào mở:

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
| Nút "i" của sự kiện (`TutorialButton.popup`) | Variant của `TutorialPopup` (vd `WeeklyTutorialPopup`, nút i card Weekly) |

Popup mở giữa ván gọi `IGamePause.Pause()` khi mở và `Resume()` khi đóng (để lưới khóa, đồng hồ dừng).

Nạp level mới thì `GameFlowUI` (nghe `LevelLoadedEvent`) đóng mọi popup đang mở có `CloseOnLevelLoad` (`NoticePopup` = false, `RefillEnergyPopup` = false khi đang bắt buộc).

## Thành phần UI dùng chung (`Runtime/UI/Framework`, `Prefabs/UI/Widgets`)

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
| `TutorialPopup` + `TutorialButton` | Base popup hướng dẫn sự kiện / liveops (prefab `Prefabs/UI/Popups/TutorialPopup`, mỗi sự kiện 1 variant); cách thêm: [HowTo_MoRong](HowTo_MoRong.md#popup-hướng-dẫn-sự-kiện--liveops-nút-i) |
| Widget prefab | `CloseButton`, `Button_Primary`, `SettingsButton` |

## Sửa UI

Mở prefab trong Prefab Mode (`Prefabs/InGame`, `Prefabs/UI/{Popups, Screens, Widgets}`), sửa rồi lưu. Không sửa instance trong scene rồi Apply All. Tham chiếu tới object ngoài prefab: qua `Init` từ `GameBootstrap` (Collector tự thu khi lưu scene / vào Play).

## Sửa / mở rộng

- Thêm popup: [HowTo_MoRong.md → Thêm popup](HowTo_MoRong.md#thêm-popup).
- Thêm tab Menu: [HowTo_MoRong.md → Thêm tab / màn hình](HowTo_MoRong.md#thêm-tab--màn-hình-menu).
