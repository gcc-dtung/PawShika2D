# 08 – Hiệu ứng lưới xuất hiện

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `4002899` "Hiệu ứng lưới xuất hiện: ô nảy lần lượt ngẫu nhiên, số hiện dần" (nhánh `Day-2`) |
| Thời gian push | 2026-10-03 17:15 |
| Cập nhật lần cuối | 2026-10-03 17:16 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 16:44 (03/10) | Lưới xuất hiện dần như video tham khảo | Vào level: các ô nảy ra lần lượt theo thứ tự ngẫu nhiên, sau đó số / khóa hiện mờ dần; khóa thao tác tới khi xong |

---

## 16:44 – Lưới xuất hiện dần

### Yêu cầu

Lúc vào level, các khối xuất hiện dần dần như video `ref xuất hiện.mp4`.

### Phân tích video (cắt khung 0.07 s, đoạn level 3)

| Thời điểm | Diễn biến |
| --- | --- |
| 0 → 0.55 s | 16 ô nền nảy ra lần lượt theo thứ tự ngẫu nhiên (không theo hàng), nhịp đều; mỗi ô phóng từ chấm nhỏ lên kích thước thật trong ~0.2 s, hơi vượt rồi về |
| ~0.7 → 1.1 s | Các số hiện mờ dần lần lượt, cũng ngẫu nhiên |
| ~1.2 s | Chữ "Level 3" và thanh booster hiện mờ dần (chưa làm) |

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Core/CellView.cs` | `PlayIntro(cellDelay, cellDuration, contentDelay, contentDuration)`: ô nền scale 0 → 1 (OutBack 1.6) + hiện dần; nội dung (số, chữ + hình khóa, ô hướng) alpha 0 → alpha gốc. Sequence DOTween giữ trong field; `StopIntro` (gọi khi `Unbind` về pool / tắt object) trả ô về trạng thái bình thường |
| Sửa | `Gameplay/Core/GridSpawner.cs` | Sau `Spawn` gọi `PlayIntro`: xáo thứ tự ô và thứ tự số riêng; nhịp ô `cellInterval` 0.035 s (cả lưới ≤ `cellSpreadMax` 0.6 s – lưới 12x12 không bị lâu), số bắt đầu sau ô cuối `contentGap` 0.12 s, rải trong `contentSpread` 0.35 s; `IsIntroPlaying`. Tham số chỉnh trong Inspector, tắt được bằng `playIntro` |
| Sửa | `Gameplay/Core/GridManager.cs` | `IsInputLocked` thêm `gridSpawner.IsIntroPlaying` (kéo và booster bị khóa khi lưới đang xuất hiện) |

### Kiểm tra (Play mode, level 31 – 7x7, chụp camera mỗi 0.1 s)

| t | Kết quả |
| --- | --- |
| 0.1 → 0.7 s | Ô nảy lần lượt rải rác khắp lưới, ô mới xuất hiện từ chấm nhỏ |
| 0.8 → 1.2 s | Số hiện mờ dần, không theo thứ tự |
| 0 → 1.3 s | `IsInputLocked` = true; từ 1.4 s = false |

### Ghi chú

- Chữ LEVEL và thanh booster trong video hiện sau lưới; hiện HUD vẫn bật ngay. Có thể thêm fade cho `TopPanel` / `DownPanel` nếu cần.
- Lúc mở game, level được nạp ngầm sau Menu nên hiệu ứng chạy khi chưa thấy; bấm PLAY nạp lại level nên hiệu ứng chạy lại, nhìn thấy được.
