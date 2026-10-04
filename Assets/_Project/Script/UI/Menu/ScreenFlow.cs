using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Chuyển giữa Main Menu và màn chơi.
/// - Menu: bật Canvas_Menu, tắt Canvas_HUD, ván đang nạp ở nền bị tạm dừng (không kéo được).
/// - PLAY: kiểm tra năng lượng (GameFlowUI.RequestPlay), tắt menu, bật HUD, nạp lại level hiện tại từ đầu.
/// Popup nằm ở Canvas_Popup riêng nên dùng được ở cả 2 màn.
/// </summary>
public class ScreenFlow : Singleton<ScreenFlow>
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private MainMenuNavigator navigator;
    [SerializeField] private GameFlowUI gameFlow;
    [SerializeField] private bool startInMenu = true;
    [SerializeField] private int shopTab = 0;

    [Header("Events")]
    public UnityEvent<bool> onScreenChanged = new UnityEvent<bool>(); // true = đang ở Menu

    public bool IsMenu { get; private set; }

    private void Start()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
        if (startInMenu) ShowMenu();
        else EnterGame();
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
    }

    /// <summary>Về Main Menu (tab Home).</summary>
    public void ShowMenu()
    {
        IsMenu = true;
        if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
        if (LevelManager.Instance != null) LevelManager.Instance.Pause();
        if (hudCanvas != null) hudCanvas.SetActive(false);
        if (menuCanvas != null) menuCanvas.SetActive(true);
        if (navigator != null) navigator.Select(1, false);
        onScreenChanged?.Invoke(true);
    }

    /// <summary>Về Main Menu (nếu đang trong ván) rồi chuyển sang tab Shop (vd thiếu coin khi nạp năng lượng).</summary>
    public void ShowShop()
    {
        bool wasMenu = IsMenu;
        if (!wasMenu) ShowMenu();
        if (navigator != null) navigator.Select(shopTab, wasMenu); // đang ở Menu thì trượt sang, vừa từ ván ra thì nhảy luôn
    }

    /// <summary>Nút PLAY trên Home.</summary>
    public void Play()
    {
        if (!IsMenu) return;
        if (gameFlow != null) gameFlow.RequestPlay(EnterGame);
        else EnterGame();
    }

    /// <summary>Vào ván ngay, bỏ qua kiểm tra năng lượng (Debug Panel).</summary>
    public void EnterGameNow() => EnterGame();

    private void EnterGame()
    {
        IsMenu = false;
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (hudCanvas != null) hudCanvas.SetActive(true);
        if (LevelManager.Instance != null) LevelManager.Instance.Retry(); // nạp lại level hiện tại từ đầu, state Playing
        onScreenChanged?.Invoke(false);
    }

    // Level nạp lúc đang ở Menu (lúc mở game) -> tạm dừng lại
    private void OnLevelLoaded(int _)
    {
        if (IsMenu && LevelManager.Instance != null) LevelManager.Instance.Pause();
    }
}
