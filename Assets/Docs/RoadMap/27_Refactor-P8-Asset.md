# 27 – Refactor P8: asset theo module, Sprite Atlas, nén ASTC

| | |
| --- | --- |
| Trạng thái | Đã push |
| Commit | 5 commit `cf1625a` … `3fbc456` (P8.1 – P8.3 + tài liệu), cuối "Refactor P8: tài liệu P8, chốt RoadMap 26" (nhánh `Refactor-P8-Asset`) |
| Thời gian push | 2026-10-13 23:02 |
| Cập nhật lần cuối | 2026-10-13 23:03 |

## Tóm tắt

| Giờ | Đợt | Kết quả |
| --- | --- | --- |
| 22:41 (13/10) | Merge P7 | `Refactor` fast-forward tới `4f44f5a`, tạo nhánh `Refactor-P8-Asset` |
| 22:49 (13/10) | P8.1 – di chuyển asset | 261 asset đang dùng về `Art/` / `Audio/` / `Fonts/` của module; bản gốc sang `ThirdParty/PawBlock`, `ThirdParty/ShikaCat`; config về `Configs/` của feature, level về Gameplay, scene về `_Project/Scenes`; GUID giữ |
| 22:51 (13/10) | P8.2 – Sprite Atlas | 7 atlas V2 (Shared, Gameplay, Game, DailyChallenge, Profile, Settings, Economy) |
| 22:57 (13/10) | P8.3 – preset ASTC | 3 preset texture theo thư mục; override Android 207 texture + 7 atlas |

## Số liệu so với P0

| | P0 | P8 |
| --- | --- | --- |
| Batches / SetPass Menu | 63 / 38 | **46 / 39** |
| Batches / SetPass màn chơi (level 39) | 58 / 46 | **53 / 49** |
| Batches / SetPass Win | 133–136 / 94–97 | **92–96 / 71–73** |
| APK | 58.9 MB | **56.5 MB** |
| Asset người dùng (không nén) | 145.5 MB | **49.6 MB** |
| Test | 104 | **160** |

---

## P8.1 – asset về module

| Nguồn | Đích |
| --- | --- |
| `_Project/GameAssets` (bản gốc) | `Assets/ThirdParty/PawBlock` (giữ cấu trúc, gồm `Texture2D`, `10_Data_Configs`) |
| `_Project/ShikaCat_GameAssets` | `Assets/ThirdParty/ShikaCat` |
| Ảnh / Spine / material / shader đang dùng (261) | `<module>/Art/...`; dùng ≥ 2 module → `Shared/Art` |
| Âm thanh, font | `Shared/Audio` (`ShikaCat/` riêng), `Shared/Fonts` |
| `Configs/*` | `Configs/` của feature sở hữu; `GameConfig` → `Game/Configs`, `AudioDatabase` → `Shared/Configs` |
| `Levels/` | `Features/Gameplay/Configs/Levels` |
| `Prefabs/` cũ (Cell, PlacedBlock, DragCountBubble...) | `Features/Gameplay/Prefabs` |
| `Scenes/`, `Settings/` | `_Project/Scenes`, `_Project/Settings` |

- Sửa đường dẫn cứng: 8 tool Level, `FeelPlayModeTests`, `GameplayVisualsSetup`, `AudioSetup`, `GameFontSetup`, `LevelEditorWindow`; `GameConfigSetup` tìm config theo kiểu; `EditorBuildSettings` trỏ scene mới.

## P8.2 – Sprite Atlas

| Atlas | Sprite | Trang |
| --- | --- | --- |
| `Shared/Art/SharedAtlas` | 53 | 1 |
| `Features/Gameplay/Art/GameplayAtlas` | 81 | 2 |
| `Game/Art/GameAtlas` | 18 | 1 |
| `Features/DailyChallenge/Art/DailyChallengeAtlas` | 15 | 1 |
| `Features/Profile/Art/ProfileAtlas` | 11 | 2 |
| `Features/Settings/Art/SettingsAtlas` | 7 | 1 |
| `Features/Economy/Art/EconomyAtlas` | 2 | 1 |

Cài đặt: không Rotation, không Tight Packing, Padding 4, không mipmap, max 2048.

## P8.3 – preset nén

| Preset | Bộ lọc | Android |
| --- | --- | --- |
| `TextureBackground` | `Assets/_Project/**/Backgrounds/**` | ASTC 8x8 (8 ảnh) |
| `TextureUI` | `Assets/_Project/**/Art/UI/**` | ASTC 4x4 (121 ảnh) |
| `TextureDefault` | `Assets/_Project/**` | ASTC 6x6 (78 ảnh) |
| Sprite Atlas (7) | — | ASTC 4x4 |

- Kiểm tra: 160 / 160 test; build Android `Builds/P8/PawBlock.apk`; Play với save mẫu: Daily Reward, Menu, level 23, ảnh chụp không vỡ khối. Save thật của máy sao lưu trước và trả lại sau mỗi lần thử.
- **Chưa thử trên máy Android thật.**
