using NUnit.Framework;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;

namespace PawBlock.Features.Teams.Tests
{
    /// <summary>EditMode: TeamService giả lập offline (save chỉ trong bộ nhớ, không đụng save.json).</summary>
    public class TeamServiceTests
    {
        private JsonFileSaveService _save;
        private CoinWallet _coins;
        private TeamsDatabase _db;
        private int _level;

        [SetUp]
        public void SetUp()
        {
            _save = new JsonFileSaveService(null);
            _coins = new CoinWallet(_save);
            _db = ScriptableObject.CreateInstance<TeamsDatabase>();
            _db.teamNames = new[] { "Breizh", "Bradenburg", "Brianna", "Miray", "Boo Crew" };
            _db.memberNames = new[] { "Luna", "Casper" };
            _db.logos = new Sprite[0];
            _level = 100;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_db);

        private TeamService Create() => new TeamService(_save, _coins, () => _level, _db);

        [Test]
        public void FakeTeams_SameSeed_SameData()
        {
            TeamService a = Create();
            TeamService b = new TeamService(new JsonFileSaveService(null), _coins, () => _level, _db);
            for (int i = 0; i < _db.teamNames.Length; i++)
            {
                TeamInfo x = a.Find("t" + i), y = b.Find("t" + i);
                Assert.AreEqual(x.members, y.members);
                Assert.AreEqual(x.logo, y.logo);
                Assert.AreEqual(x.requiredLevel, y.requiredLevel);
            }
        }

        [Test]
        public void Search_IgnoresCase_MatchesSubstring()
        {
            var found = Create().Search("BR");
            Assert.AreEqual(3, found.Count); // Breizh, Bradenburg, Brianna
            Assert.IsEmpty(Create().Search("   "));
        }

        [Test]
        public void Join_Then_Leave_UpdatesMembersAndSave()
        {
            TeamService sut = Create();
            TeamInfo team = sut.Find("t3");
            team.members = 10;
            Assert.AreEqual(TeamJoinResult.Ok, sut.Join("t3"));
            Assert.AreEqual(11, sut.MembersOf(team));
            Assert.AreEqual("t3", _save.Get<TeamSave>(TeamSave.Key).joinedId);
            Assert.AreEqual(TeamJoinResult.AlreadyInTeam, sut.Join("t0"));
            sut.Leave();
            Assert.IsNull(sut.MyTeam);
            Assert.AreEqual(10, sut.MembersOf(team));
        }

        [Test]
        public void Join_FullOrLevelTooLow_Rejected()
        {
            TeamService sut = Create();
            sut.Find("t0").members = _db.capacity;
            Assert.AreEqual(TeamJoinResult.Full, sut.Join("t0"));
            TeamInfo t1 = sut.Find("t1");
            t1.members = 1;
            t1.requiredLevel = 50;
            _level = 10;
            Assert.AreEqual(TeamJoinResult.LevelTooLow, sut.Join("t1"));
            Assert.IsNull(sut.MyTeam);
        }

        [Test]
        public void Create_SpendsCoins_AndBecomesMyTeam()
        {
            _coins.Add(_db.createCost + 5);
            TeamService sut = Create();
            Assert.AreEqual(TeamCreateResult.InvalidName, sut.Create("ab", "", 0, 1));
            Assert.AreEqual(TeamCreateResult.Ok, sut.Create("  Ghost Squad ", "boo", 2, 12));
            Assert.AreEqual(5, _coins.Balance);
            Assert.AreEqual("Ghost Squad", sut.MyTeam.name);
            Assert.AreEqual(1, sut.MembersOf(sut.MyTeam));
            Assert.IsTrue(sut.Search("ghost").Count == 1);

            // vẫn còn sau khi mở lại game
            TeamService reload = Create();
            Assert.AreEqual("Ghost Squad", reload.MyTeam.name);

            reload.Leave(); // đội tự tạo: rời là giải tán
            Assert.IsNull(reload.Find(TeamService.CreatedId));
        }

        [Test]
        public void Create_NotEnoughCoins_NoTeam()
        {
            TeamService sut = Create();
            Assert.AreEqual(TeamCreateResult.NotEnoughCoins, sut.Create("Ghost Squad", "", 0, 1));
            Assert.IsNull(sut.MyTeam);
        }

        private sealed class FakeClock : IClock
        {
            public long Now = 1_800_000_000;
            public long UtcNowSeconds => Now;
            public System.DateTime LocalNow => System.DateTimeOffset.FromUnixTimeSeconds(Now).LocalDateTime;
            public event System.Action Synced { add { } remove { } }
        }

        [Test]
        public void Join_SeedsFeed_EndsWithPlayerJoined()
        {
            var clock = new FakeClock();
            var sut = new TeamService(_save, _coins, () => _level, _db, clock, () => ("Me", 3));
            sut.Find("t0").members = 10;
            sut.Join("t0");
            Assert.Greater(sut.Feed.Count, 1);
            TeamFeedItem last = sut.Feed[sut.Feed.Count - 1];
            Assert.AreEqual(TeamFeedType.Joined, last.Type);
            Assert.IsTrue(last.mine);
            Assert.AreEqual("Me", last.name);
            sut.Leave();
            Assert.AreEqual(0, sut.Feed.Count);
        }

        [Test]
        public void SendMessage_TrimsAndRequiresTeam()
        {
            var sut = new TeamService(_save, _coins, () => _level, _db, new FakeClock(), () => ("Me", 0));
            Assert.IsFalse(sut.SendMessage("hi")); // chưa vào đội
            sut.Find("t1").members = 5;
            sut.Join("t1");
            Assert.IsFalse(sut.SendMessage("   "));
            Assert.IsTrue(sut.SendMessage("  hello  "));
            Assert.AreEqual("hello", sut.Feed[sut.Feed.Count - 1].text);
        }

        [Test]
        public void RequestLives_CooldownAndHelpsGrantLives()
        {
            var clock = new FakeClock();
            int granted = 0;
            var sut = new TeamService(_save, _coins, () => _level, _db, clock, () => ("Me", 0), n => granted += n);
            sut.Find("t2").members = 3;
            sut.Join("t2");
            Assert.IsTrue(sut.RequestLives());
            Assert.IsFalse(sut.RequestLives()); // đang chờ
            Assert.AreEqual(_db.lifeRequestCooldownHours * 3600L, sut.LifeRequestCooldownLeft());

            clock.Now += _db.lifeHelpMinutes * 60L * 2;
            sut.UpdateLifeHelps();
            Assert.AreEqual(2, granted);

            clock.Now += _db.lifeHelpMinutes * 60L * 10;
            sut.UpdateLifeHelps();
            Assert.AreEqual(3, granted); // không quá số đồng đội

            clock.Now += _db.lifeRequestCooldownHours * 3600L;
            Assert.AreEqual(0, sut.LifeRequestCooldownLeft());
        }

        [Test]
        public void RankOf_HighestScoreIsOne()
        {
            TeamService sut = Create();
            Assert.AreEqual(1, sut.RankOf(sut.Top(1)[0]));
        }

        [Test]
        public void StepRequiredLevel_ClampsAndSteps()
        {
            TeamService sut = Create();
            Assert.AreEqual(5, sut.StepRequiredLevel(1, 1));
            Assert.AreEqual(10, sut.StepRequiredLevel(5, 1));
            Assert.AreEqual(_db.minRequiredLevel, sut.StepRequiredLevel(1, -1));
            Assert.AreEqual(_db.maxRequiredLevel, sut.StepRequiredLevel(_db.maxRequiredLevel, 1));
        }
    }
}
