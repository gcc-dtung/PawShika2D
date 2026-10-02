using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Luồng 1 level: nạp level, thắng -> level kế, hết mạng -> chơi lại.
/// Giai đoạn 3 sẽ chèn WinUI / popup Hồi mạng / LoseUI vào giữa thay cho việc tự chuyển.
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    private const string CurrentLevelKey = "PawBlock_CurrentLevel";

    [SerializeField] private LevelDatabase database;
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private float autoAdvanceDelay = 1.5f;

    [Header("Camera fit (tỉ lệ màn hình dành cho lưới)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0.3f, 1f)] private float boardHeightRatio = 0.62f;
    [SerializeField, Range(0.5f, 1f)] private float boardWidthRatio = 0.92f;
    [SerializeField, Range(-0.5f, 0.5f)] private float boardCenterOffset = 0.02f; // >0: lưới lệch lên trên
    [SerializeField] private float minOrthoSize = 5f;

    [Header("Events")]
    public UnityEvent<int> onLevelLoaded = new UnityEvent<int>(); // số level hiển thị (bắt đầu từ 1)

    private int _currentIndex;
    private Coroutine _pending;

    public int CurrentIndex => _currentIndex;
    public int CurrentLevelNumber => _currentIndex + 1;
    public LevelDefinition CurrentLevel => database != null ? database.Get(_currentIndex) : null;

    protected override void Awake()
    {
        base.Awake();
        if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        if (targetCamera == null) targetCamera = Camera.main;
        _currentIndex = PlayerPrefs.GetInt(CurrentLevelKey, 0);
    }

    private void Start()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.AddListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.AddListener(OnOutOfLives);
        LoadLevel(_currentIndex);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null) GridManager.Instance.onGameWin.RemoveListener(OnWin);
        if (LivesManager.Instance != null) LivesManager.Instance.onOutOfLives.RemoveListener(OnOutOfLives);
    }

    public void LoadLevel(int index)
    {
        if (database == null || database.Count == 0)
        {
            Debug.LogError("[LevelManager] Chưa có LevelDatabase. Chạy menu Tools/PawBlock/Import ShikaCat Levels.");
            return;
        }

        CancelPending();
        _currentIndex = Mathf.Clamp(index, 0, database.Count - 1);
        PlayerPrefs.SetInt(CurrentLevelKey, _currentIndex);
        PlayerPrefs.Save();

        LevelDefinition level = database.Get(_currentIndex);
        gridSpawner.Spawn(level);
        FitCamera(level);
        onLevelLoaded?.Invoke(CurrentLevelNumber);
        Debug.Log($"[LevelManager] Level {CurrentLevelNumber}/{database.Count}: {level.name} ({level.size}x{level.Height})");
    }

    public void Retry() => LoadLevel(_currentIndex);

    public void NextLevel()
    {
        // Hết level thì quay vòng về level 1 (tạm thời, chưa có màn "Coming soon")
        int next = _currentIndex + 1 >= database.Count ? 0 : _currentIndex + 1;
        LoadLevel(next);
    }

    public void PreviousLevel() => LoadLevel(Mathf.Max(0, _currentIndex - 1));

    private void OnWin()
    {
        CancelPending();
        _pending = StartCoroutine(After(autoAdvanceDelay, NextLevel));
    }

    private void OnOutOfLives()
    {
        CancelPending();
        _pending = StartCoroutine(After(autoAdvanceDelay, Retry));
    }

    private IEnumerator After(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        _pending = null;
        action();
    }

    private void CancelPending()
    {
        if (_pending != null)
        {
            StopCoroutine(_pending);
            _pending = null;
        }
    }

    /// <summary>Phóng camera cho lưới vừa vùng giữa HUD trên và thanh booster dưới.</summary>
    private void FitCamera(LevelDefinition level)
    {
        if (targetCamera == null || !targetCamera.orthographic || level == null) return;

        float gridW = level.size * GridSpawner.CellSpacing;
        float gridH = level.Height * GridSpawner.CellSpacing;
        float aspect = targetCamera.aspect;

        float sizeByHeight = gridH / 2f / boardHeightRatio;
        float sizeByWidth = gridW / 2f / (aspect * boardWidthRatio);
        float ortho = Mathf.Max(minOrthoSize, sizeByHeight, sizeByWidth);
        targetCamera.orthographicSize = ortho;

        // Dời camera xuống để lưới nằm hơi cao hơn giữa màn hình
        Vector3 pos = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(0f, -boardCenterOffset * 2f * ortho, pos.z);
    }
}
