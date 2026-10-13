# 08 – Điểm danh 7 ngày (Daily Reward)

> Code ở `Assets/_Project/Features/DailyReward/Scripts/` ([README](../../_Project/Features/DailyReward/Docs/README.md)).

## Tính năng làm gì

- Chu kỳ 7 ngày theo **lịch máy**, ô i = ngày `cycleStart + i`. Ngày 7 là quà lớn.
- Mỗi ô có trạng thái **Claimed / Today / Missed / Future**. Hôm nay bấm CLAIM là nhận; ngày đã lỡ xem quảng cáo để nhận bù (`recoverMissedWithAd`).
- Quá 7 ngày → chu kỳ mới. Đồng hồ máy lùi → coi như ngày đầu.
- Vào game lần đầu trong ngày mà còn quà → tự mở popup (`autoOpen`). Nút Daily trên Home lắc + chấm đỏ khi còn quà.

## File liên quan

| File | Việc |
| --- | --- |
| `DailyRewardDatabase` (+ asset `Configs/DailyRewardDatabase.asset`) | `days[7]`, mỗi ngày nhiều `DailyRewardItem { kind, booster, amount }`; icon coin / vô hạn |
| `DailyRewardModel` | **Model** C# thuần: trạng thái từng ô, `MarkClaimed`, `Refresh(today)`; lưu section `"dailyReward"` (`DailyRewardSave { cycleStart, claimedMask }`) |
| `DailyRewardManager` | Trên `Manager`: `Claim(day)`, trao quà (`GrantItem`; cài `IRewardGranter` / `IRewardIcons` cho DailyChallenge, LuckyWheel), kiểm tra sang ngày mỗi 5 s và khi quay lại app; `onChanged`, `onClaimed` |
| `DailyRewardPopup`, `DailyRewardSlot` (prefab) | Popup 7 ô, CLAIM / "Next reward in" |
| `DailyRewardButton` | Nút Daily trên Home (nằm trong prefab `Game/Prefabs/Menu/HomePanel`) |

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Đổi quà | Sửa `DailyRewardDatabase.asset` trong Inspector (UI tự đọc) |
| Thêm loại quà (vd. "Thêm giờ") | 1. Thêm giá trị **cuối** `DailyRewardKind` (`Core/Contracts/Types`). 2. `case` trong `DailyRewardManager.GrantItem` / `IconOf`. 3. Icon + chữ trong `DailyRewardSlot` (2 `switch`: icon, số). 4. Icon mới thì thêm field vào database |
| Chu kỳ dài hơn 7 ngày | `DailyRewardDatabase.CycleDays` + thêm ô `DailyRewardSlot` trong prefab `DailyRewardPopup` (đang 3 hàng: 3 + 3 + 1) |
| Test | `DailyRewardManager` → chuột phải → Next Day / Reset, hoặc Debug Panel |

## Lưu ý

- `claimedMask` là bitmask 32 bit → chu kỳ tối đa 32 ngày.
- Ngày lấy từ `IClock` (`TrustedClockService.LocalNow`): có mạng thì theo giờ server (header `Date` HTTPS) + đồng hồ đơn điệu, đổi giờ máy không có tác dụng; offline thì giờ máy nhưng không lùi dưới mốc đã lưu (section `"clock"`). Đồng bộ xong (`IClock.Synced`) thì tính lại ngày. Đổi múi giờ máy vẫn lệch được tối đa ±1 ngày.
