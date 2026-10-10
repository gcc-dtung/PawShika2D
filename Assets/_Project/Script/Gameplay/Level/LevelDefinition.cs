using System;
using UnityEngine;

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
