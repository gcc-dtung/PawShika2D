using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.Profile
{
    /// <summary>Avatar + khung đè lên trên. Dùng ở nút avatar Main Menu, popup hồ sơ, xem trước khi sửa hồ sơ.</summary>
    public class ProfileAvatarView : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image frameImage;

        public void Set(AvatarDatabase db, int avatarId, int frameId)
        {
            if (db == null) return;
            if (avatarImage != null) avatarImage.sprite = db.Get(avatarId);
            if (frameImage != null)
            {
                frameImage.sprite = db.Frame(frameId);
                frameImage.enabled = frameImage.sprite != null;
            }
        }
    }
}
