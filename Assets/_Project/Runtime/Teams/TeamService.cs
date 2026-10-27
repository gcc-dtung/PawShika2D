using System;
using System.Collections.Generic;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Teams
{
    /// <summary>
    /// Đội (giống guild), giả lập offline: danh sách đội giả sinh cố định theo seed (TeamsDatabase), tìm theo tên,
    /// tạo đội (trừ coin), vào / rời đội; bảng tin "My Team" (ai vào đội, tin nhắn, xin mạng) lưu section "teams".
    /// Xin mạng: mỗi lifeRequestCooldownHours 1 lần, đồng đội giả giúp 1 mạng mỗi lifeHelpMinutes (tối đa lifeRequestMax),
    /// mạng nhận được cộng qua grantLives (EnergyManager.Add). Đội tự tạo chỉ có người chơi nên không ai giúp.
    /// Chỉ biết contract (ví coin, đồng hồ) và hàm lấy cấp / tên / avatar người chơi; GameBootstrap tạo và truyền vào.
    /// </summary>
    public sealed class TeamService
    {
        public const string CreatedId = "my";
        public const int NameMinLength = 3;
        public const int NameMaxLength = 16;

        private readonly ISaveService _save;
        private readonly TeamSave _data;
        private readonly ICurrencyWallet _coins;
        private readonly Func<int> _playerLevel;
        private readonly Func<(string name, int avatar)> _player;
        private readonly IClock _clock;
        private readonly Action<int> _grantLives;
        private readonly TeamsDatabase _db;
        private readonly List<TeamInfo> _fakes = new List<TeamInfo>();

        /// <summary>Vào / rời / tạo đội.</summary>
        public event Action Changed;

        /// <summary>Bảng tin đổi (tin nhắn, xin mạng, được giúp).</summary>
        public event Action FeedChanged;

        public TeamService(ISaveService save, ICurrencyWallet coins, Func<int> playerLevel, TeamsDatabase db,
            IClock clock = null, Func<(string name, int avatar)> player = null, Action<int> grantLives = null)
        {
            _save = save;
            _data = save.Get<TeamSave>(TeamSave.Key);
            if (_data.feed == null) _data.feed = new List<TeamFeedItem>();
            _coins = coins;
            _playerLevel = playerLevel;
            _db = db;
            _clock = clock;
            _player = player;
            _grantLives = grantLives;
            BuildFakes();
        }

        public TeamsDatabase Database => _db;
        public int Capacity => _db.capacity;
        public int CreateCost => _db.createCost;
        public int PlayerLevel => _playerLevel != null ? _playerLevel() : 1;
        public long Now => _clock != null ? _clock.UtcNowSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Đội đang ở (null nếu chưa vào).</summary>
        public TeamInfo MyTeam => string.IsNullOrEmpty(_data.joinedId) ? null : Find(_data.joinedId);

        public bool IsMine(TeamInfo team) => team != null && team.id == _data.joinedId;

        /// <summary>Số thành viên hiển thị (tính cả người chơi nếu đang ở đội này).</summary>
        public int MembersOf(TeamInfo team) => team == null ? 0 : team.members + (IsMine(team) ? 1 : 0);

        /// <summary>Điểm đội hiển thị (cộng điểm người chơi nếu đang ở đội này).</summary>
        public int ScoreOf(TeamInfo team) => team == null ? 0 : team.score + (IsMine(team) ? PlayerLevel : 0);

        /// <summary>Hạng đội theo điểm (1 = cao nhất).</summary>
        public int RankOf(TeamInfo team)
        {
            if (team == null) return 0;
            int score = ScoreOf(team), rank = 1;
            foreach (TeamInfo t in All())
            {
                if (t != team && ScoreOf(t) > score) rank++;
            }
            return rank;
        }

        /// <summary>Danh sách tab "Teams": đội mạnh lên trên.</summary>
        public List<TeamInfo> Top(int count)
        {
            List<TeamInfo> all = All();
            all.Sort((a, b) => ScoreOf(b).CompareTo(ScoreOf(a)));
            if (all.Count > count) all.RemoveRange(count, all.Count - count);
            return all;
        }

        /// <summary>Đội có tên chứa <paramref name="query"/> (không phân biệt hoa thường), đội mạnh lên trên.</summary>
        public List<TeamInfo> Search(string query, int max = 20)
        {
            var result = new List<TeamInfo>();
            string q = (query ?? "").Trim();
            if (q.Length == 0) return result;
            foreach (TeamInfo t in All())
            {
                if (t.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) result.Add(t);
            }
            result.Sort((a, b) => ScoreOf(b).CompareTo(ScoreOf(a)));
            if (result.Count > max) result.RemoveRange(max, result.Count - max);
            return result;
        }

        public TeamJoinResult CanJoin(TeamInfo team)
        {
            if (team == null) return TeamJoinResult.NotFound;
            if (MyTeam != null) return TeamJoinResult.AlreadyInTeam;
            if (MembersOf(team) >= Capacity) return TeamJoinResult.Full;
            if (PlayerLevel < team.requiredLevel) return TeamJoinResult.LevelTooLow;
            return TeamJoinResult.Ok;
        }

        public TeamJoinResult Join(string teamId)
        {
            TeamInfo team = Find(teamId);
            TeamJoinResult r = CanJoin(team);
            if (r != TeamJoinResult.Ok) return r;
            _data.joinedId = teamId;
            SeedFeed(team);
            _save.MarkDirty();
            Changed?.Invoke();
            FeedChanged?.Invoke();
            return r;
        }

        /// <summary>Rời đội. Đội tự tạo chỉ có người chơi nên rời là giải tán.</summary>
        public void Leave()
        {
            if (string.IsNullOrEmpty(_data.joinedId)) return;
            if (_data.joinedId == CreatedId) _data.created = new TeamInfo();
            _data.joinedId = "";
            _data.feed.Clear();
            _save.MarkDirty();
            Changed?.Invoke();
            FeedChanged?.Invoke();
        }

        public TeamCreateResult Create(string name, string description, int logo, int requiredLevel)
        {
            if (MyTeam != null) return TeamCreateResult.AlreadyInTeam;
            string clean = (name ?? "").Trim();
            if (clean.Length < NameMinLength || clean.Length > NameMaxLength) return TeamCreateResult.InvalidName;
            if (!_coins.TrySpend(CreateCost)) return TeamCreateResult.NotEnoughCoins;
            _data.created = new TeamInfo
            {
                id = CreatedId,
                name = clean,
                description = (description ?? "").Trim(),
                logo = logo,
                requiredLevel = ClampRequiredLevel(requiredLevel),
                members = 0,
                score = 0,
                activity = (int)TeamActivity.High,
            };
            _data.joinedId = CreatedId;
            _data.feed.Clear();
            AddFeed(TeamFeedType.Joined, Player().name, Player().avatar, "", Now, true);
            _save.MarkDirty();
            Changed?.Invoke();
            FeedChanged?.Invoke();
            return TeamCreateResult.Ok;
        }

        /// <summary>Cấp yêu cầu kế tiếp khi bấm mũi tên (<paramref name="dir"/> = ±1), kẹp trong khoảng của database.</summary>
        public int StepRequiredLevel(int current, int dir)
        {
            int step = Math.Max(1, _db.requiredLevelStep);
            int next = current + dir * step;
            if (dir > 0 && current == _db.minRequiredLevel && _db.minRequiredLevel % step != 0) next = step; // 1 -> 5 -> 10...
            return ClampRequiredLevel(next);
        }

        public int ClampRequiredLevel(int level) => Math.Max(_db.minRequiredLevel, Math.Min(_db.maxRequiredLevel, level));

        /// <summary>Thành viên giả của đội (cố định theo id), cấp cao lên trên; đội trưởng không nhất thiết đứng đầu. Người chơi do UI chèn thêm.</summary>
        public List<TeamMember> FakeMembers(TeamInfo team)
        {
            var list = new List<TeamMember>();
            if (team == null || team.id == CreatedId) return list;
            var rng = new Random(_db.seed ^ StableHash(team.id));
            string[] names = _db.memberNames;
            int leader = team.members > 0 ? rng.Next(Math.Min(3, team.members)) : -1;
            for (int i = 0; i < team.members; i++)
            {
                string n = names != null && names.Length > 0 ? names[rng.Next(names.Length)] : "Player" + rng.Next(1000, 9999);
                int level = team.requiredLevel + rng.Next(0, 400);
                int days = rng.NextDouble() < 0.6 ? rng.Next(0, 3) : rng.Next(3, 90);
                list.Add(new TeamMember(n, rng.Next(Math.Max(1, _db.avatarCount)), level, i == leader, false, days));
            }
            list.Sort((a, b) => b.Level.CompareTo(a.Level));
            return list;
        }

        // ---------- Bảng tin ----------

        public IReadOnlyList<TeamFeedItem> Feed => _data.feed;

        /// <summary>Gửi tin nhắn vào bảng tin; false nếu chưa ở đội hoặc tin rỗng.</summary>
        public bool SendMessage(string text)
        {
            string clean = (text ?? "").Trim();
            if (MyTeam == null || clean.Length == 0) return false;
            if (clean.Length > _db.messageMaxLength) clean = clean.Substring(0, _db.messageMaxLength);
            AddFeed(TeamFeedType.Chat, Player().name, Player().avatar, clean, Now, true);
            _save.MarkDirty();
            FeedChanged?.Invoke();
            return true;
        }

        /// <summary>Giây còn lại tới lần xin mạng tiếp (0 = xin được).</summary>
        public long LifeRequestCooldownLeft()
        {
            if (_data.lastLifeRequest <= 0) return 0;
            long end = _data.lastLifeRequest + _db.lifeRequestCooldownHours * 3600L;
            return Math.Max(0, end - Now);
        }

        public bool RequestLives()
        {
            if (MyTeam == null || LifeRequestCooldownLeft() > 0) return false;
            long now = Now;
            _data.lastLifeRequest = now;
            AddFeed(TeamFeedType.LifeRequest, Player().name, Player().avatar, "", now, true);
            _save.MarkDirty();
            FeedChanged?.Invoke();
            return true;
        }

        /// <summary>Đồng đội giả giúp mạng theo thời gian; cộng mạng mới nhận. Gọi định kỳ khi đang xem My Team.</summary>
        public void UpdateLifeHelps()
        {
            TeamInfo team = MyTeam;
            if (team == null || team.members <= 0) return;
            long now = Now;
            int total = 0;
            foreach (TeamFeedItem it in _data.feed)
            {
                if (it.Type != TeamFeedType.LifeRequest || !it.mine || it.helped >= _db.lifeRequestMax) continue;
                long minutes = Math.Max(0, now - it.time) / 60;
                int target = (int)Math.Min(_db.lifeRequestMax, minutes / Math.Max(1, _db.lifeHelpMinutes));
                target = Math.Min(target, team.members);
                if (target <= it.helped) continue;
                total += target - it.helped;
                it.helped = target;
            }
            if (total <= 0) return;
            _grantLives?.Invoke(total);
            _save.MarkDirty();
            FeedChanged?.Invoke();
        }

        /// <summary>"Just now", "33m ago", "3h ago", "8d ago".</summary>
        public static string FormatAgo(long seconds)
        {
            if (seconds < 60) return "Just now";
            if (seconds < 3600) return seconds / 60 + "m ago";
            if (seconds < 86400) return seconds / 3600 + "h ago";
            return seconds / 86400 + "d ago";
        }

        /// <summary>"3h59m", "12m".</summary>
        public static string FormatCooldown(long seconds)
        {
            long h = seconds / 3600, m = (seconds % 3600) / 60;
            return h > 0 ? h + "h" + m + "m" : Math.Max(1, m) + "m";
        }

        public TeamInfo Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == CreatedId) return _data.HasCreated ? _data.created : null;
            return _fakes.Find(t => t.id == id);
        }

        private (string name, int avatar) Player() => _player != null ? _player() : ("Player", 0);

        // Bảng tin lúc vào đội giả: vài thành viên vừa vào, 1 lời chào, rồi người chơi vào
        private void SeedFeed(TeamInfo team)
        {
            _data.feed.Clear();
            long now = Now;
            List<TeamMember> members = FakeMembers(team);
            var rng = new Random(_db.seed ^ StableHash(team.id) ^ 7);
            int joins = Math.Min(6, members.Count);
            for (int i = 0; i < joins; i++)
            {
                TeamMember m = members[rng.Next(members.Count)];
                AddFeed(TeamFeedType.Joined, m.Name, m.Avatar, "", now - (joins - i) * 3600L * (1 + rng.Next(20)), false);
                if (i == joins / 2 && _db.greetings != null && _db.greetings.Length > 0)
                    AddFeed(TeamFeedType.Chat, m.Name, m.Avatar, _db.greetings[rng.Next(_db.greetings.Length)], now - 1980, false);
            }
            AddFeed(TeamFeedType.Joined, Player().name, Player().avatar, "", now, true);
        }

        private void AddFeed(TeamFeedType type, string name, int avatar, string text, long time, bool mine)
        {
            _data.feed.Add(new TeamFeedItem { type = (int)type, name = name, avatar = avatar, text = text, time = time, mine = mine });
            while (_data.feed.Count > Math.Max(1, _db.feedMax)) _data.feed.RemoveAt(0);
        }

        // string.GetHashCode có thể khác giữa các runtime; cần cố định để thành viên giả không đổi
        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        private List<TeamInfo> All()
        {
            var all = new List<TeamInfo>(_fakes);
            if (_data.HasCreated) all.Add(_data.created);
            return all;
        }

        private void BuildFakes()
        {
            string[] names = _db.teamNames ?? new string[0];
            string[] descs = _db.teamDescriptions ?? new string[0];
            int logoCount = Math.Max(1, _db.logos != null ? _db.logos.Length : 1);
            int[] levels = { 1, 1, 5, 10, 15, 20, 30, 50 };
            var rng = new Random(_db.seed);
            for (int i = 0; i < names.Length; i++)
            {
                // nhiều đội gần đầy, ít đội vắng (giống danh sách thật)
                int members = rng.NextDouble() < 0.35 ? rng.Next(40, _db.capacity + 1) : rng.Next(1, 30);
                _fakes.Add(new TeamInfo
                {
                    id = "t" + i,
                    name = names[i],
                    description = descs.Length > 0 ? descs[i % descs.Length] : "",
                    logo = rng.Next(logoCount),
                    requiredLevel = levels[rng.Next(levels.Length)],
                    members = members,
                    score = members * 200 + rng.Next(1000),
                    activity = members > 35 ? (int)TeamActivity.High : rng.Next(3),
                });
            }
        }
    }
}
