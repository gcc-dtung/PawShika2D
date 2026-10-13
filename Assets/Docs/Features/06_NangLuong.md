# 06 – Năng lượng (lượt chơi ngoài sảnh)

> Code ở `Assets/_Project/Features/Economy/Scripts/` (feature Economy, [README](../../_Project/Features/Economy/Docs/README.md)); số liệu ở config của Gameplay.

## Tính năng làm gì

- Tối đa **5** năng lượng, hồi **1 / 30 phút** (kể cả khi tắt game – tính theo giờ thật).
- **Thua** 1 level (`GameState.Lost`) hoặc **thoát giữa ván** → −1. Thắng không tốn.
- Hết năng lượng: bấm PLAY / Retry → popup "REFILL LIVES?" (900 coin nạp đầy / xem ads +1 / chờ đồng hồ).
- **Năng lượng vô hạn** trong N phút (quà Shop / Daily): không trừ khi thua.

> Trong ván không còn mạng (đồng hồ đếm ngược, [02](02_ThoiGian-VongDoiVan.md)); icon tim trên HUD Menu là năng lượng.

## File liên quan

| File | Việc |
| --- | --- |
| `EnergyModel` | **Model** C# thuần, nhận `now` từ ngoài (dễ test): `Refresh` cộng bù theo thời gian, `TryConsume`, `Add`, `AddInfinite`, `SecondsToNext` |
| `EnergyManager` (`IEnergyService`) | Trên `Manager`; `Init(save, clock, maxEnergy, refillSeconds)`: Bootstrap lấy 5 / 1800 từ `LivesEnergyConfig` (Gameplay); nghe `GameStateChangedEvent` Lost → `ConsumeOne`; tick mỗi giây; lưu section `"economy"` (`EconomySave.energy`) |
| `EnergyHUD` (prefab) | Tim + số + đồng hồ / FULL / ∞; bấm → popup nạp (`IMenuPopups.OpenEnergyRefill`) |
| `RefillEnergyPopup` (prefab) | `ShowOptional` (từ nút tim) / `ShowRequired(onReady, onCancel)` (chặn PLAY / Retry); giá / số khi xem ads từ `EconomyConfig` (`refillCoinCost`, `refillAdsEnergy`) truyền qua `Init` |
| `Game/Scripts/GameFlowUI.RequestPlay` | Cổng kiểm tra năng lượng trước khi vào ván |

## Luồng

```text
PLAY ─► ScreenFlow.Play ─► GameFlowUI.RequestPlay(start)
          ├ EnergyManager.CanPlay → start() (vào ván)
          └ hết → RefillEnergyPopup.ShowRequired(start, cancel) → có năng lượng thì tự đóng và start()
Thua ─► LevelManager: Lost ─► EnergyManager.ConsumeOne("lose")   (vô hạn thì bỏ qua)
```

Lưu: `EconomySave.energy` (`EnergySave { energy, refillStart, infiniteUntil }`, unix giây). `energy = -1` = save mới → đầy.

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Số tối đa / thời gian hồi | `Features/Gameplay/Configs/LivesEnergyConfig` (`maxEnergy`, `refillSeconds`) |
| Giá nạp đầy / số khi xem ads | `Features/Gameplay/Configs/EconomyConfig` (`refillCoinCost`, `refillAdsEnergy`) |
| Trừ năng lượng khi **bắt đầu** ván thay vì khi thua | Bỏ nghe `Lost` trong `EnergyManager`, gọi `ConsumeOne` trong `ScreenFlow.Play` sau `RequestPlay` |
| Test nhanh | Chuột phải component `EnergyManager` → Debug (Set 0 / Set Full / Skip 1 refill / Infinite 5 min), hoặc Debug Panel |

## Lưu ý

- Đồng hồ máy bị lùi: `EnergyModel` không cộng âm.
- `onTick` chạy theo `unscaledDeltaTime` nên đồng hồ vẫn chạy khi game tạm dừng.
- Thời gian hồi lấy từ `IClock` (`TrustedClockService`: giờ server khi có mạng, chặn lùi giờ khi offline), không dùng giờ máy trực tiếp: chỉnh giờ máy không hồi năng lượng. Đồng bộ xong (`Synced`) thì `RefreshNow`.
