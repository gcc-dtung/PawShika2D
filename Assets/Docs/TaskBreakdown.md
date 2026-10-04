# Task Breakdown – Thay thời gian bằng mạng

Chia 6 giai đoạn. Giai đoạn 1–3 cho ra một vòng chơi hoàn chỉnh chơi thử được; 4–6 hoàn thiện sản phẩm.

Ký hiệu: `[x]` xong · `[ ]` chưa làm · `[~]` tạm thời / cần làm lại

---

## Quyết định đã duyệt

| # | Quyết định | Chốt |
| --- | --- | --- |
| 1 | Khi nào trừ mạng | Chỉ khi khối đúng số ô nhưng sai vị trí (lưới không còn lời giải) |
| 2 | Số mạng mỗi level | 3 cho mọi level |
| 3 | Cách tính sao | Bỏ sao |
| 4 | Undo | Không có Undo |
| 5 | Booster Freeze Time | Đổi thành Khiên: chặn 1 lần sai kế tiếp |
| 6 | Hết mạng | Popup Hồi mạng (ads +1, coin +3) rồi mới sang Lose |
| 7 | Năng lượng ngoài sảnh | Giữ (phương án B), đổi icon khác tim |
| 8 | Daily Challenge | Thay bằng tab **Progress**: lên tới level nào thì nhận quà gì (game gốc sinh lưới theo ngày bằng thuật toán, không có) |

## Câu hỏi mở

- Ô khóa / ô ẩn / mũi tên xuất hiện từ level nào, mật độ bao nhiêu? (dữ liệu ShikaCat không có)
- Màn Win, Lose, Home của game gốc trông thế nào? (chưa có ảnh tham chiếu)
- Hết 205 level thì làm gì: quay vòng, màn "Coming soon", hay bộ sinh level?
- ~~Năng lượng: Setting → HOME giữa level rồi PLAY lại miễn phí~~ – đã chốt: bỏ dở level hỏi lại (QUIT LEVEL) và mất 1 năng lượng như thua.
- Giá nạp đầy năng lượng (đang 900 coin) và hồi mạng (900 coin) chưa có số gốc.

---

## Giai đoạn 1 — Mạng trong màn ✅

**Kết quả:** đặt sai trong Play mode thì mất tim, hết tim thì không thao tác được.

- [x] `LivesManager`: 3 mạng, `LoseLife`, `AddLives`, event `onLivesChanged` / `onLifeLost` / `onOutOfLives`
- [x] `LivesUI`: hàng tim `heart_1` / `heart_2` trên HUD Canvas, hiệu ứng nảy + lắc khi mất tim
- [x] `ShikakuSolver`: nước sai = đặt xong lưới không còn lời giải
- [x] `GridManager.IsInputLocked`: khóa thao tác khi thắng hoặc hết mạng
- [x] Gắn vào `SampleScene`, chạy thử: tim vỡ 3 → 2, vùng sai chớp đỏ

## Giai đoạn 2 — Gameplay một level ✅

**Kết quả:** chơi được một level đầy đủ như ảnh Level 26 của bản gốc, chỉ thiếu popup.

- [x] `LevelManager` + `LevelDatabase`: nạp level, lưu tiến độ, thắng → level kế, hết mạng → chơi lại
- [x] Camera tự zoom theo kích thước lưới; `BackgroundFitter` giữ nền phủ kín
- [x] `ShikaCatLevelImporter`: nhập 205 level từ JSON (lật trục y, kiểm tra tổng diện tích)
- [x] Ô khóa: giảm 1 sau mỗi khối đặt đúng, về 0 thì lộ số, còn khóa thì không chọn được
- [x] Ô ẩn `?`: bỏ kiểm tra số ô, solver quyết định; đặt đúng thì lộ số
- [x] Mũi tên ngang / dọc: chặn khối sai hướng, không trừ mạng
- [x] `DragCountBubble`: bong bóng số ô bám ngón tay khi kéo
- [x] `PlacedBlock`: khối mèo (thân 9-slice, tai, số, mặt hoạt hình), hiệu ứng bật khi xuất hiện
- [x] `GameplayVisualsSetup`: cắt atlas Spine `ingame_cat_face_03` thành 10 bộ mặt mèo
- [x] `GameplayFeedback`: chữ khen khi đặt đúng; chớp đỏ, rung camera, rung máy khi sai
- [x] `LevelHUD`: chữ `LEVEL n` (nút chơi lại đã bỏ, thay bằng nút cài đặt)
- [x] Font: Fredoka Bold cho toàn game (tạo lại font asset, viền chữ ở Menu); LiberationSans làm fallback
- [~] Sprite mũi tên: ảnh debug tạm
- [ ] Gắn ô khóa / ẩn / mũi tên vào level thật (chờ trả lời câu hỏi mở)
- [x] Âm thanh: AudioManager + AudioDatabase; đặt khối (`block_true`), mở khóa (`unlock_obs2`), sai, thắng / thua, booster, popup, nút, tiếng mèo, nhạc nền; cài đặt Nhạc / Âm / Rung (chưa có popup cài đặt)

## Giai đoạn 3 — Thay các thành phần dính tới thời gian (gần xong)

**Kết quả:** vòng chơi hoàn chỉnh vào level → chơi → thắng/thua → level kế.

| Gốc | Thay bằng |
| --- | --- |
| Khung LEVEL + đồng hồ 03:00 | Khung LEVEL + hàng tim (đã có, cần làm đẹp) |
| Thanh sao tụt theo giờ | Bỏ |
| `MoreTimePopup` | `ReviveLivesPopup` (+1 ads, +3 coin), cùng layout |
| `WinUI` sao theo thời gian | `WinUI` không sao, thưởng coin |
| Banner HARD LEVEL | Giữ, thêm số mạng nếu cần |
| `hardLayer` viền đỏ | Bỏ (thử viền đỏ khi còn 1 mạng: khó chơi) |
| `TimingManager`, `EstimateDuration` | Bỏ |

- [x] `GameState` trong `LevelManager`: Playing / Paused → Won / OutOfLives → (hồi mạng) Playing hoặc Lost
- [x] `ReviveLivesPopup`: ads +1 mạng, 900 coin +3 mạng, nút X = thua; hồi mạng chơi tiếp lưới đang dở
- [x] `WinPopup`: không sao, +10 coin, x2 bằng ads, Next → level kế
- [x] `LosePopup`: nút Retry
- [x] ~~`ConfirmReplayPopup` cho nút Replay~~ – đã bỏ cùng nút Replay
- [x] `SettingsPopup`: Nhạc / Âm / Rung, HOME; tạm dừng ván khi mở
- [x] ~~Viền đỏ nhấp nháy khi còn 1 mạng~~ – đã làm rồi bỏ: che lưới, khó chơi
- [x] `CoinWallet` (save JSON) + `RewardedAds` giả lập (chờ SDK ở Giai đoạn 6)
- [~] Giao diện popup dựng bằng menu `Build Popups` từ sprite gốc; chưa có hiệu ứng pháo hoa / mèo trên Win
- [x] Độ khó Hard / Very Hard (`LevelDefinition.difficulty`): nút PLAY, khung HUD, hiệu ứng vào màn
- [ ] Làm đẹp HUD: khung LEVEL dùng sprite gốc thay cho ô màu

## Giai đoạn 4 — Booster ✅

| Booster | Mở ở level | Giá (coin) | Hành vi |
| --- | --- | --- | --- |
| Hint | 5 | 1200 / 3 lượt | Tự đặt 1 khối đúng (dùng `ShikakuSolver.TrySolve`) |
| Khiên (thay Freeze Time) | 7 | 1500 / 3 lượt | Chặn lần sai kế tiếp trong level; khung băng `ice_effect_2` phủ hàng tim (chưa có Spine runtime) |
| Reveal | 9 | 1000 / 1 lượt | Chọn 1 số, giải mọi khối cùng số |

- [x] `BoosterManager` + 3 nút `BoosterButton` trong `Canvas_HUD/DownPanel/Booster` (menu `Build Boosters`)
- [x] Số lượt (`BoosterInventory`, save JSON, tặng `valueStart`), dấu `+` khi hết → `BuyBoosterPopup`, ổ khóa "LV n" khi chưa tới level
- [x] Hint và Reveal lấy lời giải khớp với các khối đã đặt (`BoardModel.TrySolveRemaining`); Reveal có banner chọn số + nút X
- [x] Khiên: quầng sáng trên nút + khung băng trên tim, `LivesManager` bỏ qua 1 lần `LoseLife`; đổi level / chơi lại thì mất
- [x] Sửa `BoosterDatabase.asset`: trỏ script mới, Freeze Time → Shield, icon, bỏ `activeSeconds`
- [x] Lượt chỉ bị trừ khi booster có tác dụng (Hint đặt được khối, Reveal có khối để giải)

## Giai đoạn 5 — Năng lượng ngoài sảnh (phương án B) ✅

Chưa có màn Home (Giai đoạn 6) nên năng lượng hiện ở HUD trong màn, và chặn ngay trong màn.

- [x] Thua 1 level (state Lost) tốn 1 năng lượng, tối đa 5 (`EnergyModel` + `EnergyManager`)
- [x] Hồi 1 năng lượng sau 30 phút, tính theo mốc thời gian nên tắt game vẫn hồi
- [x] Icon tim như bản gốc (`icon_hear_2`, vô hạn `icon_hear_infinity2`; trước đó là cá ShikaCat) + HUD góc trên phải: số, đồng hồ, FULL, ∞
- [x] `RefillEnergyPopup` (thay `RefillLivesPopup`): ads +1, 900 coin nạp đầy; mở từ HUD (có X) hoặc bắt buộc khi hết năng lượng mà Retry / mở game (tự đóng khi có năng lượng)
- [x] API "năng lượng vô hạn" (`EnergyManager.AddInfinite`) – Daily Challenge (Giai đoạn 6) sẽ gọi
- [~] Chưa chống chỉnh giờ máy (đồng hồ lùi thì chỉ đặt lại mốc, không cộng thêm)

## Giai đoạn 6 — Meta, kinh tế, SDK

- [x] Main Menu (`Canvas_Menu`): thanh tab dưới **Shop | Home | Progress**, panel trượt ngang (`MainMenuNavigator`, menu `Build Main Menu`)
- [x] Home: nút PLAY Level n (`ScreenFlow`: ẩn menu, bật HUD, nạp lại level; hết năng lượng → popup nạp có X), năng lượng + coin trên cùng (bấm coin → tab Shop)
- [x] Popup chuyển sang `Canvas_Popup` riêng (dùng chung Menu và màn chơi); popup Thua có nút HOME
- [ ] Setting, avatar, nút Starter / No Ads trên Home
- [ ] Về Home sau khi thắng (bản gốc: `gamePlay_LevelWin_BackToHome` = 10) và nút Home giữa ván
- [ ] Shop / IAP: gói Coin1–6, Starter, Bundle, NoAds (theo ảnh Shop của bản gốc)
- [ ] Progress: mốc level → quà (coin, booster, năng lượng vô hạn), nhận quà
- [ ] Coin: thưởng khi thắng theo độ khó, x2 bằng ads
- [ ] Tutorial: luật cơ bản, ô khóa, ô ẩn, mũi tên, từng booster, cơ chế mạng
- [ ] Ads (MAX / AdMob), Firebase Analytics + Remote Config

---

## Rủi ro

- **Level phải tự làm thêm.** 205 level ShikaCat không có cơ chế ô đặc biệt; game gốc sinh level ngẫu nhiên và code đã mất thân hàm.
- **Ô ẩn có thể làm level có nhiều lời giải.** Solver xử lý được (không phạt nước đi hợp lệ), nhưng Hint/Reveal cần chọn một lời giải nhất quán.
- **HUD trộn world-space và Canvas.** Lưới, khối, bong bóng dùng SpriteRenderer; HUD và popup dùng Canvas Overlay.
