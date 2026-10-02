# 03 – Tối ưu: prefab, ObjectPool, ClueCell C# thuần

| | |
| --- | --- |
| Trạng thái | **Chưa push** |
| Commit | — |
| Thời gian push | — |
| Cập nhật lần cuối | 2026-10-02 17:12 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 16:41 | Sửa SelectionBox | Khung chọn nằm trên khối; sửa lỗi lần kéo đầu |
| 16:53 | ClueCell C# thuần | Object con dựng sẵn trong `Cell.prefab`, không tạo GameObject |
| 17:01 | Prefab + ObjectPool | Khối, ô, chữ khen, chớp đỏ, bong bóng dùng lại thay vì tạo / hủy |
| 17:08 | Sửa lỗi pool | Hết `MissingReferenceException` khi thoát Play mode |

---

## 16:41 – Khung chọn luôn nằm trên khối mèo

### Vấn đề

1. Kéo qua khối đã đặt thì khung chọn bị che (khung order 10, khối 19–22).
2. Lần kéo đầu sau khi vào game, khung chọn không hiện ở khung hình đầu: object để tắt sẵn trong scene → `Awake()` chạy lần đầu ngay trong `Show()` và gọi `Hide()`.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Input/SelectionBox.cs` | `sortingOrder` 10 → 30; `Awake()` chỉ ẩn khi chưa có lượt kéo |
| Sửa | `Docs/ProjectStructure.md` | Bảng sortingOrder |

### Tác dụng

Khung chọn đè lên khối (30 > 22), vẫn dưới bong bóng (40), chớp đỏ (50), chữ khen (60). Lần kéo đầu hiện khung ngay.

---

## 16:53 – ClueCell thành class C# thuần

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Core/ClueCell.cs` | MonoBehaviour → class C# thuần: `Value`, `LockSteps`, `IsHidden`, `Constraint`, `StepLock()`, `Reveal()` |
| Sửa | `Gameplay/Core/Cell.cs` | Sở hữu `ClueCell`; tham chiếu `valueText`, `lockRoot`, `lockText`, `arrow`; `StepLock()`, `RevealClue()`; `SetText("{0}", n)` |
| Sửa | `Prefabs/Cell.prefab` | Thêm `Arrow`, `Lock` (`Shackle`, `Body`, `LockText`), mặc định tắt |
| Sửa | `GridManager.cs`, `PlacedBlock.cs` | Dùng `cell.Clue`, `cell.StepLock()`, `cell.RevealClue()`, `cell.ValueText` |
| Xóa | `GameplayVisuals`: `lockBodySprite`, `lockShackleSprite`, `arrowSprite` | Sprite nằm trong prefab |
| Xóa | `Cell.cs`: đoạn `OnDeselect` đã comment | |

### Tác dụng

Ô số không tạo GameObject, không `GetComponent` / `AddComponent` lúc chạy; chỉnh hình ổ khóa, mũi tên trong prefab; logic ô số tách riêng.

### Kiểm tra

Ổ khóa 2 → 0 sau 2 khối đúng rồi hiện số 7; ô ẩn hiện `?`; mũi tên đúng hướng.

---

## 17:01 – Prefab + ObjectPool

### Thêm

| File | Nội dung |
| --- | --- |
| `Prefabs/Gameplay/PlacedBlock.prefab` | Thân (Sliced, order 20), `Number` (22), `Face` (21), `Ears` (19) |
| `Prefabs/Gameplay/Praise.prefab` | Chữ khen (60) |
| `Prefabs/Gameplay/MistakeFlash.prefab` | `WhiteBlock` 9-slice tô đỏ (50) |
| `Prefabs/Gameplay/DragCountBubble.prefab` | Vòng tròn (40) + `Count` (41); 1 instance để tắt sẵn trong scene |

### Sửa

| File | Nội dung |
| --- | --- |
| `PlacedBlock.cs` | Dùng object con của prefab; `Remove()` → `ReleaseCells()`; reset scale khi tái sử dụng |
| `GridManager.cs` | Pool khối; lưu sẵn số màu; bỏ tự tạo SelectionBox |
| `GridSpawner.cs` | Pool ô (144); tra clue / ô Void bằng Dictionary / HashSet |
| `GameplayFeedback.cs` | Pool chữ khen và vùng chớp đỏ; bỏ `Sprite.Create` |
| `DragCountBubble.cs` | Bỏ tự dựng; chỉ đổi chữ khi số ô đổi; không ẩn trong `Awake` |
| `DragAndDropController.cs` | Bỏ tự tạo bong bóng |
| `CatFaceAnimator.cs` | Lấy SpriteRenderer một lần, reset khung khi tái sử dụng |
| Scene `SampleScene` | Gán prefab cho `GridManager`, `GameplayFeedback`; thêm instance `DragCountBubble` |

### Tác dụng

- Đổi level / chơi lại không Instantiate / Destroy ô và khối nữa.
- Ít rác bộ nhớ (GC) khi chơi lâu.
- Chỉnh hình trong prefab, không sửa code; thiếu tham chiếu thì báo lỗi rõ ràng.

### Kiểm tra

Level 12×12 đặt nửa số khối → chơi lại → 9×9 → quay lại 12×12: ô và khối được tái sử dụng, 0 ô trỏ tới khối cũ, không lỗi.

`new GameObject` chỉ còn trong code Editor dựng prefab và `LivesUI` (3 hình tim, tạo một lần).

---

## 17:08 – Sửa MissingReferenceException khi thoát Play mode

### Nguyên nhân

Thoát Play mode: scene hủy object trước, rồi Editor mới gọi `Clear()` cho mọi `ObjectPool` → `actionOnDestroy` gọi `Destroy` trên object đã hủy.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `GridSpawner.cs`, `GridManager.cs`, `GameplayFeedback.cs` | `actionOnDestroy` kiểm tra null trước khi `Destroy` (cả 4 pool) |

### Kiểm tra

Đặt 3 khối → chuyển sang level 3×1 → thoát Play: 0 lỗi, 0 cảnh báo.

### Ghi chú

- Kịch bản thử ghi đè `PawBlock_CurrentLevel`; đã đặt lại thành 55 (level 56).
- Pool mới: luôn kiểm tra null trong `actionOnDestroy`.
