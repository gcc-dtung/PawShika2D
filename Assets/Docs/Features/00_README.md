# Tài liệu theo tính năng

Mỗi file dưới đây giải thích **một tính năng**: nó làm gì, chạy theo luồng nào, dữ liệu / cấu hình nằm đâu, sự kiện nào phát ra, và **muốn sửa / mở rộng thì đụng vào đâu**.

- Module, thư mục, class, scene, event: [../ProjectStructure.md](../ProjectStructure.md); mỗi mảng có tài liệu riêng ở [../Modules/](../Modules/)
- Luật chơi, cách chạy dự án: [../README.md](../README.md)
- Nhật ký thay đổi theo lần push: [../RoadMap/00_README.md](../RoadMap/00_README.md)

## Danh sách

| # | Tính năng | Đọc khi muốn… |
| --- | --- | --- |
| [01](01_Gameplay-LuoiVaKeoTha.md) | Lưới, kéo thả, kiểm tra nước đi, solver, thắng | hiểu 1 nước đi chạy qua những class nào |
| [02](02_ThoiGian-VongDoiVan.md) | Đồng hồ đếm ngược, trạng thái ván (`GameState`), Thắng / Thêm giờ / Thua | đổi công thức thời gian, luồng popup sau ván |
| [03](03_CoChe-Mechanic.md) | Cơ chế: Void, Hướng, Khóa, "?" | hiểu từng cơ chế chạy thế nào |
| [04](04_Level-DuLieuVaCongCu.md) | Dữ liệu level, Level Editor, Validator, Mechanic Injector, độ khó, Hard / Very Hard | làm / xếp / kiểm tra level |
| [05](05_Booster.md) | Booster Hint / Đóng băng / Reveal, mua, tutorial mở khóa | thêm / chỉnh booster |
| [06](06_NangLuong.md) | Năng lượng (lượt chơi) ngoài sảnh | đổi số năng lượng, thời gian hồi |
| [07](07_Economy-Shop.md) | Coin, Shop, IAP, quảng cáo có thưởng | thêm gói Shop, gắn SDK thật |
| [08](08_DiemDanh.md) | Điểm danh 7 ngày | đổi quà, thêm loại quà |
| [09](09_Tutorial.md) | Tutorial lần đầu, popup cơ chế, tutorial booster | sửa / thêm hướng dẫn |
| [10](10_UI-Menu-Popup.md) | Main Menu (tab là prefab), ScreenFlow, HUD, Popup, Safe Area | thêm màn hình / popup / nút |
| [11](11_Save.md) | Lưu dữ liệu người chơi (JSON v2, mỗi feature 1 section) | thêm dữ liệu cần lưu |
| [12](12_AmThanh-PhanHoi.md) | Âm thanh, rung, chữ khen, rung camera | thêm âm / hiệu ứng phản hồi |
| [13](13_Editor-Builder-Debug.md) | Công cụ Editor (level, setup asset, tự điền tham chiếu), Debug Panel, Cheat Panel | tìm tool, test nhanh |
| [14](14_GameFeel.md) | Game feel: hiệu ứng lúc kéo / đặt / gỡ khối, combo, tiến độ, đồng hồ, test | chỉnh / tắt 1 hiệu ứng, biết nó nằm ở đâu |
| [15](15_Sao.md) | Sao theo thời gian: thanh 3 mốc trên HUD, tiêu đề + sao ở màn thắng, coin theo sao, ví sao + Vòng quay may mắn | đổi mốc sao, coin mỗi sao, giá / quà vòng quay |
| [16](16_DailyChallenge.md) | Daily Challenge (lịch tháng, quà mốc 7 / 14 / 21) và Level Generator – tạm rút khỏi tab Menu | đổi quà / độ khó Daily, sinh level mới hàng loạt |
| [17](17_Teams.md) | Đội (tab 4): danh sách, tìm, tạo, vào / rời đội (giả lập offline) | đổi giá tạo, sức chứa, tên đội giả |

**Hướng dẫn mở rộng**

| File | Nội dung |
| --- | --- |
| [HowTo_ThemMechanic.md](HowTo_ThemMechanic.md) | Thêm cơ chế mới từng bước (ví dụ đầy đủ), checklist mọi chỗ phải sửa |
| [HowTo_MoRong.md](HowTo_MoRong.md) | Thêm booster, popup, gói Shop, quà điểm danh, âm thanh, dữ liệu save, màn hình; nguyên tắc giữ code dễ mở rộng |

---

## Kiến trúc trong 1 trang

Chi tiết: [../ProjectStructure.md](../ProjectStructure.md), quy tắc: [../Refactor/UnityCodingGuidelines.md](../Refactor/UnityCodingGuidelines.md) + [../Refactor/ProjectConventions.md](../Refactor/ProjectConventions.md).

```text
 Runtime/Core/Contracts   event (EventBus<T>) · interface (ICurrencyWallet, IEnergyService, IRewardGranter…) · enum dùng chung
       ▲
 Runtime/Core/Foundation  EventBus · Save (ISaveService, JSON theo section) · AudioManager · IClock · IAdsService / IIAPService · InputGate
       ▲
 Shared           PopupView · MenuPanel · widget UI (ButtonPress, SafeAreaZone…) · art / font / âm thanh dùng chung
       ▲
 Features/*       Gameplay (lưới, đồng hồ, booster, tutorial, level)   ┐ không tham chiếu nhau,
                  Economy · Shop · DailyReward · DailyChallenge ·       │ chỉ qua event / interface
                  LuckyWheel · Profile · Settings                       ┘ của Runtime/Core/Contracts
       ▲
 Game             GameBootstrap (tạo service, gọi Init cho mọi component) · ScreenFlow · GameFlowUI · Main Menu · SaveMigrator
```

Trong mỗi feature:

```text
 Config (ScriptableObject, Configs/)  ─►  Model (C# thuần, test được)  ◄─►  Component (MonoBehaviour, Init từ Bootstrap)
                                                                              │ UnityEvent / event C# (trong feature)
                                                                              │ EventBus (giữa feature)
                                                                              ▼
                                                                View (prefab, chỉ vẽ, không quyết định luật)
```

### Quy tắc của dự án (giữ để dễ mở rộng)

1. **Module tách bạch.** Feature không `using` / tham chiếu / lồng prefab của feature khác; cần gì thì qua event / interface trong `Runtime/Core/Contracts`. Chung 1 assembly (`PawBlock.Runtime`) nên compiler không bắt: tự soát khi review ([Dependencies](../Refactor/Dependencies.md)).
2. **Không Singleton cho game logic, không Find / GetComponent lúc chạy.** Dependency qua `Init(...)` do `GameBootstrap` gọi; tham chiếu `[SerializeField]`, Editor tự điền (`OnValidate`, Collector, `UIAutoSetup`).
3. **Tách Model / View.** Luật nằm trong class C# thuần (`…Model`, `…Service`); MonoBehaviour View chỉ đọc model để vẽ.
4. **Giao tiếp bằng event.** Thêm tính năng phụ (âm thanh, phân tích, hiệu ứng) = thêm 1 listener, không sửa luồng chính.
5. **Số liệu để trong ScriptableObject** ở `Configs/` của feature (mở nhanh từ `Data/Config/GameConfig`). Chỉnh cân bằng không cần sửa code.
6. **Animation bằng DOTween**, giữ tween trong field, `SetLink(gameObject)`, `Kill()` trong `OnDisable`.
7. **UI là prefab**, sửa trong Prefab Mode; mỗi popup / tab có script gốc riêng.

### Thứ tự đọc gợi ý cho người mới

1. [../ProjectStructure.md](../ProjectStructure.md) → hiểu module và `GameBootstrap`.
2. [01 Lưới và kéo thả](01_Gameplay-LuoiVaKeoTha.md) → hiểu `BoardModel.Check` và `ShikakuSolver`.
3. [02 Đồng hồ và vòng đời ván](02_ThoiGian-VongDoiVan.md) → hiểu `GameState`.
4. [03 Cơ chế](03_CoChe-Mechanic.md) → [HowTo_ThemMechanic](HowTo_ThemMechanic.md).
5. [04 Level](04_Level-DuLieuVaCongCu.md) → mở **Tools → PawBlock → Level Editor** và thử.
6. Phần còn lại đọc khi cần.

### Tìm nhanh điểm mở rộng trong code

Mọi chỗ phải sửa khi thêm cơ chế đều có chú thích `// [MECHANIC]`. Tìm trong IDE (Ctrl + Shift + F) chữ `[MECHANIC]`, hoặc:

```bash
grep -rn "\[MECHANIC\]" Assets/_Project
```
