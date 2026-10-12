using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Profile
{
    /// <summary>
    /// Ghi chỉ số hồ sơ (section "profile") từ event trong Core/Contracts, đặt trên Manager:
    /// thắng ngay lượt đầu (LevelCompletedEvent.Attempts), level đã qua, thắng Hard / Very Hard,
    /// chuỗi thắng (thua hẳn thì về 0), coin nhận được (CoinsChangedEvent), booster đã dùng (BoosterUsedEvent).
    /// Màn tutorial không phát các event này nên không tính.
    /// </summary>
    public class ProfileStatsRecorder : MonoBehaviour
    {
        private PlayerProfileService _profile;

        public void Init(PlayerProfileService profile) => _profile = profile;

        private void OnEnable()
        {
            EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);
            EventBus<LevelFailedEvent>.Subscribe(OnLevelFailed);
            EventBus<CoinsChangedEvent>.Subscribe(OnCoinsChanged);
            EventBus<BoosterUsedEvent>.Subscribe(OnBoosterUsed);
        }

        private void OnDisable()
        {
            EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
            EventBus<LevelFailedEvent>.Unsubscribe(OnLevelFailed);
            EventBus<CoinsChangedEvent>.Unsubscribe(OnCoinsChanged);
            EventBus<BoosterUsedEvent>.Unsubscribe(OnBoosterUsed);
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (_profile == null)
            {
                return;
            }
            ProfileSave p = _profile.Save;
            if (e.Attempts == 1)
            {
                p.firstTryWins++;
            }
            if (e.IsHard)
            {
                p.hardWins++;
            }
            if (!e.IsDaily)
            {
                p.levelsCleared = Mathf.Max(p.levelsCleared, e.Number); // Daily: Number là level thường chưa thắng
            }
            p.winStreak++;
            p.bestWinStreak = Mathf.Max(p.bestWinStreak, p.winStreak);
            _profile.NotifyStatsChanged();
        }

        private void OnLevelFailed(LevelFailedEvent e)
        {
            if (_profile == null)
            {
                return;
            }
            _profile.Save.winStreak = 0;
            _profile.NotifyStatsChanged();
        }

        private void OnCoinsChanged(CoinsChangedEvent e)
        {
            if (_profile == null || e.Delta <= 0)
            {
                return;
            }
            _profile.Save.coinsEarned += e.Delta;
            _profile.NotifyStatsChanged();
        }

        private void OnBoosterUsed(BoosterUsedEvent e)
        {
            if (_profile == null)
            {
                return;
            }
            _profile.Save.boostersUsed++;
            _profile.NotifyStatsChanged();
        }
    }
}
