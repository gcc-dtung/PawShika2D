using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    // [MECHANIC] Thêm loại để Mechanic Injector gắn tự động (+ MechanicRule mặc định bên dưới, + TryX trong MechanicInjector)
    public enum MechanicKind
    {
        Void = 0,
        Constraint = 1,
        Lock = 2,
        Hidden = 3,
        Wall = 4,   // tường (hàng rào) giữa 2 ô
        Corner = 5  // ô số ở góc / cạnh của khối
    }

    /// <summary>
    /// Cấu hình Mechanic Injector (Tools → PawBlock → Mechanic Injector): level nào giới thiệu cơ chế nào,
    /// tỉ lệ level có cơ chế, kết hợp, số lượng mỗi cơ chế. Cùng cấu hình + cùng seed = cùng kết quả.
    /// </summary>
    public class MechanicInjectorSettings : ScriptableObject
    {
        [Serializable]
        public class MechanicRule
        {
            public MechanicKind kind;
            [Tooltip("Level (số thứ tự trong database) bắt đầu giới thiệu cơ chế; 0 = tắt cơ chế")]
            public int introLevel;
            [Tooltip("Số level liền nhau chỉ có cơ chế này (dạy người chơi)")]
            public int introLength = 3;
            [Tooltip("Số lượng mỗi level: Void = số khối bỏ, Constraint = số mũi tên, Lock = số ô khóa, Hidden = số ô \"?\", Wall = số đoạn tường, Corner = số ô góc / cạnh. Level giới thiệu dùng min")]
            public Vector2Int amount = new Vector2Int(1, 2);
            [Tooltip("Trọng số khi random cơ chế cho level thường")]
            public float weight = 1f;
        }

        [Tooltip("Đổi seed = bộ level khác hoàn toàn")]
        public int seed;

        public List<MechanicRule> mechanics = new List<MechanicRule>
        {
            // Mọi cơ chế giới thiệu trước level 20 (level xếp theo độ khó bằng Reorder Levels)
            new MechanicRule { kind = MechanicKind.Void, introLevel = 5, introLength = 2, amount = new Vector2Int(1, 2), weight = 1f },
            new MechanicRule { kind = MechanicKind.Constraint, introLevel = 8, introLength = 2, amount = new Vector2Int(2, 3), weight = 1f },
            new MechanicRule { kind = MechanicKind.Lock, introLevel = 12, introLength = 2, amount = new Vector2Int(1, 2), weight = 1f },
            new MechanicRule { kind = MechanicKind.Hidden, introLevel = 16, introLength = 2, amount = new Vector2Int(1, 2), weight = 1f },
            new MechanicRule { kind = MechanicKind.Wall, introLevel = 24, introLength = 2, amount = new Vector2Int(3, 5), weight = 1f },
            new MechanicRule { kind = MechanicKind.Corner, introLevel = 27, introLength = 2, amount = new Vector2Int(2, 3), weight = 1f }
        };

        [Header("Level thường (sau giai đoạn giới thiệu)")]
        [Range(0f, 1f), Tooltip("Xác suất 1 level có cơ chế")]
        public float mechanicChance = 0.7f;
        [Tooltip("Level chia hết cho số này là level nghỉ: không cơ chế (0 = tắt)")]
        public int reliefEvery = 10;
        [Tooltip("Cơ chế mới nhất được ưu tiên trong số level này sau khi giới thiệu xong")]
        public int newestBoostLevels = 10;
        public float newestBoost = 3f;

        [Header("Kết hợp nhiều cơ chế")]
        [Tooltip("Từ level này mới random kết hợp 2 cơ chế")]
        public int comboStartLevel = 30;
        [Range(0f, 1f)]
        public float comboChance = 0.25f;
        [Tooltip("Level Hard / Very Hard luôn kết hợp 2 / 3 cơ chế (trong số đã mở), ưu tiên \"?\" → khóa → Void, không dùng hướng / tường / góc (cho thêm thông tin)")]
        public bool comboOnHardLevels = true;

        [Header("Giới hạn")]
        [Tooltip("Số bước khóa (min, max); không vượt quá số khối còn lại")]
        public Vector2Int lockSteps = new Vector2Int(2, 5);
        [Tooltip("Sau khi bỏ khối thành Void, level còn ít nhất bấy nhiêu khối")]
        public int minCluesAfterVoid = 3; // level đầu nhỏ (4–6 khối) vẫn giới thiệu được Void
        [Range(0f, 0.6f), Tooltip("Tỉ lệ ô bị bỏ tối đa so với level gốc")]
        public float maxVoidRatio = 0.3f;
        [Tooltip("Số lần thử mỗi cơ chế trước khi bỏ cuộc (mỗi lần thử chạy LevelValidator)")]
        public int maxAttempts = 8;

        public MechanicRule Get(MechanicKind kind) => mechanics.Find(m => m.kind == kind);
    }
}
