# 25 – Refactor P6: Assembly Definition

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | 3 commit `eece450` … `e1ec79a` (P6.1, P6.2 + tài liệu), cuối "Refactor P6: tài liệu P6, chốt RoadMap 24" (nhánh `Refactor-P6-Asmdef`) |
| Thời gian push | 2026-10-13 03:20 |
| Cập nhật lần cuối | 2026-10-13 03:21 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 00:36 (13/10) | P6.1 – gỡ phụ thuộc sai | `using` sai chiều 76 → 1: config về Gameplay, `IDailyLevelLoader`, `MenuShortcutButton` về Shared, `IMenuPopups.OpenQuitLevel`, builder đa feature về `Game/Editor` |
| 03:16 (13/10) | P6.2 – asmdef | `DOTween.Modules` + 6 runtime + 4 Editor + 4 Test; `Assembly-CSharp` không còn script của dự án; 160 test; build Android |

---

## P6.1 – gỡ phụ thuộc sai trước khi tạo asmdef

**Thêm**

| File | Nội dung |
| --- | --- |
| `Core/Contracts/Interfaces/IDailyLevelLoader` | Sinh + nạp level Daily của 1 ngày (Gameplay cài) |
| `Core/Foundation/Util/DayNumber` | Số ngày từ 1/1/1970 ↔ ngày (thay `DailyChallengeModel.DateOf` ở Gameplay) |
| `Features/Gameplay/Scripts/Level/DailyLevelLoader` | Sinh level Daily bằng `LevelGenerator` (thông số chép từ `DailyChallengeDatabase`), nạp `LevelManager.LoadDaily`, hủy level cũ; đặt cùng LevelManager |
| `Shared/Scripts/UI/RewardText` | "x2" / "15m" cho 1 món quà (DailyChallenge, LuckyWheel dùng chung) |
| `Features/Gameplay/Tests/Editor/GridGeometryTests` | 3 test ô "dính" chuyển từ `FeelCurvesTests` |

**Sửa / di chuyển**

- `LivesEnergyConfig`, `EconomyConfig` → `Features/Gameplay/Scripts/Config` (GUID giữ, asset không đổi); `EnergyManager.Init(..., maxEnergy, refillSeconds)`, `RefillEnergyPopup.Init(..., refillAdsEnergy, refillCoinCost)`.
- `DailyChallengeManager` gọi `IDailyLevelLoader`; `DailyChallengeDatabase` bỏ `generator`.
- `MenuShortcutButton` → `Shared/Scripts/UI` (GUID giữ): `opensShop` (Starter, No Ads) phát `ShopOpenRequestedEvent` thay `navigator` + `tab`; `CoinHUD` cũng mở Shop qua event.
- `IMenuPopups.OpenQuitLevel()` (GameFlowUI cài); `SettingsPopup` bỏ `quitPopup`.
- `FeelCurves.StickyCell` → `GridGeometry.StickyCell`.
- Bỏ field không dùng `rewardIcons` (DailyChallengePanel, LuckyWheelPopup).
- 13 builder (Booster, Difficulty, Stars, Tutorial, WinScreen, DailyChallenge, DailyReward, LuckyWheel, Profile, Settings, Shop, BackgroundPattern, UIZone) → `Game/Editor/Builders` (namespace `PawBlock.Game`); xóa 6 thư mục `Editor/` rỗng của feature Meta; builder bỏ các dòng gán field đã xóa ở P5 / P6 (`gameFlow`, `navigator`, `tab`, `quitPopup`).
- `LevelEditorWindow` vào ván qua `IScreenNavigator`; `BoosterInventoryTests` không cần `CoinWallet`; gỡ `using` thừa toàn dự án.

## P6.2 – asmdef

| Assembly | Thư mục |
| --- | --- |
| `DOTween.Modules` | `Plugins/Demigiant/DOTween/Modules` (ASMDEFManager của DOTween) |
| `PawBlock.Core.Contracts`, `PawBlock.Core.Foundation` | `Core/Contracts`, `Core/Foundation` |
| `PawBlock.Shared` | `Shared` |
| `PawBlock.Gameplay` | `Features/Gameplay` |
| `PawBlock.Meta` | `Features/` (Economy, Shop, DailyReward, DailyChallenge, LuckyWheel, Profile, Settings) |
| `PawBlock.Game` | `Game` |
| `PawBlock.Core.Foundation.Editor`, `PawBlock.Shared.Editor`, `PawBlock.Gameplay.Editor`, `PawBlock.Game.Editor` | các thư mục `Editor/` |
| `PawBlock.Core.Foundation.Tests`, `PawBlock.Gameplay.Tests`, `PawBlock.Meta.Tests` (+ 3 `.asmref`), `PawBlock.Game.Tests` | các thư mục `Tests/` |

## Kiểm tra

- Compile sạch ngay khi thêm asmdef; thử thêm tham chiếu `PawBlock.Game.ScreenFlow` trong Gameplay → lỗi CS0234 (đã gỡ).
- 160 / 160 test trên 4 assembly test.
- SampleScene + prefab dự án 0 Missing Script (2176 Missing Script có sẵn trong prefab art gốc `08_Prefabs`, 5 script bản gốc không có trong dự án).
- Build Android 0 lỗi, APK 59.0 MB.
- Play với `save_v1_played.json`: năng lượng 5/5, hồi 1800 s, nạp 900 coin; Coin / Starter mở Shop, Daily ở Home mở popup (tự mở lúc vào game, không tự nhận); chơi Daily (`Daily_20261013`, HUD "OCT 13", thắng → ngày Done); Settings → Home trong ván hỏi thoát; Hint, thắng level thường.
- Save thật của máy dev trả về bản gốc (md5 khớp).

## Ghi chú

- `PawBlock.Meta` chung cho mọi feature meta (theo plan): compiler không chặn Meta ↔ Meta; còn `RewardGranterTests` → `CoinWallet`.
- Phát hiện: dừng Play rồi trả save ngay thì `SaveRunner` (ghi trong `OnDestroy`) có thể ghi đè sau đó; quy trình thử nay chờ Unity thoát Play hẳn.
- Builder trong `Game/Editor/Builders` chỉ được dọn tối thiểu cho compile; P7 thay bằng prefab.
