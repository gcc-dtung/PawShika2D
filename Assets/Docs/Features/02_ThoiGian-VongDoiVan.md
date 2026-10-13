# 02 – Đồng hồ, vòng đời 1 ván

> Đường dẫn tính từ `Assets/_Project/Features/Gameplay/Scripts/` (trừ khi ghi rõ). Từ nhánh `Old-Core` (RoadMap 17) game bỏ 3 mạng / Khiên, quay lại đồng hồ đếm ngược như bản gốc.

## Tính năng làm gì

- Mỗi level có **đồng hồ đếm ngược**; đặt khối không bị phạt, chỉ cần phủ kín lưới đúng trước khi hết giờ.
- Số sao (3 / 2 / 1 / 0) theo phần thời gian còn lại ([15](15_Sao.md)).
- Hết giờ → popup Thêm giờ (ads +30 s / 900 coin +60 s) → bỏ cuộc thì Thua (−1 năng lượng) → Retry / Home.
- Thắng → màn thắng (coin theo độ khó + sao, x2 bằng ads) → level kế.

## File liên quan

| File | Việc |
| --- | --- |
| `Timer/LevelTimer` | Tổng giờ = `rules.timeLimit` của level nếu > 0, không thì `baseSeconds + secondsPerCell × ô + (5.5–6.5 s) × khối`, tối thiểu `minSeconds`. Đứng ở tổng giờ tới khi người chơi chạm lần đầu; chỉ chạy khi thao tác được. Freeze dừng `freezeSeconds`. Event `onTimeChanged`, `onTimeWarning`, `onLowTick`, `onTimeAdded`, `onTimeUp`, `onFreezeChanged` |
| `Core/Contracts/Types/GameState` | `Playing`, `Paused`, `Won`, `OutOfTime`, `Lost` |
| `Level/LevelManager` | Giữ `GameState`; `LoadLevel`, `LoadDaily`, `LoadTutorial`, `Retry`, `NextLevel`, `Pause` / `Resume` (`IGamePause`), `Revive(seconds)`, `GiveUp`; phát `LevelLoadedEvent`, `GameStateChangedEvent`, `LevelCompletedEvent`, `LevelFailedEvent`, `AllLevelsCompletedEvent`; ván không Playing thì khóa lưới (`InputGate.Set`) |
| `Visuals/BoardCameraFitter` | Căn camera vừa lưới mỗi khi `GridSpawner` dựng lưới mới |
| `Game/Scripts/GameFlowUI` | Nghe `GameStateChangedEvent` → mở popup; đặt `LevelManager.AutoAdvance = false` |
| `UI/WinPopup`, `MoreTimePopup`, `LosePopup`, `QuitLevelPopup` | Popup cuối ván / thoát giữa ván (prefab ở `Features/Gameplay/Prefabs/Popups`) |
| `UI/TimerUI`, `TimeStarBar`, `LowTimeVignette`, `FreezeIndicator` | Đồng hồ trên HUD, thanh 3 mốc sao, viền đỏ sắp hết giờ, lớp băng khi Freeze |

## Máy trạng thái

```text
              LoadLevel / Retry / Revive
                     │
                     ▼
   Pause() ◄──── Playing ────► (lưới kín, đúng) ──0.8 s──► Won ──► WinPopup ──► NextLevel
   Resume() ───►    │
                    └─ (LevelTimer.onTimeUp) ──0.8 s──► OutOfTime ──► MoreTimePopup
                                                   ├ Revive(giây) ─► Playing (giữ nguyên lưới)
                                                   └ GiveUp()     ─► Lost ─► LosePopup ─► Retry / Home
```

- Chỉ `Playing` nhận thao tác kéo. Popup mở giữa ván (Settings, Profile, Notice, BuyBooster...) gọi `IGamePause.Pause()`.
- `stateChangeDelay` (0.8 s) chờ hiệu ứng lóe sáng xong rồi mới đổi trạng thái.
- Scene không có `GameFlowUI` (scene test) thì `LevelManager` tự chuyển level / chơi lại sau `autoAdvanceDelay`.
- `EnergyManager` nghe `Lost` → trừ 1 năng lượng. Thoát giữa ván (`QuitLevelPopup`) cũng trừ 1 qua `IEnergyService.ConsumeOne`.

## Sửa / mở rộng

| Muốn | Sửa ở |
| --- | --- |
| Công thức thời gian, ngưỡng cảnh báo, giây Freeze | `Features/Gameplay/Configs/LivesEnergyConfig` (`baseSeconds`, `secondsPerCell`, `secondsPerBlockMin/Max`, `minSeconds`, `warningSeconds`, `freezeSeconds`) |
| Thời gian riêng 1 level | Level Editor → `rules.timeLimit` |
| Thêm giờ: giây / giá | `Features/Gameplay/Configs/EconomyConfig` (`moreTimeAdsSeconds`, `moreTimeCoinSeconds`, `moreTimeCoinCost`) |
| Coin thưởng khi thắng | `EconomyConfig` → `winCoinReward` (Normal 10) / `winCoinRewardHard` (20) / `winCoinRewardVeryHard` (30) + `coinsPerStar` (5) × sao, qua `WinCoinReward(difficulty, stars)` |
| Giới hạn số lần thêm giờ | `LevelManager.HasRevived` đã có sẵn – kiểm tra trong `MoreTimePopup` |
| Thêm trạng thái ván mới | Thêm giá trị vào `GameState` (Contracts), set trong `LevelManager`, thêm `case` trong `GameFlowUI`. Kiểm tra lại mọi chỗ so `== GameState.Playing` |
| Hết level thì làm gì | `LevelManager.NextLevel`: level cuối → `AllLevelsCompletedEvent` → `GameFlowUI` hiện "COMING SOON" (`comingSoonTitle / comingSoonMessage`), Continue → `RestartFromFirst`; không có `GameFlowUI` thì quay vòng luôn |

## Lưu ý

- Đổi level (kể cả bằng Debug Panel) thì `GameFlowUI` đóng mọi popup có `CloseOnLevelLoad` (Notice và popup bắt buộc nạp năng lượng thì giữ).
- Tutorial lần đầu (`LevelManager.IsPractice`) và Daily Challenge (`IsDaily`) dùng chung máy trạng thái; tutorial không phát `LevelLoadedEvent` / `LevelCompletedEvent` nên không tính chỉ số hồ sơ.
