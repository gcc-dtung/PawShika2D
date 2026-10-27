using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using PawBlock.Features.Economy;
using PawBlock.Features.Profile;
using PawBlock.Features.Teams;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tab Đội của Main Menu (TeamsPanel.prefab), giống guild của bản gốc nhưng giả lập offline (TeamService).
    /// Chưa vào đội: 3 tab con
    /// - Teams: top đội (đội mạnh lên trên), nút View mở Team Info.
    /// - Search: ô nhập tên + nút tìm, kết quả như tab Teams.
    /// - Create: logo (bấm để đổi), tên, mô tả, cấp yêu cầu (mũi tên), nút Create trừ coin.
    /// Đã vào đội: thay 3 tab bằng trang My Team (banner đội + nút Team Info, bảng tin: ai vào đội / tin nhắn / xin mạng,
    /// nút Message mở ô nhập, nút Request xin mạng có thời gian chờ).
    /// Team Info: thanh tiêu đề + nút X thay 3 tab; logo, tên, mô tả, sức chứa, điểm / hoạt động / cấp yêu cầu / hạng,
    /// nút Join / Leave, danh sách thành viên (top 3 huy chương, người chơi được chèn vào nếu ở đội này).
    /// Đổi trang: trang cũ mờ đi, trang mới hiện dần, các dòng hiện lần lượt (thời gian thật).
    /// Ghép Teams + Economy + Profile nên nằm ở Game. GameBootstrap gọi Init.
    /// </summary>
    public class TeamsPanel : MenuPanel
    {
        private enum Page { Teams, Search, Create, Info, MyTeam }

        [SerializeField] private AvatarDatabase avatars;
        [SerializeField] private int topCount = 30;

        [Header("Đầu trang")]
        [SerializeField] private Button[] tabButtons = new Button[3];
        [SerializeField] private GameObject[] tabActive = new GameObject[3];
        [Tooltip("Chứa 3 nút tab (ẩn ở Team Info / My Team)")]
        [SerializeField] private GameObject tabsRoot;
        [Tooltip("Khung đầu trang của 3 tab (ẩn ở Team Info / My Team, thay bằng titlePlate)")]
        [FormerlySerializedAs("headerTitle")]
        [SerializeField] private GameObject listHeader;
        [SerializeField] private GameObject titlePlate;
        [SerializeField] private TMP_Text titlePlateText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string infoTitle = "Team Info";
        [SerializeField] private string myTeamTitle = "My Team";

        [Header("Trang")]
        [SerializeField] private CanvasGroup teamsPage;
        [SerializeField] private CanvasGroup searchPage;
        [SerializeField] private CanvasGroup createPage;
        [SerializeField] private CanvasGroup infoPage;
        [SerializeField] private CanvasGroup myTeamPage;
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
        [SerializeField] private TMP_Text infoScore;
        [SerializeField] private TMP_Text infoRank;
        [SerializeField] private TMP_Text infoActivity;
        [SerializeField] private Image infoActivityDot;
        [Tooltip("Màu Low, Medium, High")]
        [SerializeField] private Color[] activityColors = { new Color(1f, 0.35f, 0.3f), new Color(1f, 0.85f, 0.2f), new Color(0.4f, 0.98f, 0.15f) };
        [SerializeField] private string[] activityNames = { "Low", "Medium", "High" };
        [SerializeField] private TeamMemberRow memberTemplate;
        [SerializeField] private RectTransform memberContent;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("Nền nút Join (xanh) / Leave (đỏ)")]
        [SerializeField] private Sprite joinSprite;
        [SerializeField] private Sprite leaveSprite;
        [Tooltip("Viền chữ nút Join (xanh) / Leave (đỏ)")]
        [SerializeField] private Material joinLabelMaterial;
        [SerializeField] private Material leaveLabelMaterial;

        [Header("Menu thành viên")]
        [Tooltip("Lớp trong suốt phủ trang Team Info: bấm ra ngoài menu thì đóng")]
        [SerializeField] private Button memberMenuBlocker;
        [Tooltip("Khung menu (mũi nhọn ở trên, pivot đặt tại mũi nhọn)")]
        [SerializeField] private RectTransform memberMenu;
        [SerializeField] private Button viewProfileButton;
        [SerializeField] private Button removeMemberButton;
        [Tooltip("Nút Xoá thành viên khi không được dùng: mờ đi như bản gốc")]
        [SerializeField] private float removeDisabledAlpha = 0.45f;
        [SerializeField] private float menuPopDuration = 0.22f;
        [SerializeField] private string leaveTitle = "Leave Team?";
        [SerializeField] private string leaveMessage = "Are you sure you want to leave the team?";

        [Header("Đội của tôi")]
        [SerializeField] private Image myLogo;
        [SerializeField] private TMP_Text myName;
        [SerializeField] private Button myInfoButton;
        [SerializeField] private ScrollRect feedScroll;
        [SerializeField] private RectTransform feedContent;
        [SerializeField] private TeamFeedRow noticeTemplate;
        [SerializeField] private TeamFeedRow chatOtherTemplate;
        [SerializeField] private TeamFeedRow chatMineTemplate;
        [SerializeField] private TeamFeedRow requestTemplate;
        [SerializeField] private Button messageButton;
        [SerializeField] private GameObject chatBar;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button requestButton;
        [SerializeField] private Sprite requestReadySprite;
        [SerializeField] private Sprite requestWaitSprite;
        [SerializeField] private GameObject requestTimer;
        [SerializeField] private TMP_Text requestTimerText;

        private TeamService _teams;
        private PlayerProfileService _profile;
        private GameFlowUI _flow;
        private Page _page = Page.Teams;
        private Page _returnPage = Page.Teams;
        private TeamInfo _shown;
        private int _logo;
        private int _requiredLevel;
        private float _nextTick;
        private int _scrollEndFrames;
        private Sequence _anim;
        private TeamMember _menuMember;
        private readonly List<TeamRow> _teamRows = new List<TeamRow>();
        private readonly List<TeamRow> _searchRows = new List<TeamRow>();
        private readonly List<TeamMemberRow> _memberRows = new List<TeamMemberRow>();
        private readonly List<GameObject> _feedRows = new List<GameObject>();

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(TeamService teams, CoinWallet coinWallet, PlayerProfileService profile, GameFlowUI flow)
        {
            if (_teams != null)
            {
                _teams.Changed -= OnTeamsChanged;
                _teams.FeedChanged -= OnFeedChanged;
            }
            _teams = teams;
            _profile = profile;
            _flow = flow;
            if (_teams != null)
            {
                _teams.Changed += OnTeamsChanged;
                _teams.FeedChanged += OnFeedChanged;
                _requiredLevel = _teams.ClampRequiredLevel(_teams.Database.defaultRequiredLevel);
            }
            if (coins != null) coins.Init(coinWallet);
        }

        private void OnDestroy()
        {
            if (_teams == null) return;
            _teams.Changed -= OnTeamsChanged;
            _teams.FeedChanged -= OnFeedChanged;
        }

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Page p = (Page)i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => Show(p));
            }
            if (searchButton != null) searchButton.onClick.AddListener(() => Show(Page.Search));
            if (searchInput != null) searchInput.onSubmit.AddListener(_ => Show(Page.Search));
            if (logoButton != null) logoButton.onClick.AddListener(NextLogo);
            if (levelPrev != null) levelPrev.onClick.AddListener(() => StepLevel(-1));
            if (levelNext != null) levelNext.onClick.AddListener(() => StepLevel(1));
            if (createButton != null) createButton.onClick.AddListener(OnCreate);
            if (actionButton != null) actionButton.onClick.AddListener(OnAction);
            if (closeButton != null) closeButton.onClick.AddListener(CloseInfo);
            if (myInfoButton != null) myInfoButton.onClick.AddListener(() => OpenInfo(_teams != null ? _teams.MyTeam : null));
            if (messageButton != null) messageButton.onClick.AddListener(ToggleChat);
            if (sendButton != null) sendButton.onClick.AddListener(SendChat);
            if (chatInput != null) chatInput.onSubmit.AddListener(_ => SendChat());
            if (requestButton != null) requestButton.onClick.AddListener(OnRequest);
            if (memberMenuBlocker != null) memberMenuBlocker.onClick.AddListener(CloseMemberMenu);
            if (viewProfileButton != null) viewProfileButton.onClick.AddListener(ViewMemberProfile);
            CloseMemberMenu();
            foreach (Component t in new Component[] { rowTemplate, memberTemplate, noticeTemplate, chatOtherTemplate, chatMineTemplate, requestTemplate })
            {
                if (t != null) t.gameObject.SetActive(false);
            }
            if (chatBar != null) chatBar.SetActive(false);
        }

        private void OnEnable() => Show(EntryPage(), false);

        protected override void OnShown() => Show(EntryPage());

        private void OnDisable()
        {
            _anim?.Kill();
            _anim = null;
            CloseMemberMenu();
        }

        private void LateUpdate()
        {
            if (_scrollEndFrames <= 0 || feedScroll == null) return;
            _scrollEndFrames--;
            feedScroll.verticalNormalizedPosition = 0f;
        }

        private void Update()
        {
            if (_page != Page.MyTeam || _teams == null || Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;
            _teams.UpdateLifeHelps();
            RefreshRequestButton();
        }

        private bool InTeam => _teams != null && _teams.MyTeam != null;

        // Trang khi mở tab: đã vào đội thì My Team, chưa thì giữ tab con đang chọn
        private Page EntryPage()
        {
            if (InTeam) return _page == Page.Info ? Page.Info : Page.MyTeam;
            return _page == Page.MyTeam || _page == Page.Info ? Page.Teams : _page;
        }

        private void OnTeamsChanged()
        {
            if (_page == Page.Info) RefreshInfo();
        }

        private void OnFeedChanged()
        {
            if (_page == Page.MyTeam) RefreshMyTeam(false);
        }

        private void Show(Page page, bool animate = true)
        {
            if (_teams == null) return;
            _page = page;
            CloseMemberMenu();
            RefreshHeader();
            CanvasGroup target = PageGroup(page);
            if (target != null) target.gameObject.SetActive(true); // bật trước để layout / cuộn tính đúng
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
                case Page.MyTeam:
                    _teams.UpdateLifeHelps();
                    RefreshMyTeam(true);
                    break;
            }
            Fade(target, rows, animate);
        }

        private void RefreshHeader()
        {
            bool list = _page == Page.Teams || _page == Page.Search || _page == Page.Create;
            if (tabsRoot != null) tabsRoot.SetActive(list);
            if (listHeader != null) listHeader.SetActive(list);
            if (titlePlate != null) titlePlate.SetActive(!list);
            if (titlePlateText != null) titlePlateText.SetText(_page == Page.MyTeam ? myTeamTitle : infoTitle);
            if (closeButton != null) closeButton.gameObject.SetActive(_page == Page.Info);
            for (int i = 0; i < tabActive.Length; i++)
            {
                if (tabActive[i] != null) tabActive[i].SetActive(i == (int)_page);
            }
        }

        private CanvasGroup PageGroup(Page page)
        {
            switch (page)
            {
                case Page.Teams: return teamsPage;
                case Page.Search: return searchPage;
                case Page.Create: return createPage;
                case Page.MyTeam: return myTeamPage;
                default: return infoPage;
            }
        }

        // Trang cũ mờ đi, trang mới hiện dần; dòng hiện lần lượt
        private void Fade(CanvasGroup target, List<CanvasGroup> rows, bool animate)
        {
            _anim?.Kill();
            Sequence seq = animate ? DOTween.Sequence().SetUpdate(true).SetLink(gameObject) : null;
            foreach (CanvasGroup g in new[] { teamsPage, searchPage, createPage, infoPage, myTeamPage })
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
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i] == null) continue;
                    if (seq == null) { rows[i].alpha = 1f; continue; }
                    rows[i].alpha = 0f;
                    seq.Insert(fadeDuration * 0.5f + i * rowStagger, rows[i].DOFade(1f, fadeDuration));
                }
            }
            _anim = seq;
        }

        private List<CanvasGroup> FillRows(List<TeamInfo> teams, RectTransform parent, List<TeamRow> pool)
        {
            var groups = new List<CanvasGroup>();
            if (rowTemplate == null || parent == null) return groups;
            while (pool.Count < teams.Count) pool.Add(Instantiate(rowTemplate, parent));
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

        // ---------- Thông tin đội ----------

        private void OpenInfo(TeamInfo team)
        {
            if (team == null) return;
            _shown = team;
            if (_page != Page.Info) _returnPage = _page;
            Show(Page.Info);
        }

        private void CloseInfo()
        {
            // Đã vào đội thì về My Team; chưa thì về danh sách đã mở Team Info
            Show(InTeam ? Page.MyTeam : _returnPage == Page.Search ? Page.Search : Page.Teams);
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
            if (infoScore != null) infoScore.SetText("{0}", _teams.ScoreOf(_shown));
            if (infoRank != null) infoRank.SetText("{0}", _teams.RankOf(_shown));
            int act = Mathf.Clamp(_shown.activity, 0, 2);
            if (infoActivity != null)
            {
                infoActivity.SetText(activityNames.Length > act ? activityNames[act] : "");
                if (activityColors.Length > act) infoActivity.color = activityColors[act];
            }
            if (infoActivityDot != null && activityColors.Length > act) infoActivityDot.color = activityColors[act];

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
                    Material mat = mine ? leaveLabelMaterial : joinLabelMaterial;
                    if (mat != null) actionLabel.fontSharedMaterial = mat;
                }
            }

            List<TeamMember> members = _teams.FakeMembers(_shown);
            if (mine && _profile != null)
            {
                PlayerProfile me = _profile.Local();
                members.Add(new TeamMember(me.Name, me.AvatarId, me.Level, _shown.id == TeamService.CreatedId, true));
                members.Sort((a, b) => b.Level.CompareTo(a.Level));
            }
            FillMembers(members);
        }

        private void FillMembers(List<TeamMember> members)
        {
            if (memberTemplate == null || memberContent == null) return;
            while (_memberRows.Count < members.Count)
            {
                TeamMemberRow row = Instantiate(memberTemplate, memberContent);
                row.Clicked += OpenMemberMenu;
                _memberRows.Add(row);
            }
            for (int i = 0; i < _memberRows.Count; i++)
            {
                bool used = i < members.Count;
                _memberRows[i].gameObject.SetActive(used);
                if (used) _memberRows[i].Set(i + 1, members[i], Avatar(members[i].Avatar));
            }
        }

        private void OnAction()
        {
            if (_shown == null) return;
            if (_teams.IsMine(_shown))
            {
                if (_flow != null) _flow.ShowConfirm(leaveMessage, leaveTitle, LeaveTeam);
                else LeaveTeam();
                return;
            }
            TeamJoinResult r = _teams.Join(_shown.id);
            if (r != TeamJoinResult.Ok) Notice(JoinMessage(r)); // vào được: Team Info cập nhật (nút Leave), X về My Team
        }

        private void LeaveTeam()
        {
            _teams.Leave();
            Show(Page.Teams);
        }

        // ---------- Menu thành viên ----------

        // Bấm 1 thành viên: menu bật ra ngay dưới avatar (mũi nhọn chỉ vào avatar), bấm ra ngoài thì đóng
        private void OpenMemberMenu(TeamMemberRow row)
        {
            if (memberMenu == null || row == null) return;
            _menuMember = row.Member;
            if (memberMenuBlocker != null)
            {
                memberMenuBlocker.gameObject.SetActive(true);
                memberMenuBlocker.transform.SetAsLastSibling();
            }
            memberMenu.gameObject.SetActive(true);
            memberMenu.SetAsLastSibling();
            PlaceMenu(row.AvatarRect);
            if (removeMemberButton != null)
            {
                // Đội giả không có thành viên thật để xoá (cần server); chỉ đội trưởng mới xoá được
                removeMemberButton.interactable = false;
                CanvasGroup g = removeMemberButton.GetComponent<CanvasGroup>();
                if (g != null) g.alpha = removeDisabledAlpha;
            }
            memberMenu.DOKill();
            memberMenu.localScale = Vector3.one * 0.3f;
            memberMenu.DOScale(1f, menuPopDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(memberMenu.gameObject);
        }

        // Mũi nhọn (pivot) ở đáy avatar; menu không tràn ra ngoài trang theo chiều dọc
        private void PlaceMenu(RectTransform avatarRect)
        {
            var parent = (RectTransform)memberMenu.parent;
            Vector3[] corners = new Vector3[4];
            avatarRect.GetWorldCorners(corners);
            Vector3 bottom = (corners[0] + corners[3]) * 0.5f;
            Vector2 local = parent.InverseTransformPoint(bottom);
            Rect area = parent.rect;
            Vector2 size = memberMenu.rect.size;
            float minY = area.yMin + size.y * memberMenu.pivot.y;
            local.y = Mathf.Max(local.y, minY);
            local.x = Mathf.Clamp(local.x, area.xMin + size.x * memberMenu.pivot.x, area.xMax - size.x * (1f - memberMenu.pivot.x));
            memberMenu.anchorMin = memberMenu.anchorMax = new Vector2(0.5f, 0.5f);
            memberMenu.anchoredPosition = local - area.center;
        }

        private void CloseMemberMenu()
        {
            if (memberMenuBlocker != null) memberMenuBlocker.gameObject.SetActive(false);
            if (memberMenu == null) return;
            memberMenu.DOKill();
            memberMenu.gameObject.SetActive(false);
        }

        private void ViewMemberProfile()
        {
            TeamMember m = _menuMember;
            CloseMemberMenu();
            if (_flow == null) return;
            if (m.IsLocal) _flow.OpenProfile();
            else _flow.ShowProfile(MemberProfile(m));
        }

        // Hồ sơ giả cho thành viên giả: số liệu cố định theo tên, tăng theo cấp
        private static PlayerProfile MemberProfile(TeamMember m)
        {
            int h = 17;
            foreach (char c in m.Name) h = h * 31 + c;
            h &= int.MaxValue;
            int lv = Mathf.Max(1, m.Level);
            ProfileStat[] stats =
            {
                new ProfileStat(ProfileStatType.FirstTryWins, lv * (40 + h % 30) / 100),
                new ProfileStat(ProfileStatType.CoinsEarned, lv * (90 + h % 60)),
                new ProfileStat(ProfileStatType.LevelsCleared, lv - 1),
                new ProfileStat(ProfileStatType.HardWins, lv * (8 + h % 10) / 100),
                new ProfileStat(ProfileStatType.BoostersUsed, lv * (20 + h % 40) / 100),
                new ProfileStat(ProfileStatType.BestWinStreak, 3 + h % 25)
            };
            long joined = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (30 + h % 700) * 86400L;
            return new PlayerProfile((10000000 + h % 90000000).ToString(), m.Name, m.Avatar, joined, lv, stats, false);
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

        // ---------- Đội của tôi ----------

        private void RefreshMyTeam(bool scrollToEnd)
        {
            TeamInfo team = _teams.MyTeam;
            if (team == null) return;
            if (myLogo != null) myLogo.sprite = _teams.Database.Logo(team.logo);
            if (myName != null) myName.SetText(team.name);
            FillFeed();
            RefreshRequestButton();
            if (scrollToEnd && feedScroll != null && feedContent != null)
            {
                // dòng mới vừa sinh: dựng lại layout ngay rồi cuộn xuống cuối; chữ tự co giãn có thể đổi chiều cao ở frame sau nên cuộn lại vài frame
                LayoutRebuilder.ForceRebuildLayoutImmediate(feedContent);
                feedScroll.verticalNormalizedPosition = 0f;
                _scrollEndFrames = 3;
            }
        }

        // Bảng tin dựng lại toàn bộ (≤ feedMax dòng, chỉ khi có thay đổi)
        private void FillFeed()
        {
            if (feedContent == null) return;
            foreach (GameObject g in _feedRows)
            {
                g.SetActive(false); // Destroy chờ hết frame: tắt ngay để layout không tính dòng cũ khi cuộn xuống cuối
                Destroy(g);
            }
            _feedRows.Clear();
            long now = _teams.Now;
            int lifeMax = _teams.Database.lifeRequestMax;
            foreach (TeamFeedItem it in _teams.Feed)
            {
                TeamFeedRow template = it.Type == TeamFeedType.Joined ? noticeTemplate
                    : it.Type == TeamFeedType.LifeRequest ? requestTemplate
                    : it.mine ? chatMineTemplate : chatOtherTemplate;
                if (template == null) continue;
                TeamFeedRow row = Instantiate(template, feedContent);
                row.gameObject.SetActive(true);
                row.Set(it, Avatar(it.avatar), now, lifeMax);
                _feedRows.Add(row.gameObject);
            }
        }

        private void RefreshRequestButton()
        {
            if (requestButton == null) return;
            long left = _teams.LifeRequestCooldownLeft();
            bool ready = left <= 0;
            Sprite bg = ready ? requestReadySprite : requestWaitSprite;
            if (bg != null && requestButton.image != null) requestButton.image.sprite = bg;
            if (requestTimer != null) requestTimer.SetActive(!ready);
            if (requestTimerText != null && !ready) requestTimerText.SetText(TeamService.FormatCooldown(left));
        }

        private void OnRequest()
        {
            if (!_teams.RequestLives())
            {
                Notice("You can ask for lives again in " + TeamService.FormatCooldown(_teams.LifeRequestCooldownLeft()) + ".");
                return;
            }
            RefreshMyTeam(true);
        }

        private void ToggleChat()
        {
            if (chatBar == null) return;
            bool on = !chatBar.activeSelf;
            chatBar.SetActive(on);
            if (on && chatInput != null) chatInput.ActivateInputField();
        }

        private void SendChat()
        {
            if (chatInput == null) return;
            if (_teams.SendMessage(chatInput.text))
            {
                chatInput.text = "";
                if (chatBar != null) chatBar.SetActive(false);
                RefreshMyTeam(true);
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
                    Show(Page.MyTeam);
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

        private Sprite Avatar(int id) => avatars != null ? avatars.Get(id) : null;

        private void Notice(string message)
        {
            if (_flow != null) _flow.ShowNotice(message, "TEAMS");
        }
    }
}
