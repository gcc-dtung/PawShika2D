# 16 – Nút màn thắng xám sau khi bấm

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `ff104c0` "check break" (nhánh `Day-6`, `main`) |
| Thời gian push | 2026-10-09 15:31 |
| Cập nhật lần cuối | 2026-10-09 14:20 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 13:55 (09/10) | Nút Continue / x2 Coin xám sau khi bấm | Bấm nhận thưởng thì 2 nút chuyển xám (sprite + chữ viền xám) như nút Mua booster khi thiếu coin |
| 14:20 (09/10) | Rung mạnh hơn + chẩn đoán rung trên máy | Rung Light / Medium dài hơn (máy không chỉnh được cường độ rung lâu hơn nữa); Cheat Panel hiện trạng thái motor rung, số lần gọi, lỗi, nút rung kiểu cũ |

---

## 13:55 (09/10) – Nút Continue / x2 Coin xám sau khi bấm

Yêu cầu: các nút Continue khi thắng, ấn xong thì chuyển xám như các nút booster khi không đủ coin mua.

**Sửa**

- `UI/Popups/WinPopup.cs`: intro không tắt `interactable` nữa (nếu tắt, nút sẽ xám ngay lúc bật lên) mà chặn bấm bằng cờ `_ready` (bật khi 2 nút đã hiện xong). Bấm Continue / x2 Coin → `SetButtons(false)` → `UIGrayscaleWhenDisabled` làm xám cả 2 nút; x2 Coin xem quảng cáo thất bại thì 2 nút sáng lại.
- `Editor/UI/WinScreenBuilder.cs`: gọi `PopupBuilder.GrayWhenDisabled` cho `NextButton`, `DoubleButton` (Build lại màn thắng vẫn giữ).
- Scene: thêm `UIGrayscaleWhenDisabled` (material `UI_Grayscale`, chữ `Outline Gray`) vào 2 nút, `disabledColor` = trắng.

### Kiểm tra

- Unity biên dịch 0 lỗi.
- Play mode (lưu tạm `save.json`, xong trả lại): thắng level → trong intro bấm bị bỏ qua, nút vẫn màu; intro xong 2 nút màu bình thường (`Default UI Material`); bấm Continue → cả 2 nút `UI_Grayscale`, chữ `Outline Gray`, coin bay rồi sang level kế; lần thắng sau 2 nút lại màu.

## 14:20 (09/10) – Rung mạnh hơn + chẩn đoán rung trên máy

Người dùng báo cài APK nhưng không thấy rung.

### Đã kiểm tra

- APK 11:59 (`launcher-release.apk`): manifest có `android.permission.VIBRATE`; `Assembly-CSharp.dll` có `Haptics`, `GameHapticEvents`, gọi `Vibrator` / `createOneShot` → code rung đã vào bản build.
- Không có máy nối `adb` nên chưa đọc được logcat.
- Nghi nhất: Light 15 ms / Medium 25 ms quá ngắn, motor rung kiểu cũ (máy không chỉnh được cường độ) gần như không kịp quay → không cảm thấy.

**Sửa**

- `Haptics`: Light 25 ms (không chỉnh cường độ: 30 ms), Medium 40 (50), Heavy 70 (90), nhịp thắng / thua dài hơn. Thêm `Status` (API, có motor rung, chỉnh được cường độ), `PlayCount`, `LastError`, `PlayLegacy()` (`Handheld.Vibrate`).
- Cheat Panel → Khác → Thử rung: hiện `Status`, số lần đã gọi, lỗi; nút "Rung dài (kiểu cũ)"; nút bật / tắt cài đặt Rung.

### Kiểm tra

- Unity biên dịch 0 lỗi; biên dịch thử cho target Android qua. Chưa test trên máy thật (cần build lại APK).
