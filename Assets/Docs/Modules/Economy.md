# Economy

Tiền và lượt chơi ngoài màn: ví coin, ví sao, năng lượng (hồi theo thời gian, vô hạn có hạn), HUD coin / năng lượng và popup nạp năng lượng.

Chi tiết hành vi: [06 Năng lượng](../Features/06_NangLuong.md), [07 Economy – Shop](../Features/07_Economy-Shop.md).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Runtime` (chung); chỉ dùng `Core.Contracts`, `Core.Foundation`, `UI/Framework` (`PawBlock.Shared`) |
| Phát event | `CoinsChangedEvent` (CoinWallet), `StarsBalanceChangedEvent` (StarWallet), `ShopOpenRequestedEvent` (CoinHUD, RefillEnergyPopup thiếu coin) |
| Nghe event | `GameStateChangedEvent` (thua → trừ năng lượng; popup nạp đóng theo ván) |
| Cài interface | `ICurrencyWallet` (CoinWallet, StarWallet), `IEnergyService` (EnergyManager) |
| Dùng interface | `ISaveService`, `IClock`, `IAdsService`, `IGamePause`, `IMenuPopups` |
| Save | Section `"economy"` (`EconomySave`: coin, sao, năng lượng `EnergySave`) |
| Config | Không có asset riêng: số năng lượng / giá nạp nằm trong `LivesEnergyConfig`, `EconomyConfig` (Gameplay), Bootstrap truyền dạng số |

## Class chính

| Class | Vai trò |
| --- | --- |
| `CoinWallet`, `StarWallet` (C# thuần) | Ví: `Balance`, `Add`, `TrySpend`; event `Changed` trong Economy + event contract cho feature khác |
| `EnergyModel` (C# thuần) | Luật hồi năng lượng theo mốc thời gian (tắt game vẫn hồi), vô hạn có hạn |
| `EnergyManager` | Giữ `EnergyModel`, đọc giờ qua `IClock` (chống chỉnh giờ máy), `ConsumeOne`, `AddInfinite` |
| `CoinHUD`, `EnergyHUD` (prefab) | Số coin (bấm → Shop), tim năng lượng + đồng hồ hồi (bấm → popup nạp) |
| `RefillEnergyPopup` (prefab) | Xem quảng cáo +1 hoặc trả coin nạp đầy; `ShowOptional` (từ HUD) / `ShowRequired` (hết năng lượng mà bấm PLAY) |

## Cách dùng

```csharp
// GameBootstrap
var coins = new CoinWallet(save);                       // trong GameServices
energyManager.Init(save, clock, lives.maxEnergy, lives.refillSeconds);
refillEnergyPopup.Init(coins, ads, levelManager, energyManager, economy.refillAdsEnergy, economy.refillCoinCost);

// Feature khác: chỉ biết contract
ICurrencyWallet wallet = coins;
if (wallet.TrySpend(price)) { ... }
EventBus<CoinsChangedEvent>.Subscribe(OnCoinsChanged);
```

## Ghi chú kiến trúc

- Ví là class C# thuần, Bootstrap tạo 1 instance, truyền qua `Init` (không static, không Singleton).
- Hai kênh báo đổi: `event Changed` cho view trong Economy (biết `CoinWallet`), event contract cho feature khác (chỉ biết `ICurrencyWallet`).
- `CoinHUD` / `EnergyHUD` là widget dùng ở nhiều tab (Shop, Home): prefab nằm ở Economy, tab ghép ở `Prefabs/UI/Screens/Home`.
- Test: `Tests/Editor/CurrencyWalletTests` (save trong bộ nhớ).
