# Hướng dẫn mở rộng dự án

Công thức chung cho mọi tính năng mới, rồi từng trường hợp hay gặp. Thêm **cơ chế** xem riêng [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md).

## Công thức chung

```text
1. Dữ liệu cấu hình → ScriptableObject trong Assets/_Project/Configs/ (số liệu chỉnh trong Inspector)
2. Luật            → class C# thuần (…Model) hoặc static service, không đụng Unity UI
3. Điều phối       → Manager MonoBehaviour trên object Manager (Singleton<T>), phát UnityEvent
4. Lưu             → field trong SaveData (mặc định tại khai báo) + SaveSystem.MarkDirty()
5. Hiển thị        → View / UI nghe event, không tự quyết định luật
6. Dựng UI         → Builder trong Editor/UI/, menu Tools → PawBlock → Builders
7. Phụ trợ         → âm thanh (GameAudioEvents), cheat (DebugCheats + Debug Panel), docs
```

Câu hỏi tự kiểm tra trước khi merge:

- [ ] Xóa View đi thì luật vẫn chạy (test được bằng code)?
- [ ] Đổi số liệu không cần sửa code?
- [ ] Tính năng mới chỉ **nghe** event của hệ thống cũ, không bắt hệ thống cũ gọi mình?
- [ ] Chạy lại Builder 2 lần ra cùng kết quả?
- [ ] Save cũ (thiếu field mới) vẫn mở được?
- [ ] Enum mới thêm ở **cuối**, lưu bằng **tên**?
- [ ] Cần chặn kéo thả thì đã thêm vào `GridManager.IsInputLocked`; cần tạm dừng ván thì `LevelManager.Pause / Resume`?
- [ ] Docs: file feature tương ứng + `ProjectStructure.md` + RoadMap?

---

## Thêm booster

Ví dụ **"Undo Mistake"**: hồi lại 1 mạng vừa mất (`LivesManager.AddLives(1)`).

1. `BoosterDatabase.cs`: thêm **cuối** `BoosterType` (`UndoMistake = 3`).
2. `Configs/BoosterDatabase.asset`: thêm 1 `BoosterData` (tên, `detail` = lời nhắc tutorial, `levelUnlock`, `valueStart`, `valueOneBuy`, `priceOneBuy`, icon).
3. `BoosterManager.Press`: thêm `case BoosterType.UndoMistake: UseUndo(); break;`. Trong `UseUndo` làm việc, **chỉ gọi `Consume(type)` khi có tác dụng**.
   - Đặt khối: lấy từ `Board.TrySolveRemaining`, kiểm tra `Board.Check(r).IsReady`, gọi `GridManager.PlaceSolved(r)`.
   - Chạy theo thời gian: giữ tween như `_revealPlacing` để `IsBusy` khóa lưới.
   - Có trạng thái "đang bật" (như Khiên): thêm vào `IsActive`.
4. UI: `BoosterBuilder.Build` thêm `BuildButton(…/Booster4, BoosterType.UndoMistake, db)` (thêm ô trong `DownPanel/Booster`), chạy **Build Boosters**. `BoosterButton` tự đọc icon / số / khóa.
5. Tutorial mở khóa tự chạy theo `levelUnlock` (không cần code). Muốn bước 2 riêng như Khiên: xem `BoosterTutorial` (`Demonstrating`).
6. Shop / Daily nếu bán / tặng: thêm field `ShopProduct` + `ShopService.Grant`; Daily dùng `DailyRewardKind.Booster` sẵn có.
7. Âm: `GameAudioEvents` đã nghe `onBoosterUsed` cho mọi loại.
8. Debug Panel tự liệt kê mọi `BoosterType`.

## Thêm popup

1. Script `UI/Popups/XxxPopup.cs` kế thừa `PopupView`; khởi tạo trong `OnOpen()`; nút đóng gọi `Close()`.
2. Mở giữa ván → `LevelManager.Instance.Pause()` khi mở, `Resume()` khi đóng.
3. Thêm field `[SerializeField] XxxPopup` vào `GameFlowUI`, mở từ đó (nghe event hoặc hàm public `OpenXxx()`), thêm vào `CloseAll` / `OnLevelLoaded`.
4. Dựng UI trong `PopupBuilder` (hoặc builder riêng như `SettingsBuilder`): `Dim` + `Panel` + nội dung, tái dùng khung chung; nối tham chiếu `GameFlowUI` bằng `PopupBuilder.Assign`.
5. Thông báo đơn giản không cần popup mới: `GameFlowUI.ShowNotice(message, title, onClose)`.

## Thêm gói Shop

- Gói cùng kiểu với mục có sẵn: thêm `ShopProduct` vào `ShopDatabase.asset` (đúng `ShopSection`) → **Build Shop**.
- Mục mới (vd. "Booster Pack"): thêm giá trị **cuối** `ShopSection`, dựng mục trong `ShopBuilder` (dùng `ShopCardView`), quà mới → field `ShopProduct` + `ShopService.Grant` + `ShopCardView`.
- Thanh toán thật: chỉ sửa `IAPService.Purchase` ([07](07_Economy-Shop.md)).

## Thêm loại quà điểm danh

`DailyRewardKind` (thêm cuối) → `DailyRewardManager.GrantItem` → `DailyRewardSlot` (icon + chữ) → icon mới trong `DailyRewardDatabase`. Xem [08](08_DiemDanh.md).

## Thêm âm thanh

`SoundId` (số mới) → **Setup Audio** → gán clip trong `AudioDatabase.asset` → handler trong `GameAudioEvents`. Xem [12](12_AmThanh-PhanHoi.md).

## Thêm dữ liệu save

```csharp
// SaveData.cs
public int bestStreak;                                   // kiểu đơn: mặc định ngay tại khai báo
public StatsSave stats = new StatsSave();                // object lồng: new sẵn

[Serializable] public class StatsSave { public int wins; public int losses; }
```

```csharp
// SaveSystem.Upgrade – file cũ thiếu object lồng thì JsonUtility để null
if (data.stats == null) data.stats = new StatsSave();
```

Ghi: sửa `SaveSystem.Data.xxx` rồi `SaveSystem.MarkDirty()`. Đổi ý nghĩa field cũ: tăng `SaveData.CurrentVersion` + chuyển đổi trong `Upgrade`. Hiển thị trong Debug: Tools → PawBlock → Save Data → Log Save.

## Thêm tab / màn hình Menu

Ví dụ làm tab **Progress** thật:

1. Nội dung panel: thay `BuildProgressPlaceholder` trong `MainMenuBuilder` (hiện dựng panel tạm) hoặc builder riêng như `ShopBuilder.BuildPanel`.
2. Script UI trong `UI/Menu/` đọc dữ liệu (save / database), nghe `MainMenuNavigator.onTabChanged` nếu cần làm mới khi mở tab.
3. Thêm tab thứ 4: thêm tên vào `TabNames` trong `MainMenuBuilder`, panel tương ứng trong `Panels` (đúng thứ tự tab), chạy **Build Main Menu**. `MainMenuNavigator.ShopTab` (0, dùng cho `ScreenFlow` và `CoinHUD`) và `ShowMenu` (`Select(1)` = Home) chọn tab theo chỉ số – chèn tab ở giữa thì sửa lại 2 chỗ này.

## Thêm hệ thống chạy theo ván (nhiệm vụ, chuỗi thắng, thống kê…)

Không sửa `GridManager` / `LevelManager`. Tạo `XxxManager : Singleton<XxxManager>` trên `Manager`, trong `Start()` nghe:

| Cần biết | Nghe |
| --- | --- |
| Vào level | `LevelManager.onLevelLoaded(n)` |
| Thắng / hết mạng / thua / tạm dừng | `LevelManager.onStateChanged(state)` |
| Đặt đúng / sai | `GridManager.onBlockPlaced`, `onMistake` |
| Thao tác sai luật | `GridManager.onInvalidMove(result, …)` |
| Mở khóa | `GridManager.onClueUnlocked` |
| Dùng booster | `BoosterManager.onBoosterUsed` |
| Coin / booster đổi | `CoinWallet.Changed`, `BoosterInventory.Changed` |
| Mua gói | `ShopService.Purchased` |
| Menu ↔ ván | `ScreenFlow.onScreenChanged(isMenu)` |

Nhớ `RemoveListener` trong `OnDestroy`. Lưu tiến độ trong `SaveData`.

## Thêm cheat

Hàm static trong `Gameplay/Cheats/DebugCheats` (chỉ gọi API công khai) → nút trong `Editor/Debug/DebugPanelWindow`.

## Gắn SDK (Ads, IAP, Firebase)

| SDK | Chỗ duy nhất phải sửa |
| --- | --- |
| Quảng cáo có thưởng | `RewardedAds.ShowAd` |
| Thanh toán | `IAPService.Purchase` (+ `IsInitialized`) |
| Analytics | Manager mới nghe các event ở bảng trên (không rải lệnh log khắp nơi) |
| Remote Config | Ghi đè giá trị ScriptableObject lúc khởi động (giá, số lượt, `levelUnlock`) – nơi dùng không đổi |
