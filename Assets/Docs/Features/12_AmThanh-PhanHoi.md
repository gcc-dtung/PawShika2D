# 12 – Âm thanh, rung, phản hồi

## Tính năng làm gì

- Nhạc nền lặp (fade khi đổi / bật / tắt), hiệu ứng âm (SFX) theo sự kiện game, rung máy kiểu haptic (nhẹ / vừa / mạnh / nhịp thắng - thua) theo sự kiện game.
- Cài đặt Nhạc / Âm / Rung lưu trong save.
- Phản hồi hình ảnh: chữ khen khi đặt đúng, rung camera + chớp đỏ khi sai.

## File liên quan

| File | Việc |
| --- | --- |
| `Gameplay/Audio/SoundId` | Enum các âm (có số cố định, nhóm theo hàng chục) |
| `Gameplay/Audio/AudioDatabase` (+ asset `Configs/AudioDatabase.asset`) | Mỗi `SoundId` → nhiều clip (phát ngẫu nhiên), âm lượng, cao độ, cooldown; nhạc nền |
| `Gameplay/Audio/AudioManager` | `AudioManager.Play(id)`, `PlayChance(id, p)`, `Vibrate(HapticType)`; pool AudioSource; `MusicOn` / `SoundOn` / `VibrateOn` |
| `Gameplay/Audio/GameAudioEvents` | **Nối âm vào event có sẵn** – không script gameplay nào gọi âm trực tiếp (trừ UI chung) |
| `Gameplay/Feedback/GameplayFeedback` | Chữ khen (pool), rung camera |
| `Gameplay/Feedback/Haptics` (+ `Plugins/iOS/HapticsIOS.mm`) | Rung ngắn có cường độ: Android `Vibrator` + `VibrationEffect` (API 26+; máy cũ chỉ chỉnh thời gian), iOS `UIImpactFeedbackGenerator`; Editor không rung |
| `Gameplay/Feedback/GameHapticEvents` | **Nối rung vào event có sẵn** (trên object `AudioManager`), đi qua `AudioManager.Vibrate` nên theo cài đặt Rung |
| `UI/Common/UIButtonSounds` | Âm Click cho mọi nút |
| `Editor/Audio/AudioSetup` | **Setup Audio**: tạo / bổ sung database (chỉ thêm âm thiếu), object `AudioManager` |

## Âm được nối ở đâu (`GameAudioEvents`)

| Event | Âm |
| --- | --- |
| `GridManager.onBlockPlaced` | BlockPlace + CatHappy (35 %) |
| `GridManager.onMistake` | Mistake + CatSad (80 %) |
| `GridManager.onClueUnlocked` | Unlock |
| `EventBus<GameStateChangedEvent>` | Win / OutOfLives / Lose |
| `GridSpawner.gridCellEvent` | LevelStart (không phát khi ở Menu) |
| `LivesManager.onShieldChanged / onShieldBlocked` | ShieldOn / ShieldBlock |
| `BoosterManager.onBoosterUsed` | Booster |
| `CoinWallet.Changed` (tăng) | Coin |

## Rung được nối ở đâu (`GameHapticEvents`)

| Event | Rung |
| --- | --- |
| `GridManager.onBlockPlaced` / `onClueUnlocked` | Light (tắt được bằng checkbox trên component) |
| `BoosterManager.onBoosterUsed`, `LivesManager.onShieldBlocked` | Medium |
| `GridManager.onMistake` | Heavy |
| `GameStateChangedEvent` Won | Success (2 nhịp nhẹ → mạnh) |
| `GameStateChangedEvent` OutOfLives / Lost | Failure (2 nhịp mạnh) |

`Haptics` bỏ qua lần rung cách lần trước < 0.03 s (nhiều sự kiện cùng frame). Thử từng kiểu: Cheat Panel → tab Khác → Thử rung.

## Thêm âm mới

1. Thêm giá trị vào `SoundId` với **số mới chưa dùng** (đừng đánh lại số cũ – asset lưu theo số).
2. **Tools → PawBlock → Builders → Setup Audio** (thêm mục còn thiếu vào database) → gán clip trong `AudioDatabase.asset`.
3. Phát âm: thêm 1 handler trong `GameAudioEvents` nghe event có sẵn. Chưa có event phù hợp → thêm `UnityEvent` vào manager liên quan rồi nghe nó, **đừng** gọi `AudioManager.Play` từ trong luật game.

## Thêm phản hồi khi thao tác sai luật

`GridManager.onInvalidMove(PlacementResult, center, size)` đã phát nhưng **chưa ai nghe**. Ví dụ: rung nhẹ khung khi `WrongSize`, hiện chữ "Locked!" khi `Locked` – thêm listener trong `GameplayFeedback`.
