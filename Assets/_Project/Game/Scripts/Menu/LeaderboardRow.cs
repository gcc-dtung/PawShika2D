using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Game
{
    /// <summary>1 dòng bảng xếp hạng tuần (LeaderboardPanel nhân từ dòng mẫu): hạng, tên, điểm; dòng của người chơi thì đổi nền.</summary>
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color localColor = new Color(0.75f, 1f, 0.6f);

        public void Set(int rank, string playerName, int score, bool isLocal)
        {
            rankText.SetText("{0}", rank);
            nameText.SetText(playerName);
            scoreText.SetText("{0}", score);
            background.color = isLocal ? localColor : normalColor;
        }
    }
}
