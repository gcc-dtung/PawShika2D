namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// 1 Update duy nhất chạy khung hình cho mọi CatFaceAnimator, thay vì mỗi khối mèo một Update.
    /// Đặt sẵn trong scene (cùng GridManager); không có thì mặt mèo đứng yên ở khung đầu.
    /// </summary>
    public class CatFaceTicker : UnityEngine.MonoBehaviour
    {
        private void Update() => CatFaceAnimator.TickAll(UnityEngine.Time.time);
    }
}
