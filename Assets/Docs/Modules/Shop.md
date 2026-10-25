# Shop

Bán gói coin / booster / năng lượng vô hạn / tắt quảng cáo qua IAP. Hiện chưa có SDK thanh toán: `FakeIAPService` luôn thất bại (popup "Purchase Failed!"), như bản gốc.

Chi tiết hành vi: [07 Economy – Shop](../Features/07_Economy-Shop.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Runtime` (chung); chỉ dùng `Core.Contracts`, `Core.Foundation`, `UI/Framework` (`PawBlock.Shared`) |
| Phát / nghe event | Không (tab Shop mở bằng `ShopOpenRequestedEvent` do Game xử lý) |
| Dùng interface | `ICurrencyWallet` (coin), `IBoosterInventory`, `IEnergyService` (vô hạn), `IIAPService`, `ISaveService` |
| Save | Section `"shop"` (`ShopSave`: đã mua tắt quảng cáo `noAds`) |
| Config | `Data/Config/ShopDatabase` (`ShopProduct`: section, giá, coin, booster `hint` / `shield` (= Freeze) / `reveal`, vô hạn, `removeAds`, nền thẻ / nền ô vật phẩm, `saleTag`) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `ShopService` (C# thuần) | `Buy(product)`: thanh toán qua `IIAPService`, thành công thì trao quà qua contract |
| `ShopDatabase` | Danh sách gói, đọc lúc chạy (sửa asset không cần dựng lại UI) |
| `ShopCardView` | 1 thẻ gói: tên, giá (`289.000` + icon tiền trong prefab), hình, nền theo gói, ô booster (lưới, món = 0 ẩn) + ô vàng (`coinItem`), `itemsBackgrounds[]`; bấm giá (hoặc cả thẻ) → `ShopService.Buy` |

Prefab (`Prefabs/UI/Widgets/Shop/`): `ShopSpecialCard`, `ShopRemoveAdsCard`, `ShopBundleCard`, `ShopCoinCard` (ảnh rip trong `Source/Art/UI/Home/HalloweenRip/Shop/`, gitignore; ảnh hồn ma ôm coin của Premium Pack tự gen bằng Gemini: `Source/Art/UI/Home/Halloween/GhostCoins.png`). Tab Shop (`Prefabs/UI/Screens/Home/ShopPanel`) ghép thẻ Shop với `CoinHUD` / `EnergyHUD` của Economy nên nằm ở Game.

## Cách dùng

```csharp
// GameBootstrap: cần kho booster + năng lượng (component trong scene) nên tạo sau manager
services.Shop = new ShopService(save, services.Coins, boosterManager, energyManager, iap);
shopPanel.Init(services.Shop, services.Coins, energyManager, gameFlowUI); // tab tự Init từng ShopCardView
```

Gắn SDK thật: viết class cài `IIAPService` (Runtime/Core/Foundation/Services) rồi thay `FakeIAPService` trong `GameBootstrap`; Shop không phải sửa.

## Ghi chú kiến trúc

- Shop không biết Economy / Gameplay: chỉ dùng `ICurrencyWallet`, `IBoosterInventory`, `IEnergyService`.
- Thêm gói: chỉ thêm dòng trong `ShopDatabase` (ShopPanel spawn thẻ theo section). Section mới: thêm cuối enum `ShopSection` + 1 slot trong `ShopPanel`.
