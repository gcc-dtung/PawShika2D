using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Level Daily Challenge: sinh bằng LevelGenerator (seed = ngày yyyyMMdd, cùng ngày mọi máy cùng level) rồi nạp vào
    /// LevelManager.LoadDaily. Level tạo trong bộ nhớ, đổi ngày thì hủy level cũ. DailyChallenge gọi qua IDailyLevelLoader.
    /// </summary>
    public class DailyLevelLoader : MonoBehaviour, IDailyLevelLoader
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        [Tooltip("Thông số sinh level mỗi ngày (seed = ngày); gốc: lưới 10 x 10")]
        [SerializeField] private LevelGeneratorSettings generator = LevelGeneratorSettings.Preset(LevelGeneratorPreset.Daily);

        private LevelDefinition _level; // level Daily đang nạp (tạo trong bộ nhớ)

        public bool LoadDaily(long day)
        {
            if (_levels == null) return false;
            LevelDefinition level = CreateLevel(day);
            if (level == null)
            {
                Debug.LogError($"[DailyLevelLoader] Không sinh được level ngày {DayNumber.ToDate(day):yyyy-MM-dd}.");
                return false;
            }
            LevelDefinition old = _level;
            _level = level;
            _levels.LoadDaily(level, day);
            if (old != null && old != level) Destroy(old);
            return true;
        }

        /// <summary>Level của 1 ngày: cùng ngày luôn ra cùng level (seed = yyyyMMdd).</summary>
        public LevelDefinition CreateLevel(long day)
        {
            int seed = LevelGenerator.SeedForDate(DayNumber.ToDate(day));
            LevelDefinition level = LevelGenerator.Generate(generator, seed).level;
            if (level != null) level.name = level.id = $"Daily_{DayNumber.ToDate(day):yyyyMMdd}";
            return level;
        }
    }
}
