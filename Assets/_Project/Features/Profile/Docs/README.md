# Profile

Hồ sơ người chơi: ID 8 số, tên, avatar, tháng bắt đầu, level và lưới chỉ số (thắng lượt đầu, chuỗi thắng, coin nhận, booster đã dùng...). Popup dùng được cho cả hồ sơ người khác (sau này mở từ bảng xếp hạng, chỉ xem).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Meta`; dùng `Core.Contracts`, `Core.Foundation`, `Shared` |
| Nghe event | `LevelCompletedEvent`, `LevelFailedEvent`, `CoinsChangedEvent`, `BoosterUsedEvent` (ProfileStatsRecorder ghi chỉ số) |
| Dùng interface | `IScreenNavigator`, `IGamePause`, `IMenuPopups`, `IClock`, `ISaveService` |
| Save | Section `"profile"` (`ProfileSave`: ID, tên, avatar, ngày bắt đầu, chỉ số) |
| Config | `Configs/AvatarDatabase` (danh sách avatar) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `PlayerProfileService` (C# thuần) | Tạo hồ sơ lần đầu, đổi tên / avatar, `Local()` dựng `PlayerProfile` để hiển thị |
| `PlayerProfile`, `ProfileStats` | Dữ liệu chỉ đọc để vẽ (của mình hoặc người khác) |
| `ProfileStatsRecorder` | Nghe event contract, ghi chỉ số |
| `ProfilePopup` (prefab) | Avatar, tên (sửa được nếu `IsLocal`), lưới `ProfileStatTile`, chọn avatar (`AvatarItem`) |
| `AvatarButton` | Avatar trên Home, bấm mở popup qua `IMenuPopups` |

## Cách dùng

```csharp
// GameServices: level hiện tại của Gameplay truyền dạng Func<int>
Profile = new PlayerProfileService(save, clock, () => currentLevel);
profilePopup.Init(services.Profile, screenFlow, levelManager /* IGamePause */);
profileStatsRecorder.Init(services.Profile);
```

## Ghi chú kiến trúc

- Chỉ số lấy hoàn toàn từ event trong `Core/Contracts`, Profile không biết Gameplay / Economy. Màn tutorial không phát các event này nên không tính.
- Thêm chỉ số: thêm `ProfileStatType` + field trong `ProfileSave` (có giá trị mặc định) + chỗ ghi trong `ProfileStatsRecorder`.
