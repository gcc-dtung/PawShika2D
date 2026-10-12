# Baseline – PawBlock trước refactor

> Mốc so sánh đo ở **P0** ([RefactorPlan.md](RefactorPlan.md)), trước khi đổi code. Sau P8 đo lại đúng cách này và ghi cột "Sau".

| | |
| --- | --- |
| Đo lúc | 2026-10-12 11:45–11:51 |
| Nhánh / commit | `Refactor` từ `c3b16a4` (Day-7) |
| Unity | 2022.3.62f3, URP 14.0.12 |
| Máy đo | Editor Windows, Game view dọc 9:16 (Play mode) |

---

## 1. Test

Chạy EditMode, lọc assembly **`Assembly-CSharp-Editor`** (test của dự án, gồm cả 7 `[UnityTest]` chạy Play mode thật).

| | Trước | Sau |
| --- | --- | --- |
| Tổng | 104 | |
| Đạt | **104** | |
| Hỏng / bỏ qua | 0 / 0 | |
| Thời gian | 9.5 s | |

> Plan ghi "46 test" (số cũ ở RoadMap 18 lúc 21:50); hiện có 104 ca (53 `[Test]` + 7 `[UnityTest]`, phần còn lại là `[TestCase]`).
> Test Runner không lọc sẽ chạy 994 test: ~890 của package `com.besty.unity-skills`, trong đó 3 test của package fail sẵn (`NewCapabilitiesTests`, `PerceptionSkillsTests`, `SelectionDrivenSkillTests`), không liên quan game.

## 2. Batches / SetPass (Render counters, Play mode)

Đọc counter `Render` của Profiler (cùng số với Frame Debugger / Stats) khi màn đã đứng yên.

| Màn | Batches | SetPass | Triangles | Sau (Batches / SetPass) |
| --- | --- | --- | --- | --- |
| Menu (Home, level 23) | **63** | **38** | 943 | |
| Màn chơi level 39, lưới 10 x 10 (Hard, có khóa + "?"), lúc mới vào | **58** | **46** | 1 153 | |
| Popup Win (Perfect, 3 sao, đang có confetti) | **133–136** | **94–97** | ~2 475 | |

Cách đo lại:
1. Play mode, đợi Menu hiện xong → đọc counter.
2. `DebugCheats.PlayLevel(39)` → đợi intro lưới xong → đọc counter.
3. `DebugCheats.WinNow()` → đợi popup hiện xong → đọc 2 lần (confetti làm số dao động).

## 3. Build Report (Android)

Build release, IL2CPP, ARMv7 + ARM64, `Builds/P0-Baseline/PawShika.apk` (thư mục `Builds/` không vào git). Build 80 s, 0 lỗi, 0 cảnh báo.

| | Trước | Sau |
| --- | --- | --- |
| **Dung lượng APK** | **58.9 MB** (61 711 169 byte) | |
| Asset người dùng (không nén) | 145.5 MB | |
| Textures | 140.0 MB (96.2 %) | |
| Sounds | 1.6 MB | |
| Shaders | 1.4 MB | |
| Other Assets | 2.1 MB | |
| Scripts | 266 KB | |
| "Complete build size" (Unity báo, gồm lib IL2CPP 2 kiến trúc) | 748.1 MB | |

### Asset nặng nhất (≥ 1.5 MB, không nén)

| # | Asset | Dung lượng |
| --- | --- | --- |
| 1 | `02_Cat_Characters_Spine/Spine_Cat_Face/ingame_cat_face_03_4.png` | 15.8 MB |
| 2 | `02_Cat_Characters_Spine/Spine_Cat_Face/ingame_cat_face_03_2.png` | 15.8 MB |
| 3 | `02_Cat_Characters_Spine/Spine_Cat_Face/ingame_cat_face_03_3.png` | 15.8 MB |
| 4 | `02_Cat_Characters_Spine/Spine_Cat_Face/ingame_cat_face_03.png` | 15.7 MB |
| 5 | `04_UI_Graphics/Backgrounds/bg.png` | 5.9 MB |
| 6 | Built-in: Splash Screen Unity Logo | 2.7 MB |
| 7 | `04_UI_Graphics/Frames_Borders/khung_expert_4.png` | 2.6 MB |
| 8 | `04_UI_Graphics/Frames_Borders/khung_expert_3.png` | 2.2 MB |
| 9 | `04_UI_Graphics/Frames_Borders/frame_starter.png` | 2.0 MB |
| 10 | `04_UI_Graphics/Frames_Borders/frame_pack_ads1.png` | 2.0 MB |
| 11 | `04_UI_Graphics/Frames_Borders/frame_pack.png` | 2.0 MB |
| 12 | `05_VFX_Particles/Ice_Effects/ice_effect.png` | 2.0 MB |
| 13 | `04_UI_Graphics/Backgrounds/home_bg.png` | 2.0 MB |
| 14–24 | `ShikaCat_GameAssets/.../Cats_InGame/Image_Cat_*.png` (11 ảnh: ElectricBlue, Purple, Orange, Blue, Green, Lime, BabyBlue, Yellow, Red, WarmPink, Teal) | 1.5 MB mỗi ảnh |

Đường dẫn rút gọn, gốc là `Assets/_Project/GameAssets/` (trừ dòng 14–24 là `Assets/_Project/ShikaCat_GameAssets/02_Cats_And_Decoration/`).

**Ghi chú cho P8:** 4 atlas mặt mèo Spine chiếm ~63 MB (43 % asset) → kiểm tra kích thước / nén ASTC; 11 ảnh mèo in-game cùng cỡ → hợp vào Sprite Atlas.

## 4. Smoke test (mục 6 của plan)

Lần P0 chạy tự động qua Unity MCP các mục bên dưới; mục còn lại **chưa chạy**, người dùng chạy tay trong Editor rồi đánh dấu.

| Mục | Kết quả |
| --- | --- |
| Mở game (save hiện có) vào Menu, Console không lỗi | Đạt |
| Tắt / mở lại: coin, level giữ nguyên | Đạt một phần: mở lại Play mode, coin 2135 và nút PLAY "Level 23" đúng save; chưa kiểm booster, năng lượng, hồ sơ, settings |
| Bấm Play → level hiện, intro chạy | Đạt (level 39, qua `DebugCheats.PlayLevel`) |
| Thắng: popup Win, sao theo thời gian | Đạt (Perfect 3 sao, Continue 35 / x2 Coin 70) |
| Console không có Error / Exception | Đạt |
| Test xanh | Đạt (104 / 104) |
| Kéo thả, chạm gỡ, hết giờ / Lose, booster, cơ chế, Quit, năng lượng, Shop, Daily, Lucky Wheel, Profile, Settings, Cheat Panel | Chưa chạy |

Đo xong đã trả `save.json` về đúng bản trước khi đo (= `SaveSamples/save_v1_played.json`).

## 5. Save mẫu (cho migrate ở P3)

`SaveSamples/`, định dạng v1 (`SaveData.CurrentVersion = 1`), đúng thứ tự field `JsonUtility` ghi ra:

| File | Nội dung |
| --- | --- |
| `save_v1_new.json` | Người mới: level 0, 0 coin, chưa có booster, `energy = -1` (đầy), daily / challenge `-1`, chưa xong tutorial; chỉ có hồ sơ vừa tạo |
| `save_v1_played.json` | Save thật trên máy dev: level 22, 2135 coin, 7 sao, 4 booster, daily đã nhận 4 ngày, challenge 1 ngày, hồ sơ 39 level, 16 `levelStats`, đã xem 3 cơ chế, nhạc tắt |
| `save_v1_infinite_energy.json` | Như `played`, nhưng năng lượng 0 và **vô hạn tới 2100-01-01** (`infiniteUntil = 4102444800`), luôn "đang chạy" khi test |

Migrate v1 → v2 ở P3 phải giữ đúng mọi giá trị trên.
