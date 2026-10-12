#if !DISABLE_CHEATS
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Economy;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Cheat Panel trong game (chạy cả bản build APK): bấm nhanh 3 lần vào góc trên trái màn hình
    /// (hoặc phím F1 / ` khi có bàn phím) để mở. Gọi DebugCheats nên giống hệt Debug Panel trong Editor.
    /// Vẽ bằng IMGUI, tự tạo lúc mở game, không cần đặt gì vào scene. Lúc mở có 1 Canvas trong suốt phủ
    /// toàn màn hình để chặn chạm xuống UI / lưới bên dưới.
    /// Muốn bỏ hẳn khỏi bản phát hành: thêm Scripting Define Symbol DISABLE_CHEATS.
    /// </summary>
    public class CheatPanel : MonoBehaviour
    {
        // Vùng bấm mở panel: góc trên trái (tỉ lệ theo màn hình), đủ 3 lần, mỗi lần cách nhau không quá TapGap giây
        private const float ZoneWidth = 0.25f;
        private const float ZoneHeight = 0.08f;
        private const int TapsToOpen = 3;
        private const float TapGap = 0.5f;

        private const float RefWidth = 720f; // kích thước tham chiếu để phóng chữ / nút theo màn hình
        private const string PrefShowFps = "Debug_ShowFps";

        private static readonly string[] Tabs = { "Level", "Tài nguyên", "Booster", "Khác" };
        private static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
        private static readonly int[] FpsOptions = { 30, 60, 90, 120 };

        private bool _open;
        private int _tab;
        private int _taps;
        private float _lastTap;
        private int _levelInput = 1;
        private Vector2 _scroll;

        private bool _showFps;
        private float _fpsTimer;
        private int _fpsFrames;
        private float _fps;

        private GameObject _blocker;
        private bool _stylesReady;
        private float _stylesScale;
        private GUIStyle _box, _title, _label, _small, _button, _tabOn, _tabOff, _fpsStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CheatPanel>() != null) return;
            GameObject go = new GameObject("[CheatPanel]");
            DontDestroyOnLoad(go);
            go.AddComponent<CheatPanel>();
        }

        private void Awake()
        {
            _showFps = PlayerPrefs.GetInt(PrefShowFps, 0) == 1;
        }

        private void OnDestroy()
        {
            if (_blocker != null) Destroy(_blocker);
        }

        private void Update()
        {
            UpdateFps();
            if (!_open) DetectOpenGesture();
        }

        // ---------- Mở panel ----------

        private void DetectOpenGesture()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame))
            {
                SetOpen(true);
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            Vector2 p = pointer.position.ReadValue();
            bool inZone = p.x <= Screen.width * ZoneWidth && p.y >= Screen.height * (1f - ZoneHeight);
            if (!inZone)
            {
                _taps = 0;
                return;
            }

            float now = Time.unscaledTime;
            _taps = now - _lastTap <= TapGap ? _taps + 1 : 1;
            _lastTap = now;
            if (_taps < TapsToOpen) return;
            _taps = 0;
            SetOpen(true);
        }

        private void SetOpen(bool open)
        {
            _open = open;
            if (open)
            {
                _levelInput = Mathf.Max(1, DebugCheats.CurrentLevel);
                _scroll = Vector2.zero;
            }
            SetBlocker(open);
        }

        // Canvas trong suốt sortingOrder cao nhất: Button / popup bên dưới không nhận chạm,
        // InputManager thấy con trỏ đang trên UI nên cũng không bắt đầu kéo trên lưới
        private void SetBlocker(bool on)
        {
            if (on && _blocker == null)
            {
                _blocker = new GameObject("[CheatPanel Blocker]", typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
                DontDestroyOnLoad(_blocker);
                Canvas canvas = _blocker.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                _blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            }
            if (_blocker != null) _blocker.SetActive(on);
        }

        // ---------- FPS ----------

        private void UpdateFps()
        {
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer < 0.5f) return;
            _fps = _fpsFrames / _fpsTimer;
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }

        // ---------- Vẽ ----------

        private void OnGUI()
        {
            if (!_open && !_showFps) return;

            float scale = Mathf.Max(0.75f, Mathf.Min(Screen.width, Screen.height) / RefWidth);
            BuildStyles(scale);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            // Chừa tai thỏ / thanh trạng thái (safeArea tính từ dưới lên)
            Rect safe = Screen.safeArea;
            float top = (Screen.height - safe.yMax) / scale;
            float bottom = safe.yMin / scale;

            if (_showFps)
            {
                GUI.Label(new Rect(w - 190f, top + 6f, 180f, 34f), $"{_fps:0} FPS", _fpsStyle);
            }

            if (!_open) return;

            Rect panel = new Rect(12f, top + 12f, w - 24f, h - top - bottom - 24f);
            GUI.Box(panel, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, panel.height - 24f));

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Cheat Panel", _title);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Đóng ✕", _button, GUILayout.Width(150f))) SetOpen(false);
            }
            GUILayout.Label(StatusText(), _small);
            GUILayout.Space(6f);

            using (new GUILayout.HorizontalScope())
            {
                for (int i = 0; i < Tabs.Length; i++)
                {
                    if (GUILayout.Button(Tabs[i], i == _tab ? _tabOn : _tabOff))
                    {
                        _tab = i;
                        _scroll = Vector2.zero;
                    }
                }
            }
            GUILayout.Space(8f);

            HandleDragScroll();
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUIStyle.none);
            switch (_tab)
            {
                case 0: DrawLevel(); break;
                case 1: DrawResources(); break;
                case 2: DrawBoosters(); break;
                default: DrawOther(); break;
            }
            GUILayout.Space(20f);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // IMGUI không tự cuộn bằng ngón tay: kéo dọc thì dịch nội dung
        private void HandleDragScroll()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag)
            {
                _scroll.y = Mathf.Max(0f, _scroll.y - e.delta.y);
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                _scroll.y = Mathf.Max(0f, _scroll.y + e.delta.y * 20f);
                e.Use();
            }
        }

        private static string StatusText()
        {
            string screen = DebugCheats.InMenu ? "Main Menu" : "Trong ván";
            LevelManager lm = LevelManager.Instance;
            string state = lm != null ? lm.State.ToString() : "-";
            EnergyManager en = EnergyManager.Instance;
            string energy = en != null ? (en.IsInfinite ? "∞" : $"{en.Current}/{en.Max}") : "-";
            return $"Level {DebugCheats.CurrentLevel}/{DebugCheats.LevelCount} · {screen} · {state}\n" +
                   $"Coin {DebugCheats.CoinBalance} · ★ {DebugCheats.StarBalance} · Giờ {DebugCheats.TimerStatus} · Năng lượng {energy}";
        }

        // ---------- Tab: Level + thắng / thua ----------

        private void DrawLevel()
        {
            Header("Level");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("◀ Trước")) DebugCheats.PreviousLevel();
                if (Btn("Chơi lại")) DebugCheats.RetryLevel();
                if (Btn("Sau ▶")) DebugCheats.NextLevel();
            }

            int max = Mathf.Max(1, DebugCheats.LevelCount);
            _levelInput = Mathf.Clamp(_levelInput, 1, max);
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("-10")) _levelInput = Mathf.Max(1, _levelInput - 10);
                if (Btn("-1")) _levelInput = Mathf.Max(1, _levelInput - 1);
                GUILayout.Label($"Level {_levelInput}", _title, GUILayout.Width(170f));
                if (Btn("+1")) _levelInput = Mathf.Min(max, _levelInput + 1);
                if (Btn("+10")) _levelInput = Mathf.Min(max, _levelInput + 10);
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Đổi level")) DebugCheats.GoToLevel(_levelInput);
                if (Btn("Đổi + vào ván")) { DebugCheats.PlayLevel(_levelInput); SetOpen(false); }
            }
            if (Btn("Vào ván level hiện tại (bỏ qua năng lượng)")) { DebugCheats.PlayLevel(DebugCheats.CurrentLevel); SetOpen(false); }

            Header("Thắng / Thua");
            bool canPlay = !DebugCheats.InMenu && LevelManager.Instance != null && LevelManager.Instance.State == GameState.Playing;
            GUI.enabled = canPlay;
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Thắng ngay")) { DebugCheats.WinNow(); SetOpen(false); }
                if (Btn("Đặt 1 khối đúng")) DebugCheats.SolveOneBlock();
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Hết giờ")) { DebugCheats.TimeUp(); SetOpen(false); }
                if (Btn("Thua hẳn")) { DebugCheats.LoseNow(); SetOpen(false); }
            }
            GUI.enabled = true;
            if (!canPlay) GUILayout.Label("Chỉ dùng được khi đang chơi trong ván.", _small);

            Header("Tutorial");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Chơi lại tutorial")) { DebugCheats.PlayTutorial(); SetOpen(false); }
                if (Btn("Bỏ cờ đã xem tutorial")) DebugCheats.ResetTutorial();
            }
            if (Btn("Xem lại popup cơ chế")) DebugCheats.ResetMechanicIntros();
        }

        // ---------- Tab: Coin / đồng hồ / năng lượng ----------

        private void DrawResources()
        {
            Header($"Coin · {DebugCheats.CoinBalance}");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("+100")) DebugCheats.AddCoins(100);
                if (Btn("+1000")) DebugCheats.AddCoins(1000);
                if (Btn("+10000")) DebugCheats.AddCoins(10000);
                if (Btn("= 0")) DebugCheats.SetCoins(0);
            }

            Header($"Sao ★ (Vòng quay may mắn) · {DebugCheats.StarBalance}");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("+10")) DebugCheats.AddStars(10);
                if (Btn("+50")) DebugCheats.AddStars(50);
                if (Btn("-10")) DebugCheats.AddStars(-10);
                if (Btn("= 0")) DebugCheats.SetStars(0);
            }

            Header($"Đồng hồ trong ván · {DebugCheats.TimerStatus}");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("-10s")) DebugCheats.AddTime(-10f);
                if (Btn("+10s")) DebugCheats.AddTime(10f);
                if (Btn("+60s")) DebugCheats.AddTime(60f);
                if (Btn("Đầy giờ")) DebugCheats.FullTime();
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn(DebugCheats.TimerPaused ? "Chạy đồng hồ" : "Dừng đồng hồ")) DebugCheats.ToggleTimerPause();
                if (Btn("Đóng băng")) DebugCheats.Freeze();
            }

            Header("Năng lượng");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("+1")) DebugCheats.AddEnergy(1);
                if (Btn("Đầy")) DebugCheats.EnergyFull();
                if (Btn("Hết (0)")) DebugCheats.EnergyEmpty();
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Bỏ qua 1 lần chờ")) DebugCheats.EnergySkipRefill();
                if (Btn("Vô hạn 5 phút")) DebugCheats.EnergyInfinite(5);
            }
        }

        // ---------- Tab: Booster ----------

        private void DrawBoosters()
        {
            Header("Booster");
            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{type}: {DebugCheats.BoosterCount(type)}", _label, GUILayout.Width(200f));
                    if (Btn("+1")) DebugCheats.AddBooster(type, 1);
                    if (Btn("+10")) DebugCheats.AddBooster(type, 10);
                    if (Btn("= 0")) DebugCheats.SetBooster(type, 0);
                }
            }
            if (Btn("Tất cả +10"))
            {
                foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType))) DebugCheats.AddBooster(type, 10);
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Mở khóa tất cả")) DebugCheats.UnlockAllBoosters();
                if (Btn("Khóa lại tất cả")) DebugCheats.LockAllBoosters();
            }
        }

        // ---------- Tab: Khác ----------

        private void DrawOther()
        {
            Header($"FPS mục tiêu: {(FrameRateSetup.TargetFps > 0 ? FrameRateSetup.TargetFps.ToString() : "mặc định")} · đang chạy {_fps:0}");
            using (new GUILayout.HorizontalScope())
            {
                foreach (int fps in FpsOptions)
                {
                    if (Btn(fps.ToString())) FrameRateSetup.Apply(fps);
                }
            }
            if (Btn(_showFps ? "Ẩn bộ đếm FPS" : "Hiện bộ đếm FPS"))
            {
                _showFps = !_showFps;
                PlayerPrefs.SetInt(PrefShowFps, _showFps ? 1 : 0);
                PlayerPrefs.Save();
            }

            Header($"Tốc độ game: x{Time.timeScale:0.##}");
            using (new GUILayout.HorizontalScope())
            {
                foreach (float s in Speeds)
                {
                    if (Btn("x" + s)) Time.timeScale = s;
                }
            }

            Header("Thử rung");
            GUILayout.Label($"{Haptics.Status}\nĐã gọi rung: {Haptics.PlayCount} lần · cách rung lần cuối: {Haptics.LastMethod}" +
                            (string.IsNullOrEmpty(Haptics.LastError) ? "" : $"\nLỗi: {Haptics.LastError}"), _small);
            // 3 nút 1 hàng cho vừa màn điện thoại
            System.Array types = System.Enum.GetValues(typeof(HapticType));
            for (int i = 0; i < types.Length; i += 3)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int j = i; j < Mathf.Min(i + 3, types.Length); j++)
                    {
                        HapticType type = (HapticType)types.GetValue(j);
                        if (Btn(type.ToString())) Haptics.Play(type); // gọi thẳng, bỏ qua cài đặt Rung
                    }
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Rung dài (kiểu cũ)")) Haptics.PlayLegacy();
                if (Btn($"Cài đặt Rung: {(AudioManager.VibrateOn ? "BẬT" : "TẮT")}")) AudioManager.VibrateOn = !AudioManager.VibrateOn;
            }

            Header("Điểm danh");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Sang ngày mới")) DebugCheats.DailyNextDay();
                if (Btn("Reset chu kỳ")) DebugCheats.DailyReset();
            }

            Header("Save");
            using (new GUILayout.HorizontalScope())
            {
                if (Btn("Lưu save ngay")) DebugCheats.SaveNow();
                if (Btn("Thử mua IAP")) DebugCheats.TryIAP();
            }
        }

        // ---------- Style ----------

        private bool Btn(string text) => GUILayout.Button(text, _button);

        private void Header(string text)
        {
            GUILayout.Space(10f);
            GUILayout.Label(text, _label);
        }

        private void BuildStyles(float scale)
        {
            if (_stylesReady && Mathf.Approximately(_stylesScale, scale)) return;
            _stylesReady = true;
            _stylesScale = scale;

            Texture2D bg = Solid(new Color(0.08f, 0.09f, 0.12f, 0.96f));
            Texture2D btn = Solid(new Color(0.22f, 0.25f, 0.32f, 1f));
            Texture2D btnDown = Solid(new Color(0.35f, 0.55f, 0.9f, 1f));
            Texture2D tabOn = Solid(new Color(0.3f, 0.5f, 0.85f, 1f));

            _box = new GUIStyle { normal = { background = bg } };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = Color.white } };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.85f, 0.4f) } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, normal = { textColor = new Color(0.8f, 0.82f, 0.88f) } };
            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 21,
                fixedHeight = 60f,
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = btn, textColor = Color.white },
                hover = { background = btn, textColor = Color.white },
                active = { background = btnDown, textColor = Color.white },
            };
            _tabOff = new GUIStyle(_button) { fontStyle = FontStyle.Bold };
            _tabOn = new GUIStyle(_tabOff) { normal = { background = tabOn, textColor = Color.white }, hover = { background = tabOn, textColor = Color.white } };
            _fpsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = Solid(new Color(0f, 0f, 0f, 0.55f)), textColor = Color.green },
            };
        }

        private static Texture2D Solid(Color c)
        {
            Texture2D t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
#endif
