using System;

namespace PawBlock.Game
{
    /// <summary>
    /// Luật điểm bot của cuộc thi tuần (giả lập offline, kiểu "bám đuổi" của game casual):
    /// - Điểm thô mỗi bot = tốc độ (điểm / giờ, ngẫu nhiên theo seed tuần) x số giờ đã qua trong tuần.
    /// - Chỉ <c>above</c> bot (theo điểm thô cao nhất) được đứng trên người chơi, và hơn tối đa maxAhead điểm; các bot còn lại bị giữ dưới người chơi.
    /// - Bot đứng ngay dưới người chơi luôn bám trong maxLead điểm (bot thứ hai trong 2 x maxLead): chơi nhiều thì lên top 1 nhưng không bỏ xa top 2.
    /// - Số bot được đứng trên (<see cref="AboveCount"/>) tăng theo thời gian bỏ chơi, tối đa maxDrop.
    /// Hàm thuần, không đọc giờ / save để test được (WeeklyContest lo phần đó).
    /// </summary>
    public static class WeeklyBots
    {
        [Serializable]
        public struct Settings
        {
            public int maxLead;
            public int maxAhead;
            public float hoursPerRank;
            public int maxDrop;
            public float rateMin;
            public float rateMax;

            public static Settings Default => new Settings
            {
                maxLead = 3, maxAhead = 12, hoursPerRank = 8f, maxDrop = 5, rateMin = 0.1f, rateMax = 0.8f,
            };
        }

        /// <summary>Số bot được đứng trên người chơi: hạng còn nợ (debt) + cứ hoursPerRank giờ bỏ chơi thêm 1, tối đa maxDrop.</summary>
        public static int AboveCount(int debt, double idleHours, Settings s)
        {
            int idleRanks = s.hoursPerRank > 0f ? (int)(Math.Max(0.0, idleHours) / s.hoursPerRank) : 0;
            return Math.Min(s.maxDrop, Math.Max(0, debt) + idleRanks);
        }

        /// <summary>Điểm của <paramref name="rivals"/> bot (cùng thứ tự với danh sách tên).</summary>
        public static int[] Scores(int seed, int rivals, double hoursIntoWeek, int playerScore, int above, Settings s)
        {
            var rng = new Random(seed);
            var raw = new int[rivals];
            for (int i = 0; i < rivals; i++)
            {
                double rate = s.rateMin + rng.NextDouble() * (s.rateMax - s.rateMin);
                raw[i] = (int)(rate * Math.Max(0.0, hoursIntoWeek));
            }

            // Thứ tự theo điểm thô giảm dần (bằng nhau thì theo chỉ số để ổn định)
            var order = new int[rivals];
            for (int i = 0; i < rivals; i++) order[i] = i;
            Array.Sort(order, (a, b) => raw[a] != raw[b] ? raw[b].CompareTo(raw[a]) : a.CompareTo(b));

            var result = new int[rivals];
            int p = Math.Max(0, playerScore);
            int chasers = 0;
            for (int k = 0; k < rivals; k++)
            {
                int i = order[k];
                if (k < above)
                {
                    // Được vượt: hơn người chơi ít nhất 1 điểm; trần giảm dần theo thứ hạng (maxAhead, maxAhead - step...) để điểm không dồn cục
                    int step = Math.Max(1, s.maxAhead / Math.Max(1, s.maxDrop));
                    int cap = p + Math.Max(1, s.maxAhead - k * step);
                    result[i] = Math.Min(Math.Max(raw[i], p + 1), cap);
                    continue;
                }
                int v = Math.Min(raw[i], p - 1);
                if (chasers < 2 && p > 0)
                {
                    // Bám đuổi: bot đầu dưới người chơi trong maxLead, bot sau trong 2 x maxLead
                    v = Math.Max(v, p - s.maxLead * (chasers + 1));
                    chasers++;
                }
                result[i] = Math.Max(0, Math.Min(v, Math.Max(0, p - 1)));
            }
            return result;
        }
    }
}
