using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PawBlock.Features.Profile;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Xếp hạng (LeaderboardPanel.prefab): cuộc thi tuần giả lập offline (chưa có server).
    /// Đối thủ là danh sách tên cố định, điểm sinh theo số tuần (cùng tuần thì giống nhau, sang tuần đổi);
    /// người chơi lấy điểm = cấp hiện tại của hồ sơ. Mỗi lần mở tab thì xếp lại: 3 người đầu lên bục, còn lại vào danh sách.
    /// Dòng danh sách nhân từ dòng mẫu (con tắt sẵn) lần đầu mở tab.
    /// </summary>
    public class LeaderboardPanel : MenuPanel
    {
        [SerializeField] private string[] rivalNames = { "Pumpkin", "Boo", "Casper", "Luna", "Wicky", "Batty", "Mochi", "Spooky", "Nyx", "Toffee" };
        [Tooltip("Điểm đối thủ: khoảng ngẫu nhiên theo tuần")]
        [SerializeField] private Vector2Int rivalScore = new Vector2Int(5, 80);

        [Header("View")]
        [SerializeField] private TMP_Text[] podiumNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] podiumScores = new TMP_Text[3];
        [SerializeField] private LeaderboardRow rowTemplate;
        [SerializeField] private TMP_Text timerText;

        private PlayerProfileService _profile;
        private readonly List<LeaderboardRow> _rows = new List<LeaderboardRow>();

        private struct Entry
        {
            public string Name;
            public int Score;
            public bool IsLocal;
        }

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(PlayerProfileService profile)
        {
            _profile = profile;
        }

        // Bật lên cả khi chỉ lướt qua lúc chuyển tab (Shop <-> Home) -> xếp luôn để không lộ dữ liệu mẫu
        private void OnEnable()
        {
            Refresh();
        }

        protected override void OnShown()
        {
            Refresh();
        }

        private void Refresh()
        {
            DateTime now = DateTime.UtcNow;
            int week = (int)((now - new DateTime(2024, 1, 1)).TotalDays / 7);
            var rng = new System.Random(week);

            var entries = new List<Entry>(rivalNames.Length + 1);
            foreach (string n in rivalNames) entries.Add(new Entry { Name = n, Score = rng.Next(rivalScore.x, rivalScore.y + 1) });
            PlayerProfile local = _profile != null ? _profile.Local() : null;
            entries.Add(new Entry { Name = local != null ? local.Name : "You", Score = local != null ? local.Level : 0, IsLocal = true });
            entries.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.IsLocal ? -1 : b.IsLocal ? 1 : 0);

            for (int i = 0; i < podiumNames.Length && i < entries.Count; i++)
            {
                if (podiumNames[i] != null) podiumNames[i].SetText(entries[i].Name);
                if (podiumScores[i] != null) podiumScores[i].SetText("{0}", entries[i].Score);
            }

            int listCount = Mathf.Max(0, entries.Count - podiumNames.Length);
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
                Entry e = entries[i + podiumNames.Length];
                _rows[i].Set(i + podiumNames.Length + 1, e.Name, e.Score, e.IsLocal);
            }

            if (timerText != null)
            {
                int daysLeft = 7 - (int)((now - new DateTime(2024, 1, 1)).TotalDays % 7);
                timerText.SetText("{0}d left", daysLeft);
            }
        }
    }
}
