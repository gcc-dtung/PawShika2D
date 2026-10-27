# Teams

Đội (giống guild của bản gốc), **giả lập offline** (chưa có server): danh sách đội giả sinh cố định theo seed, tìm theo tên, tạo đội (trừ coin), vào / rời đội, bảng tin My Team (ai vào đội, tin nhắn, xin mạng – đồng đội giả giúp theo thời gian). Chưa có quà đội, kho báu đội như bản gốc.

Chi tiết hành vi: [17 Teams](../Features/17_Teams.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Runtime` (chung); chỉ dùng `Core.Contracts`, `Core.Foundation` |
| Phát / nghe event | Không; `TeamService.Changed` (C# event) cho UI |
| Dùng interface | `ICurrencyWallet` (coin tạo đội), `ISaveService`, `IClock`; cấp / tên / avatar người chơi qua `Func`, cộng mạng qua `Action<int>` (Bootstrap nối `EnergyManager.Add`) |
| Save | Section `"teams"` (`TeamSave`: `joinedId`, đội tự tạo `created`, bảng tin `feed`, `lastLifeRequest`) |
| Config | `Data/Config/TeamsDatabase` (logo, sức chứa 50, giá tạo 3000, khoảng / bước cấp yêu cầu, seed, tên đội / mô tả / tên thành viên giả) – `GameConfig.teams` trỏ tới |

## Class chính

| Class | Vai trò |
| --- | --- |
| `TeamService` (C# thuần) | `Top`, `Search`, `CanJoin` / `Join` / `Leave`, `Create`, `MembersOf` / `ScoreOf` / `RankOf`, `FakeMembers` (cố định theo id đội), `StepRequiredLevel`; bảng tin `Feed`, `SendMessage`, `RequestLives` / `LifeRequestCooldownLeft` / `UpdateLifeHelps`; event `Changed`, `FeedChanged`; đội tự tạo id `"my"`, rời là giải tán |
| `TeamInfo`, `TeamMember`, `TeamFeedItem`, `TeamActivity`, `TeamJoinResult`, `TeamCreateResult` | Dữ liệu đội / thành viên / dòng bảng tin, kết quả thao tác |
| `TeamRow` | 1 dòng danh sách: hạng, logo, tên, "Capacity x/50", nút View; đội đang ở có viền sáng |
| `TeamMemberRow` | 1 dòng thành viên: hạng (top 3 huy chương), avatar, tên, Leader / lần chơi cuối, ô Level, viền người chơi; bấm → `Clicked` (menu thành viên) |
| `TeamFeedRow` | 1 dòng bảng tin (3 mẫu: thông báo, bong bóng tin nhắn trái / phải, thẻ xin mạng) |

Tab Đội (`Runtime/UI/Home/TeamsPanel`, prefab `Prefabs/UI/Screens/Home/TeamsPanel`) ghép Teams + Economy (`CoinHUD`) + Profile (tên / avatar / cấp người chơi) nên nằm ở Game. Ảnh rip (khung, nút, 20 logo) trong `Source/Art/UI/Home/HalloweenRip/Teams/` (gitignore).

## Cách dùng

```csharp
// GameBootstrap
var teams = new TeamService(save, s.Coins, () => s.Profile.Local().Level, config.teams, clock,
    () => (me.Name, me.AvatarId), lives => energyManager.Add(lives));
teamsPanel.Init(teams, s.Coins, s.Profile, gameFlowUI);
```

## Ghi chú

- Danh sách giả: `TeamsDatabase.teamNames` (mỗi tên 1 đội), số thành viên / logo / cấp yêu cầu random theo `seed` (≈ 35 % đội gần đầy). Đổi seed là đổi cả danh sách.
- Thêm thao tác mới (chat, quà...) cần server thật: thay phần sinh dữ liệu trong `TeamService` bằng gọi API, UI giữ nguyên.
