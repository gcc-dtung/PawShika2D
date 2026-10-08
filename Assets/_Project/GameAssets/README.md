# 🐱 BỘ SƯU TẬP TÀI NGUYÊN GAME PAWBLOCK (ORGANIZED ASSETS)

Toàn bộ tài nguyên (assets) của game PawBlock đã được trích xuất, cắt tỉa (slice sprites từ Sprite Atlas), chuẩn hóa và phân loại vào các thư mục chuyên biệt dưới đường dẫn: `E:\PawBlock\GameAssets\`.

---

## 📁 CẤU TRÚC THƯ MỤC TỔNG QUAN

```text
E:\PawBlock\GameAssets\
│
├── 🔊 01_Audio/                        -> Toàn bộ âm thanh game (.ogg)
│   ├── BGM/                            -> Nhạc nền game (BGM.ogg)
│   ├── Cat_Voices/                     -> 13 giọng meow & biểu cảm mèo (Angry, Cute, Happy, Sad...)
│   ├── Gameplay_SFX/                   -> 11 hiệu ứng âm thanh chơi game (block_true, win, fail, booster...)
│   └── UI_SFX/                         -> 3 âm thanh giao diện (click, earn_coin, earn_reward)
│
├── 🐱 02_Cat_Characters_Spine/         -> Nhân vật & Hoạt cảnh Spine 2D
│   ├── Spine_Cat_Face/                 -> 5 texture mặt mèo + file atlas/skeleton cho Spine
│   ├── Spine_Freeze/                   -> Hiệu ứng đóng băng Spine 2D (skel.bytes + atlas + png)
│   ├── Spine_Noti_Emoji/               -> Thông báo & Emoji Spine 2D (EmojiOne + Noti)
│   └── Praise_Badges/                  -> Huy hiệu khen ngợi nổi (Amazing, Beauty, Great, Nice, Perfect, Wow...)
│
├── 🧱 03_Gameplay_Blocks_Grid/         -> Thành phần giải đố cốt lõi
│   ├── Blocks/                         -> 11 màu khối hình chữ nhật (Blue, Cyan, Red, Yellow, Green...)
│   ├── Cat_Ears/                       -> 10 màu tai mèo tương ứng mọc lên khi ghép khối thành công
│   ├── Grid_Cells/                     -> Ô lưới cờ (CellBG), điểm tutorial, thanh tiến trình
│   └── Boosters_Obstacles/             -> Icon & khung chức năng bổ trợ (Freeze, Hint, Reveal, Refill...)
│
├── 🖼️ 04_UI_Graphics/                 -> Đồ họa giao diện người dùng (UI)
│   ├── Backgrounds/                    -> Hình nền (Home BG, Loading BG, Shop BG, Pattern...)
│   ├── Popups_Dialogs/                 -> Popup Starter, Remove Ads, Popup Tutorial, Dialog...
│   ├── Buttons/                        -> 50 nút bấm UI (Xanh lá, Vàng, Đỏ, Setting, Video, Hard, Lock...)
│   ├── Frames_Borders/                 -> 31 khung viền (Khung avatar, khung level, khung win, khung gói...)
│   ├── Icons/                          -> 52 biểu tượng (Vàng, Sao, Mạng/Tim, Âm thanh, No Ads, Cúp...)
│   ├── Shop_Packs/                     -> Các gói vật phẩm cửa hàng (Bundle 1-6, Rương, Hộp quà...)
│   └── Dev_Debug_UI/                   -> Icon console gỡ lỗi / Ingame Debug UI
│
├── ✨ 05_VFX_Particles/                -> Hiệu ứng hình ảnh & hạt (Visual Effects)
│   ├── Sparks_Twinkle/                 -> Vệt sáng, hạt lấp lánh (Spark White/Yellow, Twinkle...)
│   ├── Fireworks/                      -> 12 frame hiệu ứng pháo hoa ăn mừng chiến thắng
│   ├── Ice_Effects/                    -> Băng tuyết (Ice effect, snowflake)
│   └── Light_Glow/                     -> Ánh sáng tia, hào quang thắng cuộc (Light, Tia sáng...)
│
├── 🔤 06_Fonts/                        -> Phông chữ game (Fredoka-Bold, LiberationSans, SDF Assets)
│
├── 🎬 07_Animations/                   -> Hoạt họa Unity
│   ├── AnimationClips/                 -> 7 file .anim (ButtonHome, Idle, Loading, RotateFX...)
│   └── AnimatorControllers/            -> 6 file .controller điều khiển trạng thái hoạt ảnh
│
├── 📦 08_Prefabs/                      -> Prefab Unity (Block.prefab, Cell.prefab, Adjust.prefab)
│
├── 🎨 09_Shaders_Materials/            -> Shaders & Materials chuyên dụng
│   ├── Shaders/                        -> Shaders UI Dissolve, SoftMask, Spine, TextMeshPro, HSV...
│   └── Materials/                      -> 38 vật liệu hiển thị đồ họa
│
├── 📄 10_Data_Configs/                 -> Dữ liệu cấu hình & ScriptableObject Databases (bản gốc lưu trữ; game dùng bản trong Assets/_Project/Configs)
│   └── DailyChallengeData, Databases (Audio, Color, Booster, Shop Packs, Remote Config...)
│
└── 🗃️ 11_Original_Atlases_Textures/    -> 90 Texture gốc & Sprite Atlas nguyên bản
```
