# 07 – Coin, Shop, IAP, quảng cáo có thưởng

## Tính năng làm gì

- **Coin**: nhận khi thắng (+10, x2 bằng ads), Daily, Shop; tiêu cho booster, hồi mạng, nạp năng lượng.
- **Shop** (tab bên trái Main Menu): Starter Pack, Remove Ads, No Ads Bundle, Special Bundle, lưới gói Coin; giá VND.
- **IAP** (mua bằng tiền thật): **chưa có SDK** → luôn thất bại, hiện "Purchase Failed!" như bản gốc.
- **Quảng cáo có thưởng**: **giả lập**, luôn thành công sau 0.5 s.

> Dự án portfolio: không có tài khoản store / ads thật. Hai service đã tách sẵn để thay SDK mà không sửa nơi gọi.

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Economy/CoinWallet` | Static: `Balance`, `Add`, `TrySpend`, `CanAfford`, event `Changed`; lưu `SaveData.coins` |
| `Gameplay/Economy/ShopDatabase` (+ asset) | `ShopProduct`: `productId`, tên, `ShopSection`, giá, hình, coin / hint / shield / reveal / `infiniteMinutes` / `removeAds`, tem giảm giá |
| `Gameplay/Economy/ShopService` | `Buy(product)` → `IAPService.Purchase` → thành công thì `Grant` → event `Purchased` |
| `Gameplay/Economy/IAPService` | Static, **chỗ gắn SDK thanh toán** |
| `Gameplay/Economy/RewardedAds` | Trên `Manager`, **chỗ gắn SDK quảng cáo**: `RewardedAds.Show(placement, ok => …)` |
| `UI/Menu/ShopCardView`, `CoinHUD` | Thẻ gói, số coin |
| `Editor/UI/ShopBuilder` | Dựng tab Shop |

## Luồng mua

```text
ShopCardView (bấm giá) ─► ShopService.Buy(product)
                             └► IAPService.Purchase(productId, ok)
                                  ├ ok    → ShopService.Grant: CoinWallet.Add, BoosterInventory.Add,
                                  │          EnergyManager.AddInfinite, SaveData.noAds → Purchased
                                  └ fail  → IAPService.PurchaseFailed → GameFlowUI.ShowNotice("Purchase Failed!")
```

## Gắn SDK thật

| Service | Thay | Giữ nguyên |
| --- | --- | --- |
| `IAPService` | Thân `Purchase` (gọi Unity IAP / Google Play Billing), gọi `Succeed` / `Fail` trong callback; `IsInitialized` | Chữ ký `Purchase(productId, onDone)` và 2 event |
| `RewardedAds` | Thân `ShowAd` (MAX / AdMob), gọi `onDone(true)` khi được thưởng | `Show(placement, onDone)`, `IsShowing` |

Nơi gọi ads hiện có: `ReviveLivesPopup` (+1 mạng), `WinPopup` ("win_x2"), `RefillEnergyPopup` (+1 năng lượng), `DailyRewardManager` (nhận bù ngày lỡ).

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Đổi giá / nội dung gói | Sửa `Configs/ShopDatabase.asset` |
| Thêm gói vào mục có sẵn | Thêm `ShopProduct` vào asset → chạy **Build Shop** |
| Thêm loại quà mới trong gói (vd. booster mới) | Thêm field vào `ShopProduct`, trao trong `ShopService.Grant`, hiển thị trong `ShopCardView` |
| Ẩn quảng cáo khi đã mua No Ads | Kiểm tra `ShopService.NoAds` (interstitial chưa có; rewarded vẫn cho xem vì người chơi chủ động) |

Chi tiết: [HowTo_MoRong.md → Thêm gói Shop](HowTo_MoRong.md#thêm-gói-shop).
