using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Game
{
    /// <summary>
    /// 1 dòng bảng xếp hạng (LeaderboardPanel nhân từ dòng mẫu): hạng, avatar, tên, điểm.
    /// Dòng của người chơi bật viền xanh (highlight) như bản gốc.
    /// </summary>
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject highlight;

        public void Set(int rank, string playerName, int score, Sprite avatarSprite, bool isLocal)
        {
            rankText.SetText("{0}", rank);
            nameText.SetText(playerName);
            scoreText.SetText("{0}", score);
            if (avatar != null && avatarSprite != null) avatar.sprite = avatarSprite;
            if (highlight != null) highlight.SetActive(isLocal);
        }
    }
}
