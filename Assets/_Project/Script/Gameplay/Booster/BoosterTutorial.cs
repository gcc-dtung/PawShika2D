using UnityEngine;

/// <summary>
/// Mở khóa booster bằng tutorial (như bản gốc): booster khóa tới khi lần đầu vào level >= levelUnlock (BoosterDatabase).
/// Lúc đó, khi lưới đã hiện xong (và băng HARD đã chạy xong), màn hình tối lại, chỉ nút booster đó sáng + bàn tay chỉ vào,
/// lời nhắc lấy từ BoosterData.detail. Người chơi phải bấm nút: booster mở khóa (lưu SaveData.unlockedBoosters)
/// và lần dùng đầu tiên miễn phí (BoosterManager). Mỗi lần nạp level chỉ chạy tối đa 1 tutorial (booster mở sớm nhất trước).
/// </summary>
public class BoosterTutorial : Singleton<BoosterTutorial>
{
    [SerializeField] private BoosterTutorialView view;
    [Tooltip("Chờ thêm sau khi lưới hiện xong rồi mới tối màn hình")]
    [SerializeField] private float startDelay = 0.3f;

    private GridSpawner _spawner;
    private bool _armed;     // vừa nạp level: chờ đủ điều kiện để xét tutorial
    private float _readyTime;

    /// <summary>Đang hiện tutorial: lưới bị khóa, chỉ nút Target bấm được.</summary>
    public static bool Active { get; private set; }
    public static BoosterType Target { get; private set; }

    private void Start()
    {
        _spawner = FindFirstObjectByType<GridSpawner>();
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.AddListener(OnLevelLoaded);
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.onLevelLoaded.RemoveListener(OnLevelLoaded);
        Active = false;
    }

    private void OnLevelLoaded(int _)
    {
        if (Active) Close();
        _armed = true;
        _readyTime = -1f;
    }

    private void Update()
    {
        if (!_armed || Active) return;
        if (!CanStartNow())
        {
            _readyTime = -1f;
            return;
        }
        if (_readyTime < 0f) _readyTime = Time.time + startDelay;
        if (Time.time < _readyTime) return;

        _armed = false;
        BoosterData data = NextToUnlock();
        if (data != null) Open(data);
    }

    /// <summary>Đang chơi thật (không ở Menu, không popup, không tutorial lần đầu), lưới hiện xong, chưa đặt khối nào.</summary>
    private bool CanStartNow()
    {
        LevelManager lm = LevelManager.Instance;
        GridManager gm = GridManager.Instance;
        if (lm == null || gm == null || gm.Board == null || !lm.IsPlaying) return false;
        if (ScreenFlow.Instance != null && ScreenFlow.Instance.IsMenu) return false;
        if (TutorialController.Active || MechanicIntro.Active || DifficultyIntro.Playing || gm.IsSelecting) return false;
        if (_spawner != null && _spawner.IsIntroPlaying) return false;
        return true;
    }

    /// <summary>Booster chưa mở có levelUnlock nhỏ nhất mà level hiện tại đã đạt tới.</summary>
    private static BoosterData NextToUnlock()
    {
        BoosterManager bm = BoosterManager.Instance;
        if (bm == null || bm.Database == null || LevelManager.Instance == null) return null;
        int level = LevelManager.Instance.CurrentLevelNumber;
        BoosterData best = null;
        foreach (BoosterData d in bm.Database.boosterDatas)
        {
            if (d == null || SaveSystem.Data.IsBoosterUnlocked(d.boosterType) || level < d.levelUnlock) continue;
            if (best == null || d.levelUnlock < best.levelUnlock) best = d;
        }
        return best;
    }

    private void Open(BoosterData data)
    {
        BoosterButton button = FindButton(data.boosterType);
        if (button == null || view == null)
        {
            // Không có giao diện: mở khóa luôn để người chơi không bị kẹt booster
            Unlock(data.boosterType);
            return;
        }
        if (GridManager.Instance != null) GridManager.Instance.CancelSelection();
        Target = data.boosterType;
        Active = true;
        button.Refresh(); // Target coi như đã mở: hiện icon + số lượt
        view.Show(button, data.detail);
    }

    /// <summary>BoosterManager gọi khi người chơi bấm đúng nút: mở khóa, đóng lớp tối (booster chạy ngay sau đó).</summary>
    public void Complete()
    {
        if (!Active) return;
        BoosterType type = Target;
        Close();
        Unlock(type);
    }

    private void Close()
    {
        Active = false;
        if (view != null) view.Hide();
    }

    private static void Unlock(BoosterType type)
    {
        SaveSystem.Data.UnlockBooster(type);
        SaveSystem.MarkDirty();
        BoosterButton button = FindButton(type);
        if (button != null) button.Refresh();
    }

    private static BoosterButton FindButton(BoosterType type)
    {
        foreach (BoosterButton b in FindObjectsByType<BoosterButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.Type == type) return b;
        }
        return null;
    }
}
