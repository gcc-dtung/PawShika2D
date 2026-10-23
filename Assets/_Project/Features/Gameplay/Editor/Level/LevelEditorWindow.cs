using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PawBlock.Core.Foundation;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Tools → PawBlock → Level Editor: chỉnh sửa / xếp một level (LevelDefinition) trực quan.
    /// - Chọn level theo LevelDatabase (◀ ▶, danh sách) hoặc kéo thả asset bất kỳ; tạo mới, nhân bản, đổi thứ tự trong database.
    /// - Công cụ trên lưới: Khối (kéo hình chữ nhật -> 1 khối + số = diện tích, đặt ở ô bắt đầu kéo), Số (thêm / chọn số),
    ///   Void (bật / tắt ô bỏ trống, kéo để tô), Tường (bấm gần cạnh ô: bật / tắt tường cạnh đó), Xóa. Chuột phải = Xóa.
    /// - Số đang chọn: giá trị, hướng, khóa, "?", góc / cạnh, màu.
    /// - Kiểm tra: LevelValidator (dữ liệu, có lời giải, không cần đoán) + lời giải duy nhất; Giải: lưu lời giải của solver.
    /// - Chơi thử: ghi level vào save (section "gameplay"), vào Play mode thẳng level đang sửa.
    /// Lưới vẽ y = 0 ở dưới cùng như trong game. Mọi thay đổi có Undo.
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/_Project/Features/Gameplay/Configs/Levels/LevelDatabase.asset";
        public const string CustomFolder = "Assets/_Project/Features/Gameplay/Configs/Levels/Custom";
        private const int MaxSize = 12;

        private enum Tool { Block, Clue, Void, Erase, Wall, Corner }

        private static readonly string[] ToolNames = { "Khối", "Số", "Void", "Xóa", "Tường", "Góc / cạnh" };
        private static readonly string[] ToolTips =
        {
            "Kéo hình chữ nhật: tạo 1 khối, số = diện tích, đặt ở ô bắt đầu kéo (xóa khối / số / Void cũ trong vùng)",
            "Bấm ô: chọn số có sẵn hoặc thêm số mới",
            "Bấm / kéo: bật tắt ô Void (bỏ trống)",
            "Bấm ô: xóa số và khối chứa ô đó",
            "Bấm gần cạnh ô: bật / tắt tường ở cạnh đó (khối không được vắt qua)",
            "Bấm vào ô số: góc ô = đòi số nằm ở góc khối, giữa cạnh ô = đòi số nằm ở cạnh khối, giữa ô = bỏ dấu (bấm lại đúng chỗ cũ cũng bỏ)"
        };

        private static readonly Color WallColor = new Color(0.91f, 0.67f, 0.41f);
        private static readonly Color CornerColor = new Color(0.86f, 0.82f, 1f);

        private static readonly Color[] Palette =
        {
            new Color(0.55f, 0.80f, 0.95f), new Color(0.65f, 0.88f, 0.55f), new Color(0.75f, 0.65f, 0.95f),
            new Color(0.98f, 0.62f, 0.62f), new Color(0.98f, 0.85f, 0.45f), new Color(0.98f, 0.70f, 0.85f),
            new Color(0.55f, 0.90f, 0.85f), new Color(0.98f, 0.72f, 0.45f), new Color(0.70f, 0.75f, 0.98f)
        };

        private LevelDatabase _database;
        private LevelDefinition _level;
        private Tool _tool = Tool.Block;
        private int _selectedClue = -1;
        private int _newWidth = 5, _newHeight = 5;
        private Vector2 _scroll;

        // Kéo chuột trên lưới
        private bool _dragging;
        private Vector2Int _dragStart, _dragCurrent;
        private bool _voidPaintValue;

        // Kết quả kiểm tra (xóa khi level đổi)
        private List<string> _errors = new List<string>();
        private List<string> _warnings = new List<string>();
        private List<string> _infos = new List<string>();

        [MenuItem("Tools/PawBlock/Level Editor", priority = 1)]
        public static void Open()
        {
            LevelEditorWindow w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(420, 560);
        }

        /// <summary>Mở editor với level chỉ định (vd. từ Inspector).</summary>
        public static void Open(LevelDefinition level)
        {
            Open();
            GetWindow<LevelEditorWindow>().SetLevel(level);
        }

        private void OnEnable()
        {
            if (_database == null) _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (_level == null && Selection.activeObject is LevelDefinition sel) _level = sel;
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelDefinition sel && sel != _level)
            {
                SetLevel(sel);
                Repaint();
            }
        }

        private void SetLevel(LevelDefinition level)
        {
            _level = level;
            _selectedClue = -1;
            ClearReport();
            if (_level != null)
            {
                _newWidth = _level.size;
                _newHeight = _level.Height;
            }
        }

        // =====================================================================
        // GUI
        // =====================================================================

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawLevelPicker();
            if (_level == null)
            {
                EditorGUILayout.HelpBox("Chọn một level trong database, kéo thả LevelDefinition vào ô Level, hoặc bấm Tạo mới.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(4);
            DrawSizeRow();
            EditorGUILayout.Space(4);
            DrawToolbar();
            DrawStatus();
            DrawGrid();
            DrawSelectedClue();
            EditorGUILayout.Space(6);
            DrawActions();
            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelPicker()
        {
            EditorGUILayout.BeginHorizontal();
            _database = (LevelDatabase)EditorGUILayout.ObjectField("Database", _database, typeof(LevelDatabase), false);
            EditorGUILayout.EndHorizontal();

            int index = DatabaseIndex();
            if (_database != null && _database.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(index <= 0))
                {
                    if (GUILayout.Button("◀", GUILayout.Width(28))) SetLevel(_database.levels[index - 1]);
                }
                string[] names = new string[_database.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    LevelDefinition l = _database.levels[i];
                    names[i] = $"{i + 1}. {(l != null ? l.name : "(trống)")}";
                }
                int picked = EditorGUILayout.Popup(index, names);
                if (picked != index && picked >= 0) SetLevel(_database.levels[picked]);
                using (new EditorGUI.DisabledScope(index < 0 || index >= _database.Count - 1))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28))) SetLevel(_database.levels[index + 1]);
                }
                EditorGUILayout.EndHorizontal();
            }

            LevelDefinition other = (LevelDefinition)EditorGUILayout.ObjectField("Level", _level, typeof(LevelDefinition), false);
            if (other != _level) SetLevel(other);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Tạo mới", "Level trống 5x5 trong Levels/Custom, chèn sau level đang chọn"))) CreateLevel(null);
            using (new EditorGUI.DisabledScope(_level == null))
            {
                if (GUILayout.Button(new GUIContent("Nhân bản", "Bản sao trong Levels/Custom, chèn ngay sau"))) CreateLevel(_level);
            }
            using (new EditorGUI.DisabledScope(_database == null || index < 0))
            {
                if (GUILayout.Button(new GUIContent("▲ Lên", "Đổi thứ tự trong database"), GUILayout.Width(56))) MoveInDatabase(index, -1);
                if (GUILayout.Button(new GUIContent("▼ Xuống", "Đổi thứ tự trong database"), GUILayout.Width(66))) MoveInDatabase(index, 1);
            }
            using (new EditorGUI.DisabledScope(_database == null || _level == null))
            {
                if (index < 0)
                {
                    if (GUILayout.Button(new GUIContent("Thêm vào DB", "Thêm vào cuối database"))) AddToDatabase(_database.Count);
                }
                else if (GUILayout.Button(new GUIContent("Bỏ khỏi DB", "Chỉ bỏ khỏi danh sách, không xóa asset")))
                {
                    Undo.RecordObject(_database, "Remove Level From Database");
                    _database.levels.RemoveAt(index);
                    EditorUtility.SetDirty(_database);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_level != null && index < 0 && _database != null)
                EditorGUILayout.HelpBox("Level chưa có trong database: không chơi thử được.", MessageType.None);
            if (_level != null && AssetDatabase.GetAssetPath(_level).Contains("/ShikaCat/"))
                EditorGUILayout.HelpBox("Level ShikaCat: chạy lại Import ShikaCat Levels sẽ ghi đè chỉnh sửa. Muốn giữ lâu dài thì Nhân bản sang Custom.", MessageType.Warning);
            if (_level != null && AssetDatabase.GetAssetPath(_level).StartsWith(MechanicInjectorWindow.OutputFolder + "/"))
                EditorGUILayout.HelpBox("Level do Mechanic Injector sinh: Áp dụng lại tool sẽ ghi đè chỉnh sửa. Muốn giữ lâu dài thì Nhân bản sang Custom.", MessageType.Warning);
        }

        private void DrawSizeRow()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Kích thước", GUILayout.Width(70));
            _newWidth = Mathf.Clamp(EditorGUILayout.IntField(_newWidth, GUILayout.Width(36)), 1, MaxSize);
            EditorGUILayout.LabelField("x", GUILayout.Width(12));
            _newHeight = Mathf.Clamp(EditorGUILayout.IntField(_newHeight, GUILayout.Width(36)), 1, MaxSize);
            using (new EditorGUI.DisabledScope(_newWidth == _level.size && _newHeight == _level.Height))
            {
                if (GUILayout.Button(new GUIContent("Áp dụng", "Đổi kích thước; số / khối / Void ra ngoài lưới bị bỏ"), GUILayout.Width(70))) Resize(_newWidth, _newHeight);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(new GUIContent("Độ khó", "Hard / Very Hard: nút PLAY, khung HUD riêng và hiệu ứng vào màn"), GUILayout.Width(44));
            LevelDifficulty diff = (LevelDifficulty)EditorGUILayout.EnumPopup(_level.difficulty, GUILayout.Width(80));
            if (diff != _level.difficulty)
            {
                Record("Difficulty");
                _level.difficulty = diff;
            }
            int auto = LevelTimer.EstimateAutoSeconds(ClueCount(), _level.size * _level.Height - VoidSet().Count, AssetDatabase.LoadAssetAtPath<LivesEnergyConfig>("Assets/_Project/Features/Gameplay/Configs/LivesEnergyConfig.asset"));
            EditorGUILayout.LabelField(new GUIContent("Thời gian (s)",
                $"Số giây của level. 0 = tự tính: 8 s + 0.25 s/ô + 5.5–6.5 s/khối, tối thiểu 45 s (hiện khoảng {auto} s)"), GUILayout.Width(80));
            int limit = EditorGUILayout.IntField(_level.rules.timeLimit, GUILayout.Width(40));
            if (limit != _level.rules.timeLimit)
            {
                Record("Time Limit");
                _level.rules.timeLimit = Mathf.Max(0, limit);
            }
            EditorGUILayout.LabelField(_level.rules.timeLimit > 0 ? FormatTime(_level.rules.timeLimit) : $"tự động ~{FormatTime(auto)}",
                EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }

        private static string FormatTime(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

        private void DrawToolbar()
        {
            GUIContent[] contents = new GUIContent[ToolNames.Length];
            for (int i = 0; i < contents.Length; i++) contents[i] = new GUIContent(ToolNames[i], ToolTips[i]);
            _tool = (Tool)GUILayout.Toolbar((int)_tool, contents, GUILayout.Height(24));
            EditorGUILayout.LabelField(ToolTips[(int)_tool] + ". Chuột phải = xóa.", EditorStyles.miniLabel);
        }

        private void DrawStatus()
        {
            int playable = _level.size * _level.Height - VoidSet().Count;
            int sum = _level.clues != null ? _level.clues.Sum(c => c.value) : 0;
            int covered = SolutionList().Sum(r => r.width * r.height);
            string text = $"Tổng số: {sum} / ô chơi được: {playable}   ·   Số: {ClueCount()}   ·   Khối đã xếp phủ {covered} ô";
            MessageType type = sum == playable ? MessageType.None : MessageType.Warning;
            EditorGUILayout.HelpBox(text, type);
        }

        // =====================================================================
        // Lưới
        // =====================================================================

        private void DrawGrid()
        {
            int w = _level.size, h = _level.Height;
            float avail = position.width - 24f;
            float cell = Mathf.Clamp(Mathf.Floor(avail / w), 18f, 56f);
            Rect area = GUILayoutUtility.GetRect(cell * w, cell * h, GUILayout.ExpandWidth(false));
            area.x += Mathf.Max(0f, (avail - cell * w) / 2f);
            area.width = cell * w;
            area.height = cell * h;

            HandleGridInput(area, cell);
            if (Event.current.type != EventType.Repaint) return;

            HashSet<Vector2Int> voids = VoidSet();
            EditorGUI.DrawRect(area, new Color(0.16f, 0.16f, 0.2f));

            // Ô nền
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Rect r = CellRect(area, cell, x, y);
                    bool isVoid = voids.Contains(new Vector2Int(x, y));
                    EditorGUI.DrawRect(Inset(r, 1f), isVoid ? new Color(0.1f, 0.1f, 0.12f) : new Color(0.32f, 0.32f, 0.38f));
                    if (isVoid) GUI.Label(r, "×", CenterStyle(cell * 0.4f, new Color(1f, 1f, 1f, 0.25f)));
                }
            }

            // Khối đã xếp (lời giải lưu trong level)
            foreach (SolutionRectData s in SolutionList())
            {
                Rect r = BlockRect(area, cell, s.x, s.y, s.width, s.height);
                Color c = Palette[Mathf.Abs(s.clueId) % Palette.Length];
                EditorGUI.DrawRect(Inset(r, 2f), new Color(c.r, c.g, c.b, 0.55f));
                DrawOutline(Inset(r, 2f), c, 2f);
            }

            // Khung đang kéo (công cụ Khối)
            if (_dragging && _tool == Tool.Block)
            {
                RectInt d = DragRect();
                Rect r = BlockRect(area, cell, d.x, d.y, d.width, d.height);
                EditorGUI.DrawRect(r, new Color(1f, 1f, 1f, 0.18f));
                DrawOutline(r, Color.white, 2f);
                GUI.Label(r, (d.width * d.height).ToString(), CenterStyle(cell * 0.5f, new Color(1f, 1f, 1f, 0.8f)));
            }

            // Số
            if (_level.clues != null)
            {
                for (int i = 0; i < _level.clues.Length; i++)
                {
                    ClueData c = _level.clues[i];
                    if (c.x < 0 || c.y < 0 || c.x >= w || c.y >= h) continue;
                    Rect r = CellRect(area, cell, c.x, c.y);
                    if (i == _selectedClue) DrawOutline(Inset(r, 1f), new Color(1f, 0.85f, 0.2f), 3f);
                    GUI.Label(r, ClueLabel(c), CenterStyle(cell * 0.42f, Color.white));
                    // [MECHANIC] Ký hiệu cơ chế trên lưới Level Editor
                    string mark = ConstraintMark(c.mechanic.constraint);
                    if (mark != null) GUI.Label(new Rect(r.x + 2, r.y, r.width, r.height * 0.45f), mark, CornerStyle(cell * 0.26f));
                    if (c.mechanic.lockSteps > 0)
                        GUI.Label(new Rect(r.x, r.yMax - r.height * 0.42f, r.width - 3, r.height * 0.42f), "L" + c.mechanic.lockSteps, RightStyle(cell * 0.22f));
                    DrawCornerMark(Inset(r, 3f), c.mechanic.corner, Mathf.Max(2f, cell * 0.07f));
                    if (c.mechanic.ambiguity != ClueAmbiguity.None)
                        GUI.Label(new Rect(r.x + 3, r.yMax - r.height * 0.42f, r.width, r.height * 0.42f),
                            AmbiguityRules.Describe(c.mechanic, c.value), CornerStyle(cell * 0.2f));
                }
            }

            // Tường: vạch gỗ trên đường kẻ giữa 2 ô, cọc vuông ở 2 đầu
            if (_level.walls != null)
            {
                float t = Mathf.Max(3f, cell * 0.12f);
                foreach (WallData wall in _level.walls)
                {
                    Rect a = CellRect(area, cell, wall.x, wall.y);
                    Rect line = wall.side == WallSide.Right
                        ? new Rect(a.xMax - t / 2f, a.y, t, a.height)
                        : new Rect(a.x, a.y - t / 2f, a.width, t);
                    EditorGUI.DrawRect(line, WallColor);
                    Vector2 p0 = wall.side == WallSide.Right ? new Vector2(a.xMax, a.y) : new Vector2(a.x, a.y);
                    Vector2 p1 = wall.side == WallSide.Right ? new Vector2(a.xMax, a.yMax) : new Vector2(a.xMax, a.y);
                    foreach (Vector2 p in new[] { p0, p1 }) EditorGUI.DrawRect(new Rect(p.x - t, p.y - t, t * 2f, t * 2f), WallColor);
                }
            }
        }

        // Dấu góc (chữ L ở góc) / cạnh (vạch ở giữa cạnh) như trên ô trong game
        private static void DrawCornerMark(Rect r, ClueCorner corner, float t)
        {
            if (corner == ClueCorner.None) return;
            float arm = r.width * 0.32f;
            bool left = corner == ClueCorner.TopLeft || corner == ClueCorner.BottomLeft;
            bool top = corner == ClueCorner.TopLeft || corner == ClueCorner.TopRight;
            switch (corner)
            {
                case ClueCorner.Top: EditorGUI.DrawRect(new Rect(r.center.x - arm / 2f, r.y, arm, t), CornerColor); return;
                case ClueCorner.Bottom: EditorGUI.DrawRect(new Rect(r.center.x - arm / 2f, r.yMax - t, arm, t), CornerColor); return;
                case ClueCorner.Left: EditorGUI.DrawRect(new Rect(r.x, r.center.y - arm / 2f, t, arm), CornerColor); return;
                case ClueCorner.Right: EditorGUI.DrawRect(new Rect(r.xMax - t, r.center.y - arm / 2f, t, arm), CornerColor); return;
            }
            float x = left ? r.x : r.xMax - arm, y = top ? r.y : r.yMax - t;
            EditorGUI.DrawRect(new Rect(x, y, arm, t), CornerColor);
            float vx = left ? r.x : r.xMax - t, vy = top ? r.y : r.yMax - arm;
            EditorGUI.DrawRect(new Rect(vx, vy, t, arm), CornerColor);
        }

        private void HandleGridInput(Rect area, float cell)
        {
            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            bool inside = area.Contains(e.mousePosition);
            Vector2Int pos = CellAt(area, cell, e.mousePosition);

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown when inside:
                    GUIUtility.hotControl = controlId;
                    if (e.button == 1)
                    {
                        EraseAt(pos);
                    }
                    else if (e.button == 0)
                    {
                        _dragStart = _dragCurrent = pos;
                        switch (_tool)
                        {
                            case Tool.Block:
                                _dragging = true;
                                break;
                            case Tool.Clue:
                                ClickClue(pos);
                                break;
                            case Tool.Void:
                                _voidPaintValue = !VoidSet().Contains(pos);
                                _dragging = true;
                                SetVoid(pos, _voidPaintValue);
                                break;
                            case Tool.Erase:
                                EraseAt(pos);
                                break;
                            case Tool.Wall:
                                ToggleWallAt(area, cell, e.mousePosition, pos);
                                break;
                            case Tool.Corner:
                                SetCornerAt(area, cell, e.mousePosition, pos);
                                break;
                        }
                    }
                    e.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    if (_dragging)
                    {
                        Vector2Int clamped = new Vector2Int(Mathf.Clamp(pos.x, 0, _level.size - 1), Mathf.Clamp(pos.y, 0, _level.Height - 1));
                        if (clamped != _dragCurrent)
                        {
                            _dragCurrent = clamped;
                            if (_tool == Tool.Void) SetVoid(clamped, _voidPaintValue);
                        }
                        Repaint();
                    }
                    e.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    if (_dragging && _tool == Tool.Block) PlaceBlock(DragRect(), _dragStart);
                    _dragging = false;
                    e.Use();
                    break;
            }
        }

        /// <summary>
        /// Công cụ Góc / cạnh: chia ô số thành lưới 3x3 theo vị trí chuột. Ô góc = góc, ô giữa cạnh = cạnh, ô giữa = bỏ dấu.
        /// Bấm lại đúng dấu đang có thì bỏ dấu.
        /// </summary>
        private void SetCornerAt(Rect area, float cell, Vector2 mouse, Vector2Int pos)
        {
            if (_level.clues == null) return;
            int index = System.Array.FindIndex(_level.clues, c => c.x == pos.x && c.y == pos.y);
            if (index < 0) return;

            Rect r = CellRect(area, cell, pos.x, pos.y);
            float fx = (mouse.x - r.x) / r.width, fy = (mouse.y - r.y) / r.height; // fy = 0 ở mép trên màn hình
            bool left = fx < 1f / 3f, right = fx > 2f / 3f, top = fy < 1f / 3f, bottom = fy > 2f / 3f;

            ClueCorner picked;
            if (top && left) picked = ClueCorner.TopLeft;
            else if (top && right) picked = ClueCorner.TopRight;
            else if (bottom && left) picked = ClueCorner.BottomLeft;
            else if (bottom && right) picked = ClueCorner.BottomRight;
            else if (top) picked = ClueCorner.Top;
            else if (bottom) picked = ClueCorner.Bottom;
            else if (left) picked = ClueCorner.Left;
            else if (right) picked = ClueCorner.Right;
            else picked = ClueCorner.None;

            Record("Set Corner");
            ClueData c = _level.clues[index];
            c.mechanic.corner = c.mechanic.corner == picked ? ClueCorner.None : picked;
            _level.clues[index] = c;
            _selectedClue = index;
            ClearReport();
        }

        /// <summary>Cạnh gần chuột nhất của ô <paramref name="pos"/>: bật / tắt tường ở đó (cạnh viền lưới bỏ qua).</summary>
        private void ToggleWallAt(Rect area, float cell, Vector2 mouse, Vector2Int pos)
        {
            Rect r = CellRect(area, cell, pos.x, pos.y);
            float dl = mouse.x - r.x, dr = r.xMax - mouse.x, dt = mouse.y - r.y, db = r.yMax - mouse.y;
            float min = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(dt, db));
            WallData w;
            if (min == dr) w = new WallData { x = pos.x, y = pos.y, side = WallSide.Right };
            else if (min == dl) w = new WallData { x = pos.x - 1, y = pos.y, side = WallSide.Right };
            else if (min == dt) w = new WallData { x = pos.x, y = pos.y, side = WallSide.Top };   // phía trên màn hình = y + 1
            else w = new WallData { x = pos.x, y = pos.y - 1, side = WallSide.Top };
            Vector2Int a = WallRules.CellA(w), b = WallRules.CellB(w);
            if (a.x < 0 || a.y < 0 || b.x >= _level.size || b.y >= _level.Height) return;

            Record("Toggle Wall");
            List<WallData> walls = (_level.walls ?? new WallData[0]).ToList();
            if (!walls.Remove(w)) walls.Add(w);
            _level.walls = walls.ToArray();
            ClearReport();
        }

        private RectInt DragRect()
        {
            int xMin = Mathf.Min(_dragStart.x, _dragCurrent.x), yMin = Mathf.Min(_dragStart.y, _dragCurrent.y);
            int xMax = Mathf.Max(_dragStart.x, _dragCurrent.x), yMax = Mathf.Max(_dragStart.y, _dragCurrent.y);
            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        private Vector2Int CellAt(Rect area, float cell, Vector2 mouse)
        {
            int x = Mathf.FloorToInt((mouse.x - area.x) / cell);
            int row = Mathf.FloorToInt((mouse.y - area.y) / cell);
            return new Vector2Int(x, _level.Height - 1 - row); // y = 0 ở dưới cùng
        }

        private Rect CellRect(Rect area, float cell, int x, int y) => BlockRect(area, cell, x, y, 1, 1);

        private Rect BlockRect(Rect area, float cell, int x, int y, int w, int h)
        {
            float top = area.y + (_level.Height - (y + h)) * cell;
            return new Rect(area.x + x * cell, top, w * cell, h * cell);
        }

        // =====================================================================
        // Số đang chọn
        // =====================================================================

        private void DrawSelectedClue()
        {
            if (_level.clues == null || _selectedClue < 0 || _selectedClue >= _level.clues.Length) return;

            ClueData c = _level.clues[_selectedClue];
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Số #{_selectedClue} tại ({c.x}, {c.y})  ·  id {c.id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            // [MECHANIC] Trường sửa cơ chế của ô số đang chọn (nhớ gán lại vào c.mechanic bên dưới)
            int value = Mathf.Max(1, EditorGUILayout.IntField("Giá trị", c.value));
            ClueConstraint constraint = (ClueConstraint)EditorGUILayout.EnumPopup(new GUIContent("Hướng", "Ngang / dọc / vuông (ô nền sau số)"), c.mechanic.constraint);
            ClueCorner corner = (ClueCorner)EditorGUILayout.EnumPopup(new GUIContent("Góc / cạnh", "Ô số phải nằm ở góc / cạnh này của khối (dấu trên ô)"), c.mechanic.corner);
            int lockSteps = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Khóa (bước)", "Mở sau khi đặt đủ số khối khác"), c.mechanic.lockSteps));
            bool hidden = EditorGUILayout.Toggle(new GUIContent("Ẩn số \"?\"", "Người chơi không thấy số tới khi đặt đúng"), c.mechanic.hidden);
            ClueAmbiguity ambiguity = (ClueAmbiguity)EditorGUILayout.EnumPopup(
                new GUIContent("Số mơ hồ", "Thay số đúng bằng điều kiện: ≥ / ≤ ngưỡng, chẵn, lẻ, hoặc 1 trong 2 giá trị. \"Giá trị\" vẫn là số ô thật"), c.mechanic.ambiguity);
            int ambiguityValue = c.mechanic.ambiguityValue;
            if (ambiguity == ClueAmbiguity.AtLeast || ambiguity == ClueAmbiguity.AtMost || ambiguity == ClueAmbiguity.Either)
            {
                string tip = ambiguity == ClueAmbiguity.Either ? "Giá trị thứ hai (khác Giá trị)" : "Ngưỡng (tính cả ngưỡng)";
                ambiguityValue = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent(ambiguity == ClueAmbiguity.Either ? "  Giá trị 2" : "  Ngưỡng", tip), ambiguityValue));
            }
            int color = EditorGUILayout.IntField(new GUIContent("Màu", "Chỉ số màu (dữ liệu ShikaCat, game hiện chưa dùng)"), c.color);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit Clue");
                c.value = value;
                c.mechanic.constraint = constraint;
                c.mechanic.corner = corner;
                c.mechanic.lockSteps = lockSteps;
                c.mechanic.hidden = hidden;
                c.mechanic.ambiguity = ambiguity;
                c.mechanic.ambiguityValue = ambiguityValue;
                c.color = color;
                _level.clues[_selectedClue] = c;
                ClearReport();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bỏ chọn")) _selectedClue = -1;
            if (GUILayout.Button("Xóa số này")) EraseAt(new Vector2Int(c.x, c.y));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // =====================================================================
        // Nút chức năng
        // =====================================================================

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Kiểm tra", "Dữ liệu, có lời giải, không cần đoán (ô ? / khóa), lời giải duy nhất"), GUILayout.Height(26))) RunCheck();
            if (GUILayout.Button(new GUIContent("Giải", "Tìm lời giải bằng solver và lưu vào level (hiện thành các khối)"), GUILayout.Height(26))) SolveAndStore();
            if (GUILayout.Button(new GUIContent("Xóa khối", "Bỏ các khối đã xếp, giữ nguyên số"), GUILayout.Height(26)))
            {
                Record("Clear Blocks");
                _level.solution = new SolutionRectData[0];
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lưu", GUILayout.Height(26)))
            {
                AssetDatabase.SaveAssetIfDirty(_level);
                if (_database != null) AssetDatabase.SaveAssetIfDirty(_database);
                ShowNotification(new GUIContent("Đã lưu"));
            }
            using (new EditorGUI.DisabledScope(DatabaseIndex() < 0 || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(new GUIContent("▶ Chơi thử", "Lưu rồi vào Play mode, vào thẳng level này"), GUILayout.Height(26))) PlayTest();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReport()
        {
            foreach (string s in _errors) EditorGUILayout.HelpBox(s, MessageType.Error);
            foreach (string s in _warnings) EditorGUILayout.HelpBox(s, MessageType.Warning);
            foreach (string s in _infos) EditorGUILayout.HelpBox(s, MessageType.Info);
        }

        private void ClearReport()
        {
            _errors.Clear();
            _warnings.Clear();
            _infos.Clear();
        }

        private void RunCheck()
        {
            ClearReport();
            LevelValidator.Report report = LevelValidator.Validate(_level);
            _errors.AddRange(report.errors);
            _warnings.AddRange(report.warnings);
            if (!report.IsValid) return;

            switch (CheckUnique(_level, out string detail))
            {
                case Uniqueness.Unique:
                    _infos.Add("Hợp lệ: có đúng 1 lời giải" + (HasHiddenInfo() ? ", giải được không cần đoán." : "."));
                    break;
                case Uniqueness.Multiple:
                    _warnings.Add("Có nhiều lời giải: " + detail + ". Game vẫn nhận mọi cách giải (chỉ tính sai khi lưới hết lời giải), nhưng level kém chặt, người chơi có thể phải đoán.");
                    break;
                default:
                    _warnings.Add("Không chắc lời giải duy nhất (quá giới hạn tìm kiếm).");
                    break;
            }
        }

        private enum Uniqueness { Unique, Multiple, Unknown }

        /// <summary>
        /// Lời giải duy nhất: với mỗi số, thử mọi cách đặt khác cách trong lời giải; còn giải được tiếp = có lời giải thứ 2.
        /// </summary>
        private static Uniqueness CheckUnique(LevelDefinition level, out string detail)
        {
            detail = null;
            if (!ShikakuSolver.TrySolve(level, null, null, null, out List<RectInt> solution, out bool aborted) || aborted)
                return Uniqueness.Unknown;

            bool anyAborted = false;
            for (int i = 0; i < level.clues.Length; i++)
            {
                ClueData c = level.clues[i];
                RectInt mine = solution.FirstOrDefault(r => r.Contains(new Vector2Int(c.x, c.y)));
                foreach (RectInt r in ShikakuSolver.GetCandidates(level, null, i, null, null))
                {
                    if (r.Equals(mine)) continue;
                    bool ok = ShikakuSolver.TrySolve(level, new List<RectInt> { r }, null, null, out _, out bool ab);
                    if (ab) { anyAborted = true; continue; }
                    if (!ok) continue;
                    detail = $"số {c.value} tại ({c.x}, {c.y}) đặt được {mine.width}x{mine.height} từ ({mine.x}, {mine.y}) " +
                             $"hoặc {r.width}x{r.height} từ ({r.x}, {r.y})";
                    return Uniqueness.Multiple;
                }
            }
            return anyAborted ? Uniqueness.Unknown : Uniqueness.Unique;
        }

        private void SolveAndStore()
        {
            ClearReport();
            if (!ShikakuSolver.TrySolve(_level, null, null, null, out List<RectInt> solution, out bool aborted) || solution == null)
            {
                _errors.Add(aborted ? "Quá giới hạn tìm kiếm, chưa tìm được lời giải." : "Không có lời giải.");
                return;
            }

            Record("Solve Level");
            List<SolutionRectData> list = new List<SolutionRectData>();
            foreach (RectInt r in solution)
            {
                int clueId = -1;
                foreach (ClueData c in _level.clues)
                {
                    if (r.Contains(new Vector2Int(c.x, c.y))) { clueId = c.id; break; }
                }
                list.Add(new SolutionRectData { clueId = clueId, x = r.x, y = r.y, width = r.width, height = r.height });
            }
            _level.solution = list.ToArray();
            _infos.Add($"Đã lưu lời giải ({list.Count} khối). Bấm Kiểm tra để xem có duy nhất không.");
        }

        private void PlayTest()
        {
            int index = DatabaseIndex();
            if (index < 0) return;
            AssetDatabase.SaveAssetIfDirty(_level);
            AssetDatabase.SaveAssetIfDirty(_database);
            var save = new JsonFileSaveService(JsonFileSaveService.DefaultPath);
            if (save.NeedsMigration)
            {
                Debug.LogWarning("[LevelEditor] Save còn định dạng cũ: vào Play 1 lần để chuyển sang v2 rồi Play Test lại (level không đổi).");
            }
            else
            {
                save.Get<GameplaySave>(GameplaySave.Key).currentLevel = index;
                save.MarkDirty(); // ngoài Play mode: ghi file ngay
            }
            LevelEditorPlayTest.Request();
            EditorApplication.isPlaying = true;
        }

        // =====================================================================
        // Sửa dữ liệu
        // =====================================================================

        private void Record(string name)
        {
            Undo.RecordObject(_level, name);
            EditorUtility.SetDirty(_level);
        }

        /// <summary>Công cụ Khối: thay mọi thứ trong vùng bằng 1 khối + 1 số (= diện tích) ở ô bắt đầu kéo.</summary>
        private void PlaceBlock(RectInt rect, Vector2Int clueCell)
        {
            Record("Place Block");
            List<ClueData> clues = (_level.clues ?? new ClueData[0]).ToList();
            List<SolutionRectData> blocks = SolutionList();

            // Khối cũ chồng lên vùng mới: bỏ khối và số của nó
            foreach (SolutionRectData b in blocks.Where(b => Overlaps(b, rect)).ToList())
            {
                blocks.Remove(b);
                clues.RemoveAll(c => c.id == b.clueId);
            }
            clues.RemoveAll(c => rect.Contains(new Vector2Int(c.x, c.y)));
            _level.cellOverrides = (_level.cellOverrides ?? new CellOverrideData[0])
                .Where(o => !(o.type == CellType.Void && rect.Contains(new Vector2Int(o.x, o.y)))).ToArray();

            int id = NextClueId(clues);
            clues.Add(new ClueData { id = id, x = clueCell.x, y = clueCell.y, value = rect.width * rect.height });
            blocks.Add(new SolutionRectData { clueId = id, x = rect.x, y = rect.y, width = rect.width, height = rect.height });

            _level.clues = clues.ToArray();
            _level.solution = blocks.ToArray();
            _selectedClue = clues.Count - 1;
            ClearReport();
        }

        private void ClickClue(Vector2Int pos)
        {
            int index = ClueIndexAt(pos);
            if (index >= 0)
            {
                _selectedClue = index;
                return;
            }
            if (VoidSet().Contains(pos)) return;

            Record("Add Clue");
            List<ClueData> clues = (_level.clues ?? new ClueData[0]).ToList();
            // Ô nằm trong 1 khối chưa có số: số mới = diện tích khối đó
            SolutionRectData? block = SolutionList().Cast<SolutionRectData?>()
                .FirstOrDefault(b => Contains(b.Value, pos) && !clues.Any(c => c.id == b.Value.clueId));
            int value = block.HasValue ? block.Value.width * block.Value.height : 1;
            int id = block.HasValue ? block.Value.clueId : NextClueId(clues);
            if (block.HasValue && clues.Any(c => c.id == id)) id = NextClueId(clues);
            clues.Add(new ClueData { id = id, x = pos.x, y = pos.y, value = value });
            _level.clues = clues.ToArray();
            _selectedClue = clues.Count - 1;
            ClearReport();
        }

        private void SetVoid(Vector2Int pos, bool isVoid)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= _level.size || pos.y >= _level.Height) return;
            bool now = VoidSet().Contains(pos);
            if (now == isVoid) return;

            Record("Toggle Void");
            List<CellOverrideData> overrides = (_level.cellOverrides ?? new CellOverrideData[0]).ToList();
            overrides.RemoveAll(o => o.x == pos.x && o.y == pos.y);
            if (isVoid)
            {
                overrides.Add(new CellOverrideData { x = pos.x, y = pos.y, type = CellType.Void });
                RemoveClueAndBlockAt(pos);
                if (_level.walls != null) _level.walls = _level.walls.Where(w => WallRules.CellA(w) != pos && WallRules.CellB(w) != pos).ToArray();
            }
            _level.cellOverrides = overrides.ToArray();
            ClearReport();
        }

        private void EraseAt(Vector2Int pos)
        {
            if (ClueIndexAt(pos) < 0 && !SolutionList().Any(b => Contains(b, pos))) return;
            Record("Erase");
            RemoveClueAndBlockAt(pos);
            ClearReport();
        }

        /// <summary>Bỏ số tại ô và khối chứa ô (cùng số của khối đó).</summary>
        private void RemoveClueAndBlockAt(Vector2Int pos)
        {
            List<ClueData> clues = (_level.clues ?? new ClueData[0]).ToList();
            List<SolutionRectData> blocks = SolutionList();

            foreach (ClueData c in clues.Where(c => c.x == pos.x && c.y == pos.y).ToList())
            {
                clues.Remove(c);
                blocks.RemoveAll(b => b.clueId == c.id);
            }
            foreach (SolutionRectData b in blocks.Where(b => Contains(b, pos)).ToList())
            {
                blocks.Remove(b);
                clues.RemoveAll(c => c.id == b.clueId && Contains(b, new Vector2Int(c.x, c.y)));
            }

            _level.clues = clues.ToArray();
            _level.solution = blocks.ToArray();
            _selectedClue = -1;
        }

        private void Resize(int width, int height)
        {
            Record("Resize Level");
            bool In(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
            _level.size = width;
            _level.height = height;
            _level.clues = (_level.clues ?? new ClueData[0]).Where(c => In(c.x, c.y)).ToArray();
            _level.cellOverrides = (_level.cellOverrides ?? new CellOverrideData[0]).Where(o => In(o.x, o.y)).ToArray();
            _level.solution = SolutionList().Where(b => b.x + b.width <= width && b.y + b.height <= height).ToArray();
            _level.walls = (_level.walls ?? new WallData[0]).Where(w => In(WallRules.CellB(w).x, WallRules.CellB(w).y)).ToArray();
            _selectedClue = -1;
            ClearReport();
        }

        // =====================================================================
        // Asset / database
        // =====================================================================

        private void CreateLevel(LevelDefinition source)
        {
            EnsureFolder(CustomFolder);
            LevelDefinition level;
            if (source != null)
            {
                level = Instantiate(source);
            }
            else
            {
                level = CreateInstance<LevelDefinition>();
                level.size = 5;
                level.height = 5;
                level.clues = new ClueData[0];
                level.cellOverrides = new CellOverrideData[0];
                level.solution = new SolutionRectData[0];
                level.rules = new LevelRules(); // timeLimit 0 = tự tính theo số ô / khối
            }
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            level.id = "custom_" + stamp;
            level.source = new LevelSource { game = "Custom", sourceIndex = -1 };

            string path = AssetDatabase.GenerateUniqueAssetPath($"{CustomFolder}/Level_Custom_{stamp}.asset");
            level.name = System.IO.Path.GetFileNameWithoutExtension(path); // bản Instantiate mang đuôi "(Clone)"
            AssetDatabase.CreateAsset(level, path);
            Undo.RegisterCreatedObjectUndo(level, "Create Level");

            if (_database != null)
            {
                int index = DatabaseIndex();
                SetLevel(level);
                AddToDatabase(index >= 0 ? index + 1 : _database.Count);
            }
            else
            {
                SetLevel(level);
            }
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(level);
        }

        private void AddToDatabase(int index)
        {
            Undo.RecordObject(_database, "Add Level To Database");
            _database.levels.Insert(Mathf.Clamp(index, 0, _database.Count), _level);
            EditorUtility.SetDirty(_database);
        }

        private void MoveInDatabase(int index, int delta)
        {
            int target = index + delta;
            if (target < 0 || target >= _database.Count) return;
            Undo.RecordObject(_database, "Reorder Levels");
            (_database.levels[index], _database.levels[target]) = (_database.levels[target], _database.levels[index]);
            EditorUtility.SetDirty(_database);
        }

        /// <summary>Thêm mọi level trong Levels/Custom chưa có vào cuối database (Import ShikaCat Levels gọi để không mất level tự làm).</summary>
        public static void AppendCustomLevels(LevelDatabase database)
        {
            if (database == null || !AssetDatabase.IsValidFolder(CustomFolder)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { CustomFolder }))
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && !database.levels.Contains(level)) database.levels.Add(level);
            }
            EditorUtility.SetDirty(database);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // =====================================================================
        // Tiện ích
        // =====================================================================

        private int DatabaseIndex() => _database != null && _level != null ? _database.levels.IndexOf(_level) : -1;

        private int ClueCount() => _level.clues != null ? _level.clues.Length : 0;

        // [MECHANIC] Cơ chế giấu thông tin (như khóa / "?") thì thêm vào đây
        private bool HasHiddenInfo() => _level.clues != null
            && _level.clues.Any(c => c.mechanic.hidden || c.mechanic.lockSteps > 0 || c.mechanic.ambiguity != ClueAmbiguity.None);

        private int ClueIndexAt(Vector2Int pos)
        {
            if (_level.clues == null) return -1;
            for (int i = 0; i < _level.clues.Length; i++)
            {
                if (_level.clues[i].x == pos.x && _level.clues[i].y == pos.y) return i;
            }
            return -1;
        }

        private HashSet<Vector2Int> VoidSet()
        {
            HashSet<Vector2Int> set = new HashSet<Vector2Int>();
            if (_level.cellOverrides == null) return set;
            foreach (CellOverrideData o in _level.cellOverrides)
            {
                if (o.type == CellType.Void) set.Add(new Vector2Int(o.x, o.y));
            }
            return set;
        }

        private List<SolutionRectData> SolutionList() => (_level.solution ?? new SolutionRectData[0]).ToList();

        private static int NextClueId(List<ClueData> clues) => clues.Count == 0 ? 0 : clues.Max(c => c.id) + 1;

        private static bool Contains(SolutionRectData b, Vector2Int p) =>
            p.x >= b.x && p.y >= b.y && p.x < b.x + b.width && p.y < b.y + b.height;

        private static bool Overlaps(SolutionRectData b, RectInt r) =>
            b.x < r.xMax && r.x < b.x + b.width && b.y < r.yMax && r.y < b.y + b.height;

        private static string ClueLabel(ClueData c) => c.mechanic.hidden ? $"?{c.value}" : c.value.ToString();

        private static string ConstraintMark(ClueConstraint c) => c switch
        {
            ClueConstraint.Horizontal => "→",
            ClueConstraint.Vertical => "↑",
            ClueConstraint.Square => "□",
            _ => null
        };

        private static Rect Inset(Rect r, float d) => new Rect(r.x + d, r.y + d, r.width - 2 * d, r.height - 2 * d);

        private static void DrawOutline(Rect r, Color c, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        private static GUIStyle CenterStyle(float size, Color color) => new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(8, Mathf.RoundToInt(size)),
            normal = { textColor = color }
        };

        private static GUIStyle CornerStyle(float size) => new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.Max(7, Mathf.RoundToInt(size)),
            normal = { textColor = new Color(1f, 0.9f, 0.5f) }
        };

        private static GUIStyle RightStyle(float size) => new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = Mathf.Max(7, Mathf.RoundToInt(size)),
            normal = { textColor = new Color(1f, 0.75f, 0.75f) }
        };
    }

    /// <summary>Chơi thử từ Level Editor: vào Play mode xong thì bỏ qua Menu, vào thẳng màn chơi.</summary>
    [InitializeOnLoad]
    public static class LevelEditorPlayTest
    {
        private const string Key = "PawBlock_LevelEditorPlayTest";

        static LevelEditorPlayTest()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void Request() => SessionState.SetBool(Key, true);

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            // Chờ 1 frame để ScreenFlow.Start (mở Menu) chạy xong rồi mới vào màn chơi
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying) return;
                foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) // Editor được tìm (guide 8.3)
                {
                    if (mb is IScreenNavigator screens)
                    {
                        screens.Play();
                        break;
                    }
                }
            };
        }
    }
}
