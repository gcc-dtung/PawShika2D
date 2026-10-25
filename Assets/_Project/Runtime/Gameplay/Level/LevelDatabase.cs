using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>Danh sách level theo thứ tự chơi. Tạo/cập nhật bằng menu Tools/PawBlock/Builders/Import ShikaCat Levels.</summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "ScriptableObjects/LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        public List<LevelDefinition> levels = new List<LevelDefinition>();

        public int Count => levels.Count;

        public LevelDefinition Get(int index)
        {
            if (levels.Count == 0) return null;
            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }
    }
}
