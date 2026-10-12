using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyReward;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Lệnh cheat / debug dùng chung (Debug Panel trong Editor gọi; sau này có thể gắn vào panel trong game).
    /// Chỉ thao tác qua API sẵn có của các manager nên hiệu ứng, popup, lưu save chạy đúng như chơi thật.
    /// Mọi lệnh tự bỏ qua nếu manager tương ứng chưa có (vd đang không Play).
    /// </summary>
    public static class DebugCheats
    {
        private static GameServices _services;
        private static GameScene _scene;

        /// <summary>GameBootstrap gọi lúc mở game (Play mode); chưa gọi thì mọi lệnh bỏ qua.</summary>
        public static void Init(GameServices services, GameScene scene)
        {
            _services = services;
            _scene = scene;
        }

        private static ScreenFlow Screens => _scene?.Screens;

        /// <summary>LevelManager trong scene (Cheat Panel, Debug Panel đọc trạng thái ván).</summary>
        public static LevelManager Levels => _scene?.Levels;
        private static GridManager Grid => _scene?.Grid;
        private static BoosterManager Boosters => _scene?.Boosters;
        private static LevelTimer Timer => _scene?.Timer;

        /// <summary>Năng lượng trong scene (Cheat Panel, Debug Panel hiển thị).</summary>
        public static EnergyManager Energy => _scene?.Energy;

        public static int CoinBalance => _services != null ? _services.Coins.Balance : 0;
        public static int StarBalance => _services != null ? _services.Stars.Balance : 0;

        private static GameplaySave Gameplay => _services?.Save.Get<GameplaySave>(GameplaySave.Key);

        // ---------- Level ----------

        public static int LevelCount => Levels != null ? Levels.LevelCount : 0;
        public static int CurrentLevel => Levels != null ? Levels.CurrentLevelNumber : 0;
        public static bool InMenu => Screens != null && Screens.IsMenu;

        /// <summary>Nạp level số <paramref name="number"/> (bắt đầu từ 1). Đang ở Menu thì chỉ đổi level (nút PLAY hiện số mới).</summary>
        public static void GoToLevel(int number)
        {
            if (Levels == null) return;
            Levels.LoadLevel(Mathf.Clamp(number, 1, Mathf.Max(1, LevelCount)) - 1);
        }

        /// <summary>Nạp level rồi vào ván ngay (bỏ qua kiểm tra năng lượng).</summary>
        public static void PlayLevel(int number)
        {
            if (Levels == null) return;
            Levels.LoadLevel(Mathf.Clamp(number, 1, Mathf.Max(1, LevelCount)) - 1);
            if (InMenu) Screens.EnterGameNow(); // EnterGame nạp lại level hiện tại từ đầu
        }

        public static void NextLevel() => GoToLevel(CurrentLevel >= LevelCount ? 1 : CurrentLevel + 1);
        public static void PreviousLevel() => GoToLevel(Mathf.Max(1, CurrentLevel - 1));
        public static void RetryLevel() => GoToLevel(CurrentLevel);

        // ---------- Thắng / thua ----------

        /// <summary>Đặt hết khối còn lại theo lời giải -> thắng thật (lưới lóe sáng, màn thắng).</summary>
        public static void WinNow()
        {
            GridManager gm = Grid;
            if (gm == null || gm.Board == null || gm.IsGameWon) return;
            if (!gm.Board.TrySolveHint(out List<RectInt> rects) || rects == null)
            {
                Debug.LogWarning("[DebugCheats] Không tìm được lời giải.");
                return;
            }
            PlaceAll(gm, rects, int.MaxValue);
        }

        /// <summary>Đặt đúng 1 khối của lời giải (như Hint, không tốn booster).</summary>
        public static void SolveOneBlock()
        {
            GridManager gm = Grid;
            if (gm == null || gm.Board == null || gm.IsGameWon) return;
            if (gm.Board.TrySolveHint(out List<RectInt> rects) && rects != null) PlaceAll(gm, rects, 1);
        }

        // Ô số còn khóa thì chưa đặt được: lặp tới khi đặt hết (mỗi khối đặt mở khóa dần). Khối sai đè lên thì PlaceSolved tự gỡ
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

        /// <summary>Hết giờ ngay -> popup Thêm giờ.</summary>
        public static void TimeUp()
        {
            LevelTimer timer = Timer;
            if (timer == null) return;
            timer.DebugPaused = false;
            timer.ResetFreeze();
            timer.SetRemaining(0f); // frame kế đồng hồ báo hết giờ
        }

        /// <summary>Thua hẳn (bỏ qua popup Thêm giờ) -> popup Thua, trừ 1 năng lượng.</summary>
        public static void LoseNow()
        {
            LevelManager lm = Levels;
            if (lm == null || lm.State != GameState.Playing) return;
            // LevelManager đổi sang OutOfTime sau stateChangeDelay: chờ đúng sự kiện đó rồi từ chối thêm giờ
            System.Action<GameStateChangedEvent> onState = null;
            onState = e =>
            {
                if (e.State != GameState.OutOfTime) return;
                EventBus<GameStateChangedEvent>.Unsubscribe(onState);
                MoreTimePopup popup = Object.FindFirstObjectByType<MoreTimePopup>();
                if (popup != null && popup.IsOpen) popup.CloseImmediate();
                lm.GiveUp();
            };
            EventBus<GameStateChangedEvent>.Subscribe(onState);
            TimeUp();
        }

        // ---------- Tài nguyên ----------

        public static void AddCoins(int amount)
        {
            if (_services == null) return;
            if (amount > 0) _services.Coins.Add(amount);
            else if (amount < 0) _services.Coins.TrySpend(Mathf.Min(-amount, _services.Coins.Balance));
        }

        public static void SetCoins(int value) => AddCoins(Mathf.Max(0, value) - CoinBalance);

        /// <summary>Cộng / trừ sao trong ví sao (Vòng quay may mắn); trừ quá số đang có thì về 0.</summary>
        public static void AddStars(int amount)
        {
            if (_services == null) return;
            if (amount > 0) _services.Stars.Add(amount);
            else if (amount < 0) _services.Stars.TrySpend(Mathf.Min(-amount, _services.Stars.Balance));
        }

        public static void SetStars(int value) => AddStars(Mathf.Max(0, value) - StarBalance);

        // ---------- Đồng hồ trong ván ----------

        /// <summary>Chuỗi trạng thái đồng hồ cho Debug / Cheat Panel, vd "1:05 (băng 12s)".</summary>
        public static string TimerStatus
        {
            get
            {
                LevelTimer t = Timer;
                if (t == null) return "-";
                int s = Mathf.CeilToInt(t.Remaining);
                string res = $"{s / 60}:{s % 60:00}";
                if (t.IsFrozen) res += $" (băng {Mathf.CeilToInt(t.FreezeLeft)}s)";
                if (t.DebugPaused) res += " (dừng)";
                return res;
            }
        }

        /// <summary>Cộng (dương) / trừ (âm) giây trên đồng hồ trong ván. Trừ không làm hết giờ (dùng TimeUp).</summary>
        public static void AddTime(float seconds)
        {
            LevelTimer t = Timer;
            if (t == null) return;
            if (seconds >= 0f) t.AddTime(seconds);
            else t.SetRemaining(Mathf.Max(1f, t.Remaining + seconds));
        }

        /// <summary>Về lại đủ thời gian của level.</summary>
        public static void FullTime()
        {
            LevelTimer t = Timer;
            if (t != null) t.SetRemaining(t.Total);
        }

        public static bool TimerPaused => Timer != null && Timer.DebugPaused;

        /// <summary>Dừng / chạy tiếp đồng hồ (thử level không lo hết giờ).</summary>
        public static void ToggleTimerPause()
        {
            if (Timer != null) Timer.DebugPaused = !Timer.DebugPaused;
        }

        /// <summary>Bật đóng băng như booster (không tốn lượt).</summary>
        public static void Freeze()
        {
            if (Timer != null) Timer.Freeze(_scene.LivesEnergy != null ? _scene.LivesEnergy.freezeSeconds : 0f);
        }

        /// <summary>Năng lượng (tim ở Main Menu).</summary>
        public static void AddEnergy(int amount)
        {
            if (Energy != null) Energy.Add(amount);
        }

        public static void EnergyFull() { if (Energy != null) Energy.DebugFull(); }
        public static void EnergyEmpty() { if (Energy != null) Energy.DebugEmpty(); }
        public static void EnergySkipRefill() { if (Energy != null) Energy.DebugSkip(); }
        public static void EnergyInfinite(int minutes) { if (Energy != null) Energy.AddInfinite(minutes * 60); }

        public static int BoosterCount(BoosterType type)
        {
            BoosterData data = Boosters != null ? Boosters.Get(type) : null;
            return data != null && _services != null ? _services.Boosters.Get(data) : 0;
        }

        public static void AddBooster(BoosterType type, int amount)
        {
            BoosterData data = Boosters != null ? Boosters.Get(type) : null;
            if (data != null && _services != null) _services.Boosters.SetCount(data, Mathf.Max(0, _services.Boosters.Get(data) + amount));
        }

        public static void SetBooster(BoosterType type, int value)
        {
            BoosterData data = Boosters != null ? Boosters.Get(type) : null;
            if (data != null && _services != null) _services.Boosters.SetCount(data, Mathf.Max(0, value));
        }

        /// <summary>Mở khóa mọi booster, bỏ qua tutorial.</summary>
        public static void UnlockAllBoosters()
        {
            if (Gameplay == null) return;
            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType))) Gameplay.UnlockBooster(type);
            _services.Save.MarkDirty();
            RaiseLockChanged(true);
        }

        /// <summary>Khóa lại mọi booster: nạp level (>= level mở) để xem lại tutorial mở khóa.</summary>
        public static void LockAllBoosters()
        {
            if (Gameplay == null) return;
            Gameplay.unlockedBoosters.Clear();
            _services.Save.MarkDirty();
            RaiseLockChanged(false);
        }

        private static void RaiseLockChanged(bool unlocked)
        {
            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType)))
                EventBus<BoosterLockChangedEvent>.Raise(new BoosterLockChangedEvent(type, unlocked));
        }

        // ---------- Khác ----------

        public static void DailyNextDay() { if (_scene?.DailyReward != null) _scene.DailyReward.DebugNextDay(); }
        public static void DailyReset() { if (_scene?.DailyReward != null) _scene.DailyReward.DebugReset(); }

        public static void SaveNow()
        {
            if (_services == null) return;
            _services.Save.MarkDirty();
            _services.Save.SaveNow();
        }

        /// <summary>Chơi lại tutorial từ bước 1 (xong / Skip thì về level đang chơi).</summary>
        public static void PlayTutorial()
        {
            if (Screens != null) Screens.EnterTutorial();
        }

        /// <summary>Bỏ cờ đã xem popup cơ chế (Hướng / Khóa / "?"): nạp level có cơ chế là hiện lại.</summary>
        public static void ResetMechanicIntros()
        {
            if (Gameplay == null) return;
            Gameplay.seenMechanics.Clear();
            _services.Save.MarkDirty();
        }

        /// <summary>Bỏ cờ đã xem tutorial: lần mở game sau (đang ở level 1) sẽ chạy lại tutorial.</summary>
        public static void ResetTutorial()
        {
            if (Gameplay == null) return;
            Gameplay.tutorialDone = false;
            _services.Save.MarkDirty();
        }

        /// <summary>Thử mua bằng tiền thật (chưa có SDK: luôn hiện "Purchase Failed!").</summary>
        public static void TryIAP() => _services?.Iap.Purchase("debug_test_pack");
    }
}
