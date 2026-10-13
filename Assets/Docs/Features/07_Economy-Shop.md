# 07 – Coin, Shop, IAP, quảng cáo có thưởng

> Code: ví ở `Features/Economy`, Shop ở `Features/Shop`, service ads / IAP ở `Core/Foundation/Services` (đều trong `Assets/_Project/`). README: [Economy](../../_Project/Features/Economy/Docs/README.md), [Shop](../../_Project/Features/Shop/Docs/README.md).

## Tính năng làm gì

- **Coin**: nhận khi thắng (theo độ khó + sao, x2 bằng ads), Daily, Lucky Wheel, Shop; tiêu cho booster, thêm giờ, nạp năng lượng.
- **Shop** (tab bên trái Main Menu): Starter Pack, Remove Ads, No Ads Bundle, Special Bundle, lưới gói Coin; giá VND.
- **IAP** (mua bằng tiền thật): **chưa có SDK** → luôn thất bại, hiện "Purchase Failed!" như bản gốc.
- **Quảng cáo có thưởng**: **giả lập**, luôn thành công sau `EconomyConfig.adsSimulatedDuration` (0.5 s).

> Dự án portfolio: không có tài khoản store / ads thật. Hai service đã tách sau interface để thay SDK mà không sửa nơi gọi.

## File liên quan

| File | Việc |
| --- | --- |
| `Features/Economy/Scripts/CoinWallet` (`ICurrencyWallet`) | Class C# thuần: `Balance`, `Add`, `TrySpend`, `CanAfford`, event `Changed` + `CoinsChangedEvent`; lưu section `"economy"` |
| `Features/Shop/Scripts/ShopDatabase` (+ `Configs/ShopDatabase.asset`) | `ShopProduct`: `productId`, tên, `ShopSection`, giá, hình, coin / hint / shield (= Đóng băng) / reveal / `infiniteMinutes` / `removeAds`, tem giảm giá |
| `Features/Shop/Scripts/ShopService` | `Buy(product)` → `IIAPService.Purchase` → thành công thì `Grant` → event `Purchased`; `NoAds` (section `"shop"`) |
| `Core/Foundation/Services/IIAPService`, `FakeIAPService` | **Chỗ gắn SDK thanh toán** |
| `Core/Foundation/Services/IAdsService`, `FakeAdsService` | **Chỗ gắn SDK quảng cáo**: `Show(placement, ok => …)` |
| `Features/Shop/Scripts/ShopCardView`, `Features/Economy/Scripts/CoinHUD` | Thẻ gói (prefab `ShopCoinCard`, `ShopBuyButton`), số coin |
| `Game/Prefabs/Menu/ShopPanel` (+ `Game/Scripts/Menu/ShopPanel`) | Tab Shop: ghép thẻ Shop + HUD coin / năng lượng, tự `Init` cho thẻ |

## Luồng mua

```text
ShopCardView (bấm giá) ─► ShopService.Buy(product)
                             └► IIAPService.Purchase(productId, ok)
                                  ├ ok    → ShopService.Grant: ICurrencyWallet.Add, IBoosterInventory.Add,
                                  │          IEnergyService.AddInfinite, noAds → Purchased
                                  └ fail  → IIAPService.PurchaseFailed → GameFlowUI: NoticePopup "Purchase Failed!"
```

## Gắn SDK thật

| Service | Làm | Giữ nguyên |
| --- | --- | --- |
| IAP | Viết class cài `IIAPService` (Unity IAP / Google Play Billing): gọi `onDone` + `PurchaseSucceeded` / `PurchaseFailed` trong callback | Chữ ký `Purchase(productId, onDone)` và 2 event |
| Quảng cáo | Viết class cài `IAdsService` (MAX / AdMob): gọi `onDone(true)` khi được thưởng | `Show(placement, onDone)` |

Rồi thay `new FakeIAPService()` / `new FakeAdsService(...)` trong `GameBootstrap`. Nơi gọi ads hiện có: `MoreTimePopup` (+30 s), `WinPopup` (x2 coin), `RefillEnergyPopup` (+1 năng lượng), `DailyRewardManager` (nhận bù ngày lỡ), `DailyChallengePanel` (chơi bù ngày lỡ).

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Đổi giá / nội dung gói | Sửa `Features/Shop/Configs/ShopDatabase.asset` (thẻ đọc lúc chạy) |
| Thêm gói vào mục có sẵn | Thêm `ShopProduct` vào asset → mở prefab `Game/Prefabs/Menu/ShopPanel`, nhân bản 1 thẻ (`ShopCoinCard` / dòng gói), đặt `productId` |
| Thêm loại quà mới trong gói (vd. booster mới) | Thêm field vào `ShopProduct`, trao trong `ShopService.Grant`, hiển thị trong `ShopCardView` |
| Ẩn quảng cáo khi đã mua No Ads | Kiểm tra `ShopService.NoAds` (interstitial chưa có; rewarded vẫn cho xem vì người chơi chủ động) |

Chi tiết: [HowTo_MoRong.md → Thêm gói Shop](HowTo_MoRong.md#thêm-gói-shop).
