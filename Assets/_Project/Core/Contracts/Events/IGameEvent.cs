namespace PawBlock.Core.Contracts
{
    /// <summary>
    /// Đánh dấu struct là sự kiện đi qua EventBus. Thêm sự kiện mới: thêm 1 file readonly struct : IGameEvent
    /// trong Core/Contracts/Events, bên phát gọi EventBus&lt;X&gt;.Raise, bên nghe Subscribe / Unsubscribe.
    /// </summary>
    public interface IGameEvent { }
}
