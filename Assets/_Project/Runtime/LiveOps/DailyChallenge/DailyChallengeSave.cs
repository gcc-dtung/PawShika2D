using System;
using System.Collections.Generic;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>Daily Challenge (DailyChallengeModel): ngày mở khóa, các ngày đã thắng, quà mốc đã nhận.</summary>
    [Serializable]
    public class DailyChallengeSave
    {
        public const string Key = "dailyChallenge";

        public long startDay = -1;                              // ngày mở khóa (số ngày từ 1/1/1970 theo lịch), -1 = chưa mở; ngày trước đó không tính lỡ
        public List<int> doneDays = new List<int>();            // các ngày đã thắng (số ngày từ 1/1/1970)
        public List<int> claimedGifts = new List<int>();        // quà mốc đã nhận (DailyChallengeModel.GiftKey: năm, tháng, mốc)
    }
}
