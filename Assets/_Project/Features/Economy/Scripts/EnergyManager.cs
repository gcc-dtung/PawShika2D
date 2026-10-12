using System;
using UnityEngine;
using UnityEngine.Events;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Game;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Năng lượng ngoài màn chơi (phương án B, Giai đoạn 5), icon cá thay tim để không lẫn với mạng trong màn.
    /// Thua 1 level (state Lost) tốn 1; tối đa 5; hồi 1 sau 30 phút (gamePlay_TimeRefillHeart = 1800 của bản gốc).
    /// Lưu trong SaveData.energy: số năng lượng, mốc bắt đầu hồi, hạn vô hạn (unix giây).
    /// </summary>
    public class EnergyManager : MonoBehaviour, IEnergyService
    {
        private ISaveService _save;
        private EnergySave _energy;
        private IClock _clock;

        /// <summary>GameBootstrap gọi trước Awake / Start: section "economy" (năng lượng).</summary>
        public void Init(ISaveService save, IClock clock)
        {
            _save = save;
            _clock = clock;
            _energy = save.Get<EconomySave>(EconomySave.Key).energy;
            _model = null; // đọc lại theo save vừa nhận
        }

        [Header("Events")]
        public UnityEvent<int, int> onEnergyChanged = new UnityEvent<int, int>(); // (hiện tại, tối đa)
        public UnityEvent onTick = new UnityEvent();                              // mỗi giây, để UI cập nhật đồng hồ

        private static LivesEnergyConfig Config => GameConfig.LivesEnergy;

        private EnergyModel _model;
        private float _tickTimer;

        public int Current => Model.Energy;
        public int Max => Model.Max;
        public bool IsFull => Model.IsFull;
        public bool IsInfinite => Model.IsInfinite(Now);
        public bool CanPlay => Model.CanPlay(Now);
        public int SecondsToNext => Model.SecondsToNext(Now);
        public int InfiniteSecondsLeft => (int)Math.Max(0, Model.InfiniteUntil - Now);

        private long Now => _clock != null ? _clock.UtcNowSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // giờ server nếu có mạng, chặn chỉnh giờ máy

        private EnergyModel Model
        {
            get
            {
                if (_model == null) Load();
                return _model;
            }
        }

        private void Awake()
        {
            if (_model == null && _energy != null) Load();
        }

        private void Start()
        {
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            if (_clock != null) _clock.Synced += RefreshNow;
            RefreshNow();
        }

        private void OnDestroy()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            if (_clock != null) _clock.Synced -= RefreshNow;
        }

        private void Update()
        {
            _tickTimer += Time.unscaledDeltaTime;
            if (_tickTimer < 1f) return;
            _tickTimer = 0f;
            RefreshNow();
            onTick?.Invoke();
        }

        // Quay lại game sau khi ẩn: tính bù ngay
        private void OnApplicationFocus(bool focus)
        {
            if (focus) RefreshNow();
        }

        // ---------- API ----------

        /// <summary>Cộng năng lượng (xem quảng cáo, mua).</summary>
        public void Add(int amount)
        {
            Model.Add(amount, Now);
            SaveAndNotify();
        }

        public void RefillFull() => Add(Max);

        /// <summary>Phần thưởng "năng lượng vô hạn" (Daily Challenge – Giai đoạn 6).</summary>
        public void AddInfinite(int seconds)
        {
            Model.AddInfinite(seconds, Now);
            SaveAndNotify();
        }

        /// <summary>Trừ 1 năng lượng (đang vô hạn thì không trừ). Thua level, bỏ dở level (QuitLevelPopup).</summary>
        public void ConsumeOne(string reason)
        {
            if (Model.TryConsume(Now)) Debug.Log($"[EnergyManager] {reason} -> -1 năng lượng, còn {Current}/{Max}");
            SaveAndNotify();
        }

        // ---------- Nội bộ ----------

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.State == GameState.Lost) ConsumeOne("Thua level");
        }

        private void RefreshNow()
        {
            if (Model.Refresh(Now)) SaveAndNotify();
        }

        private void Load()
        {
            EnergySave save = _energy ?? new EnergySave(); // chưa Init (lỗi nối): dùng tạm mặc định, không ghi
            _model = new EnergyModel(
                Config.maxEnergy, Config.refillSeconds,
                save.energy < 0 ? Config.maxEnergy : save.energy, // -1: lần đầu chơi, đầy
                save.refillStart,
                save.infiniteUntil);
            _model.Refresh(Now);
        }

        private void SaveAndNotify()
        {
            if (_energy == null) return;
            EnergySave save = _energy;
            save.energy = _model.Energy;
            save.refillStart = _model.RefillStart;
            save.infiniteUntil = _model.InfiniteUntil;
            _save.MarkDirty();
            onEnergyChanged?.Invoke(_model.Energy, _model.Max);
            onTick?.Invoke();
        }

        /// <summary>"mm:ss" hoặc "h:mm:ss".</summary>
        public static string FormatTime(int seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";
        }

        // ---------- Thử nghiệm (menu chuột phải trên component) ----------

        [ContextMenu("Debug/Set 0")]
        public void DebugEmpty() { Model.DebugSet(0, Now); SaveAndNotify(); }

        [ContextMenu("Debug/Set Full")]
        public void DebugFull() { Model.DebugSet(Max, Now); SaveAndNotify(); }

        [ContextMenu("Debug/Skip 1 refill")]
        public void DebugSkip() { Model.DebugSkip(Config.refillSeconds); RefreshNow(); onTick?.Invoke(); }

        [ContextMenu("Debug/Infinite 5 min")]
        public void DebugInfinite() => AddInfinite(300);
    }
}
