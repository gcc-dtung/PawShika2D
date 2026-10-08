# Cấu trúc dự án

## Thư mục

```text

Assets/
├── Docs/                               Tài liệu (README, TaskBreakdown, file này; Features/ = docs theo tính năng + HowTo mở rộng)
├── Scenes/
│   └── SampleScene.unity               Scene chơi duy nhất
├── Settings/                           URP 2D (Renderer2D, UniversalRP)
├── TextMesh Pro/                       Font LiberationSans SDF đang dùng
└── _Project/
    ├── Script/                         Code game, chia theo Gameplay / UI / Editor (chi tiết bên dưới)
    ├── Prefabs/
    │   ├── Cell.prefab                 Ô lưới: Visual (CellBG), Text (TMP), Arrow, Lock (Shackle, Body, LockText), BoxCollider2D
    │   └── Gameplay/                   Prefab dùng với ObjectPool (không tạo GameObject lúc chơi)
    │       ├── PlacedBlock.prefab      Thân, Number, Face, Ears, Ring (viền đủ value, tắt sẵn)
    │       ├── Praise.prefab           Chữ khen
    │       ├── MistakeFlash.prefab     Vùng chớp đỏ khi sai (WhiteBlock 9-slice)
    │       └── DragCountBubble.prefab  Bong bóng số ô (1 instance đặt sẵn trong scene)
    ├── Levels/
    │   ├── LevelDatabase.asset         Danh sách level theo thứ tự chơi (Reorder Levels: 1 Custom 4x4 + 201 ShikaCat / Mechanic + Test)
    │   ├── MechanicInjectorSettings.asset  Lịch gắn cơ chế (Mechanic Injector)
    │   ├── LevelDifficultySettings.asset   Trọng số công thức độ khó (Level Difficulty)
    │   ├── ShikaCat/Level_001..205     LevelDefinition sinh từ JSON ShikaCat (bản gốc, không cơ chế)
    │   ├── Custom/                     Level tự làm (Level Editor); Level_Custom_Level01_4x4 = Level 1 (thay ShikaCat/Level_001)
    │   └── Mechanic/Level_NNN          Bản có cơ chế do Mechanic Injector sinh, thay bản gốc trong database
    ├── Configs/                        Config dự án đang dùng (sửa số liệu ở đây): EconomyConfig, LivesEnergyConfig, BoosterDatabase,
    │                                   ShopDatabase, DailyRewardDatabase, AudioDatabase, DifficultyTheme (chuyển từ 10_Data_Configs, bản cũ giữ lại làm lưu trữ)
    ├── Resources/
    │   ├── GameConfig.asset            Config gốc: trỏ tới mọi asset trong Configs/ + GameplayVisuals (Setup Game Config)
    │   └── GameplayVisuals.asset       Sprite khối, tai, mặt mèo, ổ khóa, chữ khen...
    ├── SO_LevelDefinition.asset        Level mẫu 4x4, chỉ dùng khi scene không có LevelManager
    ├── GameAssets/                     Asset trích từ Paw Block (xem README.md trong đó)
    │   ├── 01_Audio  02_Cat_Characters_Spine  03_Gameplay_Blocks_Grid
    │   ├── 04_UI_Graphics  05_VFX_Particles  06_Fonts  07_Animations
    │   └── 08_Prefabs  09_Shaders_Materials  10_Data_Configs  11_Original_Atlases_Textures
    ├── ShikaCat_GameAssets/            Asset trích từ ShikaCat, gồm Level_Data/shikacat-levels.json
    └── Texture2D/                      Texture rời trích từ ShikaCat (panel, icon, nút, mèo...)
```

---

## Script

```text
Script/
├── Gameplay/
│   ├── Core/       GridManager, GridSpawner, GridGeometry, BoardModel, CellModel, CellView, ClueCell
│   ├── Config/     GameConfig (Resources), EconomyConfig, LivesEnergyConfig
│   ├── Level/      LevelManager, GameState, LevelDefinition, LevelDatabase
│   ├── Lives/      LivesManager
│   ├── Economy/    CoinWallet, RewardedAds, IAPService, ShopDatabase, ShopService
│   ├── Booster/    BoosterDatabase (+ BoosterType, BoosterData), BoosterInventory, BoosterManager, BoosterTutorial
│   ├── Cheats/     DebugCheats
│   ├── Daily/      DailyRewardDatabase, DailyRewardModel, DailyRewardManager
│   ├── Energy/     EnergyModel, EnergyManager
│   ├── Block/      BlockModel, PlacedBlockView, CatFaceAnimator, ColorType
│   ├── Input/      InputManager, DragAndDropController, DragSelection, SelectionBoxView, DragCountBubble
│   ├── Solver/     ShikakuSolver
│   ├── Tutorial/   TutorialController, TutorialLevels, TutorialHand, MechanicIntro
│   ├── Feedback/   GameplayFeedback
│   ├── Audio/      SoundId, AudioDatabase, AudioManager, GameAudioEvents
│   ├── Save/       SaveData, SaveSystem, SaveRunner
│   ├── Visuals/    GameplayVisuals, BackgroundFitter, PlayZone
│   └── Util/       Singleton
├── UI/
│   ├── Common/     SafeAreaZone, BackgroundCover, UIButtonSounds, NoClickSound, UIButtonPress, ButtonPress, SettingsButton, UIConfetti, TMPLetterPopIn, ButtonPressIgnore, NestedScrollRect
│   ├── HUD/        LevelHUD, LivesUI, BoosterButton, RevealBanner, ShieldIndicator, EnergyHUD, TutorialView, BoosterTutorialView
│   ├── Menu/       ScreenFlow, MainMenuNavigator, MenuSwipe, MenuTabButton, PlayButtonView, CoinHUD, MenuShortcutButton, DailyRewardButton, ShopCardView
│   └── Popups/     PopupView, GameFlowUI, WinPopup, ReviveLivesPopup, LosePopup, SettingsPopup, QuitLevelPopup, NoticePopup, SettingToggleView, BuyBoosterPopup, RefillEnergyPopup, DailyRewardPopup, DailyRewardSlot
└── Editor/         Chỉ chạy trong Editor (tên thư mục "Editor" là bắt buộc)
    ├── Config/     GameConfigSetup (Setup Game Config: tạo Configs/ + Resources/GameConfig)
    ├── Level/      ShikaCatLevelImporter, LevelEditorWindow, LevelValidator, MechanicInjector*, LevelDifficulty*
    ├── UI/         PopupBuilder, ShopBuilder, SettingsBuilder, DifficultyBuilder, DailyRewardBuilder, WinScreenBuilder, BoosterBuilder, MainMenuBuilder, TutorialBuilder
    └── Visuals/    GameplayVisualsSetup
```

Quy tắc đặt file mới:
- Code chạy trong màn chơi, không phải Canvas → `Gameplay/<tính năng>/`. Tính năng mới (Booster, Audio...) tạo thư mục riêng cùng cấp.
- Code điều khiển Canvas / popup → `UI/<nhóm>/`, ví dụ `UI/Popups/` cho WinUI, LoseUI, ReviveLivesPopup ở Giai đoạn 3.
- Công cụ Editor → `Editor/<tính năng>/`.
- Dự án chưa dùng `.asmdef`, mọi script nằm chung `Assembly-CSharp`; di chuyển file không cần sửa `using`.
- **Animation dùng DOTween, không dùng coroutine.** Mỗi tween được giữ trong field (`Tween` / `Sequence`), gắn `SetLink(gameObject)`, và bị `Kill()` trong `OnDisable` (kèm khi phát lại / trả về pool). Callback `OnKill` chỉ xóa field nếu field vẫn trỏ đúng tween đó (Kill gọi trong callback có thể bị DOTween hoãn). Cái gì cần trả về trạng thái gốc khi bị hủy (camera, tim, chữ khen về pool) thì làm trong `OnKill`. Popup dùng `SetUpdate(true)` để chạy theo thời gian thực.
- **Tách logic và view:** dữ liệu và luật là class C# thuần tên `…Model` (không biết gì về SpriteRenderer / transform); phần hiển thị là MonoBehaviour tên `…View` (chỉ đọc model để vẽ, không tự quyết định luật). `GridManager` nối hai bên. Polish chỉ cần sửa file `…View`.

### Gameplay/Core

| File | Vai trò |
| --- | --- |
| `GridSpawner` | Sinh `CellView` + `CellModel` từ `LevelDefinition`, bỏ ô Void, phát `gridCellEvent`; hiệu ứng xuất hiện (ô nảy lần lượt thứ tự ngẫu nhiên, rồi số hiện dần; `IsIntroPlaying` khóa thao tác) |
| `GridManager` | Điều phối: nhận vùng kéo → hỏi `BoardModel` → điều khiển `SelectionBoxView` / `PlacedBlockView`, trừ mạng, phát event. Vùng đủ điều kiện: chờ khung giãn hết vào vùng mới hiện mèo; thả tay khi khung chưa giãn xong thì khóa thao tác, chờ giãn xong rồi mới đặt / báo sai |
| `GridGeometry` | Struct C# thuần: tọa độ ô / vùng → world; `WorldToCell` (kẹp trong lưới) |
| `BoardModel` | **Logic** C# thuần: các ô, các khối đã đặt, `Check(rect)` → `PlacementResult`, gọi solver, đặt khối, giảm khóa, kiểm tra kín lưới |
| `CellModel` | **Logic** C# thuần: vị trí, `ClueCell`, khối đang chiếm; event `Changed` khi ô số đổi |
| `CellView` | **View**: bật/tắt object con có sẵn trong prefab (số, ổ khóa, ô nền điều hướng); là đích raycast khi kéo; `PlayIntro` / `StopIntro` (nảy + hiện dần, DOTween) |
| `ClueCell` | **Logic** C# thuần: giá trị, số bước khóa, ẩn/hiện, hướng |

### Gameplay/Level, Lives, Solver

| File | Vai trò |
| --- | --- |
| `LevelManager` | Nạp level từ `LevelDatabase`, lưu tiến độ, chuyển level, zoom camera; giữ `GameState` (`Pause`/`Resume`, `Revive`, `GiveUp`), phát `onStateChanged` sau `stateChangeDelay` 0.8 s. Scene không có `GameFlowUI` thì tự chuyển như cũ |
| `GameState` | Enum: Playing, Paused, Won, OutOfLives, Lost. Chỉ Playing nhận thao tác kéo |
| `LevelDefinition` | ScriptableObject level: kích thước, ô Void, clue, lời giải, luật |
| `LevelDatabase` | ScriptableObject chứa danh sách level |
| `LivesManager` | 3 mạng, trừ / hồi mạng, báo hết mạng; khiên (`ActivateShield`, chặn 1 lần `LoseLife`, mất khi `ResetLives`) |
| `ShikakuSolver` | Backtracking (MRV). `IsSolvable` để phát hiện nước sai; `TrySolve` cho Hint/Reveal |

### Gameplay/Economy

| File | Vai trò |
| --- | --- |
| `CoinWallet` | Static, coin lưu `SaveData.coins`: `Add`, `TrySpend`, event `Changed` |
| `RewardedAds` | Quảng cáo có thưởng, đặt trên `Manager`. Hiện giả lập: chờ 0.5 s rồi trả thành công. Thay thân `ShowRoutine` khi gắn SDK |
| `IAPService` | Mua bằng tiền thật (VND). Chưa có SDK: `Purchase(productId, onDone)` luôn thất bại, phát `PurchaseFailed` (popup "Purchase Failed!" như bản gốc). Tích hợp Google Play / App Store: thay thân `Purchase`, nơi gọi giữ nguyên |
| `ShopDatabase` | ScriptableObject `Configs/ShopDatabase.asset` + `ShopProduct` (mã IAP, tên, mục `ShopSection`, giá VND, hình, coin / Hint / Shield / Reveal / phút năng lượng vô hạn / removeAds, tem giảm giá). Số chép từ `*PackShop.asset` gốc (freeze = Shield), giá theo video |
| `ShopService` | `Buy(product)` → `IAPService.Purchase`; thành công thì `Grant` (coin, booster, năng lượng vô hạn, `SaveData.noAds`), phát `Purchased` |

### Gameplay/Booster

| File | Vai trò |
| --- | --- |
| `BoosterDatabase` | ScriptableObject + `BoosterType` (Hint 0, Shield 1, Reveal 2) + `BoosterData`. Tên field giữ như bản gốc; asset ở `Configs/BoosterDatabase.asset` |
| `BoosterInventory` | Static, số lượt lưu `SaveData.boosters` (theo tên loại); lần đầu tặng `valueStart`; event `Changed` |
| `BoosterManager` | Trên `Manager`. `IsUnlocked`: đã lưu trong `SaveData.unlockedBoosters` (qua tutorial mở khóa) hoặc đang là đích của tutorial. `Press(type)`: đang tutorial mở khóa → chỉ nhận đúng nút, gọi `BoosterTutorial.Complete`, lần dùng này miễn phí (`_freeUse`; Reveal miễn phí tới khi chọn số); chưa mở → bỏ qua; hết lượt → `onBuyRequested`; Hint đặt khối nhỏ nhất của lời giải; Khiên bật `LivesManager`; Reveal vào chế độ chọn số (`HandleCellTap` từ `DragAndDropController`), đặt lần lượt mọi khối cùng số (DOTween, cách 0.12 s, `IsBusy` khóa lưới). Chỉ trừ lượt khi có tác dụng. `Buy(type)` trừ coin |
| `BoosterTutorial` | Trên `Manager`. Sau mỗi lần nạp level, chờ đang chơi thật (không Menu / popup / tutorial lần đầu / lưới đang hiện / băng HARD) thêm 0.3 s → booster chưa mở có `levelUnlock` nhỏ nhất mà level hiện tại đã tới: `Active`, `Target`, `BoosterTutorialView.Show` (lời nhắc = `BoosterData.detail`). Mỗi lần nạp level tối đa 1 tutorial. `Active` khóa lưới (`GridManager.IsInputLocked`). `Complete` (bấm đúng nút): lưu `unlockedBoosters`, đóng lớp tối. Khiên: bước 2 "kéo sai thử" (`Demonstrating`): nền tối tắt, `TutorialHand` chỉ 1 vùng đúng số ô nhưng sai lời giải, chờ `onShieldBlocked` rồi khen 2 s |

### Gameplay/Cheats

| File | Vai trò |
| --- | --- |
| `DebugCheats` | Lệnh cheat dùng chung (static), chỉ gọi API sẵn có nên hiệu ứng / popup / save chạy như chơi thật: level (`GoToLevel`, `PlayLevel` – vào ván bỏ qua năng lượng, Next / Prev / Retry), thắng (`WinNow` – đặt hết lời giải, `SolveOneBlock`), thua (`LoseLife`, `OutOfLives` → popup Hồi mạng, `LoseNow` – chờ sự kiện OutOfLives rồi `GiveUp` → popup Thua), coin, mạng / khiên, năng lượng, booster (`BoosterInventory.SetCount`), điểm danh, lưu save, tutorial (`PlayTutorial` – chơi lại từ bước 1, `ResetTutorial` – bỏ cờ đã xem, `ResetMechanicIntros` – xem lại popup cơ chế), booster (`UnlockAllBoosters`, `LockAllBoosters` – xem lại tutorial mở khóa) |

### Gameplay/Tutorial

Cách sửa bước / tạo tutorial mới: [TutorialGuide.md](TutorialGuide.md).

| File | Vai trò |
| --- | --- |
| `TutorialController` | Trên `Manager`. `Pending` (tĩnh): save chưa có `tutorialDone` và đang ở level 1 → `ScreenFlow` vào thẳng tutorial khi mở game, `LevelManager` không nạp level. 4 bước nạp bằng `LevelManager.LoadTutorial`; thắng bước (`GridManager.onGameWin`) → chờ 1.1 s sang bước kế; đặt khối → bàn tay chuyển sang số kế. Xong / Skip: lưu `tutorialDone`, `LoadLevel` level hiện tại. `Active` (tĩnh): `GridManager` không trừ mạng, `LevelManager` không sang WinPopup |
| `TutorialLevels` | 4 bước như bản gốc, dựng `LevelDefinition` trong code: 3 ô ngang, 3 ô dọc, 3x3 (3 + 6), 3x3 (2 + 4 + 3); lời nhắc tiếng Anh; gợi ý bàn tay (số, ô bắt đầu, ô kết thúc) |
| `MechanicIntro` | Trên `Manager`. Sau mỗi lần nạp level (lưới hiện xong, băng HARD xong, không tutorial khác) + 0.15 s: level có cơ chế chưa xem (Hướng → Khóa → "?") thì mở `MechanicIntroPopup` lần lượt; Ok lưu `SaveData.seenMechanics`. Popup tạm dừng ván nên `BoosterTutorial` chờ popup xong. Void không có popup |
| `TutorialHand` | Object `TutorialHand` (world, sprite `tutorial_hand`, order 80): hiện → nhấn → trượt tới ô cuối → nhấc → mờ, lặp; ẩn khi người chơi đang kéo (`GridManager.IsSelecting`) |

### Gameplay/Daily

| File | Vai trò |
| --- | --- |
| `DailyRewardDatabase` | ScriptableObject `Configs/DailyRewardDatabase.asset`: quà 7 ngày (mỗi ngày nhiều món: Coin / Booster / InfiniteEnergy – phút), icon coin / năng lượng vô hạn, `recoverMissedWithAd` |
| `DailyRewardModel` | **Logic** C# thuần: chu kỳ 7 ngày theo lịch máy từ `cycleStart`; ô i = ngày `cycleStart + i`; trạng thái Claimed / Today / Missed / Future; quá 7 ngày thì chu kỳ mới; đồng hồ lùi thì coi như ngày đầu. Lưu `SaveData.daily` (`cycleStart`, `claimedMask`) |
| `DailyRewardManager` | Trên `Manager`. `Claim(day)`: hôm nay nhận luôn, ngày lỡ xem quảng cáo (không chờ nếu đang có quảng cáo khác); trao quà (CoinWallet / BoosterInventory / EnergyManager.AddInfinite); kiểm tra sang ngày mới mỗi 5 s và khi quay lại app; `autoOpen`: vào game lần đầu trong ngày còn quà thì mở popup. `onChanged`, `onClaimed`. Context menu Debug: Next Day / Reset |

### Gameplay/Energy

| File | Vai trò |
| --- | --- |
| `EnergyModel` | **Logic** C# thuần (nhận `now` từ ngoài): năng lượng, mốc bắt đầu đếm hồi, hạn vô hạn; `Refresh` cộng bù theo thời gian đã trôi, `TryConsume`, `Add`, `AddInfinite`, `SecondsToNext` |
| `EnergyManager` | Trên `Manager`. Tối đa 5, hồi 1/1800 s; nghe `GameState.Lost` → −1 (`ConsumeOne`, dùng cả khi bỏ dở level); lưu `SaveData.energy`; tick mỗi giây (`onTick`) cho đồng hồ, `onEnergyChanged`. Context menu Debug: Set 0 / Set Full / Skip 1 refill / Infinite 5 min |

### Gameplay/Block

| File | Vai trò |
| --- | --- |
| `BlockModel` | **Logic** C# thuần: vùng, các ô chiếm, ô số, màu (= màu khung chọn của lượt kéo tạo ra khối) |
| `DifficultyTheme` | ScriptableObject `Configs/DifficultyTheme.asset` + `LevelDifficulty` (Normal / Hard / VeryHard) + `DifficultyStyle`: chữ tab, mặt nút PLAY + màu chữ + viền chữ PLAY, khung HUD + viền chữ tab, đầu lâu, băng chữ; dùng chung khung PLAY `khung_home_hard`, dải sọc. `LevelDefinition.difficulty` (sửa trong Level Editor) |
| `PlacedBlockView` | **View** khối mèo: thân 9-slice, tai, số, mặt, viền `Ring`. Trạng thái: xem trước (đặc ngay, viền màu mèo thu bên trong 0.2 s) → `Confirm` (bật) hoặc `PlayReject` (rung, co nhỏ + mờ; `rejectStyle`: `GrayOut` mèo xám – mặc định / `RedRing` viền đỏ ngoài mép / `GrayAndRedRing`). `PlayWinFlash`: thắng thì thân + tai phủ trắng 0.6 rồi tan (lớp sprite đè lên dùng `flashMaterial` = `BlockFlash.mat`, GUI/Text Shader) |
| `CatFaceAnimator` | Chạy chuỗi khung hình mặt mèo trên SpriteRenderer |
| `ColorType` | Enum 10 màu + bảng màu RGB |

### Gameplay/Input, Feedback

| File | Vai trò |
| --- | --- |
| `InputManager` | Đọc chuột/chạm, chuyển cho `DragAndDropController` |
| `DragAndDropController` | Raycast tìm ô lúc bắt đầu kéo; khi kéo đổi vị trí ngón tay sang world rồi chuyển cho `GridManager`; cập nhật bong bóng |
| `DragSelection` | **Logic** C# thuần của vùng đang kéo: không bao giờ phủ lên khối đã đặt / ô Void (bị chặn thì giữ vùng hợp lệ trước đó), vuốt nhanh thì đi qua từng ô ở giữa; `BlockDir` = mép nào đang bị ngón tay đẩy vào vật cản |
| `SelectionBoxView` | Khung màu mờ phủ vùng đang kéo, random 1 `ColorType` mỗi lượt kéo (mèo xem trước và khối đặt ra dùng đúng màu này); đổi ô thì giãn / co tới vùng mới trong `resizeDuration` 0.15 s (`resizeEase` OutQuad, tween DOTween, chỉnh trong Inspector), giãn xong báo `WhenSettled`; mờ đi khi đang hiện mèo xem trước. Bình thường ôm sát ô (`inset` 0.1); bị chặn (khối, Void, mép lưới) thì mép phía đó trượt sang lấp khe, dừng ngay trước vật cản (`blockedExtend` 0.1) |
| `DragCountBubble` | Bong bóng số ô bám ngón tay |
| `GameplayFeedback` | Chữ khen khi đúng; rung camera, rung máy khi sai (qua `AudioManager.Vibrate`, theo cài đặt Rung) |

### Gameplay/Audio

| File | Vai trò |
| --- | --- |
| `SoundId` | Enum các âm: UI (Click, PopupOpen/Close, Coin), màn chơi (LevelStart, BlockPlace, Mistake, Unlock, Win, Lose, OutOfLives), booster (Booster, ShieldOn, ShieldBlock), tiếng mèo (CatHappy, CatSad) |
| `AudioDatabase` | ScriptableObject `Configs/AudioDatabase.asset`: mỗi âm → nhiều clip (phát ngẫu nhiên), âm lượng, khoảng cao độ, cooldown; nhạc nền + âm lượng |
| `AudioManager` | Trên object `AudioManager`. Nhạc nền lặp, đổi / bật / tắt có fade (DOTween, thời gian thực); pool `sfxVoices` AudioSource 2D, hết thì dùng lại nguồn phát lâu nhất; cooldown từng âm. `Play(id)`, `PlayChance(id, p)`, `Vibrate()`; `MusicOn` / `SoundOn` / `VibrateOn` lưu `SaveData.settings`, sự kiện `SettingsChanged` |
| `GameAudioEvents` | Nối âm vào sự kiện có sẵn: `GridManager` (đặt khối + mèo vui 35 %, sai + mèo buồn 80 %, mở khóa), `LevelManager.onStateChanged` (Won / OutOfLives / Lost), `GridSpawner.gridCellEvent` (LevelStart, bỏ qua khi ở Menu), `LivesManager` (khiên bật / chặn), `BoosterManager.onBoosterUsed`, `CoinWallet.Changed` (nhận coin) |

### Gameplay/Save

| File | Vai trò |
| --- | --- |
| `SaveData` | Toàn bộ dữ liệu người chơi (JsonUtility): `version`, `savedAt`, `currentLevel`, `coins`, `boosters` (tên loại + số lượt), `energy` (số, mốc hồi, hạn vô hạn), `settings` (nhạc, âm, rung). Field mới đặt mặc định tại khai báo |
| `SaveSystem` | Static. `Data` nạp lười từ `persistentDataPath/save.json`; `MarkDirty()` (Play: SaveRunner ghi sau 0.5 s; Editor: ghi ngay), `Save()`, `Delete()`. Ghi an toàn: `.tmp` → giữ bản cũ `.bak` → đổi tên; đọc lần lượt file chính → `.tmp` → `.bak`. Không có file: chuyển PlayerPrefs bản cũ rồi xóa key. `Upgrade` theo `version`. Reset static khi vào Play (tắt Domain Reload vẫn đúng) |
| `SaveRunner` | Object ẩn, DontDestroyOnLoad, tạo khi cần: gom thay đổi ghi sau 0.5 s, luôn ghi khi app vào nền / mất focus / thoát |

### Gameplay/Visuals, Util

| File | Vai trò |
| --- | --- |
| `GameplayVisuals` | ScriptableObject gom sprite; nạp qua `Resources.Load("GameplayVisuals")` |
| `BackgroundFitter` | Nền world (`bg`) đi theo camera; scale trong scene là tối thiểu: đã phủ kín camera thì giữ, chưa kín thì phóng vừa đủ |
| `Singleton` | Base class `Singleton<T>` và `DontDestroyOnLoadSingleton<T>` |

### UI/HUD

| File | Vai trò |
| --- | --- |
| `LevelHUD` | Chữ `LEVEL n`; level Hard / Very Hard: khung có tab ghi mức khó (`khung_hard1` / `khung_expert1`, chữ tab viền theo mức khó) bao ô LEVEL, nền ô đổi sang `rounded_white` bo góc |
| `DifficultyIntro` | Trên `Canvas_HUD/DifficultyIntro`. Vào ván level Hard / Very Hard: chờ lưới hiện xong → nền tối, dải sọc `khung_expert_4` + băng chữ (`khung_hard_3` HARD LEVEL / `khung_expert_3` EXTREME LEVEL) trượt chéo từ góc trên trái vào, mèo đầu lâu (`khung_hard_2` / `khung_expert_2`) bật ra + nhịp, giữ 1.3 s, đầu lâu thu lại, băng trượt ra, nền sáng lại; âm `HardLevel`. `Playing` (tĩnh) khóa thao tác trên lưới |
| `LivesUI` | Hàng tim trên Canvas |
| `BoosterButton` | 1 nút booster: icon, số lượt (`btn_number`), dấu `+` (`btn_add_0`), ổ khóa "LV n", quầng sáng xoay khi đang bật; bấm → `BoosterManager.Press` |
| `RevealBanner` | "TAP A NUMBER TO REVEAL" + X (`khung_reveal2`, `revel_X`), đè lên thanh booster khi đang chọn số |
| `ShieldIndicator` | Khung băng `ice_effect_2` phủ hàng tim khi có khiên; khiên chặn lỗi thì phồng to rồi mờ |
| `TutorialView` | `Canvas_HUD/UIZone/TutorialPanel` (tắt sẵn): tiêu đề Tutorial, nút Skip, vạch tiến độ `tutorial_dot` (bước hiện tại sáng), khung lời nhắc `popup_tutorial` 9-slice (nảy nhẹ khi đổi bước). Hiện thì ẩn `TopPanel`, `DownPanel`, ẩn thì trả lại |
| `BoosterTutorialView` | `Canvas_HUD/BoosterTutorial` (Canvas riêng sortingOrder 5, tắt sẵn): nền tối chặn thao tác, khung lời nhắc `khung_reveal2` đè khung LEVEL, bàn tay nhấn lặp lên nút. Nút booster được nhấc trên nền tối bằng Canvas override sorting (+ GraphicRaycaster, thêm lúc chạy) và bật quầng sáng (`BoosterButton.SetHighlight`) |
| `EnergyHUD` | Icon tim (`icon_hear_2`), số năng lượng, đồng hồ / FULL / ∞; có `infiniteSprite` (ô ở Home) thì khi vô hạn đổi sang tim ∞ (`icon_hear_infinity2`) và ẩn số; bấm khi chưa đầy → `GameFlowUI.OpenEnergyRefill` |

### UI/Menu

| File | Vai trò |
| --- | --- |
| `MainMenuNavigator` | Trên `Canvas_Menu`. Panel xếp ngang theo thứ tự tab, đổi tab thì trượt (`duration` 0.3 s, `ease` OutCubic); tab chọn rộng `selectedWidth` 408, nền `home_tab1` trượt theo, tab khác chia đều; xong thì tắt panel khuất màn hình. Tab / nền tab chọn neo bottom center. `Select(index)`, `onTabChanged`; vuốt: `BeginSwipe` / `Swipe(dx)` / `EndSwipe(v)` (panel theo ngón tay, đang ở tab đầu / cuối thì không kéo ra ngoài được; thả: quá `swipeThreshold` 20% bề rộng hoặc nhanh hơn `swipeVelocity` 900/s thì sang tab kề, thời gian trượt theo quãng còn lại, tối thiểu `minReleaseRatio`); `LayoutInEditor` (chuột phải component) xếp sẵn trong Editor |
| `ShopCardView` | 1 thẻ / dòng / ô Shop: đọc `ShopProduct` lúc chạy (tên, giá, hình, coin, ô vật phẩm 2x2 cố định Hint / Shield / Reveal / vô hạn, ẩn món = 0); bấm giá → `ShopService.Buy`; dòng Remove Ads ẩn khi đã `noAds` |
| `MenuSwipe` | Trên `Canvas_Menu/Panels`: nhận kéo từ mọi thứ trong panel, kéo dọc thì bỏ qua; vận tốc làm mượt, dừng tay > 0.08 s rồi thả = vận tốc 0. Kéo quá ngưỡng thì EventSystem hủy click của nút đang ấn |
| `ScreenFlow` | Trên `Manager`. Menu: bật `Canvas_Menu`, tắt `Canvas_HUD`, `LevelManager.Pause` (cả khi level nạp lúc mở game). `Play()` → `GameFlowUI.RequestPlay` (kiểm tra năng lượng) → tắt menu, bật HUD, `LevelManager.Retry` (nạp lại level từ đầu). `ShowMenu()` (popup Thua / Cài đặt → HOME). `ShowShop()`: về Menu nếu đang trong ván rồi chọn tab `MainMenuNavigator.ShopTab` (Nạp năng lượng thiếu coin). `onScreenChanged` |
| `PlayButtonView` | Nút PLAY: "Level n" theo `onLevelLoaded`, thở nhẹ (DOTween lặp, kill khi tắt) → `ScreenFlow.Play` |
| `CoinHUD` | Số coin (nghe `CoinWallet.Changed`, icon nảy); bấm → tab Shop |
| `SafeAreaZone` | UI Zone: neo RectTransform theo `Screen.safeArea` (chọn cạnh trên / dưới / trái / phải) để UI tránh tai thỏ, thanh home; chỉ chạy khi Play, tự cập nhật khi màn hình đổi |
| `BackgroundCover` | Ảnh nền UI phủ kín RectTransform cha: kích thước trong scene là tối thiểu, chưa kín thì phóng đều vừa đủ (tính cả lệch tâm) |
| `MenuShortcutButton` | Nút phụ trên Home (Starter, No Ads): định kỳ icon (`icon`, pivot ở đáy) nhấc lên `lift`, to `scaleUp`, lắc nghiêng `angle` nhỏ dần `swings` nhịp rồi hạ xuống (OutBounce), ~0.65 s / lần; nghỉ `interval`, lần đầu chờ `firstDelay` (Starter 1.4 / 0.6, No Ads 2.35 / 1.3 – nhịp ~2 s / ~3 s như bản gốc). Khung tròn / ruy băng / chữ đứng yên. Bấm → chuyển tab (mặc định Shop) |
| `MenuTabButton` | 1 tab: icon to 1.3x + nhô lên, hiện chữ khi được chọn (nảy nhẹ OutBack `selectOvershoot` 2; bỏ chọn thu về nhanh `deselectRatio` 0.6); có `ButtonPress` tắt (đổi tab đã có hiệu ứng riêng); vạch ngăn `home_tab3` chỉ giữa 2 tab không chọn: đổi ngay khi bắt đầu trượt, vạch mới hiện dần cả quãng, vạch cũ mờ nhanh (`dividerHideRatio`) |

### UI/Popups

| File | Vai trò |
| --- | --- |
| `MechanicIntroPopup` | `Canvas_Popup/Popups/MechanicIntroPopup` như popup "Orientation" bản gốc: khung `popup_tutorial`, tiêu đề, 1 dòng giải thích, ảnh `popup_tutorial_4` (Hướng) / `_2` (Khóa) / `_3` ("?"), nút xanh Ok. Mở thì tạm dừng ván |
| `PopupView` | Base: CanvasGroup, nền tối `Dim` chắn bấm, `Panel` bật lên (scale 0.6 → 1), dùng thời gian thực; âm PopupOpen / PopupClose |
| `UIButtonSounds` | Trên Canvas_Menu / Canvas_HUD / Canvas_Popup: mọi Button bên trong (kể cả đang tắt) phát Click; nút có `NoClickSound` thì bỏ qua |
| `UIButtonPress` | Trên Canvas_Menu / Canvas_HUD / Canvas_Popup: gắn `ButtonPress` cho mọi Button bên trong (kể cả đang tắt); nút đã có `ButtonPress` thì giữ nguyên |
| `ButtonPress` | Cảm giác nhấn như bản gốc (chỉ khi nút tương tác được). Nút có khung + mặt riêng (con `Face` / `Fill`: PLAY, Refill, Free live): khung đứng yên, mọi con lún `insetScale` 0.94 quanh tâm nút và tối `pressedTint` 0.78 → lộ viền tối quanh mặt như bị nhấn xuống; Button đang tô khung thì chuyển `targetGraphic` sang mặt. Nút 1 ảnh: cả nút lún `pressScale` 0.93 và tối (tạm dừng tween scale khác trên nút). Thả: nảy nhẹ về (OutBack `releaseOvershoot` 3, 0.18 s); kéo ra ngoài / vuốt: về luôn. Thời gian thực. Tắt cho 1 nút: gắn sẵn `ButtonPress` rồi bỏ tick |
| `GameFlowUI` | Trên `Canvas_Popup/Popups`. Nghe `onStateChanged`: Won → Win, OutOfLives → Hồi mạng, Lost → Thua; `OpenSettings` cho nút bánh răng (HUD / Menu; trong ván chỉ mở khi đang chơi); `RequestPlay` cho nút PLAY; `OpenEnergyRefill` (Menu). Hết năng lượng mà PLAY / Retry → `RefillEnergyPopup.ShowRequired` (X: PLAY → ở lại Menu, Retry → mở lại popup Thua) |
| `WinPopup` | Màn thắng như bản gốc (không bảng, nền tối 0.95, không có sao): ô coin trên cùng, chữ "WELL DONE!" to (156, tự co) ngay trên huy hiệu mèo (`khung_win1` + `cat_1`) với quầng sáng + tia sáng xoay, `UIConfetti`. Mở: chữ đi vào từng ký tự (`TMPLetterPopIn`) cùng lúc huy hiệu + tia bật, confetti bắn 2 bên rồi mưa → chữ cuối đáp xuống thì Continue / x2 Coin bật. Nhận: huy hiệu thu lại, coin (≤ 12) bung ra rồi bay vào ô coin, số đếm lên → `NextLevel` |
| `TMPLetterPopIn` | Chữ TMP hiện lần lượt từng ký tự: mỗi chữ bắt đầu to `startScale` 2.4, lệch ±`startRotation` 16°, trong suốt → thu về cỡ chuẩn (OutBack `overshoot` 1.7), hiện dần (`fadeRatio`), xoay về thẳng; chữ sau trễ `letterDelay` 0.06 s, mỗi chữ `letterDuration` 0.38 s. Sửa đỉnh lưới chữ (không tách object), giữ màu / gradient / viền. `Play`, `Hide`, `Finish`, `Duration` |
| `UIConfetti` | Confetti bằng ParticleSystem vẽ lên uGUI qua **UIParticle** (package `com.coffee.ui-particle` 4.14.1): UIParticle scale 100 (1 đơn vị particle = 100 đơn vị canvas) + 3 ParticleSystem con `BurstLeft` / `BurstRight` (60 mảnh, bắn chéo lên 55–85°) / `Rain` (22 mảnh / giây từ mép trên). Dùng chung `ConfettiUI.mat` (UI/Default – UIParticle chỉ hỗ trợ shader UI) + atlas `confetti_sheet` 2x2 qua Texture Sheet Animation → 1 mesh, 1 draw call. Trọng lực, lực cản 1.6, Noise (lắc), xoay + lật trục X, mờ 20 % cuối. Vị trí phát tính theo rect lúc gọi (màn cao / thấp đều đúng mép) |
| `NestedScrollRect` | ScrollRect dọc trong vùng vuốt ngang: lượt kéo ngang nhiều hơn dọc thì chuyển lên cha (`MenuSwipe` chuyển tab) |
| `ReviveLivesPopup` | Ads +1 mạng, 900 coin +3 mạng (mờ khi thiếu coin), X → `GiveUp` |
| `SettingsPopup` | Cài đặt như ref: thân + tab "SETTING" (`popup_1_body` 9-slice), 3 `SettingToggleView` (Music / Sound / Vibrate), X, Home (trong ván → `QuitLevelPopup`; ở Menu ẩn), "Ver" + `Application.version`. Mở ở Menu: ẩn Home, thân ngắn đi `menuHeightCut` (con của Panel neo mép trên, chữ phiên bản neo mép dưới). Mở trong ván thì `Pause`, X thì `Resume` |
| `QuitLevelPopup` | "QUIT LEVEL" như bản gốc: "You will lose" + tim vỡ "-1", nút đỏ Quit. Mở từ Cài đặt → Home khi đang trong ván (ván vẫn tạm dừng). Quit: `EnergyManager.ConsumeOne` (vô hạn thì không trừ) → `ScreenFlow.ShowMenu`; X: `Resume` chơi tiếp |
| `NoticePopup` | Thông báo chung "NOTICE" (khung chung, khung trắng nội dung, nút xanh Continue, X): `GameFlowUI.ShowNotice(message, title, onClose)`; mở trong ván thì tạm dừng. GameFlowUI nghe `IAPService.PurchaseFailed` → "Purchase Failed!" |
| `SettingToggleView` | 1 nút tròn: bật = `btn_setting2` (xanh), tắt = `btn_setting1` (xám) + gạch đỏ `off`; bấm đảo `AudioManager.MusicOn / SoundOn / VibrateOn` (lưu save), vừa bật rung thì rung thử; vẽ lại theo `AudioManager.SettingsChanged` |
| `SettingsButton` | Nút bánh răng (HUD trong ván, thanh trên Menu) → `GameFlowUI.OpenSettings` |
| `DailyRewardPopup` | Popup điểm danh (khung chung, "DAILY REWARD"): 7 `DailyRewardSlot` (như bản gốc: hàng 1 ngày 1–3, hàng 2 ngày 4–6, hàng 3 ô ngày 7 rộng hết 3 cột, icon to); CLAIM nhận quà hôm nay, nhận rồi thay bằng "Next reward in hh:mm:ss"; bấm ô hôm nay cũng nhận, ô lỡ có icon video thì xem quảng cáo bù |
| `DailyRewardSlot` | 1 ô: "Day n", tối đa 5 món (icon + số: coin "100", booster "x1", vô hạn "30m"); hôm nay viền vàng nhấp nháy, đã nhận mờ + tick, lỡ xám (+ video), chưa tới bình thường; nhận thì ô nảy + tick bật ra |
| `DailyRewardButton` | Nút Daily trên Home (cùng `MenuShortcutButton` tab -1): bấm → `GameFlowUI.OpenDailyReward`; còn quà hôm nay thì lắc + chấm đỏ (`SetAttention`) |
| `BuyBoosterPopup` | Hết lượt booster: icon, mô tả, "x n", giá coin (mờ khi thiếu), X; mở thì `Pause` |
| `RefillEnergyPopup` | "REFILL LIVES?" như ref (khung chung với Cài đặt): tim to có số năng lượng, "Get more lives to continue playing", ô đồng hồ `mm:ss` tới lần hồi kế (đầy: FULL); nút xanh Refill 900 coin nạp đầy (mờ khi đã đầy; thiếu coin thì đóng popup, không gọi `onCancel`, rồi `ScreenFlow.ShowShop`), nút vàng Free live (ads +1). `ShowOptional` (từ nút tim, có X) / `ShowRequired(onReady, onCancel)` (có năng lượng thì tự đóng rồi gọi `onReady`; có `onCancel` thì hiện X); tạm dừng ván khi mở |
| `LosePopup` | FAILED, "−1 🐟"; RETRY (qua `GameFlowUI` kiểm tra năng lượng), HOME → `ScreenFlow.ShowMenu` |

### Editor

| File | Menu | Việc làm |
| --- | --- | --- |
| `Level/ShikaCatLevelImporter` | Tools → PawBlock → Builders → Import ShikaCat Levels | JSON → `LevelDefinition` + `LevelDatabase` (thêm cuối: level Test, level Custom) |
| `Level/LevelEditorWindow` | Tools → PawBlock → Level Editor | Cửa sổ sửa / xếp level: chọn theo database, tạo mới / nhân bản vào `Levels/Custom`, đổi thứ tự; công cụ Khối / Số / Void / Xóa trên lưới; sửa hướng, khóa, "?"; Kiểm tra (LevelValidator + lời giải duy nhất), Giải, Chơi thử (`LevelEditorPlayTest` vào thẳng màn chơi) |
| `Level/LevelOrderTool` | Tools → PawBlock → Builders → Reorder Levels | Level 1 = Custom 4x4; level ShikaCat (bỏ 001, 004, level 1 khối) xếp theo D lý thuyết (bỏ phần T4); từ level 11 mỗi khối 10: ...9 đỉnh (khó nhất khối này + khối sau, không lấy level nhiều lời giải), ...0 nghỉ; đánh dấu ...9 từ 19 Hard, chia 30 dư 29 Very Hard (ghi bản gốc ShikaCat); Test / Custom khác giữ cuối; chạy Mechanic Injector |
| `Level/MechanicInjectorWindow` (+ `MechanicInjector`, `MechanicInjectorSettings`) | Tools → PawBlock → Mechanic Injector | Gắn Void / hướng / khóa / "?" vào level ShikaCat theo lịch trong `MechanicInjectorSettings.asset` (giới thiệu từng cơ chế, level nghỉ, kết hợp, level Hard / Very Hard nhiều cơ chế); mỗi lần gắn chạy `LevelValidator`, sai thì thử chỗ khác; random theo seed + số level nên chạy lại ra y hệt. Xem trước / Áp dụng (ghi `Levels/Mechanic`, thay vào database) / Khôi phục bản gốc |
| `Level/LevelDifficultyWindow` (+ `LevelDifficultyAnalyzer`, `LevelDifficultySettings`) | Tools → PawBlock → Level Difficulty | Độ khó lý thuyết từng level: mô phỏng người chơi giải từng khối (T1 đơn trực tiếp / T2 loại trừ 1 bước / T3 thử sâu / T4 không có nước bắt buộc) → điểm D, thời gian, rủi ro mất mạng (công thức: `Docs/DifficultyFormula.md`, trọng số trong `LevelDifficultySettings.asset`); biểu đồ D theo level (rê chuột hiện Level n / tên / D, bấm mở Level Editor); tick Hard / Very Hard (ghi cả bản gốc ShikaCat nếu là level Mechanic), Áp dụng gợi ý theo phân vị; Xuất CSV; Nạp CSV dữ liệu thực tế → tương quan Spearman |
| `Visuals/GameplayVisualsSetup` | Tools → PawBlock → Builders → Setup Gameplay Visuals | 9-slice khối, cắt atlas mặt mèo, tạo `GameplayVisuals.asset` |
| `UI/PopupBuilder` | Tools → PawBlock → Builders → Build Popups | Tạo `Canvas_Popup` (Overlay 1080x1920, sortingOrder 20) nếu chưa có, xóa rồi dựng lại `Popups` trong đó (xóa cả bản cũ trong `Canvas_HUD`), nối `GameFlowUI` cho SettingsButton / EnergyHUD / ScreenFlow, thêm `RewardedAds` vào `Manager` |
| `UI/ShopBuilder` | Tools → PawBlock → Builders → Build Shop | Thay `ShopPanel`: mái hiên + ô năng lượng / coin, danh sách cuộn (`NestedScrollRect`, VerticalLayoutGroup): Starter Pack, Remove Ads, No Ads Bundle, Special Bundle (5 gói), lưới Coin 3x2; tạo `ShopDatabase.asset` nếu chưa có |
| `UI/MainMenuBuilder` | Tools → PawBlock → Builders → Build Main Menu | `Canvas_Menu`: CanvasScaler 1080x1920, sortingOrder 10; `Panels` (ShopPanel – ShopBuilder, HomePanel + `PlayButton`, `Starter`, `NoAds`, `TopBar` (Avatar, Energy, Coin, Settings – ô kem `khung_pill_kem`), ProgressPanel tạm, mỗi panel `RectMask2D`); `AlwayOn/TabBar` (tab có `ButtonPress` tắt); Starter dựng từ `starter_back` / `starter_chest` / `starter_ribbon`, No Ads lắc riêng `Tv`; `MainMenuNavigator`, `MenuSwipe` trên `Panels`; `ScreenFlow` trên `Manager`; chữ viền dùng preset của `GameFontSetup` |
| `Save/SaveDataMenu` | Tools → PawBlock → Save Data | Log Save (in nội dung save.json), Open Save Folder, Delete Save (hỏi xác nhận, không chạy khi đang Play) |
| `Audio/AudioSetup` | Tools → PawBlock → Builders → Setup Audio | Tạo / bổ sung `AudioDatabase` (chỉ thêm âm còn thiếu), BGM nhập Streaming, object `AudioManager` (AudioManager + GameAudioEvents), `UIButtonSounds` trên 3 canvas |
| `UI/UIZoneBuilder` | Tools → PawBlock → Builders → Build UI Zone | `Canvas_HUD/UIZone` (4 cạnh) chứa PlayZone, TopPanel, DownPanel, RevealBanner; gọi Build Main Menu để dựng `HomePanel/UIZone` (cạnh trên). Builder HUD tìm theo `PopupBuilder.HudRoot` (UIZone nếu có) |
| `UI/TutorialBuilder` | Tools → PawBlock → Builders → Build Tutorial | Dựng lại `Canvas_HUD/UIZone/TutorialPanel` (TutorialView), object `TutorialHand`, gắn `TutorialController` lên `Manager`; dựng `Canvas_HUD/BoosterTutorial` (BoosterTutorialView), gắn `BoosterTutorial`; dựng `Canvas_Popup/Popups/MechanicIntroPopup`, gắn `MechanicIntro` + 3 ảnh |
| `UI/GameFontSetup` | Tools → PawBlock → Builders → Apply Game Font | Fredoka Bold làm font mặc định TMP (fallback LiberationSans), gán shader `TMP_SDF-Mobile` theo đường dẫn, tạo preset viền `Fredoka-Bold SDF - Outline Purple / Green / Brown`, đổi font mọi chữ LiberationSans trong scene và prefab `Assets/_Project` |
| `UI/SettingsBuilder` | Tools → PawBlock → Builders → Build Settings | `TopPanel`: xóa ReplayButton / Energy, dựng `SettingsButton` góc trên phải; thay `SettingsPopup` trong `Canvas_Popup/Popups` (xóa ConfirmReplayPopup cũ) mà không dựng lại popup khác; gắn `SettingsButton` cho nút Settings trên Menu |
| `UI/BoosterBuilder` | Tools → PawBlock → Builders → Build Boosters | Dựng phần con của 3 nút booster, `RevealBanner`, `TopPanel/LevelFrame/Shield`; thêm `BoosterManager` vào `Manager` |

---

## Scene `SampleScene`

```text
Main Camera            Camera orthographic, LevelManager chỉnh size/vị trí
Global Light 2D
SpawnGrid              GridSpawner
Manager                GridManager, InputManager, DragAndDropController,
                       LivesManager, LevelManager, GameplayFeedback, RewardedAds, BoosterManager,
                       EnergyManager, ScreenFlow, TutorialController, BoosterTutorial, MechanicIntro
TutorialHand           TutorialHand / Sprite (tắt sẵn): bàn tay tutorial, đầu ngón ở gốc object
SelectionBox           SelectionBoxView + SpriteRenderer
bg                     SpriteRenderer + BackgroundFitter
Canvas_HUD             Canvas Overlay, CanvasScaler 1080x1920, sortingOrder 0; ScreenFlow bật khi vào màn chơi, tắt ở Menu
└── UIZone             SafeAreaZone (4 cạnh): mọi UI của HUD nằm trong safe area
    ├── PlayZone       PlayZone + Image (tắt, bật `showZone` để xem vùng): lưới luôn nằm trọn trong vùng này
    ├── TopPanel       LevelHUD
    │   ├── LevelFrame / LevelText, Hearts (LivesUI), Shield (ShieldIndicator, khung băng)
    │   └── SettingsButton  SettingsButton (khung_setting_3 + bánh răng), góc trên phải
    ├── DownPanel/Booster  Booster1..3: BoosterButton (Hint, Shield, Reveal) / Glow, Icon, Count, Add, Lock
    ├── RevealBanner   RevealBanner + CanvasGroup (ẩn khi không chọn số)
    └── TutorialPanel  TutorialView + CanvasGroup (tắt sẵn): Title, SkipButton, Dots/Dot1..4, Bubble/Message
└── BoosterTutorial    BoosterTutorialView + Canvas (override, order 5) + GraphicRaycaster + CanvasGroup (tắt sẵn): Dim, Bubble/Message, Hand
Canvas_Popup           Canvas Overlay 1080x1920, sortingOrder 20, luôn bật (dùng chung Menu và màn chơi)
└── Popups             GameFlowUI (dựng bằng Build Popups)
    ├── WinPopup  ReviveLivesPopup  LosePopup  SettingsPopup  BuyBoosterPopup  RefillEnergyPopup  MechanicIntroPopup   (tắt sẵn)
    └── mỗi popup: Dim (đen alpha 0.88 – Linear color space nên cần cao để tối như ref), Panel / Title, nội dung, nút
Canvas_Menu            Canvas Overlay 1080x1920, sortingOrder 10 (trên HUD), MainMenuNavigator
├── Panels             ShopPanel | HomePanel | ProgressPanel (trượt ngang, RectMask2D)
│   └── HomePanel      Background (BackgroundCover), PlayButton,
│                      UIZone (SafeAreaZone cạnh trên): Starter, NoAds,
│                      TopBar (Avatar, Energy: EnergyHUD tim + số, đồng hồ, "+"; Coin: CoinHUD; Settings: SettingsButton) – chỉ ở tab Home
└── AlwayOn            Lớp luôn hiện (không chặn bấm)
    └── TabBar         1080x200 neo bottom center: Background (home_tab2), SelectedTab (home_tab1), Tab_Shop / Tab_Home / Tab_Progress (neo bottom center)
EventSystem
DragCountBubble        Instance của prefab, để tắt sẵn; DragAndDropController bật khi kéo
```

Khối mèo, chữ khen không đặt sẵn trong scene: `GridManager` và `GameplayFeedback` tạo chúng từ prefab qua `ObjectPool` ở lần dùng đầu, sau đó tái sử dụng. Ô lưới cũng được `GridSpawner` tái sử dụng giữa các level.

---

## Luồng một nước đi

```text
InputManager ─► DragAndDropController ─► GridManager.BeginSelection (chạm 1 ô trống)
                       │
                       ├► GridManager.UpdateSelection (kéo) ─► DragSelection.MoveTo (kẹp, không phủ khối / Void)
                       │                    ├► SelectionBoxView.Show (mép trượt mượt) / SetBlocked (mép bị chặn lấp khe)
                       │                    └► BoardModel.Check == Ready ? (chờ khung giãn xong) PlacedBlockView.ShowPreview
                       └► GridManager.EndSelection (thả tay) ─► BoardModel.Check (Ready mà khung chưa giãn xong: chờ)
                              │
              ┌───────────────┼──────────────────────────────┐
              ▼               ▼                              ▼
     Chưa Ready          Ready, BoardModel.IsSolvableWith?  Ready + còn lời giải
     onInvalidMove            │ không                        │
     (PlacementResult)        ▼                              ▼
                     PlacedBlockView.PlayReject          BoardModel.Place (giảm khóa)
                     onMistake + LivesManager.LoseLife   PlacedBlockView.Confirm, onBlockPlaced
                              ▼                              │ phủ kín lưới?
                     LivesUI, GameplayFeedback               ▼
                              │ hết mạng                  onGameWin
                              ▼                              ▼ (0.8 s)
                     onOutOfLives ─(0.8 s)► OutOfLives   Won ─► WinPopup ─► NextLevel
                                 ReviveLivesPopup ─► Revive (Playing, giữ lưới)
                                                 └─► GiveUp ─► Lost ─► LosePopup ─► Retry
```

## Event chính

| Nguồn | Event | Người nghe |
| --- | --- | --- |
| `GridSpawner` | `gridCellEvent(List<CellView>)` | `GridManager`, `LivesManager` |
| `GridManager` | `onBlockPlaced(BlockModel)` | `GameplayFeedback` |
| `GridManager` | `onMistake(center, size)` | `GameplayFeedback` |
| `GridManager` | `onInvalidMove(PlacementResult, center, size)` | (chưa dùng, dành cho phản hồi khi thả sai luật) |
| `GridManager` | `onClueUnlocked(CellModel)` | (chưa dùng, dành cho âm thanh/hiệu ứng mở khóa) |
| `GridManager` | `onGameWin` | `LevelManager` |
| `LivesManager` | `onLivesChanged(current, max)`, `onLifeLost(current)` | `LivesUI` |
| `LivesManager` | `onOutOfLives` | `LevelManager` |
| `LevelManager` | `onLevelLoaded(number)` | `LevelHUD`, `GameFlowUI` |
| `LevelManager` | `onStateChanged(GameState)` | `GameFlowUI`, `BoosterManager`, `EnergyManager` (Lost → −1), `RefillEnergyPopup` |
| `LivesManager` | `onShieldChanged(bool)`, `onShieldBlocked` | `BoosterButton`, `ShieldIndicator` |
| `BoosterManager` | `onBuyRequested(type)` | `GameFlowUI` → `BuyBoosterPopup` |
| `BoosterManager` | `onRevealModeChanged(bool)`, `onBoosterUsed(type)` | `BoosterButton`, `RevealBanner` |
| `BoosterInventory` | `Changed(type, count)` (event C#) | `BoosterButton` |
| `EnergyManager` | `onEnergyChanged(current, max)`, `onTick` | `EnergyHUD`, `RefillEnergyPopup` |

---

## Quy ước

### Tọa độ lưới

- `x` = cột (trái → phải), `y` = hàng (**dưới → trên**), gốc (0, 0) ở góc dưới-trái.
- Ô `(x, y)` nằm tại world `((x - (w-1)/2) * 1.2, (y - (h-1)/2) * 1.2)`, lưới căn giữa gốc tọa độ.
- `GridSpawner.CellSpacing = 1.2`.
- ShikaCat đếm hàng từ trên xuống; importer lật: `y = rows - 1 - row`.

### Thứ tự vẽ (sortingOrder)

| Order | Thành phần |
| --- | --- |
| 0 | Cell (SortingGroup; bên trong: nền 0, mũi tên 5, số 6, ổ khóa 6–8) |
| 19 | Tai mèo |
| 20 | Thân khối |
| 21 | Mặt mèo |
| 22 | Số trên khối |
| 24 | Viền đủ value (`Ring`) |
| 30 | SelectionBoxView (khung đang kéo) |
| 40–41 | Bong bóng số ô |
| 60 | Chữ khen |
| 80 | Bàn tay tutorial |

Canvas_HUD (order 0) < BoosterTutorial (Canvas override, order 5) < nút booster đang được chỉ (order 6) < Canvas_Menu (10) < Canvas_Popup (20).

### Dữ liệu level (`LevelDefinition`)

| Trường | Ý nghĩa |
| --- | --- |
| `size`, `height` | Số cột, số hàng (`height = 0` nghĩa là lưới vuông) |
| `cellOverrides` | Ô `Void` (không chơi được) |
| `clues[]` | `x`, `y`, `value`, `color`, `mechanic { constraint, lockSteps, hidden }` |
| `solution[]` | Lời giải tham khảo (`clueId`, `x`, `y`, `width`, `height`) – solver không phụ thuộc vào nó |
| `rules.mistakeLimit` | Hiện không dùng; số mạng lấy từ `LivesEnergyConfig.maxLives` |
