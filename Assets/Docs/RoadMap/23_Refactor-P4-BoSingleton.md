# 23 – Refactor P4: bỏ Singleton game logic, cờ static

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-12 22:30 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 16:46 (12/10) | P4.1 – `ScreenFlow` | Cài `IScreenNavigator` (`IsMenu`, `ShowMenu`, `Play`, `PlayDaily`); Gameplay / meta hết gọi ngược `ScreenFlow.Instance`; Shop mở qua `ShopOpenRequestedEvent` |
| 16:51 (12/10) | P4.2 – `LevelManager` | Cài `IGamePause` (`IsPlaying`, `Pause`, `Resume`, `CancelDrag`); popup cùng Gameplay nhận qua `Init`; `DailyChallengeManager` nghe `LevelCompletedEvent` |
| 16:54 (12/10) | P4.3 – `GridManager` | Popup Settings hủy kéo qua `IGamePause.CancelDrag` |
| 16:58 (12/10) | P4.4 – `BoosterManager` | Cài `IBoosterCatalog.ItemIcon`; `DailyRewardManager` cài `IRewardIcons.IconOf` cho ô quà, bong bóng quà, Lucky Wheel |
| 17:02 (12/10) | P4.5 – `LevelTimer` | 7 file HUD / booster nhận qua `Init` |
| 17:04 (12/10) | P4.6 – `EnergyManager` | `QuitLevelPopup` dùng `IEnergyService` |
| 17:07 (12/10) | P4.7 – 4.9 – Daily*, `LevelStars`, `TutorialController` | Bỏ Singleton; `TutorialController.Active` / `Pending` static → `IsActive` / `IsPending` của instance |
| 22:21 (12/10) | P4.10 – `GameConfig`, `GameplayVisuals` | Bỏ static, ra khỏi `Resources/` → `_Project/Configs/` (giữ GUID); Bootstrap truyền config con qua `Init` |
| 22:25 (12/10) | P4.11 – Singleton an toàn | `Singleton<T>` không Find / tự tạo, `LogError` khi thiếu, `HasInstance`; chỉ còn `AudioManager` dùng |

---

## Thêm

| File | Nội dung |
| --- | --- |
| `Core/Contracts/Interfaces/IScreenNavigator` | Hỏi đang ở menu, về menu, chơi level, chơi Daily (cài ở `ScreenFlow`) |
| `Core/Contracts/Interfaces/IGamePause` | Đang chơi, dừng / chạy tiếp, hủy kéo (cài ở `LevelManager`) |
| `Core/Contracts/Interfaces/IBoosterCatalog` | Icon vật phẩm của booster (cài ở `BoosterManager`) |
| `Core/Contracts/Interfaces/IRewardIcons` | Icon của một món quà (cài ở `DailyRewardManager`) |
| `Game/Scripts/GameScene` | Object trong scene cho Cheat / Debug Panel (Screens, Levels, Grid, Boosters, Timer, Energy, DailyReward, LivesEnergy) |

## Sửa

- 10 Singleton game logic bỏ `: Singleton<T>`: `ScreenFlow`, `LevelManager`, `GridManager`, `BoosterManager`, `LevelTimer`, `EnergyManager`, `DailyRewardManager`, `DailyChallengeManager`, `LevelStars`, `TutorialController` (+ cờ static). Nơi gọi nhận tham chiếu qua `Init` từ `GameBootstrap` (`GameBootstrapCollector` tự điền field mới).
- Thuộc tính / hàm static đang đọc `.Instance` → instance: `WinPopup` (thưởng), `LevelStatsRecorder` (`InMenu`, `IsPractice`), `MechanicIntro`, `BoosterTutorial.Unlock`, `DailyChallengePanel.Manager`.
- `GameConfig` bỏ `Instance` / `Economy` / `LivesEnergy` static; `GameplayVisuals` bỏ `Instance`. `GameBootstrap` có `[SerializeField] GameConfig config; GameplayVisuals visuals;`, truyền `LivesEnergyConfig`, `EconomyConfig`... cho từng component.
- `IBoosterHost.FreezeSeconds` (Freeze booster không đọc config static); `LevelTimer.ComputeSeconds(level, cells, cfg)`, `EstimateAutoSeconds(blocks, cells, cfg)` nhận config; `PlacedBlockView.Init(visuals)` gọi trong `createFunc` của pool ở `GridManager`.
- `Singleton<T>`: đăng ký ở `Awake`, xóa ở `OnDestroy`, thiếu thì `LogError`, `HasInstance`; bản trùng chỉ hủy component. `AudioManager`, `GameAudioEvents` dùng `HasInstance`. `CatFaceTicker` tự quản instance (không kế thừa Singleton).
- `DebugCheats.Init(GameServices, GameScene)`; truy cập `DebugCheats.Levels` / `Energy` / `CoinBalance` / `StarBalance`.
- Test: `RewardGranterTests`, `BoosterInventoryTests` truyền thêm `null` cho tham số `Init` mới.

## Xóa / di chuyển

- `Resources/GameConfig.asset`, `Resources/GameplayVisuals.asset` → `_Project/Configs/` (di chuyển trong Unity, GUID giữ nguyên); thư mục `_Project/Resources` xóa.
- `DontDestroyOnLoadSingleton<T>`.

## Kiểm tra

- 160 / 160 test EditMode; compile sạch; `grep .Instance` trong `Features/`, `Game/` chỉ còn `AudioManager`.
- Play với `save_v1_played.json` sau mỗi mục: menu, popup Settings / Profile / DailyReward / Refill / LuckyWheel, đổi nhạc, Shop, tab Daily, PLAY level 23, Time's up → quảng cáo +30 s, thắng → Continue (+coin), booster. Console 0 lỗi.
- Save thật của máy dev trả về bản gốc sau mỗi lần thử (md5 khớp).
- `using` sai chiều 75 → 80: `LivesEnergyConfig` (Economy) nay truyền thẳng vào Gameplay thay vì qua `GameConfig` (Game); chuyển ở P6.

## Ghi chú

- Không thêm `EnergyChangedEvent`, `IAdsFreeStatus`: chưa có feature thứ hai cần.
- `GameBootstrap` 214 dòng (> 150), chưa tách installer; để P5 / P9.
- Có sẵn, chưa sửa: `EnergyManager.InfiniteSecondsLeft` tràn `(int)` khi hạn vô hạn quá xa; DOTween nới sức chứa khi cheat WinNow trên lưới 10x10 (đỉnh tạm thời).
