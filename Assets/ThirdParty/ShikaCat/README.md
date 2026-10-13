# 🐱 BỘ SƯU TẬP TÀI NGUYÊN SHIKACAT (SHIKACAT GAME ASSETS)

Toàn bộ tài nguyên của game **ShikaCat** (Tripledot Studios) trong thư mục `E:\Data\` đã được phân tích, khử trùng lặp (deduplicate) và gộp vào các thư mục chuyên biệt tại: `E:\Data\ShikaCat_GameAssets\`.

---

## 📁 CẤU TRÚC THƯ MỤC TỔNG QUAN

```text
E:\Data\ShikaCat_GameAssets\
│
├── 🔊 01_Audio/                        -> Toàn bộ âm thanh (.ogg)
│   ├── BGM/                            -> Nhạc nền game (SC_BGM_01.ogg)
│   ├── Gameplay_SFX/                   -> Âm thanh khi tạo hộp mèo (Box_Created), hoàn thành bàn (AllBoxesCreated), âm sai (Mistake)
│   └── UI_SFX/                         -> Âm thanh nút bấm (Button), thắng (Win), thua (Lose), popup đóng/mở
│
├── 🐱 02_Cats_And_Decoration/          -> Đồ họa Mèo & Nội thất / Đồ chơi
│   ├── Cats_InGame/                    -> 12 chú mèo sắc màu trong trận (BabyBlue, Blue, ElectricBlue, Green, Lime, Orange, Pink, Purple, Red, Teal, WarmPink, Yellow)
│   ├── Cats_EndGame/                   -> 12 ảnh mèo phiên bản chiến thắng / màn hình kết thúc
│   └── Cat_Furniture_Toys/             -> 15 đồ chơi & nội thất mèo (Nhà cây, đệm ngủ, cào móng, xương cá, cuộn len, bóng, bát ăn...)
│
├── 🧱 03_Gameplay_Boxes_Grid/          -> Cơ chế giải đố Shikaku cốt lõi
│   ├── Panels_Colored_Boxes/           -> 13 bảng màu khối chữ nhật tương ứng từng loại mèo
│   ├── Grid_Tiles/                     -> Bàn cờ ô lưới (Tile_Board, 9Sliced, AreaTex, White1px...)
│   ├── Drag_Feedback/                  -> Vệt kéo thả (Overlay_DragPreview, Overlay_DragPreview_Red), khung mạng sống (LifeContainer)
│   └── Level_Data/                     -> shikacat-levels.json (Trọn bộ 205 màn chơi Shikaku chuẩn của ShikaCat)
│
├── 🖼️ 04_UI_Graphics/                 -> Giao diện người dùng
│   ├── Backgrounds_Screens/            -> Màn hình nền (BackgroundScreen, Splash, Loading, Core BG, Logo...)
│   ├── Win_Lose_Screens/               -> Màn hình kết thúc Thắng / Thua (Image_Win_Bg, Image_Lose_Bg...)
│   ├── Buttons/                        -> 14 nút bấm UI (Button_CTA, Primary, Secondary, Hint, Settings, Close, Red...)
│   ├── Icons_Badges/                   -> 24 biểu tượng (Icon_Coin, Chest, Collection, Heart/Life, Trophy, Star, Ad, Settings, Home, Paw...)
│   ├── Popups_Panels/                  -> 22 khung viền popup & navigation bar
│   ├── Progress_Bars_Toggles/          -> Thanh tiến trình (ProgressBar, Chest progression), nút gạt Toggle, Checkmark
│   └── Dev_Debug_UI/                   -> Icon công cụ gỡ lỗi, điều hướng, slider gauge...
│
├── ✨ 05_VFX_Particles/                -> Hiệu ứng hình ảnh (Bóng mờ FX_Shadow_OrbSoft, Pháo giấy Confetti mừng chiến thắng)
│
├── 🔤 06_Fonts/                        -> Phông chữ game (Baloo 2 SDF Atlas, Roboto-Bold, Roboto-Regular, LiberationSans, NotInter, PerfectDOSVGA437)
│
├── 🎬 07_Animations/                   -> 37 Animation Clips (Button Down/Up/Interactive, Popup Open/Close, Ads, Progression...)
│
├── 📦 08_Prefabs/                      -> Toàn bộ Prefab Unity đã phân nhóm (Gameplay, App Navigation, Settings & Shared, UI Elements)
│
├── 🎨 09_Shaders_Materials/            -> Shaders URP & Materials cho SDF Font, Confetti, Graphy...
│
└── 🗃️ 10_Original_Textures/            -> Toàn bộ 303 file Texture gốc nguyên bản
```
