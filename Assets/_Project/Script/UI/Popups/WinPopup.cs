using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Thắng level: không có sao (cơ chế mạng), thưởng coin; xem quảng cáo để nhân đôi.</summary>
public class WinPopup : PopupView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button doubleButton;
    [SerializeField] private int coinReward = 10; // gamePlay_CoinWinLevelNormal trong RemoteConfig gốc

    private bool _claimed;

    protected override void Awake()
    {
        base.Awake();
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (doubleButton != null) doubleButton.onClick.AddListener(OnDouble);
    }

    protected override void OnOpen()
    {
        _claimed = false;
        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : 0;
        if (titleText != null) titleText.SetText($"LEVEL {level}");
        if (rewardText != null) rewardText.SetText($"+{coinReward}");
        SetButtons(true);
    }

    private void OnNext() => Claim(coinReward);

    private void OnDouble()
    {
        SetButtons(false);
        RewardedAds.Show("win_x2", ok =>
        {
            if (ok) Claim(coinReward * 2);
            else SetButtons(true);
        });
    }

    private void Claim(int coins)
    {
        if (_claimed) return;
        _claimed = true;
        CoinWallet.Add(coins);
        Close();
        if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
    }

    private void SetButtons(bool interactable)
    {
        if (nextButton != null) nextButton.interactable = interactable;
        if (doubleButton != null) doubleButton.interactable = interactable;
    }
}
