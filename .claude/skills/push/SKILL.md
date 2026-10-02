---
name: push
description: Commit toàn bộ thay đổi hiện tại và push lên nhánh được chỉ định (vd "/push Day-1"), rồi chốt file RoadMap của lần push. Dùng khi người dùng nói "push", "push lên nhánh X", "/push X".
argument-hint: <tên nhánh>
---

# Push lên nhánh

Đối số: `$ARGUMENTS` = tên nhánh đích. Không có thì dùng nhánh hiện tại (`git branch --show-current`).

## 1. Chuẩn bị

1. `git status --short` và `git diff --stat`. Không có gì thay đổi và không có commit chưa push → báo người dùng, dừng.
2. Nhánh hiện tại khác nhánh đích:
   - Nhánh đích đã có (local hoặc `origin`) → hỏi người dùng có muốn `git switch <nhánh>` (mang theo thay đổi) không. Không tự chuyển.
   - Chưa có → hỏi có tạo nhánh mới từ nhánh hiện tại không.
   - Không bao giờ push thẳng lên `main` nếu người dùng không nói rõ.
3. Đọc lướt diff để chắc không có file rác: ảnh chụp thử (`Assets/Screenshots/`), `Temp/`, `Library/`, file bí mật (`.env`, key). Có thì báo, không add.

## 2. Commit

- `git add -A` (sau khi đã loại file rác ở bước 1).
- **Dòng tiêu đề**: tóm tắt ngắn (≤ 72 ký tự), tiếng Việt, theo kiểu các commit trước (`git log --oneline -5`), vd `Tai mèo, level test, kiểm tra level, ô nền điều hướng`.
- **Phần mô tả** (sau 1 dòng trống): liệt kê các thay đổi theo nhóm, mỗi dòng 1 ý, gạch đầu dòng `-`. Lấy nội dung từ file RoadMap "Chưa push" (`Assets/Docs/RoadMap/`) nếu có, đối chiếu với diff thật. Ghi cả lỗi đã sửa và cách đã kiểm tra nếu quan trọng.
- Cuối message: dòng attribution theo hướng dẫn hiện hành của hệ thống (nếu có).
- Commit bằng file message (`git commit -F <file>`) để giữ xuống dòng và tiếng Việt; file đặt trong scratchpad.
- Hook lỗi → sửa nguyên nhân, không dùng `--no-verify`.

## 3. Push

- `git push origin <nhánh>` (nhánh mới: `git push -u origin <nhánh>`).
- Bị từ chối vì remote có commit mới → báo người dùng, không `--force`.
- Push xong: `git log -1 --format="%h %s"` và `git reflog origin/<nhánh> -1 --date=format:"%Y-%m-%d %H:%M"` để lấy hash và giờ push.

## 4. Chốt RoadMap

Quy ước ở `Assets/Docs/RoadMap/00_README.md`: mỗi lần push là 1 file `NN_TenNgan.md`.

1. File đang "Chưa push" → sửa phần đầu:
   - `Trạng thái` → `Đã push`
   - `Commit` → `` `<hash>` "<tiêu đề>" (nhánh `<nhánh>`) ``
   - `Thời gian push` → giờ lấy từ reflog
   - `Cập nhật lần cuối` → giờ hiện tại
2. Dòng tương ứng trong `00_README.md`: `Chưa push` → `Đã push`, cột Push → `<ngày giờ> · `<hash>``.
3. Hai file này để **chưa commit**; chúng đi theo lần push sau (giống các lần trước). Thay đổi tiếp theo ghi vào file số mới.

## 5. Báo cáo

Ngắn gọn: nhánh, hash, tiêu đề commit, số file, link so sánh/nhánh trên GitHub nếu lấy được từ `git remote get-url origin`, và việc đã chốt RoadMap.
