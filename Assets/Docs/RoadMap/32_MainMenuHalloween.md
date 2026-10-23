# 32 – Main Menu theme Halloween (nền động, hồn ma, 5 tab)

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-24 07:20 |

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
