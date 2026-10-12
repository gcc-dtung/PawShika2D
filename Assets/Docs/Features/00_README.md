# Tài liệu theo tính năng

Mỗi file dưới đây giải thích **một tính năng**: nó làm gì, chạy theo luồng nào, dữ liệu / cấu hình nằm đâu, sự kiện nào phát ra, và **muốn sửa / mở rộng thì đụng vào đâu**.

- Bảng từng file script (vai trò từng class): [../ProjectStructure.md](../ProjectStructure.md)
- Luật chơi, cách chạy dự án: [../README.md](../README.md)
- Nhật ký thay đổi theo lần push: [../RoadMap/00_README.md](../RoadMap/00_README.md)

## Danh sách

| # | Tính năng | Đọc khi muốn… |
| --- | --- | --- |
| [01](01_Gameplay-LuoiVaKeoTha.md) | Lưới, kéo thả, kiểm tra nước đi, solver, thắng | hiểu 1 nước đi chạy qua những class nào |
| [02](02_Mang-VongDoiVan.md) | Mạng, Khiên, trạng thái ván (`GameState`), Thắng / Hồi mạng / Thua | sửa số mạng, luồng popup sau ván |
| [03](03_CoChe-Mechanic.md) | Cơ chế: Void, Hướng, Khóa, "?" | hiểu từng cơ chế chạy thế nào |
| [04](04_Level-DuLieuVaCongCu.md) | Dữ liệu level, Level Editor, Validator, Mechanic Injector, độ khó, Hard / Very Hard | làm / xếp / kiểm tra level |
| [05](05_Booster.md) | Booster Hint / Khiên / Reveal, mua, tutorial mở khóa | thêm / chỉnh booster |
| [06](06_NangLuong.md) | Năng lượng (lượt chơi) ngoài sảnh | đổi số năng lượng, thời gian hồi |
| [07](07_Economy-Shop.md) | Coin, Shop, IAP, quảng cáo có thưởng | thêm gói Shop, gắn SDK thật |
| [08](08_DiemDanh.md) | Điểm danh 7 ngày | đổi quà, thêm loại quà |
| [09](09_Tutorial.md) | Tutorial lần đầu, popup cơ chế, tutorial booster | sửa / thêm hướng dẫn |
| [10](10_UI-Menu-Popup.md) | Main Menu, ScreenFlow, HUD, Popup, Safe Area | thêm màn hình / popup / nút |
| [11](11_Save.md) | Lưu dữ liệu người chơi (JSON) | thêm dữ liệu cần lưu |
| [12](12_AmThanh-PhanHoi.md) | Âm thanh, rung, chữ khen, rung camera | thêm âm / hiệu ứng phản hồi |
| [13](13_Editor-Builder-Debug.md) | Builder dựng UI bằng code, Debug Panel, công cụ Editor | dựng lại UI, test nhanh |
| [14](14_GameFeel.md) | Game feel: hiệu ứng lúc kéo / đặt / gỡ khối, combo, tiến độ, đồng hồ, test | chỉnh / tắt 1 hiệu ứng, biết nó nằm ở đâu |
| [15](15_Sao.md) | Sao theo thời gian: thanh 3 mốc trên HUD, tiêu đề + sao ở màn thắng, coin theo sao, ví sao + Vòng quay may mắn | đổi mốc sao, coin mỗi sao, giá / quà vòng quay |
| [16](16_DailyChallenge.md) | Daily Challenge (lịch tháng, quà mốc 7 / 14 / 21) và Level Generator | đổi quà / độ khó Daily, sinh level mới hàng loạt |

**Hướng dẫn mở rộng**

| File | Nội dung |
| --- | --- |
| [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md) | Thêm cơ chế mới từng bước (ví dụ đầy đủ), checklist mọi chỗ phải sửa |
| [HowTo_MoRong.md](HowTo_MoRong.md) | Thêm booster, popup, gói Shop, quà điểm danh, âm thanh, dữ liệu save, màn hình; nguyên tắc giữ code dễ mở rộng |

---

## Kiến trúc trong 1 trang

```text
                ┌──────────────── Editor (chỉ chạy trong Unity Editor) ────────────────┐
                │ Level Editor · Validator · Mechanic Injector · Level Difficulty       │
                │ Builders (dựng UI bằng code) · Debug Panel · Save Data menu           │
                └────────────────────────────┬──────────────────────────────────────────┘
                                             │ ghi asset
   Dữ liệu (ScriptableObject)                ▼
   LevelDatabase / LevelDefinition · BoosterDatabase · ShopDatabase · DailyRewardDatabase
   AudioDatabase · DifficultyTheme · GameplayVisuals
                                             │ đọc
   Model (C# thuần, không MonoBehaviour)     ▼
   BoardModel · CellModel · ClueCell · BlockModel · DragSelection · EnergyModel · DailyRewardModel
   ShikakuSolver (static) · CoinWallet / BoosterInventory / ShopService (static) · SaveData
                                             │ gọi / nghe event
   Manager (MonoBehaviour trên object Manager, đa số là Singleton<T>)
   LevelManager · GridManager · LivesManager · BoosterManager · EnergyManager
   DailyRewardManager · TutorialController · BoosterTutorial · MechanicIntro · ScreenFlow
                                             │ UnityEvent / event C#
   View / UI (chỉ vẽ, không quyết định luật)  ▼
   CellView · PlacedBlockView · SelectionBoxView · HUD (LevelHUD, LivesUI, BoosterButton…)
   Popup (PopupView + GameFlowUI) · Menu (MainMenuNavigator, PlayButtonView…)
                                             │
   Lưu: SaveSystem → save.json (SaveRunner ghi sau 0.5 s)
```

### 5 quy tắc của dự án (giữ để dễ mở rộng)

1. **Tách Model / View.** Luật nằm trong class C# thuần tên `…Model` (hoặc static service); MonoBehaviour tên `…View` chỉ đọc model để vẽ. Sửa hiệu ứng thì chỉ sửa View, sửa luật thì chỉ sửa Model.
2. **Giao tiếp bằng event.** Manager phát `UnityEvent` / `event Action`; UI, âm thanh, tutorial **nghe**, không bị gọi trực tiếp. Thêm tính năng phụ (âm thanh, phân tích, hiệu ứng) = thêm 1 listener, không sửa luồng chính.
3. **Số liệu để trong ScriptableObject.** Giá, số lượt, level mở khóa, quà… nằm trong asset `Assets/_Project/Configs/*.asset` / `Levels/*.asset`, mở nhanh từ `Resources/GameConfig.asset`. Chỉnh cân bằng không cần sửa code.
4. **Animation bằng DOTween**, giữ tween trong field, `SetLink(gameObject)`, `Kill()` trong `OnDisable`. Không dùng coroutine.
5. **UI dựng bằng Builder** (`Tools → PawBlock → Builders → …`). Sửa layout trong code builder rồi chạy lại menu, không sửa tay trong scene (sửa tay sẽ mất khi build lại).

### Thứ tự đọc gợi ý cho người mới

1. [01 Lưới và kéo thả](01_Gameplay-LuoiVaKeoTha.md) → hiểu `BoardModel.Check` và `ShikakuSolver`.
2. [02 Mạng và vòng đời ván](02_Mang-VongDoiVan.md) → hiểu `GameState`.
3. [03 Cơ chế](03_CoChe-Mechanic.md) → [HowTo_ThemMechanic](HowTo_ThemMechanic.md).
4. [04 Level](04_Level-DuLieuVaCongCu.md) → mở **Tools → PawBlock → Level Editor** và thử.
5. Phần còn lại đọc khi cần.

### Tìm nhanh điểm mở rộng trong code

Mọi chỗ phải sửa khi thêm cơ chế đều có chú thích `// [MECHANIC]`. Tìm trong IDE (Ctrl + Shift + F) chữ `[MECHANIC]`, hoặc:

```bash
grep -rn "\[MECHANIC\]" Assets/_Project/Script
```
