using System.Collections.Generic;
using System.Text;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Tách / ghép object JSON cấp ngoài cùng theo key: { "version": 2, "economy": { ... }, ... }.
    /// JsonUtility không đọc được Dictionary nên save chia section dùng lớp này cho cấp ngoài, JsonUtility cho từng section.
    /// Chỉ hiểu đủ JSON để tách đúng (chuỗi có escape, object / mảng lồng nhau); giá trị giữ nguyên văn bản gốc.
    /// </summary>
    public static class JsonSections
    {
        /// <summary>
        /// Đọc object ngoài cùng thành key → văn bản giá trị (object, mảng, số, chuỗi kèm dấu nháy...).
        /// Trả về false nếu không phải object JSON hợp lệ ở cấp ngoài.
        /// </summary>
        public static bool TryParse(string json, out Dictionary<string, string> sections)
        {
            sections = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            int i = SkipSpace(json, 0);
            if (i >= json.Length || json[i] != '{')
            {
                return false;
            }
            i = SkipSpace(json, i + 1);
            if (i < json.Length && json[i] == '}')
            {
                return SkipSpace(json, i + 1) == json.Length;
            }

            while (i < json.Length)
            {
                if (json[i] != '"' || !TryReadString(json, i, out int keyEnd, out string key))
                {
                    return false;
                }
                i = SkipSpace(json, keyEnd);
                if (i >= json.Length || json[i] != ':')
                {
                    return false;
                }
                int valueStart = SkipSpace(json, i + 1);
                if (!TrySkipValue(json, valueStart, out int valueEnd))
                {
                    return false;
                }
                sections[key] = json.Substring(valueStart, valueEnd - valueStart);

                i = SkipSpace(json, valueEnd);
                if (i >= json.Length)
                {
                    return false;
                }
                if (json[i] == '}')
                {
                    return SkipSpace(json, i + 1) == json.Length;
                }
                if (json[i] != ',')
                {
                    return false;
                }
                i = SkipSpace(json, i + 1);
            }
            return false;
        }

        /// <summary>
        /// Ghép key → văn bản JSON thành 1 object, mỗi key 1 dòng. Giá trị ghi nguyên văn: giá trị mới serialize
        /// (JsonUtility prettyPrint) thì qua <see cref="IndentValue"/> trước; giá trị đọc từ file giữ đúng như cũ.
        /// </summary>
        public static string Build(IEnumerable<KeyValuePair<string, string>> sections)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            bool first = true;
            foreach (KeyValuePair<string, string> s in sections)
            {
                if (!first)
                {
                    sb.Append(",\n");
                }
                first = false;
                sb.Append("    \"").Append(Escape(s.Key)).Append("\": ");
                sb.Append(s.Value.Trim());
            }
            sb.Append("\n}\n");
            return sb.ToString();
        }

        // ---------- Nội bộ ----------

        private static int SkipSpace(string s, int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }
            return i;
        }

        /// <summary>Đọc chuỗi bắt đầu ở s[start] == '"'; end = vị trí ngay sau dấu nháy đóng.</summary>
        private static bool TryReadString(string s, int start, out int end, out string value)
        {
            var sb = new StringBuilder();
            for (int i = start + 1; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"')
                {
                    end = i + 1;
                    value = sb.ToString();
                    return true;
                }
                if (c == '\\')
                {
                    if (++i >= s.Length)
                    {
                        break;
                    }
                    char e = s[i];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 4 >= s.Length)
                            {
                                end = s.Length;
                                value = null;
                                return false;
                            }
                            sb.Append((char)System.Convert.ToInt32(s.Substring(i + 1, 4), 16));
                            i += 4;
                            break;
                        default: sb.Append(e); break; // \" \\ \/
                    }
                    continue;
                }
                sb.Append(c);
            }
            end = s.Length;
            value = null;
            return false;
        }

        /// <summary>Bỏ qua 1 giá trị JSON bất kỳ bắt đầu ở s[start]; end = ngay sau giá trị.</summary>
        private static bool TrySkipValue(string s, int start, out int end)
        {
            end = start;
            if (start >= s.Length)
            {
                return false;
            }
            char c = s[start];
            if (c == '"')
            {
                return TryReadString(s, start, out end, out _);
            }
            if (c == '{' || c == '[')
            {
                int depth = 0;
                for (int i = start; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (ch == '"')
                    {
                        if (!TryReadString(s, i, out int strEnd, out _))
                        {
                            return false;
                        }
                        i = strEnd - 1;
                    }
                    else if (ch == '{' || ch == '[')
                    {
                        depth++;
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i + 1;
                            return true;
                        }
                    }
                }
                return false;
            }
            // số, true, false, null: tới dấu phẩy / ngoặc đóng / khoảng trắng
            int j = start;
            while (j < s.Length && s[j] != ',' && s[j] != '}' && s[j] != ']' && !char.IsWhiteSpace(s[j]))
            {
                j++;
            }
            end = j;
            return j > start;
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>Giá trị nhiều dòng mới serialize: thụt thêm 4 dấu cách từ dòng thứ 2 để nằm gọn dưới key.</summary>
        public static string IndentValue(string value) => value.Trim().Replace("\r\n", "\n").Replace("\n", "\n    ");
    }
}
