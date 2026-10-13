# DailyChallenge

Tab thứ 3 của Main Menu: mỗi ngày 1 level (sinh theo seed ngày, mọi máy cùng level), lịch tháng (đã thắng / lỡ / hôm nay / chưa tới), chơi bù ngày lỡ bằng quảng cáo, quà mốc 7 / 14 / 21 ngày thắng trong tháng. Mở khóa từ 1 level nhất định.

Chi tiết hành vi: [16 Daily Challenge](../../../../Docs/Features/16_DailyChallenge.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Meta`; dùng `Core.Contracts`, `Core.Foundation`, `Shared` |
| Nghe event | `LevelCompletedEvent` (thắng level Daily → đánh dấu ngày) |
| Dùng interface | `IDailyLevelLoader` (Gameplay sinh + nạp level), `IRewardGranter` + `IRewardIcons` (DailyReward trao quà / icon), `IScreenNavigator` (`PlayDaily`), `IAdsService`, `IClock`, `ISaveService` |
| Save | Section `"dailyChallenge"` (`DailyChallengeSave`: ngày mở khóa, ngày đã thắng, mốc quà đã nhận) |
| Config | `Configs/DailyChallengeDatabase` (level mở khóa, quà 3 mốc) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `DailyChallengeModel` (C# thuần) | Luật: trạng thái từng ngày, quà theo tháng, tháng xem được |
| `DailyChallengeManager` | Giữ model + save, nạp level ngày qua `IDailyLevelLoader`, trao quà mốc qua `IRewardGranter`; ngày theo `IClock` |
| `DailyChallengePanel : MenuPanel` (prefab `DailyPanel`) | Tab Daily: tháng, thanh tiến trình + 3 hộp quà, lịch 7 cột, nút chơi |
| `DailyDayCell`, `DailyGiftSlot`, `DailyGiftBubble`, `GiftItemView` | Ô ngày, hộp quà mốc, bong thoại xem quà |
| `DailyTabBadge` | Ổ khóa / chấm đỏ trên tab Daily (`MenuTabBar` nối) |

## Cách dùng

```csharp
// GameBootstrap
dailyChallengeManager.Init(save, dailyRewardManager, reachedLevel, clock, dailyLevelLoader);
dailyChallengePanel.Init(ads, screenFlow, dailyRewardManager, dailyChallengeManager);
menuTabBar.Init(dailyChallengeManager); // chấm đỏ tab
```

`reachedLevel` là `Func<int>` (level cao nhất đã tới, của Gameplay) để biết đã mở khóa chưa mà không tham chiếu Gameplay.

## Ghi chú kiến trúc

- Level không lưu asset: Gameplay (`DailyLevelLoader`) sinh trong bộ nhớ bằng `LevelGenerator` với seed = ngày; DailyChallenge chỉ gọi `IDailyLevelLoader.LoadDaily(day)`.
- Quà dùng chung kiểu `DailyRewardItem` (Contracts) và trao qua `IRewardGranter`, nên không phụ thuộc DailyReward.
- Test: `Tests/Editor/DailyChallengeModelTests`.
