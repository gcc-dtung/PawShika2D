using System;
using TMPro;
using UnityEngine;

namespace PawBlock.Features.Profile
{
    /// <summary>1 món trang trí hồ sơ (avatar / khung / huy hiệu): ảnh + level mở khóa (0 = có sẵn).</summary>
    [Serializable]
    public struct ProfileItem
    {
        public Sprite sprite;
        public string title;  // tên hiển thị (huy hiệu)
        [Min(0)] public int unlockLevel;
    }

    /// <summary>Kiểu tên: màu chuyển từ trên xuống dưới.</summary>
    [Serializable]
    public struct NameStyle
    {
        public Color top;
        public Color bottom;
        [Min(0)] public int unlockLevel;

        /// <summary>Tô chữ <paramref name="text"/> theo kiểu này (chữ trắng + gradient đỉnh / đáy).</summary>
        public void ApplyTo(TMP_Text text)
        {
            if (text == null) return;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, bottom, bottom);
        }
    }

    /// <summary>
    /// Đồ trang trí hồ sơ: avatar, khung avatar, kiểu tên, huy hiệu. Thứ tự = id lưu trong save, chỉ thêm vào cuối.
    /// Asset: Data/Config/AvatarDatabase.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Avatar Database", fileName = "AvatarDatabase")]
    public class AvatarDatabase : ScriptableObject
    {
        public ProfileItem[] avatars = new ProfileItem[0];
        public ProfileItem[] frames = new ProfileItem[0];
        public NameStyle[] nameStyles = new NameStyle[0];
        public ProfileItem[] badges = new ProfileItem[0];

        public int Count => avatars != null ? avatars.Length : 0;

        /// <summary>Ảnh của <paramref name="id"/>; id không có (vd avatar của người khác mà máy này chưa có) thì ảnh 0.</summary>
        public Sprite Get(int id) => Pick(avatars, id, 0);

        /// <summary>Khung <paramref name="id"/>; không có thì khung 0.</summary>
        public Sprite Frame(int id) => Pick(frames, id, 0);

        /// <summary>Huy hiệu <paramref name="id"/>; id &lt; 0 (chưa đeo) hoặc không có thì null.</summary>
        public Sprite Badge(int id) => badges != null && id >= 0 && id < badges.Length ? badges[id].sprite : null;

        public string BadgeTitle(int id) => badges != null && id >= 0 && id < badges.Length ? badges[id].title : null;

        public NameStyle Style(int id)
        {
            if (nameStyles == null || nameStyles.Length == 0) return new NameStyle { top = Color.white, bottom = Color.white };
            return nameStyles[id >= 0 && id < nameStyles.Length ? id : 0];
        }

        private static Sprite Pick(ProfileItem[] items, int id, int fallback)
        {
            if (items == null || items.Length == 0) return null;
            return id >= 0 && id < items.Length && items[id].sprite != null ? items[id].sprite : items[fallback].sprite;
        }
    }
}
