# 12 – Âm thanh, rung, phản hồi

## Tính năng làm gì

- Nhạc nền lặp (fade khi đổi / bật / tắt, nhanh lên khi sắp hết giờ), hiệu ứng âm (SFX) theo sự kiện game, rung máy kiểu haptic theo sự kiện game.
- Cài đặt Nhạc / Âm / Rung lưu trong save (section `"settings"`).
- Phản hồi hình ảnh: chữ khen + combo khi đặt khối, mèo sai rung, hạt bụi, camera zoom khối cuối ([14](14_GameFeel.md)).

## File liên quan

| File | Việc |
| --- | --- |
| `Core/Contracts/Types/SoundId` | Enum các âm (có số cố định, nhóm theo hàng chục) |
| `Core/Foundation/Audio/AudioDatabase` (+ asset `Shared/Configs/AudioDatabase.asset`) | Mỗi `SoundId` → nhiều clip (phát ngẫu nhiên), âm lượng, cao độ, cooldown; nhạc nền |
| `Core/Foundation/Audio/AudioManager` | `AudioManager.Play(id)`, `PlayPitched`, `PlayChance(id, p)`, `Vibrate(HapticType)`; `Music` + `Sfx_0..7` là AudioSource đặt sẵn trong scene; `MusicOn` / `SoundOn` / `VibrateOn`, event `SettingsChanged`. Singleton hạ tầng duy nhất ([ProjectConventions](../Refactor/ProjectConventions.md) mục 9) |
| `Game/Scripts/GameAudioEvents` | **Nối âm vào event có sẵn** – không script luật game nào gọi âm trực tiếp (trừ UI chung) |
| `Features/Gameplay/Scripts/Feedback/GameHapticEvents` | **Nối rung vào event có sẵn** (trên object `AudioManager`), đi qua `AudioManager.Vibrate` nên theo cài đặt Rung |
| `Core/Foundation/Feedback/Haptics` (+ `Plugins/iOS/HapticsIOS.mm`) | Rung ngắn có cường độ: Android `Vibrator` + `VibrationEffect` (API 26+; máy cũ chỉ chỉnh thời gian), iOS `UIImpactFeedbackGenerator`; Editor không rung |
| `Features/Gameplay/Scripts/Feedback/GameplayFeedback`, `BlockFeedback`, `DragFeedback` | Phản hồi hình ảnh |
| `Shared/Scripts/UI/UIButtonSounds` | Âm Click + rung nhẹ cho mọi nút |
| `Game/Editor/AudioSetup` | **Setup Audio**: tạo / bổ sung database (chỉ thêm âm thiếu, clip lấy từ `Shared/Audio`), gán vào `AudioManager` |

## Âm được nối ở đâu (`GameAudioEvents`)

| Event | Âm |
| --- | --- |
| `GridManager.onBlockPlaced` | BlockPlace (cao dần theo combo) + CatHappy (xác suất) |
| `GridManager.onBlockRemoved` | BlockRemove |
| `GridManager.onMistake` | Mistake + CatSad (xác suất) |
| `GridManager.onInvalidMove` | DragCancel |
| `GridManager.onSelectionChanged` | DragTick (cao dần theo số ô), DragReady khi đủ điều kiện |
| `GridManager.onClueUnlocked` | Unlock |
| `EventBus<GameStateChangedEvent>` | Win / OutOfTime / Lose |
| `EventBus<StarsChangedEvent>` (mất sao) | StarLost |
| `GridSpawner.gridCellEvent` | LevelStart (không phát khi ở Menu) |
| `LevelTimer.onFreezeChanged` / `onFreezeEnded` / `onTimeWarning` / `onTimeAdded` | FreezeOn / FreezeEnd / TimeWarning (+ nhạc nhanh) / thêm giờ |
| `BoosterManager.onBoosterUsed` | Booster |
| `EventBus<CoinsChangedEvent>` (tăng) | Coin |

## Rung được nối ở đâu (`GameHapticEvents`)

| Event | Rung |
| --- | --- |
| `onDragCellChanged` | Selection (rất nhẹ, tắt được trên component) |
| `onBlockPlaced` / `onBlockRemoved` / `onInvalidMove` / `onClueUnlocked` | Light / Soft / Soft / Rigid |
| `onMistake` | Heavy |
| `onBoosterUsed` (trừ Đóng băng) | Medium |
| `LevelTimer.onFreezeChanged` / `onFreezeEnded` | Rise / Thud |
| `LevelTimer.onTimeWarning` / `onLowTick` | Warning / Tick |
| `GameStateChangedEvent` Won / Lost | Success / Failure |

`Haptics` bỏ qua lần rung cách lần trước < 0.03 s (nhiều sự kiện cùng frame). Thử từng kiểu: Cheat Panel → tab Khác → Thử rung.

## Thêm âm mới

1. Thêm giá trị vào `SoundId` (Contracts) với **số mới chưa dùng** (đừng đánh lại số cũ – asset lưu theo số).
2. Đặt file âm vào `Shared/Audio`, **Tools → PawBlock → Builders → Setup Audio** (thêm mục còn thiếu vào database) → gán clip trong `AudioDatabase.asset`.
3. Phát âm: thêm 1 handler trong `GameAudioEvents` nghe event có sẵn. Chưa có event phù hợp → thêm `UnityEvent` vào component liên quan (hoặc event contract nếu nguồn ở feature khác) rồi nghe nó, **đừng** gọi `AudioManager.Play` từ trong luật game.
