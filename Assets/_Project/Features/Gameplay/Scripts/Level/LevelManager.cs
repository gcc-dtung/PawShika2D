using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.DailyChallenge;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Luồng 1 level và trạng thái ván chơi:
    /// Playing -> Won (WinPopup) -> level kế
    /// Playing -> OutOfTime (MoreTimePopup) -> thêm giờ: Playing / bỏ cuộc: Lost (LosePopup) -> chơi lại.
    /// Phát LevelLoadedEvent / GameStateChangedEvent qua EventBus; ván không Playing thì khóa lưới (InputGate).
    /// Có bên xử lý luồng (GameFlowUI đặt AutoAdvance = false) thì chờ popup; không có thì tự chuyển sau autoAdvanceDelay.
    /// Căn camera theo lưới: BoardCameraFitter.
    /// Daily Challenge: LoadDaily nạp level sinh theo ngày (không thuộc database, không đổi level hiện tại đã lưu);
    /// Retry chơi lại đúng level Daily, NextLevel không làm gì (màn thắng đưa về Menu).
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        private ISaveService _save;
        private GameplaySave _data;

        /// <summary>GameBootstrap gọi trước Start: section "gameplay" (level đang chơi đã lưu).</summary>
        public void Init(ISaveService save)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
            _currentIndex = _data.currentLevel;
        }

        [SerializeField] private int currentLevel = 0;

        [SerializeField] private LevelDatabase database;
        [SerializeField] private GridSpawner gridSpawner;
        [SerializeField] private float autoAdvanceDelay = 1.5f; // chỉ dùng khi AutoAdvance (không có GameFlowUI)
        [Tooltip("Chờ hiệu ứng đặt khối / mèo rung xong rồi mới đổi trạng thái (popup hiện sau khoảng này)")]
        [SerializeField] private float stateChangeDelay = 0.8f;

        private int _currentIndex;
        private Tween _pending; // hẹn giờ đổi trạng thái / tự chuyển level
        private GameState _state = GameState.Playing;
        private bool _hasRevived; // đã thêm giờ trong lượt chơi này (lượt sau hết giờ có thể giới hạn)
        private bool _isPractice; // màn tutorial: thắng không sang WinPopup, đồng hồ không chạy
        private LevelDefinition _daily; // level Daily Challenge đang chơi (null = level thường)

        public GameState State => _state;
        public bool IsPlaying => _state == GameState.Playing;
        public bool HasRevived => _hasRevived;
        public bool IsPractice => _isPractice;
        public bool IsDaily => _daily != null;
        /// <summary>Ngày của level Daily đang chơi (số ngày từ 1/1/1970, DailyChallengeModel.DayNumber).</summary>
        public long DailyDay { get; private set; } = -1;

        /// <summary>Thắng / hết giờ thì tự sang level kế / chơi lại. GameFlowUI tắt đi để popup quyết định.</summary>
        public bool AutoAdvance { get; set; } = true;

        public int CurrentIndex => _currentIndex;
        public int LevelCount => database != null ? database.Count : 0;
        public int CurrentLevelNumber => _currentIndex + 1;
        public LevelDefinition CurrentLevel => _daily != null ? _daily : database != null ? database.Get(_currentIndex) : null;

        protected override void Awake()
        {
            base.Awake();
            if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        }

        private void Start()
        {
            if (GridManager.Instance != null) GridManager.Instance.onGameWin.AddListener(OnWin);
            if (LevelTimer.Instance != null) LevelTimer.Instance.onTimeUp.AddListener(OnTimeUp);
            if (!TutorialController.Pending) LoadLevel(_currentIndex); // tutorial lần đầu: ScreenFlow chạy tutorial thay vì nạp level
        }

        private void OnDisable()
        {
            CancelPending();
            InputGate.Unlock(this);
        }

        private void OnDestroy()
        {
            if (GridManager.Instance != null) GridManager.Instance.onGameWin.RemoveListener(OnWin);
            if (LevelTimer.Instance != null) LevelTimer.Instance.onTimeUp.RemoveListener(OnTimeUp);
        }

        public void LoadLevel(int index)
        {
            if (database == null || database.Count == 0)
            {
                Debug.LogError("[LevelManager] Chưa có LevelDatabase. Chạy menu Tools/PawBlock/Builders/Import ShikaCat Levels.");
                return;
            }

            CancelPending();
            _daily = null;
            _currentIndex = Mathf.Clamp(index, 0, database.Count - 1);
            if (_data.currentLevel != _currentIndex)
            {
                _data.currentLevel = _currentIndex;
                _save.MarkDirty();
            }

            LevelDefinition level = database.Get(_currentIndex);
            SetPractice(false);
            gridSpawner.Spawn(level);
            _hasRevived = false;
            SetState(GameState.Playing);
            EventBus<LevelLoadedEvent>.Raise(new LevelLoadedEvent(CurrentLevelNumber, level != null ? level.name : null));
            Debug.Log($"[LevelManager] Level {CurrentLevelNumber}/{database.Count}: {level.name} ({level.size}x{level.Height})");
        }

        /// <summary>
        /// Màn tutorial (TutorialController): dựng lưới như level thường nhưng không lưu level hiện tại,
        /// không phát LevelLoadedEvent (HUD level / intro / booster không đổi), thắng không sang WinPopup, đồng hồ không chạy.
        /// </summary>
        public void LoadTutorial(LevelDefinition level)
        {
            if (level == null) return;
            CancelPending();
            _daily = null;
            SetPractice(true);
            gridSpawner.Spawn(level);
            _hasRevived = false;
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Level Daily Challenge (DailyChallengeManager sinh theo ngày): như level thường nhưng không đổi level hiện tại đã lưu.
        /// LevelLoadedEvent mang IsDaily = true, Number vẫn là level thường hiện tại.
        /// </summary>
        public void LoadDaily(LevelDefinition level, long day)
        {
            if (level == null) return;
            CancelPending();
            _daily = level;
            DailyDay = day;
            SetPractice(false);
            gridSpawner.Spawn(level);
            _hasRevived = false;
            SetState(GameState.Playing);
            EventBus<LevelLoadedEvent>.Raise(new LevelLoadedEvent(CurrentLevelNumber, level != null ? level.name : null, true));
            Debug.Log($"[LevelManager] Daily {DailyChallengeModel.DateOf(day):yyyy-MM-dd}: {level.name} ({level.size}x{level.Height})");
        }

        /// <summary>Chơi lại từ đầu level đang chơi (Daily thì đúng level Daily đó).</summary>
        public void Retry()
        {
            if (_daily != null) LoadDaily(_daily, DailyDay);
            else LoadLevel(_currentIndex);
        }

        /// <summary>Vào ván level thường hiện tại (nút PLAY ở Menu), kể cả khi vừa chơi Daily.</summary>
        public void PlayCurrent() => LoadLevel(_currentIndex);

        /// <summary>Đang ở level cuối của database.</summary>
        public bool IsLastLevel => database != null && _currentIndex >= database.Count - 1;

        /// <summary>
        /// Sang level kế. Level cuối: phát AllLevelsCompletedEvent; có bên xử lý (GameFlowUI hiện "Coming soon")
        /// thì chờ bên đó gọi RestartFromFirst, không có thì quay vòng về level 1 luôn.
        /// </summary>
        public void NextLevel()
        {
            if (database == null || database.Count == 0 || IsDaily) return; // Daily: màn thắng tự đưa về Menu
            if (!IsLastLevel)
            {
                LoadLevel(_currentIndex + 1);
                return;
            }
            if (!_data.allLevelsCleared)
            {
                _data.allLevelsCleared = true;
                _save.MarkDirty();
            }
            EventBus<AllLevelsCompletedEvent>.Raise(new AllLevelsCompletedEvent(database.Count));
            if (AutoAdvance) RestartFromFirst();
        }

        /// <summary>Chơi lại từ level 1 sau khi đã qua hết level.</summary>
        public void RestartFromFirst() => LoadLevel(0);

        public void PreviousLevel() => LoadLevel(Mathf.Max(0, _currentIndex - 1));

        // ---------- Trạng thái ----------

        /// <summary>Popup mở giữa ván (xác nhận chơi lại...): khóa thao tác trên lưới.</summary>
        public void Pause()
        {
            if (_state == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (_state == GameState.Paused) SetState(GameState.Playing);
        }

        /// <summary>Popup Thêm giờ: cộng giây rồi chơi tiếp đúng lưới đang dở.</summary>
        public void Revive(float seconds)
        {
            if (_state != GameState.OutOfTime) return;
            _hasRevived = true;
            if (LevelTimer.Instance != null) LevelTimer.Instance.AddTime(seconds);
            SetState(GameState.Playing);
        }

        /// <summary>Đóng popup Thêm giờ -> thua.</summary>
        public void GiveUp()
        {
            if (_state == GameState.OutOfTime) SetState(GameState.Lost);
        }

        private void OnWin()
        {
            if (_isPractice) return; // TutorialController tự sang bước kế
            CancelPending();
            After(stateChangeDelay, () =>
            {
                SetState(GameState.Won);
                if (AutoAdvance) After(autoAdvanceDelay, NextLevel);
            });
        }

        private void OnTimeUp()
        {
            CancelPending();
            After(stateChangeDelay, () =>
            {
                SetState(GameState.OutOfTime);
                if (AutoAdvance) After(autoAdvanceDelay, Retry);
            });
        }

        private void SetState(GameState state)
        {
            if (_state == state) return;
            _state = state;
            Debug.Log($"[LevelManager] State -> {state}");
            InputGate.Set(this, state != GameState.Playing);
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(state));
            RaiseResult(state);
        }

        /// <summary>Kết quả ván cho feature khác (Profile, DailyChallenge, Economy), kèm dữ liệu để không phải hỏi lại LevelManager.</summary>
        private void RaiseResult(GameState state)
        {
            if (_isPractice || (state != GameState.Won && state != GameState.Lost)) return;
            LevelDefinition level = CurrentLevel;
            string id = level != null ? level.name : null;
            if (state == GameState.Won)
            {
                bool isHard = level != null && level.difficulty != LevelDifficulty.Normal;
                LevelStatSave stat = !IsDaily && id != null && _data != null ? _data.levelStats.Find(x => x.id == id) : null; // Daily không ghi số liệu
                int attempts = stat != null ? stat.attempts : 0;
                EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent(CurrentLevelNumber, id, IsDaily, IsDaily ? DailyDay : -1, isHard, attempts));
            }
            else
            {
                EventBus<LevelFailedEvent>.Raise(new LevelFailedEvent(CurrentLevelNumber, id, IsDaily));
            }
        }

        private void SetPractice(bool practice)
        {
            _isPractice = practice;
            if (LevelTimer.Instance != null) LevelTimer.Instance.IsPractice = practice;
        }

        /// <summary>Hẹn gọi action sau delay giây (thay hẹn cũ nếu có). Manager tắt thì hủy, action không chạy.</summary>
        private void After(float delay, System.Action action)
        {
            CancelPending();
            Tween tween = null;
            tween = DOVirtual.DelayedCall(delay, () =>
                {
                    if (_pending == tween) _pending = null;
                    action();
                })
                .SetLink(gameObject);
            _pending = tween;
        }

        private void CancelPending()
        {
            _pending?.Kill();
            _pending = null;
        }

        [ContextMenu("Load Level")]
        public void GoToLevel()
        {
            LoadLevel(currentLevel); // LoadLevel tự lưu level hiện tại
        }
    }
}
