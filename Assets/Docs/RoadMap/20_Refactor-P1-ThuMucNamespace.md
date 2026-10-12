# 20 – Refactor P1: thư mục và namespace

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `8578e3f` "Refactor P1: thư mục theo module, namespace PawBlock, tách Contracts" (nhánh `Refactor-P1-ThuMucNamespace`) |
| Thời gian push | 2026-10-12 12:47 |
| Cập nhật lần cuối | 2026-10-12 12:47 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 12:14 (12/10) | Refactor P1 – thư mục và namespace | 198 script chuyển từ `Script/` sang `Core` / `Shared` / `Features/*` / `Game` (giữ GUID); `GameEvents.cs` tách 7 event + `IGameEvent`, `BoosterType` / `GameState` / `SoundId` sang `Core/Contracts`; 207 file bọc `namespace PawBlock.…`; xóa `Script/`. 104 test xanh, 0 Missing Script, smoke test không lỗi. Không đổi logic |

---

## 12:14 (12/10) – Refactor P1: thư mục và namespace

### Mục tiêu

Giai đoạn P1 của [RefactorPlan.md](../Refactor/RefactorPlan.md): code nằm đúng module (bảng 3.2), mọi type có namespace. Không đổi một dòng logic.

### Thay đổi

**Di chuyển** (bằng `AssetDatabase.MoveAsset` trong Unity, 198 file, GUID giữ nguyên 198 / 198)

| Đích | Nội dung |
| --- | --- |
| `Core/Foundation/` (`Events`, `Save`, `Audio`, `Clock`, `Feedback`, `Util`, `Input`, `Services`, `Editor`, `Tests/Editor`) | EventBus, Save*, AudioManager / Database, TrustedClock*, Haptics, FeelCurves, ProceduralSprites, FrameRateSetup, TweenSetup, TextColor, Singleton, InputGate, RewardedAds, IAPService, SaveDataMenu, FeelCurvesTests |
| `Core/Contracts/Types/` | GameState, SoundId |
| `Shared/Scripts/UI/`, `Shared/Editor/` | 11 script `UI/Common` (trừ SettingsButton), PopupView, NoticePopup, BackgroundFitter; GameFontSetup, UIZoneBuilder, BackgroundPatternBuilder |
| `Features/Gameplay/Scripts/` (`Board`, `Block`, `Booster`, `Input`, `Intro`, `Level`, `Solver`, `Timer`, `Stars`, `Tutorial`, `Feedback`, `Visuals`, `Analytics`, `UI`) | Lõi màn chơi + HUD + 6 popup trong ván; `Booster/Effects` và `Level/Generator` gộp lên 1 cấp (tối đa 4 tầng) |
| `Features/Gameplay/Editor/` (`Level`, `Builders`), `Tests/Editor` | 12 tool level, 6 builder, 7 file test |
| `Features/Economy`, `Shop`, `DailyReward`, `DailyChallenge`, `LuckyWheel`, `Profile`, `Settings` (`Scripts/`, `Editor/`, `Tests/Editor/`) | Theo bảng 3.2 |
| `Game/Scripts/` (+ `Cheats/`), `Game/Editor/` | ScreenFlow, MainMenuNavigator, Menu*, PlayButtonView, GameFlowUI, GameAudioEvents, GameConfig, CheatPanel, DebugCheats; GameConfigSetup, DebugPanelWindow, AudioSetup, MainMenuBuilder, PopupBuilder (P7 tách) |

**Thêm**

| File | Nội dung |
| --- | --- |
| `Core/Contracts/Events/*.cs` | `IGameEvent` (chuyển từ `EventBus.cs`), `LevelLoadedEvent`, `GameStateChangedEvent`, `AllLevelsCompletedEvent`, `ScreenChangedEvent`, `BoosterLockChangedEvent`, `BoosterTutorialCompletedEvent`, `StarsChangedEvent` (mỗi struct 1 file, giữ nguyên code + comment) |
| `Core/Contracts/Types/BoosterType.cs` | Tách enum khỏi `BoosterDatabase.cs` |
| `Docs/Refactor/Dependencies.md` | 89 `using` sai chiều trong 57 file (bảng theo cặp module + nguyên nhân chính), đầu vào cho P2–P4 |

**Sửa**

| File | Nội dung |
| --- | --- |
| 207 file `.cs` | Bọc `namespace PawBlock.<Module>[.<Feature>] { … }` dạng block (thụt 4 dấu cách, dòng `#if` giữ cột 0), thêm `using PawBlock.…` cần thiết; giữ nguyên CRLF / LF của từng file. Không sửa logic, không sửa format |
| `Docs/Refactor/ProjectConventions.md` | Mục 7: một namespace / module, Editor cùng namespace (không `.Editor`), test `.Tests`, không đặt type trùng tên namespace, 4 tầng thư mục, P1 không format |
| `Docs/Refactor/RefactorPlan.md` | Tick P1, kết quả P1, estimate thực tế 1.5 h / dự kiến 9 h, tổng 12 % |

**Xóa**

- `Script/Gameplay/Events/GameEvents.cs` (đã tách), thư mục `Script/` (trống).

### Cách làm

1. Script ánh xạ 199 file theo bảng 3.2, kiểm tra không sót / trùng, không quá 4 tầng.
2. Trước khi di chuyển kiểm tra chỗ lưu tên type dạng chuỗi: UnityEvent (`m_TargetAssemblyTypeName`) trong scene / prefab, `SerializeReference`, `Type.GetType`. Không có chỗ nào trỏ tới type của dự án (chỉ prefab gốc ShikaCat) → thêm namespace không mất tham chiếu.
3. Kiểm tra tên type trùng type thư viện ngoài (UnityEngine, TMPro, DOTween, Spine…) trong namespace đang `using`: không có.
4. `MoveAsset` trong `StartAssetEditing` / `StopAssetEditing`; compile sạch ngay sau khi chuyển.
5. Script bọc namespace + dò `using` theo tên type xuất hiện trong code.

### Lỗi gặp và sửa

- Lần dò `using` đầu bỏ qua nội dung chuỗi → sót `DailyChallengeModel` nằm trong chuỗi nội suy `$"…{DailyChallengeModel.DateOf(day)}…"` (`LevelManager`, CS0103). Dò lại tính cả chuỗi: thêm đúng 1 `using`; 2 `using` thừa do tên nằm trong chuỗi thường (Tooltip của `LevelDefinition`, tên GameObject trong `UIZoneBuilder`) đã bỏ.
- MCP chặn `AssetDatabase.DeleteAsset` → xóa `GameEvents.cs` + `Script/` bằng `git rm`.

### Kiểm tra

- Compile sạch cả `Assembly-CSharp` (212 type) và `Assembly-CSharp-Editor` (45 type), không type nào ở namespace gốc.
- Test EditMode 104 / 104 xanh (tên test giờ có namespace, vd `PawBlock.Features.LuckyWheel.Tests.LuckyWheelTests`).
- Missing Script: `SampleScene` 0 (1 368 MonoBehaviour), 7 prefab dự án 0, 98 prefab gốc ShikaCat thiếu script như trước; 365 ScriptableObject đọc được.
- 37 menu `Tools/PawBlock/…` vẫn đủ.
- Build Android (IL2CPP) 0 lỗi / 0 cảnh báo: kiểm cả đoạn `#if UNITY_ANDROID` / `UNITY_IOS` trong `Haptics.cs` mà Editor không biên dịch.
- Smoke test Play mode qua MCP: Menu → level 39 (10 x 10) → đặt khối → Đóng băng (tuyết, đếm 20) → hết giờ (popup Time's up) → thua (popup Failed) → chơi lại → thắng → Shop → Settings / Profile / Daily Reward / nạp năng lượng → tab Daily (lịch) → Lucky Spin. Console 0 lỗi / cảnh báo. Save máy dev trả về bản trước khi test.
- `git status`: scene, prefab, asset không đổi; chỉ file `.cs` / `.meta` / thư mục.

### Ghi chú

- Cheat `TimeUp` không có tác dụng khi đồng hồ đang chờ chạm đầu tiên (tính năng RoadMap 18, có từ trước, không phải lỗi P1); smoke test gọi `LevelTimer.StartNow()` trước.
- Tài liệu cũ còn ghi đường dẫn `Script/…` (`ProjectStructure.md`, `Features/*`, `TutorialGuide.md`): cập nhật ở P9. RoadMap cũ giữ nguyên (nhật ký).
