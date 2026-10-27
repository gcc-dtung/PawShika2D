using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Teams
{
    /// <summary>
    /// 1 dòng thành viên trong màn thông tin đội: hạng (top 3 là huy chương vàng / bạc / đồng), avatar, tên,
    /// dòng phụ "Leader" + vương miện hoặc chấm xanh + "1d ago", ô "Level"; người chơi có viền sáng.
    /// Bấm vào dòng: báo Clicked (TeamsPanel mở menu Xem hồ sơ / Xoá thành viên dưới avatar).
    /// </summary>
    public class TeamMemberRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image medal;
        [Tooltip("Huy chương hạng 1, 2, 3")]
        [SerializeField] private Sprite[] medalSprites = new Sprite[3];
        [Tooltip("Số trên vòng huy chương (top 3); hạng khác dùng rankText")]
        [SerializeField] private TMP_Text medalText;
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject leaderLine;
        [SerializeField] private GameObject seenLine;
        [SerializeField] private TMP_Text seenText;
        [SerializeField] private GameObject localHighlight;
        [SerializeField] private Button button;

        public event Action<TeamMemberRow> Clicked;

        public TeamMember Member { get; private set; }

        /// <summary>Avatar: menu thành viên mở ngay dưới, mũi nhọn chỉ vào nó.</summary>
        public RectTransform AvatarRect => avatar != null ? avatar.rectTransform : (RectTransform)transform;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Set(int rank, TeamMember member, Sprite avatarSprite)
        {
            Member = member;
            bool top = rank >= 1 && rank <= 3 && medalSprites != null && medalSprites.Length >= rank && medalSprites[rank - 1] != null;
            if (medal != null)
            {
                medal.gameObject.SetActive(top);
                if (top) medal.sprite = medalSprites[rank - 1];
            }
            if (medalText != null) medalText.SetText("{0}", rank);
            if (rankText != null)
            {
                rankText.gameObject.SetActive(!top);
                rankText.SetText("{0}", rank);
            }
            if (avatar != null && avatarSprite != null) avatar.sprite = avatarSprite;
            if (nameText != null) nameText.SetText(member.Name);
            if (levelText != null) levelText.SetText("{0}", member.Level);
            if (leaderLine != null) leaderLine.SetActive(member.IsLeader);
            if (seenLine != null) seenLine.SetActive(!member.IsLeader);
            if (seenText != null) seenText.SetText(member.IsLocal || member.LastSeenDays == 0 ? "Online" : member.LastSeenDays + "d ago");
            if (localHighlight != null) localHighlight.SetActive(member.IsLocal);
        }
    }
}
