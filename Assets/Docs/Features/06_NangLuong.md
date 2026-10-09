# 06 – Năng lượng (lượt chơi ngoài sảnh)

## Tính năng làm gì

- Tối đa **5** năng lượng, hồi **1 / 30 phút** (kể cả khi tắt game – tính theo giờ thật).
- **Thua** 1 level (`GameState.Lost`) hoặc **thoát giữa ván** → −1. Thắng không tốn.
- Hết năng lượng: bấm PLAY / Retry → popup "REFILL LIVES?" (900 coin nạp đầy / xem ads +1 / chờ đồng hồ).
- **Năng lượng vô hạn** trong N phút (quà Shop / Daily): không trừ khi thua.

> "Mạng" (3 tim trong ván) và "năng lượng" (ngoài sảnh) là 2 thứ khác nhau. Mạng xem [02](02_Mang-VongDoiVan.md).

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Energy/EnergyModel` | **Model** C# thuần, nhận `now` từ ngoài (dễ test): `Refresh` cộng bù theo thời gian, `TryConsume`, `Add`, `AddInfinite`, `SecondsToNext` |
| `Gameplay/Energy/EnergyManager` | Trên `Manager`; `maxEnergy` 5, `refillSeconds` 1800 đọc từ `Configs/LivesEnergyConfig.asset`; giá nạp / số năng lượng xem quảng cáo ở `EconomyConfig` (`refillCoinCost`, `refillAdsEnergy`); nghe `Lost` → `ConsumeOne`; tick mỗi giây; lưu `SaveData.energy` |
| `UI/HUD/EnergyHUD` | Tim + số + đồng hồ / FULL / ∞; bấm → popup nạp |
| `UI/Popups/RefillEnergyPopup` | `ShowOptional` (từ nút tim) / `ShowRequired(onReady, onCancel)` (chặn PLAY / Retry) |
| `UI/Popups/GameFlowUI.RequestPlay` | Cổng kiểm tra năng lượng trước khi vào ván |

## Luồng

```text
PLAY ─► ScreenFlow.Play ─► GameFlowUI.RequestPlay(start)
          ├ EnergyManager.CanPlay → start() (vào ván)
          └ hết → RefillEnergyPopup.ShowRequired(start, cancel) → có năng lượng thì tự đóng và start()
Thua ─► LevelManager: Lost ─► EnergyManager.ConsumeOne("lose")   (vô hạn thì bỏ qua)
```

Lưu: `SaveData.energy { energy, refillStart, infiniteUntil }` (unix giây). `energy = -1` = save mới → đầy.

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Số tối đa / thời gian hồi | Inspector `EnergyManager` |
| Giá nạp đầy / số khi xem ads | Inspector `RefillEnergyPopup` |
| Trừ năng lượng khi **bắt đầu** ván thay vì khi thua | Bỏ nghe `Lost` trong `EnergyManager`, gọi `ConsumeOne` trong `ScreenFlow.Play` sau `RequestPlay` |
| Test nhanh | Chuột phải component `EnergyManager` → Debug (Set 0 / Set Full / Skip 1 refill / Infinite 5 min), hoặc Debug Panel |

## Lưu ý

- Đồng hồ máy bị lùi: `EnergyModel` không cộng âm. Muốn chống gian lận đổi giờ cần giờ server – thay `EnergyManager.Now`.
- `onTick` chạy theo `unscaledDeltaTime` nên đồng hồ vẫn chạy khi game tạm dừng.
- Thời gian hồi lấy từ `TrustedClock.UtcNowSeconds` (giờ server khi có mạng, chặn lùi giờ khi offline), không dùng giờ máy trực tiếp: chỉnh giờ máy không hồi năng lượng. Đồng bộ xong thì `RefreshNow`.
