# 07 – Coin, Shop, IAP, quảng cáo có thưởng

> Code: ví ở `Features/Economy`, Shop ở `Features/Shop`, service ads / IAP ở `Runtime/Core/Foundation/Services` (đều trong `Assets/_Project/`). README: [Economy](../Modules/Economy.md), [Shop](../Modules/Shop.md).

## Tính năng làm gì

- **Coin**: nhận khi thắng (theo độ khó + sao, x2 bằng ads), Daily, Lucky Wheel, Shop; tiêu cho booster, thêm giờ, nạp năng lượng.
- **Shop** (tab bên trái Main Menu, giao diện theo Foodie Sizzle Halloween): mái sọc "SHOP"; **Special Pack** (Limited Pack trên thẻ bí ngô / hồn ma; theme chính đã là ma nên không có mục Halloween riêng; không có đồng hồ), thẻ **No Ads**, ruy băng **BUNDLES** (No Ads Bundle + 5 gói Big / Great / Super / Premium / Legendary, mỗi gói 1 màu nền), ruy băng **COINS** (lưới 3x2); giá là số `289.000` + icon tờ tiền `Source/Art/UI/Common/Icons/icon_money` (font VN-Lilita tự tạo không có glyph ₫ đẹp như bản gốc); sao lấp lánh quanh icon chính mỗi thẻ là Particle System qua `UIParticle` (Coffee) – object `Sparkles`, material `Source/Art/UI/Shop/Effects/FX_Sparkle_UI`. Thẻ Limited Pack dựng từ Spine gốc `packshopHLW` (animation `idle` 4 s): bí ngô nhún theo số liệu Spine (`UIFloat` co giãn ±2.4 % / ±2.6 %, nghiêng ±2.4°, 1.33 s); `PumpkinGlow` (4 vầng sáng, 60 khung 15 fps), `GhostLeft` / `GhostRight` (hồn ma vươn lên – mờ đi – mọc lại, 120 khung 30 fps), `Bats/doi1..3` (dơi bay qua, khung + vị trí) đều là `UISpriteSequence`, sheet ở `HalloweenRip/Shop/PackFX/` (chỉ local; `.txt` cạnh mỗi sheet: ô, số khung, tâm từng khung theo toạ độ skeleton).
- **IAP** (mua bằng tiền thật): **chưa có SDK** → luôn thất bại, hiện "Purchase Failed!" như bản gốc.
- **Quảng cáo có thưởng**: **giả lập**, luôn thành công sau `EconomyConfig.adsSimulatedDuration` (0.5 s).

> Dự án portfolio: không có tài khoản store / ads thật. Hai service đã tách sau interface để thay SDK mà không sửa nơi gọi.

## File liên quan

| File | Việc |
| --- | --- |
| `Runtime/Economy/CoinWallet` (`ICurrencyWallet`) | Class C# thuần: `Balance`, `Add`, `TrySpend`, `CanAfford`, event `Changed` + `CoinsChangedEvent`; lưu section `"economy"` |
| `Runtime/Shop/ShopDatabase` (+ `Data/Config/ShopDatabase.asset`) | `ShopProduct`: `productId`, tên, `ShopSection` (Starter, RemoveAds, NoAdsBundle, Bundle, Coin, **Event**, **Limited**), giá, hình, nền thẻ `background`, nền ô vật phẩm `itemsBackground`, coin / hint / shield (= Đóng băng) / reveal / `infiniteMinutes` / `removeAds`, nhãn góc `saleTag`; `PriceText` = `289.000` |
| `Runtime/Shop/ShopService` | `Buy(product)` → `IIAPService.Purchase` → thành công thì `Grant` → event `Purchased`; `NoAds` (section `"shop"`) |
| `Runtime/Core/Foundation/Services/IIAPService`, `FakeIAPService` | **Chỗ gắn SDK thanh toán** |
| `Runtime/Core/Foundation/Services/IAdsService`, `FakeAdsService` | **Chỗ gắn SDK quảng cáo**: `Show(placement, ok => …)` |
| `Runtime/Shop/ShopCardView` | Thẻ gói, prefab trong `Prefabs/UI/Widgets/Shop/`: `ShopSpecialCard` (khung bí ngô + ruy băng SPECIAL PACK, cả thẻ là nút mua; dùng cho section Limited và Event); thẻ gói chia 2 ô như bản gốc: **ô booster** (`BoosterPanel`, GridLayout 2 cột – thêm booster chỉ cần thêm con) và **ô vàng** (`CoinPanel`: đồng coin + số), cả 2 ô đổi nền theo `itemsBackground`, `ShopRemoveAdsCard`, `ShopBundleCard`, `ShopCoinCard`. Ô vật phẩm ẩn khi = 0, ô vàng (`coinItem`) ẩn khi gói không có coin |
| `Prefabs/UI/Screens/Home/ShopPanel` (+ `Runtime/UI/Home/ShopPanel`) | Tab Shop: header mái sọc, spawn thẻ theo `slots` (section → prefab, chỗ chèn); thanh coin / năng lượng giữ trong prefab nhưng tắt (video gốc không có) |

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
| Đổi giá / nội dung gói | Sửa `Data/Config/ShopDatabase.asset` (thẻ đọc lúc chạy) |
| Thêm gói vào mục có sẵn | Chỉ thêm `ShopProduct` vào `ShopDatabase.asset` (đúng section, đặt `background` / `itemsBackground` / `icon` nếu muốn khác prefab): `ShopPanel` tự spawn thẻ, không sửa prefab |
| Thêm loại quà mới trong gói (vd. booster mới) | Thêm field vào `ShopProduct`, trao trong `ShopService.Grant`, hiển thị trong `ShopCardView` |
| Ẩn quảng cáo khi đã mua No Ads | Kiểm tra `ShopService.NoAds` (interstitial chưa có; rewarded vẫn cho xem vì người chơi chủ động) |

Chi tiết: [HowTo_MoRong.md → Thêm gói Shop](HowTo_MoRong.md#thêm-gói-shop).
