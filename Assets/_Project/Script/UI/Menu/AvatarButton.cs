using UnityEngine;
using UnityEngine.UI;

/// <summary>Avatar góc trên trái Main Menu: hiện avatar đang chọn, bấm mở popup hồ sơ của mình.</summary>
[RequireComponent(typeof(Button))]
public class AvatarButton : MonoBehaviour
{
    [SerializeField] private AvatarDatabase avatars;
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameFlowUI gameFlow;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        PlayerProfileService.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => PlayerProfileService.Changed -= Refresh;

    private void OnClick()
    {
        if (gameFlow != null) gameFlow.OpenProfile();
    }

    private void Refresh()
    {
        if (avatarImage != null && avatars != null) avatarImage.sprite = avatars.Get(PlayerProfileService.Save.avatar);
    }
}
