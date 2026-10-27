# 17 – Teams (Đội)

> Tab thứ 4 của Main Menu (thay Daily Challenge từ 2026-10-25; Daily sẽ thành 1 liveops sau). Code: [Modules/Teams](../Modules/Teams.md), tab `Runtime/UI/Home/TeamsPanel`.

## Hành vi

| Trang | Nội dung |
| --- | --- |
| **Teams** | Top 30 đội (đội nhiều thành viên lên trên): hạng, logo, tên, "Capacity x/50", nút **View** |
| **Search** | Ô "Team name" + nút **SEARCH** (hoặc Enter): đội có tên chứa chuỗi (không phân biệt hoa thường), tối đa 20; không có thì "No team found" |
| **Create** | Logo (bấm để đổi, 20 mẫu), tên (3–16 ký tự), mô tả (≤ 100), cấp yêu cầu (mũi tên, bước 5, 1–200), nút **Create 3000** coin; coin hiện góc trên |
| **Team Info** | 3 tab thay bằng thanh tiêu đề "Team Info" + nút **X**. Logo, tên, mô tả, Capacity; ô số liệu: Team Score, Activity (chấm màu Low / Medium / High), Required Level, Team Rank; nút **Join** (xanh) / **Full** / **Lv X** (tắt) hoặc **Leave** (đỏ); thành viên xếp theo cấp: top 3 huy chương vàng / bạc / đồng, dòng phụ "Leader" + vương miện hoặc chấm xanh + "1d ago" / "Online", ô "Level", người chơi có viền sáng |
| **My Team** | Đã vào đội thì tab Đội mở thẳng trang này (thanh tiêu đề "My Team", không còn 3 tab). Banner logo + tên + nút **Team Info**; bảng tin: "X joined the team.", bong bóng tin nhắn (của mình bên phải), thẻ xin mạng (tim + thanh x/5); nút **Message** mở ô nhập + **Send** (≤ 80 ký tự); nút **Request** xin mạng, sau đó xám + đồng hồ chờ 4 giờ. Đồng đội giả giúp 1 mạng mỗi 20 phút (tối đa 5, không quá số thành viên), mạng cộng vào năng lượng |

- Đổi trang: trang cũ mờ đi, trang mới hiện dần, các dòng hiện lần lượt từ trên xuống (như bản gốc).
- Vào đội: sĩ số +1, bảng tin tạo sẵn vài dòng (thành viên vừa vào, 1 lời chào) rồi "<bạn> joined the team."; X ở Team Info về My Team. Leave (trong Team Info): hỏi lại bằng `ConfirmPopup` "Leave Team?" (Yes / No / X), Yes thì xoá bảng tin, về tab Teams. Rời đội tự tạo = giải tán.
- Thanh tiêu đề Team Info / My Team giống bản gốc: dải nâu bo góc dưới (`Header_Team`) + tấm tên (`Header_Team_a`) thò xuống, X ở góc phải; khung đầu trang 3 tab (`listHeader`) ẩn đi, trang bắt đầu sát dưới thanh. Chữ nút Join viền xanh, Leave viền đỏ.
- Dòng đội / thành viên / thông báo dùng sprite viền mỏng (`PosBar_Thin`, `Time_Stamp_Thin` – vẽ lại viền 6 → 3 px và 4 → 2.5 px, giữ độ bo).
- Bấm 1 thành viên (Team Info): dòng tối nhẹ, menu bật ra dưới avatar (mũi nhọn chỉ vào avatar, khung `Detail_Popup`): **View Profile** mở hồ sơ (mình: hồ sơ thật; thành viên giả: hồ sơ giả cố định theo tên, chỉ số theo cấp), **Remove Member** mờ, không bấm được (đội giả, cần server). Bấm ra ngoài thì đóng.
- Nút mũi tên: nút đứng thẳng, chỉ lật hình con. Canvas bật "Ignore Reversed Graphics" nên hình bị lật trục Y (xoay 180°) không nhận chạm.
- Lỗi (tên sai, thiếu coin, đội đầy, chưa đủ cấp) hiện `NoticePopup` "TEAMS".

## Sửa nhanh

| Muốn | Sửa |
| --- | --- |
| Giá tạo, sức chứa, khoảng cấp | `Data/Config/TeamsDatabase` |
| Xin mạng, độ dài tin, số dòng bảng tin, lời chào | `TeamsDatabase` (`lifeRequestMax`, `lifeRequestCooldownHours`, `lifeHelpMinutes`, `messageMaxLength`, `feedMax`, `greetings`) |
| Tên đội / thành viên giả, mô tả | `TeamsDatabase.teamNames`, `memberNames`, `teamDescriptions` (+ `seed`) |
| Logo | `TeamsDatabase.logos` (thứ tự = chỉ số lưu trong save) |
| Chữ tiêu đề / nút | `TeamsPanel` (`infoTitle`, `myTeamTitle`, `activityNames`, `leaveTitle`, `leaveMessage`), chữ trong prefab |
