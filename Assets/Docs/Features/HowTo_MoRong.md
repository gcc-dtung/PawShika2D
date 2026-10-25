# Hướng dẫn mở rộng dự án

Công thức chung cho mọi tính năng mới, rồi từng trường hợp hay gặp. Thêm **cơ chế** xem riêng [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md). Quy tắc kiến trúc: [UnityCodingGuidelines](../Refactor/UnityCodingGuidelines.md) + [ProjectConventions](../Refactor/ProjectConventions.md).

## Công thức chung

```text
1. Chọn mảng       → chỉ 1 mảng dùng: Runtime/<mảng> (sự kiện / liveops: Runtime/LiveOps/<Tên>); ≥ 2 mảng cần event / interface: Runtime/Core/Contracts;
                      hạ tầng không dính luật game: Runtime/Core/Foundation; widget / art dùng chung: Shared; nối nhiều feature: Game
2. Dữ liệu cấu hình → ScriptableObject trong Data/Config/ (số liệu chỉnh trong Inspector)
3. Luật             → class C# thuần (…Model / …Service), test trong Runtime/Tests/<mảng>/Editor
4. Điều phối        → MonoBehaviour trên object Manager (hoặc trong prefab), nhận dependency qua Init(...) do GameBootstrap gọi;
                      KHÔNG Singleton, KHÔNG Find / GetComponent lúc chạy (tham chiếu [SerializeField], Editor tự điền)
5. Giao tiếp        → trong feature: gọi thẳng / UnityEvent / event C#; giữa feature: EventBus<XxxEvent> hoặc interface trong Runtime/Core/Contracts
6. Lưu              → class section [Serializable] của feature (Key riêng), save.Get<T>(Key) + MarkDirty()
7. Hiển thị         → prefab trong Prefabs/UI/{Popups, Screens, Widgets/<mảng>} hoặc Prefabs/InGame, View nghe event, không tự quyết định luật
8. Nối              → GameBootstrap: thêm field (Collector tự điền) + 1 dòng Init
9. Phụ trợ          → âm thanh (GameAudioEvents), rung (GameHapticEvents), cheat (DebugCheats + Debug Panel), docs
```

Câu hỏi tự kiểm tra trước khi merge (đủ bộ: guide mục 15):

- [ ] Feature mới không `using` / tham chiếu / lồng prefab của feature khác (chung assembly `PawBlock.Runtime` nên compiler không bắt)?
- [ ] Xóa View đi thì luật vẫn chạy (test được bằng code)?
- [ ] Đổi số liệu không cần sửa code?
- [ ] Tính năng mới chỉ **nghe** event của hệ thống cũ, không bắt hệ thống cũ gọi mình?
- [ ] Save cũ (thiếu field mới) vẫn mở được?
- [ ] Enum mới thêm ở **cuối**, lưu bằng **tên**?
- [ ] Cần chặn kéo thả thì `InputGate.Lock(this)` / `Unlock(this)`; lớp phủ sau khi nạp level thì cài `ILevelIntro`; cần tạm dừng ván thì `IGamePause.Pause / Resume`?
- [ ] Không thêm `switch (type)` / cờ tĩnh mà hệ thống khác phải đọc? (dùng Strategy / EventBus / InputGate)
- [ ] Sprite mới nằm trong atlas của module, texture theo preset ([ProjectConventions](../Refactor/ProjectConventions.md) mục 13)?
- [ ] Docs: README feature + file `Docs/Features` tương ứng + RoadMap?

---

## Thêm booster

Ví dụ **"Thêm giờ"**: cộng 15 giây vào đồng hồ.

1. `Runtime/Core/Contracts/Types/BoosterType.cs`: thêm **cuối** (`AddTime = 3`).
2. `Data/Config/BoosterDatabase.asset`: thêm 1 `BoosterData` (tên, `detail` = lời nhắc tutorial, `levelUnlock`, `valueStart`, `valueOneBuy`, `priceOneBuy`, icon trong `Source/Art/UI/Gameplay`).
3. Luật: tạo `Runtime/Gameplay/Booster/AddTimeBooster.cs`:

   ```csharp
   public class AddTimeBooster : BoosterEffect
   {
       public override BoosterType Type => BoosterType.AddTime;

       public override void Activate()
       {
           LevelTimer timer = Host.Timer;
           if (timer == null || timer.IsTimeUp) return; // không có tác dụng: không trừ lượt
           Host.Consume(Type);
           Host.AfterEffect(Type, () => timer.AddTime(15f)); // chờ hiệu ứng icon bay tới
       }
   }
   ```

   rồi thêm 1 dòng `case BoosterType.AddTime: return new AddTimeBooster();` vào `BoosterEffectFactory`. **Không sửa `BoosterManager`.**
   - Đặt khối: lấy từ `Host.Grid.Board.TrySolveRemaining`, kiểm tra `Board.Check(r).IsReady`, gọi `Host.Grid.PlaceSolved(r)`; nhiều khối theo thời gian thì `Host.PlaceInSequence(rects)` (tự khóa lưới).
   - Có trạng thái "đang bật" (như Reveal): override `IsActive`, `PressWhileActive`, `Cancel`; báo nút vẽ lại bằng `Host.NotifyModeChanged(Type, on)`.
   - Cần chạm ô trên lưới: override `HandleCellTap`.
4. UI: mở prefab `Prefabs/UI/Screens/Game/GameplayHUD`, nhân bản 1 instance `BoosterButton` trong `UIZone/DownPanel/Booster`, đặt `type`. Lưu scene: Collector tự thêm nút vào danh sách của Bootstrap. `BoosterButton` tự đọc icon / số / khóa.
5. Tutorial mở khóa tự chạy theo `levelUnlock` (không cần code). Muốn bước 2 riêng: component mới nghe `EventBus<BoosterTutorialCompletedEvent>` (mẫu: `FreezeTutorialDemo`).
6. Shop / Daily nếu bán / tặng: thêm field `ShopProduct` + 1 dòng trong `ShopProduct.BoosterAmount` (`ShopService.Grant` tự duyệt mọi `BoosterType`); Daily / Lucky dùng `DailyRewardKind.Booster` sẵn có.
7. Âm / rung: `GameAudioEvents`, `GameHapticEvents` đã nghe `onBoosterUsed` cho mọi loại; Profile đếm qua `BoosterUsedEvent`.
8. Debug Panel tự liệt kê mọi `BoosterType`.

## Thêm popup

1. Script kế thừa `PopupView` (`Runtime/UI/Framework`) trong `Runtime/<mảng>` (popup của App thì `Runtime/App`): hàm riêng nhận dữ liệu rồi gọi `Open()`; khởi tạo trong `OnOpened()`, dọn trong `OnClosed()`; nút đóng gọi `Close()`. Dependency nhận qua `Init(...)`.
2. Mở giữa ván → `IGamePause.Pause()` khi mở, `Resume()` khi đóng.
3. Prefab trong `Prefabs/UI/Popups`: gốc có `CanvasGroup` + `Canvas` + `GraphicRaycaster`, `Dim` + `Panel` + nội dung, dùng widget `Prefabs/UI/Widgets` (`CloseButton`, `Button_Primary`). Đặt instance **tắt sẵn** vào `Canvas_Popup/Popups` trong scene.
4. Ai mở: popup dùng chung từ nhiều nơi → thêm hàm vào `IMenuPopups` + cài trong `GameFlowUI`; chỉ 1 feature mở → feature đó giữ tham chiếu. Thêm field + `Init` trong `GameBootstrap`. Popup tự vào danh sách đóng khi đổi level (Bootstrap thu mọi `PopupView`); muốn giữ thì override `CloseOnLevelLoad`.
5. Thông báo đơn giản không cần popup mới: `GameFlowUI.ShowNotice(message, title, onClose)`.

### Popup hướng dẫn sự kiện / liveops (nút "i")

Không cần script mới. Base: `Prefabs/UI/Popups/TutorialPopup` (`TutorialPopup`: nền tối, băng-rôn treo dây bật lên, các bước bật lên lần lượt, "Tap to continue"; chạm lần 1 hiện hết, lần 2 đóng).
1. Tạo **Prefab Variant** của `TutorialPopup` cạnh prefab sự kiện (mẫu: `Prefabs/UI/Screens/Home/WeeklyTutorialPopup`), đổi chữ `Banner/Title`.
2. Dưới `Steps` đặt mỗi bước 1 object (ảnh + `Caption`), mũi tên `Source/Art/UI/Common/Tutorial/TutorialArrow` (mũi tên ngược chiều xoay Y 180, đừng scale âm vì hiệu ứng pop đặt lại scale). Điền mảng `steps` (`content`, `arrow` của bước đó, bước cuối để trống).
3. Đặt instance tắt vào `Canvas_Popup/Popups` (`Dim` chỉ là Image chặn raycast, chạm do `TutorialPopup.OnPointerClick` bắt; **đừng thêm Button vào Dim**, `UIAutoSetup` sẽ gắn `ButtonPress` làm nền lún khi nhấn); nút "i" của sự kiện gắn `TutorialButton`, trường `popup` override trên instance trong scene (Gán từ ngoài).

## Thêm gói Shop

- Gói cùng kiểu với mục có sẵn: thêm `ShopProduct` vào `Data/Config/ShopDatabase.asset` (đúng `ShopSection`) → mở prefab `Prefabs/UI/Screens/Home/ShopPanel`, nhân bản 1 thẻ cùng kiểu, đặt `productId`.
- Mục mới (vd. "Booster Pack"): thêm giá trị **cuối** `ShopSection`, thêm khối thẻ trong prefab `ShopPanel` (dùng `ShopCardView`), quà mới → field `ShopProduct` + `ShopService.Grant` + `ShopCardView`.
- Thanh toán thật: chỉ thay cài đặt `IIAPService` ([07](07_Economy-Shop.md)).

## Thêm loại quà (điểm danh, Daily Challenge, vòng quay)

`DailyRewardKind` (Contracts, thêm cuối) → `DailyRewardManager.GrantItem` + `IconOf` → `DailyRewardSlot` (chữ số lượng) → icon mới trong `DailyRewardDatabase`. Daily Challenge và Lucky Wheel dùng chung qua `IRewardGranter` / `IRewardIcons` nên tự nhận. Xem [08](08_DiemDanh.md).

## Thêm âm thanh

`SoundId` (Contracts, số mới) → file vào `Source/Audio` → **Setup Audio** → gán clip trong `Data/Config/AudioDatabase.asset` → handler trong `GameAudioEvents`. Xem [12](12_AmThanh-PhanHoi.md).

## Thêm dữ liệu save

```csharp
// Field mới trong section có sẵn (vd. Runtime/Profile/ProfileSave.cs): mặc định ngay tại khai báo
public int bestStreak;
public StatsSave stats = new StatsSave();   // object lồng: new sẵn (file cũ thiếu thì JsonUtility giữ mặc định)

// Feature mới: section riêng, không sửa file chung
[Serializable]
public class QuestSave
{
    public const string Key = "quest";
    public List<string> done = new List<string>();
}

QuestSave data = save.Get<QuestSave>(QuestSave.Key); // save: ISaveService nhận qua Init
data.done.Add(id);
save.MarkDirty();
```

Đổi ý nghĩa field cũ: tăng `JsonFileSaveService.CurrentVersion` + bước chuyển trong `SaveMigrator` + test. Xem nhanh: Tools → PawBlock → Save Data → Log Save. Chi tiết: [11](11_Save.md).

## Thêm tab / màn hình Menu

Ví dụ có sẵn: tab **Daily** ([16](16_DailyChallenge.md)).

1. Script gốc của tab kế thừa `MenuPanel` (Shared): giữ `[SerializeField]` view con, `Init(...)` nối dependency cho chúng, làm mới trong `OnShown()`. Tab của 1 feature đặt ở feature đó; tab ghép nhiều feature đặt ở `Runtime/UI/Home`.
2. Prefab tab (`Prefabs/UI/Screens/Home`), gốc có `RectMask2D` + Canvas con. Lồng vào `Prefabs/UI/Screens/Home/MainMenu/Panels` đúng thứ tự tab.
3. Thêm vào `MainMenuNavigator.menuPanels` và 1 `MenuTabButton` trong prefab `MenuTabBar`. `MainMenuNavigator.ShopTab` / `RankTab` / `HomeTab` / `DailyTab` / `CollectionTab` (0 / 1 / 2 / 3 / 4) là chỉ số tab – chèn tab ở giữa thì sửa các hằng này.
4. `GameBootstrap`: thêm field tab + 1 dòng `Init`.

## Thêm hệ thống chạy theo ván (nhiệm vụ, chuỗi thắng, thống kê…)

Không sửa `GridManager` / `LevelManager`. Tạo feature mới (`Features/Quest`) hoặc component trong Gameplay, nghe event (EventBus: `Subscribe` trong `OnEnable`, `Unsubscribe` trong `OnDisable`; UnityEvent: `AddListener` / `RemoveListener` cặp đôi):

| Cần biết | Feature khác nghe (Contracts) | Trong Gameplay nghe |
| --- | --- | --- |
| Vào level | `EventBus<LevelLoadedEvent>` (`Number`, `LevelId`, `IsDaily`) | |
| Thắng / hết giờ / thua / tạm dừng | `EventBus<GameStateChangedEvent>` (`State`) | |
| Kết quả ván | `LevelCompletedEvent`, `LevelFailedEvent` | |
| Đặt / gỡ khối, sai | | `GridManager.onBlockPlaced`, `onBlockRemoved`, `onMistake` |
| Thao tác sai luật, mở khóa | | `GridManager.onInvalidMove`, `onClueUnlocked` |
| Dùng booster | `BoosterUsedEvent` | `BoosterManager.onBoosterUsed` |
| Coin / sao đổi | `CoinsChangedEvent`, `StarsBalanceChangedEvent` | |
| Sao trong ván | `StarsChangedEvent` | |
| Menu ↔ ván | `EventBus<ScreenChangedEvent>` (`IsMenu`) | |
| Booster mở khóa / khóa lại | `BoosterLockChangedEvent` | |

Sự kiện mới dùng giữa module: thêm `readonly struct XxxEvent : IGameEvent` vào `Runtime/Core/Contracts/Events/`, bên phát `EventBus<XxxEvent>.Raise(...)`. Lưu tiến độ trong section save riêng.

## Thêm lớp phủ sau khi nạp level (intro, tutorial, quảng cáo giữa màn…)

Cài `ILevelIntro` trên 1 component (Gameplay):

```csharp
public class StreakIntro : MonoBehaviour, ILevelIntro
{
    public int Order => 15;                 // sau popup cơ chế (10), trước tutorial booster (20)
    public float StartDelay => 0.2f;
    public bool ShouldPlay(LevelIntroContext level) => false;          // có cần hiện không
    public void Play(LevelIntroContext level, Action onDone) { }       // hiện UI, xong gọi onDone() đúng 1 lần
    public void Cancel() { }                                           // tắt UI, không gọi onDone
}
```

Thêm vào mảng `ILevelIntro[]` mà `GameBootstrap` truyền cho `LevelIntroSequencer.Init`. Sequencer chờ lưới hiện xong và khóa lưới trong lúc chạy. Không sửa `GridManager` hay intro khác.

## Thêm cheat

Hàm static trong `Runtime/Cheat/DebugCheats` (chỉ gọi API công khai) → nút trong `Runtime/Editor/App/DebugPanelWindow` (và `CheatPanel` nếu cần trong bản build).

## Gắn SDK (Ads, IAP, Firebase)

| SDK | Chỗ duy nhất phải sửa |
| --- | --- |
| Quảng cáo có thưởng | Class mới cài `IAdsService`, thay `FakeAdsService` trong `GameBootstrap` |
| Thanh toán | Class mới cài `IIAPService`, thay `FakeIAPService` trong `GameBootstrap` |
| Analytics | Component mới nghe các event ở bảng trên (không rải lệnh log khắp nơi) |
| Remote Config | Ghi đè giá trị ScriptableObject lúc khởi động trong `GameBootstrap` (giá, số lượt, `levelUnlock`) – nơi dùng không đổi |
