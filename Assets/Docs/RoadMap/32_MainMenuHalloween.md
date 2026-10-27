# 32 – Main Menu theme Halloween (nền động, hồn ma, 5 tab)

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `be61fce` "Teams đầy đủ, màn chơi Halloween, hiệu ứng thẻ Limited Pack, sửa vuốt tab" (nhánh `RefactorProjectStructure`) |
| Thời gian push | 2026-10-27 09:32 |
| Cập nhật lần cuối | 2026-10-27 09:33 |

## 05:20 – Dựng lại Home theo video mẫu, đổi theme mèo → Halloween

**Nguồn**: video Home Halloween của Foodie Sizzle (98 s) làm mẫu bố cục / chuyển động. Ảnh, font lấy từ bản rip `E:\FoodleSizzle` (chỉ để học, **không push**: thư mục `Game/Art/UI/HalloweenRip/` nằm trong `.gitignore`). Nhân vật chính là hồn ma tự gen bằng Gemini (`Game/Art/UI/Halloween/GhostMascot.png`, commit bình thường).

**Spine 4.2 → ảnh tĩnh**: skeleton Halloween xuất bằng Spine 4.2.43, project dùng runtime 4.1 nên không nạp được (nâng runtime thì 3 skeleton 4.1 cũ hỏng). Dùng spine-csharp 4.2 chính chủ chạy bằng dotnet (ngoài project) dựng pose gốc, cắt từng mảnh (mây trước / sau, nhà, đèn cửa sổ, cây, dơi treo, mộ, bí ngô + quầng, nến + quầng) thành PNG; chuyển động `idle` làm lại bằng component UI.

**Thêm**
- `Shared/Scripts/UI/UIFloat.cs`: trôi lệch / lắc góc / co giãn theo sin (mây, cây, dơi treo, chổi, hồn ma).
- `Shared/Scripts/UI/UIFlicker.cs`: alpha (và co giãn) chập chờn theo Perlin (đèn cửa sổ, quầng nến, quầng bí ngô).
- `Shared/Scripts/UI/UIFlyAcross.cs`: bay từ A tới B lượn sóng, vỗ cánh, nghỉ ngẫu nhiên; `pingPong` đi qua lại và lật ngang (dơi, ma nhỏ).
- `Game/Scripts/Menu/GhostMascot.cs`: chạm hồn ma thì nhún bẹp + lắc (DOTween).
- `Game/Scripts/Menu/LockedMenuButton.cs`: nút tính năng chưa mở ("Lv X"), bấm thì icon lắc, nhãn nảy.
- `Game/Scripts/Menu/LeaderboardPanel.cs` + `LeaderboardRow.cs`: tab Xếp hạng tuần giả lập offline (10 đối thủ, điểm theo số tuần; người chơi = cấp hiện tại), bục top 3 + danh sách cuộn (`NestedScrollRect`).
- `Game/Scripts/Menu/LockedTabPanel.cs`: tab Bộ sưu tập (màn khóa tĩnh có hồn ma).
- Prefab `Game/Prefabs/Menu/LeaderboardPanel`, `CollectionPanel`.
- Font `VN-Lilita-One SDF` (SDF16, padding 16, 2048, dynamic, nạp sẵn chữ Việt) + 4 material `Outline Brown / Green / Purple`, `Plain` (màu viền lưu `.linear`).

**Sửa**
- `HomePanel.prefab`: bỏ `Background` (ảnh tĩnh phòng mèo), thêm `Scene` (Sky · Top: trăng, mây, 3 dơi · World neo đáy khung 1080x2412: nhà ma 2 bên, mây, sân, rào, ma nhỏ, nến, hồn ma + bóng, mộ, bí ngô, chổi). Thanh trên, nút hai bên (khung vuông tối, icon Halloween, nhãn dưới) và nút Play (xanh 9-slice, mạng nhện, nhện đung đưa, chữ "Level n") đổi sprite / font. Thêm 5 nút khóa trang trí (vạc Lv 20, rương Lv 50, bóng bay Lv 25, đầu bếp Lv 35, mũ Lv 45).
- `MenuTabBar.prefab`: 5 tab Shop | Rank | Home | Daily | Collection, nền tab be / tab chọn cam (9-slice), tab chọn rộng 300.
- `MainMenu.prefab`: `Panels` thêm `LeaderboardPanel` (1) và `CollectionPanel` (4); `MainMenuNavigator.menuPanels` 5 phần tử, `startIndex` 2.
- `MainMenuNavigator.cs`: hằng `ShopTab 0 / RankTab 1 / HomeTab 2 / DailyTab 3 / CollectionTab 4`.
- `GameBootstrap.cs`: field `leaderboardPanel` + `Init(s.Profile)`; scene `Main` nối field.
- Tài liệu: `ProjectStructure.md`, `Features/10_UI-Menu-Popup.md`, `Features/HowTo_MoRong.md`.

**Lỗi gặp khi làm**: `Shader.Find("TextMeshPro/Mobile/Distance Field")` trả về shader trùng tên trong `ThirdParty/ShikaCat` (queue 2000) → chữ thành khối đen. Gán thẳng `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader`.

**Kiểm tra**: Play ở 1080x1920: Home (nền động, dơi bay, ma đi, đèn chập chờn, chạm ma / nút khóa), tab Rank, Collection, Daily đúng; console không lỗi. EditMode 1069 test: test của project đạt; 3 test lỗi thuộc package `com.besty.unity-skills` (không liên quan).

## 06:10 – Thanh tab: mỗi tab một ô nền như bản gốc

**Phản hồi**: thanh tab thiếu khung nền phía sau (bản gốc mỗi tab là một ô be viền nâu riêng).

**Sửa**
- `MenuTabBar.cs`: thêm `tiles` (ô nền từng tab, cùng thứ tự `tabs`), `Layout` / `Animate` cho ô chạy theo vị trí và bề rộng của tab.
- `MenuTabBar.prefab`: thêm `TabBar/Tiles` (5 ô `TabNormal` 9-slice, cao 225) nằm dưới `SelectedTab`; `SelectedTab` cao 225 (không nhô lên nữa, chỉ icon nhô); vạch ngăn trong suốt (viền ô thay vạch); icon tab chọn phóng 1.3, nhô lên y 160, chữ ở y 38.
- `LeaderboardPanel.cs`: xếp lại cả trong `OnEnable` – lúc chuyển Shop <-> Home panel lướt qua không còn hiện "Player 0".

**Kiểm tra**: Play, chuyển Home → Shop, dừng ở 0.12 s: ô cam trượt dần, icon Shop to ra + nhô lên, chữ hiện dần, icon Rank thu về – khớp khung 20.44 s của video.

## 06:25 – Bỏ Tiles: một khung chung + vạch ngăn

**Phản hồi**: ô nền từng tab (`Tiles`) làm viền bị lặp, chồng lên nhau.

**Sửa**
- `MenuTabBar.cs`: bỏ field `tiles` và phần cập nhật ô trong `Layout` / `Animate` (về như trước 06:10).
- `MenuTabBar.prefab`: xóa `TabBar/Tiles`; `Background` = `BarBg` 9-slice phủ cả thanh (nền be đặc viền nâu – `TabNormal` chỉ có viền, ruột trong suốt nên trước đó nhìn xuyên ra sân); bật lại vạch ngăn (6x200, nâu `#A86B3D`), vẫn ẩn cạnh tab chọn.

**Kiểm tra**: Play, tab Rank: một khung liền, vạch ngăn giữa các tab thường, ô cam tab chọn không chồng viền; console không lỗi.

## 06:40 – Vạch ngăn nối liền viền, dày đều

**Phản hồi**: vài ô thiếu đường nối phía trên; các vạch dày mỏng khác nhau.

**Nguyên nhân**: vạch cao 200 (đặt từ y 6) nên không chạm viền trên của thanh; vạch 6 đơn vị rơi vào vị trí lẻ pixel nên có vạch 4 px, có vạch 6 px (ảnh 948 px).

**Sửa** (`MenuTabBar.prefab`)
- Vạch ngăn cao 225 (cả thanh), rộng 6, màu `#C67C32` (đúng màu / độ dày viền `BarBg` đo từ sprite).
- `TabBar` thêm `Canvas` con bật Pixel Perfect (+ `GraphicRaycaster` để nút tab vẫn bấm được): viền và vạch bám pixel, dày đều.

**Kiểm tra**: Play, tab Rank: các vạch đều 2 px ở 474 px (= viền ngoài), nối liền viền trên / dưới; console không lỗi.

## 06:55 – Tách prefab con cho Home; dọn override cũ trong scene

**Yêu cầu**: muốn chỉnh riêng từng phần Menu dễ hơn.

**Thêm** (`Game/Prefabs/Menu/`)
- `HomeScene.prefab`: toàn bộ nền động (Sky, Top, World) – trước là object `Scene` trong `HomePanel`.
- `GhostMascot.prefab`: nhóm `GhostShadow` + `Ghost` (nằm trong `HomeScene/World`).
- `LockedMenuButton.prefab`: nút khóa trang trí; 5 nút `Locked_*` trong `HomePanel/UIZone` là instance (override icon, nhãn "Lv X", vị trí).

**Sửa**
- `MainMenu.prefab`: đặt sẵn vị trí panel theo tab Home (Shop −2160, Rank −1080, Home 0, Daily 1080, Collection 2160) và thanh tab ở trạng thái Home – mở scene / prefab là thấy đúng Home, không còn 5 panel chồng nhau.
- Scene `Main`: gỡ 16 override cũ trên instance `Canvas_Menu` (vị trí / bật tắt 3 panel cũ, `SelectedTab`, `Tab_Shop` / `Tab_Home` / `Tab_Daily` và icon theo bố cục 3 tab) – nguyên nhân thanh tab lệch trong Edit mode. Giữ các tinh chỉnh khác (Timer, Amount, Settings, DailyPanel).

**Kiểm tra**: Play: Home giống hệt trước khi tách; console không lỗi. Edit mode: panel / tab đúng vị trí.

## 07:05 – Môi trường Prefab Mode cho UI (1080x1920)

**Phản hồi**: mở `HomePanel` trong Prefab Mode thì bố cục lệch, nền chỉ thấy sân, nút Play nằm giữa.

**Nguyên nhân**: project chưa đặt *UI Environment* cho Prefab Mode → Unity dùng Canvas mặc định không có CanvasScaler, kích thước bằng Game view (474x843 đơn vị) trong khi UI thiết kế theo 1080x1920.

**Thêm / Sửa**
- `_Project/Settings/Scenes/UIPrefabEnvironment.unity`: 1 Canvas Overlay + CanvasScaler 1080x1920, match width (như `MainMenu`), bật kênh TexCoord1 / Normal / Tangent cho TMP.
- `ProjectSettings/EditorSettings.asset`: `Prefab Mode → UI Environment` = scene trên.

**Kiểm tra**: mở `HomePanel` trong Prefab Mode: Canvas môi trường 1080x1920, panel 1080x1920 (bằng lúc chạy).

## 07:20 – Dơi bay đúng animation gốc

**Phản hồi**: dơi bay khác hẳn video.

**Nguyên nhân**: bản gốc (`hlw2026_bat`, anim `idle` 6.33 s) là 3 **mesh biến dạng** – cánh vỗ bằng uốn lưới, cả đàn bay vòng cung từ giữa-phải lên trái, to dần rồi nhỏ lại (như lại gần rồi ra xa), xoay theo hướng bay, mờ ở cuối. Bản cũ chỉ trượt thẳng + lượn sin + co dọc ảnh tĩnh.

**Thêm**
- `Shared/Scripts/UI/UISpriteSequence.cs`: phát chuỗi sprite + vị trí theo vòng lặp (`fps`, `loopFrames`, `firstFrame`, `startOffset`), ẩn Image ngoài khoảng hiện.
- `HalloweenRip/Bg/Bats/doi1..3_sheet.png`: 134 / 113 / 109 khung (30 fps) render từ Spine 4.2 bằng runtime chính chủ (dotnet, ngoài project), mỗi khung kèm tâm theo toạ độ skeleton; import dạng Multiple, cắt lưới.

**Sửa**: `HomeScene.prefab` → `Top`: bỏ `Bat1..3` (`UIFlyAcross`), thêm `Bats` (gốc 450, −600) với 3 con `doi1..3` dùng `UISpriteSequence` (vòng 190 khung). `UIFlyAcross` vẫn dùng cho ma nhỏ đi dạo.

**Kiểm tra**: ghép offline khung 55–155 lên nền mock: đàn bay vòng cung, cánh vỗ, to / nhỏ dần như video; console không lỗi. Người dùng xem trong Play: bay ổn.

## 08:00 – Bảng xếp hạng đủ như bản gốc; sửa SafeAreaZone khi mở Device Simulator

**Phản hồi**: phần Leaderboard thiếu nhiều so với video.

**Sửa**
- `LeaderboardPanel.cs`: 3 tab con Weekly / Players / Teams (nút + lớp `Active`); Weekly có bục, Players chỉ danh sách từ hạng 1 (điểm toàn thời gian), Teams hiện màn khóa; avatar đối thủ / người chơi lấy từ `Profile/Configs/AvatarDatabase`; đồng hồ "Xd Yh" tới hết tuần.
- `LeaderboardRow.cs`: thêm avatar, viền `highlight` cho dòng người chơi (thay cho đổi màu nền).
- `LeaderboardPanel.prefab` dựng lại theo `ModalLeaderBoard` gốc: header cao + 3 nút tab (nút phải lật ngang) + ô đồng hồ có kim lắc; card tuần (rèm, khăn tiêu đề, nút i, tia sáng xoay, 3 trụ `Rank 1..3 W` có rương trôi, avatar khung, ruy băng `Tag 1..3`, tên, ô điểm); dòng 182 px (ô hạng, avatar, tên, logo đội + "---", ô `Score`, viền `Current Player Detect`); màn khóa Teams.
- Ảnh thêm (HalloweenRip/Panels): `SelectedTabButtonMiddle`, `CurrentPlayerDetect`, `CurrentPlayerPointer`, `RankFrame`, `TeamDetailSlot`, `NoTeams`, `ClockArrow`.
- `Shared/Scripts/UI/SafeAreaZone.cs`: khi `Screen.safeArea` vượt kích thước `Screen` (Device Simulator mở cạnh Game view: safe area theo máy giả lập, Screen theo Game view) thì coi như cả màn hình – trước đó anchor ra 2.7 và đẩy cả `UIZone` ra ngoài (Leaderboard trống).

**Kiểm tra**: Play, tab Rank: Weekly / Players / Teams đúng; console không lỗi.

## 08:40 – Nền khung avatar; bảng tuần dùng bot bám đuổi

**Phản hồi**: khung avatar mất nền; muốn bảng xếp hạng kiểu bot như game thật (chơi nhiều lên top 1 nhưng không bỏ xa top 2; bỏ chơi thì bị đẩy xuống nhưng không quá xa).

**Thêm**
- `Game/Art/UI/Halloween/AvatarBg.png`: nền hồng có tia (tự vẽ bằng script, giống nền của avatar gốc) – đặt dưới ảnh mèo trong mọi khung avatar của bảng.
- `Game/Scripts/Menu/WeeklyBots.cs`: hàm thuần tính điểm bot – điểm thô = tốc độ (theo seed tuần) x giờ đã qua; chỉ `above` bot được đứng trên người chơi (trần `maxAhead` giảm dần theo hạng), còn lại giữ dưới; 2 bot ngay dưới bám trong `maxLead` / 2 x `maxLead`; `AboveCount(debt, idleHours)` = nợ + 1 hạng mỗi `hoursPerRank` giờ bỏ chơi, tối đa `maxDrop`.
- `Game/Scripts/Menu/WeeklyContest.cs` + section save `"weeklyContest"`: đếm màn thắng trong tuần (`LevelCompletedEvent`), sang tuần mới về 0; khi thắng: nợ = số bot đang đứng trên − 1 (quay lại leo từng hạng).
- `Game/Tests/Editor/WeeklyBotsTests.cs`: 6 test (đang chơi là top 1 và top 2 bám sát; bỏ 1 ngày bị 3 bot vượt, không quá `maxAhead`; tối đa `maxDrop`; điểm 0; bot vượt không trùng điểm; cùng đầu vào cùng kết quả).

**Sửa**
- `LeaderboardPanel.cs`: `Init(profile, save, clock)`, tab Weekly dùng `WeeklyContest`; đồng hồ theo tuần của contest. `GameBootstrap` truyền `save`, `clock`.
- `LeaderboardPanel.prefab`: dựng lại (lần trước bị Prefab Mode đang mở của người dùng tự lưu đè – Auto Save), thêm `Bg` vào khung avatar, điền sẵn `weeklyBots` (3 / 12 / 8 h / 5 / 0.1–0.8).

**Kiểm tra**: 6/6 test `WeeklyBotsTests` đạt. Play, tuần chưa thắng màn nào: 5 bot đứng trên (12, 10, 8, 6, 4 điểm), người chơi hạng 6; console không lỗi.

## 23:00 – Base popup hướng dẫn sự kiện (nút i), áp cho Weekly Contest

**Phản hồi**: video mẫu bấm nút i xanh của cuộc thi tuần; popup hướng dẫn mọi liveops / sự kiện đều có dạng này nên làm 1 base.

**Thêm**
- `Shared/Scripts/UI/TutorialPopup.cs` (kế thừa `PopupView`): băng-rôn tiêu đề treo dây rơi từ trên xuống, đung đưa (OutBack + OutElastic); mảng `steps` (`content` bật lên, `arrow` vẽ dần nếu Image Filled, không thì bật lên) hiện lần lượt; cuối cùng "Tap to continue" hiện rồi nhịp. Chạm `Dim` khi đang chạy = hiện hết, chạm lần nữa = đóng. `Show(onClose)`.
- `Shared/Scripts/UI/TutorialButton.cs`: nút i mở popup (`popup` gán từ ngoài, như `LuckyWheelButton`).
- `Shared/Prefabs/TutorialPopup.prefab` (base: Dim, Banner (2 dây, mặt 9-slice, Title), Steps trống, TapHint).
- `Shared/Art/UI/Tutorial/`: `TutorialBanner` (băng-rôn vàng, viền cam, 2 đinh), `TutorialRope` (dây thừng tiled), `TutorialArrow` (mũi tên cong) – tự vẽ bằng script.
- `Game/Prefabs/Menu/WeeklyTutorialPopup.prefab` (variant): "Weekly Contest", 3 bước Beat the levels! (`popup_tutorial_2`) → Take the lead! (`IconRank`) → Win rewards! (`GoldChest` + `GlowRays`), 2 mũi tên.

**Sửa**
- `LeaderboardPanel.prefab`: nút `Info` thêm `TutorialButton`.
- `Main.unity`: instance `WeeklyTutorialPopup` (tắt) trong `Canvas_Popup/Popups`, override `popup` của nút Info.
- Docs: [10](../Features/10_UI-Menu-Popup.md), [HowTo_MoRong](../Features/HowTo_MoRong.md) (cách thêm popup hướng dẫn cho sự kiện mới).

**Kiểm tra**: Play: bấm Info → popup mở, băng-rôn rơi, 3 bước hiện lần lượt; chạm 1 lần hiện hết (bước 3 scale 1, mũi tên fill 1, TapHint alpha 1), chạm lần 2 đóng; console không lỗi. Ảnh chụp từ MCP không thấy lớp tối (NoticePopup cũng vậy) – chưa nhìn bằng mắt trong Game view.

## 23:15 – Popup hướng dẫn: hiệu ứng pop thay cho trượt

**Phản hồi**: dùng hiệu ứng pop thay vì trượt.

**Sửa**
- `TutorialPopup.cs`: băng-rôn bật lên tại chỗ (scale 0 → 1, OutBack) thay cho rơi từ trên xuống + đung đưa (bỏ `bannerDrop`); mũi tên luôn bật lên (bỏ nhánh vẽ dần kiểu Filled). Gộp chung hàm `AppendPop`.
- `WeeklyTutorialPopup.prefab`: 2 mũi tên về Image Simple.
- Docs: [HowTo_MoRong](../Features/HowTo_MoRong.md).

**Kiểm tra**: Play, bấm Info: băng-rôn bắt đầu scale 0 đúng vị trí rồi bật lên, bước 1 + mũi tên bật lên lần lượt; console không lỗi.

## 23:30 – Mũi tên hướng dẫn theo mẫu

**Phản hồi**: dùng mũi tên giống ảnh mẫu (Foodie Sizzle).

**Sửa**
- `Shared/Art/UI/Tutorial/TutorialArrow.png` vẽ lại: thân cong dày kiểu quả chuối, đuôi tròn, đầu tam giác to, ruột vàng sáng, viền cam đậm mảnh (240x220).
- `WeeklyTutorialPopup.prefab`: 2 mũi tên 190x174, `preserveAspect`; Arrow2 bỏ nghiêng Z, dời xuống (40, -125) trỏ vào rương.

**Kiểm tra**: Play, mở popup (bản trong scene Main) và chạm hiện hết: 2 mũi tên mới đúng chiều (bước 1 → bục, bục → rương), nền tối hiện; console không lỗi.

## 23:45 – Dim của popup hướng dẫn không nhún khi chạm

**Phản hồi**: chạm vào nền tối thì nó nhún như nút.

**Nguyên nhân**: `Dim` có `Button` (vùng chạm) nên `UIAutoSetup` tự gắn `ButtonPress` khi lưu scene / vào Play.

**Sửa**
- `Shared/Prefabs/TutorialPopup.prefab`: `Dim` có sẵn `ButtonPress` **bỏ tick** (quy ước của `UIButtonPress`: nút đã có ButtonPress thì giữ nguyên, bỏ tick để tắt) → mọi variant thừa hưởng.
- `Main.unity`: gỡ `ButtonPress` tự gắn trên instance `WeeklyTutorialPopup` (revert override).

**Kiểm tra**: Play, mở popup, giả lập nhấn Dim: chỉ còn 1 `ButtonPress` (tắt), Dim giữ scale 1; vẫn chạm để hiện hết / đóng được.

## 23:59 – Dim vẫn lún khi nhấn: bỏ hẳn Button khỏi Dim

**Phản hồi**: nhấn vào Dim vẫn bị co lại (ảnh: vùng tối thu vào ~7% mỗi cạnh = `pressScale` 0.93 của `ButtonPress`).

**Nguyên nhân**: cách bỏ tick `ButtonPress` (23:45) không bền – instance trong scene vẫn bị gắn thêm 1 `ButtonPress` **bật** (added override), vì Dim còn `Button` nên `UIAutoSetup` coi là nút.

**Sửa**
- `TutorialPopup.cs`: cài `IPointerClickHandler` trên gốc (chạm vào Dim nổi lên tới gốc), bỏ trường `tapArea`.
- `Shared/Prefabs/TutorialPopup.prefab`: `Dim` bỏ `Button` và `ButtonPress`, chỉ còn Image chặn raycast → `UIAutoSetup` không gắn gì nữa.
- `Main.unity`: gỡ `ButtonPress` added trên Dim của `WeeklyTutorialPopup`.
- Docs: [HowTo_MoRong](../Features/HowTo_MoRong.md) ghi rõ không thêm Button vào Dim.

**Kiểm tra**: Play, mở popup: Dim không có handler `pointerDown` nào (không thể lún), handler click là `WeeklyTutorialPopup`; chạm sau khi hiện hết thì đóng; lưu scene lại không bị gắn thêm component; console không lỗi.

## 00:10 – Nút tab Weekly / Teams bị lật ngược

**Phản hồi**: hình nút Weekly và Teams đang bị ngược.

**Nguyên nhân**: sprite `TabButtonSide` / `SelectedTabButtonSide` bo tròn ở mép **phải**, nhưng prefab lại lật nút phải (Teams) và để nguyên nút trái (Weekly).

**Sửa**
- `LeaderboardPanel.prefab`: `BtnWeekly` scale X −1 (con `Label`, `Timer` lật lại −1 để chữ / đồng hồ không ngược); `BtnTeams` và `Label` về scale 1.

**Kiểm tra**: Play, tab Rank: Weekly bo tròn mép trái, Teams bo tròn mép phải, chữ và đồng hồ đúng chiều; Prefab Mode đang mở đã nạp bản mới (không dirty).

## 00:35 – Anim rương rau (nút Lv 50) theo video

**Phản hồi**: làm lại anim các liveops, trước tiên là rương ở Home cho giống video (dùng anim / spine có sẵn trong bản rip nếu có).

**Tìm trong `E:\FoodleSizzle`**: không có `.anim` cho nút này; spine `Chest` là rương thưởng (bronze … epic, có khóa) – không phải icon. Đúng icon là spine **`iconHomeLab`** (Spine 4.2.43: `chest`, `chest2`, `nap`, `carot`, `ngo`, `ngo2`, slot clipping `clip`), 2 animation `idle` (1 s) và `start` (2 s). Video: `idle` liên tục, khoảng 12 s một lần `start` (~1.7 s: rương giãn, nắp đóng sập, rương đóng nảy, nắp bật mở, rau trồi ra).

**Thêm**
- Tool dump Spine 4.2 (dotnet, ngoài project) thêm `SkeletonClipping` để cắt rau theo slot `clip` khi nắp đóng.
- `Game/Art/UI/HalloweenRip/Side/ChestAnim.png` (gitignore): 90 khung 167x149 (`idle` 0–29, `start` 30–89, 30 fps), lưới 10 cột, Multiple, Full Rect, không vào atlas. Khung đầu trùng khít `Chest.png` cũ (lệch 13, 14 px).

**Sửa**
- `HomePanel.prefab` → `Locked_Chest/Content/Chest`: `UISpriteSequence` 360 bước (`idle` x10 rồi `start`, 12 s/vòng), cùng vị trí; rect 175x156, pivot (0.5, 0), (−1, −55.8) để khung đầu đè đúng chỗ ảnh tĩnh cũ (tỉ lệ 150/143). Lắc khi bấm của `LockedMenuButton` vẫn dùng (chỉ xoay, không đụng vị trí).
- Docs: [10](../Features/10_UI-Menu-Popup.md).

**Kiểm tra**: ảnh render 90 khung so cạnh video (`Temp/TutorialShots/chest_compare.gif`): cùng nhịp đóng / mở. Play: `Chest` đổi khung theo thời gian (t = 12.2 s → khung `idle` 5), vị trí giữ nguyên; console không lỗi. Chưa xem bằng mắt trên Home (Game view đang ở tỉ lệ ngang).

## 00:50 – Anim vạc hotpot (nút Lv 20)

**Phản hồi**: làm tiếp anim hotpot.

**Nguồn**: spine `BaseGame/icon_hotpotHLW` (Spine 4.2, binary: `1_hotpot`, `water` (mesh), `1_ma`, `1_thia`), animation `idle` (4 s) và `click` (1.33 s). Video chỉ phát `idle` lặp liên tục (không thấy `click`).

**Thêm**
- `Game/Art/UI/HalloweenRip/Side/CauldronAnim.png` (gitignore): 120 khung 162x143 (`idle`, 30 fps), lưới 11 cột, Multiple, Full Rect. Khung đầu khớp `Cauldron.png` cũ (tỉ lệ 0.99, lệch 0, 4 px).

**Sửa**
- `HomePanel.prefab` → `Locked_Cauldron/Content/Chest`: `UISpriteSequence` 120 khung lặp; rect 150.6x132.9, pivot (0.5, 0), (0.3, −49.8) để khung đầu đè đúng ảnh tĩnh cũ.
- Docs: [10](../Features/10_UI-Menu-Popup.md).

**Kiểm tra**: ảnh render so cạnh video (`Temp/TutorialShots/hotpot_compare.gif`); Play: t = 5.1 s → khung 33, vị trí giữ nguyên; console không lỗi. Chưa xem bằng mắt trên Home.

## 01:10 – Anim icon Daily (lịch)

**Phản hồi**: làm tương tự cho Daily.

**Nguồn**: spine `TextAsset/Quest` (Spine 4.2, atlas PMA: `quest`, `paper1`, `paper_fly`, `shadow`, `tick`), animation `idle` (0.67 s) và `click` (1.63 s: dấu tích xanh đóng lên, tờ lịch bị xé rơi xuống, lộ tờ mới). File `Quest.png` cùng thư mục là ảnh khác trùng tên (bảng kẹp giấy); texture đúng là `01_Images_2D/Texture2D/Quest_0.png` (224x224). Video: `idle` lặp, khoảng 15.7 s một lần `click`.

**Thêm**
- `raster.py` (ngoài project) thêm `--pma`: bỏ premultiply texture trước khi dựng.
- `Game/Art/UI/HalloweenRip/Side/CalendarAnim.png` (gitignore): 69 khung 170x267 (cao vì tờ lịch rơi xuống dưới), lưới 12 cột, Multiple, Full Rect. Khung đầu khớp `Calendar.png` cũ (tỉ lệ 1, lệch 24, 18 px).

**Sửa**
- `HomePanel.prefab` → `Daily/Content/Calendar`: `UISpriteSequence` 469 bước (`idle` x21 rồi `click`, ~15.6 s/vòng); rect 183.5x288.1, pivot (0.5, 0), (−9.15, −179.2) để khung đầu đè đúng ảnh tĩnh cũ; Image không nhận raycast nên rect cao không chặn bấm.
- `MenuShortcutButton` của `Daily`: bỏ `icon` (trước là `Calendar`) – kiểu nhấc + lắc mời bấm sẽ ghi đè vị trí mà `UISpriteSequence` đặt mỗi khung; video gốc cũng chỉ có anim spine. Chấm đỏ (`badge`) giữ nguyên.
- Docs: [10](../Features/10_UI-Menu-Popup.md).

**Kiểm tra**: ảnh render so cạnh video (`Temp/TutorialShots/daily_compare.gif`); prefab đã lưu đúng (469 khung, khung 420 = `click` đầu, 468 = `click` cuối); console không lỗi. Chưa xem trong Play (Unity đang ở Play mode từ trước, bản đang chạy dùng prefab cũ – cần dừng rồi Play lại).

## 01:30 – Anim icon No Ads và hộp quà Starter

**Phản hồi**: làm tương tự cho No Ads và hộp quà.

**Nguồn**: spine `TextAsset/No Ads` (PMA: vòng đỏ, gạch chéo, chữ A / D / S) và `TextAsset/Gift_box` (PMA: `giftbox`, `blink`), đều có `idle` (0.67 s) và `click` (No Ads 1.37 s: chữ ADS phồng lên nảy, gạch chéo vẽ lại; Gift 0.6 s: hộp nhún nảy + lấp lánh). Texture cùng thư mục khớp atlas. Video: No Ads ~11.5 s một lần, hộp quà ~11.8 s một lần.

**Thêm** (gitignore, `HalloweenRip/Side/`)
- `NoAdsAnim.png`: 61 khung 204x204, lưới 10 cột. Khung đầu khớp `NoAds.png` (tỉ lệ 1, lệch 31, 33).
- `GiftAnim.png`: 38 khung 157x158, lưới 12 cột. Khung đầu khớp `Gift.png` (tỉ lệ 0.995, lệch 14, 15).

**Sửa**
- `HomePanel.prefab`:
  - `NoAds/Content/Tv`: `UISpriteSequence` 341 bước (`idle` x15 rồi `click`), `startOffset` 3 s; rect 218.6x218.6, pivot (0.5, 0), (1.07, −94.82).
  - `Starter/Content/Chest`: 358 bước (`idle` x17 rồi `click`), `startOffset` 7 s; rect 164.4x165.4, (−6.43, −59.71).
  - `MenuShortcutButton` của `NoAds`, `Starter`: bỏ `icon` (lý do như Daily).
- Docs: [10](../Features/10_UI-Menu-Popup.md).

**Kiểm tra**: GIF so cạnh video (`Temp/TutorialShots/noads_gift_compare.gif`); Play: cả 5 icon (rương, vạc, Daily, No Ads, quà) đổi khung theo thời gian, vị trí giữ nguyên; console không lỗi.

## 02:30 – Tổ chức lại dự án theo mẫu Snacky_Dash

**Yêu cầu**: tổ chức cấu trúc thư mục và cách dựng scene giống dự án `E:\Git\Snacky_Dash`. Người dùng chọn: theo hẳn Snacky (gộp asmdef), giữ 1 scene sắp kiểu Snacky, giữ tên gốc `_Project`.

**Thư mục** (mọi di chuyển bằng `AssetDatabase.MoveAsset`, 125 lượt, giữ GUID):
- `Core/*`, `Features/*/Scripts`, `Game/Scripts`, `Shared/Scripts/UI` → `Runtime/{Core, Gameplay, Economy, Shop, Profile, Settings, LiveOps/{DailyReward, DailyChallenge, LuckyWheel, WeeklyContest}, UI/{Framework, Home}, App, Save, Cheat}`; mọi `Editor/` → `Runtime/Editor/<nguồn>`, mọi `Tests/` → `Runtime/Tests/<nguồn>`.
- Mọi `Configs/` → `Data/Config/`, level → `Data/Levels/`.
- Prefab → `Prefabs/InGame/`, `Prefabs/UI/{Popups, Screens/Home, Screens/Game, Widgets(/<mảng>)}`.
- Ảnh / font / âm / shader → `Source/Art/{UI/<nhóm>, InGame/<nhóm>, Avatars, Fonts, Atlases}`, `Source/Audio`, `Source/Effect/{Shaders, Materials}`. Mỗi thư mục được atlas gói dời nguyên khối nên 7 atlas vẫn gói đúng; ảnh UI vẫn dưới `…/Art/UI/` cho preset ASTC 4x4.
- README từng feature → `Assets/Docs/Modules/<F>.md`. Xóa 4 thư mục rỗng `Core`, `Features`, `Game`, `Shared`.

**Assembly**: 14 asmdef + 3 asmref → `PawBlock.Runtime` (`Runtime/`), `PawBlock.Editor` (`Runtime/Editor/`), `PawBlock.Tests` (`Runtime/Tests/`). Không asset nào lưu tên assembly (SerializeReference / UnityEvent) nên không vỡ dữ liệu. Namespace giữ nguyên. Đánh đổi: compiler không còn chặn phụ thuộc sai chiều (ghi ở ProjectConventions 11, Dependencies).

**Sửa code**
- Hằng đường dẫn trong 12 tool Editor + `FeelPlayModeTests` (Level tools, `GameplayVisualsSetup`, `AudioSetup`, `GameConfigSetup`, `GameFontSetup`) → đường dẫn mới; đã soát mọi hằng `Assets/_Project/…` đều trỏ tới chỗ có thật (`Data/Levels/Generated` do tool tự tạo).
- `AudioSetup.FindRoot`: tìm theo tên trong cả cây scene (AudioManager, 3 Canvas không còn ở gốc).
- `.gitignore`: thư mục rip → `/Assets/_Project/Source/Art/UI/Home/HalloweenRip/`.

**Scene `Main`**: `[Managers]` (GameBootstrap, Manager, AudioManager) · `---- MÀN CHƠI ----` (bg, SpawnGrid, SelectionBox, DragCountBubble, TutorialHand) · `---- ĐIỀU KHIỂN ----` (Main Camera, Global Light 2D) · `---- GIAO DIỆN ----` (Canvas_HUD, Canvas_Menu, Canvas_Popup) · EventSystem. Chỉ đổi cha (nhóm ở gốc toạ độ), không đổi component.

**Docs**: viết lại [ProjectStructure](../ProjectStructure.md) (mảng code, thư mục, scene), [ProjectConventions](../Refactor/ProjectConventions.md) mục 6, 11–14, [Dependencies](../Refactor/Dependencies.md); đổi đường dẫn trong 25 tài liệu Features / Modules (RoadMap, RefactorPlan, TaskBreakdown giữ nguyên vì là lịch sử). Dò link nội bộ: 0 link hỏng.

**Kiểm tra**: biên dịch sạch (3 assembly); 185/185 test `PawBlock.Tests` đạt (3 test hỏng sẵn của package `UnitySkills`, không liên quan); quét mọi prefab + scene: 0 missing script, 0 missing reference; 7 atlas gói đúng thư mục; Play: Menu chạy, 5 icon anim đúng khung, bấm PLAY vào màn chơi (HUD bật, level chạy), console không lỗi.

## 04:50 – Shop theo video Foodie Sizzle

**Phản hồi**: cập nhật Shop panel giống video (vd3).

**Nguồn**: prefab `ModalShop` của bản rip (dump cây + cắt 43 sprite từ atlas `UIShop` / `_NewUI2`): mái sọc `UITop`, ruy băng `Frame1/2/3`, khung gói Halloween `shop_2` + bí ngô + hồn ma + nhãn `tag` (X5 VALUE), khung gói giới hạn `UIMain` + `PatternTile` + đầu bếp `Char` + nhãn `TagSale`, nền 6 hạng gói (`BigBundle`, `GreatBundle`, `UltraBundle`, `SuperiorBundle`, `Legendary Bundle`, `NoAds` + ô vật phẩm), ảnh gói, `CoinLV1..6`, ô coin `CoinUIFix`, nút giá `NewBtnGreen`, đồng hồ. Ảnh nằm ở `Source/Art/UI/Home/HalloweenRip/Shop/` (gitignore), viền 9-slice giữ như gốc. Bố cục / kích thước lấy từ `ModalShop` (cùng canvas 1080).

**Sửa code**
- `ShopDatabase.cs`: `ShopSection` thêm cuối `Event`, `Limited`; `ShopProduct` thêm `itemsBackground`, `endsAt` + `EndsAt(now)`; `PriceText` → `289.000 ₫` (vi-VN).
- `ShopCardView.cs`: `itemsBackground`, `coinItem` (ẩn khi không có coin), `timerText` + `timerFormat` đếm ngược mỗi giây (`FormatLeft`: `1D2H` / `11H14M` / `5M30S`, theo giờ máy, chỉ hiển thị).

**Prefab**
- Dựng lại `ShopBundleCard`, `ShopRemoveAdsCard`, `ShopCoinCard`; thêm `ShopEventCard`, `ShopLimitedCard` (thẻ có ruy băng + đồng hồ, cả thẻ là nút mua). Font `VN-Lilita-One` (rip).
- `ShopPanel.prefab`: nền tối, header `UITop` + "SHOP" thay 4 tấm mái cũ, tắt thanh coin / năng lượng (video không có), bỏ header Remove Ads, ruy băng `BUNDLES` / `COINS`, Content padding 40 / 300 / 400 spacing 60, lưới coin 300x400 3 cột; `slots` 6 section (Event, Limited, RemoveAds ở đầu; NoAdsBundle, Bundle sau `BUNDLES`; Coin trong `CoinGrid`).

**Dữ liệu** (`ShopDatabase.asset`): thêm gói `X2 Halloween` (Event, 289.000 ₫, 2000 coin + 2 mỗi booster + 2h vô hạn, hết 2026-11-01); Starter Pack → `Limited Pack` (section Limited, nhãn X5 VALUE); đổi tên / nền / ảnh: No Ads, No Ads Bundle, Big / Great / Super / Premium / Legendary Pack; icon 6 gói coin `CoinLV1..6`. Số coin / booster / giá các gói cũ giữ nguyên.

**Kiểm tra**: render `Canvas_Menu` ra ảnh dọc 1080x1920 (camera tạm, Game view người dùng đang ngang): đủ các mục như video; bấm mua ô coin → popup Purchase Failed (IAP giả); 185/185 test đạt; console không lỗi.

## 05:10 – Shop: gộp gói Halloween vào Special Pack, thay ảnh đầu bếp

**Phản hồi**: theme chính đã là ma nên không cần mục Halloween Pack, cho thẻ đó làm Special Pack; ảnh đầu bếp thay bằng ảnh hợp Halloween (tìm trong rip, không có thì gen bằng Gemini).

**Sửa**
- Prefab `ShopEventCard` → đổi tên `ShopSpecialCard` (giữ GUID): ruy băng cam `Frame3` "SPECIAL PACK", bỏ chữ "x2" ở bí ngô. Xóa `ShopLimitedCard` (thẻ đầu bếp). `ShopPanel.slots`: Limited và Event đều dùng `ShopSpecialCard` (Event để dành cho sự kiện sau).
- `ShopDatabase.asset`: bỏ gói `X2 Halloween`; `Limited Pack` (66.000 ₫) hiện trên thẻ Special.
- Ảnh đầu bếp còn lại là `SuperiorBundle3` của Premium Pack. Atlas Halloween của rip không có nhân vật cầm coin hợp (chỉ có capybara phù thủy – không phải linh vật game) → gen bằng Gemini (3.6 Flash; mô hình Argon bị khóa, cần nâng cấp gói nên không dùng): đính kèm `GhostMascot.png`, yêu cầu giữ nguyên nét / màu / mũ, hồn ma ôm đống coin, nền trắng. Tách nền bằng loang từ mép (giữ trắng bên trong), cắt sát, 512 px → `Source/Art/UI/Home/Halloween/GhostCoins.png` (tự gen nên commit như `GhostMascot`). Premium Pack `icon` = `GhostCoins`.

**Kiểm tra**: không còn prefab / asset nào dùng `Char`, `SuperiorBundle3`; render Shop: Special Pack (bí ngô, hồn ma, X5 VALUE, đồng hồ) đứng đầu, Premium Pack là hồn ma ôm coin; console không lỗi.

## 05:40 – Shop: sao lấp lánh, icon tiền, bỏ đồng hồ, thẻ No Ads, thẻ gói 2 ô

**Phản hồi** (so với game gốc): quanh icon mỗi gói có sao lấp lánh; ký hiệu ₫ lỗi font; bỏ "TIME LEFT"; thẻ No Ads thiếu vầng sáng sau icon và icon đè lên phần nền dưới; thẻ gói chia 2 ô: booster + số lượng (sau này thêm booster) và lượng vàng.

**Sửa code**
- `ShopDatabase.cs`: bỏ `endsAt` / `EndsAt`; `PriceText` chỉ còn số (`289.000`).
- `ShopCardView.cs`: bỏ đồng hồ (`timerText`, `Update`); `itemsBackground` → `itemsBackgrounds[]` (tô cả ô booster và ô vàng); số booster hiện trơn "2" như bản gốc.

**Thêm**: `Source/Art/UI/Common/Icons/icon_money.png` (tờ tiền trắng tự vẽ, thay ₫ – font VN-Lilita tự tạo dùng glyph đ gạch, khác bản gốc); `HalloweenRip/Shop/0_Coin.png` (đồng coin vàng cho ô vàng).

**Prefab**
- `ShopBundleCard` dựng lại: icon chính bên trái (vầng sáng + sao), `BoosterPanel` 280x280 (GridLayout 2 cột 115 px: Hint, Freeze, Reveal, Infinite), `CoinPanel` 170x280 (coin + số); giá = `PriceRow` (HorizontalLayout: số + icon tiền).
- `ShopRemoveAdsCard`: icon ADS 220 px nằm trong vùng trên (không đè dải dưới / tiêu đề) + `LightEffect` phía sau + sao.
- `ShopCoinCard`: giá + icon tiền, sao quanh đống coin.
- `ShopSpecialCard`: bỏ hàng đồng hồ (thẻ cao 700), giá + icon tiền, sao quanh bí ngô.
- `UISparkles` (có sẵn): sao `fx_sparkle`, nghỉ 0.1–0.7 s, hiện 0.8 s; `ShopPanel.slots` gán lại prefab sau khi dựng lại.

**Kiểm tra**: render Shop 1080x1920 đúng bố cục 2 ô, giá + icon tiền, không còn đồng hồ; Play: 14 cụm sao, cụm trên thẻ đang thấy chạy (4/4 sao hiện lúc đo); 185/185 test đạt; console không lỗi.

## 06:40 – Tab 4: Daily Challenge → Teams (Đội, giống guild)

**Yêu cầu**: tab thứ 4 thay Daily Challenge bằng Teams (giống guild, theo video bản gốc: Các đội / Tìm kiếm / Tạo); Daily Challenge để sau làm liveops.

**Nguồn**: `ModalTeam`, `ClanCell`, `PopupTeamInfo` của rip (dump cây + cắt sprite); 20 logo `logo__1..20` (`TeamLogoData` của rip rỗng, logo nạp kiểu Addressables). Video chỉ có chuyển 3 tab con (mờ dần, dòng hiện lần lượt); màn thông tin đội / My Team / Join–Leave làm theo `ModalTeam` (phần TeamInfo). Bỏ chat, quà đội, kho báu đội.

**Thêm**
- `Runtime/Teams/` (`PawBlock.Features.Teams`): `TeamsDatabase` (SO), `TeamInfo` / `TeamMember` / kết quả, `TeamSave` (section `"teams"`), `TeamService` (đội giả cố định theo seed, `Top`, `Search`, `Join` / `Leave`, `Create` trừ coin, `FakeMembers` theo hash ổn định của id), `TeamRow`, `TeamMemberRow`.
- `Runtime/UI/Home/TeamsPanel.cs`: 4 trang (Teams / Search / Create / Info), tab 3 thành My Team khi đã vào đội, nút Join xanh / Leave đỏ, chuyển trang mờ dần + dòng hiện lần lượt (DOTween, thời gian thật), thông báo lỗi qua `GameFlowUI.ShowNotice`.
- `Runtime/Tests/Teams/Editor/TeamServiceTests.cs`: 7 test (seed cố định, tìm kiếm, vào / rời, đầy / thiếu cấp, tạo trừ coin + lưu, thiếu coin, bước cấp).
- `Data/Config/TeamsDatabase.asset` (40 tên đội, 8 mô tả, 24 tên thành viên, 20 logo, giá 3000, sức chứa 50, cấp 1–200 bước 5); `GameConfig.teams`.
- `Prefabs/UI/Screens/Home/TeamsPanel.prefab`: chép header + 3 nút tab từ `LeaderboardPanel` (cùng bộ ảnh), các trang dựng bằng ảnh rip `HalloweenRip/Teams/` (gitignore).

**Sửa**
- `MainMenu.prefab`: `Panels` thay instance `DailyPanel` bằng `TeamsPanel` (cùng vị trí), `MainMenuNavigator.menuPanels[3]`.
- `MenuTabBar.prefab`: `Tab_Daily` → `Tab_Teams` (icon `IconTeam`, nhãn "Teams"), gỡ `DailyTabBadge`, tắt khóa / chấm đỏ.
- `GameBootstrap`: tạo `TeamService`, `teamsPanel.Init(...)`; `dailyChallengePanel` thành tùy chọn (có thì Init). `MainMenuNavigator.DailyTab` → `TeamsTab`; `ScreenFlow.ShowMenu` luôn về Home.
- Docs: [17_Teams](../Features/17_Teams.md), [Modules/Teams](../Modules/Teams.md), 00, 10, 11, 16, [Modules/DailyChallenge](../Modules/DailyChallenge.md), README, ProjectStructure.

**Kiểm tra**: 192/192 test đạt; quét prefab + scene 0 missing; Play: danh sách 30 đội, tìm "bri" ra 5 đội, View → Join (sĩ số +1, nút Leave đỏ, tab My Team), Leave, tạo "Ghost Squad" (trừ 3000 coin, cấp 5, người chơi là đội trưởng có viền); sau thử đã trả save về (rời đội, coin về 1715); console không lỗi.

## 2026-10-26 09:30 – Teams: Team Info đầy đủ, My Team (bảng tin, chat, xin mạng); sửa nút mũi tên

**Phản hồi**: bổ sung theo ảnh bản gốc (Thông tin đội có điểm / hoạt động / hạng, huy chương top 3; Đội của tôi có bảng tin, Tin nhắn, Yêu cầu mạng); nút prev của Teams không bấm được.

**Lỗi nút**: `LevelPrev` và nút quay lại cũ xoay 180° trục Y để quay đầu mũi tên; `GraphicRaycaster` bật *Ignore Reversed Graphics* nên bỏ qua hình bị lật → không nhận chạm. Sửa: nút đứng thẳng (Image trong suốt nhận chạm), hình mũi tên là con được lật. Kiểm bằng raycast thật qua EventSystem: Next 1 → 5 → 10, Prev 10 → 5.

**Sửa code**
- `TeamInfo.cs`: `activity` (`TeamActivity`), `TeamMember.LastSeenDays`, `TeamFeedItem` / `TeamFeedType`.
- `TeamSave.cs`: `feed`, `lastLifeRequest`. `TeamsDatabase.cs`: `feedMax`, `messageMaxLength`, `lifeRequestMax` 5, `lifeRequestCooldownHours` 4, `lifeHelpMinutes` 20, `greetings`.
- `TeamService.cs`: nhận `IClock`, tên / avatar người chơi, `grantLives`; `ScoreOf`, `RankOf`, bảng tin tạo sẵn khi vào đội, `SendMessage`, `RequestLives`, `UpdateLifeHelps` (đồng đội giả giúp theo thời gian, cộng mạng), `FormatAgo`, `FormatCooldown`; thành viên giả xếp theo cấp, đội trưởng là 1 trong 3 người đầu.
- `TeamMemberRow.cs` (huy chương, Leader / "1d ago"), `TeamFeedRow.cs` (mới).
- `TeamsPanel.cs`: trang My Team; Team Info / My Team thay 3 tab bằng thanh tiêu đề (+ X ở Team Info); đã vào đội thì tab mở thẳng My Team; bật trang trước khi nạp để layout / cuộn đúng; bảng tin cuộn xuống cuối (lặp 3 frame vì chữ tự co giãn đổi chiều cao), dòng cũ tắt ngay trước khi Destroy.
- `GameBootstrap`: truyền `clock`, profile, `energyManager.Add` cho `TeamService`.
- `TeamServiceTests`: +4 test (bảng tin vào / rời, gửi tin, xin mạng + chờ + giúp không quá số thành viên, hạng). Tổng 196 test.

**Prefab / ảnh**: thêm ảnh rip `HalloweenRip/Teams/` (Chat Box, My Chat, Time Stamp, Time Bar, GreenProcess, Header Team a, NewBtnYellow / Green / Gray, Pos 1–3, i_message, i_heart, 7_Time, X) với viền 9-slice như gốc; `Source/Art/UI/Common/Icons/icon_dot.png` (tự vẽ, tô màu Activity / Online). `TeamsPanel.prefab`: nhóm `Tabs`, `TitlePlate`, `CloseButton`; dựng lại `InfoPage`, thêm `MyTeamPage`.

**Kiểm tra**: 196/196 test; Play: đội đầy hiện Full; vào "Zombie Kittens" → Leave đỏ, 50/50, hạng 2; X → My Team; gửi "Hi team! Boo :)" (bong bóng phải); Request → nút xám 3h59m; tua 40 phút → 2/5 mạng; đáy bảng tin trùng đáy khung. Sau thử đã trả save (rời đội, xoá bảng tin, mốc xin mạng); console không lỗi.

## 2026-10-26 11:00 – Teams: thanh tiêu đề như bản gốc, viền dòng mỏng

**Phản hồi** (video bản gốc): Team Info không to như vậy, chỉ có dải nâu bo tròn phía trên + tấm "Team Info", không có phần sáng; viền dòng thành viên / bảng tin quá dày.

**Sửa**
- `TeamsPanel.cs`: `headerTitle` → `listHeader` (`FormerlySerializedAs`) trỏ cả khung `Header` của 3 tab, ẩn ở Team Info / My Team; `joinLabelMaterial` / `leaveLabelMaterial` đổi viền chữ nút.
- `TeamsPanel.prefab`: `TitlePlate` = dải `Header_Team` (top +277, cao 411, như `ModalTeam` gốc) + con `Plate` (`Header_Team_a`, rộng 580) + `Title`; X đặt (-100, -64). `InfoPage` / `MyTeamPage` bắt đầu cách đỉnh 225 (trước 400). Dòng đội / thành viên dùng `PosBar_Thin`, dòng thông báo `Time_Stamp_Thin` cao 110.
- Ảnh (rip, gitignore): `HalloweenRip/Teams/Header_Team.png`; `PosBar_Thin.png`, `Time_Stamp_Thin.png` vẽ lại viền mỏng từ ảnh gốc (giữ hình ngoài, tô lại vành theo khoảng cách tới mép). Material `Font/VN-Lilita-One SDF - Outline Red.mat`.

**Kiểm tra**: 196/196 test; Play: Team Info (Join / Leave), My Team chụp so với video; không còn tham chiếu rỗng trong `TeamsPanel`. Save đã trả lại sau khi thử.

## 2026-10-26 12:30 – Teams: menu thành viên, hỏi lại khi rời đội

**Yêu cầu**: như video bản gốc – bấm thành viên hiện menu Xem hồ sơ / Xoá thành viên; Rời đi hỏi "Rời đội?" Có / Không.

**Sửa code**
- `UI/Framework/ConfirmPopup.cs` (mới, dùng chung): tiêu đề, câu hỏi, Yes / No / X; chỉ Yes gọi `onYes`. `GameFlowUI.ShowConfirm` (thiếu popup thì làm luôn).
- `TeamMemberRow`: `Button` + event `Clicked`, `Member`, `AvatarRect`.
- `TeamsPanel`: Leave → `ShowConfirm` rồi mới rời; menu thành viên (lớp chặn trong suốt + khung `Detail_Popup` bật ra OutBack dưới avatar, không tràn khỏi trang), View Profile (`OpenProfile` cho mình, hồ sơ giả theo tên cho thành viên giả), Remove Member mờ 45 % và không bấm được (đội giả). Đổi trang / tắt panel thì đóng menu.

**Prefab / ảnh**: `Prefabs/UI/Popups/ConfirmPopup.prefab` (từ NoticePopup: giữ Dim, khung `Team_Infor_Bar` ppum 1.45, X `icon_close`, nút `Green_Button_Small` / `Red_Button_Small`), đặt trong `Canvas_Popup/Popups` cạnh NoticePopup, `GameFlowUI.confirmPopup` trỏ tới. `TeamsPanel.prefab`: `InfoPage/MemberMenuBlocker`, `InfoPage/MemberMenu`; `MemberTemplate` nhận chạm (Image raycast + Button). Ảnh rip mới: `HalloweenRip/Teams/Detail_Popup.png`, `icon_close.png`.

**Lỗi gặp**: chữ tạo bằng code có `m_fontSize: -99` → TMP coi là chưa khởi tạo, lúc chạy nạp mặc định (cỡ tối đa 72, bật xuống dòng) nên "Remove Member" xuống 2 dòng. Sửa: đặt `fontSize` cụ thể. (`GameplayHUD.prefab` có 2 chữ cũng như vậy từ trước, chưa đụng.)

**Kiểm tra**: 196/196 test; Play bằng raycast thật qua EventSystem: bấm thành viên → menu dưới avatar; View Profile → hồ sơ "Ember" cấp 411; bấm ra ngoài → đóng; Leave → popup; No / X giữ đội; Yes rời đội về tab Teams. Không tham chiếu rỗng; save đã trả lại.

## 2026-10-26 13:00 – Teams: bảng tin My Team chạy lên dưới banner

**Yêu cầu**: cho khung bảng tin (Viewport) cao lên, chạy lên dưới banner như bản gốc.

**Sửa** (`TeamsPanel.prefab`, `MyTeamPage`): `Feed` Top 205 → 100 (chui dưới banner 90 px; Viewport / Scroll giãn theo Feed); `Banner` chuyển xuống sau `Feed` trong Hierarchy để vẽ đè lên bảng tin; `Feed/.../Content` padding trên 10 → 100 để cuộn lên đầu thì dòng đầu nằm ngay dưới banner.

**Kiểm tra**: Play – cuộn cuối: dòng cũ trượt xuống dưới banner như bản gốc; cuộn đầu: dòng đầu không bị che.

## 2026-10-26 13:30 – Hồn ma Home không còn là Button

**Yêu cầu**: hồn ma giữa Home không phải nút.

**Sửa**: `GhostMascot.prefab` (`Ghost/Mascot`) bỏ component `Button`; `GhostMascot.cs` nhận chạm bằng `IPointerClickHandler` (qua Image Raycast Target của chính object), bỏ trường `button` / `RequireComponent(Button)`. Chạm vẫn nhún bẹp + lắc như cũ, nhưng không còn là nút (UIAutoSetup không gắn ButtonPress / tiếng nút).

**Kiểm tra**: Play – raycast thật trúng `Mascot`, chạy hiệu ứng 0.5 s; console sạch.

## 2026-10-26 14:00 – Sửa chữ "Level n" trên nút Play ở level Hard / Very Hard

**Lỗi**: level khó, chữ "Level n" vẫn viền xanh lá của nút thường mà mặt chữ lại đổi sang đỏ / tím tối (`playLevelTextColor` – kiểu cũ khi nút còn chữ PLAY to, "Level n" là dòng phụ) → chữ tối, viền xanh trên nền đỏ / tím.

**Sửa**: `DifficultyStyle.playTitleOutline` → `playLevelOutline` (`FormerlySerializedAs`), bỏ `playLevelTextColor`; `PlayButtonView` đổi material viền của `levelText` (mặt chữ giữ trắng), bỏ trường `playText` (chữ PLAY đã ẩn). `Data/Config/DifficultyTheme`: Hard = `VN-Lilita-One SDF - Outline Red`, Very Hard = `Outline Purple` (cùng font với chữ Level; material nằm trong HalloweenRip như `Outline Green` đang dùng).

**Kiểm tra**: Play – Level 38 (Hard) chữ trắng viền đỏ, Level 48 (Very Hard) chữ trắng viền tím; trả lại level đang chơi, không đụng save.

## 2026-10-26 16:00 – Màn chơi theo theme Halloween (theo mockup đã duyệt)

**Yêu cầu**: màn chơi không hợp theme Halloween; đổi asset (trừ booster) theo mockup đã duyệt.

**Asset tự vẽ** (script Python, commit được):
- `Source/Art/InGame/Gameplay/Halloween/`: `Cell_Stone` (ô trống, đá tím đậm), `Cell_Clue` (ô số, sáng hơn), `Cell_Mystery` ("?", tím), `Cell_Fuzzy` (số mơ hồ E / O / > / <, cam) – 169 px, ppu 150 như `CellBG`; `BoardFrame_HLW` (viền nâu – kem, lòng tím, 9-slice 40, ppu 80).
- `Source/Art/UI/Gameplay/Halloween/`: `LevelFrame_HLW` (kem, lòng tím), `LevelFrame_Hard` / `LevelFrame_VeryHard` (thêm tab đỏ / tím), `PumpkinClock`, `SoftGlow` (quầng nến).

**Asset từ rip** (`HalloweenRip/Gameplay/`, chỉ local): `GameBG` (render sân nhà ma của `HomeScene` bỏ hồn ma / ma nhỏ / chổi, mờ nhẹ, ám tím, tối 4 góc, sương đáy, dơi), `TimeBarFrame` (`HLWHomeProcess`, viền 66/52), `TimeBarFill` (`LvProgress2` xoay ngang), `WebCorner`; dùng thêm `Play/Spider`, `Top/Setting`, `Bg/candleL/R`, font Lilita.

**Sửa**:
- Code: `CellView` thêm `clueSprite` / `mysterySprite` / `fuzzySprite`, đổi ô nền theo loại ô số (ô khóa giữ ô trống); `BoardDecor` mới (đồ trang trí bám góc `BoardFrame`, đặt lại khi cỡ khung đổi, nhấp nhô nhẹ).
- Scene: `bg` → `GameBG` (order -5), tắt `Pattern`; `BoardFrame` → `BoardFrame_HLW`; thêm `---- MÀN CHƠI ----/BoardDecor` (`Ghost` = `GhostMascot` order -2 ló sau góc trái trên; `Web` order 30 góc phải trên). Bỏ override sprite nút cài đặt của `Canvas_HUD` (khung3 / btn_setting) để nhận nút kem mới.
- `Cell.prefab`: ô nền `Cell_Stone` + 3 sprite ô số. `Fredoka-Bold SDF - Cell Number.mat`: viền / bóng chữ số đen → tím đậm.
- `GameplayHUD.prefab`: khung LEVEL `LevelFrame_HLW`; `HardFrame` 420x270 (khung + tab), tab chữ 34; `LevelHUD.roundedBackground` bỏ (khung khó vẫn dùng nền thường); "LEVEL n" + giờ font Lilita viền tím, giờ màu vàng cam (`TimerUI.normalColor`), icon `PumpkinClock`; thanh sao khung `TimeBarFrame` (Sliced, ppum 1.3, 640x86), ruột `TimeBarFill`, FillArea 588x36; nút cài đặt `Setting` (kem, ẩn `Gear`); `TopPanel/Spider` (tơ + nhện, `UIFloat`); `DownPanel/CandleL/R` (nến + `SoftGlow` vàng, `UIFlicker`).
- `DifficultyTheme.hudFrame`: Hard → `LevelFrame_Hard`, Very Hard → `LevelFrame_VeryHard`.

**Kiểm tra**: Play – level 38 (Hard), level 7 (thường), level 48 (Very Hard) khớp mockup; băng HARD LEVEL vẫn chạy; trả lại level đang chơi. Test 196/196.

## 2026-10-26 16:20 – Hồn ma Home không nhận chạm nữa

**Yêu cầu**: chạm vào hồn ma ở sảnh vẫn có phản ứng (nhún); hồn ma chỉ để trang trí.

**Sửa**: `GhostMascot.prefab` bỏ component `GhostMascot`, tắt Raycast Target của `Mascot`; xoá `Runtime/UI/Home/GhostMascot.cs` (không còn chỗ nào dùng). Gỡ thêm `ButtonPress` còn gắn trên `Mascot` (do `UIAutoSetup` thêm hồi còn là Button, nằm ở override của `HomePanel` / `MainMenu` / scene) – đây là thứ làm con ma vẫn nhún khi nhấn. Hồn ma vẫn trôi lơ lửng (`UIFloat`), chạm xuyên qua.

## 2026-10-27 09:00 – Sửa chữ "Level n" trên nút Play bị tối sau khi chơi xong ván

**Lỗi**: bấm Play → chơi xong → về Home: chữ "Level n" tối màu.

**Nguyên nhân** (`ButtonPress`): nhấn thì tô tối các hình con (CrossFadeColor 0.07 s), thả tay thì sáng lại dần và xoá danh sách `_tinted`. Nút Play làm ẩn màn Menu ngay trong frame click nên CrossFade sáng lại bị dừng giữa chừng (coroutine dừng theo object); `OnDisable` gọi `Tint(false)` nhưng danh sách đã rỗng → chữ kẹt màu tối (0.78) tới lần sau.

**Sửa**: `ButtonPress.OnDisable` gọi `ClearTint()` – trả màu trắng ngay (giữ alpha) cho mọi `tintTargets` (trừ hình Button tự tô). Áp dụng cho mọi nút làm ẩn màn khi bấm.

**Kiểm tra**: Play – nhấn giữ nút Play (chữ 0.78) → thả → vào ván → về Menu: chữ "Level 43" màu 1.0.

## 2026-10-27 09:40 – Vuốt tab Main Menu: không kéo quá 1 tab, Home vuốt được ở mọi chỗ

**Lỗi 1**: kéo chuột dài ở tab giữa thì trượt quá tab bên cạnh, lộ màn chơi phía sau (chỉ 2 panel kề bên được bật). **Sửa**: `MainMenuNavigator.Swipe` kẹp độ lệch trong ±1 bề rộng panel.

**Lỗi 2**: ở Home phải đặt tay lên nút Play mới vuốt được – nền Home toàn hình trang trí không nhận chạm (trước đây hồn ma là nút nên nhận thay). **Sửa**: `HomePanel.prefab` bật Raycast Target cho Image trong suốt ở gốc panel (như các panel khác) → kéo ở chỗ trống tới được `MenuSwipe`.

**Kiểm tra**: Play – 3 điểm trống trên Home đều tới `MenuSwipe` (Panels); ở tab Teams `Swipe(-3000)` dừng ở Collection (x = 0), `Swipe(+3000)` dừng ở Home.

## 2026-10-27 10:30 – Shop: thẻ Limited Pack sống động như video; sao lấp lánh thành Particle System

**Yêu cầu** (video4): bí ngô nhún, 2 hồn ma lắc qua lại, vầng sáng bung ra từ bí ngô, dơi bay qua lại; sao lấp lánh đổi sang Particle System (lo `Update` nặng).

**Sửa `ShopSpecialCard.prefab`**:
- `Pumpkin`: pivot đáy (giữ nguyên vị trí hiển thị), `UIFloat` squash (0.06, 0.08) chu kỳ 0.9 s → nhún bẹp – giãn quanh đáy. `Sparkles` chuyển ra ngoài `Pumpkin` (không bị co giãn theo).
- `GhostLeft` / `GhostRight`: `UIFloat` lắc ±9° (2.2 / 2.6 s), trôi nhẹ, lệch pha.
- `Main/PumpkinRings` (sau bí ngô): `UIParticle` + 1 hạt / 1.1 s, vòng sáng kem (`fx_ring.png` tự vẽ) to dần 35 % → 100 % (440 px), mờ dần.
- `Body/Bats` (trên cùng): 2 hệ hạt trái → phải / phải → trái, texture sheet `doi1_sheet` (12x12, 134 khung, vỗ cánh), noise cho bay lượn, ≤ 3 con mỗi chiều. Material `HalloweenRip/Shop/FX_Bat_UI` (chỉ local).

**Sao lấp lánh → Particle System** (5 thẻ: Special, Bundle, Coin, RemoveAds, Starter): bỏ `UISparkles` + 3–4 Image con, thay bằng `UIParticle` (scale 100) + `ParticleSystem` (vùng chữ nhật = rect cũ, cỡ / màu / thời gian sống / tốc độ xoay giữ như cũ, mật độ = số sao / (nghỉ TB + sống), to – nhỏ theo `sizeOverLifetime`, thời gian thật). Xoá `UISparkles.cs`. Material `Source/Art/UI/Shop/Effects/FX_Sparkle_UI` (UI/Default).

**Kiểm tra**: Play tab Shop – chụp 3 khung: bí ngô bẹp / giãn, vầng sáng, ma nghiêng 2 phía, dơi bay, sao lấp lánh; console sạch.

## 2026-10-27 11:00 – Bí ngô Limited Pack nhún nhẹ lại theo Spine gốc

**Phản hồi**: bí ngô lắc mạnh quá; kiểm tra bản rip có Spine của các anim này không.

**Tìm thấy**: `BaseGame/Sprite/_NewUI2/FoodieStreakHLW/packshopHLW/` (Spine 4.2.43, 1 animation `idle` 4 s mỗi file):
- `packshopHLW_right`: `s_bi` (bí ngô), `s_glow1..4` (4 vầng sáng to dần + mờ), `s_shadow`, `maL` (hồn ma, mesh), `doi1..3` (dơi bay qua).
- `packshopHLW_left`: `maR` (hồn ma còn lại, mesh).
Đọc bằng spine-csharp 4.2 (dotnet, ngoài project): bí ngô co giãn ±2.4 % ngang / ±2.6 % dọc, nghiêng ±2.4°, chu kỳ 1.33 s (3 nhịp / 4 s). Hồn ma không chỉ lắc: vươn cao lên rồi mờ biến mất, sau đó mọc lại từ nhỏ (mesh deform).

**Sửa**: `Pumpkin` `UIFloat` squash (0.06, 0.08) / 0.9 s → (0.024, 0.026) / 1.333 s, thêm tilt 2.4° / 1.333 s.

## 2026-10-27 12:00 – Limited Pack: hồn ma, vầng sáng, dơi dựng từ Spine gốc

**Yêu cầu**: dựng hồn ma, vầng sáng và dơi của thẻ Limited Pack từ Spine `packshopHLW` cho giống video.

**Cách làm** (như rương / vạc ở Home): spine-csharp 4.2 (dotnet, ngoài project) dump `idle` 4 s ở 30 fps → script Python vẽ từng khung (tam giác mesh, alpha slot) thành sheet lưới + file `.txt` (ô, số khung, tâm từng khung theo toạ độ skeleton):
- `Pack_ghostR` (`maL` của `packshopHLW_right`, 0.62x, 14x9) và `Pack_ghostL` (`maR` của `packshopHLW_left`, 0.7x, 12x10): hồn ma vươn cao, mờ biến mất rồi mọc lại.
- `Pack_glow` (`s_glow1..4`, 0.4x, 15 fps, 60 khung): 4 vầng sáng đồng tâm co giãn, mờ dần.
- `Pack_doi1..3` (cắt quanh từng con mỗi khung, 1x): dơi vỗ cánh bay qua; vị trí từng khung đi theo đường bay gốc.
Sheet ở `HalloweenRip/Shop/PackFX/` (chỉ local), Sprite Multiple cắt bằng `ISpriteEditorDataProvider`. Android đang giới hạn ảnh 2048 px nên sheet giữ ≤ 2048.

**Ghép vào `ShopSpecialCard.prefab`** (`UISpriteSequence` – sprite + vị trí từng khung, cùng đồng hồ nên khớp nhịp như Spine):
- Toạ độ skeleton → thẻ: lấy `s_bi` (cùng cỡ 330x314 với ảnh bí ngô) làm mốc: `Main` neo phải dưới = skeleton + (-284, 247); `Body` = skeleton + (-294, 247). Hồn ma trái (skeleton riêng) đặt sao cho khung đầu trùng chỗ hồn ma cũ (+107, +243, neo trái dưới).
- `Main/PumpkinGlow` (sau bí ngô), `GhostRight` / `GhostLeft` (bỏ `UIFloat`), `Body/Bats/doi1..3` (trên cùng).
- Bỏ bản làm tay: `PumpkinRings` (hạt + `fx_ring.png`, `FX_Ring_UI.mat`), `Bats` (hạt + `FX_Bat_UI.mat`). Bí ngô giữ `UIFloat` theo số liệu Spine; sao lấp lánh giữ Particle System.

**Kiểm tra**: Play tab Shop, 4 thời điểm trong vòng 4 s: vầng sáng sau bí ngô, 2 hồn ma ở các pha khác nhau (đứng / vươn / mờ), dơi bay ngang; console sạch.
