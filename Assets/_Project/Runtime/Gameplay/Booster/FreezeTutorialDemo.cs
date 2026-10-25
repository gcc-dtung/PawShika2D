using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Bước 2 tutorial Đóng băng (tách khỏi BoosterTutorial): nghe BoosterTutorialCompletedEvent, nếu là Freeze thì
    /// nền tối đã tắt, hiện lời nhắc "đồng hồ đã đứng yên" vài giây rồi tắt (người chơi kéo tiếp bình thường).
    /// Booster khác muốn có bước riêng sau tutorial: viết 1 component tương tự nghe cùng sự kiện.
    /// </summary>
    public class FreezeTutorialDemo : MonoBehaviour
    {
        private LivesEnergyConfig _lives;
        private LevelTimer _timer;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelTimer timer, LivesEnergyConfig lives)
        {
            _timer = timer;
            _lives = lives;
        }

        [SerializeField] private BoosterTutorialView view;
        [SerializeField] private string frozenMessage = "The timer is frozen for {0} seconds - take your time!";
        [Tooltip("Chờ sau khi bấm nút Đóng băng rồi mới hiện lời nhắc")]
        [SerializeField] private float startDelay = 0.2f;
        [Tooltip("Thời gian hiện lời nhắc")]
        [SerializeField] private float messageTime = 2.5f;

        private Tween _demoStart;
        private Tween _demoEnd;
        private bool _showingMessage;

        private void OnEnable()
        {
            EventBus<BoosterTutorialCompletedEvent>.Subscribe(OnTutorialCompleted);
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
        }

        private void OnDisable()
        {
            EventBus<BoosterTutorialCompletedEvent>.Unsubscribe(OnTutorialCompleted);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            StopDemo();
        }

        private void OnTutorialCompleted(BoosterTutorialCompletedEvent e)
        {
            if (e.Type != BoosterType.Freeze) return;
            // Đóng băng bật ngay sau sự kiện này (BoosterManager), chờ 1 nhịp rồi mới kiểm tra
            _demoStart?.Kill();
            _demoStart = DOVirtual.DelayedCall(startDelay, StartDemo, false).SetLink(gameObject);
        }

        private void OnLevelLoaded(LevelLoadedEvent _) => StopDemo();

        private void StartDemo()
        {
            _demoStart = null;
            LevelTimer timer = _timer;
            if (timer == null || !timer.IsFrozen || view == null) return; // không bật được: bỏ bước này
            view.ShowMessage(string.Format(frozenMessage, Mathf.CeilToInt(_lives.freezeSeconds)));
            _showingMessage = true;
            _demoEnd?.Kill();
            _demoEnd = DOVirtual.DelayedCall(messageTime, HideMessage).SetLink(gameObject);
        }

        private void StopDemo()
        {
            _demoStart?.Kill();
            _demoStart = null;
            _demoEnd?.Kill();
            _demoEnd = null;
            HideMessage();
        }

        private void HideMessage()
        {
            if (!_showingMessage) return;
            _showingMessage = false;
            if (view != null) view.Hide();
        }
    }
}
