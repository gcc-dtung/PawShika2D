# 17 – Teams (Đội)

> Tab thứ 4 của Main Menu (thay Daily Challenge từ 2026-10-25; Daily sẽ thành 1 liveops sau). Code: [Modules/Teams](../Modules/Teams.md), tab `Runtime/UI/Home/TeamsPanel`.

## Hành vi

| Trang | Nội dung |
| --- | --- |
| **Teams** | Top 30 đội (đội nhiều thành viên lên trên): hạng, logo, tên, "Capacity x/50", nút **View** |
| **Search** | Ô "Team name" + nút **SEARCH** (hoặc Enter): đội có tên chứa chuỗi (không phân biệt hoa thường), tối đa 20; không có thì "No team found" |
| **Create** | Logo (bấm để đổi, 20 mẫu), tên (3–16 ký tự), mô tả (≤ 100), cấp yêu cầu (mũi tên, bước 5, 1–200), nút **Create 3000** coin; coin hiện góc trên. Đã ở đội thì tab này thành **My Team** |
| **Info** | Logo, tên, mô tả, Capacity, Required Level, danh sách thành viên (đội trưởng có vương miện, người chơi có viền sáng); nút **Join** (xanh) / **Full** / **Lv X** (tắt) hoặc **Leave** (đỏ); mũi tên quay lại khi xem đội khác |

- Đổi trang: trang cũ mờ đi, trang mới hiện dần, các dòng hiện lần lượt từ trên xuống (như bản gốc).
- Vào đội: sĩ số +1, tab 3 thành My Team. Rời đội tự tạo = giải tán (đội biến mất khỏi danh sách).
- Lỗi (tên sai, thiếu coin, đội đầy, chưa đủ cấp) hiện `NoticePopup` "TEAMS".

## Sửa nhanh

| Muốn | Sửa |
| --- | --- |
| Giá tạo, sức chứa, khoảng cấp | `Data/Config/TeamsDatabase` |
| Tên đội / thành viên giả, mô tả | `TeamsDatabase.teamNames`, `memberNames`, `teamDescriptions` (+ `seed`) |
| Logo | `TeamsDatabase.logos` (thứ tự = chỉ số lưu trong save) |
| Chữ nút / tab | `TeamsPanel` (`createLabel`, `myTeamLabel`), chữ trong prefab |
