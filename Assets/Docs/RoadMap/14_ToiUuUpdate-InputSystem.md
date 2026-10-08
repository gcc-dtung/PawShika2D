# 14 – Tối ưu Update, chuyển Input System mới

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-09 00:30 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 11:31 | Giảm Update / LateUpdate, Input System mới | Mặt mèo chạy chung 1 Update; chữ ẩn không đẩy mesh mỗi frame; Save / tutorial chỉ chạy khi có việc; nền chỉ ghi transform khi camera đổi; đọc input bằng Input System package |
| Chiều | Số trên khối mèo rõ hơn | Số cùng tông màu khối, đậm và tối hơn; sửa chữ TMP bị nhạt do Linear color space |
| Chiều | Ổ khóa giống bản gốc | Thân khóa thon lại, quai to hơn, số nhỏ và sẫm; `TextColor.Display` dùng chung cho chữ TMP |
| 00:30 (09/10) | Rà SOLID: EventBus, InputGate, hàng đợi intro, Booster Strategy | Hệ thống giao tiếp qua `EventBus<T>`; khóa lưới qua `InputGate`; băng HARD / popup cơ chế / tutorial booster chạy bằng `LevelIntroSequencer`; booster mới chỉ cần 1 class + 1 dòng Factory; popup tự đóng khi đổi level; camera tách khỏi `LevelManager` |

---

## 11:31 – Giảm Update / LateUpdate, chuyển Input System mới

### Mục tiêu / Vấn đề

Rà 17 chỗ dùng `Update` / `LateUpdate` (không có `FixedUpdate`). Vài chỗ tốn mỗi frame dù không có việc: mỗi khối mèo một `Update`, chữ `TMPLetterPopIn` đang ẩn đẩy lại mesh lên GPU mỗi frame, `SaveRunner` / `MechanicIntro` / `BoosterTutorial` chạy suốt chỉ để `return`, `BackgroundFitter` ghi transform mỗi frame. Input dùng Input Manager cũ.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Block/CatFaceTicker.cs`: kế thừa `DontDestroyOnLoadSingleton<CatFaceTicker>`, tự tạo bằng `Ensure()` khi mặt mèo đầu tiên chạy; 1 `Update` duy nhất chạy khung hình cho mọi `CatFaceAnimator`.
- Package `com.unity.inputsystem` 1.14.2.

**Sửa**

- `CatFaceAnimator`: bỏ `Update`; đăng ký vào danh sách tĩnh ở `Play` / `OnEnable`, gỡ O(1) ở `OnDisable`; khung = `(Time.time + phase) * fps`.
- `TMPLetterPopIn`: bỏ nhánh ẩn trong `LateUpdate`; nghe `TMPro_EventManager.TEXT_CHANGED_EVENT`, chỉ ẩn lại khi TMP dựng lại lưới chữ; `HideAll()` chỉ đổi alpha màu (an toàn khi số chữ đổi).
- `SaveRunner`: bỏ `Update`; `Ensure()` lần đầu bẩn thì chạy coroutine `WaitForSecondsRealtime(0.5)`; `Flush()` dừng coroutine cũ. Bỏ `HideFlags.DontSave` (cả `CatFaceTicker`): Editor không hủy object `DontSave` khi thoát Play nên object rò sang Edit mode (đã gặp 1 `[CatFaceTicker]` sót, đã xóa).
- `MechanicIntro`, `BoosterTutorial`: bỏ `Update` + `_armed` / `_readyTime`; `OnLevelLoaded` chạy coroutine `WaitToStart()` tới khi đủ điều kiện rồi dừng.
- `BoosterTutorial`: bỏ `LateUpdate` chờ `_demoStart`; dùng `DOVirtual.DelayedCall(0.2f, StartDemo, false)` (thời gian có scale như cũ), `StopDemo` hủy tween.
- `BackgroundFitter`: chỉ tính và ghi `localScale` / `position` khi `orthographicSize`, `aspect`, vị trí camera hoặc sprite đổi.
- `InputManager`: đọc `Pointer.current` (chuột / ngón chạm đầu) của Input System; kiểm tra bấm trúng UI bằng `EventSystem.RaycastAll` chỉ tính `GraphicRaycaster` (ổn định hơn `IsPointerOverGameObject` ở frame vừa chạm).
- `SampleScene` / EventSystem: `StandaloneInputModule` → `InputSystemUIInputModule`.
- Player Settings: Active Input Handling = Input System Package (New) (cần mở lại Unity).

**Giữ nguyên (đã ổn)**: `EnergyManager` (1 s), `DailyRewardManager` (5 s), `DailyRewardPopup` (0.25 s), `SafeAreaZone`, `PlayZone`, `BackgroundCover`, `TutorialHand`, `SelectionBoxView` (object tắt khi không dùng). `UIGrayscaleWhenDisabled`: chỉ 1 nút dùng, chưa đổi sang subclass `Button`.

### Tác dụng

- Số lời gọi `Update` không còn tăng theo số khối mèo trên lưới.
- Chữ đang ẩn không upload mesh mỗi frame.
- Lúc rảnh không còn `Update` của Save / tutorial; nền không đánh dấu transform thay đổi mỗi frame.
- Input dùng Input System, sẵn sàng cho touch nhiều ngón / thiết bị mới.

### Kiểm tra

- Compile không lỗi / cảnh báo mới.
- Play: vào màn, đặt 1 khối bằng code → `CatFaceTicker` được tạo, mặt mèo đổi khung (`_070` → `_072`), danh sách `Running` = 1.
- Sau khi đổi sang Singleton: `Instance` nằm trong scene `DontDestroyOnLoad`, chỉ 1 ticker, mặt mèo đổi khung (`_051` → `_085`); thoát Play còn 0 `CatFaceTicker` / `SaveRunner` (không rò).
- Raycast UI / tìm ô tại vị trí trỏ đúng (không trúng UI, trúng `CellView`).
- Chưa kiểm tra được kéo bằng chuột thật (Editor không focus nên sự kiện input giả lập không vào game) → cần thử tay: kéo trên lưới, bấm nút / popup không kéo xuyên, chơi trên Device Simulator / điện thoại.

---

## Chiều – Số trên khối mèo rõ hơn

### Mục tiêu / Vấn đề

Số trên khối đã đặt bị chìm (khối đỏ: số hồng xám). Màu đặt là đỏ sẫm `(0.47, 0.15, 0.27)` nhưng chụp màn hình ra `(182, 108, 143)`: project dùng **Linear color space**, shader `TextMeshPro/Mobile/Distance Field` không đổi màu đỉnh sang linear nên mọi màu chữ bị nhạt.

### Thay đổi (Thêm / Sửa / Xóa)

**Sửa**

- `PlacedBlockView.NumberColor`: màu số = cùng hue với màu khối, saturation `s * 1.15 + 0.1`, value `v * 0.55` (như bản gốc: khối đỏ số đỏ sẫm, khối xanh số xanh sẫm); Linear thì đổi `.linear` trước khi gán cho TMP.

### Tác dụng

Số trên khối đọc rõ, cùng tông màu khối (xanh lơ / cam / nâu / xanh lá / tím đều kiểm tra).

### Kiểm tra

- Play level 11, đặt 5 khối bằng code, chụp màn hình: số `2`, `4`, `6`, `25`, `5` hiện màu sẫm cùng tông, rõ trên thân.
- Lưu ý: chữ số trên ô lưới (Cell prefab) cũng bị nhạt cùng lý do nhưng màu đã được chỉnh theo mắt nên giữ nguyên.

---

## Chiều – Ổ khóa giống bản gốc

### Mục tiêu / Vấn đề

Ổ khóa trên ô số bị "béo" (thân rộng ~72% bước ô, bản gốc ~50%; quai nhỏ so với thân) và số trên khóa bị mờ (cùng lỗi Linear color space với số trên khối).

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Util/TextColor.cs`: `TextColor.Display(color)` đổi màu sang linear khi project dùng Linear, để chữ TMP ra đúng màu chọn.

**Sửa**

- `Cell.prefab` / Lock (đo theo ảnh gốc, bước ô 1.2): Body scale 0.82 → 0.6, y −0.1 → −0.12; Shackle scale 0.77 → 0.85, y 0.2 → 0.19; LockText fontSize 4.5 → 3, y −0.12 → −0.15, màu (0.36, 0.32, 0.7) → (0.29, 0.29, 0.455) như bản gốc.
- `CellView.Awake`: màu `lockText` qua `TextColor.Display`.
- `PlacedBlockView.NumberColor`: dùng `TextColor.Display` thay code đổi linear riêng.

### Tác dụng

Ổ khóa cân đối như bản gốc, số trên khóa sẫm rõ.

### Kiểm tra

- Play level 12 (có ô khóa thật) + tạo tạm ô khóa 2 bước, chụp màn hình đặt cạnh ảnh gốc: tỉ lệ thân / quai / số và màu số khớp. Đã xóa object thử.

---

## 00:30 (09/10) – Rà SOLID: EventBus, InputGate, hàng đợi intro, Booster Strategy

### Mục tiêu / Vấn đề

Rà dự án theo SOLID (không quá chặt, ưu tiên dễ mở rộng, tránh god class). Không có god class ở runtime, nhưng:

- **Phụ thuộc chéo bằng cờ tĩnh:** `GridManager.IsInputLocked` phải biết `BoosterManager`, `DifficultyIntro` (UI), `BoosterTutorial`; `MechanicIntro` / `BoosterTutorial` kiểm tra cờ `Active` của nhau; core (`GridManager`, `LevelManager`) hỏi `TutorialController.Active`.
- **Booster không mở rộng được (OCP):** `switch (type)` trong `BoosterManager.Press`, `IsActive` theo từng loại, `ShopService` hard-code 3 booster, nửa `BoosterTutorial` là demo riêng của Khiên; `BoosterManager` ↔ `BoosterTutorial` gọi vòng nhau.
- **Lạm dụng singleton:** `LevelManager.Instance` 119 chỗ, `GridManager.Instance` 57 chỗ. `Singleton` hủy cả GameObject khi trùng, mà mọi manager nằm chung 1 object `Manager` → trùng 1 cái là mất cả cụm.
- **Class ôm việc (SRP):** `LevelManager` căn camera; `GameFlowUI` lặp danh sách 9 popup ở 2 chỗ để đóng khi đổi level; `LevelManager.HasFlowUI` gọi `FindFirstObjectByType` mỗi lần thắng / thua.

Quyết định: tự viết, không thêm package ngoài (R3, Reflex) để dễ giải thích và không thêm rủi ro cài đặt.

### Thay đổi (Thêm / Sửa / Xóa)

**Thêm**

- `Gameplay/Events/EventBus.cs`: `EventBus<T>` (Observer / Pub-Sub) cho `T : struct, IGameEvent`; Raise trên bản chụp (an toàn khi nghe / gỡ trong lúc phát, kể cả phát lồng), 1 listener lỗi chỉ log; `EventBusRegistry` xóa kênh khi vào Play.
- `Gameplay/Events/GameEvents.cs`: `LevelLoadedEvent`, `GameStateChangedEvent`, `ScreenChangedEvent`, `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent`.
- `Gameplay/Input/InputGate.cs`: khóa kéo thả theo chủ khóa (`Lock` / `Unlock` / `IsLocked` / `Describe`), tự bỏ chủ khóa Unity đã bị hủy.
- `Gameplay/Intro/ILevelIntro.cs` + `LevelIntroSequencer.cs`: hàng đợi intro sau khi nạp level, tự tìm mọi `ILevelIntro` trong scene, chạy theo `Order`, khóa lưới trong lúc chạy, `onDone` mở khóa ngay trong frame.
- `Gameplay/Booster/Effects/`: `BoosterEffect` + `IBoosterHost` (Strategy), `HintBooster`, `ShieldBooster`, `RevealBooster`, `BoosterEffectFactory` (Factory).
- `Gameplay/Booster/ShieldTutorialDemo.cs`: bước "kéo sai thử" của Khiên tách khỏi `BoosterTutorial`, nghe `BoosterTutorialCompletedEvent`.
- `Gameplay/Visuals/BoardCameraFitter.cs`: căn camera theo lưới (tách từ `LevelManager`), nghe `GridSpawner.gridCellEvent`.
- Scene: thêm `LevelIntroSequencer`, `ShieldTutorialDemo` (view = `BoosterTutorial`), `BoardCameraFitter` (chép `playZone`, `Main Camera`, tỉ lệ 0.6 / 0.86 / 0.02 từ `LevelManager`) vào object `Manager`.

**Sửa**

- `Singleton<T>`: bản trùng chỉ `Destroy(this)` + cảnh báo (không hủy GameObject).
- `LevelManager`: bỏ `onLevelLoaded` / `onStateChanged` (UnityEvent, scene không nối gì) → phát qua EventBus; `SetState` khóa / mở `InputGate`; `IsPractice` (thay `TutorialController.Active`), đặt cả `LivesManager.IsPractice`; `AutoAdvance` thay `HasFlowUI`; bỏ phần camera.
- `GridManager.IsInputLocked`: chỉ còn trạng thái lưới + `gridSpawner.IsIntroPlaying` + `InputGate.IsLocked`; `DeductLife` luôn gọi `LivesManager.LoseLife` (màn luyện tập tự bỏ qua).
- `LivesManager`: `IsPractice` → `LoseLife` bỏ qua.
- `BoosterManager`: viết lại, giữ phần chung, luật chuyển sang `BoosterEffect`; `TutorialTarget`, `BeginTutorial` / `EndTutorial`, event C# `TutorialPressed`; `onRevealModeChanged(bool)` → `onModeChanged(type, bool)`; thêm `onTutorialChanged`, `CancelModes()`; `PlaceInSequence` khóa lưới qua `InputGate`.
- `BoosterTutorial`, `MechanicIntro`, `DifficultyIntro`: thành `ILevelIntro` (Order 20 / 10 / 0), bỏ coroutine chờ + cờ tĩnh `Active` / `Playing`; `BoosterTutorial`, `MechanicIntro` không còn kế thừa `Singleton`.
- `PopupView`: nghe `LevelLoadedEvent`, đang mở thì tự đóng; `CloseOnLevelLoad` (Notice: false, Refill: false khi bắt buộc).
- `GameFlowUI`: bỏ `OnLevelLoaded` / `CloseAll` lặp, dùng mảng `AllPopups`; đặt `LevelManager.AutoAdvance = false`.
- `ShopProduct.BoosterAmount(type)` + `ShopService.Grant` duyệt mọi `BoosterType`.
- Chuyển sang EventBus: `BoosterButton` (+ `BoosterButton.Find`), `RevealBanner` (X → `CancelModes`), `LevelHUD`, `PlayButtonView`, `ScreenFlow` (phát `ScreenChangedEvent`, bỏ `onScreenChanged`), `EnergyManager`, `RefillEnergyPopup`, `GameAudioEvents`, `DebugCheats` (`LoseNow`, khóa / mở booster).
- Editor: `DifficultyBuilder` bỏ gán `gridSpawner`; `TutorialBuilder` thêm `ShieldTutorialDemo`, `LevelIntroSequencer`.
- Docs: `ProjectStructure.md`, `TutorialGuide.md`, `Features/01, 02, 04, 05, 09, 10, 12, HowTo_MoRong`.

**Xóa**

- Cờ tĩnh `DifficultyIntro.Playing`, `MechanicIntro.Active`, `BoosterTutorial.Active / Target / Demonstrating` (Demonstrating chuyển sang `ShieldTutorialDemo`).
- `LevelManager.FitCamera`, `HasFlowUI`; `BoosterManager.IsRevealMode`, `UseHint / UseShield / SetRevealMode`.

### Tác dụng

| Thêm mới | Trước | Sau |
| --- | --- | --- |
| 1 booster | `BoosterManager` (switch, `IsActive`), `ShopService`, `BoosterTutorial` nếu có bước riêng | 1 class `XBooster` + 1 dòng Factory (+ field Shop) |
| 1 lớp phủ chặn kéo thả | `GridManager.IsInputLocked` + các intro khác | `InputGate.Lock(this)` |
| 1 intro sau khi nạp level | Cờ `Active` + sửa điều kiện chờ ở 2–3 class | 1 component `ILevelIntro` |
| 1 popup | 3 chỗ trong `GameFlowUI` | 1 field + mảng `AllPopups` |
| 1 hệ thống nghe sự kiện ván | `LevelManager.Instance.onX.AddListener` | `EventBus<X>.Subscribe` |

### Kiểm tra

- Biên dịch Unity: 0 lỗi, 0 cảnh báo.
- Play mode (lưu tạm `save.json`, chạy xong trả lại):
  - Khóa hết booster, vào level 12: hàng đợi khóa lưới → tutorial Hint; bấm Khiên bị bỏ qua; bấm Hint → mở khóa, đặt 1 khối miễn phí, `InputGate` mở ngay.
  - Chơi lại: tutorial Khiên → bấm → khiên bật miễn phí → 0.2 s sau `ShieldTutorialDemo` chỉ cách sai; kéo sai bằng code → khiên chặn, còn 3 mạng, demo tự tắt.
  - Chơi lại: tutorial Reveal → bật / bấm lại hủy / bật lại → chạm ô số → đặt lần lượt 4 khối, `InputGate` khóa bởi `BoosterManager` trong lúc đặt rồi mở; hết lượt bấm Reveal → popup mua, ván Paused.
  - `WinNow` → state Won, WinPopup mở; nạp level khác → WinPopup tự đóng.
  - Level 28 (Hard + "?"): băng HARD chạy trước, xong mới tới popup cơ chế (ván Paused), Ok → lưới mở khóa.
  - `LoseNow` → popup Thua; `PlayTutorial` → `IsPractice`, `LoseLife` không trừ mạng; Skip → `IsPractice` tắt, nạp lại level, popup Thua tự đóng.
  - Console không có lỗi.
