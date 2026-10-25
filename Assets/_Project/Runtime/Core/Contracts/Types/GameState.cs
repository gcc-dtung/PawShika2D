namespace PawBlock.Core.Contracts
{
    /// <summary>Trạng thái 1 ván chơi. Chỉ Playing mới nhận thao tác kéo trên lưới.</summary>
    public enum GameState
    {
        Playing,
        Paused,     // popup mở giữa ván (xác nhận chơi lại)
        Won,        // phủ kín lưới -> WinPopup
        OutOfTime,  // hết giờ -> MoreTimePopup
        Lost        // từ chối thêm giờ -> LosePopup
    }
}
