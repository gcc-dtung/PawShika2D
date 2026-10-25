using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Teams
{
    /// <summary>1 dòng danh sách đội (tab Teams / Search): hạng, logo, tên, sức chứa "x/50", nút View; đội đang ở có viền sáng.</summary>
    public class TeamRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image logo;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button viewButton;
        [SerializeField] private GameObject mineHighlight;
        [SerializeField] private CanvasGroup group;

        private TeamInfo _team;
        private Action<TeamInfo> _onView;

        /// <summary>Cho TeamsPanel làm hiệu ứng hiện dần.</summary>
        public CanvasGroup Group => group;

        private void Awake()
        {
            if (viewButton != null) viewButton.onClick.AddListener(() => _onView?.Invoke(_team));
        }

        public void Set(int rank, TeamInfo team, Sprite logoSprite, int members, int capacity, bool mine, Action<TeamInfo> onView)
        {
            _team = team;
            _onView = onView;
            if (rankText != null) rankText.SetText("{0}", rank);
            if (logo != null && logoSprite != null) logo.sprite = logoSprite;
            if (nameText != null) nameText.SetText(team.name);
            if (capacityText != null) capacityText.SetText("{0}/{1}", members, capacity);
            if (mineHighlight != null) mineHighlight.SetActive(mine);
        }
    }
}
