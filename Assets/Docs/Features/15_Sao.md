# 15 – Sao theo thời gian (thanh 3 mốc, màn thắng)

## Tính năng làm gì

- Mỗi ván bắt đầu 3 sao. Thời gian còn lại tụt qua mốc thì mất 1 sao (như `SafeArea/TimeAndStar/TimeProgress` của bản gốc, 3 mốc tên `60` / `20` / `0`):
  - còn > 60 % tổng thời gian: 3 sao
  - còn > 20 %: 2 sao
  - còn giây nào: 1 sao
  - hết giờ: 0 sao
- Sao **chỉ giảm**: hết giờ rồi thêm giờ (popup Thêm giờ, cheat) vẫn 0 sao. Đóng băng thì đồng hồ đứng nên sao cũng đứng.
- HUD: thanh thời gian ngay dưới khung LEVEL, 3 sao vàng ở 3 mốc. Tụt qua mốc → sao vàng phồng lên rồi tan (âm `poof_star`), còn lại sao xám.
- Màn thắng: tiêu đề ảnh theo số sao (3 **Perfect**, 2 **Great**, 1 **Nice**, 0 **Well done**), 3 sao vàng đập xuống lần lượt (phóng to 2.6 lần → 1 trong 0.18 s, cách nhau 0.18 s, số của `WinUI` gốc), sao không đạt chỉ còn bóng xám.
- **Tác dụng:** mỗi sao cộng `EconomyConfig.coinsPerStar` (5) coin vào thưởng thắng (Normal 3 sao = 10 + 15 = 25; nút x2 gấp đôi cả phần sao). Ở màn thắng 2 nút hiện trước, mỗi sao đập xuống thì số coin trên nút cộng thêm và nảy lên.
- Lưu sao cao nhất mỗi level: `LevelStatSave.bestStars`.
- **Ví sao + Vòng quay may mắn:** thắng thì cộng số sao của ván vào ví (`StarWallet`, `SaveData.stars`). Nút "Lucky" trên Home mở vòng quay: mỗi lần quay 10 sao, 8 ô quà theo trọng số (coin 50 / 100 / 200 / 500, Hint, Freeze, Reveal, 15 phút năng lượng vô hạn). Quà trao ngay lúc bấm, vòng quay chỉ là hình. Cấu hình: `Configs/LuckyWheelDatabase.asset`; dựng: **Build Lucky Wheel**; logic: `LuckyWheelModel` (test `LuckyWheelTests`).

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Stars/StarRating` | **Logic** C# thuần: `FromRatio(tỉ lệ còn lại, ngưỡng 2 sao, ngưỡng 3 sao)`, `MarkerRatio(sao)`, `Title(số sao)`; `StarTracker` giữ số sao (chỉ giảm) |
| `Gameplay/Stars/LevelStars` | Cùng `LevelTimer`: nghe `onTimeChanged` + lưới mới, phát `StarsChangedEvent { Stars, Lost }` |
| `Gameplay/Config/LivesEnergyConfig` | `threeStarRatio` 0.6, `twoStarRatio` 0.2 |
| `UI/HUD/TimeStarBar` | `LevelFrame/TimeBar`: nền `tin_trnh2`, phần còn lại `tin_trnh1` (Sliced, co chiều rộng nên 2 đầu luôn bo tròn), mốc sao đặt lúc chạy theo config. Đóng băng → phần còn lại ngả xanh băng |
| `UI/Popups/WinPopup` | `titleImages[4]` (theo `WinTitle`), `stars[3]`, nhóm "Sao đập xuống" |
| `Gameplay/Audio/GameAudioEvents` | `StarLost` (`poof_star`) khi mất sao (không phát lúc tụt về 0 vì đã có âm hết giờ) |
| `Gameplay/Analytics/LevelStatsRecorder` | Ghi `bestStars` khi thắng |
| `Editor/UI/StarsBuilder` | **Build Stars**: thêm `LevelStars`, dựng `TimeBar`, gọi lại Build Win Screen |
| `Editor/UI/WinScreenBuilder` | Tiêu đề (4 ảnh chồng nhau) + 3 bóng `star_5` + 3 sao `star_2`, vị trí theo `WinUI` gốc |

Ảnh: `04_UI_Graphics/Popups_Dialogs/well_done_1 / nice_1 / great_1 / perfect_1.png` (cắt từ atlas `EndGame` theo `m_Rect` trong `ExportedProject/Assets/Sprite`), `03_Gameplay_Blocks_Grid/Grid_Cells/tin_trnh1 / 2.png` (= `tiến_trình1 / 2` gốc), `Icons/star.png`, `star2.png`, `star_2.png`, `star_5.png`.

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Đổi mốc sao | `LivesEnergyConfig.threeStarRatio` / `twoStarRatio`; mốc trên HUD tự dời theo |
| Đổi chữ tiêu đề theo số sao | `StarRating.Title` (+ test `StarRatingTests.TieuDeTheoSoSao`) |
| Đổi coin mỗi sao | `EconomyConfig.coinsPerStar` (0 = tắt); công thức `EconomyConfig.WinCoinReward(difficulty, stars)` |
| Hiện sao đã đạt ở chỗ khác (bản đồ level, Profile) | Đọc `SaveData.levelStats[i].bestStars` (chỉ có ý nghĩa khi `wins > 0`) |
| Đổi giá quay / ô quà / tỉ lệ | `LuckyWheelDatabase.asset` (đổi số ô thì chạy lại **Build Lucky Wheel** để vẽ lại vòng) |
| Dựng lại | **Tools → PawBlock → Builders → Build Stars** (dựng lại cả màn thắng) |

## Test

`Editor/Tests/StarRatingTests` (19 test): ngưỡng đúng mốc là đã mất sao, tracker chỉ giảm và tụt 2 → 0 trong 1 lần, thêm giờ không lấy lại sao, tổng thời gian 0 thì bỏ qua, coin thưởng theo độ khó + sao.
