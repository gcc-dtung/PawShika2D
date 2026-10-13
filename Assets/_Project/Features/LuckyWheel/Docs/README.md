# LuckyWheel

Vòng quay may mắn mở từ nút "Lucky" trên Home: mỗi lần quay tiêu sao (ví sao), chọn ô theo trọng số, trao quà ngay rồi mới diễn vòng quay.

Chi tiết hành vi: [15 Sao](../../../../Docs/Features/15_Sao.md) (mục ví sao + vòng quay).

## Phụ thuộc

| | |
| --- | --- |
| Assembly | `PawBlock.Meta`; dùng `Core.Contracts`, `Core.Foundation`, `Shared` |
| Nghe event | `StarsBalanceChangedEvent` (số sao trên nút / popup) |
| Dùng interface | `ICurrencyWallet` (sao), `IRewardGranter`, `IRewardIcons` |
| Save | Không (sao nằm ở Economy, quà trao qua `IRewardGranter`) |
| Config | `Configs/LuckyWheelDatabase` (giá quay, các ô: quà + trọng số) |

## Class chính

| Class | Vai trò |
| --- | --- |
| `LuckyWheelModel` (C# thuần) | Chọn ô theo trọng số, góc dừng dưới kim |
| `LuckyWheelPopup` (prefab) | SPIN: trừ sao, chọn ô, trao quà, diễn vòng quay chậm dần + tick |
| `LuckyWheelButton` | Nút trên Home: số sao, lắc + chấm đỏ khi đủ sao quay. Field `popup` gán trên instance (Tooltip "Gán từ ngoài") |

## Cách dùng

```csharp
// GameBootstrap
luckyWheelPopup.Init(services.Stars, dailyRewardManager, dailyRewardManager);
// HomePanel.Init(...) gọi luckyWheelButton.Init(stars)
```

## Ghi chú kiến trúc

- Quà trao **trước** khi diễn vòng quay: tắt app giữa lúc quay không mất quà.
- Số ô trên vòng vẽ sẵn trong prefab: đổi số ô trong `LuckyWheelDatabase` thì sửa prefab `LuckyWheelPopup` cho khớp.
- Test: `Tests/Editor/LuckyWheelTests`.
