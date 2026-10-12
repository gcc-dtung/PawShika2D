namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Mở các popup dùng chung từ nút ở feature (bánh răng, Daily, avatar, năng lượng). Game cài đặt (GameFlowUI):
    /// GameFlowUI quyết định có được mở lúc này không (vd trong ván đang có popup khác thì bỏ qua).
    /// </summary>
    public interface IMenuPopups
    {
        void OpenSettings();
        void OpenDailyReward();
        void OpenProfile();
        void OpenEnergyRefill();
    }
}
