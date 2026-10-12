using System;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Logic năng lượng (class C# thuần, nhận thời gian hiện tại từ ngoài nên test được, không phụ thuộc Unity).
    /// Hồi theo mốc thời gian: lưu số năng lượng + thời điểm bắt đầu đếm lượt hồi kế tiếp, mỗi lần đọc thì cộng bù
    /// số lượt đã trôi qua -> tắt game vẫn hồi. Đầy thì ngừng đếm.
    /// </summary>
    public class EnergyModel
    {
        public int Max { get; }
        public int RefillSeconds { get; }
        public int Energy { get; private set; }
        public long RefillStart { get; private set; }   // unix giây; chỉ có nghĩa khi Energy < Max
        public long InfiniteUntil { get; private set; } // unix giây; > now thì không tốn năng lượng

        public bool IsFull => Energy >= Max;

        public EnergyModel(int max, int refillSeconds, int energy, long refillStart, long infiniteUntil)
        {
            Max = Math.Max(1, max);
            RefillSeconds = Math.Max(1, refillSeconds);
            Energy = Math.Max(0, Math.Min(energy, Max));
            RefillStart = refillStart;
            InfiniteUntil = infiniteUntil;
        }

        public bool IsInfinite(long now) => InfiniteUntil > now;
        public bool CanPlay(long now) => IsInfinite(now) || Energy > 0;

        /// <summary>Cộng các lượt hồi đã đủ thời gian. Trả về true nếu Energy thay đổi.</summary>
        public bool Refresh(long now)
        {
            if (IsFull) return false;
            if (RefillStart <= 0 || RefillStart > now) RefillStart = now; // chưa có mốc / đồng hồ máy bị lùi

            long gained = (now - RefillStart) / RefillSeconds;
            if (gained <= 0) return false;

            Energy = (int)Math.Min(Max, Energy + gained);
            RefillStart = IsFull ? 0 : RefillStart + gained * RefillSeconds;
            return true;
        }

        /// <summary>Số giây tới lượt hồi kế tiếp; 0 khi đầy.</summary>
        public int SecondsToNext(long now)
        {
            if (IsFull) return 0;
            long start = RefillStart > 0 && RefillStart <= now ? RefillStart : now;
            return (int)Math.Max(0, RefillSeconds - (now - start));
        }

        /// <summary>Thua 1 level. Đang vô hạn thì không trừ. Trả về false nếu đã hết.</summary>
        public bool TryConsume(long now)
        {
            Refresh(now);
            if (IsInfinite(now)) return true;
            if (Energy <= 0) return false;
            if (IsFull) RefillStart = now; // từ đầy xuống -> bắt đầu đếm
            Energy--;
            return true;
        }

        /// <summary>Cộng năng lượng (xem ads, mua), không vượt Max; đồng hồ hồi giữ nguyên.</summary>
        public void Add(int amount, long now)
        {
            if (amount <= 0) return;
            Refresh(now);
            Energy = Math.Min(Max, Energy + amount);
            if (IsFull) RefillStart = 0;
        }

        /// <summary>Năng lượng vô hạn thêm <paramref name="seconds"/> giây (cộng dồn nếu đang vô hạn).</summary>
        public void AddInfinite(int seconds, long now)
        {
            if (seconds <= 0) return;
            InfiniteUntil = Math.Max(InfiniteUntil, now) + seconds;
        }

        // Chỉ dùng để thử: đặt thẳng số năng lượng / tua đồng hồ hồi
        public void DebugSet(int energy, long now)
        {
            Energy = Math.Max(0, Math.Min(energy, Max));
            RefillStart = IsFull ? 0 : now;
        }

        public void DebugSkip(int seconds) { if (RefillStart > 0) RefillStart -= seconds; }
    }
}
