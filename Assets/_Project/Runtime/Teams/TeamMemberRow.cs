using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Teams
{
    /// <summary>1 dòng thành viên trong màn thông tin đội: hạng, avatar, tên, cấp; đội trưởng có vương miện, người chơi có viền sáng.</summary>
    public class TeamMemberRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject leaderIcon;
        [SerializeField] private GameObject localHighlight;

        public void Set(int rank, TeamMember member, Sprite avatarSprite)
        {
            if (rankText != null) rankText.SetText("{0}", rank);
            if (avatar != null && avatarSprite != null) avatar.sprite = avatarSprite;
            if (nameText != null) nameText.SetText(member.Name);
            if (levelText != null) levelText.SetText("Lv {0}", member.Level);
            if (leaderIcon != null) leaderIcon.SetActive(member.IsLeader);
            if (localHighlight != null) localHighlight.SetActive(member.IsLocal);
        }
    }
}
