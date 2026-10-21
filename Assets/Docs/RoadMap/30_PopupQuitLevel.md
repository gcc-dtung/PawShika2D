# 30 – Popup Quit Level: chữ dễ đọc hơn; Shop: hiệu ứng sáng; outline chữ toàn UI; nút Setting; chữ Level HUD + số ô; sửa crash giả lập

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | – |
| Thời gian push | – |
| Cập nhật lần cuối | 2026-10-20 12:20 |

## 09:38 – Chữ popup Quit Level theo bản gốc (phương án A)

**Thêm**
- 2 material TMP riêng (không sửa material dùng chung, vì `Outline Purple` có 17 prefab dùng, `Outline Red` có 5):
  - `Shared/Fonts/Fredoka-Bold SDF - Outline Purple Thick.mat`: dilate 0.1 → 0.15, outline 0.3 → 0.35 màu tím đậm hơn `#2E1466`, bóng dưới (underlay) lệch -1.1 → -1.6, dilate 0.6 → 0.7.
  - `Shared/Fonts/Fredoka-Bold SDF - Outline Red Thick.mat`: dilate 0.1 → 0.15, outline 0.28 → 0.34, bóng lệch -0.8 → -1.2, dilate 0.6 → 0.7.

**Sửa** (`Gameplay/Prefabs/Popups/QuitLevelPopup.prefab`)
- "QUIT LEVEL": dùng `Outline Purple Thick`.
- "You will lose": màu `#5C3DB8` → `#4A299A` (tím đậm hơn), cỡ 62 → 66, Bold.
- "-1": dùng `Outline Red Thick`, cỡ 118 → 130, vị trí (78, -58) → (88, -62).
- Chữ nút "Quit": dùng `Outline Red Thick`, cỡ 68 → 74.

**Nền không tối**: không phải lỗi. Lớp `Dim` (đen 88%) vẫn chạy khi mở popup trong level (Setting → Home lúc đang chơi). Ảnh lỗi là do popup bị bật sẵn trong scene `Main` ở chế độ Edit nên hiện đè lên Menu. Đã tắt lại, không đổi code.

**Kiểm tra**: Play mode, vào level 35, mở popup: nền tối, chữ đậm và dễ đọc hơn (ảnh `Captures/quit_before.png`, `Captures/quit_after.png`). Console không lỗi / cảnh báo.

## 10:05 – Shop: quầng sáng + sao lấp lánh bên trái, vệt sáng chạy qua icon (theo video mẫu)

**Thêm**
- `Shared/Art/Shaders/UI_Shine.shader` (`PawBlock/UI/Shine`) + `Shared/Art/Materials/UI_Shine.mat`: như UI/Default, cộng thêm vệt sáng chéo (hình /) quét qua, chỉ sáng trong phần sprite có alpha. Vệt tính theo vị trí trên màn hình (không theo UV) nên không lệch khi sprite nằm trong atlas; mọi icon dùng chung material sáng lần lượt từ trái sang phải. Chạy theo `_Time`, không cần script. Chỉnh trên material: màu / độ rộng / độ nghiêng / thời gian quét (0.9 s) / chu kỳ (3 s).
- `Shared/Scripts/UI/UIPulse.cs`: phóng to / thu nhỏ + sáng / mờ nhẹ theo hình sin (thời gian thật).
- `Shared/Scripts/UI/UISparkles.cs`: các sao con hiện ngẫu nhiên trong vùng rect, phóng to – xoay – biến mất, nghỉ ngẫu nhiên rồi lặp; danh sách sao do Editor tự thu.
- `Features/Shop/Art/UI/Effects/fx_glow.png` (quầng tròn mềm trắng), `fx_sparkle.png` (sao 4 cánh trắng), tô màu bằng `Image.color`.

**Sửa** (`Features/Shop/Prefabs/`)
- `ShopBundleCard`, `ShopStarterCard`: thêm `Body/Glow` (sau Icon, `UIPulse`, hồng; Starter dùng hồng nhạt vì nền tím) và `Body/Sparkles` (trước Icon, 4 sao 90 px, `UISparkles`); Coin + 4 icon vật phẩm dùng `UI_Shine.mat`.
- `ShopCoinCard` (Icon), `ShopRemoveAdsCard` (Icon): dùng `UI_Shine.mat`.

**Kiểm tra**: render thẻ ra ảnh ngoài màn hình (`Captures/fx_test.png`): vệt sáng quét coin → vật phẩm, chỉ trong hình icon; quầng sáng + sao hiện đúng chỗ. Console không lỗi. Chưa xem hoạt cảnh liền mạch trong Play (Unity chạy nền nên bị giảm frame) – cần mở Shop xem lại tốc độ / độ đậm.

## 10:40 – Outline chữ toàn UI: viền tối, dày, nổi khỏi nền

**Nguyên nhân chữ khó nhìn**
1. Atlas `Fredoka-Bold SDF` tạo bằng chế độ **SDFAA** (chỉ tính khoảng cách sát mép chữ) với padding 9 / 90pt: viền dày hơn ~0.3 sẽ nhạt dần thành trong suốt, nên viền luôn mỏng.
2. Project dùng **Linear color space** nhưng shader `TextMeshPro/Mobile/Distance Field` dùng màu outline / underlay như sRGB → màu viền hiện ra **nhạt hơn nhiều** so với màu chọn (vd chọn `#1A0840` ra `#593289`), nên viền tím gần trùng nền tím.

**Sửa**
- `Shared/Fonts/Fredoka-Bold SDF.asset`: render mode SDFAA → **SDF16**, padding 9 → **16**, atlas 1024 → **2048** (75 ký tự cũ không còn chỗ với padding lớn), bật multi-atlas. Dựng lại atlas (đủ 75 ký tự).
- 8 material `Fredoka-Bold SDF - Outline *` (+ material mặc định): `_GradientScale` 10 → 17, `_TextureWidth/Height` 2048.
  - Dilate 0.1 → 0.25 (Thick / Bold 0.3), outline 0.28–0.3 → 0.4 (Thick 0.45, Green Bold 0.5), bóng dưới lệch -0.8/-1.1 → -0.9 (Thick -1.1), underlay dilate 0.4 (Thick 0.5).
  - Màu viền + bóng tối hẳn: Purple `#2A1060`, Brown `#4A1A06`, Green `#0E4206`, Red `#5E0408`, Gray `#262626`, Purple Thick `#1E0848`, Red Thick `#4E0306`, Green Bold `#0A3604`.
  - **Lưu ý**: màu lưu trong material ở dạng linear (`Color.linear`) để hiện đúng màu trên. Trong Inspector trông gần như đen, đó là đúng. Muốn đổi màu thì chọn màu mong muốn rồi lưu `.linear`, không chọn thẳng.

**Kiểm tra**: render 8 popup trước / sau (`Captures/text_before.png`, `Captures/text_after.png`), dải test 5 nền (`Captures/text_test.png`), Play màn Shop (`Captures/text_after_shop.png`): viền tối, đặc, không cắt ô vuông; chữ không tràn khung. Console không lỗi. Chữ dùng material mặc định (không viền, vd "Music", "Remove Ads") giữ nguyên.

## 11:00 – Outline mỏng lại (bản 10:40 làm chữ nặng)

**Sửa** 8 material `Fredoka-Bold SDF - Outline *` (giữ màu tối, atlas SDF16 / padding 16):
- Thường (Purple / Brown / Green / Red / Gray): dilate 0.25 → **0.05**, outline 0.4 → **0.17**, bóng lệch -0.9 → **-0.6**, underlay dilate 0.4 → **0.1**.
- Purple Thick / Red Thick: dilate 0.3 → 0.08, outline 0.45 → 0.22, bóng -1.1 → -0.75, underlay dilate 0.5 → 0.15. Green Bold: outline 0.5 → 0.24, còn lại như Thick.

**Kiểm tra**: dải thử `Captures/text_test.png` (bản dày) và `Captures/text_test_thin.png` (bản mới): vùng viền tối giảm khoảng 1/2 (22 655 → 11 636 px), chữ nhẹ hơn, viền vẫn đặc. Popup: `Captures/text_after_thin.png`.

## 11:20 – Nút Setting: khung vuông tím + bánh răng như bản gốc

**Thêm**
- `Shared/Art/UI/Frames_Borders/khung_setting_4.png`: copy từ `ThirdParty/PawBlock/04_UI_Graphics/Frames_Borders/` (khung vuông bo góc tím, trước chưa dùng). Nằm trong thư mục SharedAtlas nên tự vào atlas.

**Sửa**
- `Shared/Prefabs/Widgets/SettingsButton.prefab`: nền = `khung_setting_4` (giữ tỉ lệ); con `Gear` = `khung_setting_3` (bánh răng kem viền tím), 100 px ở giữa, lệch lên 3 px (chừa bóng dưới của khung), không nhận raycast. Trước đó là bánh răng tím `btn_setting` đè lên bánh răng kem, không có khung.
- `Game/Prefabs/Menu/HomePanel.prefab` (nút 118 px): override `Gear` 88 → 78 px, lệch lên 2 px. `GameplayHUD` (150 px) dùng giá trị prefab.
- `Shared/Art/UI/Buttons/btn_setting.png` giờ không còn dùng (để nguyên).

**Kiểm tra**: Play, màn Home (`Captures/setting_home.png`) và trong level (`Captures/setting_gameplay.png`): nút giống ảnh bản gốc. Console không lỗi.

## 11:40 – Chữ "LEVEL n" trên HUD trong ván dễ đọc hơn

**Nguyên nhân**: `LevelText` dùng material không viền (`Fredoka-Bold SDF Material`) + kiểu Bold giả (shader làm phình nét) → chữ trắng nhòe, chìm vào nền khung tím nhạt.

**Sửa** (`Gameplay/Prefabs/GameplayHUD.prefab`, `UIZone/TopPanel/LevelFrame/LevelText`): material → `Outline Purple` (như chữ đồng hồ bên dưới), kiểu Bold → Normal (font Fredoka-Bold vốn đậm), cỡ 54.7 → 58, giãn chữ 0 → 2.

**Kiểm tra**: render khung Level cũ / mới (`Captures/level_text_cmp.png`): chữ có viền tím đậm, nét gọn, tách khỏi nền. Không script nào đổi material chữ này lúc chạy (`LevelHUD` chỉ `SetText`).

## 12:00 – Số trong ô lưới dễ đọc hơn

**Nguyên nhân**: số ô (`Cell.prefab` → `Text (TMP)`, TextMeshPro world-space) màu tím nhạt `#9E99E6`, không viền, kiểu Bold giả làm nét phình lấp kín lỗ số ("6", "4", "3" thành cục tròn) → chìm trên nền ô tím than.

**Thêm**: `Shared/Fonts/Fredoka-Bold SDF - Cell Number.mat` (copy từ Outline Purple): viền + bóng `#120A2E` (lưu dạng linear), dilate 0.08, outline 0.2, bóng lệch -0.7, underlay dilate 0.15.

**Sửa** `Gameplay/Prefabs/Cell.prefab` → `Text (TMP)`: material → `Cell Number`, Bold → Normal, màu → `#F4F1FF` (trắng ngả tím). Không script nào đổi màu số ô (chỉ `SetText`, alpha khi mờ dần vẫn chạy).

**Giữ nguyên**: số trên khối đã đặt (`PlacedBlock` → `Number`, màu sẫm theo màu khối như bản gốc), số trên khóa, bong bóng đếm khi kéo.

**Kiểm tra**: render 4 ô trước / sau (`Captures/cell_number_before.png`, `Captures/cell_number_after.png`): số trắng, có viền tối, lỗ số rõ. Test `FeelPlayModeTests` chỉ kiểm tra scale / vị trí số, không phụ thuộc material.

## 12:20 – APK crash trên giả lập: tắt Auto Graphics API

**Hiện tượng**: APK chạy được trên điện thoại thật nhưng crash trên giả lập.

**Nguyên nhân (khả năng cao)**: Player Settings Android bật **Auto Graphics API** → Unity chọn **Vulkan trước** OpenGLES3. Vulkan trên giả lập (LDPlayer / BlueStacks / Nox / AVD) hay lỗi driver nên crash khi khởi tạo đồ họa. Chưa có logcat xác nhận (lúc kiểm tra không có giả lập nào kết nối adb).

**Sửa** (`ProjectSettings/ProjectSettings.asset`): tắt Auto Graphics API cho Android, thứ tự API: **OpenGLES3, Vulkan** (máy nào có GLES3 đều dùng GLES3).

**Chưa đổi**: Target Architectures vẫn ARMv7 + ARM64 (IL2CPP). Nếu vẫn crash trên giả lập x86_64 thì thêm x86_64 (APK to hơn) hoặc lấy logcat: `adb logcat -s Unity AndroidRuntime CRASH DEBUG` (adb có sẵn trong `Unity/Hub/Editor/2022.3.62f3/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/`).

**Kiểm tra**: đọc lại setting: auto = false, APIs = OpenGLES3, Vulkan. Cần build APK mới và cài lên giả lập để xác nhận.

## Badge Daily Challenge thành dấu "!", ẩn Lucky Wheel

**Sửa** `Game/Prefabs/Menu/MenuTabBar.prefab` → `Tab_Daily/Icon/Badge`: thêm con `Mark` (chữ "!" trắng, copy từ badge nút Lucky), badge 44 → 52 cho giống badge Lucky. Chữ "!" (cả badge Lucky) dùng material `Fredoka-Bold SDF - Outline Red` để có viền đỏ sẫm, không đụng material mặc định của các text khác. `DailyTabBadge` vẫn bật / tắt cả `Badge` như cũ, thêm nảy nhắc: khi badge hiện thì cứ 2.5s nảy 2 nhịp tại chỗ (`DOJumpAnchorPos`, cao 14, 0.5s; chỉnh trong Inspector), tắt badge / tắt menu thì dừng và trả về vị trí cũ.

**Sửa** `Game/Prefabs/Menu/HomePanel.prefab` → `UIZone/Lucky`: tắt (inactive) – bỏ sự kiện Lucky Wheel khỏi Home. Code `Features/LuckyWheel` và popup trong `Canvas_Popup` giữ nguyên (bật lại nút là dùng được).

**Kiểm tra**: đọc lại prefab: badge 52×52 có `Mark` "!"; instance Lucky trong `Main` inactive. Console không lỗi.
