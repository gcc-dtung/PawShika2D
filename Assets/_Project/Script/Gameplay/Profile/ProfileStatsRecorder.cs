using UnityEngine;

/// <summary>
/// Ghi chỉ số hồ sơ (SaveData.profile) theo sự kiện có sẵn, đặt trên Manager:
/// thắng ngay lượt đầu (theo SaveData.levelStats của LevelStatsRecorder), level đã qua, thắng Hard / Very Hard,
/// chuỗi thắng (thua hẳn thì về 0), coin nhận được (CoinWallet.Changed), booster đã dùng.
/// Màn tutorial không tính.
/// </summary>
public class ProfileStatsRecorder : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        CoinWallet.Changed += OnCoinChanged;
    }

    private void Start()
    {
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBoosterUsed.AddListener(OnBoosterUsed);
    }

    private void OnDisable()
    {
        EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
        CoinWallet.Changed -= OnCoinChanged;
    }

    private void OnDestroy()
    {
        if (BoosterManager.Instance != null) BoosterManager.Instance.onBoosterUsed.RemoveListener(OnBoosterUsed);
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        LevelManager lm = LevelManager.Instance;
        if (lm == null || lm.IsPractice) return;
        ProfileSave p = PlayerProfileService.Save;

        if (e.State == GameState.Won)
        {
            LevelDefinition level = lm.CurrentLevel;
            if (level != null)
            {
                LevelStatSave stat = SaveSystem.Data.levelStats.Find(s => s.id == level.name);
                if (stat != null && stat.attempts == 1) p.firstTryWins++;
                if (level.difficulty != LevelDifficulty.Normal) p.hardWins++;
            }
            if (!lm.IsDaily) p.levelsCleared = Mathf.Max(p.levelsCleared, lm.CurrentLevelNumber); // Daily: CurrentLevelNumber là level thường chưa thắng
            p.winStreak++;
            p.bestWinStreak = Mathf.Max(p.bestWinStreak, p.winStreak);
        }
        else if (e.State == GameState.Lost)
        {
            p.winStreak = 0;
        }
        else return;

        SaveSystem.MarkDirty();
        PlayerProfileService.NotifyStatsChanged();
    }

    private void OnCoinChanged(int balance, int delta)
    {
        if (delta <= 0) return;
        PlayerProfileService.Save.coinsEarned += delta;
        SaveSystem.MarkDirty();
        PlayerProfileService.NotifyStatsChanged();
    }

    private void OnBoosterUsed(BoosterType type)
    {
        PlayerProfileService.Save.boostersUsed++;
        SaveSystem.MarkDirty();
        PlayerProfileService.NotifyStatsChanged();
    }
}
