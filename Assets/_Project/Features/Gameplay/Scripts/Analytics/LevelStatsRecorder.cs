using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Ghi số liệu chơi thật từng level vào GameplaySave.levelStats để chỉnh đường độ khó theo dữ liệu thật
    /// (Tools → PawBlock → Level Stats → Xuất CSV, rồi nạp vào cửa sổ Level Difficulty để so với độ khó lý thuyết).
    /// - 1 lượt = từ lúc nạp level (LevelLoadedEvent, không tính màn tutorial) tới Won / Lost; nạp lại giữa chừng = bỏ cuộc (quit).
    ///   Lượt chỉ được tính khi người chơi thật sự thao tác (đặt / gỡ khối / dùng booster): level nạp sẵn ở nền Menu không bị đếm.
    /// - Thời gian chỉ tính lúc ván ở Playing (không tính popup / Menu).
    /// - Đếm nước sai (khối bị gỡ: GridManager.onBlockRemoved), booster dùng, lần hết giờ.
    /// Khóa theo tên asset level nên đổi thứ tự level không lẫn số liệu.
    /// </summary>
    public class LevelStatsRecorder : MonoBehaviour
    {
        private LevelStars _levelStars;
        private BoosterManager _boosterManager;
        private GridManager _grid;
        private LevelManager _levels;
        private IScreenNavigator _screens;
        private ISaveService _save;
        private GameplaySave _data;

        /// <summary>GameBootstrap gọi trước Start: section "gameplay".</summary>
        public void Init(ISaveService save, IScreenNavigator screens, LevelManager levels, GridManager grid, BoosterManager boosterManager, LevelStars levelStars)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
            _screens = screens;
            _levels = levels;
            _grid = grid;
            _boosterManager = boosterManager;
            _levelStars = levelStars;
        }

        private LevelStatSave _current;  // lượt đang chơi, null nếu không có
        private string _levelId;         // level vừa nạp (chưa thành lượt cho tới khi người chơi thao tác)
        private int _levelNumber;
        private bool _ended;             // lượt đã kết thúc (Won / Lost)
        private float _playingSince = -1f;
        private float _attemptSeconds;

        private void OnEnable()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        }

        private void Start()
        {
            if (_grid != null)
            {
                _grid.onBlockRemoved.AddListener(OnBlockRemoved);
                _grid.onBlockPlaced.AddListener(OnBlockPlaced);
            }
            if (_boosterManager != null) _boosterManager.onBoosterUsed.AddListener(OnBoosterUsed);
        }

        private void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            PauseClock();
        }

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.onBlockRemoved.RemoveListener(OnBlockRemoved);
                _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
            }
            if (_boosterManager != null) _boosterManager.onBoosterUsed.RemoveListener(OnBoosterUsed);
        }

        // App vào nền: không tính thời gian ở ngoài game
        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseClock();
            else if (!_ended && _levels != null && _levels.IsPlaying && !InMenu) ResumeClock();
        }

        // ---------- Sự kiện ----------

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            CloseAttempt(); // đang dở mà nạp level khác / chơi lại = bỏ cuộc
            _levelId = !e.IsDaily ? e.LevelId : null; // Daily (sinh theo ngày) không ghi số liệu level
            _levelNumber = e.Number;
            _ended = false;
            _attemptSeconds = 0f;
            _playingSince = -1f;
            if (_levels != null && _levels.IsPlaying && !InMenu) ResumeClock();
        }

        /// <summary>Thao tác đầu tiên của người chơi trong level vừa nạp: bắt đầu tính 1 lượt.</summary>
        private bool EnsureAttempt()
        {
            if (_ended || IsPractice) return false;
            if (_current != null) return true;
            if (string.IsNullOrEmpty(_levelId)) return false;
            _current = Get(_levelId, _levelNumber);
            _current.attempts++;
            _save.MarkDirty();
            return true;
        }

        private void OnBlockPlaced(BlockModel _) => EnsureAttempt();

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (_ended || IsPractice) return;
            if (e.State == GameState.Playing && !InMenu) ResumeClock();
            else PauseClock();
            if (_current == null) return;

            switch (e.State)
            {
                case GameState.OutOfTime:
                    _current.outOfTime++;
                    _save.MarkDirty();
                    break;
                case GameState.Won:
                    _current.wins++;
                    _current.playSeconds += _attemptSeconds;
                    if (_current.bestWinSeconds <= 0f || _attemptSeconds < _current.bestWinSeconds) _current.bestWinSeconds = _attemptSeconds;
                    if (_levelStars != null) _current.bestStars = Mathf.Max(_current.bestStars, _levelStars.Stars);
                    End();
                    break;
                case GameState.Lost:
                    _current.losses++;
                    _current.playSeconds += _attemptSeconds;
                    End();
                    break;
            }
        }

        private void OnBlockRemoved(BlockModel _)
        {
            if (!EnsureAttempt()) return;
            _current.mistakes++;
            _save.MarkDirty();
        }

        private void OnBoosterUsed(BoosterType type)
        {
            if (!EnsureAttempt()) return;
            _current.boosters++;
            _save.MarkDirty();
        }

        // ---------- Lượt chơi ----------

        private void CloseAttempt()
        {
            PauseClock();
            if (_current == null) return;
            if (!_ended)
            {
                _current.quits++;
                _current.playSeconds += _attemptSeconds;
                _save.MarkDirty();
            }
            _current = null;
        }

        private void End()
        {
            _ended = true;
            _save.MarkDirty();
        }

        private void ResumeClock()
        {
            if (_playingSince < 0f) _playingSince = Time.realtimeSinceStartup;
        }

        private void PauseClock()
        {
            if (_playingSince < 0f) return;
            _attemptSeconds += Time.realtimeSinceStartup - _playingSince;
            _playingSince = -1f;
        }

        private bool InMenu => _screens != null && _screens.IsMenu;
        private bool IsPractice => _levels != null && _levels.IsPractice;

        private LevelStatSave Get(string id, int number)
        {
            List<LevelStatSave> list = _data.levelStats;
            LevelStatSave s = list.Find(x => x.id == id);
            if (s == null)
            {
                s = new LevelStatSave { id = id };
                list.Add(s);
            }
            s.level = number; // số thứ tự hiện tại (có thể đổi khi xếp lại level)
            return s;
        }
    }
}
