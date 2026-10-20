using System;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    [CreateAssetMenu(fileName = "SO_LevelDefinition", menuName = "ScriptableObjects/LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string id;
        public LevelSource source;
        public int size;
        public int height;

        public int Height => height > 0 ? height : size;

        public CellOverrideData[] cellOverrides;
        public ClueData[] clues;
        public SolutionRectData[] solution;
        [Tooltip("Tường giữa 2 ô kề nhau: khối không được nằm vắt qua (WallRules)")]
        public WallData[] walls;
        public LevelRules rules;
        [Tooltip("Hard / Very Hard: nút PLAY, khung HUD riêng và hiệu ứng vào màn (DifficultyTheme)")]
        public LevelDifficulty difficulty;
    }

    // [MECHANIC] Cơ chế theo Ô (như Void): thêm giá trị enum, ô đặt qua cellOverrides. Xem Docs/Features/HowTo_ThemMechanic.md
    public enum CellType
    {
        Playable = 0,
        Void = 1
    }

    public enum ClueConstraint
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Square = 3
    }

    /// <summary>
    /// Ô số phải nằm ở góc / cạnh này của khối (theo hướng màn hình: Top = phía trên, y lớn).
    /// Góc: đúng ô góc. Cạnh: hàng / cột ngoài cùng phía đó, vị trí dọc theo cạnh tùy ý.
    /// </summary>
    public enum ClueCorner
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 3,
        BottomRight = 4,
        Top = 5,
        Bottom = 6,
        Left = 7,
        Right = 8
    }

    [Serializable]
    public struct LevelSource
    {
        public string game;
        public int sourceIndex;
    }

    [Serializable]
    public struct CellOverrideData
    {
        public int x;
        public int y;
        public CellType type;
    }

    [Serializable]
    public struct ClueData
    {
        public int id;
        public int x;
        public int y;
        public int value;
        public int color;
        public ClueMechanic mechanic;
    }

    [Serializable]
    // [MECHANIC] Cơ chế theo Ô số: thêm field ở đây, mặc định = tắt (asset cũ không cần sửa). Xem Docs/Features/HowTo_ThemMechanic.md
    public struct ClueMechanic
    {
        public ClueConstraint constraint;
        public int lockSteps;
        public bool hidden;
        [Tooltip("Ô số phải nằm ở góc / cạnh này của khối (dấu ngoặc góc / vạch cạnh trên ô)")]
        public ClueCorner corner;
    }

    /// <summary>Tường nằm ở cạnh nào của ô (x, y): Right = giữa (x, y) và (x + 1, y); Top = giữa (x, y) và (x, y + 1).</summary>
    public enum WallSide
    {
        Right = 0,
        Top = 1
    }

    [Serializable]
    public struct WallData
    {
        public int x;
        public int y;
        public WallSide side;
    }

    [Serializable]
    public struct SolutionRectData
    {
        public int clueId;
        public int x;
        public int y;
        public int width;
        public int height;
    }

    [Serializable]
    public struct LevelRules
    {
        [Tooltip("Thời gian của level (giây). 0 = tự tính: 8 s + 0.25 s/ô + 5.5–6.5 s/khối, tối thiểu 45 s (LivesEnergyConfig)")]
        [Min(0)] public int timeLimit;
    }
}
