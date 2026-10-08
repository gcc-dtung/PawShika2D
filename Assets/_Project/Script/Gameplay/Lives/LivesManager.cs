using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Cơ chế MẠNG thay cho cơ chế THỜI GIAN của bản gốc.
/// Mọi level có cùng số mạng (LivesEnergyConfig.maxLives). Nước đi sai -> mất 1 mạng. Hết mạng -> thua.
/// </summary>
public class LivesManager : Singleton<LivesManager>
{
    [SerializeField] private GridSpawner gridSpawner;

    [Header("Events")]
    public UnityEvent<int, int> onLivesChanged = new UnityEvent<int, int>(); // (current, max)
    public UnityEvent<int> onLifeLost = new UnityEvent<int>();                // current sau khi trừ
    public UnityEvent onOutOfLives = new UnityEvent();
    public UnityEvent<bool> onShieldChanged = new UnityEvent<bool>(); // bật / tắt khiên
    public UnityEvent onShieldBlocked = new UnityEvent();             // khiên vừa chặn 1 lần sai

    private int _currentLives;
    private bool _shield; // booster Khiên: chặn lần trừ mạng kế tiếp

    public int MaxLives => GameConfig.LivesEnergy.maxLives;
    public int CurrentLives => _currentLives;
    public bool IsOutOfLives => _currentLives <= 0;
    public bool HasShield => _shield;

    protected override void Awake()
    {
        base.Awake();
        _currentLives = MaxLives;
        if (gridSpawner == null)
        {
            gridSpawner = FindFirstObjectByType<GridSpawner>();
        }
    }

    private void OnEnable()
    {
        if (gridSpawner != null)
        {
            gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
        }
    }

    private void OnDisable()
    {
        if (gridSpawner != null)
        {
            gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
        }
    }

    private void OnGridSpawned(List<CellView> grid)
    {
        ResetLives();
    }

    /// <summary>Level mới / chơi lại: đủ mạng, khiên chưa dùng tới cũng mất.</summary>
    public void ResetLives()
    {
        SetShield(false);
        _currentLives = MaxLives;
        onLivesChanged?.Invoke(_currentLives, MaxLives);
    }

    public void LoseLife()
    {
        if (IsOutOfLives) return;

        if (_shield)
        {
            Debug.Log("[LivesManager] Khiên chặn 1 lần sai, không trừ mạng");
            SetShield(false);
            onShieldBlocked?.Invoke();
            return;
        }

        _currentLives--;
        Debug.LogWarning($"<color=red>[LivesManager] -1 mạng! Còn {_currentLives}/{MaxLives}</color>");

        onLifeLost?.Invoke(_currentLives);
        onLivesChanged?.Invoke(_currentLives, MaxLives);

        if (_currentLives <= 0)
        {
            Debug.Log("<color=red>✖ ✖ ✖ HẾT MẠNG -> GAME OVER ✖ ✖ ✖</color>");
            onOutOfLives?.Invoke();
        }
    }

    /// <summary>Bật khiên. Trả về false nếu đang có khiên hoặc đã hết mạng.</summary>
    public bool ActivateShield()
    {
        if (_shield || IsOutOfLives) return false;
        SetShield(true);
        return true;
    }

    private void SetShield(bool on)
    {
        if (_shield == on) return;
        _shield = on;
        onShieldChanged?.Invoke(on);
    }

    /// <summary>Hồi mạng (popup Hồi mạng ở giai đoạn 3: xem ads +1, coin +3).</summary>
    public void AddLives(int amount = 1)
    {
        if (amount <= 0) return;
        _currentLives = Mathf.Min(_currentLives + amount, MaxLives);
        onLivesChanged?.Invoke(_currentLives, MaxLives);
    }
}
