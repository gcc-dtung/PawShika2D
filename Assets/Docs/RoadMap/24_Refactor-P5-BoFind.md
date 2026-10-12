# 24 – Refactor P5: bỏ Find / GetComponent / AddComponent lúc chạy

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-12 23:45 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 23:11 (12/10) | P5.1 – Find trong scene, `IMenuPopups` | Bootstrap truyền GridSpawner, Camera, TimerUI, TutorialHand, HintMarker, danh sách intro, nút booster; nút ở feature mở popup qua `IMenuPopups` |
| 23:19 (12/10) | P5.2 – GetComponent cùng object | 26 script: `[SerializeField]` + `OnValidate` tự điền; scene + Cell / PlacedBlock / DragCountBubble prefab đã điền |
| 23:23 (12/10) | P5.3 – ButtonPress / âm nút / nút xám | `UIAutoSetup` (Editor) gắn sẵn 88 `ButtonPress`, thu danh sách con; marker `NoClickVibrate` |
| 23:25 (12/10) | P5.4 – registry raycast | `GridSpawner.TryGetCell` (`Collider2D → CellView`) |
| 23:29 (12/10) | P5.5 – runner, nguồn âm | `SaveRunner`, `TrustedClockRunner`, `CatFaceTicker` đặt sẵn; AudioManager có sẵn Music + 8 Sfx |
| 23:39 (12/10) | P5.6 – UI / sprite dựng sẵn | 11 component dựng object con trong Editor (`EditorBuild`), pool nhân bản từ mẫu; `GiftItemView` |

Grep runtime (bỏ `Editor/`, `Tests/`, `#if UNITY_EDITOR`): **118 → 2** chỗ (còn `CheatPanel`, ngoại lệ có ghi lý do).

---

## Thêm

| File | Nội dung |
| --- | --- |
| `Core/Contracts/Interfaces/IMenuPopups` | Mở Settings / Daily / hồ sơ / nạp năng lượng (GameFlowUI cài) |
| `Core/Foundation/Util/EditorChildren` | Chỉ Editor: tìm / tạo object con có component, kèm Undo |
| `Shared/Editor/UIAutoSetup` | Khi lưu scene / vào Play: gắn `ButtonPress`, thu danh sách con của `ButtonPress`, `UIButtonSounds`, `UIGrayscaleWhenDisabled`; menu `Tools/PawBlock/UI/Setup Buttons` |
| `Shared/Scripts/UI/NoClickVibrate` | Marker: nút không rung khi bấm (3 nút booster) |
| `Features/DailyChallenge/Scripts/GiftItemView` | 1 món trong bong thoại quà: icon + số |

## Sửa

- **5.1** `HintMarker`, `BlockFeedback`, `DragFeedback`, `GameplayFeedback`, `GameAudioEvents`, `LevelIntroSequencer`, `LevelStars`, `BoosterUseFx`, `BoosterTutorial`, `BoosterManager`, `DragAndDropController` nhận dependency qua `Init`; `BoosterButton.Find(buttons, type)` tìm trong mảng; `DailyRewardButton`, `DailyRewardManager`, `EnergyHUD`, `AvatarButton`, `SettingsButton` dùng `IMenuPopups`; bỏ fallback ở `LevelManager`, `LevelTimer`, `BoardCameraFitter`, `PlayZone`, `BackgroundFitter`, `DragAndDropController`, `InputManager`, `FreezeTutorialDemo`; cheat thua lấy `MoreTimePopup` qua `GameScene`.
- **5.2** Button (8 nút + `BoosterButton`, `MenuTabButton`), `CanvasGroup` (`PopupView`, `RevealBanner`), `SpriteRenderer` (`CatFaceAnimator`, `DragCountBubble`, `TutorialHand`, `BackgroundFitter`, `SelectionBoxView`), `Image` (`FreezeIndicator`, `PlayZone`), `UIParticle`, `TMP_Text`, `Canvas` cha, `MainMenuNavigator`, danh sách con (`CellView` khóa, `FreezeOverlayFX` viền) thành `[SerializeField]`. `SelectionBoxView` thêm `[RequireComponent(SpriteRenderer)]`.
- **5.3** `ButtonPress` (Selectable, mặt, Graphic được tô, con đứng yên), `UIButtonSounds` (nút Click / rung), `UIGrayscaleWhenDisabled` (Image / chữ con) có `EditorCollect()`; `UIButtonPress` chỉ còn hàm Editor gắn `ButtonPress`.
- **5.4** `CellView.HitBox`; `GridSpawner` đăng ký khi pool tạo ô, gỡ khi hủy.
- **5.5** `JsonFileSaveService.AttachRunner`, `SaveRunner.Schedule` (instance); `TrustedClockService(save, runner)`; `CatFaceTicker` chỉ còn `Update`; `AudioManager.EditorCreateSources` (menu chuột phải).
- **5.6** `EditorBuild` cho `BoosterUseFx`, `FreezeButtonCountdown`, `LowTimeVignette`, `TimerUI`, `BoosterTutorialView`, `BoosterButton` (Canvas + GraphicRaycaster), `PlacedBlockView` (lớp flash), `BlockFeedback`, `HintMarker`, `FreezeIndicator`, `WinPopup` (mẫu pool); `DailyGiftBubble` nhân bản `GiftItemView`; bỏ `Shader.Find` dự phòng của flash.
- Test: `RewardGranterTests`, `BoosterInventoryTests` thêm `null` cho tham số `Init` mới; `FeelPlayModeTests` dựng `SelectionBoxView` có sẵn renderer.

## Kiểm tra

- 160 / 160 test EditMode; compile sạch; Console 0 lỗi.
- Play với `save_v1_played.json` sau mỗi bước: nút Settings / Avatar / Daily / năng lượng / Lucky mở popup; nhấn nút lún + tô tối, nút menu rung, nút booster không rung; nút Spin xám khi thiếu sao, hết xám khi đủ; kéo chọn vùng, chạm gỡ khối; nhạc, SFX, save ghi xuống file (2140 coin), đồng bộ giờ server; mặt mèo chạy khung; bong thoại quà (x50, 15m); tutorial booster (đèn rọi, nút nhấc lên rồi trả lại, raycast trúng nút); bụi 12 hạt, 2 khung Hint, hiệu ứng booster, Freeze (10 bông tuyết, vòng đếm 20), "+15s", viền đỏ khi còn 6 s; thắng (flash 12 khối, 12 coin bay, +15 coin); cheat thua (MoreTime đóng, Lost, −1 năng lượng).
- Save thật của máy dev trả về bản gốc sau mỗi lần thử (md5 khớp).
- `using` sai chiều 80 → 76.

## Ghi chú

- Khác biệt nhỏ có chủ đích: Canvas + GraphicRaycaster của 3 nút booster có từ đầu (trước thêm khi tutorial mở khóa chạy lần đầu rồi giữ luôn).
- Ngoại lệ: `CheatPanel` tự tạo (công cụ debug, bọc `#if !DISABLE_CHEATS`).
- `GameBootstrap` 223 dòng, chưa tách installer; để P9.
