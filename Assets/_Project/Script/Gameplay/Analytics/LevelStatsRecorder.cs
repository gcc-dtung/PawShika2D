using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ghi số liệu chơi thật từng level vào SaveData.levelStats để chỉnh đường độ khó theo dữ liệu thật
/// (Tools → PawBlock → Level Stats → Xuất CSV, rồi nạp vào cửa sổ Level Difficulty để so với độ khó lý thuyết).
/// - 1 lượt = từ lúc nạp level (LevelLoadedEvent, không tính màn tutorial) tới Won / Lost; nạp lại giữa chừng = bỏ cuộc (quit).
///   Lượt chỉ được tính khi người chơi thật sự thao tác (đặt khối / đi sai / dùng booster): level nạp sẵn ở nền Menu không bị đếm.
/// - Thời gian chỉ tính lúc ván ở Playing (không tính popup / Menu).
/// - Đếm nước sai (GridManager.onMistake), booster dùng, lần hết mạng.
/// Khóa theo tên asset level nên đổi thứ tự level không lẫn số liệu.
/// </summary>
public class LevelStatsRecorder : MonoBehaviour
{
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
        if (GridManager.Instance != null)
        {
            GridManager.Instance.onMistake.AddListener(OnMistake);
            GridManager.Instance.onBlockPlaced.AddListener(OnBlockPlaced);
        }
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBoosterUsed.AddListener(OnBoosterUsed);
    }

    private void OnDisable()
    {
        EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
        EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        PauseClock();
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.onMistake.RemoveListener(OnMistake);
            GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
        }
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBoosterUsed.RemoveListener(OnBoosterUsed);
    }

    // App vào nền: không tính thời gian ở ngoài game
    private void OnApplicationPause(bool paused)
    {
        if (paused) PauseClock();
        else if (!_ended && LevelManager.Instance != null && LevelManager.Instance.IsPlaying && !InMenu) ResumeClock();
    }

    // ---------- Sự kiện ----------

    private void OnLevelLoaded(LevelLoadedEvent e)
    {
        CloseAttempt(); // đang dở mà nạp level khác / chơi lại = bỏ cuộc
        _levelId = e.Level != null ? e.Level.name : null;
        _levelNumber = e.Number;
        _ended = false;
        _attemptSeconds = 0f;
        _playingSince = -1f;
        if (LevelManager.Instance != null && LevelManager.Instance.IsPlaying && !InMenu) ResumeClock();
    }

    /// <summary>Thao tác đầu tiên của người chơi trong level vừa nạp: bắt đầu tính 1 lượt.</summary>
    private bool EnsureAttempt()
    {
        if (_ended || IsPractice) return false;
        if (_current != null) return true;
        if (string.IsNullOrEmpty(_levelId)) return false;
        _current = Get(_levelId, _levelNumber);
        _current.attempts++;
        SaveSystem.MarkDirty();
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
            case GameState.OutOfLives:
                _current.outOfLives++;
                SaveSystem.MarkDirty();
                break;
            case GameState.Won:
                _current.wins++;
                _current.playSeconds += _attemptSeconds;
                if (_current.bestWinSeconds <= 0f || _attemptSeconds < _current.bestWinSeconds) _current.bestWinSeconds = _attemptSeconds;
                End();
                break;
            case GameState.Lost:
                _current.losses++;
                _current.playSeconds += _attemptSeconds;
                End();
                break;
        }
    }

    private void OnMistake(Vector3 center, Vector2 size)
    {
        if (!EnsureAttempt()) return;
        _current.mistakes++;
        SaveSystem.MarkDirty();
    }

    private void OnBoosterUsed(BoosterType type)
    {
        if (!EnsureAttempt()) return;
        _current.boosters++;
        SaveSystem.MarkDirty();
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
            SaveSystem.MarkDirty();
        }
        _current = null;
    }

    private void End()
    {
        _ended = true;
        SaveSystem.MarkDirty();
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

    private static bool InMenu => ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu;
    private static bool IsPractice => LevelManager.Instance != null && LevelManager.Instance.IsPractice;

    private static LevelStatSave Get(string id, int number)
    {
        List<LevelStatSave> list = SaveSystem.Data.levelStats;
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
