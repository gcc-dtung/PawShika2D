# Công thức độ khó lý thuyết của level

Tool: **Tools → PawBlock → Level Difficulty** (`Features/Gameplay/Editor/Level/LevelDifficultyWindow`, `LevelDifficultyAnalyzer`).
Trọng số: `Assets/_Project/Features/Gameplay/Configs/Levels/LevelDifficultySettings.asset`, sửa được ngay trong cửa sổ tool; đổi trọng số thì điểm tính lại tức thì.

Độ khó lý thuyết đo **lối suy luận** mà level bắt người chơi đi qua, không đo cảm giác hay độ "đẹp". Mục đích là so với số liệu thực tế (tỉ lệ thắng, thời gian, số lần sai) để chỉnh lại công thức.

## 1. Mô phỏng người chơi

Tool giải level từng khối một như người chơi. Ở mỗi bước, tool dùng **kỹ thuật đơn giản nhất** còn tìm được nước chắc chắn đúng. Người chơi được giả định là chưa biết:

- số của ô "?" và ô đang khóa;
- hướng của ô đang khóa.

Ô đang khóa chưa đặt được. Mỗi khối đặt đúng làm mọi ổ khóa giảm 1 bước, giống trong game.

| Kỹ thuật | Tên | Người chơi phải làm gì |
| --- | --- | --- |
| **T1** | Đơn trực tiếp | Có một số chỉ còn đúng 1 cách đặt (bị mép lưới, Void, khối đã đặt hoặc số khác chặn) |
| **T2** | Loại trừ 1 bước | Thử từng cách đặt của một số, bỏ những cách làm một số khác hết chỗ hoặc làm một ô trống không còn số nào phủ tới được; chỉ còn 1 cách |
| **T3** | Thử sâu / phản chứng | Chỉ 1 cách đặt còn giải tiếp được tới hết lưới, nhưng phải nhẩm nhiều bước mới thấy (tool kiểm bằng `ShikakuSolver`) |
| **T4** | Không có nước bắt buộc | Mọi số còn lại đều có ≥ 2 cách đặt vẫn giải được. Chỉ gặp ở level gốc **nhiều lời giải**. Game chấm bằng solver nên chọn cách nào còn giải được cũng đúng, nhưng người chơi vẫn phải tránh cách sai |

Mỗi level cho ra một chuỗi bước, ví dụ `1121131` nghĩa là bước 3 cần T2 và bước 6 cần T3. Chuỗi này nằm ở cột "Bước giải" trong tool và cột `trace` trong CSV.

## 2. Chỉ số

| Ký hiệu | Cột CSV | Ý nghĩa |
| --- | --- | --- |
| N | `clues` | Số khối (số ô số) |
| A | `cells` | Số ô chơi được (trừ Void) |
| M | `ambiguity` | Độ mơ hồ ban đầu: trung bình log₂(số cách đặt) của mỗi số lúc lưới còn trống. Ô "?" / khóa có M lớn vì người chơi chưa biết số. Mũi tên hướng làm M giảm |
| t₁…t₄ | `t1`…`t4` | Số bước cần T1…T4 (t₁ + t₂ + t₃ + t₄ = N) |
| W | `narrow` | Tỉ lệ bước chỉ có **đúng 1** nước chắc chắn ở kỹ thuật đó (đường hẹp: bỏ sót nước đó là kẹt) |
| H, L | `hidden`, `locks` | Số ô "?", số ô khóa |
| Wa, Co | `walls`, `corners` | Số đoạn tường, số ô số ở góc / cạnh |
| | `voids`, `constraints`, `lock_steps`, `max_technique` | Số ô Void, số mũi tên hướng, tổng bước khóa, kỹ thuật khó nhất đã dùng |

## 3. Công thức

**Điểm độ khó D**

```text
D = wSize · log₂(N)
  + wAmbiguity · M
  + (wT2 · t₂ + wT3 · t₃ + wT4 · t₄) / N
  + wNarrow · W
  + wHidden · H + wLock · L
  + wWall · Wa + wCorner · Co
```

| Trọng số | Mặc định | Lý do |
| --- | --- | --- |
| wSize | 5 | Nhiều khối thì phải quét nhiều hơn; dùng log vì khối thứ 20 không khó gấp đôi khối thứ 10 |
| wAmbiguity | 4 | Lưới mở, nhiều cách đặt thì khó tìm điểm bắt đầu |
| wT2 | 25 | Phải so 2 số với nhau |
| wT3 | 60 | Phải nhẩm sâu, dễ sai nhất trong các bước có lời giải chắc chắn |
| wT4 | 40 | Không có nước bắt buộc; không sai oan nhưng dễ lạc hướng |
| wNarrow | 10 | Đường hẹp dễ kẹt |
| wHidden | 3 | Phải nhớ khối nào chưa biết số |
| wLock | 2 | Phải giải chỗ khác trước |
| wWall | 1.5 | Mỗi đoạn tường là thêm một luật khi kéo qua ranh giới |
| wCorner | 2 | Mỗi ô số ở góc / cạnh: phải đọc dấu góc, vạch cạnh |

Các hạng t / N là **tỉ lệ**, nên D không tự tăng chỉ vì level to; độ to đã được tính riêng qua log₂(N).

**Thời gian ước tính (giây)**

```text
T = timeBase + timePerCell · A + timeT1 · t₁ + timeT2 · t₂ + timeT3 · t₃ + timeT4 · t₄
  = 5 + 0.15 · A + 3 · t₁ + 6 · t₂ + 12 · t₃ + 20 · t₄
```

**Rủi ro đặt sai (số lần đặt nhầm kỳ vọng; từ thời có mạng, giờ là số lần phải gỡ khối)**

```text
R = riskT2 · t₂ + riskT3 · t₃ + riskT4 · t₄ = 0.05 · t₂ + 0.25 · t₃ + 0.3 · t₄
```

T và R so trực tiếp được với "thời gian giải trung bình" và "số lần sai trung bình" ngoài thực tế.

## 4. Gợi ý Hard / Very Hard

- **Đánh dấu hiện tại do Reorder Levels đặt theo vị trí** (đỉnh mỗi khối 10 level: ...9 Hard, chia 30 dư 29 Very Hard). Bấm **Áp dụng gợi ý** sẽ thay bằng đánh dấu theo phân vị bên dưới (chạy lại Mechanic Injector sau đó).
- Chỉ xét level từ `minLevelForHard` (15) trở đi, không xét level trong `Levels/Test`.
- Xếp các level này theo D giảm dần: `veryHardShare` (4%) đầu tiên là **Very Hard**, `hardShare` (10%) kế tiếp là **Hard**.
- Tick cột **Hard** / **V.Hard** trong tool để đổi `LevelDefinition.difficulty`. Game sẽ hiện tab mức khó trên nút PLAY, khung HUD và hiệu ứng vào màn. **Áp dụng gợi ý** đặt theo đúng cột Gợi ý.
- Với level do Mechanic Injector sinh (`Levels/Mechanic`), tool ghi cả bản gốc ShikaCat, nên chạy lại Injector vẫn giữ đánh dấu. Lưu ý: Injector cho level Hard / Very Hard kết hợp 2 / 3 cơ chế, nên sau khi đổi đánh dấu mà chạy lại Injector thì các level đó sẽ khó hơn. Hãy bấm **Phân tích lại** sau đó.

## 5. So với thực tế và chỉnh công thức

1. **Xuất CSV**: mỗi level một dòng gồm mọi chỉ số ở mục 2 và `score`, `est_seconds`, `mistake_risk`, `marked`, `suggested`.
2. Lấy số liệu thực tế theo level, ví dụ từ Firebase Analytics với sự kiện bắt đầu / thắng / thua level, thời gian, số lần sai. Lưu thành CSV có cột khóa `level` (số thứ tự trong database), `id` hoặc `name`, cộng các cột số bất kỳ:

   ```csv
   level,win_rate,avg_seconds,avg_mistakes,attempts
   1,0.99,12,0.0,1520
   2,0.98,20,0.1,1490
   ```

3. **Nạp dữ liệu thực tế** trong tool. Tool hiện hệ số tương quan **Spearman ρ** giữa D / T / R và từng cột, cho cột đang chọn hiện thành chấm xanh trên biểu đồ, và thêm một cột vào bảng.
   - Kỳ vọng: ρ(D, win_rate) âm; ρ(D, avg_mistakes), ρ(giây, avg_seconds), ρ(rủi ro, avg_mistakes) dương.
   - |ρ| ≥ 0.5 là khá, ≥ 0.7 là tốt.
4. Chỉnh trọng số theo một trong hai cách:
   - **Bằng tay**: mở mục Trọng số, chỉnh rồi xem ρ thay đổi ngay.
   - **Hồi quy**: dùng các cột của CSV lý thuyết làm biến (`log2(clues)`, `ambiguity`, `t2/clues`, `t3/clues`, `t4/clues`, `narrow`, `hidden`, `locks`) và một chỉ số thực tế làm đích (vd. `1 - win_rate` hoặc `avg_mistakes`). Hồi quy tuyến tính, rồi nhập hệ số vào trọng số (nhân cùng một hằng số nếu muốn giữ thang D cũ). Ví dụ Python:

     ```python
     import pandas as pd, numpy as np
     t = pd.read_csv("LevelDifficulty_Theory.csv"); r = pd.read_csv("real.csv")
     d = t.merge(r, on="level")
     X = np.column_stack([np.log2(d.clues), d.ambiguity, d.t2/d.clues, d.t3/d.clues,
                          d.t4/d.clues, d.narrow, d.hidden, d.locks, np.ones(len(d))])
     y = 1 - d.win_rate
     w, *_ = np.linalg.lstsq(X, y, rcond=None)
     print(dict(zip(["wSize","wAmbiguity","wT2","wT3","wT4","wNarrow","wHidden","wLock","bias"], w)))
     ```

   - Nên chỉ dùng level có đủ lượt chơi (vd. `attempts ≥ 200`), và nhớ rằng level càng sau thì người chơi càng giỏi (thiên lệch sống sót). Có thể thêm số thứ tự level làm một biến nữa.

## 6. Giới hạn đã biết

- Mô phỏng chọn **kỹ thuật thấp nhất** mỗi bước, tức giả định người chơi luôn tìm ra nước dễ nhất. Người mới có thể đi đường khó hơn.
- Không đo yếu tố thị giác: màu, kích thước ô trên màn hình nhỏ, khối dài khó kéo.
- T2 chỉ nhìn trước 1 bước. Một số nước tool xếp vào T3 có thể người chơi thấy nhanh nhờ hình quen.
- Level gốc nhiều lời giải (17 level trong khoảng 15–55) có bước T4. Mechanic Injector không gắn khóa / "?" vào các level này.

## 7. Kết quả lần đầu (2026-10-05, trọng số mặc định)

- 206 level phân tích trong ~0.14 s. D: thấp nhất 10.0 (level 1), trung vị 37.4, cao nhất 59.1.
- Kỹ thuật khó nhất cần dùng: 43 level chỉ cần T1, 138 level cần tới T2, 13 level cần T3, 12 level có T4.
- Level 20 (Hard) và 30 (Very Hard) đánh dấu mẫu trước đây có D thấp (26.6 và 18.6, toàn bước T1), nên đã bỏ đánh dấu. Đã áp dụng gợi ý: 19 level Hard và 8 level Very Hard, rải từ level 42 tới 205.

## 8. Cập nhật sau khi thêm cơ chế tường / góc (2026-10-16, trọng số mặc định)

- Công thức thêm `wWall · Wa + wCorner · Co` (mục 3). Trước đó tường và góc chỉ được đếm, chưa vào điểm.

- 203 level phân tích (bỏ Level_081, 082, 106; sinh lại cơ chế). D: thấp nhất 12.5, trung vị 37.5, cao nhất 76.4.
- Kỹ thuật khó nhất cần dùng: 39 level chỉ cần T1, 130 level cần tới T2, 25 level cần T3, 9 level có T4.
- Đã áp dụng gợi ý: 19 level Hard và 8 level Very Hard.
  - Hard: 39, 67, 69, 109, 123, 129, 159, 161, 168, 173, 175, 179, 185, 186, 191, 196, 198, 199, 201.
  - Very Hard: 49, 119, 141, 169, 176, 189, 194, 202.
- Thêm hai trọng số làm trung vị D tăng nhẹ nhưng không đổi danh sách Hard / Very Hard (xếp hạng giữ nguyên).
