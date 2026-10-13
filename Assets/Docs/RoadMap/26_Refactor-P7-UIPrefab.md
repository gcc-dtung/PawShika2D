# 26 – Refactor P7: UI thành prefab, sửa PopupView

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | 7 commit `81a6d77` … `4f44f5a` (P7.1 – P7.6 + tài liệu), cuối "Refactor P7: tài liệu P7, chốt RoadMap 25" (nhánh `Refactor-P7-UIPrefab`) |
| Thời gian push | 2026-10-13 22:41 |
| Cập nhật lần cuối | 2026-10-13 22:41 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 03:38 (13/10) | P7.1 – PopupView | abstract, `OnOpened` / `OnClosed`, event `Closed`; đóng khi đổi level chuyển về GameFlowUI |
| 03:40 (13/10) | P7.2 – 12 popup | Prefab theo feature, Canvas con, instance tắt sẵn; scene −64 000 dòng |
| 03:45 (13/10) | P7.3 – Menu, HUD | `MainMenu` lồng 4 prefab tab, `GameplayHUD`; Canvas con tab / đồng hồ / thanh sao / viền; `SettingsButton` về Shared; scene −45 000 dòng |
| 03:52 (13/10) | P7.4 – widget / item | 14 prefab widget / item (79 instance) bằng `ConvertToPrefabInstance`; Raycast Target |
| 03:57 (13/10) | P7.5 – builder | Xóa 15 builder UI (−4 900 dòng) |
| 04:14 (13/10) | P7.6 – script gốc cho từng tab | `MenuPanel` + `ShopPanel` / `HomePanel` / `MenuTabBar` trên gốc prefab, tự nối view con; `MainMenuNavigator` chỉ còn trượt / vuốt (góp ý của người dùng) |

`SampleScene`: 115 792 → **6 193 dòng** YAML; scene còn khung (Camera, EventSystem, Bootstrap, Manager, AudioManager, Canvas_Popup) + instance prefab.

---

## Prefab

| Thư mục | Prefab |
| --- | --- |
| `Shared/Prefabs` | `NoticePopup`; `Widgets/CloseButton`, `Button_Primary`, `SettingsButton` |
| `Features/Gameplay/Prefabs` | `GameplayHUD`, `BoosterButton`; `Popups/` Win, MoreTime, Lose, QuitLevel, MechanicIntro, BuyBooster |
| `Features/Economy/Prefabs` | `RefillEnergyPopup`, `CoinHUD`, `EnergyHUD` |
| `Features/DailyReward/Prefabs` | `DailyRewardPopup`, `DailyRewardSlot` |
| `Features/DailyChallenge/Prefabs` | `DailyPanel`, `DailyGiftSlot`, `DailyDayCell`, `GiftItemView`, `MonthNavButton` |
| `Features/Settings/Prefabs` | `SettingsPopup`, `SettingToggle` |
| `Features/Profile/Prefabs` | `ProfilePopup`, `AvatarItem` |
| `Features/LuckyWheel/Prefabs` | `LuckyWheelPopup` |
| `Features/Shop/Prefabs` | `ShopCoinCard`, `ShopBuyButton` |
| `Game/Prefabs` | `MainMenu`; `Menu/ShopPanel`, `HomePanel`, `MenuTabBar` |

## Sửa

- `PopupView` (Shared): abstract; `CloseOnLevelLoad` public; `Closed` bắn khi đóng hẳn (cả `CloseImmediate` lúc đang mở / đang đóng dở). 12 popup đổi `OnOpen` → `OnOpened`.
- `GameFlowUI.Init(..., PopupView[] popups)` + nghe `LevelLoadedEvent`; `GameBootstrap.popups` (12, Collector tự điền).
- `SettingsButton` → `Shared/Scripts/UI` (GUID giữ).
- `UIAutoSetup`: chạy cả khi lưu prefab (`PrefabStage.prefabSaving`); menu xử lý mọi prefab UI.
- `AudioSetup` tự có `FindRoot` / gán database.
- Tooltip "Gán từ ngoài" cho `LuckyWheelButton.popup`, `PlayZone.targetCamera`.

## P7.6 – script gốc cho từng tab Main Menu

| Script | Prefab | Việc |
| --- | --- | --- |
| `Shared/Scripts/UI/MenuPanel` (abstract) | — | Gốc chung của 1 tab: `Rect`, `SetSelected`, hook `OnShown` / `OnHidden` |
| `Game/Scripts/Menu/ShopPanel` | `ShopPanel` | Giữ 14 thẻ Shop, CoinHUD, EnergyHUD; `Init(shop, coins, energy, popups)` |
| `Game/Scripts/Menu/HomePanel` | `HomePanel` | Giữ PLAY, avatar, Daily, Lucky, CoinHUD, EnergyHUD; `Init(...)` nối cho từng nút |
| `Game/Scripts/Menu/MenuTabBar` | `MenuTabBar` | Bố cục tab chọn / nền tab chọn / vạch ngăn + hiệu ứng (tách từ navigator), event `TabClicked`, nối chấm đỏ Daily |
| `DailyChallengePanel` | `DailyPanel` | Kế thừa `MenuPanel` |

- `MainMenuNavigator` dùng `MenuPanel[] menuPanels` + `MenuTabBar`; bỏ `panels`, `tabs`, `tabBar`, `selectedBackground`, `selectedWidth` (dữ liệu chép sang `MenuTabBar` trước khi bỏ).
- `GameBootstrap`: bỏ `coinHuds`, `energyHuds`, `shopCards`, `dailyTabBadge`, `dailyRewardButton`, `luckyWheelButton`, `avatarButton`, `playButton`; thêm `shopPanel`, `homePanel`, `menuTabBar`. `SettingsButton` (widget chung cả HUD) vẫn do Bootstrap nối.
- `MainMenuNavigator`, `MenuSwipe`, `MenuTabButton`, `PlayButtonView` gom vào `Game/Scripts/Menu` (GUID giữ).
- Kiểm tra: 160 / 160 test; bấm tab, vuốt Shop → Home, `IsSelected` đúng; coin trên 2 tab, Lucky / Avatar / Daily / PLAY → ván → về Home, mua Shop → IAP lỗi, tab Daily; ảnh chụp như trước.

## Xóa

- `PopupBuilder`, `MainMenuBuilder`, `Game/Editor/Builders/*` (13 builder). Database đã tạo giữ nguyên.

## Kiểm tra

- 160 / 160 test; compile sạch; prefab không có tham chiếu `None` ngoài field tùy chọn / gán từ ngoài.
- Batches / SetPass: Menu 64 / 39 (P0 63 / 38); màn chơi level 39 64 / 53 (P0 58 / 46, tăng do Canvas nút booster từ P5 +12 và Canvas đồng hồ / sao / viền +6); Win 131–142 / 102–103 (P0 133–136 / 94–97).
- Play với `save_v1_played.json`: 12 popup qua đường của game, đổi level đóng popup (Notice giữ), 3 tab Menu (ảnh chụp có / không Canvas con giống hệt), raycast vào nút trong tab, Daily Claim +300, Shop → IAP lỗi, avatar, toggle nhạc, lịch Daily + bong thoại, HUD, Freeze. Console 0 lỗi.

## Ghi chú

- Đã thử tắt sẵn Canvas của nút booster để giảm batch: tắt Canvas component thì nút không được vẽ → giữ nguyên; giảm batch ở P8 (Sprite Atlas).
- Quy trình thử đổi: trước khi cài save mẫu luôn sao lưu save hiện có của máy (kèm giờ), trả lại đúng bản đó. Trong P7 có 1 lần save của máy đã khác bản gốc (md5 `e804…`, có thể do người dùng tự Play) và bị thay bằng save mẫu trước khi kịp sao lưu; đã trả về bản gốc v1.
