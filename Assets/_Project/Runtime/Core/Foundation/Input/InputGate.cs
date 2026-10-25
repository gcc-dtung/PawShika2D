using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Khóa thao tác trên lưới theo "chủ khóa": ai cần chặn kéo thả (intro, tutorial, Reveal đang tự đặt khối, ván không Playing...)
    /// thì Lock(this), xong thì Unlock(this). GridManager chỉ hỏi IsLocked, không cần biết ai đang khóa.
    /// Thêm 1 lớp phủ mới chặn input: chỉ gọi Lock / Unlock, không sửa GridManager.
    /// </summary>
    public static class InputGate
    {
        private static readonly HashSet<object> Owners = new HashSet<object>();

        /// <summary>Số chủ khóa đổi từ 0 sang &gt;0 hoặc ngược lại.</summary>
        public static event Action<bool> Changed;

        public static bool IsLocked
        {
            get
            {
                // Chủ khóa là object Unity đã bị hủy mà quên Unlock: bỏ đi để lưới không kẹt khóa mãi
                if (Owners.Count > 0) Owners.RemoveWhere(o => o is UnityEngine.Object u && u == null);
                return Owners.Count > 0;
            }
        }

        public static void Lock(object owner) => Set(owner, true);

        public static void Unlock(object owner) => Set(owner, false);

        public static void Set(object owner, bool locked)
        {
            if (owner == null) return;
            bool before = IsLocked;
            if (locked) Owners.Add(owner);
            else Owners.Remove(owner);
            if (before != IsLocked) Changed?.Invoke(IsLocked);
        }

        public static bool IsLockedBy(object owner) => owner != null && Owners.Contains(owner);

        /// <summary>Danh sách chủ khóa hiện tại (Debug).</summary>
        public static string Describe()
        {
            if (Owners.Count == 0) return "(mở)";
            List<string> names = new List<string>(Owners.Count);
            foreach (object o in Owners) names.Add(o is UnityEngine.Object u && u != null ? u.GetType().Name : o.ToString());
            return string.Join(", ", names);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Owners.Clear();
            Changed = null;
        }
    }
}
