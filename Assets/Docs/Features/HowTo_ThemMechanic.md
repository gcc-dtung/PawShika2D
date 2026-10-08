# Hướng dẫn: thêm một cơ chế (mechanic) mới

Đọc trước: [03 – Cơ chế](03_CoChe-Mechanic.md). Mọi chỗ cần sửa trong code đều có chú thích **`// [MECHANIC]`** – tìm chữ này (Ctrl + Shift + F) để không sót.

---

## 0. Xác định loại cơ chế

Trả lời 3 câu hỏi trước khi code – mỗi câu "có" thêm một nhóm việc:

| Câu hỏi | Ví dụ | Có thì thêm |
| --- | --- | --- |
| A. Cơ chế gắn vào **ô số** hay vào **ô lưới**? | Hướng, Khóa, "?" = ô số. Void = ô lưới | Ô lưới: xem [mục 10](#10--cơ-chế-theo-ô-lưới-như-void) |
| B. Nó có **luật hình dạng / vị trí** cho khối không? | Hướng: khối phải ngang / dọc / vuông | Bước 3 (BoardModel) + Bước 4 (Solver) – **bắt buộc khớp nhau** |
| C. Nó có **trạng thái đổi trong lúc chơi** không? | Khóa: mỗi khối đặt đúng −1 bước | Bước 2 + Bước 3b + mô phỏng trong Validator / Analyzer |
| D. Nó có **giấu thông tin** với người chơi không? | "?" giấu số, Khóa giấu số + hướng | Bước 8b (kiểm tra "không cần đoán") |

---

## Ví dụ xuyên suốt: ô "Mép" (Edge)

> **Luật:** ô số có dấu Mép thì khối chứa nó **phải chạm mép ngoài của lưới**. Ô còn khóa thì dấu Mép bị che như hướng.

Loại: ô số (A), có luật vị trí (B), không đổi trạng thái (C = không), bị che khi khóa (D = có, dùng lại cơ chế "chưa biết hướng").

### Checklist

| # | Bước | File | Bắt buộc |
| --- | --- | --- | --- |
| 1 | Dữ liệu | `Gameplay/Level/LevelDefinition.cs` | ✔ |
| 2 | Trạng thái lúc chơi | `Gameplay/Core/ClueCell.cs` (+ `CellModel` nếu đổi trạng thái) | ✔ |
| 3 | Luật đặt khối | `Gameplay/Core/BoardModel.cs` | nếu B / C |
| 4 | Solver | `Gameplay/Solver/ShikakuSolver.cs` | nếu B |
| 5 | Hiển thị trên ô | `Gameplay/Core/CellView.cs` + `Prefabs/Cell.prefab` | ✔ |
| 6 | Khối mèo, booster | `PlacedBlockView`, `BoosterManager`, `BoosterTutorial` | nếu che số / chặn chọn |
| 7 | Popup giới thiệu | `Gameplay/Tutorial/MechanicIntro.cs`, `Editor/UI/TutorialBuilder.cs` | nên có |
| 8 | Level Editor + Validator | `LevelEditorWindow`, `LevelValidator` | ✔ |
| 9 | Gắn tự động | `MechanicInjectorSettings`, `MechanicInjector` (+ asset) | nếu muốn tự xuất hiện |
| 9b | Độ khó | `LevelDifficultyAnalyzer`, `LevelDifficultySettings` | nên có |
| 11 | Docs + test | README, 03, ProjectStructure, RoadMap | ✔ |

---

## Bước 1 – Dữ liệu

`LevelDefinition.cs`, struct `ClueMechanic`: thêm field, **giá trị mặc định = tắt** (asset cũ tự có `false`, không cần sửa 200+ level).

```csharp
public struct ClueMechanic
{
    public ClueConstraint constraint;
    public int lockSteps;
    public bool hidden;
    public bool edge;   // khối phải chạm mép lưới
}
```

> Cần nhiều giá trị (vd. "chạm mép trái / phải / trên / dưới") → dùng `enum` có `None = 0`, không dùng nhiều `bool`.
> Không đổi tên / xóa field cũ: Unity serialize theo tên, đổi tên là mất dữ liệu ở mọi level.

## Bước 2 – Trạng thái lúc chơi (`ClueCell`)

```csharp
public bool MustTouchEdge => Data.mechanic.edge;
```

Cơ chế **đổi trạng thái** (như Khóa): giữ biến trong `ClueCell` (`LockSteps`), thêm hàm đổi (`StepLock()`), thêm hàm bọc trong `CellModel` gọi `Changed?.Invoke()` để `CellView` vẽ lại.

## Bước 3 – Luật đặt khối (`BoardModel.Check`)

Thêm kết quả vào `PlacementResult` (thao tác bị hủy, **không** trừ mạng):

```csharp
NotOnEdge   // ô Mép nhưng khối không chạm mép lưới
```

Thêm kiểm tra **trước** bước kiểm tra số ô (bước 5), sau `WrongShape`:

```csharp
// 4b. Ô Mép
if (clue.MustTouchEdge && !ShikakuSolver.TouchesBorder(rect, Geometry.Width, Geometry.Height))
    return new PlacementCheck(PlacementResult.NotOnEdge, rect, cells, clueCell);
```

> **Quy tắc vàng:** viết luật thành **1 hàm static dùng chung** (ở đây `ShikakuSolver.TouchesBorder`, giống `MatchesConstraint`) rồi gọi ở cả `BoardModel` và solver. Hai nơi lệch luật = game hiện mèo nhưng thả ra bị trừ mạng oan.

**3b. Cơ chế phản ứng khi đặt khối** (như Khóa −1, "?" lộ số): sửa `BoardModel.Place` (chỗ có `// [MECHANIC]`). Ô vừa đổi trạng thái thì thêm vào list / event để view và âm thanh biết (xem `onClueUnlocked`).

## Bước 4 – Solver (`ShikakuSolver`)

1. Class `Clue`: thêm `public bool edge;`
2. `BuildState`: điền field. Ô còn khóa che dấu Mép → coi như người chơi chưa biết (dùng lại `hideConstraint`):

```csharp
edge = !hideConstraint && c.mechanic.edge,
```

3. Hàm luật dùng chung + lọc ứng viên trong `BuildCandidates` (ngay sau dòng `MatchesConstraint`, tính được `x0, y0`):

```csharp
public static bool TouchesBorder(RectInt r, int width, int height) =>
    r.xMin == 0 || r.yMin == 0 || r.xMax == width || r.yMax == height;
```

```csharp
if (c.edge && !TouchesBorder(new RectInt(x0, y0, w, h), s.width, s.height)) continue;
```

Cơ chế **giấu số** thì không lọc ở đây mà thêm clue vào `unknownValue` (xem Bước 8b).

## Bước 5 – Hiển thị trên ô (`CellView` + `Cell.prefab`)

1. Mở `Prefabs/Cell.prefab`, thêm object con (vd. `EdgeMark`, SpriteRenderer, sortingOrder 5–6, **tắt sẵn**). Sprite đặt trong `GameplayVisuals` nếu dùng chung.
2. `CellView`:

```csharp
[SerializeField] private GameObject edgeMark;

// Awake: để hiệu ứng xuất hiện làm mờ / hiện dần cả dấu mới
if (edgeMark != null) _contentSprites.AddRange(edgeMark.GetComponentsInChildren<SpriteRenderer>(true));

// Refresh:
if (edgeMark != null) edgeMark.SetActive(hasClue && !locked && clue.MustTouchEdge);
```

3. Gán field `edgeMark` trong prefab.

> View chỉ đọc `ClueCell`, không tự tính luật.

## Bước 6 – Khối mèo và booster

Chỉ cần nếu cơ chế **che số** hoặc **chặn chọn ô**:

| Chỗ | Khi nào sửa |
| --- | --- |
| `PlacedBlockView.SetNumber` | Số trên mèo xem trước (ô "?" hiện "?") |
| `BoosterManager.HandleCellTap` | Reveal không được chọn ô này |
| `BoosterTutorial.FindWrongMove` | Tutorial Khiên chỉ mẫu trên ô người chơi đọc được |

Hint / Reveal dùng `Board.TrySolveRemaining` + `Board.Check(rect).IsReady` nên **tự tôn trọng luật mới** nếu Bước 3–4 đúng.

## Bước 7 – Popup giới thiệu

`MechanicIntro.cs`:

```csharp
public enum Kind { Constraint, Lock, Hidden, Edge }   // thêm CUỐI (save lưu theo tên)

[SerializeField] private Sprite edgeSprite;

// NewMechanics:
bool edge = false;
edge |= c.mechanic.edge;                         // trong vòng foreach
if (edge && !Seen(Kind.Edge)) result.Add(Kind.Edge);

// ShowNext:
case Kind.Edge:
    popup.Show("Edge Number", "This block must touch the border of the board.", edgeSprite, () => Done(kind));
    break;
```

> `ShowNext` hiện dùng `default:` cho Hidden – đổi thành `case Kind.Hidden:` để cơ chế mới không rơi vào nhánh sai.

`TutorialBuilder`: thêm hằng đường dẫn ảnh, `EnsureSprite`, `PopupBuilder.Assign(mechanicIntro, "edgeSprite", …)`. Chạy **Build Tutorial**.

## Bước 8 – Level Editor và Validator

**`LevelEditorWindow`**

- `DrawSelectedClue`: thêm `EditorGUILayout.Toggle("Mép", c.mechanic.edge)` và gán lại `c.mechanic.edge = edge;` trong khối `EndChangeCheck`.
- Vẽ lưới (chỗ `ConstraintMark`): thêm ký hiệu (vd. "▣") để nhìn ra ô Mép.
- Cơ chế giấu thông tin → thêm vào `HasHiddenInfo()`.

**`LevelValidator.CheckData`** – luật dữ liệu:

```csharp
if (m.edge && m.hidden) report.warnings.Add($"{name}: Mép + \"?\" khó đọc");   // ví dụ quy tắc kết hợp
```

**8b. Kiểm tra "không cần đoán"** – chỉ khi cơ chế giấu thông tin:

- `Validate`: thêm điều kiện vào `hasHiddenInfo`.
- `CheckNoGuessing`: thêm clue vào `unknownValue` / `unknownConstraint` đúng lúc người chơi chưa thấy.
- Cơ chế **đổi trạng thái** (như Khóa): mô phỏng y hệt trong vòng lặp cuối `CheckNoGuessing` (chỗ `lockSteps[i]--`) **và** trong `LevelDifficultyAnalyzer.Analyze`. Có 3 nơi mô phỏng cùng 1 luật: `BoardModel.Place`, `LevelValidator`, `LevelDifficultyAnalyzer` – sửa đủ cả 3.

## Bước 9 – Gắn tự động (Mechanic Injector)

1. `MechanicInjectorSettings.cs`: thêm `Edge = 4` vào **cuối** `MechanicKind` (thứ tự enum = thứ tự gắn; cơ chế đổi hình lưới như Void phải đứng đầu). Thêm `MechanicRule` mặc định vào list.
2. **Mở `Levels/MechanicInjectorSettings.asset` và thêm rule Edge bằng tay** – asset đã tạo không tự nhận giá trị mặc định mới.
3. `MechanicInjector`:
   - `TryKind`: thêm `case MechanicKind.Edge: part = TryEdge(ctx); break;` – lưu ý `default:` đang là Hidden, đổi thành `case MechanicKind.Hidden:` rồi để `default: part = null;`.
   - Viết `TryEdge(ctx)` theo mẫu `TryConstraint`: lấy `SolutionRects`, chọn clue mà khối lời giải chạm mép **và** có cách đặt khác không chạm mép (dấu Mép thực sự loại bớt lựa chọn), gắn, `LevelValidator.Validate`, sai thì `Snapshot.Restore`.
   - `Suits`: level gốc nào hợp để giới thiệu (mặc định `true`).
   - Tùy chọn: thêm vào `hardOrder` (level Hard) / `fallbacks` (thay khi gắn hỏng).
4. Mechanic Injector → Xem trước → Áp dụng. Hoặc Reorder Levels.

**9b. Độ khó** – `LevelDifficultyAnalyzer`: thêm đếm vào `Report` (vd. `edges`), cộng vào `Score` với trọng số mới trong `LevelDifficultySettings` (vd. `wEdge`), thêm cột CSV trong `LevelDifficultyWindow` nếu cần. Ghi công thức vào `DifficultyFormula.md`.

## 10 – Cơ chế theo ô lưới (như Void)

Ví dụ "ô Đá": ô không chơi được nhưng vẫn hiện trên lưới.

| Chỗ | Việc |
| --- | --- |
| `LevelDefinition.CellType` | Thêm giá trị **cuối** (`Rock = 2`) |
| `GridSpawner` (chỗ đọc `cellOverrides`) | Sinh ô kiểu mới (Void hiện không sinh `CellView`) |
| `CellModel` / `BoardModel` | Ô có chặn khối không: `Check` (`BlockedCell`), `IsAreaFree`, `IsFull` (ô không cần phủ thì bỏ qua) |
| `DragSelection` | Khung kéo có được đi qua không |
| `ShikakuSolver.BuildState` | Đánh dấu `blocked` |
| `LevelValidator.CheckData`, `MechanicInjector.VoidMask / Playable` | Đếm ô chơi được (tổng các số = số ô chơi được) |
| `LevelEditorWindow` | Công cụ vẽ ô kiểu mới |

## 11 – Kiểm tra và ghi lại

1. Unity biên dịch không lỗi (Console).
2. Level Editor → tạo level nhỏ có cơ chế → **Kiểm tra** (Validator hợp lệ) → **Giải** → **▶ Chơi thử**.
3. Trong ván: luật chặn đúng (thả sai luật không trừ mạng), đặt đúng được, hiển thị trên ô, popup giới thiệu lần đầu, Hint / Reveal vẫn đúng, tutorial Khiên không chỉ vào ô sai.
4. **Validate Levels** cho cả database.
5. Cập nhật docs: bảng "Ô đặc biệt" trong `../README.md`, bảng trong [03](03_CoChe-Mechanic.md), `ProjectStructure.md` (nếu thêm file), RoadMap.

---

## Khi số cơ chế tăng lên (gợi ý refactor)

Hiện mỗi cơ chế nằm rải ở ~12 file (đánh dấu `[MECHANIC]`). Với 4–6 cơ chế thì cách này rõ ràng, dễ đọc. Khi vượt ~6 cơ chế, nên gom mỗi cơ chế về **1 class**:

```csharp
public interface IClueMechanic
{
    bool IsOn(ClueData clue);                                    // level có dùng không (MechanicIntro, đếm độ khó)
    PlacementResult Check(ClueCell clue, RectInt rect, BoardModel board); // luật đặt khối (Ready = qua)
    bool AllowsCandidate(ClueData clue, RectInt rect, int width, int height, bool hiddenByLock); // solver
    void OnBlockPlaced(ClueCell clue);                            // đổi trạng thái
    bool HidesValue(ClueCell clue);                               // Validator / Analyzer
}
// MechanicRegistry.All: danh sách các IClueMechanic; BoardModel / Solver / Validator / Analyzer chỉ lặp qua danh sách.
```

Khi đó thêm cơ chế = 1 class mới + view + popup + TryX của Injector. Chưa làm ngay vì solver là vòng lặp nóng (gọi interface trong `BuildCandidates` cần đo hiệu năng trên level 15x15) và 4 cơ chế hiện tại chưa cần.
