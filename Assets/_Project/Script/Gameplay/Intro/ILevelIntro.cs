using System;

/// <summary>
/// 1 lớp phủ chạy sau khi nạp level (băng HARD, popup cơ chế, tutorial booster...). LevelIntroSequencer tự tìm mọi
/// component có interface này trong scene và chạy lần lượt theo Order, mỗi lúc 1 cái, lưới bị khóa trong lúc chạy.
/// Thêm intro mới: viết component implement ILevelIntro, đặt vào scene; không sửa sequencer hay GridManager.
/// </summary>
public interface ILevelIntro
{
    /// <summary>Nhỏ chạy trước.</summary>
    int Order { get; }

    /// <summary>Chờ thêm bao lâu (giây) sau khi đủ điều kiện rồi mới chạy.</summary>
    float StartDelay { get; }

    /// <summary>Level vừa nạp có cần chạy intro này không. Hỏi lúc nạp level và hỏi lại ngay trước khi chạy.</summary>
    bool ShouldPlay(LevelLoadedEvent level);

    /// <summary>Bắt đầu chạy; xong thì gọi <paramref name="onDone"/> đúng 1 lần.</summary>
    void Play(LevelLoadedEvent level, Action onDone);

    /// <summary>Hủy giữa chừng (nạp level khác): tắt giao diện, không gọi onDone.</summary>
    void Cancel();
}
