# 33 – Sửa warning / lỗi trong Console

| | |
| --- | --- |
| Trạng thái | Chưa push |
| Commit | |
| Thời gian push | |
| Cập nhật lần cuối | 2026-10-27 09:46 |

## 2026-10-27 09:44 – Kiểm tra warning Console và sửa lỗi

**Đọc Console** (lúc bắt đầu): `SpriteAtlasManager.atlasRequested wasn't listened to while GameAtlas requested`, 4 dòng `The referenced script (Unknown) on this Behaviour is missing!`. Xoá Console rồi chạy lại từ đầu (vào Play, đi qua 5 tab, bấm Play vào ván, thoát Play, biên dịch lại script) để tách lỗi thật:

- **`[ShopPanel] Chưa gán prefab / parent cho section Limited`** (lúc vào Play): ô `Event` / `Limited` của `ShopPanel` đọc ra prefab rỗng dù file vẫn trỏ đúng `ShopSpecialCard` (cùng fileID) → thẻ Limited Pack không được tạo. Nguyên nhân: Editor giữ tham chiếu cũ sau nhiều lần lưu đè `ShopSpecialCard.prefab` bằng `SaveAsPrefabAsset`. Sửa: import lại `ShopSpecialCard`, `ShopPanel`, `MainMenu` (dữ liệu không đổi). Kiểm tra: Shop tạo đủ 14 thẻ, có Limited Pack.
- **`UIParticle`: `Assertion failed on expression: 'ps->array_size()'` + `Mesh '[UIParticleRenderer] Combine Instance Mesh': abnormal mesh bounds`** (mỗi lần biên dịch lại script khi đang mở prefab thẻ Shop): hệ hạt sao lấp lánh bật `prewarm`, khi nạp lại Editor chạy trước hệ hạt lúc chưa Play nên UIParticle dựng mesh lỗi. Sửa: tắt `prewarm` ở `Sparkles` của 5 thẻ (Special, Bundle, Coin, RemoveAds, Starter); sao xuất hiện sau ~0.3 s khi mở Shop. Kiểm tra: 2 lần biên dịch lại liên tiếp không còn lỗi.
- **Thiếu script ×4**: không còn xuất hiện khi chạy lại. Scene `Main` và mọi prefab trong `_Project` không có component thiếu script; chỉ prefab gốc trong `ThirdParty/ShikaCat` (bản rip tham khảo, 1 prefab `ThirdParty/PawBlock/08_Prefabs/Cell`) còn nhiều component thiếu script → warning hiện khi Editor nạp các prefab đó (xem trước / chọn trong Project). Không dùng trong game (đã gỡ ở mục sau).
- **`GameAtlas` atlasRequested**: không còn xuất hiện khi chạy lại; `GameAtlas` bật `Include in Build` (bindAsDefault 1) như các atlas khác → là thông báo tạm lúc Editor đang đóng gói lại atlas sau khi import ảnh, không phải lỗi cấu hình.

**Kiểm tra cuối**: Play (Home, 5 tab, Shop, vào ván, thoát) và biên dịch lại: Console không còn warning / lỗi.

## 2026-10-27 09:46 – Gỡ component thiếu script trong prefab ThirdParty

**Yêu cầu**: gỡ component thiếu script trong prefab `ThirdParty` (ShikaCat, PawBlock/Cell) để hết warning.

**Sửa**: 98 prefab trong `Assets/ThirdParty` (95 ở `ShikaCat`, 3 ở `PawBlock`) – mở từng prefab, `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` trên mọi object con, lưu lại: gỡ 2176 component (script gốc của game rip không có trong dự án). Không prefab nào có component thiếu nằm trong prefab lồng nên không phải sửa prefab nguồn.

**Kiểm tra**: quét lại toàn bộ prefab trong `Assets`: 0 component thiếu script; tạo thử rồi xoá cả 98 prefab trong scene: Console không còn warning; scene `Main` không bị đổi.
