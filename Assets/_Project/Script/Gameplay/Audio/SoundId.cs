/// <summary>Các âm thanh của game. Clip gán trong AudioDatabase (mỗi âm nhiều clip thì phát ngẫu nhiên).</summary>
public enum SoundId
{
    None = 0,

    // UI
    Click = 1,
    PopupOpen = 2,
    PopupClose = 3,
    Coin = 4,
    Reward = 5, // nhận thưởng (bấm Continue / x2 ở màn thắng)

    // Màn chơi
    LevelStart = 10,
    BlockPlace = 11,
    Mistake = 12,
    Unlock = 13,
    Win = 14,
    Lose = 15,
    OutOfTime = 16,
    HardLevel = 17,   // vào level Hard / Very Hard (băng cảnh báo)
    TimeWarning = 18, // đồng hồ vừa xuống dưới ngưỡng cảnh báo
    BlockRemove = 19, // chạm khối để gỡ

    // Booster
    Booster = 20,
    FreezeOn = 21,
    FreezeEnd = 22,

    // Kéo chọn vùng
    DragTick = 40,  // vùng kéo đổi số ô (cao độ tăng theo số ô)
    DragReady = 41, // vùng kéo vừa đủ điều kiện đặt
    DragCancel = 42, // thả tay ở vùng sai (khung co xám)

    // Sao (thanh thời gian / màn thắng)
    StarLost = 50, // thời gian tụt qua 1 mốc sao
    StarEarn = 51, // màn thắng: mỗi sao đập xuống (cao độ tăng dần)

    // Tiếng mèo (phát kèm, xác suất)
    CatHappy = 30,
    CatSad = 31
}
