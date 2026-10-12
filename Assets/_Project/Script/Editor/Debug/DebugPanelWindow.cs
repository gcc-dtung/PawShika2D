using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → PawBlock → Debug Panel: chuyển level, thắng / thua ngay, cộng coin / mạng / năng lượng / booster,
/// điểm danh, tốc độ game. Dùng trong Play mode (ngoài Play chỉ xem hướng dẫn). Gọi DebugCheats nên hiệu ứng,
/// popup, lưu save chạy như chơi thật. Có thể gắn (dock) cạnh Game view.
/// </summary>
public class DebugPanelWindow : EditorWindow
{
    private int _levelInput = 1;
    private int _coinInput = 1000;
    private int _starInput = 10;
    private Vector2 _scroll;
    private bool _foldLevel = true, _foldResult = true, _foldCoin = true, _foldTimer = true, _foldBooster = true, _foldOther = true;

    private static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f };

    [MenuItem("Tools/PawBlock/Debug Panel %#d", priority = 20)] // Ctrl + Shift + D
    public static void Open()
    {
        DebugPanelWindow w = GetWindow<DebugPanelWindow>("Debug Panel");
        w.minSize = new Vector2(300, 360);
    }

    // Cập nhật số liệu hiển thị khi đang chơi
    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying) Repaint();
    }

    private void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Bấm Play để dùng Debug Panel.\nPhím tắt mở cửa sổ: Ctrl + Shift + D.", MessageType.Info);
            if (GUILayout.Button("Play", GUILayout.Height(28))) EditorApplication.isPlaying = true;
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawStatus();
        DrawLevel();
        DrawResult();
        DrawCoins();
        DrawTimerEnergy();
        DrawBoosters();
        DrawOther();
        EditorGUILayout.EndScrollView();
    }

    // ---------- Trạng thái ----------

    private void DrawStatus()
    {
        LevelManager lm = LevelManager.Instance;
        string screen = DebugCheats.InMenu ? "Main Menu" : "Trong ván";
        string state = lm != null ? lm.State.ToString() : "-";
        string energy = EnergyManager.Instance != null ? (EnergyManager.Instance.IsInfinite ? "∞" : $"{EnergyManager.Instance.Current}/{EnergyManager.Instance.Max}") : "-";

        EditorGUILayout.HelpBox(
            $"Level {DebugCheats.CurrentLevel}/{DebugCheats.LevelCount}   ·   {screen}   ·   {state}\n" +
            $"Coin {CoinWallet.Balance}   ·   ★ {StarWallet.Balance}   ·   Giờ {DebugCheats.TimerStatus}   ·   Năng lượng {energy}",
            MessageType.None);
    }

    // ---------- Level ----------

    private void DrawLevel()
    {
        if (!Section("Level", ref _foldLevel)) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("◀ Trước")) DebugCheats.PreviousLevel();
            if (GUILayout.Button("Chơi lại")) DebugCheats.RetryLevel();
            if (GUILayout.Button("Sau ▶")) DebugCheats.NextLevel();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Tới level", GUILayout.Width(60));
            _levelInput = EditorGUILayout.IntSlider(_levelInput, 1, Mathf.Max(1, DebugCheats.LevelCount));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Đổi level")) DebugCheats.GoToLevel(_levelInput);
            if (GUILayout.Button("Đổi + vào ván")) DebugCheats.PlayLevel(_levelInput);
        }
        if (GUILayout.Button("Vào ván level hiện tại (bỏ qua năng lượng)")) DebugCheats.PlayLevel(DebugCheats.CurrentLevel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Chơi lại tutorial")) DebugCheats.PlayTutorial();
            if (GUILayout.Button(new GUIContent("Bỏ cờ đã xem tutorial", "Mở game lần sau (đang ở level 1) sẽ chạy lại tutorial"))) DebugCheats.ResetTutorial();
        }
        if (GUILayout.Button(new GUIContent("Xem lại popup cơ chế", "Bỏ cờ đã xem popup Hướng / Khóa / \"?\"; nạp level có cơ chế là hiện lại"))) DebugCheats.ResetMechanicIntros();
    }

    // ---------- Thắng / thua ----------

    private void DrawResult()
    {
        if (!Section("Thắng / Thua", ref _foldResult)) return;

        bool canPlay = !DebugCheats.InMenu && LevelManager.Instance != null && LevelManager.Instance.State == GameState.Playing;
        using (new EditorGUI.DisabledScope(!canPlay))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Thắng ngay", GUILayout.Height(26))) DebugCheats.WinNow();
                if (GUILayout.Button("Đặt 1 khối đúng", GUILayout.Height(26))) DebugCheats.SolveOneBlock();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Hết giờ (Thêm giờ)")) DebugCheats.TimeUp();
                if (GUILayout.Button("Thua hẳn")) DebugCheats.LoseNow();
            }
        }
        if (!canPlay) EditorGUILayout.LabelField("Chỉ dùng được khi đang chơi trong ván.", EditorStyles.miniLabel);
    }

    // ---------- Coin ----------

    private void DrawCoins()
    {
        if (!Section("Coin / Sao ★", ref _foldCoin)) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+100")) DebugCheats.AddCoins(100);
            if (GUILayout.Button("+1000")) DebugCheats.AddCoins(1000);
            if (GUILayout.Button("+10000")) DebugCheats.AddCoins(10000);
            if (GUILayout.Button("= 0")) DebugCheats.SetCoins(0);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _coinInput = EditorGUILayout.IntField(_coinInput);
            if (GUILayout.Button("Đặt", GUILayout.Width(60))) DebugCheats.SetCoins(_coinInput);
        }

        EditorGUILayout.LabelField($"Sao ★ (Vòng quay may mắn): {StarWallet.Balance}", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+10")) DebugCheats.AddStars(10);
            if (GUILayout.Button("+50")) DebugCheats.AddStars(50);
            if (GUILayout.Button("-10")) DebugCheats.AddStars(-10);
            if (GUILayout.Button("= 0")) DebugCheats.SetStars(0);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _starInput = EditorGUILayout.IntField(_starInput);
            if (GUILayout.Button("Đặt", GUILayout.Width(60))) DebugCheats.SetStars(_starInput);
        }
    }

    // ---------- Đồng hồ / năng lượng ----------

    private void DrawTimerEnergy()
    {
        if (!Section("Đồng hồ (trong ván) / Năng lượng (Menu)", ref _foldTimer)) return;

        EditorGUILayout.LabelField($"Đồng hồ trong ván · {DebugCheats.TimerStatus}", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("-10s")) DebugCheats.AddTime(-10f);
            if (GUILayout.Button("+10s")) DebugCheats.AddTime(10f);
            if (GUILayout.Button("+60s")) DebugCheats.AddTime(60f);
            if (GUILayout.Button("Đầy giờ")) DebugCheats.FullTime();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(DebugCheats.TimerPaused ? "Chạy đồng hồ" : "Dừng đồng hồ")) DebugCheats.ToggleTimerPause();
            if (GUILayout.Button("Đóng băng")) DebugCheats.Freeze();
        }

        EditorGUILayout.LabelField("Năng lượng", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+1")) DebugCheats.AddEnergy(1);
            if (GUILayout.Button("Đầy")) DebugCheats.EnergyFull();
            if (GUILayout.Button("Hết (0)")) DebugCheats.EnergyEmpty();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Bỏ qua 1 lần chờ hồi")) DebugCheats.EnergySkipRefill();
            if (GUILayout.Button("Vô hạn 5 phút")) DebugCheats.EnergyInfinite(5);
        }
    }

    // ---------- Booster ----------

    private void DrawBoosters()
    {
        if (!Section("Booster", ref _foldBooster)) return;

        foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{type}: {DebugCheats.BoosterCount(type)}", GUILayout.Width(100));
                if (GUILayout.Button("+1")) DebugCheats.AddBooster(type, 1);
                if (GUILayout.Button("+10")) DebugCheats.AddBooster(type, 10);
                if (GUILayout.Button("= 0")) DebugCheats.SetBooster(type, 0);
            }
        }
        if (GUILayout.Button("Tất cả +10"))
        {
            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType))) DebugCheats.AddBooster(type, 10);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Mở khóa tất cả")) DebugCheats.UnlockAllBoosters();
            if (GUILayout.Button(new GUIContent("Khóa lại tất cả", "Xem lại tutorial mở khóa: nạp lại level >= level mở của booster"))) DebugCheats.LockAllBoosters();
        }
    }

    // ---------- Khác ----------

    private void DrawOther()
    {
        if (!Section("Khác", ref _foldOther)) return;

        EditorGUILayout.LabelField("Điểm danh", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sang ngày mới")) DebugCheats.DailyNextDay();
            if (GUILayout.Button("Reset chu kỳ")) DebugCheats.DailyReset();
        }

        EditorGUILayout.LabelField($"Tốc độ game: x{Time.timeScale:0.##}", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (float s in Speeds)
            {
                if (GUILayout.Button("x" + s)) Time.timeScale = s;
            }
        }
        EditorGUILayout.LabelField("Popup / hiệu ứng UI chạy theo thời gian thực nên không đổi tốc độ.", EditorStyles.miniLabel);

        if (GUILayout.Button("Thử mua bằng VND (IAP)")) DebugCheats.TryIAP();
        if (GUILayout.Button("Lưu save ngay")) DebugCheats.SaveNow();
    }

    private static bool Section(string title, ref bool fold)
    {
        EditorGUILayout.Space(4);
        fold = EditorGUILayout.Foldout(fold, title, true, EditorStyles.foldoutHeader);
        return fold;
    }
}
