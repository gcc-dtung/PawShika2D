using System;

namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Luồng màn hình Menu ↔ màn chơi. Game cài đặt (ScreenFlow); feature hỏi đang ở Menu không, hoặc yêu cầu đổi màn.
    /// Chỉ cần biết đã đổi màn: nghe ScreenChangedEvent. Mở Shop: phát ShopOpenRequestedEvent.
    /// </summary>
    public interface IScreenNavigator
    {
        bool IsMenu { get; }

        /// <summary>Về Main Menu (tab Home, hoặc Daily nếu vừa chơi level Daily).</summary>
        void ShowMenu();

        /// <summary>Nút PLAY: kiểm tra năng lượng rồi vào level thường hiện tại.</summary>
        void Play();

        /// <summary>Kiểm tra năng lượng rồi vào ván, <paramref name="load"/> nạp level Daily.</summary>
        void PlayDaily(Action load);
    }
}
