using System;
using System.Collections.Generic;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Teams
{
    /// <summary>
    /// Đội (giống guild), giả lập offline: danh sách đội giả sinh cố định theo seed (TeamsDatabase), tìm theo tên,
    /// tạo đội (trừ coin), vào / rời đội; lưu section "teams". Số thành viên hiển thị = số giả + 1 nếu người chơi ở trong đội.
    /// Chỉ biết contract (ví coin) và hàm lấy cấp người chơi; GameBootstrap tạo và truyền vào.
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
        private readonly TeamsDatabase _db;
        private readonly List<TeamInfo> _fakes = new List<TeamInfo>();

        /// <summary>Vào / rời / tạo đội.</summary>
        public event Action Changed;

        public TeamService(ISaveService save, ICurrencyWallet coins, Func<int> playerLevel, TeamsDatabase db)
        {
            _save = save;
            _data = save.Get<TeamSave>(TeamSave.Key);
            _coins = coins;
            _playerLevel = playerLevel;
            _db = db;
            BuildFakes();
        }

        public TeamsDatabase Database => _db;
        public int Capacity => _db.capacity;
        public int CreateCost => _db.createCost;
        public int PlayerLevel => _playerLevel != null ? _playerLevel() : 1;

        /// <summary>Đội đang ở (null nếu chưa vào).</summary>
        public TeamInfo MyTeam => string.IsNullOrEmpty(_data.joinedId) ? null : Find(_data.joinedId);

        public bool IsMine(TeamInfo team) => team != null && team.id == _data.joinedId;

        /// <summary>Số thành viên hiển thị (tính cả người chơi nếu đang ở đội này).</summary>
        public int MembersOf(TeamInfo team) => team == null ? 0 : team.members + (IsMine(team) ? 1 : 0);

        /// <summary>Danh sách tab "Teams": đội mạnh lên trên.</summary>
        public List<TeamInfo> Top(int count)
        {
            List<TeamInfo> all = All();
            all.Sort((a, b) => b.score.CompareTo(a.score));
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
            result.Sort((a, b) => b.score.CompareTo(a.score));
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
            TeamJoinResult r = CanJoin(Find(teamId));
            if (r != TeamJoinResult.Ok) return r;
            _data.joinedId = teamId;
            _save.MarkDirty();
            Changed?.Invoke();
            return r;
        }

        /// <summary>Rời đội. Đội tự tạo chỉ có người chơi nên rời là giải tán.</summary>
        public void Leave()
        {
            if (string.IsNullOrEmpty(_data.joinedId)) return;
            if (_data.joinedId == CreatedId) _data.created = new TeamInfo();
            _data.joinedId = "";
            _save.MarkDirty();
            Changed?.Invoke();
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
            };
            _data.joinedId = CreatedId;
            _save.MarkDirty();
            Changed?.Invoke();
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

        /// <summary>Thành viên giả của đội (cố định theo id), đội trưởng đầu tiên. Người chơi do UI chèn thêm.</summary>
        public List<TeamMember> FakeMembers(TeamInfo team)
        {
            var list = new List<TeamMember>();
            if (team == null || team.id == CreatedId) return list;
            var rng = new Random(_db.seed ^ StableHash(team.id));
            string[] names = _db.memberNames;
            for (int i = 0; i < team.members; i++)
            {
                string n = names != null && names.Length > 0 ? names[rng.Next(names.Length)] : "Player" + rng.Next(1000, 9999);
                int level = team.requiredLevel + rng.Next(0, 120);
                list.Add(new TeamMember(n, rng.Next(Math.Max(1, _db.avatarCount)), level, i == 0, false));
            }
            list.Sort((a, b) => a.IsLeader != b.IsLeader ? (a.IsLeader ? -1 : 1) : b.Level.CompareTo(a.Level));
            return list;
        }

        public TeamInfo Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == CreatedId) return _data.HasCreated ? _data.created : null;
            return _fakes.Find(t => t.id == id);
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
                    score = members * 1000 + rng.Next(1000),
                });
            }
        }
    }
}
