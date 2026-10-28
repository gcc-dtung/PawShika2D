# 33 – Sửa warning Console, thanh thời gian, đồng bộ chữ UI, icon booster Halloween

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-28 15:05 |

## 2026-10-27 09:44 – Kiểm tra warning Console và sửa lỗi

**Đọc Console** (lúc bắt đầu): `SpriteAtlasManager.atlasRequested wasn't listened to while GameAtlas requested`, 4 dòng `The referenced script (Unknown) on this Behaviour is missing!`. Xoá Console rồi chạy lại từ đầu (vào Play, đi qua 5 tab, bấm Play vào ván, thoát Play, biên dịch lại script) để tách lỗi thật:

- **`[ShopPanel] Chưa gán prefab / parent cho section Limited`** (lúc vào Play): ô `Event` / `Limited` của `ShopPanel` đọc ra prefab rỗng dù file vẫn trỏ đúng `ShopSpecialCard` (cùng fileID) → thẻ Limited Pack không được tạo. Nguyên nhân: Editor giữ tham chiếu cũ sau nhiều lần lưu đè `ShopSpecialCard.prefab` bằng `SaveAsPrefabAsset`. Sửa: import lại `ShopSpecialCard`, `ShopPanel`, `MainMenu` (dữ liệu không đổi). Kiểm tra: Shop tạo đủ 14 thẻ, có Limited Pack.
- **`UIParticle`: `Assertion failed on expression: 'ps->array_size()'` + `Mesh '[UIParticleRenderer] Combine Instance Mesh': abnormal mesh bounds`** (mỗi lần biên dịch lại script khi đang mở prefab thẻ Shop): hệ hạt sao lấp lánh bật `prewarm`, khi nạp lại Editor chạy trước hệ hạt lúc chưa Play nên UIParticle dựng mesh lỗi. Sửa: tắt `prewarm` ở `Sparkles` của 5 thẻ (Special, Bundle, Coin, RemoveAds, Starter); sao xuất hiện sau ~0.3 s khi mở Shop. Kiểm tra: 2 lần biên dịch lại liên tiếp không còn lỗi.
- **Thiếu script ×4**: không còn xuất hiện khi chạy lại. Scene `Main` và mọi prefab trong `_Project` không có component thiếu script; chỉ prefab gốc trong `ThirdParty/ShikaCat` (bản rip tham khảo, 1 prefab `ThirdParty/PawBlock/08_Prefabs/Cell`) còn nhiều component thiếu script → warning hiện khi Editor nạp các prefab đó (xem trước / chọn trong Project). Không dùng trong game (đã gỡ ở mục sau).
- **`GameAtlas` atlasRequested**: không còn xuất hiện khi chạy lại; `GameAtlas` bật `Include in Build` (bindAsDefault 1) như các atlas khác → là thông báo tạm lúc Editor đang đóng gói lại atlas sau khi import ảnh, không phải lỗi cấu hình.

**Kiểm tra cuối**: Play (Home, 5 tab, Shop, vào ván, thoát) và biên dịch lại: Console không còn warning / lỗi.

## 2026-10-27 09:46 – Gỡ component thiếu script trong prefab ThirdParty

**Yêu cầu**: gỡ component thiếu script trong prefab `ThirdParty` (ShikaCat, PawBlock/Cell) để hết warning.

**Sửa**: 98 prefab trong `Assets/ThirdParty` (95 ở `ShikaCat`, 3 ở `PawBlock`) – mở từng prefab, `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` trên mọi object con, lưu lại: gỡ 2176 component (script gốc của game rip không có trong dự án). Không prefab nào có component thiếu nằm trong prefab lồng nên không phải sửa prefab nguồn.

**Kiểm tra**: quét lại toàn bộ prefab trong `Assets`: 0 component thiếu script; tạo thử rồi xoá cả 98 prefab trong scene: Console không còn warning; scene `Main` không bị đổi.

## 2026-10-27 10:01 – Phản hồi mentor: thanh thời gian lệch, chữ UI khó đọc

**Phản hồi** (mentor): hiệu ứng đóng băng lỗi UI, ảnh booster vỡ, thanh thời gian lệch, hướng kéo booster chưa rõ, nút Mua Shop lệch, chữ chưa đẹp / khó đọc. Người dùng chốt: nút Mua là chủ ý; đóng băng để sau (đang làm lại asset); hướng kéo làm mới sau; icon booster thay theo theme (gen bằng Gemini); sửa thanh thời gian và chữ.

**Thanh thời gian (`GameplayHUD` → `LevelFrame/TimeBar`)**: ruột cũ (`TimeBarFill` – ảnh rip `LvProgress2` xoay) là hình chữ nhật sọc, không có đầu bo nên co lại thì đầu phải bị cắt vuông; `FillArea` 588x36 lệch lên 2 px, hẹp hơn lỗ trong của khung. Đo lỗ trong khung `TimeBarFrame` (135x108, viền 66/52, ppum 1.3): lòng sáng cao ~39 px, nằm giữa. Sửa: ruột mới tự vẽ `Source/Art/UI/Gameplay/Halloween/TimeBarFill_HLW.png` (viên thuốc cam bo tròn 2 đầu, dải sáng trên, viền dưới tối; 80x40, 9-slice trái/phải 20, ppum 40/36); `FillArea` 590x36 ở giữa (y 0). Kiểm tra Play: đầy / 45 % / 5 % đều nằm giữa lỗ, đầu bo tròn.

**Chữ UI**: 452 chữ trong prefab UI, trộn 2 font (Fredoka ~220 ở popup / HUD / Daily, Lilita ở Home / Shop / Teams), 25 chữ sáng không viền (popup Thua, Time's up, tutorial HUD, phiên bản...). Sửa: đổi mọi chữ UI Fredoka sang **VN-Lilita-One** (194 chữ, 30 prefab), material theo màu viền cũ: Purple / Purple Thick → `Outline Purple`, Green / Green Bold → `Outline Green`, Red / Red Thick → `Outline Red`, Brown → `Outline Brown`; Fredoka không viền: chữ sáng → `Outline Purple`, chữ tối (trên nền sáng) / placeholder → `Plain`; bỏ đậm giả (Lilita không có bản đậm). Giữ Fredoka cho số trên ô, số đếm khi kéo, số booster (material `Cell Number`; nhãn số mơ hồ tính độ rộng theo Fredoka). Kèm theo: `DifficultyTheme.hudTabOutline` → Lilita Red / Purple; material xám nút tắt `VN-Lilita-One SDF - Outline Gray` (mới, viền như bản Fredoka) cho `UIGrayscaleWhenDisabled` ở BuyBooster, LuckyWheel, Daily; `ProfilePopup` ô nhập tên dùng font Lilita. Quét lại: không chữ nào lệch font / material. Ảnh trước / sau 8 popup; Play Home + màn chơi, Console sạch.

**Lưu ý**: font Lilita và các material viền nằm trong `HalloweenRip/Font` (gitignore) → bản push thiếu font cho gần hết chữ UI. Menu `Tools/PawBlock/Builders/Apply Game Font` (`GameFontSetup`) vẫn đặt Fredoka + material mặc định cho mọi chữ – không chạy lại.

**Icon booster**: Gemini "4 Argon" đang khoá trên tài khoản Plus (biểu tượng ổ khoá) → chưa gen.

## 2026-10-27 10:32 – Icon booster theo theme Halloween (Gemini 4 Argon)

**Yêu cầu**: icon booster còn bản cũ (bóng đèn, đồng hồ báo thức, kính lúp), thay cho hợp theme; gen bằng Gemini "4 Argon" trên Chrome; ít chi tiết nhỏ vì hiển thị nhỏ (ô 130 px).

**Gen**: Gemini 4 Argon (nick `ductung.dev`, `gemini.google.com/u/1`; nick mặc định `ductung.ptit` bị khoá Argon), đính kèm `GhostMascot.png` làm mẫu nét vẽ. Qua 3 vòng: bản đầu nhiều chi tiết (đèn lồng có khung, sao lấp lánh) → bản đơn giản (bỏ lấp lánh, viền dày, khối to) → Hint đổi từ quả bí ngô (không thể hiện "gợi ý") sang **bóng đèn bí ngô** (thân bí ngô phát sáng, đuôi đèn tím, tia sáng); Freeze đổi sang **đồng hồ cát bè, mập** (khung tím 2 tấm + 2 cột, bông tuyết to, nhũ băng; bỏ vòng treo, vòng vàng nhỏ); Reveal là **quả cầu pha lê** tím trên đế gỗ cánh dơi.

**Đưa vào game**: tải 3 ảnh 1024 px; tách nền trắng bằng loang từ mép (+ điểm loang tay cho khe nền kín: 2 khe hai bên thân kính đồng hồ cát, 2 khe dưới cánh dơi; giữ bông tuyết và vệt bóng trắng), cắt sát, đặt giữa ô vuông, 512 px → `Source/Art/UI/Gameplay/Halloween/Boosters/Booster_Hint_HLW`, `Booster_Freeze_HLW`, `Booster_Reveal_HLW` (Sprite, Android ASTC 4x4 chất lượng 100, tối đa 512; ảnh tự gen nên commit được). Thay cả 2 bộ icon cũ (`btn_hint` / `btn_freze` / `btn_reveal` trên nút và `HintBig` / `FreezeBig` / `RevealBig` ở Shop / LuckyWheel) bằng icon mới qua GUID trong 8 prefab (`GameplayHUD`, `BoosterButton`, `BuyBoosterPopup`, `ProfilePopup`, `LuckyWheelPopup`, `ShopBundleCard`, `ShopSpecialCard`, `ShopStarterCard`) và `BoosterDatabase`.

**Kiểm tra**: Play – nút booster trong màn chơi và thẻ Limited Pack ở Shop hiện icon mới; Console sạch. Ảnh trước / sau.

## 2026-10-27 10:36 – Icon trên nút booster to hơn

Icon mới gần vuông nên ở ô 130x130 trông nhỏ hơn icon cũ (cũ cao, hẹp, chiếm hết chiều cao). Sửa: `BoosterButton.prefab` → `Icon` 130 → 150 (giữ tỉ lệ); 3 nút trong `GameplayHUD` là instance nên nhận theo. Kiểm tra Play: ảnh so 130 / 150.

## 2026-10-27 10:39 – Khung nút booster mới

**Yêu cầu**: thay khung nút booster (`btn_1`, tròn tím xanh) cho đẹp hơn.

**Sửa**: khung mới tự vẽ `Source/Art/UI/Gameplay/Halloween/Boosters/BoosterFrame_HLW.png` (416 px, ppu 200 → hiển thị 208): đồng bộ khung LEVEL / khung bảng – viền nâu, đáy nâu đậm lệch xuống (nổi khối), vành kem + đường tan, lòng tím đậm chuyển dọc (sáng trên, tối dưới), vệt bóng mờ phía trên, viền trong tối mảnh. Android ASTC 4x4 chất lượng 100. Gán vào `BoosterButton.prefab` (Image gốc, cũng là đích ColorTint khi nhấn); 3 nút trong `GameplayHUD` nhận theo. Ô số lượng (`btn_number` xanh dương), dấu cộng, ổ khoá giữ nguyên.

**Kiểm tra**: Play – ảnh so khung cũ / mới.

## 2026-10-27 10:49 – Khung nút booster gen bằng Gemini (mẫu cánh dơi)

**Yêu cầu**: có thể gen art mới, không nhất thiết hình tròn. Nick `ductung.dev` hết hạn mức mọi model tới 12:46 → gen bằng **3.6 Flash** ở nick `ductung.ptit`, đính kèm 3 icon booster làm mẫu nét vẽ. 3 mẫu: vuông bo góc có cánh dơi / bia mộ / bí ngô (mỗi mẫu có sẵn ô tròn kem góc dưới phải). Người dùng chọn **mẫu 1**.

**Sửa**:
- Tách nền trắng ảnh 1024x572, cắt khung 1 (347x320, ranh giới với khung 2 ở cột 359) → ghi đè `Source/Art/UI/Gameplay/Halloween/Boosters/BoosterFrame_HLW.png` (giữ GUID; ppu 100, ASTC 4x4 q100).
- `BoosterButton.prefab`: gốc 208x208 → 273x252 (thân khung ≈ 208 như cũ, cánh dơi thò ra); `Icon` (-1, -3) theo tâm lòng tím (đo tâm (172, 164) trên ảnh); `Count` về ô tròn kem (78, -87), 60x60, tắt Image `btn_number` (chỉ còn chữ số trên ô kem của khung); `Add` cùng chỗ, 64x64. 3 nút trong `GameplayHUD` (override cỡ 208) → 273x252.

**Kiểm tra**: Play – ảnh so khung tròn / khung mới; Console sạch.

## 2026-10-27 10:55 – Quay lại khung nút tròn kem – tím

**Phản hồi**: quay lại khung cũ (người dùng chọn khung tròn kem – tím tự vẽ, không phải `btn_1` gốc).

**Sửa**: vẽ lại `BoosterFrame_HLW.png` đúng thông số khung tròn (416 px, ppu 200; GUID giữ nguyên); `BoosterButton.prefab` về 208x208, `Icon` (0, 6), `Count` (68, -68) 76x76 bật lại nền `btn_number`, `Add` (68, -68) 76x76; 3 nút trong `GameplayHUD` về 208x208. Ảnh khung cánh dơi (gen bằng Gemini) vẫn ở Downloads / scratchpad, không dùng.

**Kiểm tra**: Play – 3 nút khung tròn, số lượng trên ô xanh như trước. (Lần Play đầu bật ngay sau khi lưu prefab nên còn nạp bản cũ; chạy lại thì đúng.)

## 2026-10-28 14:21 – Bỏ theme mèo còn sót, ô hướng rõ hơn, nền màn nhỏ, làm lại hồ sơ

**Yêu cầu** (4 việc): (1) chỗ còn theme mèo → Halloween, avatar Main Menu mất nền; (2) ô clue có hướng bị chìm; (3) màn nhỏ nền bị phóng to, mạng nhện chiếm chỗ; (4) làm lại hồ sơ + popup sửa hồ sơ theo mẫu Foodie Sizzle, chọn avatar không đóng ngay mà bấm Lưu mới đổi.

**Sửa**:
- **Theme mèo**: đổi GUID sprite trong prefab / data: coin chân mèo `coin.png` → `HalloweenRip/Top/Coin.png` (9 file: CoinHUD, WinPopup, BuyBooster, LuckyWheel, MoreTime, RefillEnergy, Profile, DailyRewardSlot, DailyRewardDatabase); đống coin `icon_coin_2_0 / icon_coin_3` → `HalloweenRip/Shop/CoinLV2 / CoinLv3` (WinPopup, phóng 1.7 vì ảnh mới có viền trong); mèo `cat_1` ở huy hiệu WinPopup → `Halloween/GhostMascot`. ProfilePopup: icon Hard Wins (đầu mèo) → bí ngô `bi_ngo`, lịch chân mèo → `HalloweenRip/Side/Calendar`.
- **Ô hướng** (`CellView.cs`, `Cell.prefab`): ô có ràng buộc hướng dùng nền teal riêng `Cell_Orientation.png`; khung hướng mới viền kem + mũi tên (`Badge_Dir_Tall / Wide / Square.png`, thư mục `Source/Art/InGame/Gameplay/Halloween`); vẽ lại 3 khung hướng trong ảnh `popup_tutorial_4.png` (MechanicIntro).
- **Nền / trang trí màn nhỏ**: `BackgroundFitter` luôn scale vừa phủ camera (bỏ `_baseScale` – trước giữ cỡ cố định nên màn nhỏ camera zoom vào thì nền to gấp đôi); `BoardDecor` thêm `maxSizeRatio` (mặc định 0.4): cạnh dài món trang trí ≤ 0.4 × cạnh ngắn khung bảng, chỉ thu nhỏ (màn 4x4 web/ghost ≈ 0.78 cỡ cũ, từ 5x5 giữ nguyên).
- **Hồ sơ**:
  - `AvatarDatabase`: `avatars / frames / badges` dạng `ProfileItem` (ảnh, `unlockLevel`, tên), `nameStyles` (màu gradient trên / dưới). Dữ liệu: 16 avatar đồ ăn (4 cái cuối khoá lv 10/20/30/40), 13 khung (4 khung Halloween khoá lv 5/15/25/40), 7 kiểu tên, 2 huy hiệu (lv 30/60). Ảnh rip ở `HalloweenRip/Profile` (gitignore).
  - `ProfileSave` thêm `frame`, `nameStyle`, `badge` (-1 = chưa đeo); `PlayerProfileService.SetLook` thay `SetAvatar`; `PlayerProfile` thêm `FrameId / NameStyleId / BadgeId`.
  - Mới: `ProfileAvatarView` (avatar + khung), `ProfileItemCell` + prefab `Widgets/Profile/ProfileItemCell`, `EditProfilePanel` (4 tab Avatar / Frame / Name / Badge, lưới cuộn, xem trước, sửa tên, Save / X; chọn chỉ đổi nháp, ô khoá báo "Unlocks at level N").
  - `ProfilePopup.prefab`: thẻ người chơi mới (avatar + khung, tên theo kiểu tên + bút sửa, huy hiệu, lịch, ID dưới avatar); bỏ `AvatarPicker` và ô sửa tên tại chỗ; bút / bấm avatar mở `EditPanel`.
  - `HomePanel.prefab`: nút avatar Main Menu = avatar đồ ăn (có nền) + khung theo save.

**Kiểm tra**: Console sạch sau biên dịch; Play: màn tutorial 1x3 nền đủ cả cảnh, mạng nhện nhỏ ở góc; mở hồ sơ → Edit → chọn avatar / khung / kiểu tên (save chưa đổi) → Save (save đổi, popup cập nhật) → mở lại chọn rồi X (save giữ nguyên); khôi phục save về avatar 0 / khung 0. Render prefab: WinPopup (ma thay mèo), HomePanel (avatar có nền), ô hướng.

## 2026-10-28 14:40 – Ổ khóa booster dễ nhìn, giao diện hồ sơ theo game gốc

**Yêu cầu**: (1) icon khóa nút booster màu tím dính nền, khó nhìn; (2) giao diện hồ sơ làm giống game gốc (Foodie Sizzle).

**Sửa**:
- **Ổ khóa booster**: vẽ lại `Source/Art/UI/Gameplay/Buttons/btn_lock.png` (giữ GUID, 76x103 → 228x309): thân vàng, quai bạc, viền nâu, lỗ khóa nâu. Bản cũ lưu ở scratchpad.
- **Hồ sơ theo `ModalProfile.prefab` gốc** (đọc cây + vị trí + sprite của prefab rip, cắt 23 sprite vào `HalloweenRip/Profile/UI`, gitignore, đặt 9-slice theo border gốc):
  - `ProfilePopup.prefab` / `Panel` (1080 rộng, neo đỉnh màn hình): thanh tiêu đề nâu nhạt `PROFILE` + X nâu; thẻ kem viền nâu (avatar + khung, ô tên be với bút xanh, huy hiệu (chưa đeo: khiên trống + `---`), lịch, `PLAYER ID` dưới thẻ, ruy băng vàng `LVL` nhô lên); bảng `GENERAL STATS` (ruy băng nâu, 6 ô be, số trong ô nâu nhạt, chữ trắng viền nâu). Icon "thắng ngay lần đầu" = vương miện.
  - `EditPanel`: khung `MainUI` (đầu nâu nhạt, thân kem) `EDIT PROFILE` + X; avatar, ô tên + bút, `PLAYER ID`; 4 tab nâu (tab đang mở màu kem, 2 tab đầu/cuối bo góc); lưới nền hồng nhạt; nút `SAVE` xanh.
  - `ProfileItemCell.prefab`: chọn = viền xanh ngọc + tick xanh góc phải dưới; khóa = ổ khóa vàng; ô kiểu tên có bảng tím nhạt `NameBoard`, lưới 2 cột (avatar / khung / huy hiệu 3 cột).
  - Code: `ProfileItemCell` (nền ô kiểu tên, dời khóa sang phải ở ô kiểu tên), `EditProfilePanel` (đổi cỡ ô / số cột theo tab), `ProfilePopup` (icon khiên khi chưa có huy hiệu), chữ ID → `PLAYER ID:`.

**Kiểm tra**: Console sạch; render prefab nút booster khóa, hồ sơ (dữ liệu giả lv 73), Edit tab Avatar / Frame / Name. Phiên Play đang mở của người dùng bắt đầu trước khi sửa prefab nên chưa thấy bản mới – cần dừng rồi Play lại.

## 2026-10-28 14:50 – Khung popup theo game gốc Foodie Sizzle

**Yêu cầu**: thay khung các popup bằng asset Foodie Sizzle (khung tím viền vàng hiện tại không hợp game).

**Sửa** (10 popup: BuyBooster, DailyReward, Lose, LuckyWheel, MechanicIntro, MoreTime, Notice, QuitLevel, RefillEnergy, Settings – script chạy 1 lần trong Editor):
- Khung `Panel` → `MainUI` (đầu nâu nhạt, thân kem, 9-slice). Panel tự nới: mép trên đủ chỗ thanh đầu (cách nội dung 250), mép dưới bọc cả nút đang lòi ra ngoài (Lose, MoreTime); con được bù vị trí theo neo nên đứng yên trên màn.
- Tiêu đề: tắt nền `TitleTab`, chữ trắng viền nâu trong thanh đầu; nút đóng = X nâu. Cả hai neo mép trên (Settings ở Main Menu tự cắt đáy 180 vẫn đúng). Widget `CloseButton.prefab` cũng đổi X nâu.
- Ô trong `khung_popup_1 / khung_popup_4` → `UI1_0` (be), `khung_pill_kem` → `UI2_2`.
- Nút: `btn_green / red / yellow` → `NewBtnGreen_1 / NewBtnRed_1 / NewBtnYellow_1` (9-slice, multiplier 1.35); nút 2 lớp tắt Image lớp mặt (giữ chữ).
- Chữ: Outline Purple / Plain → trắng viền nâu; chữ nhỏ (< 50, ngoài nút) → nâu đặc cho dễ đọc.
- Settings: 3 nút tròn → 3 hàng icon nâu + chữ + công tắc (`ON` / `OFF` + núm `SwitchBT`). `SettingToggleView` thêm `knob` / `knobOffset`: núm trượt phải khi bật, trái khi tắt (DOTween 0.15 s).
- Sprite mới ở `HalloweenRip/Popup` (gitignore).

**Kiểm tra**: render 10 popup trước / sau; Play: Daily Reward, Settings ở Main Menu (bấm Sound → tắt, núm sang trái, rồi bật lại), Console sạch.

## 2026-10-28 15:00 – Sửa icon booster bị tối sau lần bấm đầu

**Lỗi**: bấm nút booster lần đầu bình thường, từ lần sau icon (và số lượt) tối hơn – giống lỗi chữ nút Play hôm 27/10.

**Nguyên nhân** (`ButtonPress`): nhấn thì tô tối các hình con bằng `Graphic.CrossFadeColor`, thả tay thì sáng dần lại. CrossFade là coroutine chạy trên chính hình con; bấm booster làm hình con tắt / bật lại đúng lúc đang sáng dần (vd icon đổi sang đếm giây khi đóng băng, ô số vẽ lại, tutorial mở khóa) → coroutine dừng giữa chừng, kẹt màu ≈ 0.8. Bản sửa 27/10 (`ClearTint` khi chính nút bị tắt) không bắt được trường hợp chỉ hình con tắt.

**Sửa**: `ButtonPress.Tint` tween màu bằng DOTween chạy trên nút (`_tintT` 0 → 1, `SetUpdate(true)`, `SetLink(gameObject)`), mỗi bước `canvasRenderer.SetColor` cho các hình đang tô (giữ alpha). Hình con tắt / bật giữa chừng vẫn nhận màu đúng. `ClearTint` hủy tween + về trắng. Áp dụng cho mọi nút có `ButtonPress`.

**Kiểm tra**: Play level 61, bấm nút booster trong tutorial mở khóa (nhấn và thả ở 2 frame khác nhau như tay thật): trước sửa nút Freeze kẹt icon / số 0.80; sau sửa nút Reveal giữ = 0.78, thả = 1.00, cả 3 nút sáng đều. Console sạch. Lưu ý: lúc test đã hoàn thành tutorial mở khóa Freeze và Reveal trong save hiện tại (mỗi cái dùng 1 lượt miễn phí).

## 2026-10-28 15:05 – Daily Reward: ô hôm nay phình đè ô bên, khó nhìn

**Lỗi**: ô ngày điểm danh hôm nay trông phóng to, đè lên các ô bên cạnh, màu không nổi.

**Nguyên nhân**: `Highlight` (viền vàng của ô hôm nay) là 1 ô `khung_popup_4` tô vàng nằm sau ô, to hơn 15 px mỗi bên. Đợt đổi khung popup 14:50 đổi nhầm nó thành `UI1_0` màu trắng → khối be to hơn ô, lại nhấp nháy 1.04, che khe 12 px giữa các ô. Cũng đợt đó nền ô Day 7 bị đặt về trắng, mất màu riêng.

**Sửa**: `Highlight` = `UI1_0` tô vàng (1, 0.76, 0.12), lớn hơn ô 8 px mỗi bên; nhịp nhấp nháy 1.04 → 1.03 (`DailyRewardSlot.cs`) để viền luôn nằm trong khe. Day 7 nền cam nhạt (1, 0.87, 0.72). Sửa cả `DailyRewardSlot.prefab` lẫn 7 ô trong `DailyRewardPopup.prefab`.

**Kiểm tra**: render popup, bật highlight ô Day 2 ở scale 1.03: viền vàng rõ, không chạm ô bên cạnh; Console sạch. Phiên Play người dùng đang mở bắt đầu trước khi sửa nên cần Play lại mới thấy.
