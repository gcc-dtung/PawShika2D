using System;

namespace PawBlock.Features.Economy
{
    [Serializable]
    public class EnergySave
    {
        public int energy = -1;      // -1 = chưa có: đầy
        public long refillStart;     // unix giây
        public long infiniteUntil;   // unix giây
    }
}
