using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Chuyển giữa Main Menu và màn chơi.
    /// - Menu: bật Canvas_Menu, tắt Canvas_HUD, ván đang nạp ở nền bị tạm dừng (không kéo được).
    /// - PLAY: kiểm tra năng lượng (GameFlowUI.RequestPlay), tắt menu, bật HUD, nạp lại level hiện tại từ đầu.
    /// - PlayDaily: như PLAY nhưng nạp level Daily Challenge; về Menu sau ván Daily thì mở tab Daily.
    /// - Lần đầu mở game (TutorialController.Pending): vào thẳng tutorial thay cho Menu.
    /// Popup nằm ở Canvas_Popup riêng nên dùng được ở cả 2 màn. Đổi màn thì phát ScreenChangedEvent (EventBus).
    /// </summary>
    public class ScreenFlow : MonoBehaviour, IScreenNavigator
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        [SerializeField] private GameObject menuCanvas;
        [SerializeField] private GameObject hudCanvas;
        [SerializeField] private MainMenuNavigator navigator;
        [SerializeField] private GameFlowUI gameFlow;
        [SerializeField] private bool startInMenu = true;

        public bool IsMenu { get; private set; }

        private void Start()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            EventBus<ShopOpenRequestedEvent>.Subscribe(OnShopRequested);
            if (TutorialController.Pending) EnterTutorial(); // lần đầu mở game: vào thẳng tutorial, xong thì vào level 1
            else if (startInMenu) ShowMenu();
            else EnterGame();
        }

        /// <summary>Vào màn chơi chạy tutorial (lần đầu mở game, hoặc Debug Panel xem lại).</summary>
        public void EnterTutorial()
        {
            if (TutorialController.Instance == null) return;
            IsMenu = false;
            if (menuCanvas != null) menuCanvas.SetActive(false);
            if (hudCanvas != null) hudCanvas.SetActive(true);
            TutorialController.Instance.Begin();
            EventBus<ScreenChangedEvent>.Raise(new ScreenChangedEvent(false));
        }

        private void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            EventBus<ShopOpenRequestedEvent>.Unsubscribe(OnShopRequested);
        }

        private void OnShopRequested(ShopOpenRequestedEvent _) => ShowShop();

        /// <summary>Về Main Menu: tab Home, hoặc tab Daily nếu vừa chơi level Daily.</summary>
        public void ShowMenu()
        {
            bool daily = _levels != null && _levels.IsDaily;
            IsMenu = true;
            if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
            if (_levels != null) _levels.Pause();
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (menuCanvas != null) menuCanvas.SetActive(true);
            if (navigator != null) navigator.Select(daily ? MainMenuNavigator.DailyTab : MainMenuNavigator.HomeTab, false);
            EventBus<ScreenChangedEvent>.Raise(new ScreenChangedEvent(true));
        }

        /// <summary>Về Main Menu (nếu đang trong ván) rồi chuyển sang tab Shop (vd thiếu coin khi nạp năng lượng).</summary>
        public void ShowShop()
        {
            bool wasMenu = IsMenu;
            if (!wasMenu) ShowMenu();
            if (navigator != null) navigator.Select(MainMenuNavigator.ShopTab, wasMenu); // đang ở Menu thì trượt sang, vừa từ ván ra thì nhảy luôn
        }

        /// <summary>Nút PLAY trên Home.</summary>
        public void Play()
        {
            if (!IsMenu) return;
            if (gameFlow != null) gameFlow.RequestPlay(EnterGame);
            else EnterGame();
        }

        /// <summary>Nút Play của tab Daily: kiểm tra năng lượng rồi vào ván, <paramref name="load"/> nạp level Daily.</summary>
        public void PlayDaily(System.Action load)
        {
            if (!IsMenu || load == null) return;
            if (gameFlow != null) gameFlow.RequestPlay(() => Enter(load));
            else Enter(load);
        }

        /// <summary>Vào ván ngay, bỏ qua kiểm tra năng lượng (Debug Panel).</summary>
        public void EnterGameNow() => EnterGame();

        // Nạp lại level thường hiện tại từ đầu (kể cả vừa chơi Daily), state Playing
        private void EnterGame() => Enter(() => { if (_levels != null) _levels.PlayCurrent(); });

        private void Enter(System.Action load)
        {
            IsMenu = false;
            if (menuCanvas != null) menuCanvas.SetActive(false);
            if (hudCanvas != null) hudCanvas.SetActive(true);
            load();
            EventBus<ScreenChangedEvent>.Raise(new ScreenChangedEvent(false));
        }

        // Level nạp lúc đang ở Menu (lúc mở game) -> tạm dừng lại
        private void OnLevelLoaded(LevelLoadedEvent _)
        {
            if (IsMenu && _levels != null) _levels.Pause();
        }
    }
}
