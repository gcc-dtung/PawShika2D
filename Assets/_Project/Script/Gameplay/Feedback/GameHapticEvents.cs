using UnityEngine;

/// <summary>
/// Nối rung điện thoại vào các sự kiện có sẵn của game (giống GameAudioEvents cho âm thanh):
/// đặt khối / mở khóa ô (nhẹ), booster / khiên đỡ (vừa), đặt sai (mạnh), thắng / hết mạng / thua (nhịp đôi).
/// Đi qua AudioManager.Vibrate nên tắt "Rung" trong Cài đặt là tắt hết.
/// Đặt cùng AudioManager / GameAudioEvents trong scene.
/// </summary>
public class GameHapticEvents : MonoBehaviour
{
    [SerializeField] private bool onBlockPlaced = true;
    [SerializeField] private bool onClueUnlocked = true;

    private GridManager _grid;
    private LivesManager _lives;
    private BoosterManager _booster;

    private void Start()
    {
        _grid = GridManager.Instance;
        _lives = LivesManager.Instance;
        _booster = BoosterManager.Instance;

        if (_grid != null)
        {
            _grid.onBlockPlaced.AddListener(OnBlockPlaced);
            _grid.onMistake.AddListener(OnMistake);
            _grid.onClueUnlocked.AddListener(OnClueUnlocked);
        }
        EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        if (_lives != null) _lives.onShieldBlocked.AddListener(OnShieldBlocked);
        if (_booster != null) _booster.onBoosterUsed.AddListener(OnBoosterUsed);
    }

    private void OnDestroy()
    {
        if (_grid != null)
        {
            _grid.onBlockPlaced.RemoveListener(OnBlockPlaced);
            _grid.onMistake.RemoveListener(OnMistake);
            _grid.onClueUnlocked.RemoveListener(OnClueUnlocked);
        }
        EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        if (_lives != null) _lives.onShieldBlocked.RemoveListener(OnShieldBlocked);
        if (_booster != null) _booster.onBoosterUsed.RemoveListener(OnBoosterUsed);
    }

    private void OnBlockPlaced(BlockModel block)
    {
        if (onBlockPlaced) AudioManager.Vibrate(HapticType.Light);
    }

    private void OnMistake(Vector3 center, Vector2 size) => AudioManager.Vibrate(HapticType.Heavy);

    private void OnClueUnlocked(CellModel cell)
    {
        if (onClueUnlocked) AudioManager.Vibrate(HapticType.Light);
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        switch (e.State)
        {
            case GameState.Won: AudioManager.Vibrate(HapticType.Success); break;
            case GameState.OutOfLives:
            case GameState.Lost: AudioManager.Vibrate(HapticType.Failure); break;
        }
    }

    private void OnShieldBlocked() => AudioManager.Vibrate(HapticType.Medium);

    private void OnBoosterUsed(BoosterType type) => AudioManager.Vibrate(HapticType.Medium);
}
