using UnityEngine;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>
    /// Trên nút tab Daily: ổ khóa (btn_lock2, như DailyChallenge/Lock gốc) khi chưa tới level mở khóa,
    /// chấm đỏ (badge_red) khi hôm nay còn chưa chơi. Vẽ lại khi DailyChallengeManager đổi và mỗi lần Menu bật.
    /// </summary>
    public class DailyTabBadge : MonoBehaviour
    {
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject todayBadge;

        private void OnEnable()
        {
            if (DailyChallengeManager.Instance != null) DailyChallengeManager.Instance.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (DailyChallengeManager.Instance != null) DailyChallengeManager.Instance.Changed -= Refresh;
        }

        private void Refresh()
        {
            DailyChallengeManager m = DailyChallengeManager.Instance;
            bool unlocked = m != null && m.IsUnlocked;
            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (todayBadge != null) todayBadge.SetActive(unlocked && m.IsTodayPlayable);
        }
    }
}
