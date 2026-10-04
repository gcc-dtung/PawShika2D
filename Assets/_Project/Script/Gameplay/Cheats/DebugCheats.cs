using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lệnh cheat / debug dùng chung (Debug Panel trong Editor gọi; sau này có thể gắn vào panel trong game).
/// Chỉ thao tác qua API sẵn có của các manager nên hiệu ứng, popup, lưu save chạy đúng như chơi thật.
/// Mọi lệnh tự bỏ qua nếu manager tương ứng chưa có (vd đang không Play).
/// </summary>
public static class DebugCheats
{
    // ---------- Level ----------

    public static int LevelCount => LevelManager.Instance != null ? LevelManager.Instance.LevelCount : 0;
    public static int CurrentLevel => LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : 0;
    public static bool InMenu => ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;

    /// <summary>Nạp level số <paramref name="number"/> (bắt đầu từ 1). Đang ở Menu thì chỉ đổi level (nút PLAY hiện số mới).</summary>
    public static void GoToLevel(int number)
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.LoadLevel(Mathf.Clamp(number, 1, Mathf.Max(1, LevelCount)) - 1);
    }

    /// <summary>Nạp level rồi vào ván ngay (bỏ qua kiểm tra năng lượng).</summary>
    public static void PlayLevel(int number)
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.LoadLevel(Mathf.Clamp(number, 1, Mathf.Max(1, LevelCount)) - 1);
        if (InMenu) ScreenFlow.Instance.EnterGameNow(); // EnterGame nạp lại level hiện tại từ đầu
    }

    public static void NextLevel() => GoToLevel(CurrentLevel >= LevelCount ? 1 : CurrentLevel + 1);
    public static void PreviousLevel() => GoToLevel(Mathf.Max(1, CurrentLevel - 1));
    public static void RetryLevel() => GoToLevel(CurrentLevel);

    // ---------- Thắng / thua ----------

    /// <summary>Đặt hết khối còn lại theo lời giải -> thắng thật (lưới lóe sáng, màn thắng).</summary>
    public static void WinNow()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.Board == null || gm.IsGameWon) return;
        if (!gm.Board.TrySolveRemaining(out List<RectInt> rects) || rects == null)
        {
            Debug.LogWarning("[DebugCheats] Không tìm được lời giải khớp các khối đã đặt.");
            return;
        }
        PlaceAll(gm, rects, int.MaxValue);
    }

    /// <summary>Đặt đúng 1 khối của lời giải (như Hint, không tốn booster).</summary>
    public static void SolveOneBlock()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.Board == null || gm.IsGameWon) return;
        if (gm.Board.TrySolveRemaining(out List<RectInt> rects) && rects != null) PlaceAll(gm, rects, 1);
    }

    // Ô số còn khóa thì chưa đặt được: lặp tới khi đặt hết (mỗi khối đặt đúng mở khóa dần)
    private static void PlaceAll(GridManager gm, List<RectInt> rects, int max)
    {
        List<RectInt> left = new List<RectInt>(rects);
        int placed = 0;
        bool any = true;
        while (left.Count > 0 && any && placed < max)
        {
            any = false;
            for (int i = left.Count - 1; i >= 0 && placed < max; i--)
            {
                if (!gm.PlaceSolved(left[i])) continue;
                left.RemoveAt(i);
                placed++;
                any = true;
            }
        }
    }

    public static void LoseLife()
    {
        if (LivesManager.Instance != null) LivesManager.Instance.LoseLife();
    }

    /// <summary>Mất hết mạng -> popup Hồi mạng.</summary>
    public static void OutOfLives()
    {
        LivesManager lives = LivesManager.Instance;
        if (lives == null) return;
        int guard = 10;
        while (!lives.IsOutOfLives && guard-- > 0) lives.LoseLife(); // lần đầu có thể chỉ phá khiên
    }

    /// <summary>Thua hẳn (bỏ qua popup Hồi mạng) -> popup Thua, trừ 1 năng lượng.</summary>
    public static void LoseNow()
    {
        LevelManager lm = LevelManager.Instance;
        if (lm == null || lm.State != GameState.Playing) return;
        // LevelManager đổi sang OutOfLives sau stateChangeDelay: chờ đúng sự kiện đó rồi từ chối hồi mạng
        UnityEngine.Events.UnityAction<GameState> onState = null;
        onState = state =>
        {
            if (state != GameState.OutOfLives) return;
            lm.onStateChanged.RemoveListener(onState);
            ReviveLivesPopup revive = Object.FindFirstObjectByType<ReviveLivesPopup>();
            if (revive != null && revive.IsOpen) revive.CloseImmediate();
            lm.GiveUp();
        };
        lm.onStateChanged.AddListener(onState);
        OutOfLives();
    }

    // ---------- Tài nguyên ----------

    public static void AddCoins(int amount)
    {
        if (amount > 0) CoinWallet.Add(amount);
        else if (amount < 0) CoinWallet.TrySpend(Mathf.Min(-amount, CoinWallet.Balance));
    }

    public static void SetCoins(int value) => AddCoins(Mathf.Max(0, value) - CoinWallet.Balance);

    /// <summary>Mạng trong ván (tim trên HUD).</summary>
    public static void AddLives(int amount)
    {
        if (LivesManager.Instance != null) LivesManager.Instance.AddLives(amount);
    }

    public static void FullLives()
    {
        LivesManager lives = LivesManager.Instance;
        if (lives != null) lives.AddLives(lives.MaxLives);
    }

    public static void Shield()
    {
        if (LivesManager.Instance != null) LivesManager.Instance.ActivateShield();
    }

    /// <summary>Năng lượng (tim ở Main Menu).</summary>
    public static void AddEnergy(int amount)
    {
        if (EnergyManager.Instance != null) EnergyManager.Instance.Add(amount);
    }

    public static void EnergyFull() { if (EnergyManager.Instance != null) EnergyManager.Instance.DebugFull(); }
    public static void EnergyEmpty() { if (EnergyManager.Instance != null) EnergyManager.Instance.DebugEmpty(); }
    public static void EnergySkipRefill() { if (EnergyManager.Instance != null) EnergyManager.Instance.DebugSkip(); }
    public static void EnergyInfinite(int minutes) { if (EnergyManager.Instance != null) EnergyManager.Instance.AddInfinite(minutes * 60); }

    public static int BoosterCount(BoosterType type)
    {
        BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(type) : null;
        return data != null ? BoosterInventory.Get(data) : 0;
    }

    public static void AddBooster(BoosterType type, int amount)
    {
        BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(type) : null;
        if (data != null) BoosterInventory.SetCount(data, Mathf.Max(0, BoosterInventory.Get(data) + amount));
    }

    public static void SetBooster(BoosterType type, int value)
    {
        BoosterData data = BoosterManager.Instance != null ? BoosterManager.Instance.Get(type) : null;
        if (data != null) BoosterInventory.SetCount(data, Mathf.Max(0, value));
    }

    // ---------- Khác ----------

    public static void DailyNextDay() { if (DailyRewardManager.Instance != null) DailyRewardManager.Instance.DebugNextDay(); }
    public static void DailyReset() { if (DailyRewardManager.Instance != null) DailyRewardManager.Instance.DebugReset(); }

    public static void SaveNow() => SaveSystem.Save(true);

    /// <summary>Thử mua bằng tiền thật (chưa có SDK: luôn hiện "Purchase Failed!").</summary>
    public static void TryIAP() => IAPService.Purchase("debug_test_pack");
}
