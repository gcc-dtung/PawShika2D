# Project Conventions – PawBlock

> Ngoại lệ / bổ sung **riêng dự án này** cho [UnityCodingGuidelines.md](UnityCodingGuidelines.md).
> Guide chung không sửa; chỗ nào dự án làm khác thì ghi ở đây, kèm lý do.

| | |
| --- | --- |
| Áp dụng từ | P0 – [RefactorPlan.md](RefactorPlan.md) |
| Cập nhật lần cuối | 2026-10-12 (P5) |

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

- Test của dự án nằm trong `Assembly-CSharp-Editor` (`<module>/Tests/Editor/`, từ P1), tới P6 mới tách asmdef.
- Chạy test **lọc theo assembly `Assembly-CSharp-Editor`**: Test Runner còn có ~890 test của package `com.besty.unity-skills` (vài test của package fail sẵn, không liên quan game).
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
| Config | `GameConfig` / `GameplayVisuals` ở `_Project/Configs/`, Bootstrap giữ `[SerializeField]`, truyền **config con** (`LivesEnergyConfig`, `EconomyConfig`...) qua `Init`. Không `Resources.Load` |
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
