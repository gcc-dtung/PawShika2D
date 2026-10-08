# 05 – Booster: Hint, Khiên, Reveal

## Tính năng làm gì

| Booster | Mở ở level | Tác dụng |
| --- | --- | --- |
| **Hint** | 5 | Tự đặt khối nhỏ nhất của lời giải (khớp các khối đã đặt) |
| **Khiên** (thay Freeze Time bản gốc) | 7 | Chặn lần mất mạng kế tiếp trong level |
| **Reveal** | 9 | Chọn 1 ô số → đặt mọi khối cùng số (lần lượt cách 0.12 s) |

- Lượt **chỉ bị trừ khi có tác dụng** (Hint không tìm được chỗ thì không trừ).
- Hết lượt → popup mua bằng coin.
- Booster khóa cho tới khi qua **tutorial mở khóa**: lần đầu vào level ≥ `levelUnlock`, màn tối, chỉ nút đó sáng + bàn tay; bấm → mở khóa, lần dùng đầu miễn phí. Khiên có thêm bước bắt người chơi kéo sai để thấy khiên chặn.

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Booster/BoosterDatabase` (+ asset `Configs/BoosterDatabase.asset`) | `BoosterType` (Hint 0, Shield 1, Reveal 2) + `BoosterData`: tên, mô tả, `levelUnlock`, `valueStart`, `valueOneBuy`, `priceOneBuy`, icon |
| `Gameplay/Booster/BoosterInventory` | Static: số lượt (lưu `SaveData.boosters` theo **tên** loại), `Changed` |
| `Gameplay/Booster/BoosterManager` | `Press(type)`, `Buy(type)`, logic từng booster, chế độ Reveal |
| `Gameplay/Booster/BoosterTutorial` | Tutorial mở khóa, bước "kéo sai thử" của Khiên |
| `UI/HUD/BoosterButton` | 1 nút: icon, số lượt, "+", ổ khóa "LV n", quầng sáng |
| `UI/HUD/RevealBanner`, `ShieldIndicator`, `BoosterTutorialView` | Banner chọn số, lớp băng, lớp tối tutorial |
| `UI/Popups/BuyBoosterPopup` | Mua khi hết lượt |
| `Editor/UI/BoosterBuilder` | Dựng 3 nút, banner, khung băng |

## Luồng `Press(type)`

```text
Press(type)
 ├ đang tutorial mở khóa: chỉ nhận đúng nút → BoosterTutorial.Complete, lần này miễn phí
 ├ chưa mở khóa / lưới đang khóa → bỏ qua
 ├ Reveal đang bật → tắt Reveal
 ├ hết lượt → onBuyRequested → GameFlowUI → BuyBoosterPopup
 └ switch(type): UseHint / UseShield / SetRevealMode(true)
                  └ có tác dụng → Consume → BoosterInventory.TryConsume + onBoosterUsed
```

## Event

| Event | Ai nghe |
| --- | --- |
| `BoosterManager.onBuyRequested(type)` | `GameFlowUI` |
| `BoosterManager.onRevealModeChanged(bool)` | `BoosterButton`, `RevealBanner` |
| `BoosterManager.onBoosterUsed(type)` | `BoosterButton`, `GameAudioEvents` |
| `BoosterInventory.Changed(type, count)` | `BoosterButton` |
| `LivesManager.onShieldChanged / onShieldBlocked` | `BoosterButton`, `ShieldIndicator`, `BoosterTutorial` |

## Chỉnh số liệu (không cần code)

Sửa `BoosterDatabase.asset`: `levelUnlock`, `valueStart` (tặng khi mở), `valueOneBuy`, `priceOneBuy`, `detail` (lời nhắc tutorial).

## Thêm booster mới

Xem [HowTo_MoRong.md → Thêm booster](HowTo_MoRong.md#thêm-booster). Tóm tắt: thêm giá trị **cuối** `BoosterType` → thêm `BoosterData` vào asset → `case` trong `BoosterManager.Press` + hàm `UseX` (gọi `Consume` khi có tác dụng) → thêm nút trong `BoosterBuilder` → món trong Shop / Daily nếu có.

## Lưu ý

- Save lưu booster theo **tên** (`"Hint"`), không theo số → đổi thứ tự enum không hỏng save, nhưng **đổi tên** enum thì mất lượt cũ.
- Booster muốn đặt khối thì gọi `GridManager.PlaceSolved(rect)` với rect lấy từ `BoardModel.TrySolveRemaining`, và kiểm tra `Board.Check(rect).IsReady` (ô còn khóa không đặt được).
- Booster chạy nhiều bước theo thời gian phải khóa lưới: thêm điều kiện vào `BoosterManager.IsBusy` (đã nằm trong `GridManager.IsInputLocked`).
