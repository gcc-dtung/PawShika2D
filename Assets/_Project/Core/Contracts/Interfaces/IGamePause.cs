namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Tạm dừng / chạy tiếp ván đang chơi (popup của feature khác mở giữa ván). Gameplay cài đặt (LevelManager).
    /// </summary>
    public interface IGamePause
    {
        /// <summary>Ván đang Playing (người chơi thao tác được).</summary>
        bool IsPlaying { get; }

        /// <summary>Playing -> Paused (không làm gì nếu không Playing).</summary>
        void Pause();

        /// <summary>Paused -> Playing.</summary>
        void Resume();

        /// <summary>Hủy thao tác kéo đang dở trên lưới (trước khi mở popup che màn chơi).</summary>
        void CancelDrag();
    }
}
