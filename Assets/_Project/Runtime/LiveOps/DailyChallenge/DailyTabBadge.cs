using DG.Tweening;
using UnityEngine;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>
    /// Trên nút tab Daily: ổ khóa (btn_lock2, như DailyChallenge/Lock gốc) khi chưa tới level mở khóa,
    /// chấm đỏ "!" (badge_red) khi hôm nay còn chưa chơi – thi thoảng nảy 2 nhịp để nhắc. Vẽ lại khi DailyChallengeManager đổi và mỗi lần Menu bật.
    /// </summary>
    public class DailyTabBadge : MonoBehaviour
    {
        private DailyChallengeManager _challenge;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(DailyChallengeManager challenge)
        {
            _challenge = challenge;
        }

        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject todayBadge;

        [Header("Nảy nhắc")]
        [Tooltip("Thời gian nghỉ giữa 2 lần nảy")]
        [SerializeField] private float interval = 2.5f;
        [SerializeField] private float jumpHeight = 14f;
        [SerializeField] private float jumpDuration = 0.5f;

        private Sequence _anim;
        private RectTransform _badgeRect;
        private Vector2 _badgePos;

        private void Awake()
        {
            if (todayBadge != null)
            {
                _badgeRect = (RectTransform)todayBadge.transform;
                _badgePos = _badgeRect.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            if (_challenge != null) _challenge.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_challenge != null) _challenge.Changed -= Refresh;
            StopBounce();
        }

        private void Refresh()
        {
            DailyChallengeManager m = _challenge;
            bool unlocked = m != null && m.IsUnlocked;
            bool show = unlocked && m.IsTodayPlayable;
            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (todayBadge != null) todayBadge.SetActive(show);
            if (show) StartBounce();
            else StopBounce();
        }

        /// <summary>Nghỉ -> nảy 2 nhịp tại chỗ, lặp mãi.</summary>
        private void StartBounce()
        {
            if (_badgeRect == null || _anim != null) return;
            _badgeRect.anchoredPosition = _badgePos;
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.AppendInterval(interval);
            seq.Append(_badgeRect.DOJumpAnchorPos(_badgePos, jumpHeight, 2, jumpDuration));
            seq.SetLoops(-1);
            _anim = seq;
        }

        private void StopBounce()
        {
            _anim?.Kill();
            _anim = null;
            if (_badgeRect != null) _badgeRect.anchoredPosition = _badgePos;
        }
    }
}
