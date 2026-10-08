using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nối âm thanh vào các sự kiện có sẵn của game (không phải sửa từng script gameplay):
/// đặt khối, sai, mở khóa, thắng / hết mạng / thua, lưới xuất hiện, booster, khiên, nhận coin.
/// Đặt cùng AudioManager. Popup tự phát âm mở / đóng (PopupView), nút UI do UIButtonSounds.
/// </summary>
public class GameAudioEvents : MonoBehaviour
{
    [Header("Tiếng mèo kèm theo (xác suất mỗi lần)")]
    [SerializeField, Range(0f, 1f)] private float catHappyChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float catSadChance = 0.8f;

    private GridManager _grid;
    private GridSpawner _spawner;
    private LivesManager _lives;
    private BoosterManager _booster;

    private void Start()
    {
        _grid = GridManager.Instance;
        _lives = LivesManager.Instance;
        _booster = BoosterManager.Instance;
        _spawner = FindFirstObjectByType<GridSpawner>();

        if (_grid != null)
        {
            _grid.onBlockPlaced.AddListener(OnBlockPlaced);
            _grid.onMistake.AddListener(OnMistake);
            _grid.onClueUnlocked.AddListener(OnClueUnlocked);
        }
        EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        if (_spawner != null) _spawner.gridCellEvent.AddListener(OnGridSpawned);
        if (_lives != null)
        {
            _lives.onShieldChanged.AddListener(OnShieldChanged);
            _lives.onShieldBlocked.AddListener(OnShieldBlocked);
        }
        if (_booster != null) _booster.onBoosterUsed.AddListener(OnBoosterUsed);
        CoinWallet.Changed += OnCoinChanged;
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
        if (_spawner != null) _spawner.gridCellEvent.RemoveListener(OnGridSpawned);
        if (_lives != null)
        {
            _lives.onShieldChanged.RemoveListener(OnShieldChanged);
            _lives.onShieldBlocked.RemoveListener(OnShieldBlocked);
        }
        if (_booster != null) _booster.onBoosterUsed.RemoveListener(OnBoosterUsed);
        CoinWallet.Changed -= OnCoinChanged;
    }

    private void OnBlockPlaced(BlockModel block)
    {
        AudioManager.Play(SoundId.BlockPlace);
        AudioManager.PlayChance(SoundId.CatHappy, catHappyChance);
    }

    private void OnMistake(Vector3 center, Vector2 size)
    {
        AudioManager.Play(SoundId.Mistake);
        AudioManager.PlayChance(SoundId.CatSad, catSadChance);
    }

    private void OnClueUnlocked(CellModel cell) => AudioManager.Play(SoundId.Unlock);

    private void OnStateChanged(GameStateChangedEvent e)
    {
        switch (e.State)
        {
            case GameState.Won: AudioManager.Play(SoundId.Win); break;
            case GameState.OutOfLives: AudioManager.Play(SoundId.OutOfLives); break;
            case GameState.Lost: AudioManager.Play(SoundId.Lose); break;
        }
    }

    // Lưới nạp lúc đang ở Menu (mở game) thì không kêu: chưa nhìn thấy
    private void OnGridSpawned(List<CellView> cells)
    {
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return;
        AudioManager.Play(SoundId.LevelStart);
    }

    private void OnShieldChanged(bool on)
    {
        if (on) AudioManager.Play(SoundId.ShieldOn);
    }

    private void OnShieldBlocked() => AudioManager.Play(SoundId.ShieldBlock);

    private void OnBoosterUsed(BoosterType type) => AudioManager.Play(SoundId.Booster);

    private void OnCoinChanged(int balance, int delta)
    {
        if (delta > 0) AudioManager.Play(SoundId.Coin);
    }
}
