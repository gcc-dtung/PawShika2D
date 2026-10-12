# RoadMap – Nhật ký thay đổi

**Mỗi lần push là một file.** Mọi thay đổi chưa push được ghi dồn vào file cuối cùng (trạng thái "Chưa push"). Khi push xong, file đó được chốt (ghi commit + giờ push) và thay đổi tiếp theo sang file mới với số kế tiếp.

## Quy tắc đặt tên

```text
NN_TenNganKhongDau.md
```

- `NN`: số thứ tự 2 chữ số (`01`, `02`, … `99`), tăng 1 sau mỗi lần push. Không đánh lại số cũ.
- Tên ngắn, không dấu, nối bằng `-`, mô tả nội dung chính của lần push.
- Ngày giờ ghi **trong** file, không đưa vào tên.

## Mẫu nội dung

```markdown
# NN – Tiêu đề

| | |
| --- | --- |
| Trạng thái | Chưa push / Đã push |
| Commit | <hash> "<message>" (khi đã push) |
| Thời gian push | YYYY-MM-DD HH:MM (khi đã push) |
| Cập nhật lần cuối | YYYY-MM-DD HH:MM |

## Tóm tắt
Bảng các đợt thay đổi trong lần push này.

## <Giờ> – <Tên đợt>
### Mục tiêu / Vấn đề
### Thay đổi (Thêm / Sửa / Xóa)
### Tác dụng
### Kiểm tra
```

## Danh sách

| # | Trạng thái | Push | Nội dung |
| --- | --- | --- | --- |
| [01](01_Core-GameplayVaCoCheMang.md) | Đã push | 2026-10-02 13:46 · `681d08c` | Phân tích, cơ chế mạng, Giai đoạn 1–2 (solver, 205 level, khối mèo, HUD) |
| [02](02_TaiLieu-ToChucScript.md) | Đã push | 2026-10-02 16:31 · `08ce8ec` | Tài liệu Docs, tổ chức thư mục Script, quyết định giữ CatFaceAnimator |
| [03](03_ToiUu-PrefabPool.md) | Đã push | 2026-10-02 19:39 · `7f9a805` | SelectionBox, ClueCell C# thuần, prefab + ObjectPool, sửa lỗi pool |
| [04](04_TaiMeo-LevelTest.md) | Đã push | 2026-10-02 20:52 · `67574be` | Tai mèo nhỏ lại; level test mọi cơ chế (level 206); kiểm tra level; hướng Square + ô nền điều hướng |
| [05](05_TachLogicView-FeelKeo.md) | Đã push | 2026-10-03 00:31 · `aaf54da` | Tách logic (`…Model`) / view (`…View`); khung trượt mượt, không phủ khối / Void, mép kẹt khi bị chặn; mèo xem trước + viền màu khi đủ value; màu khối = màu khung; đặt sai: mèo xám (chọn kiểu trong Inspector) |
| [06](06_Popup-GameState.md) | Đã push | 2026-10-03 01:13 · `93c8844` | Giai đoạn 3: `GameState`, popup Thắng / Hồi mạng / Thua / Xác nhận chơi lại, coin + ads giả lập; animation chuyển sang DOTween (bỏ viền đỏ còn 1 mạng) |
| [07](07_Booster-NangLuong.md) | Đã push | 2026-10-03 16:27 · `c2b6cfb` | Giai đoạn 4: booster Hint / Khiên / Reveal, popup mua, khóa theo level; khung giãn xong mới thành mèo; Giai đoạn 5: năng lượng (cá), HUD, popup nạp, chặn khi hết; Main Menu 3 tab, màn Home (PLAY, năng lượng, coin), Menu ↔ màn chơi; font Fredoka Bold, Home giống ảnh gốc; Play Zone; UI Zone (safe area), nền phủ kín |
| [08](08_HieuUng-XuatHien.md) | Đã push | 2026-10-03 17:15 · `4002899` | Lưới xuất hiện dần: ô nảy lần lượt ngẫu nhiên, số hiện mờ dần, khóa thao tác tới khi xong |
| [09](09_LevelEditor.md) | Đã push | 2026-10-04 07:11 · `6f48193` | Level Editor; Audio Manager; save JSON; popup Setting / Refill / QUIT LEVEL / NOTICE; nhấn nút lún, vuốt chuyển tab; Starter / No Ads / Daily (điểm danh 7 ngày); màn WELL DONE + confetti (UIParticle); Debug Panel; level Hard / Very Hard; tab Shop (IAP chưa SDK) |
| [10](10_MechanicInjector.md) | Đã push | 2026-10-05 11:12 · `bb4c0c2` | Mechanic Injector: gắn Void / hướng / khóa / "?" vào level ShikaCat theo lịch (131/205 level), kiểm tra bằng LevelValidator, chạy lại ra y hệt; xóa 3 tool lỗi thời; Level Difficulty: độ khó lý thuyết (công thức D / thời gian / rủi ro), tick Hard / Very Hard, CSV so thực tế; gom menu Builders; tutorial lần đầu 4 bước như bản gốc |
| [11](11_BoosterTutorial.md) | Đã push | 2026-10-05 17:30 · `25db7d6` | Booster khóa tới khi qua tutorial mở khóa (tối màn hình, chỉ nút đó sáng + bàn tay + lời nhắc), lần dùng đầu miễn phí; popup giới thiệu cơ chế Hướng / Khóa / "?"; Level 1 4x4 như bản gốc; sửa confetti không chạy từ lần thắng thứ 2 |
| [12](12_SuaBanTayBooster.md) | Đã push | 2026-10-05 19:04 · `ed5b3b2` | Bàn tay tutorial booster vẽ trên nút; xếp lại level theo độ khó (bỏ level 4 và level 1 khối), mọi cơ chế trước level 17; Khiên giới thiệu thứ 2 (level 7) và bắt kéo sai thử; hover biểu đồ Level Difficulty |
| [13](13_TaiLieuFeature-MoRong.md) | Đã push | 2026-10-08 11:11 · `9e99a2c` | Docs theo từng tính năng (`Docs/Features/`), hướng dẫn thêm cơ chế / mở rộng, đánh dấu `// [MECHANIC]` trong code; popup hết booster theo ref + nút xám khi thiếu coin; gom số liệu vào ScriptableObject (`Configs/`, `GameConfig`) |
| [14](14_ToiUuUpdate-InputSystem.md) | Đã push | 2026-10-09 00:38 · `c327340` | Giảm Update / LateUpdate (mặt mèo chạy chung, chữ ẩn theo event, Save / tutorial bằng coroutine, nền chỉ ghi khi camera đổi); chuyển Input System mới; rà SOLID: EventBus, InputGate, hàng đợi intro (`ILevelIntro`), Booster Strategy + Factory |
| [15](15_HoanThienTaskConThieu.md) | Đã push | 2026-10-09 13:41 · `9ed6c29` | Hoàn thiện task còn thiếu: khung LEVEL art gốc, coin thắng theo độ khó, màn "Coming soon", ô hướng `khung_direc`, hiệu ứng băng Khiên, đồng hồ giờ server chống chỉnh giờ, ghi số liệu chơi thật + xuất CSV; popup Hồ sơ người chơi (đổi tên / avatar, thống kê, dùng lại cho BXH); Cheat Panel trong bản build (bấm 10 lần góc trên trái), mở khóa 60 FPS trên mobile; rung điện thoại kiểu haptic theo sự kiện; sửa chữ WELL DONE không đi vào từng chữ |
| [16](16_NutThangXam.md) | Đã push | 2026-10-09 15:31 · `ff104c0` | Nút Continue / x2 Coin ở màn thắng chuyển xám sau khi bấm (như nút Mua booster khi thiếu coin); rung mạnh hơn + chẩn đoán rung trong Cheat Panel |
| [17](17_OldCore-DongHo.md) | Đã push | 2026-10-10 20:29 · `db32b48` | Nhánh `Old-Core`: bỏ 3 mạng, quay lại đồng hồ đếm ngược như bản gốc (8 s + 0.25 s/ô + 5.5–6.5 s/khối, tối thiểu 45 s); đặt khối không hỏi lời giải, chạm khối để gỡ; Khiên → Đóng băng (dừng đồng hồ 20 s); popup Hết giờ (+30s ads / +60s 900 coin); cheat đồng hồ |
| [18](18_GameFeel-KeoDat.md) | Chưa push | | Game feel kéo / đặt: bong bóng số phóng to / thu nhỏ, nảy khi đổi số, đổi màu theo trạng thái (đủ xanh / sai đỏ); tick cao dần theo số ô + "ding" khi đủ; khối đặt xuống squash & stretch; ô quanh khối nảy lan như sóng; công thức tách `FeelCurves` + test; đợt 2: ô số sáng / lắc, bong bóng bám mượt, ô "dính", khung co xám khi thả sai, hạt bụi, mèo phản ứng, combo, thanh tiến độ, slow-motion khối cuối, viền đỏ + nhạc nhanh khi sắp hết giờ, "+30s" bay, tấm băng + mèo run, gỡ khối nhảy rồi rơi; 46 test; tài liệu `Features/14_GameFeel.md`; `Refactor/UnityCodingGuidelines.md` (quy tắc chung) + `Refactor/RefactorPlan.md` (kế hoạch refactor 10 giai đoạn) |

Giờ push lấy từ `git reflog origin/<nhánh>` (01–05: `Day-1`, 06–08: `Day-2`, 09: `Day-3`, từ 10: `Day-4`, 16: `Day-6`, 17: `Old-Core`); giờ từng đợt lấy từ thời gian sửa file và ảnh chụp thử.
