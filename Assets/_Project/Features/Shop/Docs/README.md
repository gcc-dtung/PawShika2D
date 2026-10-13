# Shop

Bán gói coin / booster / năng lượng vô hạn / tắt quảng cáo qua IAP. Hiện chưa có SDK thanh toán: `FakeIAPService` luôn thất bại (popup "Purchase Failed!"), như bản gốc.

Chi tiết hành vi: [07 Economy – Shop](../../../../Docs/Features/07_Economy-Shop.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Meta`; dùng `Core.Contracts`, `Core.Foundation` |
| Phát / nghe event | Không (tab Shop mở bằng `ShopOpenRequestedEvent` do Game xử lý) |
| Dùng interface | `ICurrencyWallet` (coin), `IBoosterInventory`, `IEnergyService` (vô hạn), `IIAPService`, `ISaveService` |
| Save | Section `"shop"` (`ShopSave`: đã mua tắt quảng cáo `noAds`) |
| Config | `Configs/ShopDatabase` (`ShopProduct`: section, giá, coin, booster `hint` / `shield` (= Freeze) / `reveal`, vô hạn, `removeAds`) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `ShopService` (C# thuần) | `Buy(product)`: thanh toán qua `IIAPService`, thành công thì trao quà qua contract |
| `ShopDatabase` | Danh sách gói, đọc lúc chạy (sửa asset không cần dựng lại UI) |
| `ShopCardView` | 1 thẻ / dòng gói: tên, giá, hình, coin, ô vật phẩm 2x2; bấm giá → `ShopService.Buy` |

Prefab: `ShopCoinCard`, `ShopBuyButton`. Tab Shop (`Game/Prefabs/Menu/ShopPanel`) ghép thẻ Shop với `CoinHUD` / `EnergyHUD` của Economy nên nằm ở Game.

## Cách dùng

```csharp
// GameBootstrap: cần kho booster + năng lượng (component trong scene) nên tạo sau manager
services.Shop = new ShopService(save, services.Coins, boosterManager, energyManager, iap);
shopPanel.Init(services.Shop, services.Coins, energyManager, gameFlowUI); // tab tự Init từng ShopCardView
```

Gắn SDK thật: viết class cài `IIAPService` (Core/Foundation/Services) rồi thay `FakeIAPService` trong `GameBootstrap`; Shop không phải sửa.

## Ghi chú kiến trúc

- Shop không biết Economy / Gameplay: chỉ dùng `ICurrencyWallet`, `IBoosterInventory`, `IEnergyService`.
- Thêm gói: thêm dòng trong `ShopDatabase` + 1 thẻ trong prefab `ShopPanel` (xem [HowTo_MoRong](../../../../Docs/Features/HowTo_MoRong.md)).
