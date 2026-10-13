# DailyReward

Điểm danh 7 ngày: mỗi ngày 1 ô quà (coin / booster / năng lượng vô hạn), lỡ ngày thì ô thành "Missed" (bù bằng quảng cáo nếu bật), tự mở popup lần đầu trong ngày ở Main Menu. Feature này cũng là nơi **trao quà và cấp icon quà** cho DailyChallenge và LuckyWheel.

Chi tiết hành vi: [08 Điểm danh](../../../../Docs/Features/08_DiemDanh.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Meta`; dùng `Core.Contracts`, `Core.Foundation`, `Shared` |
| Cài interface | `IRewardGranter` (trao quà), `IRewardIcons` (icon quà) — `DailyRewardManager` |
| Dùng interface | `ICurrencyWallet`, `IBoosterInventory`, `IBoosterCatalog` (icon booster), `IEnergyService`, `IClock`, `IAdsService`, `IScreenNavigator`, `IMenuPopups` (tự mở popup), `ISaveService` |
| Save | Section `"dailyReward"` (`DailyRewardSave`: ngày bắt đầu chu kỳ, ô đã nhận) |
| Config | `Configs/DailyRewardDatabase` (quà 7 ngày, icon coin / năng lượng, `recoverMissedWithAd`) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `DailyRewardModel` (C# thuần) | Luật chu kỳ 7 ngày theo lịch, trạng thái từng ô |
| `DailyRewardManager` | Giữ model + save, `Claim(day)`, sang ngày mới tự cập nhật, `Grant` / `IconOf` cho feature khác |
| `DailyRewardPopup` (prefab) | 7 ô quà, nút CLAIM / đồng hồ tới ngày mai |
| `DailyRewardSlot` (prefab) | 1 ô: Day n, quà, trạng thái (hôm nay / đã nhận / lỡ / chưa tới) |
| `DailyRewardButton` | Nút Daily trên Home: lắc + chấm đỏ khi còn quà |

## Cách dùng

```csharp
// GameBootstrap
dailyRewardManager.Init(save, coins, boosterManager, energyManager, clock, ads, screenFlow, boosterManager, gameFlowUI);
dailyChallengeManager.Init(save, dailyRewardManager /* IRewardGranter */, reachedLevel, clock, dailyLevelLoader);
luckyWheelPopup.Init(stars, dailyRewardManager /* IRewardGranter */, dailyRewardManager /* IRewardIcons */);
```

## Ghi chú kiến trúc

- `IRewardGranter` / `IRewardIcons` đặt ở DailyReward vì feature này đã cần trao mọi loại quà; feature khác dùng qua contract nên không phụ thuộc chéo.
- Thêm loại quà: thêm giá trị `DailyRewardKind` (Contracts) + nhánh trong `DailyRewardManager.Grant` / `IconOf` (xem [HowTo_MoRong](../../../../Docs/Features/HowTo_MoRong.md)).
- Test: `Tests/Editor/RewardGranterTests`.
