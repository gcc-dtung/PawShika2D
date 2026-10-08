# 02 – Mạng, Khiên, vòng đời 1 ván

## Tính năng làm gì

- Mỗi level có **3 mạng** (không có đồng hồ, không có sao). Đặt khối làm lưới hết lời giải → −1 mạng.
- **Khiên** (booster) chặn đúng 1 lần mất mạng tiếp theo trong level.
- Hết mạng → popup Hồi mạng (ads +1 / 900 coin +3) → bỏ cuộc thì Thua (−1 năng lượng) → Retry / Home.
- Thắng → màn WELL DONE (+10 coin, x2 bằng ads) → level kế.

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Lives/LivesManager` | Số mạng (`LivesEnergyConfig.maxLives` = 3), `LoseLife`, `AddLives`, `ActivateShield`; reset mỗi khi lưới mới sinh |
| `Gameplay/Level/GameState` | `Playing`, `Paused`, `Won`, `OutOfLives`, `Lost` |
| `Gameplay/Level/LevelManager` | Giữ `GameState`; `LoadLevel`, `Retry`, `NextLevel`, `Pause` / `Resume`, `Revive(lives)`, `GiveUp`; zoom camera vừa lưới |
| `UI/Popups/GameFlowUI` | Nghe `onStateChanged` → mở popup tương ứng |
| `UI/Popups/WinPopup`, `ReviveLivesPopup`, `LosePopup`, `QuitLevelPopup` | Các popup cuối ván |
| `UI/HUD/LivesUI`, `ShieldIndicator` | Hàng tim, lớp băng khi có khiên |

## Máy trạng thái

```text
              LoadLevel / Retry / Revive
                     │
                     ▼
   Pause() ◄──── Playing ────► (lưới kín) ──0.8 s──► Won ──► WinPopup ──► NextLevel
   Resume() ───►    │
                    └─ (hết mạng) ──0.8 s──► OutOfLives ──► ReviveLivesPopup
                                                 ├ Revive(n) ─► Playing (giữ nguyên lưới)
                                                 └ GiveUp()  ─► Lost ─► LosePopup ─► Retry / Home
```

- Chỉ `Playing` nhận thao tác kéo. Mọi popup mở giữa ván gọi `LevelManager.Pause()`.
- `stateChangeDelay` (0.8 s) chờ hiệu ứng mèo rung / lóe sáng xong rồi mới đổi trạng thái.
- Scene không có `GameFlowUI` (scene test) thì `LevelManager` tự chuyển level / chơi lại sau `autoAdvanceDelay`.
- `EnergyManager` nghe `Lost` → trừ 1 năng lượng. Thoát giữa ván (`QuitLevelPopup`) cũng trừ 1.

## Khiên

`LivesManager.LoseLife()` khi có khiên: không trừ, tắt khiên, phát `onShieldBlocked`. Khiên mất khi nạp level mới (`ResetLives`).

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Số mạng mỗi level | `Configs/LivesEnergyConfig.asset` → `maxLives`. `LevelDefinition.rules.mistakeLimit` hiện **chưa dùng** – muốn mạng theo từng level thì đọc field này trong `LivesManager.ResetLives` |
| Giá / số mạng khi hồi | `Configs/EconomyConfig.asset` (`reviveCoinCost`, `reviveCoinLives`, `reviveAdsLives`) |
| Coin thưởng khi thắng | `Configs/EconomyConfig.asset` → `winCoinReward` |
| Giới hạn số lần hồi mạng | `LevelManager.HasRevived` đã có sẵn – kiểm tra trong `ReviveLivesPopup.OnOpen` |
| Thêm trạng thái ván mới (vd. "Hết giờ") | Thêm giá trị vào `GameState`, set trong `LevelManager`, thêm `case` trong `GameFlowUI.OnStateChanged`. Kiểm tra lại mọi chỗ so `== GameState.Playing` |
| Hết level thì làm gì | `LevelManager.NextLevel` (hiện quay vòng về level 1) |

## Lưu ý

- Đổi level (kể cả bằng Debug Panel) thì `GameFlowUI` đóng mọi popup cuối ván; popup bắt buộc nạp năng lượng thì giữ.
- Trong tutorial lần đầu (`TutorialController.Active`) đặt sai **không** trừ mạng và thắng không mở WinPopup.
