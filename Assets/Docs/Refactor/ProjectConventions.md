# Project Conventions – PawBlock

> Ngoại lệ / bổ sung **riêng dự án này** cho [UnityCodingGuidelines.md](UnityCodingGuidelines.md).
> Guide chung không sửa; chỗ nào dự án làm khác thì ghi ở đây, kèm lý do.

| | |
| --- | --- |
| Áp dụng từ | P0 – [RefactorPlan.md](RefactorPlan.md) |
| Cập nhật lần cuối | 2026-10-13 (P9) |

---

## 1. DOTween

Dự án dùng DOTween cho mọi animation, không dùng coroutine (đã quen từ RoadMap 06).

| # | Quy tắc | Lý do |
| --- | --- | --- |
| 1 | Mỗi tween giữ trong field (`Tween` / `Sequence`), không bắn rồi quên | Kill được khi phát lại / tắt object |
| 2 | Luôn `SetLink(gameObject)` | Object bị hủy thì tween chết theo, không lỗi null |
| 3 | `Kill()` trong `OnDisable` (và khi phát lại, khi trả về pool) | Object pool tắt / bật lại không còn tween cũ chạy dở |
| 4 | `OnKill` chỉ xóa field **nếu field vẫn trỏ đúng tween đó** (`if (_tween == t) _tween = null;`) | `Kill` gọi trong callback của chính tween bị DOTween hoãn; xóa thẳng sẽ xóa nhầm tween mới |
| 5 | Thứ cần trả về trạng thái gốc khi bị hủy (camera, scale, chữ khen về pool) đặt trong `OnKill` | `OnKill` chạy cả khi xong lẫn khi bị hủy giữa chừng |
| 6 | Popup / UI chạy lúc game dừng dùng `SetUpdate(true)` | Chạy theo thời gian thực, không bị `timeScale` |
| 7 | Sức chứa khởi tạo 500 tween / 150 sequence (`TweenSetup`) | Tránh DOTween tự nới sức chứa giữa ván (cảnh báo + giật) |

## 2. Comment và tài liệu

- **Comment tiếng Việt** trong toàn bộ code (guide 12 yêu cầu thống nhất một ngôn ngữ; dự án chọn tiếng Việt).
- Tên type / method / biến vẫn **tiếng Anh**, không dấu.
- Tài liệu trong `Assets/Docs/` viết tiếng Việt; RoadMap mỗi lần push một file (`Docs/RoadMap/00_README.md`).

## 3. Định dạng: chỉ áp cho file đang sửa

- `.editorconfig` ở gốc repo đặt mức **warning**, không error, vì code cũ chưa theo hết (vd `if (x) return;` một dòng, thiếu `private` trước hàm Unity).
- **Không format hàng loạt.** Chỉ sửa định dạng ở file đang sửa vì lý do khác, để diff refactor dễ đọc và ít conflict.
- Không dùng "Reformat Code" cho cả thư mục / solution.

## 4. Menu Editor

- Giữ gốc `Tools/PawBlock/…` cho mọi `MenuItem` (hiện 33 menu), thay vì `Tools/<Feature>/…` của guide 6.4.
- Lý do: người dùng đã quen; gom một chỗ dễ tìm. Bên trong có thể chia `Tools/PawBlock/<Feature>/…`.

## 5. Analyzer / IDE

- Không thêm `Microsoft.Unity.Analyzers.dll` vào `Assets/`:
  - **Rider** (External Script Editor hiện tại) có sẵn inspection Unity tương đương.
  - **VS Code**: extension *Unity* (`visualstudiotoolsforunity.vstuc`) đã kèm `Microsoft.Unity.Analyzers`; package `com.unity.ide.visualstudio` tự đưa vào `.csproj` khi chọn VS Code trong *Preferences → External Tools*.
- Nhờ vậy Unity không chạy analyzer lúc compile (không thêm cảnh báo vào Console, không rủi ro lệch phiên bản Roslyn).

## 6. Test

- Test của dự án ở `<module>/Tests/Editor/`, assembly `PawBlock.Core.Foundation.Tests`, `PawBlock.Gameplay.Tests`, `PawBlock.Meta.Tests`, `PawBlock.Game.Tests` (từ P6; trước đó `Assembly-CSharp-Editor`).
- Chạy test **lọc theo 4 assembly trên**: Test Runner còn có ~890 test của package `com.besty.unity-skills` (vài test của package fail sẵn, không liên quan game).
- Test đụng tới save (ví, kho booster, quà...) **bắt buộc** dùng `new JsonFileSaveService(null)` (chỉ bộ nhớ) truyền vào constructor / `Init`: save thật ở Edit mode ghi thẳng `save.json` của máy.
- Test vào Play mode (`EnterPlayMode`): save bộ nhớ không giữ được (`ResetStatics` chạy lại, `TrustedClock` tự ghi giờ) → sao lưu byte các file save ở `[OneTimeSetUp]`, trả lại ở `[OneTimeTearDown]` (mẫu: `FeelPlayModeTests`, gồm cả `save.v1.bak`).

## 7. Namespace và thư mục (từ P1)

| Quy tắc | Ví dụ | Lý do |
| --- | --- | --- |
| **Một namespace cho mỗi module / feature**, thư mục con không thêm cấp (khác ví dụ `Core.Foundation.Save` của guide 5) | `Core/Foundation/Save/JsonFileSaveService.cs` → `PawBlock.Core.Foundation`; `Features/Gameplay/Scripts/Board/BoardModel.cs` → `PawBlock.Features.Gameplay` | Ít `using`; thư mục con trong một module gọi thẳng nhau (guide 5) |
| Code Editor dùng **cùng namespace** với module, không thêm `.Editor` | `Features/Gameplay/Editor/Level/LevelEditorWindow.cs` → `PawBlock.Features.Gameplay` | Namespace có đoạn `Editor` che mất class `UnityEditor.Editor` (`: Editor` báo lỗi) |
| Test: namespace module + `.Tests` | `PawBlock.Features.LuckyWheel.Tests` | Tách khỏi code thật, vẫn thấy type của module |
| Không đặt type trùng tên namespace (`Gameplay`, `Shop`, `Profile`, `Settings`, `Game`, `Core`…) | — | Trong `PawBlock.*`, tên đó sẽ trỏ vào namespace |
| `Core` và `Shared` không có `Scripts/`; feature và `Game` có `Scripts/` | `Core/Foundation/Audio/`, `Features/Shop/Scripts/` | Theo cây thư mục guide 5 |
| Tối đa 4 tầng từ `_Project/`: gộp `Booster/Effects` vào `Booster/`, `Level/Generator` vào `Level/` | `Features/Gameplay/Scripts/Booster/FreezeBooster.cs` | Guide 5 |

- P1 **không** sửa định dạng (`if` một dòng, `private` thiếu…) dù đụng mọi file: chỉ bọc namespace + thụt lề + `using`, để diff chỉ có thay đổi cơ học. Định dạng sửa dần khi sửa logic từng file (mục 3).
- Phụ thuộc sai chiều lộ ra sau khi có namespace: xem [Dependencies.md](Dependencies.md).

## 8. Bootstrap, Init và save (từ P3)

| Quy tắc | Cách làm |
| --- | --- |
| **GameBootstrap là nơi duy nhất tạo service** | `Game/Scripts/GameBootstrap.cs` (`[DefaultExecutionOrder(-1000)]`): save → migrate → settings, đồng hồ, quảng cáo, IAP → `GameServices` → `Init` từng component. Không `new` service ở chỗ khác |
| Component cần dependency | Thêm `public void Init(...)`, giữ vào field, **không đọc dependency trong `Awake`** (đọc ở `Init` / `OnEnable` / `Start`). Bootstrap chạy trước `Awake` / `OnEnable` của mọi script khác |
| Tham chiếu Bootstrap → component | Thêm `[SerializeField]` field (hoặc mảng) vào `GameBootstrap`; `GameBootstrapCollector` (Editor) tự điền khi **lưu scene / vào Play**. Thiếu thì Console báo `[GameBootstrap] Thiếu tham chiếu ...` |
| Feature khác nhận **contract** | `ICurrencyWallet`, `IBoosterInventory`, `IEnergyService`, `IRewardGranter`; cùng feature được nhận class cụ thể (`CoinWallet` cho `CoinHUD`) |
| Cần dữ liệu của feature khác (level hiện tại...) | Game truyền `Func<...>` qua `Init` / constructor; không đọc section của feature khác |
| **Save** | Mỗi feature 1 class `[Serializable]` có `const string Key`, lấy bằng `save.Get<T>(T.Key)` (object sống), sửa rồi `save.MarkDirty()`. Field mới: đặt giá trị mặc định ngay tại khai báo |
| Đổi cấu trúc save | Thêm bước vào `Game/Scripts/Save/SaveMigrator` + test với file mẫu (`Docs/Refactor/SaveSamples`). `JsonFileSaveService` không cho ghi file đời cũ chưa migrate |
| Công cụ Editor đọc save | `new JsonFileSaveService(JsonFileSaveService.DefaultPath)` ngoài Play mode; đang Play thì báo người dùng thoát Play trước khi ghi |
| Test đụng save | `new JsonFileSaveService(null)` (chỉ bộ nhớ) truyền vào constructor / `Init` |
| Quảng cáo / IAP / giờ | `IAdsService`, `IIAPService`, `IClock`; bản giả `FakeAdsService`, `FakeIAPService`. Tích hợp SDK: thêm class cài interface, đổi 1 dòng trong Bootstrap |

## 9. Không Singleton cho game logic (từ P4)

| Quy tắc | Cách làm |
| --- | --- |
| Không `.Instance` trong `Features/` / `Game/` | Chỉ `AudioManager` (hạ tầng) kế thừa `Singleton<T>`. Script mới nhận dependency qua `Init` từ `GameBootstrap` |
| `Singleton<T>` bản an toàn | Đặt sẵn trong scene; không tự tìm / tự tạo; thiếu thì `Instance` báo `LogError`. Hỏi mà không muốn báo lỗi: `HasInstance` (vd lúc thoát Play, hủy object) |
| Feature khác cần ra lệnh / hỏi Gameplay, màn hình | `IScreenNavigator` (ScreenFlow), `IGamePause` (LevelManager), `IBoosterCatalog`, `IRewardIcons` trong `Core/Contracts` |
| View tạo lúc chạy (pool, prefab) cần config | Bên tạo gọi `Init(...)` ngay khi tạo (vd `PlacedBlockView.Init(visuals)` trong `createFunc` của pool) |
| Config | `GameConfig` ở `Game/Configs`, `GameplayVisuals` ở `Features/Gameplay/Configs` (từ P8), Bootstrap giữ `[SerializeField]`, truyền **config con** (`LivesEnergyConfig`, `EconomyConfig`...) qua `Init`. Không `Resources.Load` |
| Hàm tính dùng config | Nhận config làm tham số (`LevelTimer.ComputeSeconds(level, cells, cfg)`), không đọc static |
| Cờ trạng thái | Thuộc tính instance (`TutorialController.IsActive` / `IsPending`), không `static bool` |
| Cheat / Debug Panel | `DebugCheats` (Game) dùng `GameServices` + `GameScene` do Bootstrap điền; feature không dùng `GameScene` |

## 10. Không tìm / lấy / gắn component lúc chạy (từ P5)

| Cần | Cách làm trong dự án |
| --- | --- |
| Component trên cùng object / con / cha | `[SerializeField]` + `OnValidate()` trong `#if UNITY_EDITOR` tự điền khi trống (chạy cả khi gắn component, mở scene). Script cũ chưa điền: gọi lại OnValidate rồi lưu scene / prefab |
| Object khác trong scene dùng chung (GridSpawner, Camera, nút booster…) | Bootstrap truyền qua `Init`; field Inspector đã gán thì giữ, không thêm fallback Find |
| Nút ở feature mở popup chung | `IMenuPopups` (GameFlowUI cài), không `[SerializeField] GameFlowUI` |
| Danh sách con thay đổi theo hierarchy (ButtonPress, âm nút, nút xám) | Hàm `EditorCollect()` + `Shared/Editor/UIAutoSetup` chạy khi lưu scene / vào Play (menu `Tools/PawBlock/UI/Setup Buttons`). Nút mới dưới Canvas có `UIButtonPress` tự được gắn `ButtonPress` khi lưu scene |
| Object con UI / sprite trước đây dựng bằng code | `[ContextMenu("Dựng sẵn object con")] public void EditorBuild()` dùng `EditorChildren.Child / On / Rect` (có Undo, có rồi thì dùng lại) rồi lưu. Lúc chạy chỉ dùng field; thiếu thì `LogError` + tắt component |
| Nhiều object cùng loại lúc chạy (pool) | `Instantiate(template)` với template là con tắt sẵn (component trả về trực tiếp, không GetComponent) |
| Sprite sinh lúc chạy (`ProceduralSprites`) | Không lưu được vào scene: object có sẵn, sprite gán ở `Awake` |
| Raycast / va chạm | Registry `Dictionary<Collider2D, T>` do nơi spawn đăng ký (vd `GridSpawner.TryGetCell`) |
| Runner hạ tầng (ghi save, đồng bộ giờ, tick mặt mèo) | Component đặt sẵn trong scene; service nhận qua `AttachRunner` / constructor. Không có runner (công cụ Editor, test): save ghi ngay, đồng hồ dùng giờ máy |
| Test dựng object bằng code | Tạo GameObject tắt, `AddComponent` các component cần, gán field qua `SerializedObject`, rồi bật (mẫu: `FeelPlayModeTests` với `SelectionBoxView`) |

**Ngoại lệ:** `Game/Scripts/Cheats/CheatPanel` tự tạo lúc chạy (`RuntimeInitializeOnLoadMethod`, `AddComponent`, `GetComponent` cho Canvas chặn chạm). Lý do: chỉ là công cụ debug, cả file bọc `#if !DISABLE_CHEATS`; đặt vào scene thì bản build có `DISABLE_CHEATS` sẽ bị Missing Script.

## 11. Assembly Definition (từ P6)

| Quy tắc | Cách làm |
| --- | --- |
| Thêm script | Đặt đúng thư mục module là vào đúng assembly; không tạo asmdef mới cho thư mục con |
| Lỗi "namespace không tồn tại / thiếu assembly reference" | Là phụ thuộc sai: sửa bằng event / contract (Core/Contracts) hoặc để Game nối; **không** thêm reference ngược |
| Feature Meta | Chung `PawBlock.Meta` (Economy, Shop, DailyReward, DailyChallenge, LuckyWheel, Profile, Settings) nên compiler không chặn Meta ↔ Meta: vẫn theo guide (không `using` feature khác), soát bằng script đo trong [Dependencies.md](Dependencies.md) |
| Code Editor | Thư mục `Editor/` có asmdef riêng (`includePlatforms: Editor`). Tool đụng nhiều feature cùng lúc đặt ở `Game/Editor` |
| Test mới cho feature Meta | Đặt ở `Features/<Feature>/Tests/Editor/`; thư mục `Tests/` cần file `PawBlock.Meta.Tests.asmref` (như DailyChallenge / DailyReward / LuckyWheel) |
| `using` thừa | Phải gỡ: `using` tới namespace của assembly không được tham chiếu cũng là lỗi compile |
| Thư viện | DOTween core (`DOTween.dll`) tự tham chiếu; extension UI / Sprite cần `DOTween.Modules`. TMP: `Unity.TextMeshPro`, UI: `UnityEngine.UI`, Spine: `spine-unity` + `spine-csharp`, UIParticle: `Coffee.UIParticle`, Input: `Unity.InputSystem` |

## 12. Prefab và popup (từ P7)

| Quy tắc | Cách làm |
| --- | --- |
| Sửa UI | Mở prefab trong Prefab Mode (`Features/<F>/Prefabs`, `Shared/Prefabs`, `Game/Prefabs`). Không sửa instance trong scene rồi Apply All. Không còn builder dựng UI (đã xóa ở P7) |
| Popup mới | Kế thừa `PopupView` (abstract), hàm riêng nhận dữ liệu rồi gọi `Open()`; dùng `OnOpened` / `OnClosed`, không override `Open` / `Close`. Root có `CanvasGroup` + `Canvas` + `GraphicRaycaster`. Thêm instance tắt vào `Canvas_Popup/Popups`; Bootstrap tự thu vào danh sách popup (GameFlowUI đóng khi đổi level nếu `CloseOnLevelLoad`) |
| Màn hình trộn nhiều feature | Ghép ở `Game/Prefabs` (MainMenu, tab Shop / Home); prefab của feature không lồng prefab feature khác |
| Script gốc của prefab | Mỗi prefab màn hình / tab có script View ở object gốc, trùng tên prefab (`ShopPanel`, `HomePanel`, `MenuTabBar`, `DailyChallengePanel`): tự giữ tham chiếu view con và nối `Init` cho chúng; Bootstrap chỉ gọi `Init` của gốc. Tab mới của Main Menu: kế thừa `MenuPanel`, thêm vào `MainMenuNavigator.menuPanels` và 1 tab trong `MenuTabBar` |
| Widget | Dùng `Shared/Prefabs/Widgets` (`CloseButton`, `Button_Primary`, `SettingsButton`); widget có script của feature nằm ở prefab của feature đó (CoinHUD, SettingToggle...) |
| Gán từ ngoài | Tham chiếu tới object ngoài prefab: qua `Init` từ Bootstrap; nếu là override trên instance thì ghi Tooltip "Gán từ ngoài" (`LuckyWheelButton.popup`, `PlayZone.targetCamera`) |
| Canvas con | Mỗi popup / tab có Canvas riêng; phần đổi liên tục (đồng hồ, thanh sao, viền đỏ, nút booster) Canvas riêng. Ẩn bằng `SetActive(false)`; **tắt Canvas component là ẩn luôn con** |
| Danh sách nút / ButtonPress | `UIAutoSetup` chạy khi lưu scene, khi lưu prefab và trước Play; `Tools/PawBlock/UI/Setup Buttons` xử lý cả mọi prefab |
| Gộp object có sẵn vào prefab | `PrefabUtility.ConvertToPrefabInstance` (khớp theo hierarchy) giữ tham chiếu trong prefab; mảng cấp scene (Bootstrap, UIButtonSounds) thu lại bằng Collector / UIAutoSetup |

## 13. Asset, atlas, nén texture (từ P8)

| Quy tắc | Cách làm |
| --- | --- |
| Bản gốc | `Assets/ThirdParty/PawBlock` (từ `GameAssets`), `Assets/ThirdParty/ShikaCat`: giữ nguyên cấu trúc, **không sửa, không tham chiếu trực tiếp**. Cần dùng ảnh nào thì `AssetDatabase.CopyAsset` sang `Art/` của module rồi tham chiếu bản copy |
| Asset đang dùng | `<module>/Art/{UI,Grid,VFX,Cats,Praise_Badges,Materials,Shaders,Spine}` của module duy nhất dùng nó; ≥ 2 module dùng → `Shared/Art`. Âm thanh `Shared/Audio`, font `Shared/Fonts` |
| Config | `Configs/` của feature sở hữu (`Features/Gameplay/Configs/Levels` cho level); `GameConfig` ở `Game/Configs`, `AudioDatabase` ở `Shared/Configs`. Tool Editor tìm config **theo kiểu** (`GameConfigSetup.Find<T>()`), không viết cứng đường dẫn |
| Di chuyển / đổi tên | Chỉ qua Unity (`AssetDatabase.MoveAsset` / Project window) để giữ GUID. Sửa các hằng đường dẫn trong tool Editor (`AudioSetup`, `GameFontSetup`, `GameplayVisualsSetup`, tool Level) cùng lúc |
| Sprite Atlas | Mỗi module có UI riêng 1 atlas V2 ở `<module>/Art/<Module>Atlas.spriteatlasv2`, trỏ tới thư mục `Art/UI` (+ `Grid`, `Cats`... nếu cần). Không Rotation, không Tight Packing, Padding 4, không mipmap, max 2048. Ảnh nền to, texture Spine, texture particle **không** vào atlas |
| Nén texture | Preset `_Project/Settings/Presets` tự áp khi import ảnh mới trong `_Project`: `Backgrounds/` ASTC 8x8, `Art/UI/` ASTC 4x4, còn lại ASTC 6x6; tắt Read/Write, mipmap. Atlas: override Android ASTC 4x4. Ảnh có sẵn đổi thư mục thì preset **không** tự chạy lại: đặt override Android bằng tay theo bảng này |
| Kiểm tra sau khi đổi asset | Batches / SetPass 3 màn và Build Report so với [Baseline.md](Baseline.md); nhìn ảnh chụp có vỡ khối / lỗi viền không |

## 14. Tài liệu và checklist (từ P9)

| Quy tắc | Cách làm |
| --- | --- |
| README feature | Feature có hơn vài script có `Features/<F>/Docs/README.md` (guide 5): tóm tắt, phụ thuộc (assembly, event phát / nghe, interface cài / dùng, section save, config), class chính, cách Bootstrap nối, ghi chú kiến trúc. Hiện có 7 (Settings 2 script: không cần) |
| Tài liệu tổng | `Docs/ProjectStructure.md` (module, thư mục, scene, event), `Docs/Features/*` (hành vi từng tính năng, cách sửa), `HowTo_*` (mở rộng). Đổi class / đường dẫn / luồng thì sửa cùng commit |
| Tài liệu lịch sử | `Docs/RoadMap/*`, `Docs/TaskBreakdown.md`, kết quả từng giai đoạn trong `RefactorPlan.md`: **không sửa** tên class / đường dẫn cũ (ghi đúng thời điểm đó) |
| Scene | `_Project/Scenes/Main.unity` (đổi tên từ `SampleScene` ở P9, GUID giữ) |
| Kiểm tra link | Mọi link markdown nội bộ phải trỏ tới file có thật (script dò ở RoadMap 28) |

### Rà checklist guide mục 15 (P9, 2026-10-13)

Kiểm tự động bằng script trên toàn `Assets/_Project` (bỏ `Editor/`, `Tests/`, khối `#if UNITY_EDITOR`):

| Mục checklist | Kết quả |
| --- | --- |
| Feature `using` / tham chiếu feature khác; Core / Shared dùng feature / Game | 0 trong code runtime / Editor. Còn 1 trong test: `RewardGranterTests` (DailyReward) dùng `CoinWallet` (Economy) để test tích hợp – chấp nhận, xem [Dependencies.md](Dependencies.md) |
| `Core/Contracts` chỉ có định nghĩa | Đạt (không có logic) |
| `.Instance` cho game logic | 0 (chỉ `AudioManager`, hạ tầng) |
| `Find` / `GetComponent` / `AddComponent` / `Camera.main` lúc chạy | 4 chỗ, đều trong `CheatPanel` (ngoại lệ mục 10) |
| Field Inspector `[SerializeField] private` | Đạt: không còn field `public` (trừ `UnityEvent`) trong MonoBehaviour |
| Ghi vào ScriptableObject lúc chạy | Không thấy (dò gán field của database / config) |
| Event đăng ký có hủy | EventBus và event C# của object khác: có cặp hủy. `onClick.AddListener` trên **nút con của chính object** (popup, nút, thẻ) không `RemoveListener`: chấp nhận vì nút và listener cùng vòng đời |
| Tween dừng khi disable | Mọi file dùng DOTween có `SetLink` / `Kill`; `MenuTabBar.Animate` chỉ thêm tween vào sequence của `MainMenuNavigator` (đã `SetLink`) |
| Thư mục ≤ 4 tầng, không `Misc/Temp/Old/Backup` | Đạt (không tính `Art/`, `Configs/Levels` giữ cấu trúc art gốc) |
| Sprite trong atlas | 207 sprite, 20 ngoài atlas: nền to (`Backgrounds/`), Spine, VFX particle – đúng quy tắc mục 13; riêng `Shop/Art/UI/Frames_Borders/frame_coin.png` (Shop chỉ 1 sprite, chưa đáng 1 atlas) |
| `Resources/` tối thiểu | Chỉ `DOTweenSettings` (+ TMP của package) |

**Còn nợ** (chưa sửa, không ảnh hưởng hành vi; làm khi đụng tới file):

| # | Nợ | Ghi chú |
| --- | --- | --- |
| 1 | `PopupView` có `protected virtual void Awake() {}` / `OnEnable() {}` rỗng (guide: không hàm lifecycle rỗng) | Là hook cho lớp con (`base.Awake()` / `base.OnEnable()` ở 12 popup). Bỏ thì đổi mọi override thành hàm riêng – làm khi sửa `PopupView` lần tới |
| 2 | 15 file còn `Debug.Log` thường (LevelManager 3, TrustedClockService 2, DailyChallengeManager 2, GridManager 2...) | Log luồng / chẩn đoán có chủ đích, không phải log thử. Nên gom qua 1 hàm log có thể tắt (`[Conditional]`) trước khi phát hành |
| 3 | `RewardGranterTests` dùng `PawBlock.Features.Economy` | Test tích hợp; tách được bằng ví giả cài `ICurrencyWallet` nếu cần |
| 4 | Field / object tên cũ: `ShopProduct.shield` (= Đóng băng), object `ShieldFrost` trong `GameplayHUD` | Đổi tên field serialize phải kèm `FormerlySerializedAs` + kiểm `ShopDatabase.asset`; object chỉ là tên |
| 5 | `Shop` chưa có atlas (1 sprite) | Thêm khi Shop có thêm art riêng |
| 6 | Chưa chạy trên máy Android thật sau P8 (ASTC) | Cần thử trước khi phát hành |
| 7 | Mục checklist không kiểm tự động được: `FormerlySerializedAs` khi đổi tên field, đúng một trách nhiệm, `switch` theo loại | Soát tay khi review từng thay đổi |
