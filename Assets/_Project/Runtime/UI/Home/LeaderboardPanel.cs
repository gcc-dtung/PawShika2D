using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Foundation;
using PawBlock.Features.Profile;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Xếp hạng (LeaderboardPanel.prefab): bảng giả lập offline (chưa có server), 3 tab con như bản gốc.
    /// - Weekly: cuộc thi tuần – 3 người đầu lên bục (avatar, ruy băng hạng, rương), còn lại vào danh sách; đồng hồ đếm tới hết tuần (thứ Hai 00:00 UTC).
    ///   Điểm người chơi = số màn thắng trong tuần, điểm bot theo luật bám đuổi (<see cref="WeeklyContest"/>, <see cref="WeeklyBots"/>).
    /// - Players: toàn thời gian – không có bục, cả danh sách từ hạng 1.
    /// - Teams: chưa có tính năng đội, chỉ hiện màn khóa.
    /// Đối thủ là tên cố định; Players: điểm cố định, người chơi = cấp hiện tại.
    /// Dòng danh sách nhân từ dòng mẫu (con tắt sẵn). Xếp lại khi bật panel (kể cả lúc chỉ lướt qua khi chuyển tab) và khi đổi tab con.
    /// </summary>
    public class LeaderboardPanel : MenuPanel
    {
        private enum Board { Weekly, Players, Teams }

        [SerializeField] private string[] rivalNames = { "Pumpkin", "Boo", "Casper", "Luna", "Wicky", "Batty", "Mochi", "Spooky", "Nyx", "Toffee" };
        [Tooltip("Luật bot cuộc thi tuần (bám đuổi người chơi)")]
        [SerializeField] private WeeklyBots.Settings weeklyBots = WeeklyBots.Settings.Default;
        [Tooltip("Điểm đối thủ toàn thời gian")]
        [SerializeField] private Vector2Int allTimeScore = new Vector2Int(30, 400);
        [SerializeField] private AvatarDatabase avatars;

        [Header("Tab con (Weekly, Players, Teams)")]
        [SerializeField] private Button[] tabButtons = new Button[3];
        [SerializeField] private GameObject[] tabActive = new GameObject[3];
        [SerializeField] private GameObject weeklyCard;
        [SerializeField] private RectTransform listArea;
        [Tooltip("Mép trên vùng danh sách (offsetMax.y) khi có bục / khi không có bục")]
        [SerializeField] private float listTopWeekly = -1040f;
        [SerializeField] private float listTopPlayers = -360f;
        [SerializeField] private GameObject teamsLocked;

        [Header("View")]
        [SerializeField] private TMP_Text[] podiumNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] podiumScores = new TMP_Text[3];
        [SerializeField] private Image[] podiumAvatars = new Image[3];
        [SerializeField] private LeaderboardRow rowTemplate;
        [SerializeField] private TMP_Text timerText;

        private PlayerProfileService _profile;
        private WeeklyContest _contest;
        private readonly List<LeaderboardRow> _rows = new List<LeaderboardRow>();
        private Board _board = Board.Weekly;

        private struct Entry
        {
            public string Name;
            public int Score;
            public int Avatar;
            public bool IsLocal;
        }

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(PlayerProfileService profile, ISaveService save, IClock clock)
        {
            _profile = profile;
            _contest?.Dispose();
            _contest = new WeeklyContest(save, clock, weeklyBots);
        }

        private void OnDestroy()
        {
            _contest?.Dispose();
            _contest = null;
        }

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Board b = (Board)i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => Show(b));
            }
        }

        private void OnEnable()
        {
            Show(_board);
        }

        protected override void OnShown()
        {
            Show(_board);
        }

        private void Show(Board board)
        {
            _board = board;
            for (int i = 0; i < tabActive.Length; i++)
            {
                if (tabActive[i] != null) tabActive[i].SetActive(i == (int)board);
            }
            bool teams = board == Board.Teams;
            if (teamsLocked != null) teamsLocked.SetActive(teams);
            if (weeklyCard != null) weeklyCard.SetActive(board == Board.Weekly);
            if (listArea != null)
            {
                listArea.gameObject.SetActive(!teams);
                Vector2 max = listArea.offsetMax;
                max.y = board == Board.Weekly ? listTopWeekly : listTopPlayers;
                listArea.offsetMax = max;
            }
            if (!teams) Refresh(board == Board.Weekly);
            RefreshTimer();
        }

        private void Refresh(bool weekly)
        {
            List<Entry> entries = BuildEntries(weekly);
            int podium = weekly ? podiumNames.Length : 0;

            for (int i = 0; i < podium && i < entries.Count; i++)
            {
                if (podiumNames[i] != null) podiumNames[i].SetText(entries[i].Name);
                if (podiumScores[i] != null) podiumScores[i].SetText("{0}", entries[i].Score);
                Sprite s = AvatarSprite(entries[i].Avatar);
                if (podiumAvatars[i] != null && s != null) podiumAvatars[i].sprite = s;
            }

            int listCount = Mathf.Max(0, entries.Count - podium);
            while (_rows.Count < listCount)
            {
                LeaderboardRow row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < listCount;
                _rows[i].gameObject.SetActive(used);
                if (!used) continue;
                Entry e = entries[i + podium];
                _rows[i].Set(i + podium + 1, e.Name, e.Score, AvatarSprite(e.Avatar), e.IsLocal);
            }
        }

        private List<Entry> BuildEntries(bool weekly)
        {
            var rng = new System.Random(weekly && _contest != null ? _contest.Week : 7919);
            int avatarCount = avatars != null ? avatars.Count : 0;
            int[] weeklyScores = weekly && _contest != null ? _contest.RivalScores(rivalNames.Length) : null;

            var entries = new List<Entry>(rivalNames.Length + 1);
            for (int i = 0; i < rivalNames.Length; i++)
            {
                int allTime = rng.Next(allTimeScore.x, allTimeScore.y + 1);
                entries.Add(new Entry
                {
                    Name = rivalNames[i],
                    Score = weeklyScores != null ? weeklyScores[i] : allTime,
                    Avatar = avatarCount > 0 ? rng.Next(avatarCount) : 0,
                });
            }
            PlayerProfile local = _profile != null ? _profile.Local() : null;
            int localScore = weekly ? (_contest != null ? _contest.PlayerScore : 0) : (local != null ? local.Level : 0);
            entries.Add(new Entry
            {
                Name = local != null ? local.Name : "You",
                Score = localScore,
                Avatar = local != null ? local.AvatarId : 0,
                IsLocal = true,
            });
            entries.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.IsLocal ? -1 : b.IsLocal ? 1 : 0);
            return entries;
        }

        private void RefreshTimer()
        {
            if (timerText == null || _contest == null) return;
            TimeSpan left = _contest.TimeLeft;
            timerText.SetText("{0}d {1}h", left.Days, left.Hours);
        }

        private Sprite AvatarSprite(int id) => avatars != null ? avatars.Get(id) : null;
    }
}
