using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Cơ chế MẠNG thay cho cơ chế THỜI GIAN của bản gốc.
/// Mọi level có cùng số mạng (mặc định 3). Nước đi sai -> mất 1 mạng. Hết mạng -> thua.
/// </summary>
public class LivesManager : Singleton<LivesManager>
{
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private int maxLives = 3;

    [Header("Events")]
    public UnityEvent<int, int> onLivesChanged = new UnityEvent<int, int>(); // (current, max)
    public UnityEvent<int> onLifeLost = new UnityEvent<int>();                // current sau khi trừ
    public UnityEvent onOutOfLives = new UnityEvent();

    private int _currentLives;

    public int MaxLives => maxLives;
    public int CurrentLives => _currentLives;
    public bool IsOutOfLives => _currentLives <= 0;

    protected override void Awake()
    {
        base.Awake();
        _currentLives = maxLives;
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

    public void ResetLives()
    {
        _currentLives = maxLives;
        onLivesChanged?.Invoke(_currentLives, maxLives);
    }

    public void LoseLife()
    {
        if (IsOutOfLives) return;

        _currentLives--;
        Debug.LogWarning($"<color=red>[LivesManager] -1 mạng! Còn {_currentLives}/{maxLives}</color>");

        onLifeLost?.Invoke(_currentLives);
        onLivesChanged?.Invoke(_currentLives, maxLives);

        if (_currentLives <= 0)
        {
            Debug.Log("<color=red>✖ ✖ ✖ HẾT MẠNG -> GAME OVER ✖ ✖ ✖</color>");
            onOutOfLives?.Invoke();
        }
    }

    /// <summary>Hồi mạng (popup Hồi mạng ở giai đoạn 3: xem ads +1, coin +3).</summary>
    public void AddLives(int amount = 1)
    {
        if (amount <= 0) return;
        _currentLives = Mathf.Min(_currentLives + amount, maxLives);
        onLivesChanged?.Invoke(_currentLives, maxLives);
    }
}
