# Hướng dẫn mở rộng dự án

Công thức chung cho mọi tính năng mới, rồi từng trường hợp hay gặp. Thêm **cơ chế** xem riêng [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md).

## Công thức chung

```text
1. Dữ liệu cấu hình → ScriptableObject trong Assets/_Project/Configs/ (số liệu chỉnh trong Inspector)
2. Luật            → class C# thuần (…Model) hoặc static service, không đụng Unity UI
3. Điều phối       → Manager MonoBehaviour trên object Manager (Singleton<T>); sự kiện nhiều nơi nghe → EventBus (GameEvents.cs),
                     sự kiện nối trong Inspector / nội bộ → UnityEvent
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
- [ ] Cần chặn kéo thả thì `InputGate.Lock(this)` / `Unlock(this)`; lớp phủ sau khi nạp level thì implement `ILevelIntro`; cần tạm dừng ván thì `LevelManager.Pause / Resume`?
- [ ] Không thêm `switch (type)` / cờ tĩnh `Active` mà hệ thống khác phải đọc? (dùng Strategy / EventBus / InputGate)
- [ ] Docs: file feature tương ứng + `ProjectStructure.md` + RoadMap?

---

## Thêm booster

Ví dụ **"Undo Mistake"**: hồi lại 1 mạng vừa mất (`LivesManager.AddLives(1)`).

1. `BoosterDatabase.cs`: thêm **cuối** `BoosterType` (`UndoMistake = 3`).
2. `Configs/BoosterDatabase.asset`: thêm 1 `BoosterData` (tên, `detail` = lời nhắc tutorial, `levelUnlock`, `valueStart`, `valueOneBuy`, `priceOneBuy`, icon).
3. Luật: tạo `Gameplay/Booster/Effects/UndoMistakeBooster.cs`:

   ```csharp
   public class UndoMistakeBooster : BoosterEffect
   {
       public override BoosterType Type => BoosterType.UndoMistake;

       public override void Activate()
       {
           LivesManager lives = Host.Lives;
           if (lives == null || lives.CurrentLives >= lives.MaxLives) return; // không có tác dụng: không trừ lượt
           lives.AddLives(1);
           Host.Consume(Type);
       }
   }
   ```

   rồi thêm 1 dòng `case BoosterType.UndoMistake: return new UndoMistakeBooster();` vào `BoosterEffectFactory`. **Không sửa `BoosterManager`.**
   - Đặt khối: lấy từ `Host.Grid.Board.TrySolveRemaining`, kiểm tra `Board.Check(r).IsReady`, gọi `Host.Grid.PlaceSolved(r)`; nhiều khối theo thời gian thì `Host.PlaceInSequence(rects)` (tự khóa lưới).
   - Có trạng thái "đang bật" (như Khiên / Reveal): override `IsActive`, `PressWhileActive`, `Cancel`; báo nút vẽ lại bằng `Host.NotifyModeChanged(Type, on)`.
   - Cần chạm ô trên lưới: override `HandleCellTap`.
4. UI: `BoosterBuilder.Build` thêm `BuildButton(…/Booster4, BoosterType.UndoMistake, db)` (thêm ô trong `DownPanel/Booster`), chạy **Build Boosters**. `BoosterButton` tự đọc icon / số / khóa.
5. Tutorial mở khóa tự chạy theo `levelUnlock` (không cần code). Muốn bước 2 riêng như Khiên: component mới nghe `EventBus<BoosterTutorialCompletedEvent>` (mẫu: `ShieldTutorialDemo`).
6. Shop / Daily nếu bán / tặng: thêm field `ShopProduct` + 1 dòng trong `ShopProduct.BoosterAmount` (`ShopService.Grant` tự duyệt mọi `BoosterType`); Daily dùng `DailyRewardKind.Booster` sẵn có.
7. Âm: `GameAudioEvents` đã nghe `onBoosterUsed` cho mọi loại.
8. Debug Panel tự liệt kê mọi `BoosterType`.

## Thêm popup

1. Script `UI/Popups/XxxPopup.cs` kế thừa `PopupView`; khởi tạo trong `OnOpen()`; nút đóng gọi `Close()`.
2. Mở giữa ván → `LevelManager.Instance.Pause()` khi mở, `Resume()` khi đóng.
3. Thêm field `[SerializeField] XxxPopup` vào `GameFlowUI` (+ mảng `AllPopups` để đóng lúc khởi động), mở từ đó (nghe event hoặc hàm public `OpenXxx()`). Đóng khi đổi level là tự động (`PopupView`); muốn giữ lại thì override `CloseOnLevelLoad`.
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

Ví dụ có sẵn: tab **Daily** (thay khung tạm "Progress" cũ), xem [16_DailyChallenge.md](16_DailyChallenge.md).

1. Nội dung panel: builder riêng trả về panel (như `DailyChallengeBuilder.BuildPanel`, `ShopBuilder.BuildPanel`), gọi từ `MainMenuBuilder`.
2. Script UI trong `UI/Menu/` đọc dữ liệu (save / database), nghe `MainMenuNavigator.onTabChanged` hoặc sự kiện của manager (vd. `DailyChallengeManager.Changed`) để làm mới.
3. Thêm tab thứ 4: thêm tên vào `TabNames` trong `MainMenuBuilder`, panel tương ứng trong `Panels` (đúng thứ tự tab), chạy **Build Main Menu**. `MainMenuNavigator.ShopTab` / `HomeTab` / `DailyTab` (0 / 1 / 2, dùng cho `ScreenFlow`, `CoinHUD`) chọn tab theo chỉ số – chèn tab ở giữa thì sửa lại các hằng này.

## Thêm hệ thống chạy theo ván (nhiệm vụ, chuỗi thắng, thống kê…)

Không sửa `GridManager` / `LevelManager`. Tạo `XxxManager : Singleton<XxxManager>` trên `Manager`, nghe (EventBus: `Subscribe` trong `OnEnable`, `Unsubscribe` trong `OnDisable`; UnityEvent: trong `Start` / `OnDestroy`):

| Cần biết | Nghe |
| --- | --- |
| Vào level | `EventBus<LevelLoadedEvent>` (`Number`, `Level`) |
| Thắng / hết mạng / thua / tạm dừng | `EventBus<GameStateChangedEvent>` (`State`) |
| Đặt đúng / sai | `GridManager.onBlockPlaced`, `onMistake` |
| Thao tác sai luật | `GridManager.onInvalidMove(result, …)` |
| Mở khóa | `GridManager.onClueUnlocked` |
| Dùng booster | `BoosterManager.onBoosterUsed` |
| Coin / booster đổi | `CoinWallet.Changed`, `BoosterInventory.Changed` |
| Mua gói | `ShopService.Purchased` |
| Menu ↔ ván | `EventBus<ScreenChangedEvent>` (`IsMenu`) |
| Booster mở khóa / khóa lại | `EventBus<BoosterLockChangedEvent>` |

Sự kiện mới dùng chung nhiều nơi: thêm `readonly struct XxxEvent : IGameEvent` vào `Gameplay/Events/GameEvents.cs`, bên phát `EventBus<XxxEvent>.Raise(...)`. Lưu tiến độ trong `SaveData`.

## Thêm lớp phủ sau khi nạp level (intro, tutorial, quảng cáo giữa màn…)

Implement `ILevelIntro` trên 1 component trong scene:

```csharp
public class StreakIntro : MonoBehaviour, ILevelIntro
{
    public int Order => 15;                 // sau popup cơ chế (10), trước tutorial booster (20)
    public float StartDelay => 0.2f;
    public bool ShouldPlay(LevelLoadedEvent level) => false; // có cần hiện không
    public void Play(LevelLoadedEvent level, Action onDone) { } // hiện UI, xong gọi onDone() đúng 1 lần
    public void Cancel() { }               // tắt UI, không gọi onDone
}
```

`LevelIntroSequencer` tự tìm, chờ lưới hiện xong và khóa lưới trong lúc chạy. Không sửa `GridManager` hay intro khác.

## Thêm cheat

Hàm static trong `Gameplay/Cheats/DebugCheats` (chỉ gọi API công khai) → nút trong `Editor/Debug/DebugPanelWindow`.

## Gắn SDK (Ads, IAP, Firebase)

| SDK | Chỗ duy nhất phải sửa |
| --- | --- |
| Quảng cáo có thưởng | `RewardedAds.ShowAd` |
| Thanh toán | `IAPService.Purchase` (+ `IsInitialized`) |
| Analytics | Manager mới nghe các event ở bảng trên (không rải lệnh log khắp nơi) |
| Remote Config | Ghi đè giá trị ScriptableObject lúc khởi động (giá, số lượt, `levelUnlock`) – nơi dùng không đổi |
