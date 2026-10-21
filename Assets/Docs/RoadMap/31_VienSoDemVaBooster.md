# 31 – Viền tối cho số đếm khi kéo và số booster

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | – |
| Thời gian push | – |
| Cập nhật lần cuối | 2026-10-21 14:25 |

## 13:55 – Số trắng ở bong bóng đếm và booster có viền tối

**Nguyên nhân**: chữ số ở `DragCountBubble` và `BoosterButton` dùng material mặc định `Fredoka-Bold SDF Material` (không viền). Material này cũng là material của số trên khối đã đặt (`PlacedBlock` → `Number`), nên thêm viền vào đó thì số trên khối cũng có viền.

**Sửa**: dùng material có sẵn `Shared/Fonts/Fredoka-Bold SDF - Cell Number.mat` (viền + bóng `#120A2E`, giống số trong ô lưới), không đụng material mặc định.
- `Gameplay/Prefabs/DragCountBubble.prefab` → `Count`.
- `Gameplay/Prefabs/BoosterButton.prefab` → `Count/Text` (số lượng) và `Lock/Level` ("LV n" khi khóa). 3 booster trong `GameplayHUD` theo prefab.
- Scene `Main`: instance `DragCountBubble` có override font / material cũ → bỏ override để theo prefab.

**Giữ nguyên**: `PlacedBlock` → `Number` vẫn material mặc định (không viền).

**Kiểm tra**: Play level 36, bật tạm bong bóng đếm "21" (`Captures/count_outline_after.png`): số trên bong bóng và số booster có viền tối. Console không lỗi.

## 14:10 – Bong bóng đếm không ra ngoài bảng

**Hiện tượng**: kéo vùng chọn ở cột / hàng sát mép, bong bóng (lệch lên trái so với ngón tay) bị đẩy ra ngoài bảng, mất một nửa.

**Sửa**
- `DragCountBubble.Show(..., Rect clampArea = default)`: có vùng thì vị trí đích của bong bóng bị kẹp sao cho **cả bong bóng** (nửa kích thước nền khi hiện đủ cỡ) nằm trong vùng; ngón tay ra ngoài thì bong bóng chạm mép và dừng. Không truyền vùng thì như cũ.
- `DragAndDropController.BoardArea()`: vùng bảng world = `Width × Spacing` × `Height × Spacing`, tâm ở gốc (theo `GridGeometry`), truyền vào `Show`.

**Kiểm tra**: Play level 36 (lưới 8×8, vùng ±4.8), gọi `Show` với ngón tay ngoài mép phải (x = 8.5): bong bóng dừng sát mép phải bảng (`Captures/drag_count_clamp.png`). Console không lỗi. Chưa thử kéo tay thật (giả lập `StartDrag` qua code không bắt đầu được vùng chọn).

## 14:25 – Bong bóng đếm được lên cao hơn bảng 1 ô

**Lý do**: kẹp đúng bằng bảng thì kéo ở hàng trên cùng, bong bóng không lên khỏi bảng được nên nằm đè lên vùng đang chọn.

**Sửa** `DragAndDropController.BoardArea()`: mép trên vùng kẹp nới thêm 1 ô (`Spacing`). Chiều ngang và mép dưới giữ đúng bằng bảng (không tràn màn hình).

**Kiểm tra**: Play level 36, gọi `Show` với ngón tay ngoài góc trên phải (9, 9): vùng (-4.8, -4.8) cao 10.8; bong bóng dừng trên hàng trên cùng 1 ô, sát mép phải bảng, không đè thanh sao (`Captures/drag_count_clamp_top.png`). Console không lỗi.
