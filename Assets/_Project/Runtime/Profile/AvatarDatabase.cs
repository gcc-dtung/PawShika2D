using UnityEngine;

namespace PawBlock.Features.Profile
{
    /// <summary>Danh sách ảnh avatar (thứ tự = AvatarId lưu trong save, chỉ thêm vào cuối). Asset: Configs/AvatarDatabase.asset.</summary>
    [CreateAssetMenu(menuName = "PawBlock/Avatar Database", fileName = "AvatarDatabase")]
    public class AvatarDatabase : ScriptableObject
    {
        public Sprite[] avatars = new Sprite[0];

        public int Count => avatars != null ? avatars.Length : 0;

        /// <summary>Ảnh của <paramref name="id"/>; id không có (vd avatar của người khác mà máy này chưa có) thì ảnh 0.</summary>
        public Sprite Get(int id)
        {
            if (Count == 0) return null;
            return id >= 0 && id < Count && avatars[id] != null ? avatars[id] : avatars[0];
        }
    }
}
