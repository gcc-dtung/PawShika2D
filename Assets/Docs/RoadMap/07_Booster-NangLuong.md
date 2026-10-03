# 07 – Giai đoạn 4 Booster, Giai đoạn 5 Năng lượng, Main Menu (3 tab, màn Home, PLAY); khung giãn xong mới thành mèo

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-03 16:10 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 01:32 (03/10) | Giai đoạn 4: 3 booster | Hint tự đặt 1 khối đúng; Khiên chặn lần sai kế tiếp; Reveal chọn 1 số → giải mọi khối cùng số. Số lượt, dấu "+" → popup mua bằng coin, ổ khóa theo level |
| 01:45 (03/10) | Đủ value: khung giãn hết rồi mới thành mèo | Khung chọn giãn trong `resizeDuration` 0.15 s (chỉnh trong Inspector); mèo chỉ hiện khi khung đã phủ kín vùng; thả tay sớm thì chờ khung giãn xong rồi mới đặt / báo sai |
| 01:59 (03/10) | Giai đoạn 5: năng lượng (cá) | Thua −1 cá, tối đa 5, hồi 1 / 30 phút (cả khi tắt game); HUD góc trên phải; popup nạp (ads +1 / 900 coin đầy); hết cá thì Retry / mở game bị chặn tới khi có cá; API năng lượng vô hạn |
| 04:06 (03/10) | Main Menu: chuyển giữa 3 panel | Thanh tab dưới Shop / Home / Progress như bản gốc, panel trượt ngang; Progress thay Daily Challenge |
| 04:13 (03/10) | Thanh tab đúng vị trí ngay trong Editor | Tab neo bottom center, xếp sẵn khi chưa chạy (Home được chọn) |
| 04:22 (03/10) | Màn Home (PLAY) + chuyển Menu ↔ màn chơi | Nút PLAY / Level n, ô năng lượng + coin; PLAY vào level (hết cá → popup nạp); Thua → HOME; popup sang `Canvas_Popup` dùng chung |
| 14:55 (03/10) | Font Fredoka Bold + màn Home giống ảnh gốc | Toàn game dùng Fredoka Bold (sửa lỗi "font hỏng" do shader rip trùng tên); chữ viền + bóng; Home thêm avatar, cài đặt, Starter, No Ads, ô kem cho cá / coin |
| 15:10 (03/10) | Play Zone | Vùng chơi giãn theo màn hình (trừ safe area); camera luôn đặt lưới trọn và ở giữa vùng, tự căn lại khi màn hình đổi – hết tràn lưới trên iPhone X |
| 15:20 (03/10) | Thanh trên chỉ ở tab Home | Avatar, ô cá, ô coin, cài đặt chuyển từ `AlwayOn` vào `HomePanel`, trượt đi cùng panel |
| 15:30 (03/10) | Vạch ngăn tab hiện từ lúc bắt đầu trượt | Vạch mới hiện dần (fade) suốt lúc nền tab chọn trượt, vạch cũ mờ nhanh trước khi nền lướt tới; trước đây vạch chỉ bật khi trượt xong |
| 15:48 (03/10) | Icon năng lượng: cá → tim | Tim như ảnh gốc ở Home, HUD, popup Thua / nạp; vô hạn → tim ∞; popup "MORE LIVES" |
| 16:10 (03/10) | UI Zone (safe area) + nền phủ kín | HUD và thanh trên của Home nằm trong safe area, tránh tai thỏ; nền Home / nền màn chơi chỉ phóng to khi chưa phủ kín thiết bị |

---

## 01:32 – Giai đoạn 4: Booster

### Mục tiêu

Làm 3 nút booster trên thanh dưới theo `TaskBreakdown.md`, thay Freeze Time (dính tới thời gian) bằng Khiên.

| Booster | Mở ở level | Lượt đầu | Giá | Hành vi |
| --- | --- | --- | --- | --- |
| Hint | 5 | 3 | 1200 coin / 3 lượt | Đặt khối **nhỏ nhất** còn đặt được trong lời giải |
| Khiên | 7 | 3 | 1500 coin / 3 lượt | Chặn 1 lần sai kế tiếp trong level (khối sai vẫn rung, không mất tim) |
| Reveal | 9 | 1 | 1000 coin / 1 lượt | Chạm 1 ô số → đặt lần lượt mọi khối cùng số (ô còn khóa bỏ qua) |

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Booster/BoosterDatabase.cs` | `BoosterType` (Hint 0, Shield 1, Reveal 2 – giữ số của bản gốc), `BoosterData`, ScriptableObject `BoosterDatabase`; tên field như asset gốc |
| Thêm | `Gameplay/Booster/BoosterInventory.cs` | Số lượt lưu PlayerPrefs `PawBlock_Booster_{type}`, lần đầu tặng `valueStart` |
| Thêm | `Gameplay/Booster/BoosterManager.cs` | `Press`, `Buy`, Hint, Khiên, chế độ Reveal (`HandleCellTap`), đặt lần lượt bằng DOTween Sequence (kill khi tắt / đổi level). Chỉ trừ lượt khi booster có tác dụng |
| Sửa | `Gameplay/Core/BoardModel.cs` | `TrySolveRemaining`: lời giải cho các ô số còn trống, khớp khối đã đặt |
| Sửa | `Gameplay/Core/GridManager.cs` | `PlaceSolved(rect)`: booster đặt khối (màu random, qua preview + Confirm như thường, có kiểm tra thắng). `IsInputLocked` thêm `BoosterManager.IsBusy` |
| Sửa | `Gameplay/Lives/LivesManager.cs` | Khiên: `HasShield`, `ActivateShield`, `onShieldChanged`, `onShieldBlocked`; `LoseLife` gặp khiên thì tắt khiên, không trừ; `ResetLives` xóa khiên |
| Sửa | `Gameplay/Input/DragAndDropController.cs` | Đang chọn số Reveal thì chạm ô = chọn, không kéo |
| Thêm | `UI/HUD/BoosterButton.cs` | Icon, số lượt, dấu "+", ổ khóa "LV n", quầng sáng xoay khi đang bật, nảy khi dùng (DOTween, kill trong `OnDisable`) |
| Thêm | `UI/HUD/RevealBanner.cs` | Banner "TAP A NUMBER TO REVEAL" + X, trượt lên che thanh booster |
| Thêm | `UI/HUD/ShieldIndicator.cs` | Khung băng `ice_effect_2` phủ hàng tim khi có khiên, thở nhẹ; chặn lỗi thì phồng to + mờ |
| Thêm | `UI/Popups/BuyBoosterPopup.cs` | Mua booster bằng coin, tạm dừng ván khi mở |
| Sửa | `UI/Popups/GameFlowUI.cs` | Nghe `onBuyRequested` → mở `BuyBoosterPopup` |
| Sửa | `Editor/UI/PopupBuilder.cs` | Dựng `BuyBoosterPopup`; các hàm dựng UI chuyển `internal` để dùng chung |
| Thêm | `Editor/UI/BoosterBuilder.cs` | Menu **Tools → PawBlock → Build Boosters** |
| Sửa | `GameAssets/10_Data_Configs/BoosterDatabase.asset` | Trỏ sang script mới (script gốc không có trong project), Freeze Time → **Shield** + mô tả mới, icon `btn_hint` / `btn_freze` / `btn_reveal`, bỏ `activeSeconds` |
| Sửa | `Scenes/SampleScene.unity` | Kết quả Build Popups + Build Boosters |
| Sửa | `Docs/README.md`, `TaskBreakdown.md`, `ProjectStructure.md` | Giai đoạn 4 xong, script / scene / event mới |

### Ghi chú thiết kế

- Hint chọn khối nhỏ nhất cho dễ thấy; Hint / Reveal dùng giá trị thật của ô ẩn "?" nên luôn có 1 lời giải nhất quán (kể cả level nhiều lời giải).
- Khiên trả lượt khi bật (giống Freeze gốc); không dùng tới thì mất khi sang level / chơi lại.
- Banner Reveal ban đầu đặt ngay dưới HUD trên nhưng đè lên hàng ô đầu; lưới lớn chiếm hết khoảng trống trên / dưới nên chuyển xuống che thanh booster (980x220).
- Hiệu ứng Spine `Freeze` chưa dùng được vì chưa có Spine runtime.

### Kiểm tra (Play mode, level 27 và 6)

| Bước | Kết quả |
| --- | --- |
| Vào level 27 | 3 nút: Hint 3, Khiên 3, Reveal 1 |
| Hint | Đặt khối 1x2 (5,4), Hint 3 → 2 |
| Khiên | Khung băng phủ tim, nút có quầng sáng; Khiên 3 → 2; bấm lại khi đang bật: không trừ lượt |
| Sai 1 lần khi có khiên | Mạng giữ 3, khiên tắt |
| Reveal → chạm ô số 3 (4,4) | Banner hiện; đặt cả 2 khối số 3, Reveal 1 → 0, lưới khóa trong lúc đặt rồi mở lại |
| Bấm Reveal khi 0 lượt | Nút có dấu "+", popup mua; 80 coin → nút mua mờ; state `Paused`; X → `Playing` |
| Cộng tạm 1000 coin → mua | Coin 1080 → 80, Reveal 0 → 1 |
| Có khiên → sang level 6 | Khiên mất; Khiên / Reveal khóa "LV 7" / "LV 9", bấm không làm gì |
| Hint ở level 6 (1 khối) | Đặt khối, thắng, WinPopup |
| Reveal → nút X trên banner | Thoát chế độ chọn, không trừ lượt |
| Console | Không lỗi / cảnh báo mới |

Sau khi thử đã trả `PawBlock_CurrentLevel` = 26, `PawBlock_Coins` = 80 và xóa 3 key booster.

---

## 01:45 – Đủ value: khung giãn hết vào vùng rồi mới biến thành mèo

### Vấn đề

Kéo tới ô làm diện tích = value thì mèo xem trước hiện ngay trong frame đó, trong khi khung chọn còn đang trượt sang ô mới (SmoothDamp 0.05 s) → mèo "nhảy" ra trước khung, cảm giác quá nhanh.

### Đã chốt với người dùng

- Đủ value: khung vẫn giãn vào ô mới như lúc chưa đủ; **giãn xong mới thành mèo**. Kéo đi chỗ khác trước khi giãn xong thì không hiện mèo.
- Thả tay khi khung chưa giãn xong: **chờ giãn xong** rồi mới đặt / báo sai (phương án b).
- Giãn chậm hơn trước, chỉnh được trong Inspector.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Gameplay/Input/SelectionBoxView.cs` | Bỏ SmoothDamp (`resizeSmoothTime`). Đổi vùng / hướng bị chặn → tween DOTween từ vị trí đang hiển thị tới đích trong `resizeDuration` (mặc định 0.15 s) với `resizeEase` (mặc định OutQuad), cả hai là `[SerializeField]`. `IsSettled`, `WhenSettled(callback)` (1 callback, gọi ngay nếu đã tới; `Hide` / `OnDisable` hủy tween và bỏ callback) |
| Sửa | `Gameplay/Core/GridManager.cs` | `RefreshSelection`: vùng Ready thì `WhenSettled(ShowPreviewFor)` thay vì hiện mèo ngay; khung chỉ mờ khi mèo đã hiện. `EndSelection`: Ready mà khung chưa tới → `_finishing` (khóa thao tác), chờ `WhenSettled` rồi `CommitSelection` (hiện mèo → kiểm tra lời giải → đặt / báo sai). `CancelSelection` bỏ trạng thái chờ |
| Sửa | `Docs/ProjectStructure.md` | Mô tả `SelectionBoxView`, `GridManager`, luồng nước đi |

### Kiểm tra (Play mode, level 28)

| Bước | Kết quả |
| --- | --- |
| Kéo tới vùng 4x3 đủ value (số 12) | Ngay sau đó: khung chưa tới đích, **chưa có mèo**; sau khi khung giãn xong thì có 1 mèo xem trước |
| `timeScale` 0.002 / 0.02, chụp ảnh | Ảnh 1: khung còn 3x3, không mèo. Ảnh 2: khung đang giãn sang cột số 12, không mèo |
| Kéo tới đủ value rồi thả tay ngay (`timeScale` 0.01) | Lúc thả: chưa đặt khối, lưới khóa, khung còn hiện. Trả `timeScale` 1: khối 4x3 được đặt, lưới mở lại, khung ẩn |
| Kéo tới đủ value rồi lùi 1 cột ngay | Không hiện mèo |
| Console | Không lỗi |

Sau khi thử đã trả `PawBlock_CurrentLevel` = 27, `PawBlock_Coins` = 90, `timeScale` = 1.

---

## 01:59 – Giai đoạn 5: Năng lượng (phương án B, icon cá)

### Mục tiêu

Theo `TaskBreakdown.md`: giữ năng lượng ngoài màn chơi của bản gốc nhưng đổi icon khác tim (tim đã dùng cho mạng trong màn). Thua 1 level tốn 1, tối đa 5, hồi 1 sau 30 phút (`gamePlay_TimeRefillHeart = 1800`). Chưa có màn Home nên hiện và chặn ngay trong màn.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Energy/EnergyModel.cs` | **Logic** C# thuần, nhận `now` từ ngoài: hồi theo mốc thời gian (`Refresh` cộng bù số lượt đã trôi → tắt game vẫn hồi), `TryConsume` (từ đầy xuống thì bắt đầu đếm), `Add`, `AddInfinite`, `SecondsToNext`; đồng hồ máy bị lùi thì đặt lại mốc |
| Thêm | `Gameplay/Energy/EnergyManager.cs` | Lưu PlayerPrefs (`PawBlock_Energy`, `…RefillStart`, `…InfiniteUntil`), nghe `GameState.Lost` → −1, tick mỗi giây, tính bù khi quay lại app; context menu Debug |
| Thêm | `UI/HUD/EnergyHUD.cs` | Góc trên phải: cá, số, đồng hồ / FULL / ∞; số đổi thì cá nảy (DOTween, kill khi tắt); bấm → popup nạp |
| Thêm | `UI/Popups/RefillEnergyPopup.cs` | Ads +1, 900 coin nạp đầy; `ShowOptional` (có X) / `ShowRequired(onReady)` (không X, có năng lượng thì tự đóng → `onReady`); tạm dừng ván, tự tạm dừng lại nếu nạp level đặt state về Playing |
| Sửa | `UI/Popups/GameFlowUI.cs` | `OpenEnergyRefill`; Retry sau khi thua và lúc mở game: hết năng lượng → popup bắt buộc |
| Sửa | `UI/Popups/LosePopup.cs` | `RetryHandler` để GameFlowUI kiểm tra năng lượng |
| Sửa | `Editor/UI/PopupBuilder.cs` | Dựng `RefillEnergyPopup`; LosePopup thêm dòng "−1 🐟" thay "Try again!"; nối `EnergyHUD.gameFlow` |
| Thêm | `Editor/UI/EnergyBuilder.cs` | Menu **Tools → PawBlock → Build Energy** |
| Sửa | `Scenes/SampleScene.unity` | Build Popups + Build Energy |
| Sửa | `Docs/README.md`, `TaskBreakdown.md`, `ProjectStructure.md` | Giai đoạn 5 xong, câu hỏi mở mới |

### Ghi chú thiết kế

- Icon: `ShikaCat_GameAssets/.../Image_FishToy_Default.png` (cá trắng xám) tô cam bằng `Image.color`.
- Chỉ thua mới tốn; thắng, hồi mạng, Replay giữa level không tốn. Replay miễn phí có thể bị lợi dụng → ghi vào Câu hỏi mở.
- Giá nạp đầy 900 coin là số tạm (`refillCost` trong Inspector).

### Kiểm tra (Play mode, level 29)

| Bước | Kết quả |
| --- | --- |
| Vào game lần đầu | 5/5, HUD "5 FULL" |
| Sai 3 lần → X ở popup Hồi mạng | State `Lost`, năng lượng 4, HUD đếm 29:55; LosePopup có "−1 🐟" |
| Set 0 → Retry | Popup "MORE FISH" bắt buộc (không có X), "0/5", "Next in 29:54" |
| Tua 1 lượt hồi | Năng lượng 1 → popup tự đóng, level chơi lại, 3 mạng, `Playing`; đồng hồ lượt kế 29:44 |
| Bấm HUD | Popup có X, state `Paused`; ads +1 → 2 |
| Cộng tạm 1000 coin → nút FULL | 5/5, trừ 900 coin, popup tự đóng, `Playing` |
| Thoát Play với 0 năng lượng → vào lại | Popup bắt buộc ngay khi vào, state `Paused`, lưới khóa (lần đầu bị nạp level đặt lại `Playing` → đã sửa) |
| Năng lượng vô hạn 5 phút | Popup tự đóng, `Playing`, HUD "∞ 05:00" |
| Console | Không lỗi |

Sau khi thử đã trả `PawBlock_CurrentLevel` = 28, `PawBlock_Coins` = 100 và xóa 3 key năng lượng.

---

## 04:06 – Main Menu: điều hướng 3 panel trong `Canvas_Menu`

### Yêu cầu

Theo 3 ảnh Main Menu của bản gốc (Shop / Home / Daily Challenge): trước mắt làm việc chuyển giữa các panel trong `Canvas_Menu`. Tab thứ 3 đổi thành **Progress** (lên level nào nhận quà gì) thay cho Daily Challenge, vì game gốc sinh lưới theo ngày bằng thuật toán không có.

### Hiện trạng trước khi sửa

Người dùng đã tạo `Canvas_Menu` (chưa lưu): `HomePanel/Background` (home_bg 2048x2048), `AlwayOn` (trong suốt nhưng `raycastTarget` bật, full màn hình → sẽ chặn bấm vào panel), CanvasScaler 800x600. `Canvas_HUD` đang được tắt.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Menu/MainMenuNavigator.cs` | Panel xếp ngang, đổi tab thì trượt (DOTween Sequence, kill khi tắt, tắt giữa chừng thì nhảy tới trạng thái cuối); tab chọn rộng 408, nền `home_tab1` trượt theo; tắt panel khuất màn hình sau khi trượt |
| Thêm | `UI/Menu/MenuTabButton.cs` | Icon to 1.3x + nhô lên, chữ hiện ở nửa sau; vạch ngăn |
| Thêm | `Editor/UI/MainMenuBuilder.cs` | Menu **Tools → PawBlock → Build Main Menu** |
| Sửa | `Scenes/SampleScene.unity` | `Canvas_Menu`: CanvasScaler 1080x1920 (như HUD), sortingOrder 10; `Panels` (ShopPanel tạm, HomePanel của người dùng chuyển vào – giữ nguyên nội dung, ProgressPanel tạm), `RectMask2D` mỗi panel; `AlwayOn` tắt `raycastTarget`, thêm `TabBar`. Lưu luôn trạng thái đang tắt của `Canvas_HUD` |
| Sửa | `Docs/README.md`, `TaskBreakdown.md`, `ProjectStructure.md` | Quyết định #8 (Progress thay Daily Challenge), danh sách việc Giai đoạn 6 |

### Asset dùng

`Backgrounds/home_tab1` (tab đang chọn), `home_tab2` (dải nền thanh tab, kéo ngang), `home_tab3` (vạch ngăn), `Icons/icon_shop`, `icon_home`, `icon_cup` (cúp cho Progress), `Backgrounds/bg_shop_1` (mái hiên sọc của Shop).

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Vào Play | Tab Home được chọn, nhô lên có chữ "Home"; Shop / Progress chỉ có icon |
| Bấm Shop | Panel Shop (mái hiên sọc, "SHOP – Coming soon"); vạch ngăn giữa Home và Progress |
| Shop → Progress (`timeScale` 0.05) | Các panel trượt sang trái qua Home; lần đầu thấy nền home_bg tràn sang panel Shop và vạch ngăn đè lên nền tab đang trượt → thêm `RectMask2D` cho panel, vạch ngăn chỉ hiện khi trượt xong; chụp lại: mép gọn |
| Kết thúc | Tab Progress nhô lên, chỉ `ProgressPanel` còn bật, không còn tween |
| Console | Không lỗi |

### Chưa làm

Nút PLAY và coin / năng lượng trên Home, quay về menu sau khi chơi, nội dung Shop và Progress, vuốt ngang để đổi tab.

---

## 04:13 – Thanh tab đúng vị trí ngay trong Editor, neo bottom center

### Vấn đề

Tab chỉ được xếp lúc `Start` khi chạy game → trong Editor 3 tab chồng ở góc trái, chữ đè nhau; người dùng muốn thấy đúng vị trí ngay khi chỉnh scene, anchor bottom center.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `UI/Menu/MainMenuNavigator.cs` | `TabSlot` trả về tâm tab so với giữa thanh (neo bottom center) thay vì mép trái; tách `LayoutRects`; thêm `LayoutInEditor` (`[ContextMenu]`): chọn tab mặc định, đặt icon / chữ / vạch ngăn, xếp tab và panel, không tắt panel |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | `TabBar` (1080x200), `SelectedTab`, 3 tab: anchor + pivot (0.5, 0); dựng xong gọi `LayoutInEditor` |
| Sửa | `Scenes/SampleScene.unity` | Chạy lại Build Main Menu |

### Kiểm tra

| Bước | Kết quả |
| --- | --- |
| Edit mode sau khi dựng | `TabBar` neo (0.5, 0) 1080x200; Shop x −372 w 336, Home x 0 w 408 (icon y 175, scale 1.3, chữ hiện), Progress x 372 w 336; `SelectedTab` x 0; ShopPanel −1080 / HomePanel 0 / ProgressPanel 1080, đều bật |
| Play → chọn Progress | `SelectedTab` x 336, Shop −372, Home −36, Progress 336 (w 408); ảnh chụp đúng như trước |
| Console | Không lỗi |

---

## 04:22 – Màn Home: nút PLAY, năng lượng + coin; chuyển Menu ↔ màn chơi

### Yêu cầu

"Làm màn hình play": màn Home như ảnh gốc (PLAY / Level n, ô tim → ô cá, ô coin) và bấm PLAY vào được màn chơi.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Menu/ScreenFlow.cs` | Mở game ở Menu (HUD tắt, ván nền tạm dừng); `Play` → kiểm tra năng lượng → tắt menu, bật HUD, nạp lại level hiện tại; `ShowMenu` |
| Thêm | `UI/Menu/PlayButtonView.cs` | "Level n", thở nhẹ (DOTween lặp, kill khi tắt), bấm → `ScreenFlow.Play` |
| Thêm | `UI/Menu/CoinHUD.cs` | Số coin, icon nảy khi đổi; bấm → tab Shop |
| Sửa | `UI/Popups/GameFlowUI.cs` | `RequestPlay(start)`; bỏ chặn lúc mở game (giờ chặn ở nút PLAY); `OpenEnergyRefill` mở được ở Menu; Retry hết năng lượng → popup nạp có X, X mở lại popup Thua |
| Sửa | `UI/Popups/RefillEnergyPopup.cs` | `ShowRequired(onReady, onCancel)`: có `onCancel` thì hiện X |
| Sửa | `UI/Popups/LosePopup.cs` | Nút HOME → `ScreenFlow.ShowMenu` |
| Sửa | `Editor/UI/PopupBuilder.cs` | Popup dựng trong `Canvas_Popup` (mới, Overlay sortingOrder 20, luôn bật) thay vì `Canvas_HUD` – HUD bị tắt khi ở Menu; popup Thua 2 nút HOME / RETRY; nối `GameFlowUI` cho mọi `EnergyHUD` và `ScreenFlow`; tìm `Canvas_HUD` cả khi đang tắt |
| Sửa | `Editor/UI/BoosterBuilder.cs`, `EnergyBuilder.cs` | Tìm `Canvas_HUD` cả khi đang tắt |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | `HomePanel/PlayButton` (viền trắng `btn_green_4` + lớp xanh `btn_green_3` như bản gốc), `AlwayOn/TopBar` (Energy: cá + số đè lên + đồng hồ + "+", Coin), `ScreenFlow` trên `Manager` |
| Sửa | `Scenes/SampleScene.unity` | Build Popups + Build Main Menu |
| Sửa | `Docs/README.md`, `TaskBreakdown.md`, `ProjectStructure.md` | Cách chạy (vào Menu trước), cấu trúc scene / script mới |

### Ghi chú

- Lần dựng đầu dùng `btn_green_2` cho nút PLAY → ra màu xanh đậm (đó là viền trắng + nền tối); xem lại bộ sprite thì nút gốc là `btn_green_4` (viền) + `btn_green_3` (lớp xanh) chồng lên nhau.
- Thắng thì vẫn sang level kế ngay trong màn chơi; chưa có nút Home giữa ván / về Home sau khi thắng.

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Vào Play | Ở Menu tab Home: "5 FULL", "100" coin, nút PLAY "Level 29"; `Canvas_HUD` tắt, state `Paused`, lưới khóa |
| PLAY | Menu tắt, HUD bật, level 29 nạp lại, `Playing`, kéo được |
| Sai 3 lần → X ở Hồi mạng | `Lost`, năng lượng 4 |
| HOME ở popup Thua | Về Menu (HUD tắt), ô cá "4 29:51" |
| Năng lượng 0 → PLAY | Popup nạp có X; X → vẫn ở Menu. PLAY lại → tua 1 lượt hồi → popup tự đóng, vào màn chơi, năng lượng 1 |
| Bấm ô cá ở Menu | Popup nạp (không bắt buộc) mở được |
| Bấm ô coin | Chuyển sang tab Shop |
| Console | Không lỗi |

Sau khi thử đã trả `PawBlock_CurrentLevel` = 28, `PawBlock_Coins` = 100, năng lượng về như trước.

---

## 14:55 – Font Fredoka Bold + màn Home giống ảnh gốc

### Yêu cầu

"Lựa chọn lại font chữ cho game, làm giao diện play giống trong ảnh" (ảnh màn Home bản gốc: avatar, ô tim + đồng hồ, ô coin, nút cài đặt, rương Starter, No Ads, nút PLAY xanh / Level n, thanh tab dưới).

### Font

Chọn **Fredoka Bold** (`GameAssets/06_Fonts/Fredoka-Bold.ttf`, có sẵn trong dự án): nét tròn, đậm, đúng kiểu chữ của bản gốc (PLAY, Starter, Home…), hợp game casual mèo.

Nguyên nhân Fredoka từng "hỏng" (README cũ: tạo lại bằng code cũng lỗi): `GameAssets/09_Shaders_Materials/Shaders/TextMeshPro_Distance Field.shader` là bản rip từ game gốc, **trùng tên** `TextMeshPro/Distance Field` với shader thật của TMP. Font asset tạo mới lấy shader bằng tên → trúng bản rip → chữ thành ô đen. Asset `Fredoka-Bold SDF` cũ còn trỏ script TMP của dự án khác nên không nạp được.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Editor/UI/GameFontSetup.cs` | Tools → PawBlock → Apply Game Font: Fredoka làm font mặc định TMP, fallback LiberationSans (ký tự `∞`), gán shader `TMP_SDF-Mobile` theo đường dẫn; tạo 3 preset viền + bóng đổ (Purple / Green / Brown); đổi font mọi chữ LiberationSans trong scene (55) và prefab (4: `Cell`, `PlacedBlock`, `DragCountBubble`) |
| Thêm | `06_Fonts/Fredoka-Bold SDF - Outline Purple / Green / Brown.mat` | Preset viền chữ |
| Sửa | `06_Fonts/Fredoka-Bold SDF.asset` | Tạo lại (Assets → Create → TextMeshPro → Font Asset, Dynamic 1024, padding 9); xóa asset cũ hỏng và `Fredoka-Bold Atlas.png` không ai dùng |
| Sửa | `TextMesh Pro/Resources/TMP Settings.asset` | Font mặc định → Fredoka-Bold SDF |
| Thêm | `04_UI_Graphics/Frames_Borders/khung_pill_kem.png` | Ô kem bo tròn 300x96 (vẽ bằng script, 9-slice 44 px) cho ô cá / coin như bản gốc |
| Thêm | `UI/Menu/MenuShortcutButton.cs` | Nút phụ Home: nhún nhẹ (DOTween), bấm → tab Shop |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Bố cục theo `home_demo3` (1080x1920): TopBar = Avatar (`khung_avatar`) · ô cá · ô coin · Settings (`khung_setting_3` + `btn_setting`); HomePanel thêm Starter (`btm_chest_stater` + chữ) và No Ads (`no_ads_2` + ruy băng `no_ads_1`); PLAY cách đáy 470, chữ PLAY viền xanh, "Level n" xanh đậm; chữ tab viền tím; số trên ô kem màu nâu |
| Sửa | `Scenes/SampleScene.unity`, 3 prefab | Apply Game Font + Build Main Menu |

### Ghi chú

- Giữ ô **cá** (năng lượng) thay cho ô tim và tab **Progress** thay Daily như đã chọn trước đó.
- Avatar và Settings mới có nút, chưa gắn chức năng (chưa có popup cài đặt / hồ sơ). Starter, No Ads tạm chuyển sang tab Shop.
- Popup và HUD trong màn chơi đổi sang Fredoka nhưng chưa thêm viền chữ.

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Vào Play | Home: avatar, "5 FULL", "110", bánh răng, Starter, No Ads, PLAY / Level 30, tab Home; chữ Fredoka, viền rõ, không ô đen |
| PLAY | Màn chơi: LEVEL 30, số trên ô lưới, "5 FULL" đều Fredoka |
| Console | Không lỗi script (chỉ 1 assertion nội bộ của Editor `previewArtifactID` sau khi xóa asset) |

---

## 15:10 – Play Zone

### Vấn đề

Trên iPhone X (Simulator, 1125x2436) lưới tràn ra hai mép màn hình. Camera chỉ được căn một lần khi nạp level, theo tỉ lệ cố định (`boardHeightRatio` / `boardWidthRatio`). Nó không biết HUD thật nằm ở đâu, không tính safe area, và không căn lại khi màn hình đổi (đổi máy trong Simulator, xoay máy).

### Cách làm

Thêm **Play Zone**: một RectTransform trong `Canvas_HUD`, neo giãn toàn màn hình, lề trên 300 (dưới LevelFrame), dưới 310 (trên hàng booster), hai bên 30. Vùng này tự giãn theo thiết bị (canvas khớp chiều ngang 1080, máy càng dài thì vùng càng cao) và bị cắt thêm theo `Screen.safeArea`.

Camera được tính sao cho lưới (tâm ở gốc world) nằm **trọn và ở giữa** vùng:
- `ortho = max(minOrthoSize, lướiCao · H / (2 · vùngCao), lướiRộng · H / (2 · vùngRộng))` (H = chiều cao màn hình, pixel)
- Dời camera sao cho tâm vùng trùng gốc world.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Visuals/PlayZone.cs` | Tính vùng chơi trên màn hình (pixel, cắt theo safe area), phóng / dời camera; `SetContent(kích thước lưới)`; `LateUpdate` căn lại khi kích thước màn hình hoặc vùng đổi (không cấp phát mỗi frame); `showZone` tô màu vùng để căn chỉnh (Image tắt hẳn khi không dùng) |
| Sửa | `Gameplay/Level/LevelManager.cs` | `playZone`: `FitCamera` giao cho PlayZone; PlayZone đang tắt (ở Menu, HUD tắt) thì căn tạm theo tỉ lệ cũ, vào màn chơi PlayZone căn lại |
| Thêm | `Editor/UI/PlayZoneBuilder.cs` | Tools → PawBlock → Build Play Zone: dựng / cập nhật `Canvas_HUD/PlayZone`, gán camera và `LevelManager.playZone` |
| Sửa | `Scenes/SampleScene.unity` | Build Play Zone |

### Kiểm tra (Play mode, Simulator iPhone X, level 31 – 7x7)

| | Kết quả |
| --- | --- |
| Safe area | y 102 → 2304 |
| Vùng chơi | x 31 → 1094, y 323 → 2124 |
| Lưới trên màn hình | x 31 → 1094 (vừa khít chiều ngang vùng), y 692 → 1754 (tâm trùng tâm vùng) |
| Ảnh camera + khung vùng / safe area | Lưới nằm trọn trong vùng, không còn tràn mép |

### Ghi chú

- HUD trên (`TopPanel`) chưa tránh tai thỏ: trên iPhone X chữ LEVEL nằm sát vùng tai thỏ. Muốn tránh thì thêm component safe area cho `TopPanel` / `DownPanel`.

---

## 15:20 – Thanh trên chỉ ở tab Home

### Yêu cầu

Avatar, ô cá, ô coin, nút cài đặt (cùng Starter, No Ads) chỉ nằm ở panel Home, không hiện ở Shop / Progress.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | `TopBar` dựng trong `HomePanel` thay vì `AlwayOn` (xóa cả bản cũ ở `AlwayOn` khi chạy lại); `AlwayOn` chỉ còn `TabBar` |
| Sửa | `Scenes/SampleScene.unity` | Build Main Menu |

### Kiểm tra (Play mode, iPhone X)

| Tab | TopBar (x, pixel) |
| --- | --- |
| Home | 0 → 1125 (hiện) |
| Shop | 1125 → 2250 (trượt ra ngoài cùng HomePanel, bị `RectMask2D` cắt) |

---

## 15:30 – Vạch ngăn tab hiện từ lúc bắt đầu trượt

### Vấn đề

Khi đổi tab, mọi vạch ngăn (`home_tab3`) bị tắt ngay, đến khi nền tab chọn trượt xong mới bật vạch mới → vạch "bật" đột ngột sau chuyển động, cảm giác trễ.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `UI/Menu/MainMenuNavigator.cs` | Đặt trạng thái vạch đích ngay khi bắt đầu chuyển tab (truyền thời lượng), bỏ bật vạch ở `OnComplete`; gom điều kiện vào `DividerVisible` |
| Sửa | `UI/Menu/MenuTabButton.cs` | `SetDividerVisible(visible, duration)`: hiện → bật với alpha 0 rồi fade lên trong cả quãng trượt; ẩn → fade về 0 trong `dividerHideRatio` (0.35) quãng rồi tắt, để kịp biến mất trước khi nền tab chọn lướt qua (vạch vẽ đè lên nền); tween giữ trong field, kill khi đổi tiếp; tắt giữa chừng thì nhảy về trạng thái cuối |

### Kiểm tra (Play mode, từng frame)

| Chuyển | Vạch |
| --- | --- |
| Home → Progress | Vạch Shop│Home bật ngay, alpha 0 → 0.13 → 0.46 → 0.72 → 1 cùng lúc nền tab trượt 0 → 336 |
| Progress → Shop | Vạch Shop│Home mờ 1 → 0.06 khi nền tới x −71, tắt trước khi nền phủ qua; vạch Home│Progress hiện dần |
| Shop → Home | Vạch Home│Progress mờ rồi tắt sau ~0.1 s |
| Đổi tab giữa chừng | Vạch đang hiện dở mờ đi tiếp từ alpha hiện tại, không giật |

---

## 15:48 – Icon năng lượng: cá → tim

### Yêu cầu

Thay icon con cá thành icon tim như ảnh gốc.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Sửa | `Editor/UI/PopupBuilder.cs` | `FishSprite` / `FishColor` → `EnergySprite` (`Icons/icon_hear_2.png`, không tô màu) + `EnergyInfiniteSprite` (`icon_hear_infinity2.png`); popup Thua "-1 ♥", popup nạp: tiêu đề "MORE LIVES", tim + "0/5", nút "+1 ♥" / "FULL ♥" |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | Ô năng lượng ở Home: tim 112, số trắng viền đỏ đậm đè lên tim; gán tim ∞ cho `EnergyHUD` |
| Sửa | `Editor/UI/EnergyBuilder.cs` | HUD trong màn: tim thay cá (số nằm trong ô bên cạnh nên vô hạn vẫn hiện "∞" ở ô, không đổi tim) |
| Sửa | `UI/HUD/EnergyHUD.cs` | `iconImage`, `normalSprite`, `infiniteSprite`: vô hạn → tim ∞ và ẩn số (như "∞ 29:29" của bản gốc) |
| Sửa | `Editor/UI/GameFontSetup.cs` | Thêm preset `Fredoka-Bold SDF - Outline Red` |
| Sửa | `Scenes/SampleScene.unity` | Apply Game Font, Build Popups, Build Energy, Build Main Menu |

### Kiểm tra (Play mode)

| Bước | Kết quả |
| --- | --- |
| Home | Tim "5" + "FULL" |
| `DebugInfinite` (5 phút) | Home: tim ∞, số ẩn, "05:00"; HUD trong màn: tim + "∞" + đồng hồ |
| Popup nạp | "MORE LIVES", tim 5/5, nút +1 ♥ / FULL ♥ |

Sau khi thử đã trả `PawBlock_EnergyInfiniteUntil` = 0.

### Ghi chú

- Trong màn chơi giờ có 2 loại tim: 3 tim mạng của ván (dưới LEVEL) và tim năng lượng (góc phải). Bản gốc cũng vậy (tim ngoài sảnh = lượt chơi).

---

## 16:10 – UI Zone (safe area) + nền phủ kín

### Yêu cầu

1. Thêm khu vực UI zone để tránh tai thỏ.
2. Nền luôn phủ kín thiết bị: ảnh đã phủ kín thì giữ nguyên, chưa kín thì phóng to cho kín (trên iPhone X nền Home 2048 px thấp hơn canvas 2338 → hở mép trên / dưới).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `UI/Common/SafeAreaZone.cs` | Neo RectTransform theo `Screen.safeArea` (tỉ lệ, không phụ thuộc CanvasScaler), chọn cạnh áp dụng; chỉ chạy khi Play; cập nhật khi safe area / màn hình đổi |
| Thêm | `UI/Common/BackgroundCover.cs` | Nền UI: `k = max(1, cha.rộng / ảnh.rộng, cha.cao / ảnh.cao)` (tính cả lệch tâm), scale đều; chỉ phóng to |
| Sửa | `Gameplay/Visuals/BackgroundFitter.cs` | Nền world `bg`: scale trong scene là tối thiểu, chỉ phóng khi chưa phủ kín camera (trước đây luôn ép đúng khít, kể cả thu nhỏ) |
| Thêm | `Editor/UI/UIZoneBuilder.cs` | Tools → PawBlock → Build UI Zone: `Canvas_HUD/UIZone` (4 cạnh) chứa PlayZone, TopPanel, DownPanel, RevealBanner; dựng lại Main Menu |
| Sửa | `Editor/UI/MainMenuBuilder.cs` | `HomePanel/UIZone` (cạnh trên) chứa Starter, NoAds, TopBar; `BackgroundCover` cho `HomePanel/Background` |
| Sửa | `Editor/UI/PopupBuilder.cs` | `HudRoot(canvas)`: UIZone nếu có, không thì canvas |
| Sửa | `Editor/UI/BoosterBuilder.cs`, `EnergyBuilder.cs`, `PlayZoneBuilder.cs` | Tìm / dựng trong `HudRoot` (UIZone) |
| Sửa | `Scenes/SampleScene.unity` | Build UI Zone + Build Play Zone |

### Lỗi gặp khi làm

Lần dựng đầu dùng `Undo.SetTransformParent` giữ vị trí world; `Canvas_HUD` đang tắt (scale 0) nên TopPanel, DownPanel… bị scale 0 (HUD biến mất). Sửa: chuyển cha với `worldPositionStays = false`, builder tự đặt lại scale 1 cho bản dựng hỏng.

### Kiểm tra (Play mode, Simulator iPhone X 1125x2436, safe area y 102 → 2304)

| Phần | Vị trí (pixel) |
| --- | --- |
| Home: nền | phóng 1.142 → y 0 → 2436 (phủ kín), trước đây 2048 / 2338 |
| Home: avatar / ô tim / Starter | đỉnh 2246 / 2252 / 2023 (< 2304, dưới tai thỏ) |
| HUD: LevelFrame, nút chơi lại, ô tim | đỉnh 2242 / 2221 / 2210 |
| HUD: Booster | đáy 192 (trên thanh home 102) |
| PlayZone | y 425 → 1992 (giữa booster và LevelFrame), lưới 7x7 x 31 → 1094, y 677 → 1739 |
| Nền màn chơi | scale 1.07 (giữ nguyên, đã phủ kín: x −168 → 1293, y −81 → 2517) |

### Ghi chú

- Thanh tab dưới (`AlwayOn/TabBar`) chưa né thanh home: nền tab vẫn chạm mép dưới (đúng ý), nhưng chữ "Home" nằm trong vùng thanh home của iPhone X. Nếu cần: tăng chiều cao thanh theo cạnh dưới safe area.
- Popup (`Canvas_Popup`) nằm giữa màn hình nên không bọc UI Zone.
