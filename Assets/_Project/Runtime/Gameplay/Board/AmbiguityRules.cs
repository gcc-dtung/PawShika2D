using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Cơ chế "số mơ hồ": ô số không ghi đúng số ô của khối mà chỉ cho khoảng / chẵn lẻ / 2 giá trị khả dĩ.
    /// <see cref="ClueData.value"/> vẫn là số ô THẬT của lời giải (tổng các số = số ô chơi được, Validator dựa vào đó);
    /// <see cref="Accepts"/> là luật dùng chung cho BoardModel.Check, BoardModel.IsSolved và ShikakuSolver
    /// (BoardModel và solver lệch luật = Hint chỉ ra vùng người chơi không đặt được).
    /// </summary>
    public static class AmbiguityRules
    {
        public static readonly ClueAmbiguity[] Kinds =
        {
            ClueAmbiguity.AtLeast, ClueAmbiguity.AtMost, ClueAmbiguity.Even, ClueAmbiguity.Odd, ClueAmbiguity.Either
        };

        /// <summary>Khối <paramref name="area"/> ô có thỏa ô số này không (ô "?" do BoardModel / solver xử lý riêng).</summary>
        public static bool Accepts(ClueMechanic m, int value, int area)
        {
            switch (m.ambiguity)
            {
                case ClueAmbiguity.AtLeast: return area >= m.ambiguityValue;
                case ClueAmbiguity.AtMost: return area <= m.ambiguityValue;
                case ClueAmbiguity.Even: return area % 2 == 0;
                case ClueAmbiguity.Odd: return area % 2 == 1;
                case ClueAmbiguity.Either: return area == value || area == m.ambiguityValue;
                default: return area == value;
            }
        }

        /// <summary>Số ô nhỏ nhất thỏa ô số (kéo chưa tới số này là "thiếu ô").</summary>
        public static int MinArea(ClueMechanic m, int value)
        {
            switch (m.ambiguity)
            {
                case ClueAmbiguity.AtLeast: return Mathf.Max(1, m.ambiguityValue);
                case ClueAmbiguity.AtMost: return 1;
                case ClueAmbiguity.Even: return 2;
                case ClueAmbiguity.Odd: return 1;
                case ClueAmbiguity.Either: return Mathf.Min(value, m.ambiguityValue);
                default: return value;
            }
        }

        /// <summary>Số ô lớn nhất thỏa ô số (int.MaxValue = không có trần: kéo thêm lúc nào cũng còn có thể đúng).</summary>
        public static int MaxArea(ClueMechanic m, int value)
        {
            switch (m.ambiguity)
            {
                case ClueAmbiguity.AtMost: return m.ambiguityValue;
                case ClueAmbiguity.Either: return Mathf.Max(value, m.ambiguityValue);
                case ClueAmbiguity.AtLeast:
                case ClueAmbiguity.Even:
                case ClueAmbiguity.Odd: return int.MaxValue;
                default: return value;
            }
        }

        /// <summary>
        /// Chữ trên ô số / khối (rich text của TMP). Nhãn rộng hơn 1 số 2 chữ số thì thu nhỏ cho vừa ô (FitSize).
        /// Fredoka không có glyph ≥ ≤ nên dùng so sánh hẳn: "≥ 6" hiện ">5", "≤ 4" hiện "<5" (noparse để TMP không coi "<" là thẻ).
        /// </summary>
        public static string Label(ClueMechanic m, int value)
        {
            switch (m.ambiguity)
            {
                case ClueAmbiguity.AtLeast: return Fit(">", (m.ambiguityValue - 1).ToString());
                case ClueAmbiguity.AtMost: return Fit("<", (m.ambiguityValue + 1).ToString());
                case ClueAmbiguity.Even: return "E"; // chẵn (Even), giải thích trong popup "Fuzzy Number"
                case ClueAmbiguity.Odd: return "O"; // lẻ (Odd)
                case ClueAmbiguity.Either:
                    return Fit("", Mathf.Min(value, m.ambiguityValue) + "/" + Mathf.Max(value, m.ambiguityValue));
                default: return value.ToString();
            }
        }

        // Bề rộng tối đa của nhãn (đơn vị 1/1000 em của Fredoka-Bold): hơi hẹp hơn số "25" (1059) để còn lề trong ô
        private const int LabelBudget = 1000;

        /// <summary>Bọc <paramref name="symbol"/> (chưa escape) + <paramref name="text"/>, thu nhỏ theo bề rộng thật của chữ.</summary>
        private static string Fit(string symbol, string text)
        {
            int width = Advance(symbol) + Advance(text);
            int percent = width <= LabelBudget ? 100 : Mathf.Max(35, LabelBudget * 100 / width);
            string body = (symbol.Length > 0 ? "<noparse>" + symbol + "</noparse>" : "") + text;
            return percent >= 100 ? body : "<size=" + percent + "%>" + body + "</size>";
        }

        /// <summary>Tổng bề rộng (advance) các ký tự trong Fredoka-Bold, đo bằng fontTools; ký tự khác tính như chữ số.</summary>
        private static int Advance(string s)
        {
            int w = 0;
            foreach (char c in s)
            {
                switch (c)
                {
                    case '1': w += 379; break;
                    case '5': w += 493; break;
                    case '6': case '7': case '9': w += 521; break;
                    case '8': w += 541; break;
                    case '4': w += 551; break;
                    case '/': w += 489; break;
                    case '<': case '>': w += 583; break;
                    default: w += 566; break; // 0, 2, 3
                }
            }
            return w;
        }

        /// <summary>Mô tả ngắn cho Level Editor / log.</summary>
        public static string Describe(ClueMechanic m, int value)
        {
            switch (m.ambiguity)
            {
                case ClueAmbiguity.AtLeast: return "≥ " + m.ambiguityValue;
                case ClueAmbiguity.AtMost: return "≤ " + m.ambiguityValue;
                case ClueAmbiguity.Even: return "chẵn";
                case ClueAmbiguity.Odd: return "lẻ";
                case ClueAmbiguity.Either: return Mathf.Min(value, m.ambiguityValue) + " hoặc " + Mathf.Max(value, m.ambiguityValue);
                default: return value.ToString();
            }
        }

        /// <summary>
        /// Tạo cơ chế loại <paramref name="kind"/> cho ô số có số thật <paramref name="value"/> (luôn thỏa số thật, nên lời giải còn đúng).
        /// Trả về false nếu loại này vô nghĩa với số đó (vd. chẵn cho số lẻ, "≥" cho số 1).
        /// Chưa kiểm tra có giúp suy luận hay không: Validator ("không cần đoán") quyết định giữ hay bỏ.
        /// </summary>
        public static bool TryMake(ClueMechanic baseMechanic, int value, ClueAmbiguity kind, System.Random rng, out ClueMechanic result)
        {
            result = baseMechanic;
            result.ambiguity = kind;
            result.ambiguityValue = 0;
            switch (kind)
            {
                case ClueAmbiguity.AtLeast:
                    if (value < 2) return false;
                    result.ambiguityValue = Mathf.Max(2, value - rng.Next(0, 3)); // ngưỡng sát số thật: value, value-1 hoặc value-2
                    return true;
                case ClueAmbiguity.AtMost:
                    result.ambiguityValue = value + rng.Next(0, 3);
                    return true;
                case ClueAmbiguity.Even:
                    return value % 2 == 0;
                case ClueAmbiguity.Odd:
                    return value % 2 == 1 && value >= 3;
                case ClueAmbiguity.Either:
                    int other = value + (rng.Next(2) == 0 ? -1 : 1) * rng.Next(1, 4);
                    if (other < 1) other = value + rng.Next(1, 4);
                    result.ambiguityValue = other;
                    return other != value;
                default:
                    return false;
            }
        }
    }
}
