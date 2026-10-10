/// <summary>Các âm thanh của game. Clip gán trong AudioDatabase (mỗi âm nhiều clip thì phát ngẫu nhiên).</summary>
public enum SoundId
{
    None = 0,

    // UI
    Click = 1,
    PopupOpen = 2,
    PopupClose = 3,
    Coin = 4,

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

    // Tiếng mèo (phát kèm, xác suất)
    CatHappy = 30,
    CatSad = 31
}
