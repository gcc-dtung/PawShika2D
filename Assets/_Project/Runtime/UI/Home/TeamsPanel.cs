using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Features.Economy;
using PawBlock.Features.Profile;
using PawBlock.Features.Teams;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Đội của Main Menu (TeamsPanel.prefab), giống guild của bản gốc nhưng giả lập offline (TeamService):
    /// - Teams: top đội (đội mạnh lên trên), nút View mở màn thông tin.
    /// - Search: ô nhập tên + nút tìm, kết quả như tab Teams.
    /// - Create: logo (bấm để đổi), tên, mô tả, cấp yêu cầu (mũi tên), nút Create trừ coin. Khi đã vào đội, tab này thành My Team.
    /// - Info: logo, tên, sức chứa, cấp yêu cầu, mô tả, danh sách thành viên (người chơi chèn vào nếu ở đội này), nút Join / Leave.
    /// Đổi tab: trang cũ mờ đi, trang mới hiện dần, các dòng hiện lần lượt từ trên xuống (thời gian thật).
    /// Ghép Teams + Economy + Profile nên nằm ở Game. GameBootstrap gọi Init.
    /// </summary>
    public class TeamsPanel : MenuPanel
    {
        private enum Page { Teams, Search, Create, Info }

        [SerializeField] private AvatarDatabase avatars;
        [SerializeField] private int topCount = 30;

        [Header("Tab con (Teams, Search, Create / My Team)")]
        [SerializeField] private Button[] tabButtons = new Button[3];
        [SerializeField] private GameObject[] tabActive = new GameObject[3];
        [SerializeField] private TMP_Text[] thirdTabLabels;
        [SerializeField] private string createLabel = "Create";
        [SerializeField] private string myTeamLabel = "My Team";

        [Header("Trang")]
        [SerializeField] private CanvasGroup teamsPage;
        [SerializeField] private CanvasGroup searchPage;
        [SerializeField] private CanvasGroup createPage;
        [SerializeField] private CanvasGroup infoPage;
        [SerializeField] private float fadeDuration = 0.18f;
        [SerializeField] private float rowStagger = 0.035f;

        [Header("Danh sách")]
        [SerializeField] private TeamRow rowTemplate;
        [SerializeField] private RectTransform teamsContent;
        [SerializeField] private RectTransform searchContent;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private GameObject searchEmpty;

        [Header("Tạo đội")]
        [SerializeField] private Image createLogo;
        [SerializeField] private Button logoButton;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField descriptionInput;
        [SerializeField] private Button levelPrev;
        [SerializeField] private Button levelNext;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button createButton;
        [SerializeField] private TMP_Text createCostText;
        [SerializeField] private CoinHUD coins;

        [Header("Thông tin đội")]
        [SerializeField] private Image infoLogo;
        [SerializeField] private TMP_Text infoName;
        [SerializeField] private TMP_Text infoMembers;
        [SerializeField] private TMP_Text infoLevel;
        [SerializeField] private TMP_Text infoDescription;
        [SerializeField] private TeamMemberRow memberTemplate;
        [SerializeField] private RectTransform memberContent;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("Nền nút Join (xanh) / Leave (đỏ)")]
        [SerializeField] private Sprite joinSprite;
        [SerializeField] private Sprite leaveSprite;
        [SerializeField] private Button backButton;

        private TeamService _teams;
        private PlayerProfileService _profile;
        private GameFlowUI _flow;
        private Page _page = Page.Teams;
        private Page _returnPage = Page.Teams;
        private TeamInfo _shown;
        private int _logo;
        private int _requiredLevel;
        private Sequence _anim;
        private readonly List<TeamRow> _teamRows = new List<TeamRow>();
        private readonly List<TeamRow> _searchRows = new List<TeamRow>();
        private readonly List<TeamMemberRow> _memberRows = new List<TeamMemberRow>();

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(TeamService teams, CoinWallet coinWallet, PlayerProfileService profile, GameFlowUI flow)
        {
            if (_teams != null) _teams.Changed -= OnTeamsChanged;
            _teams = teams;
            _profile = profile;
            _flow = flow;
            if (_teams != null) _teams.Changed += OnTeamsChanged;
            if (coins != null) coins.Init(coinWallet);
            if (_teams != null) _requiredLevel = _teams.ClampRequiredLevel(_teams.Database.defaultRequiredLevel);
        }

        private void OnDestroy()
        {
            if (_teams != null) _teams.Changed -= OnTeamsChanged;
        }

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => OnTab(index));
            }
            if (searchButton != null) searchButton.onClick.AddListener(() => Show(Page.Search));
            if (searchInput != null) searchInput.onSubmit.AddListener(_ => Show(Page.Search));
            if (logoButton != null) logoButton.onClick.AddListener(NextLogo);
            if (levelPrev != null) levelPrev.onClick.AddListener(() => StepLevel(-1));
            if (levelNext != null) levelNext.onClick.AddListener(() => StepLevel(1));
            if (createButton != null) createButton.onClick.AddListener(OnCreate);
            if (actionButton != null) actionButton.onClick.AddListener(OnAction);
            if (backButton != null) backButton.onClick.AddListener(() => Show(_returnPage));
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (memberTemplate != null) memberTemplate.gameObject.SetActive(false);
        }

        private void OnEnable() => Show(_page, false);

        protected override void OnShown() => Show(_page);

        private void OnDisable()
        {
            _anim?.Kill();
            _anim = null;
        }

        private void OnTab(int index)
        {
            if (index == 2 && _teams != null && _teams.MyTeam != null)
            {
                _shown = _teams.MyTeam;
                _returnPage = Page.Teams;
                Show(Page.Info);
                return;
            }
            Show((Page)index);
        }

        private void OnTeamsChanged()
        {
            RefreshTabs();
            if (_page == Page.Info) RefreshInfo();
        }

        private void Show(Page page, bool animate = true)
        {
            if (_teams == null) return;
            _page = page;
            RefreshTabs();
            List<CanvasGroup> rows = null;
            switch (page)
            {
                case Page.Teams: rows = FillRows(_teams.Top(topCount), teamsContent, _teamRows); break;
                case Page.Search:
                    List<TeamInfo> found = _teams.Search(searchInput != null ? searchInput.text : "");
                    rows = FillRows(found, searchContent, _searchRows);
                    if (searchEmpty != null) searchEmpty.SetActive(found.Count == 0 && searchInput != null && searchInput.text.Trim().Length > 0);
                    break;
                case Page.Create: RefreshCreate(); break;
                case Page.Info: RefreshInfo(); break;
            }
            Fade(PageGroup(page), rows, animate);
        }

        private void RefreshTabs()
        {
            bool inTeam = _teams != null && _teams.MyTeam != null;
            int tab = _page == Page.Info ? (inTeam && _teams.IsMine(_shown) ? 2 : -1) : (int)_page;
            for (int i = 0; i < tabActive.Length; i++)
            {
                if (tabActive[i] != null) tabActive[i].SetActive(i == tab);
            }
            if (thirdTabLabels != null)
            {
                foreach (TMP_Text t in thirdTabLabels)
                {
                    if (t != null) t.SetText(inTeam ? myTeamLabel : createLabel);
                }
            }
        }

        private CanvasGroup PageGroup(Page page)
        {
            switch (page)
            {
                case Page.Teams: return teamsPage;
                case Page.Search: return searchPage;
                case Page.Create: return createPage;
                default: return infoPage;
            }
        }

        // Trang cũ mờ đi, trang mới hiện dần; dòng hiện lần lượt
        private void Fade(CanvasGroup target, List<CanvasGroup> rows, bool animate)
        {
            _anim?.Kill();
            Sequence seq = animate ? DOTween.Sequence().SetUpdate(true).SetLink(gameObject) : null;
            foreach (CanvasGroup g in new[] { teamsPage, searchPage, createPage, infoPage })
            {
                if (g == null) continue;
                bool on = g == target;
                g.blocksRaycasts = on;
                g.interactable = on;
                if (on) g.gameObject.SetActive(true);
                if (seq == null) { g.alpha = on ? 1f : 0f; g.gameObject.SetActive(on); continue; }
                if (on) { g.alpha = 0f; seq.Join(g.DOFade(1f, fadeDuration)); }
                else if (g.gameObject.activeSelf)
                {
                    CanvasGroup off = g;
                    seq.Join(g.DOFade(0f, fadeDuration * 0.7f).OnComplete(() => off.gameObject.SetActive(false)));
                }
            }
            if (rows == null) return;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null) continue;
                if (seq == null) { rows[i].alpha = 1f; continue; }
                rows[i].alpha = 0f;
                seq.Insert(fadeDuration * 0.5f + i * rowStagger, rows[i].DOFade(1f, fadeDuration));
            }
            _anim = seq;
        }

        private List<CanvasGroup> FillRows(List<TeamInfo> teams, RectTransform parent, List<TeamRow> pool)
        {
            var groups = new List<CanvasGroup>();
            if (rowTemplate == null || parent == null) return groups;
            while (pool.Count < teams.Count)
            {
                TeamRow row = Instantiate(rowTemplate, parent);
                pool.Add(row);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool used = i < teams.Count;
                pool[i].gameObject.SetActive(used);
                if (!used) continue;
                TeamInfo t = teams[i];
                pool[i].Set(i + 1, t, _teams.Database.Logo(t.logo), _teams.MembersOf(t), _teams.Capacity, _teams.IsMine(t), OpenInfo);
                groups.Add(pool[i].Group);
            }
            return groups;
        }

        private void OpenInfo(TeamInfo team)
        {
            _shown = team;
            if (_page != Page.Info) _returnPage = _page;
            Show(Page.Info);
        }

        private void RefreshInfo()
        {
            if (_shown == null) return;
            bool mine = _teams.IsMine(_shown);
            if (infoLogo != null) infoLogo.sprite = _teams.Database.Logo(_shown.logo);
            if (infoName != null) infoName.SetText(_shown.name);
            if (infoMembers != null) infoMembers.SetText("{0}/{1}", _teams.MembersOf(_shown), _teams.Capacity);
            if (infoLevel != null) infoLevel.SetText("{0}", _shown.requiredLevel);
            if (infoDescription != null) infoDescription.SetText(string.IsNullOrEmpty(_shown.description) ? "-" : _shown.description);
            if (backButton != null) backButton.gameObject.SetActive(!mine);

            if (actionButton != null)
            {
                TeamJoinResult can = _teams.CanJoin(_shown);
                bool show = mine || can != TeamJoinResult.AlreadyInTeam;
                actionButton.gameObject.SetActive(show);
                actionButton.interactable = mine || can == TeamJoinResult.Ok;
                Sprite bg = mine ? leaveSprite : joinSprite;
                if (bg != null && actionButton.image != null) actionButton.image.sprite = bg;
                if (actionLabel != null)
                {
                    actionLabel.SetText(mine ? "Leave"
                        : can == TeamJoinResult.Full ? "Full"
                        : can == TeamJoinResult.LevelTooLow ? "Lv " + _shown.requiredLevel
                        : "Join");
                }
            }

            List<TeamMember> members = _teams.FakeMembers(_shown);
            if (mine && _profile != null)
            {
                PlayerProfile me = _profile.Local();
                members.Add(new TeamMember(me.Name, me.AvatarId, me.Level, _shown.id == TeamService.CreatedId, true));
                members.Sort((a, b) => a.IsLeader != b.IsLeader ? (a.IsLeader ? -1 : 1) : b.Level.CompareTo(a.Level));
            }
            FillMembers(members);
        }

        private void FillMembers(List<TeamMember> members)
        {
            if (memberTemplate == null || memberContent == null) return;
            while (_memberRows.Count < members.Count) _memberRows.Add(Instantiate(memberTemplate, memberContent));
            for (int i = 0; i < _memberRows.Count; i++)
            {
                bool used = i < members.Count;
                _memberRows[i].gameObject.SetActive(used);
                if (used) _memberRows[i].Set(i + 1, members[i], avatars != null ? avatars.Get(members[i].Avatar) : null);
            }
        }

        private void OnAction()
        {
            if (_shown == null) return;
            if (_teams.IsMine(_shown))
            {
                _teams.Leave();
                _shown = _teams.Find(_shown.id); // đội tự tạo: giải tán -> null
                Show(_shown != null ? Page.Info : Page.Teams);
                return;
            }
            TeamJoinResult r = _teams.Join(_shown.id);
            if (r != TeamJoinResult.Ok) Notice(JoinMessage(r));
        }

        private static string JoinMessage(TeamJoinResult r)
        {
            switch (r)
            {
                case TeamJoinResult.Full: return "This team is full.";
                case TeamJoinResult.LevelTooLow: return "Your level is too low for this team.";
                case TeamJoinResult.AlreadyInTeam: return "Leave your current team first.";
                default: return "Team not found.";
            }
        }

        // ---------- Tạo đội ----------

        private void RefreshCreate()
        {
            if (createLogo != null) createLogo.sprite = _teams.Database.Logo(_logo);
            if (levelText != null) levelText.SetText("{0}", _requiredLevel);
            if (createCostText != null) createCostText.SetText("{0}", _teams.CreateCost);
        }

        private void NextLogo()
        {
            int count = _teams != null && _teams.Database.logos != null ? _teams.Database.logos.Length : 0;
            if (count == 0) return;
            _logo = (_logo + 1) % count;
            RefreshCreate();
        }

        private void StepLevel(int dir)
        {
            _requiredLevel = _teams.StepRequiredLevel(_requiredLevel, dir);
            RefreshCreate();
        }

        private void OnCreate()
        {
            TeamCreateResult r = _teams.Create(nameInput != null ? nameInput.text : "", descriptionInput != null ? descriptionInput.text : "", _logo, _requiredLevel);
            switch (r)
            {
                case TeamCreateResult.Ok:
                    if (nameInput != null) nameInput.text = "";
                    if (descriptionInput != null) descriptionInput.text = "";
                    OpenInfo(_teams.MyTeam);
                    break;
                case TeamCreateResult.InvalidName:
                    Notice($"Team name must be {TeamService.NameMinLength}-{TeamService.NameMaxLength} characters.");
                    break;
                case TeamCreateResult.NotEnoughCoins:
                    Notice("Not enough coins.");
                    break;
                default:
                    Notice("Leave your current team first.");
                    break;
            }
        }

        private void Notice(string message)
        {
            if (_flow != null) _flow.ShowNotice(message, "TEAMS");
        }
    }
}
