using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Teams
{
    /// <summary>
    /// 1 dòng bảng tin My Team. Cùng script cho 3 mẫu (TeamsPanel chọn mẫu theo loại): thông báo "X joined the team.",
    /// bong bóng tin nhắn (của mình bên phải, của người khác bên trái), thẻ xin mạng (tim + thanh x/5). Trường trống thì bỏ qua.
    /// </summary>
    public class TeamFeedRow : MonoBehaviour
    {
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private string joinedFormat = "{0} joined the team.";
        [Tooltip("Thẻ xin mạng: phần lấp đầy (anchorMax.x = tỉ lệ)")]
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private TMP_Text progressText;

        public void Set(TeamFeedItem item, Sprite avatarSprite, long now, int lifeMax)
        {
            if (avatar != null && avatarSprite != null) avatar.sprite = avatarSprite;
            if (nameText != null) nameText.SetText(item.name);
            if (timeText != null) timeText.SetText(TeamService.FormatAgo(now - item.time));
            if (messageText != null)
            {
                messageText.SetText(item.Type == TeamFeedType.Joined ? string.Format(joinedFormat, item.name) : item.text);
            }
            if (progressFill != null)
            {
                Vector2 max = progressFill.anchorMax;
                max.x = lifeMax > 0 ? Mathf.Clamp01((float)item.helped / lifeMax) : 0f;
                progressFill.anchorMax = max;
                progressFill.gameObject.SetActive(item.helped > 0);
            }
            if (progressText != null) progressText.SetText("{0}/{1}", item.helped, lifeMax);
        }
    }
}
