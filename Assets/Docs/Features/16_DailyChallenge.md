# 16 – Daily Challenge và Level Generator

## Tính năng làm gì

**Daily Challenge** (tab thứ 3 của Menu, thay khung tạm "Progress"; như `HomeUI/.../DailyChallenge` của bản gốc):

- Mỗi ngày 1 level 10 x 10 **sinh theo seed ngày** (`yyyyMMdd`): cùng ngày thì mọi máy cùng level, không cần lưu asset. Gốc cũng làm vậy (`LevelGenerator.GenerateDailyChallenge(seed, 10, 10)`).
- Mở khi tới level 21 (đã thắng level 20, như `SaveManager.DailyUnlockLevel` gốc). Ngày đầu tiên thấy tab sau khi mở khóa là **ngày bắt đầu**: ngày trước đó không tính lỡ.
- Lịch tháng (Chủ nhật đầu tuần): đã thắng = khung cam `daily_khung_1`, đã lỡ = tím đậm `daily_khung_2`, hôm nay = xanh `daily_khung_3`, chưa tới / trước ngày bắt đầu = chữ mờ. Nút tháng trước / sau (từ tháng bắt đầu tới tháng này).
- Nút **Play** xanh: chơi hôm nay. Hôm nay thắng rồi mà còn ngày lỡ: nút **Play vàng** (xem quảng cáo) chơi bù ngày lỡ gần nhất; bấm thẳng 1 ô ngày lỡ cũng vậy. Hết cả hai: nút xám.
- Thắng đủ **7 / 14 / 21 ngày trong 1 tháng** (dương lịch) thì nhận quà mốc đó, mỗi tháng 1 lần. Quà nhập từ `10_Data_Configs/DailyChallengeData.txt` gốc:

  | Mốc | Quà |
  | --- | --- |
  | 7 | 15 phút năng lượng vô hạn + Hint x1 + Freeze x1 |
  | 14 | 30 phút + Hint x2 + Freeze x2 |
  | 21 | 45 phút + Hint x3 + Freeze x3 + Reveal x1 |

  Hộp đủ điều kiện: tia sáng `tia_sang` xoay sau hộp + hộp nhún; bấm nhận → hộp nảy, tick, bong thoại "Claimed!" liệt kê quà. Hộp chưa đủ / đã nhận: bấm để xem quà (bong thoại `dialog.png` + `ArrowDialog 1.png`). Chạm ra ngoài thì bong thoại ẩn nhưng không nuốt chạm (bấm hộp khác là đổi ngay).
- Trong ván Daily: HUD ghi ngày (`OCT 11`), luật như level thường (đồng hồ, sao, booster, năng lượng: thua / bỏ dở trừ 1). Thắng → Continue về **tab Daily**: thanh tiến trình chạy lên, chữ "+1" bay, ô ngày nảy. Thua → Home cũng về tab Daily.
- Tab: chữ "Daily", icon `icon_daily`; chưa mở khóa có ổ khóa `btn_lock2`, hôm nay chưa chơi có chấm đỏ `badge_red`.
- Ván Daily **không** ghi số liệu level (`LevelStatsRecorder`), không tăng "level cao nhất" của Profile, không chạy tutorial mở booster.

**Level Generator** (**Tools → PawBlock → Level Generator**): sinh hàng loạt level bằng cùng code Daily dùng lúc chạy, để làm level mới cho database.

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Level/Generator/LevelGenerator` | **Logic** C# thuần. `Generate(settings, seed)` → `Result { level, partitions, clueTries, unique }`; `Partition` (chia lưới), `Build` (đặt ô số), `SeedForDate`. `LevelGeneratorSettings` + preset Easy / Normal / Hard / Daily |
| `Gameplay/Solver/ShikakuSolver.CountSolutions` | Đếm lời giải, dừng ở `limit` (generator dùng 2 để biết có đúng 1 lời giải) |
| `Gameplay/Daily/DailyChallengeModel` | **Model** C# thuần: `StateOf(ngày, hôm nay)`, `MarkDone`, `DoneInMonth`, `TryGetNearestMissed` (tối đa 400 ngày, không trước ngày bắt đầu), `GiftState` / `MarkGiftClaimed` (khóa theo năm, tháng, mốc), `Progress` (đầy ở 21). Lưu `SaveData.challenge { startDay, doneDays, claimedGifts }` |
| `Gameplay/Daily/DailyChallengeDatabase` (+ `Configs/DailyChallengeDatabase.asset`) | `unlockLevel` 21, `replayMissedWithAd`, `generator` (thông số sinh level), `gifts` (mốc, hộp quà, `DailyRewardItem` dùng chung với điểm danh) |
| `Gameplay/Daily/DailyChallengeManager` | Cùng `LevelManager`: hôm nay theo `TrustedClock`, `IsUnlocked`, `Load(day)` (sinh level → `LevelManager.LoadDaily`), thắng thì `MarkDone` + `PendingCelebrateDay`, `ClaimGift` (trao quà bằng `DailyRewardManager.GrantItem`), sự kiện `Changed`. Debug: `DebugMarkDone`, `DebugReset` |
| `Gameplay/Level/LevelManager` | `LoadDaily(level, day)`, `IsDaily`, `DailyDay`, `Retry` chơi lại đúng level Daily, `NextLevel` bỏ qua khi Daily, `PlayCurrent` (nút PLAY thường) |
| `UI/Menu/ScreenFlow` | `PlayDaily(load)` (kiểm tra năng lượng như PLAY), `ShowMenu` mở tab Daily nếu vừa chơi Daily |
| `UI/Menu/DailyChallengePanel`, `DailyDayCell`, `DailyGiftSlot`, `DailyGiftBubble`, `DailyTabBadge` | Giao diện tab (xem trên) |
| `UI/HUD/LevelHUD.DailyLabel`, `UI/Popups/LosePopup`, `UI/Popups/WinPopup` | Nhãn ngày; Continue ở màn thắng Daily → `ScreenFlow.ShowMenu` |
| `Editor/UI/DailyChallengeBuilder` | **Build Daily Challenge**: tạo database (nhập CSV nếu chưa có quà), thêm manager, thay `ProgressPanel` bằng `DailyPanel`, đổi tab. `MainMenuBuilder` cũng gọi khi dựng lại cả Menu |
| `Editor/Level/LevelGeneratorWindow` | Cửa sổ sinh level |

## Level Generator: cách dùng

1. Chọn **Bộ có sẵn** (Easy 6 x 6 / Normal 7 x 7 / Hard 9 x 9 / Daily 10 x 10) hoặc chỉnh tay: kích thước, số ô mỗi khối (min – max), cạnh dài nhất, số khối 1 ô tối đa, "Đúng 1 lời giải".
2. **Seed đầu** + **Số level** → **Sinh**. Level i dùng seed `seed đầu + i`, nên ghi lại seed là sinh lại được đúng level đó.
3. Danh sách: seed, lưới, số ô số, ✓ (đúng 1 lời giải), điểm độ khó + giây dự kiến (`LevelDifficultyAnalyzer`, cùng thang với cửa sổ Level Difficulty), thời gian sinh. **Sắp theo độ khó** để xếp từ dễ tới khó.
4. Bấm 1 dòng để xem trước (bật / tắt lời giải). **Lưu level chọn / Lưu tất cả** → `Levels/Generated/Gen_<rộng>x<cao>_<seed>.asset`; tick "Thêm vào cuối LevelDatabase" để chơi được ngay. **Mở trong Level Editor** để sửa tay.
5. Muốn có cơ chế (Void, hướng, khóa, "?"): chạy **Mechanic Injector** trên các level đã lưu.

Số đo (20 seed mỗi preset, máy dev): Easy / Normal / Hard / Daily đều 20/20 level có đúng 1 lời giải, trung bình ≤ 1 ms / level (Daily lâu nhất 6 ms). Điểm độ khó trung bình: Easy 30, Normal 30, Hard 34, Daily 36 (database có sẵn: level 11–30 ≈ 30, 61–100 ≈ 37, 151–200 ≈ 46).

## Sửa / mở rộng

| Muốn | Làm |
| --- | --- |
| Đổi level mở khóa / quà / cho chơi bù hay không | `Configs/DailyChallengeDatabase.asset` |
| Daily khó hơn / dễ hơn | `DailyChallengeDatabase.generator` (vd. `maxArea`, kích thước). **Đổi là đổi level của mọi ngày** (kể cả ngày đã qua) |
| Thêm mốc quà | `DailyChallengeModel.Milestones` + `MaxMilestone`, thêm quà vào database, `DailyChallengeBuilder` dựng thêm hộp |
| Thưởng coin riêng cho Daily | `WinPopup.CoinReward` kiểm tra `LevelManager.Instance.IsDaily` |
| Cơ chế trong level Daily | Generator chưa gắn cơ chế lúc chạy (`MechanicInjector` nằm trong Editor). Cần tách phần gắn cơ chế ra code runtime trước |
| Thử nhanh | `DailyChallengeManager.DebugMarkDone(ngày)`, `DebugReset()`; đổi `SaveData.challenge.startDay` để có ngày lỡ |

## Test

- `Editor/Tests/DailyChallengeModelTests` (7 test): trạng thái ngày, ngày bắt đầu chỉ đặt 1 lần, ngày lỡ gần nhất, đếm theo tháng dương lịch, quà theo tháng nhận 1 lần, thanh đầy ở mốc cuối.
- `Editor/Tests/LevelGeneratorTests` (8 test): chia lưới phủ kín / không chồng / đúng giới hạn (4 preset), cùng seed cùng level, level sinh ra đúng 1 lời giải và khớp lời giải lưu sẵn, đếm lời giải nhận ra level 2 lời giải, seed theo ngày.

## Lưu ý

- Ngày lấy từ `TrustedClock.LocalNow` như điểm danh (giờ server nếu có mạng).
- Level Daily là `ScriptableObject` tạo trong bộ nhớ; `DailyChallengeManager` hủy level cũ khi nạp level mới.
- Nút Play vàng dùng `RewardedAds` (bản giả lập trong project).
