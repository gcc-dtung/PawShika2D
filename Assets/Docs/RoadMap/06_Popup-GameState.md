# 06 – Giai đoạn 3: GameState, popup Thắng / Hồi mạng / Thua; chuyển animation sang DOTween

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | `93c8844` "Popup thắng/hồi mạng/thua, GameState, coin, chuyển anim sang DOTween" (nhánh `Day-2`) |
| Thời gian push | 2026-10-03 01:13 |
| Cập nhật lần cuối | 2026-10-03 01:13 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 00:58 (03/10) | Giai đoạn 3: vòng chơi hoàn chỉnh | Thắng → WinPopup (+coin, x2 ads) → level kế; hết mạng → popup Hồi mạng (ads +1 / 900 coin +3 / đóng) → Thua → Retry; nút Replay hỏi xác nhận; còn 1 mạng thì viền đỏ nhấp nháy |
| 01:09 (03/10) | Bỏ viền đỏ còn 1 mạng; coroutine → DOTween | Viền đỏ che lưới, khó chơi → xóa. Mọi animation / hẹn giờ dùng DOTween, tween bị hủy khi object tắt |

---

## 00:58 – Giai đoạn 3: GameState + popup

### Mục tiêu

Bỏ việc tự chuyển level / tự chơi lại sau 1.5 giây; thay bằng các popup của Giai đoạn 3 trong `TaskBreakdown.md` (thay `MoreTimePopup`, `WinUI` sao theo giờ, `hardLayer` của bản gốc).

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Gameplay/Level/GameState.cs` | Enum Playing, Paused, Won, OutOfLives, Lost |
| Sửa | `Gameplay/Level/LevelManager.cs` | Giữ `State`, event `onStateChanged`; `Pause`/`Resume`/`Revive(lives)`/`GiveUp`. Thắng / hết mạng đổi trạng thái sau `stateChangeDelay` 0.8 s (để hiệu ứng khối chạy xong). Chỉ tự chuyển level / chơi lại khi scene không có `GameFlowUI` |
| Sửa | `Gameplay/Core/GridManager.cs` | `IsInputLocked` thêm điều kiện `!LevelManager.IsPlaying` (popup đang mở thì không kéo được) |
| Sửa | `Gameplay/Input/InputManager.cs` | Bấm trúng UI (nút, popup) thì không bắt đầu kéo trên lưới |
| Thêm | `Gameplay/Economy/CoinWallet.cs` | Coin lưu PlayerPrefs `PawBlock_Coins` |
| Thêm | `Gameplay/Economy/RewardedAds.cs` | Quảng cáo có thưởng giả lập (0.5 s, luôn thành công), gắn trên `Manager` |
| Thêm | `UI/Popups/PopupView.cs` | Base popup: nền tối + bảng bật lên, thời gian thực |
| Thêm | `UI/Popups/GameFlowUI.cs` | Nối `GameState` → popup; `RequestReplay` |
| Thêm | `UI/Popups/WinPopup.cs` | +10 coin (`gamePlay_CoinWinLevelNormal` của RemoteConfig gốc), x2 bằng ads |
| Thêm | `UI/Popups/ReviveLivesPopup.cs` | Ads +1 mạng, 900 coin +3 mạng, X = thua. Hồi mạng chơi tiếp lưới đang dở |
| Thêm | `UI/Popups/LosePopup.cs` | Retry |
| Thêm | `UI/Popups/ConfirmReplayPopup.cs` | Hỏi lại khi bấm Replay, tạm dừng ván |
| Thêm | `UI/HUD/LowLivesWarning.cs` | Viền đỏ nhấp nháy khi còn 1 mạng (vignette tạo bằng code, alpha 0.15–0.5) |
| Sửa | `UI/HUD/LevelHUD.cs` | Nút Replay gọi `GameFlowUI.RequestReplay` |
| Thêm | `Editor/UI/PopupBuilder.cs` | Menu **Tools → PawBlock → Build Popups**: dựng `Canvas_HUD/Popups` + `LowLivesWarning` từ sprite gốc (`popup_1`, `btn_green_1`, `btn_yellow_1`, `btn_red_1`, `btn_X_1`, `coin`, `heart_1/2`, `btn_video`), nối reference |
| Sửa | `Scenes/SampleScene.unity` | Kết quả chạy Build Popups |
| Sửa | `Docs/README.md`, `Docs/TaskBreakdown.md`, `Docs/ProjectStructure.md` | Trạng thái Giai đoạn 3, script / scene / luồng / event mới |

### Tác dụng

- Vòng chơi khép kín: vào level → chơi → thắng / thua → level kế hoặc chơi lại, không còn tự chuyển.
- Hồi mạng giữ nguyên các khối đã đặt; `GridManager` mở khóa lại ngay vì `LivesManager.IsOutOfLives` hết đúng.
- Giá hồi mạng bằng coin (900) là số tạm, chỉnh trong Inspector của `ReviveLivesPopup`.
- Chữ popup tiếng Anh như bản gốc, font vẫn LiberationSans (Fredoka hỏng).

### Kiểm tra (Play mode, level 20)

| Bước | Kết quả |
| --- | --- |
| 3 nước sai liên tiếp | Mạng 3 → 0; sau 0.8 s state `OutOfLives`, popup Hồi mạng (nút coin mờ vì 0 coin) |
| Bấm "+1" (ads) | State `Playing`, 1 mạng, lưới mở khóa, khối giữ nguyên; viền đỏ nhấp nháy |
| Sai thêm 1 lần → bấm X | State `Lost`, popup FAILED |
| Retry | Level 20 nạp lại, 3 mạng, `Playing` |
| Bấm nút Replay trên HUD | Popup REPLAY?, state `Paused`, lưới khóa |
| NO | `Playing` |
| Đặt 10 khối theo lời giải | Popup COMPLETED! +10 |
| x2 | Quảng cáo giả lập → coin 20, sang level 21 |
| Console | Không lỗi / cảnh báo mới |

Sau khi thử đã trả `PawBlock_CurrentLevel` về 19 và xóa `PawBlock_Coins`.

Ghi chú: Unity bị thu nhỏ (minimized) nên không biên dịch, MCP timeout; khôi phục cửa sổ thì chạy tiếp.

### Còn lại của Giai đoạn 3

- Banner độ khó (cần trường độ khó trong `LevelDefinition`)
- Làm đẹp HUD (khung LEVEL dùng sprite gốc)
- Hiệu ứng thắng (pháo hoa `05_VFX_Particles/Fireworks`, mèo) – chưa có

---

## 01:09 – Bỏ viền đỏ còn 1 mạng; chuyển coroutine sang DOTween

### Vấn đề / Yêu cầu

1. Viền đỏ khi còn 1 mạng phủ gần hết màn hình, nhìn lưới khó → bỏ.
2. Mọi chỗ dùng coroutine (animation, hẹn giờ) chuyển sang DOTween. Tween phải được giữ lại và hủy khi GameObject tắt.

### Thay đổi

| Loại | File | Nội dung |
| --- | --- | --- |
| Thêm | `Assets/Plugins/Demigiant/DOTween/` | Import `DOTween HOTween v2.unitypackage` (bản Asset Store đã tải trong máy). Module UI / Sprite bật sẵn |
| Thêm | `Assets/Resources/DOTweenSettings.asset` | DOTween tự tạo khi import |
| Sửa | `ProjectSettings/ProjectSettings.asset` | DOTween tự thêm define `DOTWEEN` cho mọi platform |
| Xóa | `UI/HUD/LowLivesWarning.cs` | Bỏ hiệu ứng |
| Sửa | `Editor/UI/PopupBuilder.cs` | Không dựng `LowLivesWarning` nữa (vẫn xóa object cũ nếu còn) |
| Sửa | `Scenes/SampleScene.unity` | Chạy lại Build Popups: hết object `LowLivesWarning` |
| Sửa | `Gameplay/Block/PlacedBlockView.cs` | `PopIn` = `DOScale` Yoyo; `RingIn`, `RejectRoutine` = `DOVirtual.Float` / `Sequence` giữ nguyên công thức cũ (feel không đổi). Field `Tween _anim`, `_ringAnim`; kill trong `ResetView`, `OnDisable` |
| Sửa | `Gameplay/Feedback/GameplayFeedback.cs` | Chữ khen = `Sequence` (scale 0.3 → 1.15 → 1, bay lên, `DOFade`), danh sách tween; `OnDisable` `Kill(true)` để chữ về pool. Rung camera = `DOVirtual.Float`, `OnKill` trả camera về gốc |
| Sửa | `UI/HUD/LivesUI.cs` | Tim nảy + lắc = `DOVirtual.Float`, mỗi tim 1 tween; `OnDisable` kill, `OnKill` trả scale / xoay về gốc |
| Sửa | `UI/Popups/PopupView.cs` | `DOFade` CanvasGroup + `DOScale` panel `Ease.OutBack` (đúng đường cong cũ), `SetUpdate(true)`; kill trong `OnDisable` / `CloseImmediate` |
| Sửa | `Gameplay/Level/LevelManager.cs` | Hẹn đổi trạng thái / tự chuyển level = `DOVirtual.DelayedCall`; kill trong `CancelPending` và `OnDisable` |
| Sửa | `Gameplay/Economy/RewardedAds.cs` | Giả lập quảng cáo = `DOVirtual.DelayedCall(ignoreTimeScale)`; kill trong `OnDisable` |
| Sửa | `Docs/ProjectStructure.md`, `README.md`, `TaskBreakdown.md` | Quy ước DOTween, bỏ `LowLivesWarning` |

### Quy ước an toàn đã áp dụng

- Tween giữ trong field, `SetLink(gameObject)`, `Kill()` trong `OnDisable`.
- `OnKill` chỉ xóa field nếu vẫn là tween đó: Kill gọi trong callback của chính tween (vd. khối sai trả về pool trong `OnComplete`) bị DOTween hoãn, không được xóa nhầm tween mới.
- Thứ cần về trạng thái gốc khi bị hủy (camera, tim, chữ khen) đặt trong `OnKill` nên chạy cả khi xong lẫn khi bị hủy.

### Kiểm tra (Play mode, level 22)

| Bước | Kết quả |
| --- | --- |
| `grep Coroutine/IEnumerator/yield` trong Script | Không còn |
| Đặt 1 khối đúng + 1 nước sai | Khối bật, mèo xám rung rồi về pool, camera rung về đúng `(0, -0.35)`, tim nảy về scale 1 / xoay 0; hết tween đang chạy |
| Tắt khối đang rung sai | Số tween đang chạy giảm 1, không lỗi |
| Sai tới hết mạng | Popup Hồi mạng bật lên (alpha 1, scale 1) |
| Ads +1 | `Playing`, 1 mạng, **không còn viền đỏ** |
| Mở popup xác nhận rồi `CloseImmediate` ngay | Tween của popup bị hủy (2 → 1, còn tween quảng cáo) |
| Giải hết level → Next | Level 23, +10 coin |
| Thoát Play khi tween đang chạy | Console không lỗi |

Đã trả `PawBlock_CurrentLevel` = 21, `PawBlock_Coins` = 20 như trước khi thử.

Ghi chú: Unity không chạy frame khi mất focus (`Run In Background` tắt) nên khi thử bằng MCP phải đưa cửa sổ Unity lên trước; đã bật `Application.runInBackground` tạm trong phiên Play (không lưu vào ProjectSettings).

